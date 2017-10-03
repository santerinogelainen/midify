using System;
using System.Collections.Generic;
using System.Linq;
using Midify.Helpers;

namespace Midify.WaveFile.Chunks {

    public class DataChunk {
        public byte[] Prefix = new byte[4] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' };
        public int Size; // changes
        public List<Sample> Samples = new List<Sample>(); // changes

        /// <summary>
        /// Reads all data from a wave file datachunk into this.Data
        /// </summary>
        /// <returns></returns>
        public bool Read(AudioStream from, FormatChunk format) {
            // read datachunk
            from.Read(this, littleendian: true);
#if DEBUG
            AudioStream.DebugByteObject(this, true);
#endif
            // check datachunk prefix
            if (!this.Prefix.SequenceEqual(Wave.TargetData.Prefix)) {
                Console.WriteLine("DataChunk does not start with 'data' prefix.");
                return false;
            }

            int bytespersample = ByteConverter.ToInt(format.BlockAlign, true);
            int bitsperchannel = (int)ByteConverter.ToInt(format.BitsPerChannel, true);

            Console.WriteLine("Reading samples...");
            int nextprogress = 1;
            // loop each sample
            for (int i = 0; i < this.Size; i += bytespersample) {

                // new sample, add it to the list of samples
                Sample s = new Sample(bitsperchannel);
                this.Samples.Add(s);

                // current index of the last element in our sample list
                int sampleindex = this.Samples.Count - 1;

                // read data from the stream into the sample
                from.Read(this.Samples[sampleindex], skipFields: new string[] { "Size" });

                // calculate progress, and write it to the console
                double progress = ((double)i / (double)this.Size) * 100;
                int progressint = (int)Math.Round(progress);
                if (progressint > nextprogress) {
                    Console.Write("#");
                    nextprogress = progressint + 1;
                }
#if SAMPLEDEBUG
                AudioStream.DebugByteObject(this.Samples[sampleindex], true);
#endif
            }
            Console.WriteLine();
            return true;
        }



        /// <summary>
        /// Removes unnecessary 0 bits from the start and the end is a wave file
        /// NOTE! use only after transforming to 16bit PCM!!!!
        /// </summary>
        public void Trim() {

            if (this.Samples.Count == 0) {
                return;
            }

            // start of list
            while (true) {
                if (this.Samples[0].Left.SequenceEqual(new byte[2] { 0x00, 0x00 }) && this.Samples[0].Right.SequenceEqual(new byte[2] { 0x00, 0x00 })) {
                    this.Samples.RemoveAt(0);
                    Size -= 4;
                } else {
                    break;
                }
            }

            // end of list
            while (true) {
                Sample last = this.Samples[this.Samples.Count - 1];
                if (last.Left.SequenceEqual(new byte[2] { 0x00, 0x00 }) && last.Right.SequenceEqual(new byte[2] { 0x00, 0x00 })) {
                    this.Samples.RemoveAt(this.Samples.Count - 1);
                    Size -= 4;
                } else {
                    break;
                }
            }
        }
    }
}
