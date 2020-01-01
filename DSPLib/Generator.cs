using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities;
using Utilities.Extensions;

namespace DSPLib
{
    public enum Waveforms : int
    {
        SINE,
        SQUARE,
        TRIANGLE,
        SAWTOOTH,
        RANDOM_NOISE,
        NORMAL_NOISE
    }

    public enum MergeMode : int
    {
        ADD,
        SUB,
        /// <summary>
        /// Each sample will be multiplied
        /// </summary>
        MUL,
        DIV
    }

    public class Generator
    {
        const double PIPI = 2 * Math.PI;

        static Random _random = new Random();

        readonly double _sampleRate;
        readonly List<double> _Samples = new List<double>();

        public IReadOnlyList<double> Samples => _Samples.AsReadOnly();
        public double Duration => _Samples.Count / _sampleRate;

        public Generator(double sampleRate)
        {
            _sampleRate = sampleRate;
        }

        public Generator AddOffset(double amplitudeOffset)
        {
            _Samples.AddEachSelf(amplitudeOffset);

            return this;
        }
        public Generator AddOffset(Func<Generator, double> amplitudeOffsetCalculator)
        {
            var offset = amplitudeOffsetCalculator(this);

            return AddOffset(offset);
        }

        public Generator AddSamples(IEnumerable<double> samples, double offset = 0, MergeMode mergeMode = MergeMode.ADD)
        {
            merge(samples.ToList(), (offset / _sampleRate).Round(), mergeMode);

            return this;
        }
        public Generator AddNoise(double amplitude = 1, double duration = -1, double offset = 0, MergeMode mergeMode = MergeMode.ADD)
        {
            return AddWaveform(Waveforms.RANDOM_NOISE, amplitude, duration, offset, mergeMode);
        }
        public Generator AddWaveform(Waveforms waveform, double amplitude = 1, double duration = -1, double offset = 0, 
            MergeMode mergeMode = MergeMode.ADD)
        {
            duration = duration == -1
                ? Duration
                : duration;
            int samplesCount = getSamplesCount(duration, _sampleRate);
            List<double> samples = new List<double>(samplesCount);
            for (int i = 0; i < samplesCount; i++)
            {
                samples.Add(amplitude * Waveform(waveform, 0));
            }

            merge(samples, (offset / _sampleRate).Round(), mergeMode);

            return this;
        }

        void merge(List<double> samples, int offset, MergeMode mode)
        {
            var dCount = (samples.Count + offset) - _Samples.Count;
            if (dCount > 0)
            {
                _Samples.AddRange(ArrayUtils.CreateList(0D, dCount));
            }

            Func<double, double, double> mergeFunc = null;
            switch (mode)
            {
                case MergeMode.ADD:
                    mergeFunc = (a, b) => a + b;
                    break;
                case MergeMode.SUB:
                    mergeFunc = (a, b) => a - b;
                    break;
                case MergeMode.MUL:
                    mergeFunc = (a, b) => a * b;
                    break;
                case MergeMode.DIV:
                    mergeFunc = (a, b) => a / b;
                    break;
                default:
                    throw new NotSupportedException();
            }
            for (int i = 0; i < samples.Count; i++)
            {
                _Samples[i + offset] = mergeFunc(_Samples[i + offset], samples[i]);
            }
        }

        public static List<double> Noise(double sampleRate, double amplitude = 1, double duration = 1)
        {
            return Noise(_random, sampleRate, amplitude, duration);
        }
        public static List<double> Noise(Random random, double sampleRate, double amplitude = 1, double duration = 1)
        {
            int samplesCount = getSamplesCount(duration, sampleRate);
            List<double> samples = new List<double>(samplesCount);
            for (int i = 0; i < samplesCount; i++)
            {
                samples.Add(amplitude * Waveform(random, Waveforms.RANDOM_NOISE, 0));
            }

            return samples;
        }

        public static IEnumerable<double> Tone(Waveforms waveform, double freq, double duration, double sampleRate)
        {
            int samplesCount = getSamplesCount(duration, sampleRate);
            for (int i = 0; i < samplesCount; i++)
            {
                double t = duration * (double)i / samplesCount;
                double phase = freq * PIPI * t;
                yield return Waveform(waveform, phase);
            }
        }

        public static IEnumerable<double> Sine(double freq, double duration, double sampleRate)
        {
            return Tone(Waveforms.SINE, freq, duration, sampleRate);
        }
        public static IEnumerable<double> Square(double freq, double duration, double sampleRate)
        {
            return Tone(Waveforms.SQUARE, freq, duration, sampleRate);
        }
        public static IEnumerable<double> Triangle(double freq, double duration, double sampleRate)
        {
            return Tone(Waveforms.TRIANGLE, freq, duration, sampleRate);
        }

        public static double Waveform(Waveforms waveform, double phase)
        {
            return Waveform(_random, waveform, phase);
        }
        public static double Waveform(Random randomGenerator, Waveforms waveform, double phase)
        {
            phase = phase % PIPI;
            switch (waveform)
            {
                case Waveforms.SINE:
                    return Math.Sin(phase);
                case Waveforms.SQUARE:
                    return (phase / Math.PI).Floor() % 2 == 1 ? 1 : -1;
                case Waveforms.TRIANGLE:
                    return phase < Math.PI 
                        ? 2 * (phase - Math.PI / 2) / Math.PI
                        : 1 - 2 * (phase - Math.PI) / Math.PI;
                case Waveforms.SAWTOOTH:
                    return ((phase % PIPI) - Math.PI) / Math.PI;
                case Waveforms.RANDOM_NOISE:
                    return random(randomGenerator);
                case Waveforms.NORMAL_NOISE:
                    return ((random(randomGenerator) + random(randomGenerator) + random(randomGenerator) + random(randomGenerator) + random(randomGenerator) + random(randomGenerator)) / 6);
                default:
                    throw new NotSupportedException("Waveform doesn't supported: \"{0}\"".Format(waveform));
            }
        }
        static double random(Random random)
        {
            return random.NextDouble() * 2 - 1;
        }

        static int getSamplesCount(double durationInSeconds, double sampleRate)
        {
            return (durationInSeconds * sampleRate).ToInt32();
        }
    }
}
