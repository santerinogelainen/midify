using ByteConvert;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AudioFileStream {

    class AudioStream {

        public FileStream Stream;
        public Int64 Length;

        public AudioStream(string filepath) {

            if (!File.Exists(filepath)) {
                Console.WriteLine("Error. File not found ({0}).", filepath);
                throw new ArgumentException("File has to exist.", "filepath");
            }

            this.Stream = new FileStream(filepath, FileMode.Open);
            this.Length = this.Stream.Length;
        }


        /// <summary>
        /// reads binary data into a class with byte variables or arrays (kinda similar to how structs and fread works in c)
        /// </summary>
        /// <param name="to">the object/class where to read the data</param>
        /// <param name="vlv">name of a variable with a variable length value</param>
        /// <param name="skipFields">array of variable names to skip</param>
        /// <param name="littleendian">keep little endianness of the number</param>
        /// <returns>integer of how many bytes did we read in the filestream</returns>
        public int Read(object to, string vlv = "", string[] skipFields = null, bool littleendian = false) {
            skipFields = skipFields ?? new string[0];
            int travelDistance = 0;
            // loop through each field in a class
            foreach (FieldInfo field in to.GetType().GetFields()) {

                // skip fields in skipFields array
                if (Array.IndexOf(skipFields, field.Name) == -1 && !field.IsStatic) {

                    // field is vlv
                    if (vlv != "" && field.Name == vlv) {
                        travelDistance += this.ReadVLV(field, to);
                    }

                    // ELSE if the field is a type of byte[] or byte
                    else if (field.FieldType == typeof(byte[]) || field.FieldType == typeof(byte)) {
                        travelDistance += this.ReadBytes(field, to);
                    }

                    // ELSE if the field is an int
                    else if (field.FieldType == typeof(int)) {
                        travelDistance += this.ReadInt(field, to, littleendian);
                    }
                }
            }
            return travelDistance;
        }


        // TO DO!!!!!!!!!!!! VLV calculations are kinda wrong!!! see
        // http://www.ccarh.org/courses/253/handout/vlv/


        /// <summary>
        /// Read bytes from the filestream into a field in an object (unknown field length, aka variable length value VLV)
        /// </summary>
        /// <param name="field">what field</param>
        /// <param name="to">where to append the filestream values</param>
        /// <returns>how many bytes read from filestream</returns>
        private int ReadVLV(FieldInfo field, object to) {
            // each byte as a bitarray
            List<BitArray> vlvBits = new List<BitArray>();
            // read all vlv values
            while (true) {
                byte[] b = new byte[1];
                this.Stream.Read(b, 0, 1);
                vlvBits.Add(new BitArray(b));
                if (b[0] < 0x80) {
                    break;
                }
            }

            vlvBits.Reverse();

            // transform vlvs to readable bytes
            // see http://www.ccarh.org/courses/253/handout/vlv/
            // each byte has 7 data bits and 1 continuation bit
            // total bits
            int dataBitCount = vlvBits.Count * 7;
            int contBitCount = vlvBits.Count; // * 1, but that can be left out since multiplying with 1 is useless
            int totalBits = dataBitCount + contBitCount;
            BitArray finalBits = new BitArray(totalBits);
            int cur = 0;
            foreach (BitArray bits in vlvBits) {
                // start at 1, since we skip the first bit (continuation bit of the bitarray)
                for (int i = 0; i < 7; i++) {
                    int index = (cur * 7) + i;
                    finalBits.Set(index, bits.Get(i));
                }
                cur++;
            }

            // set cont bit "margin"
            for (int i = dataBitCount; i < totalBits; i++) {
                finalBits.Set(i, false);
            }

            // transform final bitarray to byte[]
            byte[] temp = new byte[(totalBits / 8)];
            finalBits.CopyTo(temp, 0);
            temp = temp.Reverse().ToArray();
            field.SetValue(to, temp);
            return vlvBits.Count;
        }

        /// <summary>
        /// Read bytes from the filestream into a field in an object (field with a known length)
        /// </summary>
        /// <param name="field">what field</param>
        /// <param name="to">where to append the filestream values</param>
        private int ReadBytes(FieldInfo field, object to) {
            int numberOfBytes; // how many bytes can the field contain

            if (field.FieldType == typeof(byte[])) {
                numberOfBytes = ((Array)field.GetValue(to)).Length;
            } else {
                numberOfBytes = 1;
            }

            byte[] temp = new byte[numberOfBytes]; // create a temporary byte array with the field size
            this.Stream.Read(temp, 0, numberOfBytes); // read bytes into the temporary array

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
        /// <returns>number of bytes moved in the filestream (always 4)</returns>
        private int ReadInt(FieldInfo field, object to, bool littleendian = false) {
            int numberOfBytes = 4;

            // read bytes
            byte[] temp = new byte[numberOfBytes];
            this.Stream.Read(temp, 0, numberOfBytes);

            // convert bytes into an integer
            int result = ByteConverter.ToInt(temp, littleendian);

            // set value of the field
            field.SetValue(to, result);

            // return travel distance in filestream
            return numberOfBytes;
        }

        /// <summary>
        /// pretty print a class that only has variables made of byte arrays (byte[]) and ints
        /// </summary>
        /// <param name="o">object to print</param>
        public static void DebugByteObject(object o) {
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
                } else if (field.FieldType == typeof(int) && field.GetValue(o) != null) {
                    Console.Write("\n{0, -15}", field.Name);
                    Console.Write("{0, -20} ", (int)field.GetValue(o));
                }
            }
            Console.WriteLine("\n");
        }


        /// <summary>
        /// Writes bytes, byte arrays and int64s into a file as bytes
        /// </summary>
        /// <param name="to">file that we are writing to</param>
        /// <param name="from">class where we are writing from</param>
        /// <param name="skipFields">skip any fields in param from object</param>
        /// <returns>int of how many bytes we wrote</returns>
        public static int Write(FileStream to, object from, string[] skipFields = null) {
            skipFields = skipFields ?? new string[0];
            int traveldistance = 0;
            // loop each field in class
            foreach (FieldInfo field in from.GetType().GetFields()) {
                // skip fields in skipFields array
                if (Array.IndexOf(skipFields, field.Name) == -1 && (
                    field.FieldType == typeof(byte[]) ||
                    field.FieldType == typeof(byte)) ||
                    field.FieldType == typeof(int)) {

                    byte[] temp;

                    if (field.FieldType == typeof(int)) {
                        int value = (int)field.GetValue(from);
                        temp = BitConverter.GetBytes(value);
                        if (!BitConverter.IsLittleEndian) {
                            temp.Reverse();
                        }
                    } else if (field.FieldType == typeof(byte[])) {
                        temp = (byte[])field.GetValue(from);
                    } else {
                        temp = new byte[1] {
                            (byte)field.GetValue(from)
                        };
                    }

                    to.Write(temp, 0, temp.Length);
                    traveldistance += temp.Length;
                }
            }
            return traveldistance;
        }

        /// <summary>
        /// Skip (seek) bytes in filestream
        /// </summary>
        /// <param name="amount">number of bytes to skip</param>
        /// <param name="s">from (default: SeekOrigin.Current)</param>
        public void Skip(int amount, SeekOrigin s = SeekOrigin.Current) {
            this.Stream.Seek(amount, s);
        }


    }
}
