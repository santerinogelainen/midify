using System;
using System.Collections.Generic;
using Midify.Helpers;
using Midify.MidiFile.Chunks;

namespace Midify.MidiFile.Events {
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
        public int AbsoluteTiming;

        /// <summary>
        /// reads and determines the type of a track event, and adds that to the proper list
        /// </summary>
        /// <param name="from">stream where to read from</param>
        /// <param name="allevents">list of all events (notes and controllers)</param>
        /// <param name="tempoevents">list of tempo events</param>
        /// <param name="tsigevents">list of time signature events</param>
        /// <returns>number of bytes read</returns>
        public static int Read(AudioStream from, TrackChunk track, List<TempoEvent> tempoevents, List<TimeSignatureEvent> tsigevents) {
            track.Events.Add(new TrackEvent());
            int index = track.Events.Count - 1;
            int eventSize = from.Read(track.Events[index], vlv: "Timing", skipFields: new string[1] { "AbsoluteTiming" });

            track.TickSize += ByteConverter.ToInt(track.Events[index].Timing);
            track.Events[index].AbsoluteTiming = track.TickSize;

            // meta and sysex events
            switch (track.Events[index].Prefix) {
                case (byte)TrackEvent.EventType.SysEx1:
                case (byte)TrackEvent.EventType.SysEx2:
                    Console.WriteLine("System exclusive events are not supported.");
                    return -1;
                case (byte)TrackEvent.EventType.Meta:
                    eventSize += MetaEvent.Read(from, track, tempoevents, tsigevents);
                    break;
                default:
                    int midiread = MidiEvent.Read(from, track.Events);
                    if (midiread == -1) {
                        return -1;
                    }
                    eventSize += midiread;
                    break;
            }
            return eventSize;
        }

    }
}
