using System;
using Midify.Helpers;
using System.Linq;

namespace Midify.WaveFile.Chunks {
    public class FormatChunk {
        public byte[] Prefix = new byte[4] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' };
        public byte[] Size = new byte[4] { 0x10, 0x00, 0x00, 0x00 }; // 16 bytes
        public byte[] Format = new byte[2] { 0x01, 0x00 }; // 1 for PCM
        public byte[] NumChannels = new byte[2] { 0x02, 0x00 }; // 2 channels for stereo
        public byte[] SampleRate = new byte[4] { 0x44, 0xac, 0x00, 0x00 }; // 44100, samples per second
        public byte[] ByteRate = new byte[4] { 0x10, 0xb1, 0x02, 0x00 }; // BlockAlign * SampleRate, bytes per second
        public byte[] BlockAlign = new byte[2] { 0x04, 0x00 }; // NumChannels * BitsPerSample/8, bytes used by a single sample
        public byte[] BitsPerChannel = new byte[2] { 0x10, 0x00 }; // Bits per 1 channel (16, so 2 bytes per channel, and 4 bytes per sample)

        /// <summary>
        /// read the headerchunk of the wave file
        /// </summary>
        /// <returns>true if format looks like a pcm wave file</returns>
        public bool Read(AudioStream from) {
            // read format chunk
            from.Read(this);
#if DEBUG
            AudioStream.DebugByteObject(this, true);
#endif

            // check format chunk prefix
            if (!this.Prefix.SequenceEqual(Wave.TargetFormat.Prefix)) {
                Console.WriteLine("Format chunk does not start with 'fmt '");
                return false;
            }

            // maybe edit these to try and convert into a pcm wave file
            // check format chunk size
            if (!this.Size.SequenceEqual(Wave.TargetFormat.Size)) {
                Console.WriteLine("Format chunk byte size is not 16. Wave file might not be PCM.");
                return false;
            }

            // check format for PCM
            if (!this.Format.SequenceEqual(Wave.TargetFormat.Format)) {
                Console.WriteLine("Wave file is not PCM.");
                return false;
            }

            // check number of channels
            if (ByteConverter.ToInt(this.NumChannels, true) != 1 && ByteConverter.ToInt(this.NumChannels, true) != 2) {
                Console.WriteLine("Too many channels in wave file (max 2 / stereo).");
                return false;
            }

            // check sample rate
            // todo: try to convert sample rate
            if (!this.SampleRate.SequenceEqual(Wave.TargetFormat.SampleRate)) {
                Console.WriteLine("Sample rate is not 44100.");
                return false;
            }

            return true;
        }

    }
}
