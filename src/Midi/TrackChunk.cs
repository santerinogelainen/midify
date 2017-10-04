using System;
using System.Collections.Generic;
using Midify.MidiFile.Events;
using Midify.Helpers;

namespace Midify.MidiFile.Chunks {
    /// <summary>
    /// track chunk that always starts with MTrk
    /// </summary>
    public class TrackChunk : BigEndianObjectStruct {
        public byte[] Prefix = new byte[4];
        public byte[] Size = new byte[4];
        public int TickSize = 0;
        public List<TrackEvent> Events = new List<TrackEvent>();

        /// <summary>
        /// reads a single track into the list of tracks
        /// </summary>
        /// <param name="track">track</param>
        /// <returns>true if successful</returns>
        public bool Read(AudioStream from, List<TempoEvent> tempoevents, List<TimeSignatureEvent> tsigevents) {
            from.Read(this, skipFields: new string[1] { "TickSize" });

            if (ByteConverter.ToASCIIString(this.Prefix) != "MTrk") {
                Console.WriteLine("Track does not start with MTrk prefix.");
                return false;
            }

            bool success = this.ReadEvents(from, this, tempoevents, tsigevents);

            return success;
        }

        /// <summary>
        /// Reads all events in a track into a list
        /// </summary>
        /// <param name="track">track</param>
        /// <returns>true if successful</returns>
        private bool ReadEvents(AudioStream from, TrackChunk to, List<TempoEvent> tempoevents, List<TimeSignatureEvent> tsigevents) {
            int trackByteSize = ByteConverter.ToInt(to.Size);
            int i = 0;
            while (true) {
                int jumpResult = TrackEvent.Read(from, to, tempoevents, tsigevents);
                if (jumpResult == -1) {
                    Console.WriteLine("Error reading event at index {0} of {1} bytes in the track (byte {2} in whole file).", i, trackByteSize, from.Stream.Position);
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
}
