using NUnit.Framework;
using DSPLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;

namespace DSPLib.Tests
{
    [TestFixture()]
    public class DSPFunc_Tests
    {
        [Test()]
        public void StandartDeviation_Test()
        {
            var actual = DSPFunc.StandartDeviation(new double[] { 4, 9, 11, 12, 17, 5, 8, 12, 14 });

            Assert.AreEqual(3.94, actual.Round(2));
        }

        [Test()]
        public void Upsample_ArrayLength()
        {
            test(50, 100);
            test(80, 100);
            test(100, 100);

            void test(double actualSR, double desiredSR)
            {
                var signal = Enumerable.Repeat(0D, 100);
                var upsampled = DSPFunc.Upsample(signal, actualSR, desiredSR, DSPFunc.ResampleAlgorithm.LINEAR);

                Assert.AreEqual(100 * desiredSR / actualSR, upsampled.Count());
            }
        }

        [Test()]
        public void Upsample_LinearTest()
        {
            var expectedUpsampled = Enumerable.Range(0, 100).ToDoubles();
            var signal = expectedUpsampled.GroupBy(2).Select(g => g.FirstItem());
            var upsampled = DSPFunc.Upsample(signal, 50, 100, DSPFunc.ResampleAlgorithm.LINEAR);

            Assert.AreEqual(100, upsampled.Count());
            Assert.AreEqual(expectedUpsampled, upsampled);
        }
    }
}