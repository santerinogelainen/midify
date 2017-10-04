#define DEBUG
using Midify.Helpers;
using System;

namespace Midify.MidiFile.Chunks {
    /// <summary>
    /// header chunk "structure"
    /// </summary>
    public class HeaderChunk : BigEndianObjectStruct {
        public byte[] Prefix = new byte[4];
        public byte[] Size = new byte[4];
        public byte[] Format = new byte[2];
        public byte[] Tracks = new byte[2];
        public byte[] Division = new byte[2];

        /// <summary>
        /// read the header of the file to this.header
        /// </summary>
        public bool Read(AudioStream from) {
            // read the first 14 bytes in the filestream to header
            from.Read(this);
#if (DEBUG)
            // show debug info
            this.Debug();
#endif
            // probably a midi file
            if (ByteConverter.ToASCIIString(this.Prefix) == "MThd" &&
                ByteConverter.ToInt(this.Size) == 6) {

                // too many songs in one file
                if (ByteConverter.ToInt(this.Format) == 2) {
                    Console.WriteLine("Midi files with multiple songs are not supported.");
                    return false;
                }
                return true;
            }
            Console.WriteLine("The file given is not a midi file.");
            return false;
        }

    }
}
