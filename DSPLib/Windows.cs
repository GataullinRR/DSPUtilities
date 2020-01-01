using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities;
using Utilities.Extensions;
using static System.Math;

namespace DSPLib
{
    public enum WindowTypes : int
    {
        RECTANGULAR = 0,
        HAMMING,
        BLACKMAN,
        FLAT_TOP,
        HANN
    }

    public static class Windows
    {
        public static void ApplyToSamples(IList<double> values, WindowTypes window, out double energyLoss)
        {
            double wPointsSum = 0;
            for (int i = 0; i < values.Count; i++)
            {
                double phase = (i / (double)values.Count) * 2 * PI;
                double wPoint = Windows.Window(phase, window);
                values[i] *= wPoint;
                wPointsSum += wPoint;
            }
            energyLoss = values.Count / wPointsSum; 
        }

        public static List<double> GetPoints(WindowTypes window, int pointsCount)
        {
            List<double> points = new List<double>();
            for (int i = 0; i < pointsCount; i++)
            {
                double phase = (i / (double)pointsCount) * 2 * PI;
                double wPoint = Windows.Window(phase, window);
                points.Add(wPoint);
            }

            return points;
        }

        public static double Window(double phase, WindowTypes window)
        {
            switch (window)
            {
                case WindowTypes.RECTANGULAR:
                    return 1;
                case WindowTypes.HAMMING:
                    return 0.54 - 0.46 * Cos(phase);
                case WindowTypes.BLACKMAN:
                    return 0.42 - 0.5 * Cos(phase) + 0.08 * Cos(2 * phase);
                case WindowTypes.FLAT_TOP:
                    return 1 - 1.93 * Cos(phase) + 1.29 * Cos(2 * phase) - 0.388 * Cos(3 * phase) + 0.028 * Cos(4 * phase);
                case WindowTypes.HANN:
                    return 0.5 - 0.5 * Cos(phase);
                default:
                    throw new NotSupportedException("Window doesn't supported: \"{0}\"".Format(window));
            }
        }
    }
}
