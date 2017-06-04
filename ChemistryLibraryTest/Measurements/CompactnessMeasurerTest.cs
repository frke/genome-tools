﻿using System.Collections.Generic;
using ChemistryLibrary;
using ChemistryLibrary.Measurements;
using ChemistryLibrary.Objects;
using Commons;
using NUnit.Framework;

namespace ChemistryLibraryTest.Measurements
{
    [TestFixture]
    public class CompactnessMeasurerTest
    {
        [Test]
        public void VolumeAsExpected()
        {
            var aa1 = new ApproximatedAminoAcid(AminoAcidName.Alanine);
            var aa2 = new ApproximatedAminoAcid(AminoAcidName.Alanine);
            var aa3 = new ApproximatedAminoAcid(AminoAcidName.Alanine);
            var peptide = new ApproximatePeptide(new List<ApproximatedAminoAcid> { aa1, aa2, aa3 });

            // Set positions
            aa1.NitrogenPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, 0, 0, 0);
            aa1.CarbonAlphaPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, 1, 0, 0);
            aa1.CarbonPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, 1, 1, 0);
            aa2.NitrogenPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, 0, 1, 0);
            aa2.CarbonAlphaPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, 0, 1, 1);
            aa2.CarbonPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, 1, 1, 1);
            aa3.NitrogenPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, 1, 0, 1);
            aa3.CarbonAlphaPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, 0, 0, 1);
            aa3.CarbonPosition = new UnitPoint3D(SIPrefix.Pico, Unit.Meter, 0.5, 0.5, 0.5);

            var volume = CompactnessMeasurer.Measure(peptide);
            Assert.That(volume, Is.EqualTo(1).Within(1e-6));
        }
    }
}
