#define DEBUG
//#define SAMPLEDEBUG

using System;
using System.Collections.Generic;
using ByteConvert;
using System.Reflection;
using System.IO;
using System.Linq;

namespace Waves {

    enum WaveFile {
        Read,
        Write
    }

    class Wave {

        // static variables
        public static Int64 MinSize = 44; // min bytesize
        public static readonly HeaderChunk TargetHeader = new HeaderChunk();
        public static readonly FormatChunk TargetFormat = new FormatChunk();
        public static readonly DataChunk TargetData = new DataChunk();

        private FileStream Stream;
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

        /// <summary>
        /// reads binary data into a class with byte variables or arrays (kinda similar to how structs and fread works in c)
        /// </summary>
        /// <param name="to">the object/class where to append</param>
        /// <param name="offset">offset where to start reading</param>
        /// <returns>integer of how many bytes did we read in the filestream</returns>
        private int ReadStream(object to, int offset = 0, string[] skipFields = null, bool forcelittleendian = false) {
            skipFields = skipFields ?? new string[0];
            int travelDistance = 0;
            // loop through each field in a class
            foreach (FieldInfo field in to.GetType().GetFields()) {

                // skip fields in skipFields array
                if (Array.IndexOf(skipFields, field.Name) == -1 && (
                    field.FieldType == typeof(byte[]) || 
                    field.FieldType == typeof(byte)) ||
                    field.FieldType == typeof(Int64)) {

                    if (field.FieldType == typeof(Int64)) {
                        travelDistance += this.ReadInt64(field, to, offset, forcelittleendian);
                    } else {
                        travelDistance += this.ReadBytes(field, to, offset);
                    }
                }
            }
            return travelDistance;
        }

        /// <summary>
        /// Read bytes from the filestream into a field in an object (field with a known length)
        /// </summary>
        /// <param name="field">what field</param>
        /// <param name="to">where to append the filestream values</param>
        /// <param name="offset">offset (not really used)</param>
        private int ReadBytes(FieldInfo field, object to, int offset) {
            int numberOfBytes; // how many bytes can the field contain

            if (field.FieldType == typeof(byte[])) {
                numberOfBytes = ((Array)field.GetValue(to)).Length;
            } else {
                numberOfBytes = 1;
            }

            byte[] temp = new byte[numberOfBytes]; // create a temporary byte array with the field size
            this.Stream.Read(temp, offset, numberOfBytes); // read bytes into the temporary array

            // check if the orig field is a byte array or a single byte
            if (field.FieldType == typeof(byte[])) {
                field.SetValue(to, temp);
            } else {
                field.SetValue(to, temp[0]);
            }
            return numberOfBytes;
        }


        /// <summary>
        /// Read integers (64) from the filestream into a field in an object
        /// </summary>
        /// <param name="field">what field</param>
        /// <param name="to">where to</param>
        /// <param name="offset">offset, not really used</param>
        /// <returns>number of bytes moved in the filestream (always 4)</returns>
        private int ReadInt64(FieldInfo field, object to, int offset, bool forcelittleendian = false) {
            int numberOfBytes = 4;

            // read bytes
            byte[] temp = new byte[numberOfBytes];
            this.Stream.Read(temp, offset, numberOfBytes);

            // convert bytes into an integer
            Int64 result = ByteConverter.ToInt(temp, forcelittleendian);

            // set value of the field
            field.SetValue(to, result);

            // return travel distance in filestream
            return numberOfBytes;
        }

        /// <summary>
        /// pretty print a class that only has variables made of byte arrays (byte[])
        /// </summary>
        /// <param name="o">object to print</param>
        private void DebugByteObject(object o) {
            Console.WriteLine("\n{0}", o.GetType().Name);
            foreach (FieldInfo field in o.GetType().GetFields()) {
                if ((field.FieldType == typeof(byte[]) || field.FieldType == typeof(byte)) && field.GetValue(o) != null) {
                    Console.Write("\n{0, -15}", field.Name);
                    byte[] value;
                    if (field.FieldType == typeof(byte[])) {
                        value = (byte[])field.GetValue(o);
                    } else {
                        value = new byte[] { (byte)field.GetValue(o) };
                    }
                    Console.Write("{0, -20} ", ByteConverter.ToInt(value, true));
                    Console.Write("{0, -20} ", BitConverter.ToString(value));
                    Console.Write("{0}", ByteConverter.ToASCIIString(value));
                } else if (field.FieldType == typeof(Int64) && field.GetValue(o) != null) {
                    Console.Write("\n{0, -15}", field.Name);
                    Console.Write("{0, -20} ", (Int64)field.GetValue(o));
                }
            }
            Console.WriteLine("\n");
        }

        private bool Read(string filepath) {

            // check file existance
            if (!File.Exists(filepath)) {
                Console.WriteLine("Wave file does not exist.");
                return false;
            }

            // set filestream
            this.Stream = new FileStream(filepath, FileMode.Open);

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
            this.ReadStream(this.Header);
#if DEBUG
            this.DebugByteObject(this.Header);
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
            this.ReadStream(this.Format);
#if DEBUG
            this.DebugByteObject(this.Format);
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
            this.ReadStream(this.Data, forcelittleendian: true);
#if DEBUG
            this.DebugByteObject(this.Data);
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
                this.ReadStream(this.Data.Samples[sampleindex], skipFields: new string[] { "Size" });

                // calculate progress, and write it to the console
                double progress = ((double)i / (double)this.Data.Size) * 100;
                int progressint = (int)Math.Round(progress);
                if (progressint > nextprogress) {
                    Console.Write("#");
                    nextprogress = progressint + 1;
                }
#if SAMPLEDEBUG
                this.DebugByteObject(this.Data.Samples[sampleindex]);
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
            this.DebugByteObject(this.Header);
            this.DebugByteObject(this.Format);
            this.DebugByteObject(this.Data);
#endif

            return true;
        }


        private bool Write() {
            return true;
        }

    }


    class HeaderChunk {
        public byte[] Prefix = new byte[4] { (byte)'R', (byte)'I', (byte)'F', (byte)'F'};
        public Int64 FileSize = 44; // changes, (44 + DataChunk.Size)
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
        public Int64 Size; // changes
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

        public bool ToSize16() {
            // might have to do some bitwise operation tactics here, not quite sure how the channels work when they are played
            // need to research
            switch(Size) {
                case 1:
                    ChannelsTo16(0, 0);
                    break;
                case 3:
                    ChannelsTo16(0, 1);
                    break;
                case 4:
                    ChannelsTo16(0, 1);
                    break;
            }
            Size = 2;
            return true;
        }

        public void ChannelsTo16(int index1, int index2) {
            Left = new byte[2] {
                Left[index1],
                Left[index2]
            };
            Right = new byte[2] {
                Right[index1],
                Right[index2]
            };
        }
    }

}
