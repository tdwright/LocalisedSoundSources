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
                //Console.WriteLine("ITD: {0}", this.itd);
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

        private float[] previousValues = new float[2] { 0f, 0f };
        private float[] amplitudes = new float[2] { 0f, 0f };
        private float[] frequencies = new float[2] { 100f, 100f };

        private void UpdateAtZero(float[] prev, float[] current)
        {
            for (int i = 0; i < 2; i++)
            {
                if (((prev[i] <= 0) && (current[i] > 0)) || ((prev[i] >= 0) & (current[i] < 0)) || (current[i] == 0))
                {
                    //Console.WriteLine("Channel {0} updated", i);
                    amplitudes[i] = this.Amplitude;
                    frequencies[i] = this.Frequency;
                }
            }
        }

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            int sampleRate = WaveFormat.SampleRate;
            float adjust = this.itd * sampleRate;
            float[] values = new float[2];
            for (int n = 0; n < sampleCount; n += this.WaveFormat.Channels)
            {
                // first work out if we'd cross the 0 line and update values if we do
                values[0] = (float)(amplitudes[0] * Math.Sin((2 * Math.PI * sample * frequencies[0]) / sampleRate));
                values[1] = (float)(amplitudes[1] * Math.Sin((2 * Math.PI * (sample-adjust) * frequencies[1]) / sampleRate));
                this.UpdateAtZero(this.previousValues, values);
                buffer[n + offset] = values[0];
                buffer[n + offset + 1] = values[1];
                this.previousValues = values;
                sample++;
                if (sample >= sampleRate) sample = 0;
            }
            return sampleCount;
        }
    }
}
