//#define TIMESIGDEBUG
using System.Collections.Generic;
using Midify.Helpers;

namespace Midify.MidiFile.Events {
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
            t.CopyFrom(original);
            eventSize += from.Read(t, skipFields: new string[] { "Timing", "Prefix", "Size", "Type", "AbsoluteTiming" });
#if TIMESIGDEBUG
            this.Debug();
#endif
            to.Add(t);
            return eventSize;
        }

    }
}
