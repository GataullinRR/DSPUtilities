using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using Utilities.Extensions.Debugging;
using System.Reflection;

namespace DSPLib.Filtering
{
    public class SalahovFIRFilter
    {
        public double[] Core { get; }
        public double BW { get; }

        public SalahovFIRFilter(double sampleRate, double fLow, double fHigh, int coreSize)
        {
            var pi = Math.PI;

            double Fn = fLow, Fv = fHigh;

            var K = 1;
            BW = (4.0 / coreSize) * sampleRate;
            var fcv = (Fv + BW / 2) / sampleRate;
            var fcn = (Fn - BW / 2) / sampleRate;

            var a0 = 0.42;
            var a1 = 0.5;
            var a2 = 0.08;
            var w = new double[coreSize + 1];
            for (int i = 0; i < coreSize + 1; i++)
            {
                w[i] = a0 - a1 * Math.Cos(2 * pi * i / coreSize) + a2 * Math.Cos(4 * pi * i / coreSize);
            }

            var sinh_v = new double[coreSize + 1];
            for (int i = 0; i < coreSize + 1; i++)
            {
                if (i == coreSize / 2)
                {
                    sinh_v[i] = K * 2 * pi * fcv;
                }
                else
                {
                    sinh_v[i] = K * Math.Sin(2 * pi * fcv * (i - coreSize / 2)) / (i - coreSize / 2);
                }
            }

            var hv = sinh_v.Mul(w).DivEachSelf(-pi);
            hv[coreSize / 2] += 1;

            var sinh_n = new double[coreSize + 1];
            for (int i = 0; i < coreSize + 1; i++)
            {
                if (i == coreSize / 2)
                {
                    sinh_n[i] = K * 2 * pi * fcn;
                }
                else
                {
                    sinh_n[i] = K * Math.Sin(2 * pi * fcn * (i - coreSize / 2)) / (i - coreSize / 2);
                }
            }

            var hn = sinh_n.Mul(w).DivEachSelf(pi);

            var hb = hn.Sum(hv);

            var h = hb.MulEach(-1).ToArray();
            h[coreSize / 2] += 1;
            h.DivEachSelf(h.Sum());

            Core = h;
        }

        public IEnumerable<double> Filter(IEnumerable<double> data)
        {
            var m = Core.Length;
            return DSPFunc.Convolution(data.ToArray(), Core).Skip(m / 2).SkipFromEnd(m / 2);
        }
    }
}
