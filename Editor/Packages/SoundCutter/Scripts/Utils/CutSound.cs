using UnityEngine;

namespace Assets.SoundCutter.Scripts.Utils
{
    public class CutSound
    {

        public static float[] CutSamples(AudioClip clip, float start = 0.0f, float end = 1.0f)
        {
            var clipData = AudioClipData.FromAudioClip(clip);
            return CutSamples(clipData, start, end);
        }
        public static float[] CutSamples(AudioClipData clipData, float start = 0.0f, float end = 1.0f)
        {
            var samplesCount = clipData.Samples;

            start = Mathf.Clamp(start, 0.0f, 1.0f);
            end = Mathf.Clamp(end, 0.0f, 1.0f);

            var startSample = (int)(start * samplesCount);
            var endSample = (int)(end * samplesCount);

            samplesCount = endSample - startSample;

            if (samplesCount == 0)
            {
                return null;
            }

            var samples = new float[samplesCount * clipData.Channels];
//            clip.GetData(samples, startSample);
            clipData.GetData(samples, startSample);

            return samples;
        }

        public static AudioClip Cut(AudioClip clip, float start = 0.0f, float end = 1.0f)
        {            
            var clipData = AudioClipData.FromAudioClip(clip);
            return Cut(clipData, start, end);
        }

        public static AudioClip Cut(AudioClipData clipData, float start = 0.0f, float end = 1.0f)
        {
            var samples = CutSamples(clipData, start, end);
            if (samples == null)
            {
                return null;
            }
            var samplesCount = samples.Length / clipData.Channels;

            var cuttedAudioClip = AudioClip.Create("CuttedAudio", samplesCount, clipData.Channels, clipData.Frequency, false);
            cuttedAudioClip.SetData(samples, 0);
            return cuttedAudioClip;
        }        
    }
}