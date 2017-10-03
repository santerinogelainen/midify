using System;
using Midify.Helpers;

namespace Midify.WaveFile {
    public class Sample {
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
