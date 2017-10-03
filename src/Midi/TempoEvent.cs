﻿
//#define TEMPODEBUG
using System.Collections.Generic;
using Midify.Helpers;

namespace Midify.MidiFile.Events {
    public class TempoEvent : MetaEvent {

        public byte[] MSPerQN = new byte[3] { 0x07, 0xA1, 0x20 };

        public float MilPerQN {
            get {
                return (float)ByteConverter.ToInt(this.MSPerQN) / 1000;
            }
        }

        public float SecPerQN {
            get {
                return (float)ByteConverter.ToInt(this.MSPerQN) / 1000000;
            }
        }

        public float SecPerTick(byte[] division) {
            float div = (float)ByteConverter.ToInt(division);
            return SecPerQN / div;
        }

        public float MilPerTick(byte[] division) {
            float div = (float)ByteConverter.ToInt(division);
            return MilPerQN / div;
        }

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
            eventSize += from.Read(t, skipFields: new string[] { "Timing", "Prefix", "Size", "Type", "AbsoluteTiming" });
#if TEMPODEBUG
            AudioStream.DebugByteObject(t);
#endif
            to.Add(t);
            return eventSize;
        }

    }
}
