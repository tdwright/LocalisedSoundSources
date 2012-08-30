using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalisedSoundSources
{
    public class LocalisedSineWaveProvider32 : WaveProvider32
    {
        int sample;

        static readonly public float C = 331.5f; // speed of sound (m/s)

        // Mean circumference of the head is 42 cm
        // http://www.wolframalpha.com/share/clip?f=d41d8cd98f00b204e9800998ecf8427eu4grvv6f2b
        static readonly public float R = (42f/100f)/((float)Math.PI*2f); // radius of hypothetical cranial sphere (m)

        readonly public float ITDfactor = 3f * (float)(R/C); // Easier to just calc this once.

#region "Initialisation"

        public LocalisedSineWaveProvider32()
        {
            Frequency = 1000;
            Amplitude = 0.25f; // let's not hurt our ears
            AzimuthRads = 0f;
        }

        public float Frequency { get; set; }
        public float Amplitude { get; set; }

        [System.Obsolete("Use SetWaveFormat(int sampleRate). (Localisation requires exactly two channels.)")]
        public new void SetWaveFormat(int sampleRate, int channels)
        {
            // Localisation needs two channels
            if (channels == 2)
                this.SetWaveFormat(sampleRate);
            else
                throw new NotSupportedException("Localised sounds must be stereo");
        }

        public void SetWaveFormat(int sampleRate)
        {
            this.waveFormat = NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
        }

#endregion

#region "Angle & ITD stuff"

        private float azimuth;
        private float itd;
        public float AzimuthRads // From -1.571 to +1.571
        {
            get
            {
                return azimuth;
            }
            set
            {
                this.azimuth = value;
                this.itd = GetItd(this.ITDfactor, this.azimuth);
                Console.WriteLine("ITD: {0}", this.itd);
            }
        }

        public float AzimuthDegs // From -90 (left) to +90 (right)
        {
            get
            {
                return AzimuthRads * (180f / (float)Math.PI);
            }
            set
            {
                AzimuthRads = value * ((float)Math.PI / 180f);
            }
        }

        private static float GetItd(float ITDfactor, float azimuth)
        {
            // ITD(az) = 3 * (a/c) * sin(az)
            return ITDfactor * (float)Math.Sin((double)azimuth);
        }

#endregion

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            int sampleRate = WaveFormat.SampleRate;
            float adjust = this.itd * sampleRate;
            for (int n = 0; n < sampleCount; n = n + this.WaveFormat.Channels)
            {
                buffer[n + offset] = (float)(Amplitude * Math.Sin((2 * Math.PI * sample * Frequency) / sampleRate));
                buffer[n + offset + 1] = (float)(Amplitude * Math.Sin((2 * Math.PI * (sample-adjust) * Frequency) / sampleRate));
                sample++;
                if (sample >= sampleRate) sample = 0;
            }
            return sampleCount;
        }
    }
}
