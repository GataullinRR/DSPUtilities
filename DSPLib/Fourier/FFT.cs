using DSPLib;
using FFTWSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Vectors;

namespace DSPLib.Fourier
{
    public static class FFT
    {
        public class FFTResult
        {
            public readonly List<Complex> Raw;
            public readonly List<V2> PhaseSpectrum;
            public readonly List<V2> MagnitudeSpectrum;

            public FFTResult(List<Complex> raw, List<V2> phaseSpectrum, List<V2> frequencySpectrum)
            {
                ThrowUtils.ThrowIf_NullArgument(raw, phaseSpectrum, frequencySpectrum);

                Raw = raw;
                PhaseSpectrum = phaseSpectrum;
                MagnitudeSpectrum = frequencySpectrum;
            }
        }

        public static List<int> SIZES = new List<int>();
        public static FFTResult DoForward(IEnumerable<double> inputSignal, double samplingRate)
        {
            SIZES.Add(inputSignal.Count());
            //return legacyForward(inputSignal, samplingRate);
            return fastForward(inputSignal, samplingRate);

            //var corrected = AppendZerosIfSizeIncorrect(inputSignal.ToList(), out int zerosAdded);
            //double k = ArrayUtils.ConcatAll ((corrected.Count - zerosAdded) + zerosAdded) / (double)corrected.Count;
            //corrected.MulEach(k);
            //return fftwForward(inputSignal, samplingRate);
        }
        public static FFTResult DoForward(IEnumerable<Complex> inputSignal, double samplingRate)
        {
            return fastForward(inputSignal, samplingRate);
        }

        #region ##### legacyForward #####

        static FFTResult legacyForward(IEnumerable<double> inputSignal, double samplingRate)
        {
            uint length = inputSignal.Count().ToUInt32(false);
            double[] wCoefs = DSP.Window.Coefficients(DSP.Window.Type.None, length);
            double[] wInputData = DSP.Math.Multiply(inputSignal.ToArray(), wCoefs);
            double wScaleFactor = DSP.Window.ScaleFactor.Signal(wCoefs);

            DFT dft = new DFT();
            dft.Initialize(length);

            Complex[] cSpectrum = dft.Execute(wInputData);
            double[] pSpectrum = DSP.ConvertComplex.ToPhaseDegrees(cSpectrum);
            double[] lmSpectrum = DSP.ConvertComplex.ToMagnitude(cSpectrum);
            lmSpectrum = DSP.Math.Multiply(lmSpectrum, wScaleFactor);

            double[] freqSpan = dft.FrequencySpan(samplingRate);

            List<V2> frequencySpectrum = new List<V2>();
            List<V2> phaseSpectrum = new List<V2>();
            for (int i = 0; i < lmSpectrum.Length; i++)
            {
                frequencySpectrum.Add(new V2(freqSpan[i], lmSpectrum[i]));
                phaseSpectrum.Add(new V2(freqSpan[i], pSpectrum[i]));
            }

            return new FFTResult(cSpectrum.ToList(), phaseSpectrum, frequencySpectrum);
        }

        #endregion

        #region ##### fastForward #####

        static FFTResult fastForward(IEnumerable<Complex> inputSignal, double samplingRate)
        {
            int length = inputSignal.Count();

            QuickFFT fft = new QuickFFT(length);
            fft.SetSamples(inputSignal);
            fft.CalcFFT();

            return continueFastForward(fft, samplingRate);
        }
        static FFTResult fastForward(IEnumerable<double> inputSignal, double samplingRate)
        {
            int length = inputSignal.Count();
            QuickFFT fft = new QuickFFT(length);
            fft.SetSamples(inputSignal);
            fft.CalcFFT();

            return continueFastForward(fft, samplingRate);
        }
        static FFTResult continueFastForward(QuickFFT fft, double samplingRate)
        {
            Complex[] cSpectrum = fft.GetSpectrum();
            double[] pSpectrum = DSP.ConvertComplex.ToPhaseDegrees(cSpectrum);
            double[] lmSpectrum = DSP.ConvertComplex.ToMagnitude(cSpectrum);
            double[] freqSpan = new double[cSpectrum.Length];
            double step = (samplingRate / 2) / freqSpan.Length;
            for (int i = 0; i < freqSpan.Length; i++)
            {
                freqSpan[i] = step * i;
            }

            List<V2> frequencySpectrum = new List<V2>();
            List<V2> phaseSpectrum = new List<V2>();
            for (int i = 0; i < lmSpectrum.Length; i++)
            {
                frequencySpectrum.Add(new V2(freqSpan[i], lmSpectrum[i]));
                phaseSpectrum.Add(new V2(freqSpan[i], pSpectrum[i]));
            }

            return new FFTResult(cSpectrum.ToList(), phaseSpectrum, frequencySpectrum);
        }

        #endregion

        #region ##### fftwForward #####

        static FFTResult fftwForward(IEnumerable<double> inputSignal, double samplingRate)
        {
            var inArray = inputSignal.ToArray();
            fftw_complexarray input = new fftw_complexarray(inArray);
            fftw_complexarray output = new fftw_complexarray(inArray.Length / 2 + 1);

            fftw_plan plan = fftw_plan.dft_r2c_1d(inArray.Length, input, output, fftw_flags.Estimate);
            fftw.execute(plan.Handle);

            var result = output.GetData_Complex();
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = result[i] / output.Length;
            }
            result[0] /= 2;
            result[result.Length - 1] /= 2;

            double[] freqSpan = new double[result.Length];
            double step = (samplingRate / 2) / freqSpan.Length;
            for (int i = 0; i < freqSpan.Length; i++)
            {
                freqSpan[i] = step * i;
            }

            List<V2> frequencySpectrum = new List<V2>();
            List<V2> phaseSpectrum = new List<V2>();
            for (int i = 0; i < result.Length; i++)
            {
                frequencySpectrum.Add(new V2(freqSpan[i], result[i].Magnitude));
                phaseSpectrum.Add(new V2(freqSpan[i], result[i].Phase));
            }
            return new FFTResult(result.ToList(), phaseSpectrum, frequencySpectrum);
        }

        #endregion

        public static List<double> AppendZerosIfSizeIncorrect(IList<double> inputSignal, out int zerosAdded)
        {
            inputSignal = inputSignal ?? new List<double>();
            List<double> newSignal = new List<double>(inputSignal);

            int zerosCount = 0;
            if (!isSizeCorrect(inputSignal.Count))
            {
                int i = -1;
                while (inputSignal.Count > 2.Pow(++i)) { }
                zerosCount = 2.Pow(i) - inputSignal.Count;
            }
            zerosAdded = zerosCount;

            while (zerosCount > 0)
            {
                newSignal.Add(0);
                zerosCount--;
            }
            return newSignal;
        }
        static bool isSizeCorrect(int length)
        {
            return Math.Log(length, 2).IsInteger();
        }
    }

    public struct TComplex
    {
        public static TComplex operator *(TComplex l, TComplex r)
        {
            return new TComplex(l.Re * r.Re - l.Im * r.Im, l.Re * r.Im + l.Im * r.Re);
        }

        public double Re;
        public double Im;

        public TComplex(double real, double imaginary)
        {
            Re = real;
            Im = imaginary;
        }
    }
}
