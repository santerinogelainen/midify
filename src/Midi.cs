#define DEBUG
//#define METADEBUG

using ByteConvert;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Midis {

    public class Midi {

        private FileStream Stream;
        public HeaderChunk Header = new HeaderChunk();
        public List<TrackChunk> Tracks = new List<TrackChunk>();
        public bool IsLoaded = false;

        /// <summary>
        /// Loads a midi file into memory
        /// </summary>
        /// <param name="file">filepath / filename</param>
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
        /// <returns>integer of how many bytes did we read in the filestream</returns>
        private int ReadStream(object to, int offset = 0, string vlv = "", string[] skipFields = null) {
            skipFields = skipFields ?? new string[0];
            int travelDistance = 0;
            // loop through each field in a class
            foreach (FieldInfo field in to.GetType().GetFields()) {

                // skip fields in skipFields array
                if (Array.IndexOf(skipFields, field.Name) == -1) {
                    // field is vlv
                    if (vlv != "" && field.Name == vlv) {
                        travelDistance += this.ReadVLV(field, to, offset);
                    }

                    // ELSE if the field is a type of byte[] or byte
                     else if (field.FieldType == typeof(byte[]) || field.FieldType == typeof(byte)) {
                        travelDistance += this.ReadBytes(field, to, offset);
                    }
                }
            }
            return travelDistance;
        }

        /// <summary>
        /// Read bytes from the filestream into a field in an object (unknown field length, aka variable length value VLV)
        /// </summary>
        /// <param name="field">what field</param>
        /// <param name="to">where to append the filestream values</param>
        /// <param name="offset">offset (not really used)</param>
        private int ReadVLV(FieldInfo field, object to, int offset) {
            List<byte[]> vlvBytes = new List<byte[]>(); // list of byte arrays (arrays because FileStream.Read requires arrays)
            while (true) {
                vlvBytes.Add(new byte[1]);
                this.Stream.Read(vlvBytes[vlvBytes.Count-1], offset, 1);
                if (vlvBytes[vlvBytes.Count-1][0] < 0x80) {
                    break;
                }
            }
            byte[] temp = new byte[vlvBytes.Count];
            for (int i = 0; i < vlvBytes.Count; i++) {
                temp[i] = vlvBytes[i][0];
            }
            field.SetValue(to, temp);
            return vlvBytes.Count;
        }

        /// <summary>
        /// Read bytes from the filestream into a field in an object (field with a known length)
        /// </summary>
        /// <param name="field">what field</param>
        /// <param name="to">where to append the filestream values</param>
        /// <param name="offset">offset (not really used)</param>
        private int ReadBytes(FieldInfo field, object to, int offset) {
            int numberOfBytes; // how many bytes can the field contain

            if (field.FieldType == typeof(byte[])) {
                numberOfBytes = ((Array)field.GetValue(to)).Length;
            } else {
                numberOfBytes = 1;
            }

            byte[] temp = new byte[numberOfBytes]; // create a temporary byte array with the field size
            this.Stream.Read(temp, offset, numberOfBytes); // read bytes into the temporary array

            // check if the orig field is a byte array or a single byte
            if (field.FieldType == typeof(byte[])) { 
                field.SetValue(to, temp);
            } else {
                field.SetValue(to, temp[0]);
            }
            return numberOfBytes;
        }

        /// <summary>
        /// pretty print a class that only has variables made of byte arrays (byte[])
        /// </summary>
        /// <param name="o">object to print</param>
        private void DebugByteObject(object o) {
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

        /// <summary>
        /// reads all tracks in filestream into a list
        /// </summary>
        /// <returns>true if successful</returns>
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

        /// <summary>
        /// reads a single track into the list of tracks
        /// </summary>
        /// <param name="track">track</param>
        /// <returns>true if successful</returns>
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

            return this.ReadAllEvents(track);
        }

        /// <summary>
        /// Reads all events in a track into a list
        /// </summary>
        /// <param name="track">track</param>
        /// <returns>true if successful</returns>
        private bool ReadAllEvents(TrackChunk track) {
            Int64 trackByteSize = ByteConverter.ToInt(track.Size);
            Int64 i = 0;
            while (true) {
                int jumpResult = ReadEvent(track.Events);
                if (jumpResult == -1) {
                    Console.WriteLine("Error reading event at index {0} of {1} bytes", i, trackByteSize);
                    return false;
                }
                i += jumpResult;
                if (i >= trackByteSize) {
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines the type of a track event and adds that to a list
        /// </summary>
        /// <param name="to">list where to append</param>
        /// <returns>how many bytes added or -1 if errors happened</returns>
        private int ReadEvent(List<TrackEvent> to) {
            to.Add(new TrackEvent());
            int index = to.Count - 1;
            int eventSize = this.ReadStream(to[index], vlv: "Timing");
            
            // meta and sysex events
            switch(to[index].Prefix) {
                case (byte)TrackEvent.EventType.SysEx1:
                case (byte)TrackEvent.EventType.SysEx2:
                    return this.ReadSysExEvent();
                case (byte)TrackEvent.EventType.Meta:
                    eventSize += this.ReadMetaEvent(to);
                    return eventSize;
            }

            // midi events are detected with high bits (first four)
            switch (to[index].Prefix >> 4) {
                case (byte)MidiEvent.MidiEventType.Instrument:
                    eventSize += this.ReadInstrumentEvent(to);
                    return eventSize;
                case (byte)MidiEvent.MidiEventType.Controller:
                    eventSize += this.ReadControllerEvent(to);
                    return eventSize;
                case (byte)MidiEvent.MidiEventType.PitchBend:
                    eventSize += this.ReadPitchBendEvent(to);
                    return eventSize;
                case (byte)MidiEvent.MidiEventType.NoteOn:
                case (byte)MidiEvent.MidiEventType.NoteOff:
                    eventSize += this.ReadNoteEvent(to);
                    return eventSize;
            }

            Console.WriteLine("Unknown event type 0x'{0}'", BitConverter.ToString(new byte[] { to[index].Prefix }));
            return -1;
        }


        /// <summary>
        /// Reads a note event from the file into the TrackEvent List
        /// </summary>
        /// <param name="to">List where to append</param>
        /// <returns>amount of bytes read from filestream</returns>
        private int ReadNoteEvent(List<TrackEvent> to) {

            int index = to.Count - 1;

            NoteEvent n = new NoteEvent();
            n.Timing = to[index].Timing;
            n.Prefix = to[index].Prefix;

            int eventSize = this.ReadStream(n, skipFields: new string[] { "Timing", "Prefix" });

            to[index] = n;

            return eventSize;

        }

        /// <summary>
        /// Reads a controller event from the file into the TrackEvent List
        /// </summary>
        /// <param name="to">List where to append</param>
        /// <returns>amount of bytes read from filestream</returns>
        private int ReadControllerEvent(List<TrackEvent> to) {

            int index = to.Count - 1;

            ControllerEvent c = new ControllerEvent();
            c.Timing = to[index].Timing;
            c.Prefix = to[index].Prefix;

            int eventSize = this.ReadStream(c, skipFields: new string[] {"Timing", "Prefix"});

            to[index] = c;

            return eventSize;
        }

        /// <summary>
        /// skips instrument event data from the filestream, and removes the trackevent from the list
        /// </summary>
        /// <param name="to">list where to remove</param>
        /// <returns>amount of bytes skipped</returns>
        private int ReadInstrumentEvent(List<TrackEvent> to) {

            // skip instrument events in filestream
            this.Stream.Seek(InstrumentEvent.Size, SeekOrigin.Current);

            // remove event
            to.RemoveAt(to.Count-1);

            return InstrumentEvent.Size;
        }

        /// <summary>
        /// skips pitch bend event data from the filestream, and removes the trackevent from the list
        /// </summary>
        /// <param name="to">list where to remove</param>
        /// <returns>amount of bytes skipped</returns>
        private int ReadPitchBendEvent(List<TrackEvent> to) {

            // skip instrument events in filestream
            this.Stream.Seek(PitchBendEvent.Size, SeekOrigin.Current);

            // remove event
            to.RemoveAt(to.Count - 1);

            return PitchBendEvent.Size;
        }


        /// <summary>
        /// gives an error because sysex events are not supported
        /// </summary>
        /// <returns>error code -1</returns>
        private int ReadSysExEvent() {
            Console.WriteLine("System exclusive events are not supported.");
            return -1;
        }


        private int ReadMetaEvent(List<TrackEvent> to) {
            // index is always last one in list
            int index = to.Count - 1;

            // new meta event with the prefix and timing
            MetaEvent m = new MetaEvent();

            // read new data into the meta event class, Size is a VLV and skip over Timing and Prefix
            int eventSize = this.ReadStream(m, vlv: "Size", skipFields: new string[]{"Timing", "Prefix"});

            // skip the meta event data
            Int64 skip = ByteConverter.ToInt(m.Size);
            this.Stream.Seek(skip, SeekOrigin.Current);

            // remove the event since we do not need it
            to.RemoveAt(index);

#if (METADEBUG)
            this.DebugByteObject(m);
            Console.WriteLine("Bytes skipped: {0}", skip);
#endif

            return eventSize + (int)skip;
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
        public List<TrackEvent> Events = new List<TrackEvent>();
    }

    public class TrackEvent {

        // event types, midi events have multiple types that only use 4bits
        public enum EventType : byte {
            Meta = 0xff,
            SysEx1 = 0xf0,
            SysEx2 = 0xf7
        };

        // event timing
        public byte[] Timing;//VLV
        public byte Prefix;
        
    }

    public abstract class MidiEvent : TrackEvent  {
        
        // event types for midi events
        public enum MidiEventType : byte {
            NoteOn = 0x9,
            NoteOff = 0x8,
            Instrument = 0xc,
            Controller = 0xb,
            PitchBend = 0xe
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
