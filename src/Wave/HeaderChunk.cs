using System;
using System.Linq;
using Midify.Helpers;

namespace Midify.WaveFile.Chunks {
    public class HeaderChunk : LittleEndianObjectStruct {
        public byte[] Prefix = new byte[4] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' };
        public int FileSize = 44; // changes, (44 + DataChunk.Size)
        public byte[] Format = new byte[4] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' };

        /// <summary>
        /// reads the headerchunk of the wave file
        /// </summary>
        /// <returns>true if header looks like a wave file</returns>
        public bool Read(AudioStream from) {
            // read header from the filestream
            from.Read(this);
#if DEBUG
            this.Debug();
#endif

            // check that the file is a riff file
            if (!this.Prefix.SequenceEqual(Wave.TargetHeader.Prefix)) {
                Console.WriteLine("Header does not start with 'RIFF'.");
                return false;
            }

            // check that the file is a wave file
            if (!this.Format.SequenceEqual(Wave.TargetHeader.Format)) {
                Console.WriteLine("RIFF file format is not type of 'WAVE'");
                return false;
            }
            return true;
        }

    }
}
