using System;
using System.IO;
using Assets.SoundCutter.Scripts.Threads;
using Assets.SoundCutter.Scripts.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.SoundCutter.Scripts.Editor
{
    [ExecuteInEditMode]
    public class SoundCutterEditorWindow : EditorWindow
    {
        private const String FilenamePostfix = "";
        private const int BorderWidth = 16;
        private const float BorderHalfWidth = BorderWidth * 0.5f;

        private static readonly Color WaveColor = new Color(1.0f, 0.54f, 0f);
        private static readonly Color WaveBackColor = new Color(0.19f, 0.19f, 0.19f);
        private static readonly Color SelectionColor =  new Color(1f, 1f, 1f, 0.2f);
        private static readonly Color IndicatorColor = new Color(1f, 1f, 1f, 0.2f);

        private AudioClip _sourceClip;
        private AudioClip _currentClip;
        private AudioClipData _currentClipData;

        private AudioSource _audioPlayer;
        private SaveAudioJob _job;
        private GUIStyle _labelStyle;

        private float _maxValue = 1.0f;
        private float _minValue;

        private float _prevMaxValue = 1.0f;
        private float _prevMinValue;

        private bool _isRegion;
        private bool _isDirty;        
        
        private GUIStyle _regionTimeLabelStyle;
        private GUIStyle _timeLabelStyle;
        private GUIStyle _toolbarButtonStyle;
        private GUIStyle _toolbarStyle;

        private Rect _timelineRect;
        private Rect _waveAreaRect;
        private Rect _waveTextureRect;
        private Rect _positionIndicatorRect;

        private Texture2D _positionIndicatorTexture;
        private Texture2D _selectionTexture;
        private Texture2D _waveFormTexture;                                        

        [MenuItem("Assets/Sound Cutter")]
        public static void ShowWindow()
        {
            var window = GetWindow<SoundCutterEditorWindow>();
            window.titleContent = new GUIContent("Sound Cutter");                        
        }

        [MenuItem("Assets/Sound Cutter", true)]
        public static bool ShowWindowValidation()
        {
            return Selection.activeObject is AudioClip;
        }
        
        private AudioClip GetSelectedAudioClipAsset()
        {
            string[] guids = Selection.assetGUIDs;
            if (guids != null && guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);                
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                return clip;
            }

            return null;
        }

        private bool IsGuiEnabled()
        {
            return _job == null;
        }

        private void OnGUI()
        {
            InitStyles();

            GUI.enabled = IsGuiEnabled();

            AudioClip audioClip = GetSelectedAudioClipAsset();
            if (audioClip == null)
            {
                var selectAudiClipLabelRect = new Rect(0, 0, position.width, position.height);
                EditorGUI.LabelField(selectAudiClipLabelRect, "Select Audio Clip to Edit", EditorStyles.centeredGreyMiniLabel);                
                _sourceClip = null;
                return;
            }
            if (audioClip != _sourceClip)
            {
                StopPlayerIfPlaying();
                _sourceClip = audioClip;
                _currentClipData = AudioClipData.FromAudioClip(_sourceClip);
                _currentClip = _sourceClip;

                _minValue = 0.0f;
                _maxValue = 1.0f;

                _isDirty = true;
            }

            if (_currentClipData == null)
            {
                _currentClipData = AudioClipData.FromAudioClip(_sourceClip);
                _currentClip = _sourceClip;
            }

            _isDirty |= _waveFormTexture == null;
            _isDirty |= _waveFormTexture != null && (int) position.width - BorderWidth != _waveFormTexture.width;

            if (_isDirty)
            {                
                _waveFormTexture = SoundWaveUtils.DrawWaveForm(_currentClipData, (int) position.width - BorderWidth, 128,
                    WaveColor, WaveBackColor);                

                _isDirty = false;
            }

            _timelineRect.Set(0, 20, position.width, 20);

            EditorGUI.DrawRect(_timelineRect, new Color(1f,1f,1f, 0.2f));

            var texturesHeight = position.height - 80;

            _waveTextureRect.Set(BorderWidth * 0.5f, _timelineRect.yMax, position.width - BorderWidth,
                texturesHeight);

            _waveAreaRect.Set(0, _timelineRect.yMax, position.width, texturesHeight);

            EditorGUI.DrawRect(_waveAreaRect, new Color(1f, 1f, 1f, 0.2f));

            GUILayout.Space(5);

            GUILayout.BeginHorizontal(_toolbarStyle);
            GUILayout.FlexibleSpace();            

            ShowDeleteExceptSelectionButton();
            ShowDeleteButton();
            ShowSaveButton();

            GUILayout.EndHorizontal();

            Color prevColor = GUI.color;

            EditorGUI.DrawPreviewTexture(_waveTextureRect, _waveFormTexture);

            HandleEvents();


            _waveTextureRect.x = BorderHalfWidth + _waveTextureRect.width * _minValue;
            _waveTextureRect.width = _waveTextureRect.width * (_maxValue - _minValue);
          
            if (_selectionTexture == null)
            {
                _selectionTexture = SoundWaveUtils.DrawOnePixelTexture(new Color(SelectionColor.r, SelectionColor.g, SelectionColor.b, 0.02f));
            }

            GUI.color = new Color(SelectionColor.r, SelectionColor.g, SelectionColor.b, 0.2f);
            GUI.DrawTexture(_waveTextureRect, _selectionTexture, ScaleMode.StretchToFill, true);
            GUI.color = prevColor;

            bool isPlaying = IsPlaying();

            if (isPlaying)
            {
                ShowPositionIndicator();
            }

            ShowTimeLabels(isPlaying);

            GUILayout.Space(_waveAreaRect.yMax - 21);

            GUILayout.BeginHorizontal(_toolbarStyle);            

            GUILayout.Label(audioClip.name, _labelStyle, GUILayout.MinWidth(30));
            GUILayout.Space(30);
            ShowPlayButton(isPlaying);

            GUILayout.EndHorizontal();
            
            EditorGUILayout.MinMaxSlider(ref _minValue, ref _maxValue, 0.0f, 1.0f);                
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private bool IsPlaying()
        {
            return _audioPlayer != null && _audioPlayer.isPlaying;
        }

        private void InitStyles()
        {
            _toolbarStyle = _toolbarStyle ?? GUI.skin.FindStyle("PreToolbar");
            _toolbarButtonStyle = _toolbarButtonStyle ?? GUI.skin.FindStyle("PreButton");
            _labelStyle = _labelStyle ??
                          new GUIStyle(GUI.skin.FindStyle("PreLabel")) {alignment = TextAnchor.MiddleLeft};
            _timeLabelStyle = _timeLabelStyle ??
                              new GUIStyle(EditorStyles.whiteBoldLabel) {alignment = TextAnchor.UpperCenter};
            _regionTimeLabelStyle = _regionTimeLabelStyle ??
                                    new GUIStyle(EditorStyles.whiteBoldLabel) {alignment = TextAnchor.LowerCenter};
        }

        private void ShowPositionIndicator()
        {
            if (_positionIndicatorTexture == null)
            {
                _positionIndicatorTexture = SoundWaveUtils.DrawOnePixelTexture(IndicatorColor);
            }

            EditorGUI.DrawPreviewTexture(_positionIndicatorRect, _positionIndicatorTexture);
        }

        private void ShowPlayButton(bool isPlaying)
        {
            string playButtonText = isPlaying ? "\u25a0" : "\u25ba";
            string playButtonTooltip = isPlaying ? "Stop" : "Play selected region";

            if (GUILayout.Button(new GUIContent(playButtonText, playButtonTooltip), _toolbarButtonStyle,
                GUILayout.Width(30)))
            {
                ToggleSelectionPlayStop(isPlaying);
            }
        }

        private void ToggleSelectionPlayStop(bool isPlaying)
        {
            CreateAudioPlayerIfNeeded();

            if (isPlaying)
            {
                StopPlayerIfPlaying();
            }
            else
            {
                PlaySelectedRegion();

                _positionIndicatorRect.Set(_waveTextureRect.x, _waveTextureRect.yMin, 1, _waveTextureRect.height);
            }
        }

        private void ShowTimeLabels(bool isPlaying)
        {
            var labelRect = new Rect(0, _waveAreaRect.yMin, position.width, _waveAreaRect.height);

            if (isPlaying)
            {
                TimeSpan playerTime = TimeSpan.FromSeconds(_audioPlayer.time);
                string timeLabelText = string.Format("{0:D2}:{1:D2}.{2:D3}", playerTime.Minutes, playerTime.Seconds,
                    playerTime.Milliseconds);
                EditorGUI.DropShadowLabel(labelRect, timeLabelText, _timeLabelStyle);
            }           

            float regionSeconds = Mathf.Abs(_maxValue - _minValue) * _currentClip.length;
            TimeSpan regionTimeSpan = TimeSpan.FromSeconds(regionSeconds);
            string regionTimeLabelText = string.Format("{0:D2}:{1:D2}.{2:D3}", regionTimeSpan.Minutes,
                regionTimeSpan.Seconds, regionTimeSpan.Milliseconds);
            EditorGUI.DropShadowLabel(labelRect, regionTimeLabelText, _regionTimeLabelStyle);
        }        

        private void ShowDeleteExceptSelectionButton()
        {
            GUI.enabled = IsGuiEnabled() && (_minValue > 0.0f || _maxValue < 1.0f);                        

            if (GUILayout.Button(new GUIContent("Keep selection", "Keep selection and remove unselected area"), _toolbarButtonStyle,
                    GUILayout.Width(120)))
            {
                StopPlayerIfPlaying();

                _currentClipData.RemoveDataExceptRange(_minValue, _maxValue);
                _currentClip = _currentClipData.CreateAudioClip();

                _minValue = 0.0f;
                _maxValue = 1.0f;
                _isDirty = true;

                Repaint();                
            }
            GUI.enabled = IsGuiEnabled();
        }

        private void ShowDeleteButton()
        {
            GUI.enabled = IsGuiEnabled() && (_minValue > 0.0f || _maxValue < 1.0f);

            if (GUILayout.Button(new GUIContent("Remove selection", "Remove selected region"), _toolbarButtonStyle,
                    GUILayout.Width(130)))
            {
                StopPlayerIfPlaying();               

                _currentClipData.RemoveDataRange(_minValue, _maxValue);
                _currentClip = _currentClipData.CreateAudioClip();

                _minValue = 0.0f;
                _maxValue = 1.0f;

                _isDirty = true;
                Repaint();                
            }
            GUI.enabled = IsGuiEnabled();
        }

        private void ShowSaveButton()
        {
            if (GUILayout.Button(new GUIContent("Save", "Save to new file"), _toolbarButtonStyle,
                GUILayout.Width(40)))
            {
                if (_currentClipData != null)
                {
                    string fileName = _sourceClip.name + FilenamePostfix;

                    var sourceFilePath = AssetDatabase.GetAssetPath(_sourceClip);                                       
                    var dirName = Path.GetDirectoryName(sourceFilePath);
                    var fullDirPath = Path.GetFullPath(dirName);                    
                    
                    _job = new SaveAudioJob(fileName, fullDirPath, _currentClipData);
                    _job.Start();

                    StopPlayerIfPlaying();
                }
            }
        }

        private void HandleEvents()
        {
            Event currentEvent = Event.current;
            if (_waveAreaRect.Contains(currentEvent.mousePosition))
            {
                switch (currentEvent.type)
                {
                    case EventType.MouseDown:
                        StopPlayerIfPlaying();
                        if (currentEvent.button == 0)
                        {
                            _minValue = (currentEvent.mousePosition.x - BorderHalfWidth) / _waveTextureRect.width;
                            _minValue = Mathf.Clamp(_minValue, 0.0f, 1.0f);
                            _prevMinValue = _minValue;
                        }
                        else if (currentEvent.button == 1)
                        {
                            _maxValue = (currentEvent.mousePosition.x - BorderHalfWidth) / _waveTextureRect.width;
                            _maxValue = Mathf.Clamp(_maxValue, 0.0f, 1.0f);
                            _prevMaxValue = _maxValue;
                        }
                        Repaint();
                        break;
                    case EventType.MouseDrag:
                        if (currentEvent.button == 0)
                        {
                            _maxValue = (currentEvent.mousePosition.x - BorderHalfWidth) / _waveTextureRect.width;
                            _maxValue = Mathf.Clamp(_maxValue, 0.0f, 1.0f);
                            if (_maxValue < _prevMinValue)
                            {
                                _minValue = _maxValue;
                                _maxValue = _prevMinValue;
                            }
                        }
                        else if (currentEvent.button == 1)
                        {
                            _minValue = (currentEvent.mousePosition.x - BorderHalfWidth) / _waveTextureRect.width;
                            _minValue = Mathf.Clamp(_minValue, 0.0f, 1.0f);
                            if (_minValue > _prevMaxValue)
                            {
                                _maxValue = _minValue;
                                _minValue = _prevMaxValue;
                            }
                        }
                        Repaint();
                        break;
                    case EventType.MouseUp:
                        if (_minValue > _maxValue)
                        {
                            float tmp = _minValue;
                            _minValue = _maxValue;
                            _maxValue = tmp;
                        }
                        Repaint();
                        break;
                }
            }

            if (_timelineRect.Contains(currentEvent.mousePosition))
            {
                switch (currentEvent.type)
                {
                    case EventType.MouseDown:
                        if (currentEvent.button == 0)
                        {
                            CreateAudioPlayerIfNeeded();
                            if (_audioPlayer != null)
                            {                                
                                float startTime = (currentEvent.mousePosition.x - BorderHalfWidth) /
                                                  _waveTextureRect.width * _currentClip.length;
                                startTime = Mathf.Clamp(startTime, 0, _currentClip.length);

                                _audioPlayer.clip = _currentClip;
                                _audioPlayer.time = startTime;
                                _audioPlayer.Play();
                                _isRegion = false;

                                _positionIndicatorRect.Set(_waveTextureRect.x, _waveTextureRect.yMin, 1,
                                    _waveTextureRect.height);
                            }
                        }
                        break;
                }
            }

            if (currentEvent.type == EventType.KeyDown)
            {
                if (currentEvent.keyCode == KeyCode.Space)
                {
                    ToggleSelectionPlayStop(IsPlaying());
                }
            }
        }

        private void PlaySelectedRegion()
        {
            if (_audioPlayer != null)
            {
                float startTime = _minValue * _currentClip.length;
                startTime = Mathf.Clamp(startTime, 0, _currentClip.length);

                float endTime = _maxValue * _currentClip.length;
                endTime = Mathf.Clamp(endTime, 0, _currentClip.length);

                var t0 = AudioSettings.dspTime;                
                _audioPlayer.clip = _currentClip;
                _audioPlayer.time = startTime;
                _audioPlayer.PlayScheduled(t0);
                _audioPlayer.SetScheduledEndTime(t0 + (endTime - startTime));                
            }
        }

        private void StopPlayerIfPlaying()
        {
            if (_audioPlayer != null)
            {
                if (_audioPlayer.isPlaying)
                {
                    _audioPlayer.Stop();
                    _audioPlayer.time = 0;
                }
            }
        }

        private void CreateAudioPlayerIfNeeded()
        {
            if (_audioPlayer == null)
            {
                var audioPlayerGameObject = new GameObject("AudioPlayer");
                audioPlayerGameObject.hideFlags = HideFlags.HideAndDontSave;
                _audioPlayer = audioPlayerGameObject.AddComponent<AudioSource>();
                _audioPlayer.playOnAwake = false;
                _audioPlayer.volume = 1;
            }
        }

        private void Update()
        {
            if (_audioPlayer != null && _audioPlayer.isPlaying)
            {
                UpdatePlayerPositionIndicator();
            }

            if (_job != null)
            {
                ShowAudioSavingProgress();
            }
        }

        private void ShowAudioSavingProgress()
        {
            if (_job != null)
            {
                if (_job.Update())
                {
                    EditorUtility.DisplayProgressBar("Processing AudioClip", "Importing new file", 1.0f);

                    AssetDatabase.Refresh();

                    string[] guids = AssetDatabase.FindAssets(_job.Filename);
                    if (guids != null && guids.Length > 0)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                        var clip = AssetDatabase.LoadAssetAtPath(assetPath, typeof (AudioClip));
                        EditorGUIUtility.PingObject(clip);
                    }

                    EditorUtility.ClearProgressBar();

                    _job = null;
                }
                else
                {
                    EditorUtility.DisplayProgressBar("Processing AudioClip", "Saving audio data", SavWav.Progress);
                }
            }
        }

        private void UpdatePlayerPositionIndicator()
        {
            if (_audioPlayer != null && _audioPlayer.isPlaying)
            {
                float minValue = _minValue;
                float maxValue = _maxValue;

                if (!_isRegion)
                {
                    minValue = 0;
                    maxValue = 1;
                }

                float clipTime = _audioPlayer.clip.length;
                float currentTime = _audioPlayer.time;
                float ratio = currentTime / clipTime;
                float regionWidth = (position.width - BorderWidth) * (maxValue - minValue);
                _positionIndicatorRect.x = BorderWidth * 0.5f + (position.width - BorderWidth) * minValue +
                                           regionWidth * ratio;
                _positionIndicatorRect.y = _waveTextureRect.y;
                _positionIndicatorRect.height = _waveTextureRect.height;
                Repaint();
            }
        }

        private void OnDestroy()
        {
            if (_audioPlayer != null)
            {
                _audioPlayer.Stop();
                DestroyImmediate(_audioPlayer.gameObject);
                _audioPlayer = null;
            }
            _waveFormTexture = null;
            _selectionTexture = null;
            _positionIndicatorTexture = null;
            _job = null;                   
        }
    }
}