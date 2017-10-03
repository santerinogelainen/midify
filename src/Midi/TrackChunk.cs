using System.Collections.Generic;
using Midify.MidiFile.Events;

namespace Midify.MidiFile.Chunks {
    /// <summary>
    /// track chunk that always starts with MTrk
    /// </summary>
    public class TrackChunk {
        public byte[] Prefix = new byte[4];
        public byte[] Size = new byte[4];
        public int TickSize = 0;
        public List<TrackEvent> Events = new List<TrackEvent>();

    }
}
