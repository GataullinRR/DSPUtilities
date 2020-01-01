using DSPLib.Fourier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using static System.Math;

namespace DSPLib
{
    public static class DSPFunc
    {
        public enum ResampleAlgorithm
        {
            LINEAR
        }
        public static IEnumerable<double> Upsample(IEnumerable<double> signal, 
            double actualSampleRate, double desiredSampleRate, ResampleAlgorithm algorithm)
        {
            var ratioK = desiredSampleRate / actualSampleRate;
            if (ratioK < 1)
            {
                throw new ArgumentException("desiredSR must be >= than actualSR");
            }
            var ratio = MathUtils.RealToFraction(ratioK, 0.00001);
            ratio.Numerator--;
            ratio.Denominator--;

            var interpolated = getInterpolated();
            return getDecimated();

            ////////////////////////////

            IEnumerable<double> getInterpolated()
            {
                var prev = signal.FirstOrDefault();
                var k = 0D;
                foreach (var curr in signal.Skip(1))
                {
                    yield return prev;
                    k = (curr - prev) / (double)(ratio.Numerator + 1);
                    for (int i = 0; i < ratio.Numerator; i++)
                    {
                        yield return k * (i + 1) + prev;
                    }

                    prev = curr;
                }
                if (signal.CountNotLessOrEqual(1))
                {
                    yield return prev;
                    if (signal.CountNotLessOrEqual(2))
                    {
                        for (int i = 0; i < ratio.Numerator; i++)
                        {
                            yield return k * (i + 1) + prev;
                        }
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }

            IEnumerable<double> getDecimated()
            {
                return ratio.Denominator == 0
                    ? interpolated
                    : interpolated.GroupBy(ratio.Denominator + 1).Select(g => g.FirstItem());
            }
        }

        static public double CalcPirsonCorrelation(IList<double> x, IList<double> y)
        {
            double avgX = x.Average();
            double avgY = y.Average();
            double numerator = 0;
            double sumOf_SqrOf_X_Sub_AvgX = 0;
            double sumOf_SqrOf_Y_Sub_AvgY = 0;
            for (int i = 0; i < x.Count; i++)
            {
                numerator += (x[i] - avgX) * (y[i] - avgY);
                sumOf_SqrOf_X_Sub_AvgX += (x[i] - avgX).Pow(2);
                sumOf_SqrOf_Y_Sub_AvgY += (y[i] - avgY).Pow(2);
            }
            double denumerator = (sumOf_SqrOf_X_Sub_AvgX * sumOf_SqrOf_Y_Sub_AvgY).Root(2);

            return numerator / denumerator;
        }

        public static IEnumerable<double> Derivative(IEnumerable<double> signal)
        {
            var prev = signal.FirstOrDefault();
            foreach (var curr in signal.Skip(1))
            {
                yield return curr - prev;
                prev = curr;
            }
        }
        public static IEnumerable<FindResult<double>> FindExtremes(IEnumerable<double> signal)
        {
            var dY = Derivative(signal);
            var extremsCount = 0;
            var prevP = dY.FirstItem();
            var i = 1; // Because first point is lost after calling Derivative
            foreach (var p in dY.Skip(1))
            {
                if (prevP.Sign() != p.Sign())
                {
                    extremsCount++;
                    yield return new FindResult<double>(i, signal);
                }
                prevP = p;
                i++;
            }
        }

        public static double[] Convolution(IList<double> signal, IList<double> impulseResponse)
        {
            double[] result = new double[signal.Count + impulseResponse.Count - 1];

            int signalLength = signal.Count;
            int impulseResponseLength = impulseResponse.Count;
            for (int i = 0; i < signalLength; i++)
            {
                for (int j = 0; j < impulseResponseLength; j++)
                {
                    result[i + j] += signal[i] * impulseResponse[j];
                }
            }
            return result;
        }

        public static double StandartDeviation(IEnumerable<double> signal)
        {
            var mean = signal.Average();

            var n = 0;
            var acc = 0D;
            foreach (var x in signal)
            {
                acc += (x - mean).Pow(2);
                n++;
            }

            return (acc / n).Root(2);
        }

        public static IEnumerable<double> Tabulate(Func<double, double> f, double xFrom, double xTo, int count)
        {
            var step = (xTo - xFrom) / count;
            for (int i = 0; i < count; i++)
            {
                var x = xFrom + step * i;
                yield return f(x);
            }
        }

        //public static double Percentile(double[] sequence, double excelPercentile)
        //{
        //    Array.Sort(sequence);
        //    int N = sequence.Length;
        //    double n = (N - 1) * excelPercentile + 1;
        //    // Another method: double n = (N + 1) * excelPercentile;
        //    if (n == 1D) return sequence[0];
        //    else if (n == N) return sequence[N - 1];
        //    else
        //    {
        //        int k = (int)n;
        //        double d = n - k;
        //        return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
        //    }
        //}

        /// <summary>
        /// Quantile(ARR, 0.9) - returns value, which is bigger than 90% of ARR elements
        /// </summary>
        /// <param name="tau">Quantile selector, between 0.0 and 1.0 (inclusive).</param>
        public static double Quantile(IEnumerable<double> data, double tau)
        {
            var arr = data?.OrderBy(v => v)?.ToArray();
            if (tau < 0d || tau > 1d || arr == null || arr.Length == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            else if (tau == 0d || arr.Length == 1)
            {
                return arr[0];
            }
            else if (tau == 1d)
            {
                return arr[arr.Length - 1];
            }

            double h = (arr.Length + 1 / 3d) * tau + 1 / 3d;
            var hf = (int)h;
            return hf < 1 ? arr[0]
                : hf >= arr.Length ? arr[arr.Length - 1]
                    : arr[hf - 1] + (h - hf) * (arr[hf] - arr[hf - 1]);
        }
        public static double Quantile(IEnumerable<int> data, double tau)
        {
            return Quantile(data.Select(p => (double)p), tau);
        }

        //public static List<double> Convolution(IList<double> signal, IList<double> impulseResponse)
        //{
        //    double[] result = new double[signal.Count + impulseResponse.Count];
        //    result.Initialize();

        //    int signalLength = signal.Count;
        //    int impulseResponseLength = impulseResponse.Count;
        //    for (int i = 0; i < signalLength; i++)
        //    {
        //        for (int j = 0; j < impulseResponseLength; j++)
        //        {
        //            result[i + j] += signal[i] * impulseResponse[j];
        //        }
        //    }
        //    return result.ToList();
        //}

        //public static List<double> FastConvolution(IList<double> signal, IList<double> impulseResponse)
        //{
        //    if (signal == null || impulseResponse == null)
        //        throw new ArgumentNullException("signal == null || impulseResponse == null");
        //    else if (signal.Count == 0 || impulseResponse.Count == 0)
        //        throw new ArgumentException("signal.Count == 0 || impulseResponse.Count == 0");

        //    int sOrder = Log(signal.Count, 2).Ceiling();
        //    int irOrder = Log(impulseResponse.Count, 2).Ceiling();
        //    int maxOrder = Max(sOrder, irOrder);
        //    int sZerosCount = 2.Pow(maxOrder) - signal.Count;
        //    int irZerosCount = 2.Pow(maxOrder) - impulseResponse.Count;
        //    signal = ArrayUtils.ConcatAll(signal, ArrayUtils.CreateList(0D, sZerosCount));
        //    impulseResponse = ArrayUtils.ConcatAll(impulseResponse, ArrayUtils.CreateList(0D, irZerosCount));

        //    var sSpectrum = Fourier.FFT.DoForward(signal, 1).Raw;
        //    var irSpectrum = Fourier.FFT.DoForward(impulseResponse, 1).Raw;
        //    TComplex[] convolved = new TComplex[sSpectrum.Count];
        //    for (int i = 0; i < sSpectrum.Count; i++)
        //    {
        //        convolved[i] = sSpectrum[i] * irSpectrum[i];
        //    }
        //}
    }
}
