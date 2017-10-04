using System;
using System.Reflection;

namespace Midify.Helpers {
    abstract public class ObjectStruct {

        public bool IsLittleEndian;

        /// <summary>
        /// pretty print this classes variables (ints, bytes and byte arrays)
        /// </summary>
        public void Debug(bool endian) {
            Console.WriteLine("\n{0}", this.GetType().Name);
            foreach (FieldInfo field in this.GetType().GetFields()) {
                if ((field.FieldType == typeof(byte[]) || field.FieldType == typeof(byte)) && field.GetValue(this) != null) {
                    Console.Write("\n{0, -15}", field.Name);
                    byte[] value;
                    if (field.FieldType == typeof(byte[])) {
                        value = (byte[])field.GetValue(this);
                    } else {
                        value = new byte[] { (byte)field.GetValue(this) };
                    }
                    Console.Write("{0, -20} ", ByteConverter.ToInt(value, endian));
                    Console.Write("{0, -20} ", BitConverter.ToString(value));
                    Console.Write("{0}", ByteConverter.ToASCIIString(value));
                } else if (field.FieldType == typeof(int) && field.GetValue(this) != null) {
                    Console.Write("\n{0, -15}", field.Name);
                    Console.Write("{0, -20} ", (int)field.GetValue(this));
                }
            }
            Console.WriteLine("\n");
        }

        /// <summary>
        /// Copies byte arrays, bytes and integers from one object to this object
        /// </summary>
        /// <param name="from">object where to copy from</param>
        /// <param name="skipFields">skip any fields?</param>
        /// <returns>number of fields copied</returns>
        public int CopyFrom(object from, string[] skipFields = null) {
            skipFields = skipFields ?? new string[0];
            int copied = 0;
            foreach (FieldInfo field in from.GetType().GetFields()) {
                if (Array.IndexOf(skipFields, field.Name) == -1 &&
                    (field.FieldType == typeof(byte[]) || field.FieldType == typeof(byte) || field.FieldType == typeof(int)) &&
                    !field.IsStatic &&
                    field.GetValue(from) != null) {

                    FieldInfo tofield = this.GetType().GetField(field.Name);
                    if (tofield != null) {
                        tofield.SetValue(this, field.GetValue(from));
                        copied++;
                    }
                }
            }
            return copied;
        }
    }


    public class LittleEndianObjectStruct : ObjectStruct {

        public new bool IsLittleEndian = true;

        public void Debug() {
            base.Debug(this.IsLittleEndian);
        }

    }

    public class BigEndianObjectStruct : ObjectStruct {

        public new bool IsLittleEndian = false;

        public void Debug() {
            base.Debug(this.IsLittleEndian);
        }

    }


}
