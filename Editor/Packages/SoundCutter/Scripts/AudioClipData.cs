using System.Collections.Generic;
using UnityEngine;

namespace Assets.SoundCutter.Scripts
{
    public class AudioClipData
    {
        public int Frequency { get; set; }
        public int Channels { get; set; }
        public int Samples { get; set; }
        public float[] Data { get; set; }

        public float Length
        {
            get
            {
                RecalculateSamples();

                return (float)Samples / Frequency;
            }
        }

        public void InitData()
        {
            Data = new float[Samples * Channels];
        }

        public void RecalculateSamples()
        {
            Samples = Data.Length / Channels;
        }

        public void RemoveDataRange(float fromRatio, float toRatio)
        {
            var count = Data.Length / Channels;
            var listData = new List<float>(Data);
            var fromIndex = (int)(fromRatio * count) * Channels;
            var removeCount = (int)((toRatio - fromRatio) * count) * Channels;

            listData.RemoveRange(fromIndex, removeCount);

            Data = listData.ToArray();
            RecalculateSamples();                        
        }

        public void RemoveDataExceptRange(float fromRatio, float toRatio)
        {
            var count = Data.Length / Channels;
            var listData = new List<float>(Data);

            var fromIndex = (int)(toRatio * count) * Channels;
            var removeCount = (int)((1.0 - toRatio) * count) * Channels;
            listData.RemoveRange(fromIndex, removeCount);

            fromIndex = 0;
            removeCount = (int) (fromRatio * count) * Channels;
            listData.RemoveRange(fromIndex, removeCount);

            Data = listData.ToArray();
            RecalculateSamples();  
        }

        public AudioClip CreateAudioClip()
        {
            var audioClip = AudioClip.Create("AudioClip", Samples, Channels, Frequency, false);
            audioClip.SetData(Data, 0);
            return audioClip;
        }

        public void GetData(float[] data, int offsetSamples)
        {            
            var index = offsetSamples * Channels;
            var count = Data.Length - index;
            var dataCount = data.Length;

            for (var i = 0; i < count && i < dataCount; i++)
            {
                data[i] = Data[index + i];
            }            
        }

        public static AudioClipData FromAudioClip(AudioClip clip)
        {
            var clipData = new AudioClipData
            {
                Channels = clip.channels,
                Frequency = clip.frequency,
                Samples = clip.samples
            };
            clipData.InitData();
            clip.GetData(clipData.Data, 0);
            return clipData;
        }

        public void Dispose()
        {
            Data = null;
        }
    }
}
