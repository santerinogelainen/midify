using System;
using System.Collections.Generic;
using System.Linq;
using Midify.Helpers;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace Midify.WaveFile.Chunks {

    public class DataChunk : LittleEndianObjectStruct {

        public byte[] Prefix = new byte[4] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' };
        public int Size = 0;
        public List<Sample> Samples = new List<Sample>(); // changes

        /// <summary>
        /// Reads all data from a wave file datachunk into this.Data
        /// </summary>
        /// <returns></returns>
        public bool Read(AudioStream from, FormatChunk format) {
            // read datachunk
            from.Read(this);
#if DEBUG
            this.Debug();
#endif
            // check datachunk prefix
            if (!this.Prefix.SequenceEqual(Wave.TargetData.Prefix)) {
                Console.WriteLine("DataChunk does not start with 'data' prefix.");
                return false;
            }

            int bytespersample = ByteConverter.ToInt(format.BlockAlign, from.IsLittleEndian);
            int bitsperchannel = (int)ByteConverter.ToInt(format.BitsPerChannel, from.IsLittleEndian);

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
                this.Samples[sampleindex].Debug();
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


        
        public void ChangePitch(int amount) {
            int fraction = 1;
            int size = 44100 / fraction;
            int to = (Samples.Count / size) + 1;
            int shift = amount / fraction;
            for (int i = 0; i < to; i++) {
                float[] leftData = new float[size % 2 == 0 ? size + 2 : size + 1];
                float[] rightData = new float[size % 2 == 0 ? size + 2 : size + 1];
                for (int j = 0; j < size; j++) {
                    int index = j + (i * size);
                    if (Samples.ElementAtOrDefault(index) != null) {
                        leftData[j] = Samples[index].LeftFloat;
                        rightData[j] = Samples[index].RightFloat;
                    } else {
                        leftData[j] = 0;
                        rightData[j] = 0;
                    }
                }
                Fourier.ForwardReal(leftData, size);
                Fourier.ForwardReal(rightData, size);
                leftData = RollAndPad(leftData, shift);
                rightData = RollAndPad(rightData, shift);
                Fourier.InverseReal(leftData, size);
                Fourier.InverseReal(rightData, size);
                for (int j = 0; j < size; j++) {
                    int index = j + (i * size);
                    if (Samples.ElementAtOrDefault(index) != null) {
                        float leftSampleData = leftData[j];
                        if (leftSampleData >= Int16.MaxValue) {
                            leftSampleData = Int16.MaxValue;
                        }
                        if (leftSampleData <= Int16.MinValue) {
                            leftSampleData = Int16.MinValue;
                        }
                        float rightSampleData = rightData[j];
                        if (rightSampleData >= Int16.MaxValue) {
                            rightSampleData = Int16.MaxValue;
                        }
                        if (rightSampleData <= Int16.MinValue) {
                            rightSampleData = Int16.MinValue;
                        }
                        Samples[index].Left = BitConverter.GetBytes((Int16)leftSampleData);
                        Samples[index].Right = BitConverter.GetBytes((Int16)rightSampleData);
                    }
                }
            }
        }

        private float[] RollAndPad(float[] values, int shift) {
            float[] final = new float[values.Length];
            Array.Copy(values, 0, final, shift, values.Length - shift);
            for (int i = 0; i < shift; i++) {
                final[i] = 0;
            }
            return final;
        }

    }
}
