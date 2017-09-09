#define DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ByteConvert;

namespace Midis {

    public class Midi {

        private FileStream Stream;
        public HeaderChunk Header = new HeaderChunk();
        public List<TrackChunk> Tracks = new List<TrackChunk>();
        public bool IsLoaded = false;

        public Midi(string file) {
            if (this.ReadFile(file)) {
                this.IsLoaded = true;
            }
        }

        /// <summary>
        /// initalizes the FileStream variable, and sets all the headers / tracks of the file
        /// </summary>
        /// <param name="file"></param>
        /// <returns>true if successful</returns>
        private bool ReadFile(string file) {

            // check if file exists
            if (!File.Exists(file)) {
                Console.WriteLine("File '{0}' does not exists.", file);
                return false;
            }

            // init filestream
            this.Stream = new FileStream(file, FileMode.Open);

            // read the header into the Header variable
            if (!this.ReadHeader()) {
                Console.WriteLine("Error reading the header.");
                return false;
            }

            // read all the tracks into the this.Tracks list
            if (!this.ReadAllTracks()) {
                Console.WriteLine("Error reading tracks.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// reads binary data into a class with byte variables or arrays (kinda similar to how structs and fread works in c)
        /// </summary>
        /// <param name="to">the object/class where to append</param>
        /// <param name="offset">offset where to start reading</param>
        private void ReadStream(object to, int offset = 0) {
            // loop through each field in a class
            foreach (FieldInfo field in to.GetType().GetFields()) {
                // if the field is a type of byte[] or byte
                if (field.FieldType == typeof(byte[]) || field.FieldType == typeof(byte)) {
                    int numberOfBytes = ((Array)field.GetValue(to)).Length;
                    byte[] temp = new byte[numberOfBytes];
                    this.Stream.Read(temp, offset, numberOfBytes);
                    if (field.FieldType == typeof(byte[])) {
                        to.GetType().GetField(field.Name).SetValue(to, temp);
                    } else {
                        to.GetType().GetField(field.Name).SetValue(to, temp[0]);
                    }
                }
            }
        }

        /// <summary>
        /// pretty print a class that only has variables made of byte arrays (byte[])
        /// </summary>
        /// <param name="o">object to print</param>
        private void DebugByteObject(object o) {
            Console.WriteLine("\n{0}", o.GetType().Name);
            foreach (FieldInfo field in o.GetType().GetFields()) {
                if (field.FieldType == typeof(byte[]) || field.FieldType == typeof(byte)) {
                    Console.Write("\n{0, -15}", field.Name);
                    byte[] value = (byte[])field.GetValue(o);
                    Console.Write("{0, -20} ", ByteConverter.ToInt(value));
                    Console.Write("{0, -20} ", BitConverter.ToString(value));
                    Console.Write("{0}", ByteConverter.ToASCIIString(value));
                }
            }
            Console.WriteLine("\n");
        }

        /// <summary>
        /// read the header of the file to this.header
        /// </summary>
        private bool ReadHeader() {
            // read the first 14 bytes in the filestream to header
            this.ReadStream(this.Header);
#if (DEBUG)
            // show debug info
            this.DebugByteObject(this.Header);
#endif
            // probably a midi file
            if (ByteConverter.ToASCIIString(this.Header.Prefix) == "MThd" &&
                ByteConverter.ToInt(this.Header.Size) == 6) {

                // too many songs in one file
                if (ByteConverter.ToInt(this.Header.Format) == 2) {
                    Console.WriteLine("Midi files with multiple songs are not supported.");
                    return false;
                }
                return true;
            }
            Console.WriteLine("The file given is not a midi file.");
            return false;
        }


        private bool ReadAllTracks() {
            // number of tracks in the file
            Int64 numberOfTracks = ByteConverter.ToInt(this.Header.Tracks);

            // loop each track
            for (int i = 0; i < numberOfTracks; i++) {

                // add new track
                this.Tracks.Add(new TrackChunk());

                // try reading the track info into the new track
                if (!this.ReadTrack(this.Tracks[i])) {
                    Console.WriteLine("Error reading track at index {0}.", i);
                    return false;
                }

            }
            return true;
        }


        private bool ReadTrack(TrackChunk track) {
            this.ReadStream(track);

#if (DEBUG)
            // show track header info
            this.DebugByteObject(track);
#endif

            if (ByteConverter.ToASCIIString(track.Prefix) != "MTrk") {
                Console.WriteLine("Track does not start with MTrk prefix.");
                return false;
            }

            // TODO: implement trackevent reader

            return true;
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
