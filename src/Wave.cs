#define DEBUG
//#define SAMPLEDEBUG

using System;
using System.Collections.Generic;
using ByteConvert;
using AudioFileStream;
using System.IO;
using System.Linq;

namespace Waves {

    enum WaveFile {
        Read,
        Write
    }

    class Wave {

        // static variables
        public static int MinSize = 44; // min bytesize
        public static readonly HeaderChunk TargetHeader = new HeaderChunk();
        public static readonly FormatChunk TargetFormat = new FormatChunk();
        public static readonly DataChunk TargetData = new DataChunk();

        private AudioStream Stream;
        public HeaderChunk Header = new HeaderChunk();
        public FormatChunk Format = new FormatChunk();
        public DataChunk Data = new DataChunk();
        public bool IsLoaded = false;

        public Wave(WaveFile opentype, string filepath = "") {
            switch(opentype) {
                case WaveFile.Read:
                    this.IsLoaded = this.Read(filepath);
                    break;
                case WaveFile.Write:
                    this.IsLoaded = this.Write();
                    break;
            }
        }

        private bool Read(string filepath) {

            // check file existance
            if (!File.Exists(filepath)) {
                Console.WriteLine("Wave file does not exist.");
                return false;
            }

            // set filestream
            this.Stream = new AudioStream(filepath);

            // check file length
            if (this.Stream.Length <= Wave.MinSize) {
                Console.WriteLine("Wave file too small.");
                return false;
            }

            // read the headerchunk of the file
            if (!this.ReadHeader()) {
                Console.WriteLine("Error reading wave file header.");
                return false;
            }

            // check file formatchunk
            if (!this.ReadFormat()) {
                Console.WriteLine("Error reading wave file format.");
                return false;
            }

            // read all samples
            if (!this.ReadData()) {
                Console.WriteLine("Error reading wave file samples.");
                return false;
            }

            // transform clip
            if (!this.TransformTo16()) {
                Console.WriteLine("Error transforming wave file into a 16bit PCM.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// reads the headerchunk of the wave file
        /// </summary>
        /// <returns>true if header looks like a wave file</returns>
        private bool ReadHeader() {
            // read header from the filestream
            this.Stream.Read(this.Header);
#if DEBUG
            this.Stream.DebugByteObject(this.Header);
#endif

            // check that the file is a riff file
            if (!this.Header.Prefix.SequenceEqual(Wave.TargetHeader.Prefix)) {
                Console.WriteLine("Header does not start with 'RIFF'.");
                return false;
            }

            // check that the file is a wave file
            if (!this.Header.Format.SequenceEqual(Wave.TargetHeader.Format)) {
                Console.WriteLine("RIFF file format is not type of 'WAVE'");
                return false;
            }
            return true;
        }

        /// <summary>
        /// read the headerchunk of the wave file
        /// </summary>
        /// <returns>true if format looks like a pcm wave file</returns>
        private bool ReadFormat() {
            // read format chunk
            this.Stream.Read(this.Format);
#if DEBUG
            this.Stream.DebugByteObject(this.Format);
#endif

            // check format chunk prefix
            if (!this.Format.Prefix.SequenceEqual(Wave.TargetFormat.Prefix)) {
                Console.WriteLine("Format chunk does not start with 'fmt '");
                return false;
            }

            // maybe edit these to try and convert into a pcm wave file
            // check format chunk size
            if (!this.Format.Size.SequenceEqual(Wave.TargetFormat.Size)) {
                Console.WriteLine("Format chunk byte size is not 16. Wave file might not be PCM.");
                return false;
            }

            // check format for PCM
            if (!this.Format.Format.SequenceEqual(Wave.TargetFormat.Format)) {
                Console.WriteLine("Wave file is not PCM.");
                return false;
            }

            // check number of channels
            if (ByteConverter.ToInt(this.Format.NumChannels, true) != 1 && ByteConverter.ToInt(this.Format.NumChannels, true) != 2) {
                Console.WriteLine("Too many channels in wave file (max 2 / stereo).");
                return false;
            }

            // check sample rate
            // todo: try to convert sample rate
            if (!this.Format.SampleRate.SequenceEqual(Wave.TargetFormat.SampleRate)) {
                Console.WriteLine("Sample rate is not 44100.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reads all data from a wave file datachunk into this.Data
        /// </summary>
        /// <returns></returns>
        private bool ReadData() {
            // read datachunk
            this.Stream.Read(this.Data, littleendian: true);
#if DEBUG
            this.Stream.DebugByteObject(this.Data);
#endif
            // check datachunk prefix
            if (!this.Data.Prefix.SequenceEqual(Wave.TargetData.Prefix)) {
                Console.WriteLine("DataChunk does not start with 'data' prefix.");
                return false;
            }

            Int64 bytespersample = ByteConverter.ToInt(this.Format.BlockAlign, true);
            int bitsperchannel = (int)ByteConverter.ToInt(this.Format.BitsPerChannel, true);

            Console.WriteLine("Reading samples...");
            int nextprogress = 1;
            // loop each sample
            for (Int64 i = 0; i < this.Data.Size; i += bytespersample) {

                // new sample, add it to the list of samples
                Sample s = new Sample(bitsperchannel);
                this.Data.Samples.Add(s);

                // current index of the last element in our sample list
                int sampleindex = this.Data.Samples.Count - 1;

                // read data from the stream into the sample
                this.Stream.Read(this.Data.Samples[sampleindex], skipFields: new string[] { "Size" });

                // calculate progress, and write it to the console
                double progress = ((double)i / (double)this.Data.Size) * 100;
                int progressint = (int)Math.Round(progress);
                if (progressint > nextprogress) {
                    Console.Write("#");
                    nextprogress = progressint + 1;
                }
#if SAMPLEDEBUG
                this.Stream.DebugByteObject(this.Data.Samples[sampleindex]);
#endif
            }
            Console.WriteLine();
            return true;
        }

        /// <summary>
        /// Transforms the wave file into a PCM 16bit file
        /// </summary>
        /// <returns>true if successful</returns>
        private bool TransformTo16() {

            Int64 bitsperchannel = ByteConverter.ToInt(this.Format.BitsPerChannel, true);

            // check if format is already correct
            if (bitsperchannel == 16) {
                Console.WriteLine("Wave clip already correct PCM format. Skipping transformation...");
                return true;
            }

            // check if its possible to transform
            if (bitsperchannel != 8 && bitsperchannel != 24 && bitsperchannel != 32) {
                Console.WriteLine("Wave cannot be converted to 16bit audio.");
                return false;
            }

            int datachunksize = 0;

            // loop each sample
            Console.WriteLine("Transforming samples to 16bit...");
            foreach (Sample s in this.Data.Samples) {
                // transform
                s.ToSize16();

                datachunksize += 4;
            }

            // change chunks datas
            this.Data.Size = datachunksize;
            this.Header.FileSize = Wave.MinSize + datachunksize;
            this.Format.ByteRate = Wave.TargetFormat.ByteRate;
            this.Format.BlockAlign = Wave.TargetFormat.BlockAlign;
            this.Format.BitsPerChannel = Wave.TargetFormat.BitsPerChannel;

#if DEBUG
            this.Stream.DebugByteObject(this.Header);
            this.Stream.DebugByteObject(this.Format);
            this.Stream.DebugByteObject(this.Data);
#endif

            return true;
        }

        public bool Save(string filename) {

            // check for file existance and ask if user wants to overwrite the old file
            if (File.Exists(filename)) {
                Console.WriteLine("Do you want to overwrite the existing file? ({0})", filename);
                string input;
                while (true) {
                    Console.Write("y/n: ");
                    input = Console.ReadLine();
                    if (input == "y" || input == "n") {
                        break;
                    }
                }
                if (input == "n") {
                    return false;
                }
            }
            // create new file
            FileStream newfile = new FileStream(filename, FileMode.Create);

            // write header, format, and data chunks into the new file
            AudioStream.Write(newfile, this.Header);
            AudioStream.Write(newfile, this.Format);
            AudioStream.Write(newfile, this.Data);

            // write all the samples into the file
            foreach(Sample s in this.Data.Samples) {
                AudioStream.Write(newfile, s);
            }

            // close the new file
            newfile.Close();
            return true;
        }
    

        private bool Write() {
            return true;
        }

    }


    class HeaderChunk {
        public byte[] Prefix = new byte[4] { (byte)'R', (byte)'I', (byte)'F', (byte)'F'};
        public int FileSize = 44; // changes, (44 + DataChunk.Size)
        public byte[] Format = new byte[4] { (byte)'W', (byte)'A', (byte)'V', (byte)'E'};
    }

    class FormatChunk {
        public byte[] Prefix = new byte[4] { (byte)'f', (byte)'m', (byte)'t', (byte)' '};
        public byte[] Size = new byte[4] { 0x10, 0x00, 0x00, 0x00}; // 16 bytes
        public byte[] Format = new byte[2] { 0x01, 0x00 }; // 1 for PCM
        public byte[] NumChannels = new byte[2] { 0x02, 0x00 }; // 2 channels for stereo
        public byte[] SampleRate = new byte[4] { 0x44, 0xac, 0x00, 0x00 }; // 44100, samples per second
        public byte[] ByteRate = new byte[4] { 0x10, 0xb1, 0x02, 0x00 }; // BlockAlign * SampleRate, bytes per second
        public byte[] BlockAlign = new byte[2] { 0x04, 0x00 }; // NumChannels * BitsPerSample/8, bytes used by a single sample
        public byte[] BitsPerChannel = new byte[2] { 0x10, 0x00 }; // Bits per 1 channel (16, so 2 bytes per channel, and 4 bytes per sample)
    }

    class DataChunk {
        public byte[] Prefix = new byte[4] { (byte)'d', (byte)'a', (byte)'t', (byte)'a'};
        public int Size; // changes
        public List<Sample> Samples = new List<Sample>(); // changes
    }

    class Sample {
        int Size;
        public byte[] Left;
        public byte[] Right;

        public Sample(int bitsperchannel) {
            int channelbytesize = bitsperchannel / 8;
            Size = channelbytesize;
            Left = new byte[channelbytesize];
            Right = new byte[channelbytesize];
        }

        /// <summary>
        /// Transforms sample into a 16bit pcm
        /// </summary>
        /// <returns>true if successful</returns>
        public bool ToSize16() {
            ChannelsTo16(Size * 8);
            Size = 2;
            return true;
        }

        /// <summary>
        /// Transforms both channels into a 16bit pcm
        /// </summary>
        /// <param name="origbitsize">Original bitsize (8, 24 or 32)</param>
        private void ChannelsTo16(int origbitsize) {
            int l = ByteConverter.ToInt(this.Left, true);
            int r = ByteConverter.ToInt(this.Right, true);
            switch (origbitsize) {
                case 8:
                    l *= byte.MaxValue;
                    r *= byte.MaxValue;
                    break;
                case 24:
                    l /= byte.MaxValue;
                    r /= byte.MaxValue;
                    break;
                case 32:
                    l /= UInt16.MaxValue;
                    r /= UInt16.MaxValue;
                    break;
            }
            Int16 l16 = (Int16)l;
            Int16 r16 = (Int16)r;
            Left = BitConverter.GetBytes(l16);
            Right = BitConverter.GetBytes(r16);
        }
    }

}
