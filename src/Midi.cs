#define DEBUG

using System;
using System.IO;
using System.Reflection;

namespace Midis {

    public class Midi {

        private FileStream stream;
        public HeaderChunk header = new HeaderChunk();

        public Midi(string file) {
            this.ReadFile(file);
        }

        /// <summary>
        /// initalizes the FileStream variable, and sets all the headers / tracks of the file
        /// </summary>
        /// <param name="file"></param>
        private void ReadFile(string file) {
            this.stream = new FileStream(file, FileMode.Open);
            this.ReadHeader();
        }

        /// <summary>
        /// reads data into a class made of byte arrays (similar to how structs and fread works in c)
        /// </summary>
        /// <param name="to">the object/class where to append</param>
        /// <param name="offset">offset where to start reading</param>
        private void ReadStream(object to, int offset = 0) {
            foreach (FieldInfo field in to.GetType().GetFields()) {
                byte[] temp = new byte[((Array)field.GetValue(to)).Length];
                this.stream.Read(temp, offset, ((Array)field.GetValue(to)).Length);
                to.GetType().GetField(field.Name).SetValue(to, temp);
            }
        }

        /// <summary>
        /// pretty print a class that only has variables made of byte arrays (byte[])
        /// </summary>
        /// <param name="o">object to print</param>
        private void DebugByteObject(object o) {
            Console.WriteLine("\n{0}", o.GetType().Name);
            foreach (FieldInfo field in o.GetType().GetFields()) {
                Console.Write("\n{0, -15}", field.Name);
                byte[] value = (byte[])field.GetValue(o);
                Console.Write("{0, -20} ", this.ByteArrayToInt(value));
                Console.Write("{0, -20} ", BitConverter.ToString(value));
                foreach (byte b in value) {
                    Console.Write("{0}", (char)b);
                }
            }
            Console.WriteLine("\n");
        }

        /// <summary>
        /// convert a byte array of unknown size into a Int64
        /// </summary>
        /// <param name="arr">byte array</param>
        /// <returns>Int64 value of the byte(s)</returns>
        private Int64 ByteArrayToInt(byte[] arr) {
            switch (arr.Length) {
                case sizeof(sbyte):
                    return (Int64)arr[0];
                case sizeof(Int16):
                    return (Int64)BitConverter.ToInt16(arr, 0);
                case sizeof(Int32):
                    return (Int64)BitConverter.ToInt32(arr, 0);
                case sizeof(Int64):
                    return (Int64)BitConverter.ToInt64(arr, 0);
            }
            Console.WriteLine("Too big to convert into an integer.");
            return -1;
        }

        /// <summary>
        /// read the header of the file to this.header
        /// </summary>
        private void ReadHeader() {
            this.ReadStream(this.header);
#if (DEBUG)
            this.DebugByteObject(this.header);
#endif
        }
    }

    /// <summary>
    /// header chunk "structure"
    /// </summary>
    public class HeaderChunk {
        public byte[] prefix = new byte[4];
        public byte[] length = new byte[4];
        public byte[] format = new byte[2];
        public byte[] tracks = new byte[2];
        public byte[] timing = new byte[2];
    }

}
