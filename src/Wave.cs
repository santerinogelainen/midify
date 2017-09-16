using System;
using System.Collections.Generic;
using ByteConvert;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;

namespace Waves {

    enum WaveFile {
        Read,
        Write
    }

    class Wave {

        // static variables
        public static Int64 MinSize = 44; // min bytesize

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
        private int ReadStream(object to, int offset = 0, string[] skipFields = null) {
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
                        travelDistance += this.ReadInt64(field, to, offset);
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
        private int ReadInt64(FieldInfo field, object to, int offset) {
            int numberOfBytes = 4;

            // read bytes
            byte[] temp = new byte[numberOfBytes];
            this.Stream.Read(temp, offset, numberOfBytes);

            // convert bytes into an integer
            Int64 result = ByteConverter.ToInt(temp);

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
                    Console.Write("{0, -20} ", ByteConverter.ToInt(value));
                    Console.Write("{0, -20} ", BitConverter.ToString(value));
                    Console.Write("{0}", ByteConverter.ToASCIIString(value));
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

            return true;
        }

        /// <summary>
        /// reads the headerchunk of the wave file
        /// </summary>
        /// <returns>true if header looks like a wave file</returns>
        private bool ReadHeader() {
            // read header from the filestream
            this.ReadStream(this.Header);
            HeaderChunk compare = new HeaderChunk();

            // check that the file is a riff file
            if (this.Header.Prefix != compare.Prefix) {
                Console.WriteLine("Header does not start with 'RIFF'.");
                return false;
            }

            // check that the file is a wave file
            if (this.Header.Format != compare.Format) {
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
            FormatChunk compare = new FormatChunk();

            // check format chunk prefix
            if (this.Format.Prefix != compare.Prefix) {
                Console.WriteLine("Format chunk does not start with 'fmt '");
                return false;
            }

            // maybe edit these to try and convert into a pcm wave file
            // check format chunk size
            if (this.Format.Size != compare.Size) {
                Console.WriteLine("Format chunk byte size is not 16. Wave file might not be PCM.");
                return false;
            }

            // check format for PCM
            if (this.Format.Format != compare.Format) {
                Console.WriteLine("Wave file is not PCM.");
                return false;
            }

            // check number of channels
            if (this.Format.NumChannels != compare.NumChannels || this.Format.NumChannels != new byte[2] { 0x01, 0x00 }) {
                Console.WriteLine("Too many channels in wave file (max 2 / stereo).");
                return false;
            }

            // check sample rate
            // todo: try to convert sample rate
            if (this.Format.SampleRate != compare.SampleRate) {
                Console.WriteLine("Sample rate is not 44100.");
                return false;
            }

            // check byterate
            // to do: try to convert
            if (this.Format.ByteRate != compare.ByteRate) {
                Console.WriteLine("Byte rate is not 176400.");
                return false;
            }

            // check blockalign
            // to do: try to convert
            if (this.Format.BlockAlign != compare.BlockAlign) {
                Console.WriteLine("Sample byte size is not 4.");
                return false;
            }

            // check bits per channel
            // todo: try to convert
            if (this.Format.BitsPerChannel != compare.BitsPerChannel) {
                Console.WriteLine("Channel bit size is not 16.");
                return false;
            }
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
