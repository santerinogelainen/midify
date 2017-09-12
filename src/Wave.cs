using System;
using System.Collections.Generic;

namespace Waves {

    enum WaveFile {
        Read,
        Write
    }

    class Wave {

        public HeaderChunk Header = new HeaderChunk();
        public FormatChunk Format = new FormatChunk();
        public DataChunk Data = new DataChunk();
        public bool IsLoaded = false;

        public Wave(WaveFile opentype, string filepath = "") {
            switch(opentype) {
                case WaveFile.Read:
                    this.IsLoaded = this.Read();
                    break;
                case WaveFile.Write:
                    this.IsLoaded = this.Write();
                    break;
            }
        }

        private bool Read() {
            return true;
        }

        private bool Write() {
            return true;
        }

    }


    class HeaderChunk {
        public byte[] Prefix = new byte[4] { (byte)'R', (byte)'I', (byte)'F', (byte)'F'};
        public Int64 FileSize = 36; // changes, (36 + DataChunk.Size)
        public byte[] Format = new byte[4] { (byte)'W', (byte)'A', (byte)'V', (byte)'E'};
    }

    class FormatChunk {
        public byte[] Prefix = new byte[4] { (byte)'f', (byte)'m', (byte)'t', (byte)'\0'};
        public byte[] Size = new byte[4] { 0x10, 0x00, 0x00, 0x00}; // 16 bytes
        public byte[] Format = new byte[2] { 0x01, 0x00 }; // 1 for PCM
        public byte[] NumChannels = new byte[2] { 0x02, 0x00 }; // 2 channels for stereo
        public byte[] SampleRate = new byte[4] { 0x44, 0xac, 0x00, 0x00 }; // 44100, samples per second
        public byte[] ByteRate = new byte[4] { 0x10, 0xb1, 0x02, 0x00 }; // BlockAlign * SampleRate, bytes per second
        public byte[] BlockAlign = new byte[2] { 0x04, 0x00 }; // NumChannels * BitsPerSample/8, bytes used by a single channel
        public byte[] BitsPerChannel = new byte[2] { 0x10, 0x00 }; // Bits per 1 channel (16, so 2 bytes per channel, and 4 bytes per sample)
    }

    class DataChunk {
        public byte[] Prefix = new byte[4] { (byte)'d', (byte)'a', (byte)'t', (byte)'a'};
        public Int64 Size; // changes
        public List<Sample> Samples = new List<Sample>(); // changes
    }

    class Sample {
        public byte[] Left = new byte[2];
        public byte[] Right = new byte[2];
    }

}
