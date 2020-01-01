using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DSPLib.Fourier
{
    internal class QuickFFT
    {
        public readonly int Size;

        TComplex[] _x;
        TComplex[] _we;

        #region ##### GET / SET #####

        public void SetSamples(IEnumerable<double> samples)
        {
            _x = samples
                .Select(s => new TComplex(s, 0))
                .ToArray();
        }
        public void SetSamples(IEnumerable<Complex> samples)
        {
            _x = samples
                .Select(s => new TComplex(s.Real, s.Imaginary))
                .ToArray();
        }

        public Complex[] GetSpectrum()
        {
            Complex[] result = new Complex[Size / 2];
            for (int i = 0; i < Size / 2; i++)
            {
                TComplex tmp = _x[i];
                result[i] = new Complex(tmp.Re, tmp.Im);
            }
            return result;
        }

        #endregion

        #region ##### TComplex op #####

        TComplex ksum(TComplex a, TComplex b)
        {
            TComplex res;
            res.Re = a.Re + b.Re;
            res.Im = a.Im + b.Im;
            return res;
        }

        TComplex kdiff(TComplex a, TComplex b)
        {
            TComplex res;
            res.Re = a.Re - b.Re;
            res.Im = a.Im - b.Im;
            return res;
        }

        TComplex kprod(TComplex a, TComplex b)
        {
            TComplex res;
            res.Re = a.Re * b.Re - a.Im * b.Im;
            res.Im = a.Re * b.Im + a.Im * b.Re;
            return res;
        }

        #endregion

        public QuickFFT(int samplesCount)
        {
            int i;
            Size = samplesCount;
            _x = new TComplex[Size + 1];
            _we = new TComplex[Size / 2];
            for (i = 0; i < (Size / 2); i++)  // Init look up table for sine and cosine values
            {
                _we[i].Re = Math.Cos(2 * Math.PI * i / Size);
                _we[i].Im = Math.Sin(2 * Math.PI * i / Size);
            }
        }

        public void CalcFFT()
        {
            int i;
            bitInvert(_x, Size);
            calcSubFFT(_x, Size);
            for (i = 0; i < Size; i++)
            {
                _x[i].Im = _x[i].Im / Size * 2.0;
                _x[i].Re = _x[i].Re / Size * 2.0;
            }
            _x[0].Im = _x[0].Im / 2.0;
            _x[0].Re = _x[0].Re / 2.0;
        }

        void bitInvert(TComplex[] a, int n)
        {  // invert bits for each index. n is number of samples and a the array of the samples
            int i, mv = n / 2;
            int k, rev = 0;
            TComplex b;
            for (i = 1; i < n; i++) // run tru all the indexes from 1 to n
            {
                k = i;
                mv = n / 2;
                rev = 0;
                while (k > 0) // invert the actual index
                {
                    if ((k % 2) > 0)
                        rev = rev + mv;
                    k = k / 2;
                    mv = mv / 2;
                }

                {  // switch the actual sample and the bitinverted one
                    if (i < rev)
                    {
                        b = a[rev];
                        a[rev] = a[i];
                        a[i] = b;
                    }
                }
            }
        }

        void calcSubFFT(TComplex[] a, int n)
        {
            int i, k, m;
            TComplex w;
            TComplex v;
            TComplex h;
            k = 1;
            while (k <= n / 2)
            {
                m = 0;
                while (m <= (n - 2 * k))
                {
                    for (i = m; i < m + k; i++)
                    {
                        // sine and cosine values from look up table
                        w.Re = _we[((i - m) * Size / k / 2)].Re;
                        w.Im = _we[((i - m) * Size / k / 2)].Im;
                        // classic calculation of sine and cosine values
                        //w.real = Math.Cos( Math.PI * (double)(i-m) / (double)(k));
                        //w.imag = Math.Sin( Math.PI * (double)(i-m) / (double)(k));
                        h = kprod(a[i + k], w);
                        v = a[i];
                        a[i] = ksum(a[i], h);
                        a[i + k] = kdiff(v, h);
                    }
                    m = m + 2 * k;
                }
                k = k * 2;
            }
        }
    }
}