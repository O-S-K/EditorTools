#if UNITY_EDITOR
#endif
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Assets.SoundCutter.Scripts
{
    public static class SavWav
    {

        private const int HeaderSize = 44;

        private static float _progress = 0f;
        private static readonly object _handle = new object();

        public static float Progress
        {
            get
            {
                float tmp = 0f;
                lock (_handle)
                {
                    tmp = _progress;
                }
                return tmp;
            }
            set
            {
                lock (_handle)
                {
                    _progress = value;
                }
            }
        }

        public static string CreatePathInTemp(string filename)
        {            
            return CreatePath(filename, Application.persistentDataPath);           
        }

        public static string CreatePathInProject(string filename)
        {            
            return CreatePath(filename, Application.dataPath);            
        }

        public static string CreatePath(string filename, string path)
        {
            if (!filename.ToLower().EndsWith(".wav"))
            {
                filename += ".wav";
            }            
            var resultPath = Path.Combine(path, filename);

            // Make sure directory exists if user is saving to sub dir.
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
            return resultPath;
        }

        public static void MoveFileFromTemp(string filename, string destinationPath)
        {
            var fromPath = CreatePathInTemp(filename);
            //var toPath = CreatePathInProject(filename);
            var toPath = CreatePath(filename, destinationPath);
            
#if UNITY_EDITOR
            FileUtil.DeleteFileOrDirectory(toPath);

            FileUtil.MoveFileOrDirectory(fromPath, toPath);
#endif
        }

        public static bool Save(string filepath, AudioClipData clipData)
        {           
            using (var fileStream = CreateEmpty(filepath))
            {
                ConvertAndWrite(fileStream, clipData);
                WriteHeader(fileStream, clipData);
            }

            return true;
        }

        public static bool Save(string filename, AudioClip clip)
        {
            var clipData = AudioClipData.FromAudioClip(clip);
            return Save(filename, clipData);
        }        

        static FileStream CreateEmpty(string filepath)
        {
            var fileStream = new FileStream(filepath, FileMode.Create);            
            byte emptyByte = new byte();

            for (int i = 0; i < HeaderSize; i++) //preparing the header
            {
                fileStream.WriteByte(emptyByte);
            }

            return fileStream;
        }

        static void ConvertAndWrite(FileStream fileStream, AudioClipData clipData)
        {            
            var samples = clipData.Data;            

            var intData = new Int16[samples.Length];
            //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

            var bytesData = new Byte[samples.Length * 2];
            //bytesData array is twice the size of
            //dataSource array because a float converted in Int16 is 2 bytes.

            const float rescaleFactor = 32767; //to convert float to Int16

            var samplesCount = samples.Length;
            
            for (int i = 0; i < samplesCount; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);

                Progress = (float)i / samplesCount;                
            }            
            
            fileStream.Write(bytesData, 0, bytesData.Length);            
        }        

        static void WriteHeader(FileStream fileStream, AudioClipData clipData)
        {

            var hz = clipData.Frequency;
            var channels = clipData.Channels;
            var samples = clipData.Samples;

            fileStream.Seek(0, SeekOrigin.Begin);

            Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
            fileStream.Write(chunkSize, 0, 4);

            Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);

            Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);

            Byte[] subChunk1 = BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);
            
            UInt16 one = 1;

            Byte[] audioFormat = BitConverter.GetBytes(one);
            fileStream.Write(audioFormat, 0, 2);

            Byte[] numChannels = BitConverter.GetBytes(channels);
            fileStream.Write(numChannels, 0, 2);

            Byte[] sampleRate = BitConverter.GetBytes(hz);
            fileStream.Write(sampleRate, 0, 4);

            Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
            fileStream.Write(byteRate, 0, 4);

            UInt16 blockAlign = (ushort)(channels * 2);
            fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

            UInt16 bps = 16;
            Byte[] bitsPerSample = BitConverter.GetBytes(bps);
            fileStream.Write(bitsPerSample, 0, 2);

            Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(datastring, 0, 4);

            Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
            fileStream.Write(subChunk2, 0, 4);

            //		fileStream.Close();
        }
        
    }
}