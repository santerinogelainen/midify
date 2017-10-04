using Midify.MidiFile.Events;
using Midify.Helpers;
using Midify.WaveFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Midify.MidiFile {
    class TickInfo {

        private TempoEvent Tempo = new TempoEvent();
        public byte[] SampleRate;
        public byte[] Division;
        public Sample[] DefaultSample;
        public int SampleSize;
        
        public TickInfo(byte[] division, byte[] samplerate) {
            this.SampleRate = samplerate;
            this.Division = division;
            this.SetDefaultSample();
        }

        public void Update(TempoEvent tempo) {
            this.Tempo = tempo;
            this.SetDefaultSample();
        }

        private void SetDefaultSample() {
            int tick = Tempo.SamplesPerTick(this.Division, this.SampleRate);
            DefaultSample = new Sample[tick];
            SampleSize = tick;
            for (int i = 0; i < tick; i++) {
                DefaultSample[i] = new Sample();
                DefaultSample[i].Left = new byte[2] { 0x00, 0x00 };
                DefaultSample[i].Right = new byte[2] { 0x00, 0x00 };
            }
        }

    }
}
