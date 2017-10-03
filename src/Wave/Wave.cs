﻿#define DEBUG
//#define SAMPLEDEBUG

using System;
using System.Collections.Generic;
using Midify.Helpers;
using Midify.WaveFile.Chunks;
using System.IO;

namespace Midify.WaveFile {

    public class Wave {

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

        public Wave(FileMode opentype, string filepath = "") {
            switch(opentype) {
                case FileMode.Open:
                    this.IsLoaded = this.Read(filepath);
                    break;
                case FileMode.Create:
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
            if (!this.Header.Read(this.Stream)) {
                Console.WriteLine("Error reading wave file header.");
                return false;
            }

            // check file formatchunk
            if (!this.Format.Read(this.Stream)) {
                Console.WriteLine("Error reading wave file format.");
                return false;
            }

            // read all samples
            if (!this.Data.Read(this.Stream, this.Format)) {
                Console.WriteLine("Error reading wave file samples.");
                return false;
            }

            // transform clip
            if (!this.TransformTo16()) {
                Console.WriteLine("Error transforming wave file into a 16bit PCM.");
                return false;
            }

            // trim clip
            this.Data.Trim();
            this.Header.FileSize = Wave.MinSize + this.Data.Size;

#if DEBUG
            AudioStream.DebugByteObject(this.Header, true);
            AudioStream.DebugByteObject(this.Data, true);
#endif

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
            AudioStream.DebugByteObject(this.Header, true);
            AudioStream.DebugByteObject(this.Format, true);
            AudioStream.DebugByteObject(this.Data, true);
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
}