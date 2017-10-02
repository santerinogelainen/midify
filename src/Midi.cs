#define DEBUG
//#define METADEBUG
//#define NOTEDEBUG
#define TEMPODEBUG
#define TIMESIGDEBUG

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
        public List<TempoEvent> TempoChanges = new List<TempoEvent>();
        public List<TimeSignatureEvent> TimeSignatureChanges = new List<TimeSignatureEvent>();

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
            AudioStream.DebugByteObject(this.Header);
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
            AudioStream.DebugByteObject(track);
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
                int jumpResult = TrackEvent.Read(this.Stream, track.Events, this.TempoChanges, this.TimeSignatureChanges);
                if (jumpResult == -1) {
                    Console.WriteLine("Error reading event at index {0} of {1} bytes in the track (byte {2} in whole file).", i, trackByteSize, this.Stream.Stream.Position);
                    return false;
                }
                i += jumpResult;
                if (i >= trackByteSize) {
                    break;
                }
            }

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

        /// <summary>
        /// reads and determines the type of a track event, and adds that to the proper list
        /// </summary>
        /// <param name="from">stream where to read from</param>
        /// <param name="allevents">list of all events (notes and controllers)</param>
        /// <param name="tempoevents">list of tempo events</param>
        /// <param name="tsigevents">list of time signature events</param>
        /// <returns>number of bytes read</returns>
        public static int Read(AudioStream from, List<TrackEvent> allevents, List<TempoEvent> tempoevents, List<TimeSignatureEvent> tsigevents) {
            allevents.Add(new TrackEvent());
            int index = allevents.Count - 1;
            int eventSize = from.Read(allevents[index], vlv: "Timing");

            // meta and sysex events
            switch (allevents[index].Prefix) {
                case (byte)TrackEvent.EventType.SysEx1:
                case (byte)TrackEvent.EventType.SysEx2:
                    Console.WriteLine("System exclusive events are not supported.");
                    return -1;
                case (byte)TrackEvent.EventType.Meta:
                    eventSize += MetaEvent.Read(from, allevents, tempoevents, tsigevents);
                    return eventSize;
            }

            int midiread = MidiEvent.Read(from, allevents);
            if (midiread == -1) {
                return -1;
            }

            eventSize += midiread;
            return eventSize;
        }
        
    }

    public abstract class MidiEvent : TrackEvent  {
        
        // event types for midi events
        public enum MidiEventType : byte {
            NoteOff = 0x8,
            NoteOn = 0x9,
            PolyphonicAT = 0xa,
            Controller = 0xb,
            Instrument = 0xc,
            ChannelAT = 0xd,
            PitchBend = 0xe
        }

        /// <summary>
        /// Reads a midievent from a stream
        /// </summary>
        /// <param name="from">stream where to read from</param>
        /// <param name="to">list where to read</param>
        /// <returns>number of bytes read or -1 if unknown event type</returns>
        public static int Read(AudioStream from, List<TrackEvent> to) {
            int index = to.Count - 1;
            int eventSize = 0;
            // midi events are detected with high bits (first four)
            switch (to[index].Prefix >> 4) {
                case (byte)MidiEvent.MidiEventType.Controller:
                    eventSize += ControllerEvent.Read(from, to);
                    break;
                case (byte)MidiEvent.MidiEventType.NoteOn:
                case (byte)MidiEvent.MidiEventType.NoteOff:
                    eventSize += NoteEvent.Read(from, to);
                    break;
                case (byte)MidiEvent.MidiEventType.Instrument:
                case (byte)MidiEvent.MidiEventType.ChannelAT:
                    from.Skip(1);
                    return 1;
                case (byte)MidiEvent.MidiEventType.PolyphonicAT:
                case (byte)MidiEvent.MidiEventType.PitchBend:
                    from.Skip(2);
                    return 2;
                default:
                    Console.WriteLine("Unknown midi event type 0x{0}", BitConverter.ToString(new byte[] { to[index].Prefix }));
                    return -1;
            }
            return eventSize;
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

        /// <summary>
        /// Reads a noteevent from the filestream
        /// </summary>
        /// <param name="from">stream where to read from</param>
        /// <param name="to">where to read event</param>
        /// <returns>number of bytes read</returns>
        public new static int Read(AudioStream from, List<TrackEvent> to) {
            int index = to.Count - 1;

            NoteEvent n = new NoteEvent();
            AudioStream.Copy(to[index], n);

            int eventSize = from.Read(n, skipFields: new string[] { "Timing", "Prefix" });

#if NOTEDEBUG
            AudioStream.DebugByteObject(n);
#endif

            to[index] = n;
            return eventSize;
        }

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

        /// <summary>
        /// Reads a controllerevent from the filestream
        /// </summary>
        /// <param name="from">stream where to read from</param>
        /// <param name="to">where to read event</param>
        /// <returns>number of bytes read</returns>
        public new static int Read(AudioStream from, List<TrackEvent> to) {
            int index = to.Count - 1;

            ControllerEvent c = new ControllerEvent();
            AudioStream.Copy(to[index], c);

            int eventSize = from.Read(c, skipFields: new string[] { "Timing", "Prefix" });

            to[index] = c;

            return eventSize;
        }

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

        /// <summary>
        /// Reads a metaevent from the filestream
        /// </summary>
        /// <param name="from">stream where to read from</param>
        /// <param name="allevents">list of all trackevents</param>
        /// <param name="tempoevents">list of tempoevents</param>
        /// <param name="tsigevents">list of timesignatureevents</param>
        /// <returns>number of bytes read / skipped</returns>
        public new static int Read(AudioStream from, List<TrackEvent> allevents, List<TempoEvent> tempoevents, List<TimeSignatureEvent> tsigevents) {
            // index is always last one in list
            int index = allevents.Count - 1;

            // new meta event with the prefix and timing
            MetaEvent m = new MetaEvent();
            AudioStream.Copy(allevents[index], m);

            // read new data into the meta event class, Size is a VLV and skip over Timing and Prefix
            int eventSize = from.Read(m, vlv: "Size", skipFields: new string[] { "Timing", "Prefix" });

            // read tempo or time signature events
            switch (m.Type) {
                case (byte)MetaEvent.MetaEventType.Tempo:
                    eventSize += TempoEvent.Read(from, allevents[index], tempoevents);
                    allevents.RemoveAt(index);
                    return eventSize;
                case (byte)MetaEvent.MetaEventType.TimeSignature:
                    eventSize += TimeSignatureEvent.Read(from, allevents[index], tsigevents);
                    allevents.RemoveAt(index);
                    return eventSize;
            }

            // skip the meta event data
            int skip = ByteConverter.ToInt(m.Size);
            from.Skip(skip);

            // remove the event since we do not need it
            allevents.RemoveAt(index);

#if (METADEBUG)
            AudioStream.DebugByteObject(m);
            Console.WriteLine("Bytes skipped: {0}", skip);
#endif

            return eventSize + skip;
        }

    }

    public class TempoEvent : MetaEvent {

        public byte[] MSPerQN = new byte[3];

        /// <summary>
        /// Reads a tempoevent into a list of trackevents
        /// </summary>
        /// <param name="from">audiostream to read from</param>
        /// <param name="to">list of trackevents where to read</param>
        /// <returns>number of bytes read from the stream</returns>
        public static int Read(AudioStream from, TrackEvent original, List<TempoEvent> to) { 
            int eventSize = 0;
            TempoEvent t = new TempoEvent();
            AudioStream.Copy(original, t);
            eventSize += from.Read(t, skipFields: new string[] { "Timing", "Prefix", "Size", "Type" });
#if TEMPODEBUG
            AudioStream.DebugByteObject(t);
#endif
            to.Add(t);
            return eventSize;
        }

    }

    public class TimeSignatureEvent : MetaEvent {

        public byte Numerator;
        public byte Denominator;
        public byte TicksPerClick;
        public byte QN32;

        /// <summary>
        /// Reads a timesignatureevent into a list of trackevents
        /// </summary>
        /// <param name="from">audiostream to read from</param>
        /// <param name="to">list of trackevents where to read</param>
        /// <returns>number of bytes read from the stream</returns>
        public static int Read(AudioStream from, TrackEvent original, List<TimeSignatureEvent> to) {
            int eventSize = 0;
            TimeSignatureEvent t = new TimeSignatureEvent();
            AudioStream.Copy(original, t);
            eventSize += from.Read(t, skipFields: new string[] { "Timing", "Prefix", "Size", "Type" });
#if TIMESIGDEBUG
            AudioStream.DebugByteObject(t);
#endif
            to.Add(t);
            return eventSize;
        }

    }

}
