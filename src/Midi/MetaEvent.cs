
//#define METADEBUG
using System.Collections.Generic;
using Midify.Helpers;
using Midify.MidiFile.Chunks;

namespace Midify.MidiFile.Events {
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
        public new static int Read(AudioStream from, TrackChunk track, List<TempoEvent> tempoevents, List<TimeSignatureEvent> tsigevents) {
            // index is always last one in list
            int index = track.Events.Count - 1;

            // new meta event with the prefix and timing
            MetaEvent m = new MetaEvent();
            AudioStream.Copy(track.Events[index], m);

            // read new data into the meta event class, Size is a VLV and skip over Timing and Prefix
            int eventSize = from.Read(m, vlv: "Size", skipFields: new string[] { "Timing", "Prefix", "AbsoluteTiming" });

            // read tempo or time signature events
            switch (m.Type) {
                case (byte)MetaEvent.MetaEventType.Tempo:
                    eventSize += TempoEvent.Read(from, track.Events[index], tempoevents);
                    track.Events.RemoveAt(index);
                    return eventSize;
                case (byte)MetaEvent.MetaEventType.TimeSignature:
                    eventSize += TimeSignatureEvent.Read(from, track.Events[index], tsigevents);
                    track.Events.RemoveAt(index);
                    return eventSize;
            }

            // skip the meta event data
            int skip = ByteConverter.ToInt(m.Size);
            from.Skip(skip);

            // remove the event since we do not need it
            track.Events.RemoveAt(index);

#if (METADEBUG)
            AudioStream.DebugByteObject(m);
            Console.WriteLine("Bytes skipped: {0}", skip);
#endif

            return eventSize + skip;
        }

    }
}
