using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities;
using Utilities.Extensions;

namespace DSPLib.Filtering
{
    public enum FilterPassingTypes : int
    {
        LOW_PASS,
        HIGH_PASS,
        BAND_STOP,
        BAND_PASS
    }

    public class FIRFilter
    {
        bool _coreInitializationRequired = true;

        double _fixedLPAndHPCutoffFreq;
        double _fixedLowCutoffFreq;
        double _fixedHighCutoffFreq;

        public List<double> Core { get; private set; }

        #region ##### PARAMETRS #####

        int _CoreSize;
        WindowTypes _CoreWindow;
        double _SampleRate;
        FilterPassingTypes _PassingType;
        double _CutoffFreq;
        double _LowCutoffFreq;
        double _HighCutoffFreq;

        public int CoreSize
        {
            get { return _CoreSize; }
            set
            {
                _coreInitializationRequired = true;
                _CoreSize = value;
            }
        }

        public WindowTypes CoreWindow
        {
            get { return _CoreWindow; }
            set
            {
                _coreInitializationRequired = true;
                _CoreWindow = value;
            }
        }

        public double SampleRate
        {
            get { return _SampleRate; }
            set
            {
                _coreInitializationRequired = true;
                _SampleRate = value;
            }
        }

        public FilterPassingTypes PassingType
        {
            get { return _PassingType; }
            set
            {
                _coreInitializationRequired = true;
                _PassingType = value;
            }
        }

        public double CutoffFreq
        {
            get { return _CutoffFreq; }
            set
            {
                _coreInitializationRequired = true;
                _CutoffFreq = value;
            }
        }

        public double LowCutoffFreq
        {
            get { return _LowCutoffFreq; }
            set
            {
                _coreInitializationRequired = true;
                _LowCutoffFreq = value;
            }
        }

        public double HighCutoffFreq
        {
            get { return _HighCutoffFreq; }
            set
            {
                _coreInitializationRequired = true;
                _HighCutoffFreq = value;
            }
        }

        #endregion

        #region ##### CTOR #####

        public FIRFilter()
        {

        }

        #endregion

        #region ##### CORE INIT #####

        void initializeCoreIfRequired()
        {
            if (_coreInitializationRequired)
            {
                checkAndCalcParams();
                initializeCore();
                _coreInitializationRequired = false;
            }
        }
        void checkAndCalcParams()
        {
            double cutoffFreqCoef = (CoreSize / 253.0) * (10.0 / SampleRate) * 25.3333333;
            _fixedLPAndHPCutoffFreq = cutoffFreqCoef * CutoffFreq;
            _fixedLowCutoffFreq = cutoffFreqCoef * LowCutoffFreq;
            _fixedHighCutoffFreq = cutoffFreqCoef * HighCutoffFreq;
        }
        void initializeCore()
        {
            ThrowUtils.ThrowIf_True(_CoreSize % 2 != 1, "_CoreSize % 2 != 1");

            switch (PassingType)
            {
                case FilterPassingTypes.LOW_PASS:
                    Core = genLowPassCore();
                    break;
                case FilterPassingTypes.HIGH_PASS:
                    Core = genHighPassCore();
                    break;
                case FilterPassingTypes.BAND_STOP:
                    Core = genBandStopCore();
                    break;
                case FilterPassingTypes.BAND_PASS:
                    Core = genBandPassCore();
                    break;
                default:
                    throw new FilterException(FilterException.Errors.PARAMETER_NOT_SUPPORTED_BY_FILTER,
                        PassingType.ToString());
            }
        }

        List<double> genLowPassCore()
        {
            return genLowPassCore(_fixedLPAndHPCutoffFreq);
        }
        List<double> genLowPassCore(double cutoffFreq)
        {
            List<double> core = genSinc(cutoffFreq);
            Windows.ApplyToSamples(core, CoreWindow, out double energyLoss);
            core.NormalizeBySumSelf();

            return core;
        }
        List<double> genSinc(double phaseCoeff)
        {
            List<double> sincSamples = new List<double>();
            for (int i = 0; i <= CoreSize / 2; i++)
            {
                double phase = i / (double)CoreSize;
                sincSamples.Add(sinc(2 * Math.PI * phaseCoeff, phase));
            }
            List<double> leftHalf = new List<double>(sincSamples);
            leftHalf.RemoveAt(0);
            leftHalf.Reverse();
            sincSamples.InsertRange(0, leftHalf);

            return sincSamples.ReplaceNaN(1).ToList();
        }
        double sinc(double sinPhaseCoef, double phase)
        {
            return phase == 0 ? sinPhaseCoef : Math.Sin(sinPhaseCoef * phase) / phase;
        }

        List<double> genHighPassCore()
        {
            return genHighPassCore(_fixedLPAndHPCutoffFreq);
        }
        List<double> genHighPassCore(double cutoffFreq)
        {
            List<double> core = genLowPassCore(cutoffFreq);
            core.InvertSignEachSelf();
            core[core.Count / 2] += 1;

            return core;
        }

        List<double> genBandStopCore()
        {
            List<double> lpCore = genLowPassCore(_fixedLowCutoffFreq);
            List<double> hpCore = genHighPassCore(_fixedHighCutoffFreq);

            return lpCore
                .SumSelf(hpCore)
                .NormalizeBySumSelf()
                .ToList();
        }

        List<double> genBandPassCore()
        {
            List<double> core = genBandStopCore();
            core.NormalizeBySumSelf();
            core.InvertSignEachSelf();
            core[core.Count / 2] += 1;

            return core;
        }

        #endregion

        #region ##### BASE #####

        public List<double> Handle(IList<double> inputSamples)
        {
            initializeCoreIfRequired();

            return applyCore(inputSamples);
        }
        List<double> applyCore(IList<double> x)
        {
            var y = new List<double>();

            for (int yi = 0; yi < x.Count; yi++)
            {
                double tmp = 0;
                for (int bi = CoreSize - 1; bi >= 0; bi--)
                {
                    if (yi - bi > 0)
                    {
                        tmp += Core[bi] * x[yi - bi];
                    }
                }
                y.Add(tmp);
            }

            return y;
        }

        public int GetDistortedSamplesCount()
        {
            initializeCoreIfRequired();

            return CoreSize;
        }

        public void Reset()
        {
            if (Core != null)
            {
                Core.Clear();
            }
        }

        #endregion
    }
}