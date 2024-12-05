using UnityEngine;

namespace Assets.SoundCutter.Scripts.Utils
{
    public class SoundWaveUtils
    {
        public static Texture2D DrawOnePixelTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGB24, false);

            texture.SetPixel(0, 0, color);
            texture.Apply();

            return texture;
        }

        public static Texture2D DrawWaveForm(AudioClip clip, int width, int height, Color waveColor, Color waveBackColor,
            int samplesStep = 20, int channelIndex = 0)
        {
            AudioClipData clipData = AudioClipData.FromAudioClip(clip);
            return DrawWaveForm(clipData, width, height, waveColor, waveBackColor, samplesStep, channelIndex);
        }

        public static Texture2D DrawWaveForm(AudioClipData clipData, int width, int height, Color waveColor,
            Color waveBackColor, int samplesStep = 20, int channelIndex = 0)
        {
            var waveFormTexture = new Texture2D(width, height, TextureFormat.RGB24, false);

            var backgroundColors = new Color[width * height];
            int colorsCount = backgroundColors.Length;
            for (int c = 0; c < colorsCount; c++)
            {
                backgroundColors[c] = waveBackColor;
            }
            waveFormTexture.SetPixels(backgroundColors);

            float[] samples = clipData.Data;

            float size = samples.Length;
            float sizePerChannel = size / clipData.Channels;

            float step = sizePerChannel / width;
            float halfHeight = height * 0.5f;

            for (int xPixel = 0; xPixel < width; xPixel++)
            {
                var start = (int) (xPixel * step);
                var end = (int) ((xPixel + 1) * step);

                float min = float.MaxValue;
                float max = float.MinValue;
                for (int i = start; i < end; i += samplesStep)
                {
                    int index = i * clipData.Channels + channelIndex;
                    float val = samples[index];
                    min = val < min ? val : min;
                    max = val > max ? val : max;
                }

                var yMax = (int) ((max + 1) * halfHeight);
                var yMin = (int) ((min + 1) * halfHeight);

                DrawLine(waveFormTexture, xPixel, yMax, xPixel, yMin, waveColor);
            }

            waveFormTexture.Apply();

            return waveFormTexture;
        }

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
        {
            int dy = y1 - y0;
            int dx = x1 - x0;
            int stepx, stepy;

            if (dy < 0)
            {
                dy = -dy;
                stepy = -1;
            }
            else
            {
                stepy = 1;
            }
            if (dx < 0)
            {
                dx = -dx;
                stepx = -1;
            }
            else
            {
                stepx = 1;
            }
            dy <<= 1;
            dx <<= 1;

            float fraction = 0;

            tex.SetPixel(x0, y0, col);
            if (dx > dy)
            {
                fraction = dy - (dx >> 1);
                while (Mathf.Abs(x0 - x1) > 1)
                {
                    if (fraction >= 0)
                    {
                        y0 += stepy;
                        fraction -= dx;
                    }
                    x0 += stepx;
                    fraction += dy;
                    tex.SetPixel(x0, y0, col);
                }
            }
            else
            {
                fraction = dx - (dy >> 1);
                while (Mathf.Abs(y0 - y1) > 1)
                {
                    if (fraction >= 0)
                    {
                        x0 += stepx;
                        fraction -= dy;
                    }
                    y0 += stepy;
                    fraction += dx;
                    tex.SetPixel(x0, y0, col);
                }
            }
        }
    }
}