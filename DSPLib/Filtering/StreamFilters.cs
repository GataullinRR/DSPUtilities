using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;

namespace DSPLib.Filtering
{
    public static class StreamFilters
    {
        public static IEnumerable<double> MovingAverage(IEnumerable<double> samples, int windowSize)
        {
            var window = new DisplaceCollection<double>(windowSize);
            foreach (var sample in samples)
            {
                window.Add(sample);

                yield return window.Average();
            }
        }

        public static IEnumerable<double> Median(IEnumerable<double> samples, int windowSize)
        {
            var window = new DisplaceCollection<double>(windowSize);
            foreach (var sample in samples)
            {
                window.Add(sample);

                yield return DSPFunc.Quantile(window, 0.5);
            }
        }
    }
}
