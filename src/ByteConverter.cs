﻿using System;

namespace ByteConvert {
    static class ByteConverter {

        /// <summary>
        /// convert a byte array of unknown size into a Int64
        /// </summary>
        /// <param name="arr">byte array</param>
        /// <returns>Int64 value of the byte(s)</returns>
        public static Int64 ToInt(byte[] arr, bool staylittleendian = false) {

            if (arr.Length == 3) {
                arr = new byte[4] {
                    arr[0],
                    arr[1],
                    arr[2],
                    0x00
                };
            }

            if (BitConverter.IsLittleEndian && !staylittleendian) {
                Array.Reverse(arr);
            }
            Int64 result = -1;
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
                case sizeof(Int64):
                    result = BitConverter.ToInt64(arr, 0);
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
