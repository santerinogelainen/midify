#define DEBUG
//#define METADEBUG

using ByteConvert;
using System;
using System.Collections.Generic;
using System.IO;
using AudioFileStream;

namespace Midis {

    public class Midi {

        private AudioStream Stream;
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
            this.Stream = new AudioStream(file);

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
        /// read the header of the file to this.header
        /// </summary>
        private bool ReadHeader() {
            // read the first 14 bytes in the filestream to header
            this.Stream.Read(this.Header);
#if (DEBUG)
            // show debug info
            this.Stream.DebugByteObject(this.Header);
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
            int numberOfTracks = ByteConverter.ToInt(this.Header.Tracks);

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
            this.Stream.Read(track);

#if (DEBUG)
            // show track header info
            this.Stream.DebugByteObject(track);
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
            int trackByteSize = ByteConverter.ToInt(track.Size);
            int i = 0;
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
            int eventSize = this.Stream.Read(to[index], vlv: "Timing");
            
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

            Console.WriteLine("Unknown event type 0x{0}", BitConverter.ToString(new byte[] { to[index].Prefix }));
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

            int eventSize = this.Stream.Read(n, skipFields: new string[] { "Timing", "Prefix" });

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

            int eventSize = this.Stream.Read(c, skipFields: new string[] {"Timing", "Prefix"});

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
            this.Stream.Skip(InstrumentEvent.Size);

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
            this.Stream.Skip(PitchBendEvent.Size);

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
            int eventSize = this.Stream.Read(m, vlv: "Size", skipFields: new string[]{"Timing", "Prefix"});

            // read tempo or time signature events
            switch (m.Type) {
                case (byte)MetaEvent.MetaEventType.Tempo:
                    TempoEvent t = new TempoEvent();
                    t.Timing = to[index].Timing;
                    t.Prefix = to[index].Prefix;
                    t.Size = m.Size;
                    t.Type = m.Type;
                    eventSize += this.Stream.Read(t, skipFields: new string[] { "Timing", "Prefix", "Size", "Type" });
#if DEBUG
                    this.Stream.DebugByteObject(t);
#endif
                    to[index] = t;
                    return eventSize;
                case (byte)MetaEvent.MetaEventType.TimeSignature:
                    TimeSignatureEvent ts = new TimeSignatureEvent();
                    ts.Timing = to[index].Timing;
                    ts.Prefix = to[index].Prefix;
                    ts.Size = m.Size;
                    ts.Type = m.Type;
                    to[index] = ts;
                    eventSize += this.Stream.Read(to[index], skipFields: new string[] { "Timing", "Prefix", "Size", "Type" });
#if DEBUG
                    this.Stream.DebugByteObject(to[index]);
#endif
                    return eventSize;
            }

            // skip the meta event data
            int skip = ByteConverter.ToInt(m.Size);
            this.Stream.Skip(skip);

            // remove the event since we do not need it
            to.RemoveAt(index);

#if (METADEBUG)
            this.Stream.DebugByteObject(m);
            Console.WriteLine("Bytes skipped: {0}", skip);
#endif

            return eventSize + skip;
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
    /// meta events are ignored, unless they are tempo or time signature events
    /// </summary>
    public class MetaEvent : TrackEvent {

        public enum MetaEventType {
            Tempo = 0x51,
            TimeSignature = 0x58
        }
        
        public byte Type;
        public byte[] Size; //VLV
        
    }

    public class TempoEvent : MetaEvent {

        public byte[] MSPerQN = new byte[3];

    }

    public class TimeSignatureEvent : MetaEvent {

        public byte Numerator;
        public byte Denominator;
        public byte TicksPerClick;
        public byte QN32;

    }

}
