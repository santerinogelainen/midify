﻿#define DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Midis {

    public class Midi {

        private FileStream stream;
        public HeaderChunk header = new HeaderChunk();
        public List<TrackChunk> tracks = new List<TrackChunk>();

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
        public byte[] Prefix = new byte[4];
        public byte[] Size = new byte[4];
        public byte[] Format = new byte[2];
        public byte[] Tracks = new byte[2];
        public byte[] Timing = new byte[2];
    }

    /// <summary>
    /// track chunk that always starts with MTrk
    /// </summary>
    public class TrackChunk {
        public byte[] Prefix = new byte[4];
        public byte[] Size = new byte[4];
        public List<TrackEvent> events = new List<TrackEvent>();
    }

    public abstract class TrackEvent {

        // event types, midi events have multiple types that only use 4bits
        public enum EventType {
            Meta = 0xff,
            SysEx1 = 0xf0,
            SysEx2 = 0xf7
        };

        // event timing
        public byte[] Timing; //VLV
        public byte Prefix;
        
    }

    public abstract class MidiEvent : TrackEvent  {
        
        // event types for midi events
        public enum MidiEventType {
            MidiNoteOn = 0x9,
            MidiNoteOff = 0x8,
            MidiInstrument = 0xc,
            MidiConstroller = 0xb,
            MidiPitchBend = 0xe
        }

    }

    /// <summary>
    /// note on and note off are almost the same (only prefix is differrent)
    /// note events in midi files
    /// </summary>
    public class NoteEvent : MidiEvent {

        public static int Size = 2;
        public byte Pitch;
        public byte Velocity;

    }

    /// <summary>
    /// intrument events, skipping with size
    /// </summary>
    public class InstrumentEvent : MidiEvent {

        public static int Size = 1;

    }

    /// <summary>
    /// midi controller events for controlling volume, stereo (panoramic), etc.
    /// </summary>
    public class ControllerEvent : MidiEvent {

        // types that I might care about are listed here
        public enum ControllerEventType {
            Volume = 0x07,
            Panoramic = 0x0a,
            ControllersOff = 0x79,
            NotesOff = 0x7b
        };

        public static int Size = 2;
        public byte Controller;
        public byte Value;

    }

    /// <summary>
    /// useless, we only care about size so we can skip this
    /// </summary>
    public class PitchBendEvent : MidiEvent {

        public static int Size = 2;

    }


    /// <summary>
    /// meta events are ignored, we only need the size so we can skip it
    /// </summary>
    public class MetaEvent : TrackEvent {
        
        public byte Type;
        public byte[] Size; //VLV
        
    }

}