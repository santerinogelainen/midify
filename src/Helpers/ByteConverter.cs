using System;

namespace Midify.Helpers {
    static class ByteConverter {

        /// <summary>
        /// convert a byte array of unknown size into a int (note! max bytesize of 4)
        /// </summary>
        /// <param name="arr">byte array, max length 4</param>
        /// <returns>int value of the byte(s)</returns>
        public static int ToInt(byte[] arr, bool staylittleendian = false) {

            if (arr.Length == 3) {
                if (BitConverter.IsLittleEndian && !staylittleendian) {
                    arr = new byte[4] {
                        0x00,
                        arr[0],
                        arr[1],
                        arr[2]
                    };
                } else {
                    arr = new byte[4] {
                        arr[0],
                        arr[1],
                        arr[2],
                        0x00
                    };
                }
            }

            if (BitConverter.IsLittleEndian && !staylittleendian) {
                Array.Reverse(arr);
            }
            int result = -1;
            switch (arr.Length) {
                case sizeof(sbyte):
                    result = arr[0];
                    break;
                case sizeof(Int16):
                    result = BitConverter.ToInt16(arr, 0);
                    break;
                case sizeof(Int32):
                    result = BitConverter.ToInt32(arr, 0);
                    break;
            }
            if (BitConverter.IsLittleEndian && !staylittleendian) {
                Array.Reverse(arr);
            }
            return result;
        }

        /// <summary>
        /// convert a byte array of unknown size into a string
        /// </summary>
        /// <param name="arr">byte array</param>
        /// <returns>string</returns>
        public static string ToASCIIString(byte[] arr) {
            string final = "";
            foreach (byte b in arr) {
                final += (char)b;
            }
            return final;
        }

    }
}
