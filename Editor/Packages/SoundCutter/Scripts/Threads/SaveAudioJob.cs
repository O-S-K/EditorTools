using UnityEngine;

namespace Assets.SoundCutter.Scripts.Threads
{
    public class SaveAudioJob : ThreadedJob
    {
        private AudioClip _clip;
        private AudioClipData _clipData;
        private string _filepath;
        private readonly string _filename;
        private readonly string _destinationPath;

        public string Filename
        {
            get { return _filename; }
        }        

        public SaveAudioJob(string filename, string destinationPath, AudioClip clip)
        {
            
            _filename = filename;
            _destinationPath = destinationPath;
            _clip = clip;

            _clipData = AudioClipData.FromAudioClip(_clip);
        }

        public SaveAudioJob(string filename, string destinationPath, AudioClipData clipData)
        {
            _filename = filename;
            _destinationPath = destinationPath;
            _clipData = clipData;
        }

        public override void Start()
        {
            _filepath = SavWav.CreatePathInTemp(_filename);
            
            base.Start();
        }

        protected override void ThreadFunction()
        {
            SavWav.Save(_filepath, _clipData);
        }

        protected override void OnFinished()
        {
            if (_clipData != null)
            {
                SavWav.MoveFileFromTemp(_filename, _destinationPath);

                _clipData.Dispose();    
            }            
            _clipData = null;
            _clip = null;
        }
    }
}
