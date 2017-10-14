using System;
using Midify.Helpers;
using System.Collections.Generic;
using System.Numerics;

namespace Midify.WaveFile {
    public class Sample {
        int Size;
        public byte[] Left;
        public byte[] Right;

        public int LeftInt {
            get {
                return ByteConverter.ToInt(Left, true);
            }
        }

        public int RightInt {
            get {
                return ByteConverter.ToInt(Right, true);
            }
        }

        public float LeftFloat {
            get {
                return LeftInt;
            }
        }

        public float RightFloat {
            get {
                return RightInt;
            }
        }

        public Sample(int bitsperchannel = 16) {
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
            int l = LeftInt;
            int r = RightInt;
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

        /// <summary>
        /// Appends or combines samples in a list
        /// </summary>
        /// <param name="to">where to copy</param>
        /// <param name="from">where to copy from</param>
        /// <param name="offset">where to start looking in to array</param>
        public static void AppendOrCombine(List<Sample> to, List<Sample> from, int offset) {
            for (int i = 0; i < from.Count; i++) {
                int curpos = i + offset;
                if (to.Count > curpos) { // already data there
                    Sample a = to[curpos-1];
                    Sample b = from[i];
                    to[curpos - 1] = Sample.Combine(a, b);
                } else { // no data
                    to.Add(from[i]);
                }
            }
        }

        /// <summary>
        /// Combines to samples together
        /// </summary>
        /// <param name="a">sample a</param>
        /// <param name="b">sample b</param>
        /// <returns>new sample with combined values</returns>
        public static Sample Combine(Sample a, Sample b) {
            Sample result = new Sample();
            int al = a.LeftInt;
            int ar = a.RightInt;
            int bl = b.LeftInt;
            int br = b.RightInt;
            Int16 left = (Int16)((al / 2) + (bl / 2));
            Int16 right = (Int16)((ar / 2) + (br / 2));
            result.Left = BitConverter.GetBytes(left);
            result.Right = BitConverter.GetBytes(right);
            return result;
        }

    }
}
