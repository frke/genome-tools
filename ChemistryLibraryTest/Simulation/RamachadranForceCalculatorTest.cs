﻿using System.Collections.Generic;
using System.Threading;
using ChemistryLibrary.Builders;
using ChemistryLibrary.Measurements;
using ChemistryLibrary.Objects;
using ChemistryLibrary.Simulation;
using ChemistryLibrary.Simulation.RamachadranPlotForce;
using Commons;
using NUnit.Framework;

namespace ChemistryLibraryTest.Simulation
{
    [TestFixture]
    public class RamachadranForceCalculatorTest
    {
        [Test]
        public void ForceDirectionAsExpected()
        {
            var approximatePeptide = new ApproximatePeptide(new []
            {
                new ApproximatedAminoAcid(AminoAcidName.Alanine)
                {
                    NitrogenPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, -150, -150, 0),
                    CarbonAlphaPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, 0, -150, 0),
                    CarbonPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, 0, 0, 0)
                },
                new ApproximatedAminoAcid(AminoAcidName.Alanine)
                {
                    NitrogenPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, 150, 0, 0),
                    CarbonAlphaPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, 150, 0, 150),
                    CarbonPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, 300, 0, 150)
                }
            });
            var angles = AminoAcidAngleMeasurer.MeasureAngles(approximatePeptide);
            var ramachandranPlotDistribution = new RamachandranPlotFixedDistribution(AminoAcidName.Alanine, new UnitPoint2D(Unit.Degree, -90, -20));
            var distributionSource = new RamachandranPlotDistributionFixedSource(ramachandranPlotDistribution);
            var sut = new RamachandranForceCalculator(distributionSource);
            var forces = sut.Calculate(approximatePeptide);

            var aminoAcid1Forces = forces[approximatePeptide.AminoAcids[0]];
            var aminoAcid2Forces = forces[approximatePeptide.AminoAcids[1]];

            Assert.That(aminoAcid1Forces.CarbonAlphaForce.In(Unit.Newton).Z, Is.LessThan(0));
            Assert.That(aminoAcid1Forces.CarbonForce.Magnitude().In(Unit.Newton), Is.EqualTo(0).Within(1e-6));

            Assert.That(aminoAcid2Forces.NitrogenForce.Magnitude().In(Unit.Newton), Is.EqualTo(0).Within(1e-6));
            Assert.That(aminoAcid2Forces.CarbonAlphaForce.In(Unit.Newton).Y, Is.GreaterThan(0));
        }

        [Test]
        public void DihedralAnglesConverge()
        {
            var targetPhi = -90.To(Unit.Degree);
            var targetPsi = -20.To(Unit.Degree);

            var approximatePeptide = ApproximatePeptideBuilder.FromSequence(new [] { AminoAcidName.Alanine, AminoAcidName.Alanine, AminoAcidName.Alanine });
            approximatePeptide.UpdatePositions();
            var startAngles = AminoAcidAngleMeasurer.MeasureAngles(approximatePeptide);
            var simulationSettings = new ApproximatePeptideSimulationSettings
            {
                SimulationTime = 500.To(SIPrefix.Femto, Unit.Second),
                TimeStep = 2.To(SIPrefix.Femto, Unit.Second),
                ResetAtomVelocityAfterEachTimestep = true,
                UseCompactingForce = false
            };
            var ramachandranPlotDistribution = new RamachandranPlotFixedDistribution(AminoAcidName.Alanine, new UnitPoint2D(targetPhi, targetPsi));
            var distributionSource = new RamachandranPlotDistributionFixedSource(ramachandranPlotDistribution);
            var sut = new RamachandranForceCalculator(distributionSource);
            var simulator = new ApproximatePeptideFoldingSimulator(
                approximatePeptide, 
                simulationSettings,
                new CompactingForceCalculator(),
                sut,
                new BondForceCalculator());
            var simulationEndedWaitHandle = new ManualResetEvent(false);
            simulator.SimulationCompleted += (sender, args) => simulationEndedWaitHandle.Set();
            simulator.StartSimulation();
            simulationEndedWaitHandle.WaitOne();

            var finalAngles = AminoAcidAngleMeasurer.MeasureAngles(approximatePeptide);
            var middleAminoAcid = approximatePeptide.AminoAcids[1];
            Assert.That((finalAngles[middleAminoAcid].Phi - targetPhi).Abs(), 
                Is.LessThan((startAngles[middleAminoAcid].Phi - targetPhi).Abs()));
            Assert.That((finalAngles[middleAminoAcid].Psi - targetPsi).Abs(),
                Is.LessThan((startAngles[middleAminoAcid].Psi - targetPsi).Abs()));
        }
    }
}