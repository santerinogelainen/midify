using System.Collections.Generic;
using Midify.Helpers;

namespace Midify.MidiFile.Events {
    
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

            int eventSize = from.Read(c, skipFields: new string[] { "Timing", "Prefix", "AbsoluteTiming" });

            to[index] = c;

            return eventSize;
        }

    }
}
