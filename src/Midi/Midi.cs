#define DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using Midify.Helpers;
using Midify.WaveFile;
using Midify.MidiFile.Chunks;
using Midify.MidiFile.Events;
using System.Linq;

namespace Midify.MidiFile {

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
        /// reads all tracks in filestream into three lists
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

            // order tempo and timesignature changes by absolute timing
            this.TempoChanges = this.TempoChanges.OrderBy(x => x.AbsoluteTiming).ToList();
            this.TimeSignatureChanges = this.TimeSignatureChanges.OrderBy(x => x.AbsoluteTiming).ToList();

            // order all trackevents and remove useless tracks
            this.Tracks.RemoveAll(x => x.Events.Count == 0);
            foreach (TrackChunk track in this.Tracks) {
                track.Events = track.Events.OrderBy(x => x.AbsoluteTiming).ToList();
            }
            return true;
        }

        /// <summary>
        /// reads a single track into the list of tracks
        /// </summary>
        /// <param name="track">track</param>
        /// <returns>true if successful</returns>
        private bool ReadTrack(TrackChunk track) {
            this.Stream.Read(track, skipFields: new string[1] { "TickSize" });

            if (ByteConverter.ToASCIIString(track.Prefix) != "MTrk") {
                Console.WriteLine("Track does not start with MTrk prefix.");
                return false;
            }

            bool success = this.ReadAllEvents(track);

#if (DEBUG)
            // show track header info
            AudioStream.DebugByteObject(track);
#endif

            return success;
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
                int jumpResult = TrackEvent.Read(this.Stream, track, this.TempoChanges, this.TimeSignatureChanges);
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

        public Wave TrackToWave(TrackChunk track, string clippath) {
            Wave clip = new Wave(FileMode.Open, clippath);

            // final wave
            Wave final = new Wave(FileMode.Create);

            // copy of tempo events
            List<TempoEvent> tempoevents = this.TempoChanges;
            List<TrackEvent> trackevents = track.Events;
            // final list of samples
            List<Sample> samples = new List<Sample>();
            // current tempo of the song
            TempoEvent curTempo = new TempoEvent(); // default 120bpm, 500000MSPerQN

            NoteEvent[,] openEvents = new NoteEvent[16, 128];
            // loop each tick
            for (int tick = 0; tick < track.TickSize; tick++) {
                // check tempo changes, tempo changes are in order
                while (true) {
                    if (tempoevents.Count != 0 && tick == tempoevents[0].AbsoluteTiming) {
                        curTempo = tempoevents[0];
                        tempoevents.RemoveAt(0);
                    } else {
                        break;
                    }
                }
                // check note events
                while (true) {
                    if (trackevents.Count != 0 && tick == trackevents[0].AbsoluteTiming && ((trackevents[0].Prefix >> 4) == 0x9 || (trackevents[0].Prefix >> 4) == 0x8)) {
                        int channel = (int)(trackevents[0].Prefix & 0x0f);
                        int pitch = (int)((NoteEvent)trackevents[0]).Pitch;
                        if (openEvents[channel, pitch] != null) {
                            openEvents[channel, pitch] = null;
                        } else {
                            openEvents[channel, pitch] = (NoteEvent)trackevents[0];
                            // to do:
                            // place clip data into (tick * secpertick) * samplerate(44100)th position in final.data.samples list
                            // at the same time check if there already is data there...
                            // if there is, change the existing sample byte data with the following:
                            // (oldsample.left / 2) + (newsample.left / 2)
                            // (oldsample.right / 2) + (newsample.right / 2)
                            // if there is not place 0byte samples
                        }
                        trackevents.RemoveAt(0);
                    } else if (trackevents.Count != 0 && tick == trackevents[0].AbsoluteTiming && (trackevents[0].Prefix >> 4) == 0xb) {
                        byte type = ((ControllerEvent)trackevents[0]).Controller;
                        // clear all notes
                        if (type == (byte)ControllerEvent.ControllerEventType.NotesOff) {
                            for (int channel = 0; channel < 16; channel++) {
                                for (int pitch = 0; pitch < 128; pitch++) {
                                    openEvents[channel, pitch] = null;
                                }
                            }
                        }
                    } else {
                        break;
                    }
                }
            }

            return clip;
        }

    }
}
