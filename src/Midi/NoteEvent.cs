
//#define NOTEDEBUG
using System.Collections.Generic;
using Midify.Helpers;

namespace Midify.MidiFile.Events {
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

            int eventSize = from.Read(n, skipFields: new string[] { "Timing", "Prefix", "AbsoluteTiming" });

#if NOTEDEBUG
            AudioStream.DebugByteObject(n);
#endif

            to[index] = n;
            return eventSize;
        }

    }
}
