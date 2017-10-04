using System;
using System.Collections.Generic;
using Midify.Helpers;

namespace Midify.MidiFile.Events {
    public abstract class MidiEvent : TrackEvent {

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
                    to.RemoveAt(index);
                    return 1;
                case (byte)MidiEvent.MidiEventType.PolyphonicAT:
                case (byte)MidiEvent.MidiEventType.PitchBend:
                    from.Skip(2);
                    to.RemoveAt(index);
                    return 2;
                default:
                    Console.WriteLine("Unknown midi event type 0x{0}", BitConverter.ToString(new byte[] { to[index].Prefix }));
                    return -1;
            }
            return eventSize;
        }

    }
}
