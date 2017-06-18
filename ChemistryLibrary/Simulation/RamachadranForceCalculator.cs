﻿using System;
using System.Collections.Generic;
using System.IO;
using ChemistryLibrary.Extensions;
using ChemistryLibrary.Measurements;
using ChemistryLibrary.Objects;
using Commons;

namespace ChemistryLibrary.Simulation
{
    public class RamachadranForceCalculator
    {
        private readonly Dictionary<AminoAcidName, RamachandranPlotDistribution> ramachadranPlotDistributions;

        public RamachadranForceCalculator(string ramachadranDataDirectory)
        {
            ramachadranPlotDistributions = new Dictionary<AminoAcidName, RamachandranPlotDistribution>();
            var aminoAcidNames = (AminoAcidName[]) Enum.GetValues(typeof(AminoAcidName));
            foreach (var aminoAcidName in aminoAcidNames)
            {
                var distributionFilePath = Path.Combine(ramachadranDataDirectory, aminoAcidName.ToThreeLetterCode() + ".csv");
                var distribution = new RamachandranPlotDistribution(aminoAcidName, distributionFilePath);
                ramachadranPlotDistributions.Add(aminoAcidName, distribution);
            }
        }

        /// <summary>
        /// Use Ramachmadran plot position of an amino acid for pushing it 
        /// to keep it away from prohibited zones. 
        /// </summary>
        public Dictionary<ApproximatedAminoAcid, ApproximateAminoAcidForces> CalculateForce(ApproximatePeptide peptide)
        {
            var forceDictionary = new Dictionary<ApproximatedAminoAcid, ApproximateAminoAcidForces>();

            var aminoAcidAnglesDictionary = AminoAcidAngleMeasurer.MeasureAngles(peptide);
            for (var aminoAcidIdx = 0; aminoAcidIdx < peptide.AminoAcids.Count; aminoAcidIdx++)
            {
                var aminoAcid = peptide.AminoAcids[aminoAcidIdx];
                var previousAminoAcid = aminoAcidIdx > 0 ? peptide.AminoAcids[aminoAcidIdx - 1] : null;
                var nextAminoAcid = aminoAcidIdx + 1 < peptide.AminoAcids.Count
                    ? peptide.AminoAcids[aminoAcidIdx + 1]
                    : null;

                var plotDistribution = ramachadranPlotDistributions[aminoAcid.Name];
                var aminoAcidAngles = aminoAcidAnglesDictionary[aminoAcid];

                if (aminoAcidAngles.Omega != null)
                {
                    ApplyOmegaDeviationForce(previousAminoAcid, aminoAcid, aminoAcidAngles, forceDictionary);
                }
                if (aminoAcidAngles.Phi != null && aminoAcidAngles.Psi != null)
                {
                    var plotGradient = plotDistribution.GetGradient(aminoAcid.PhiAngle, aminoAcid.PsiAngle);
                    var phiDeviation = plotGradient.X;
                    var psiDeviation = plotGradient.Y;

                    ApplyPhiDeviationForce(previousAminoAcid, aminoAcid, forceDictionary, phiDeviation);
                    ApplyPsiDeviationForce(aminoAcid, nextAminoAcid, forceDictionary, psiDeviation);
                }
            }
            return forceDictionary;
        }

        private static void ApplyOmegaDeviationForce(ApproximatedAminoAcid lastAminoAcid, ApproximatedAminoAcid aminoAcid, AminoAcidAngles aminoAcidAngles, Dictionary<ApproximatedAminoAcid, ApproximateAminoAcidForces> forceDictionary)
        {
            var omegaDeviation = aminoAcidAngles.Omega.In(Unit.Degree) < 0
                ? -180 - aminoAcidAngles.Omega.In(Unit.Degree)
                : 180 - aminoAcidAngles.Omega.In(Unit.Degree);
            if(lastAminoAcid == null)
                return;

            if (!forceDictionary.ContainsKey(aminoAcid))
                forceDictionary.Add(aminoAcid, new ApproximateAminoAcidForces());
            var aminoAcidForces = forceDictionary[aminoAcid];

            if (!forceDictionary.ContainsKey(lastAminoAcid))
                forceDictionary.Add(lastAminoAcid, new ApproximateAminoAcidForces());
            var lastAminoAcidForces = forceDictionary[lastAminoAcid];

            var carbonNitrogenVector = lastAminoAcid.CarbonPosition
                .VectorTo(aminoAcid.NitrogenPosition)
                .In(SIPrefix.Pico, Unit.Meter);
            var previousCarbonCarbonAlphaVector = lastAminoAcid.CarbonPosition
                .VectorTo(lastAminoAcid.CarbonAlphaPosition)
                .In(SIPrefix.Pico, Unit.Meter);
            var forceMagnitude1 = 1.0.To(Unit.Newton);
            var forceDirection1 = Math.Sign(omegaDeviation) * previousCarbonCarbonAlphaVector
                                      .CrossProduct(carbonNitrogenVector)
                                      .Normalize();
            lastAminoAcidForces.CarbonAlphaForce += forceMagnitude1 * forceDirection1;

            var nitrogenCarbonAlphaVector = aminoAcid.NitrogenPosition
                .VectorTo(aminoAcid.CarbonAlphaPosition)
                .In(SIPrefix.Pico, Unit.Meter);
            var forceMagnitude2 = 1.0.To(Unit.Newton);
            var forceDirection2 = Math.Sign(omegaDeviation) * nitrogenCarbonAlphaVector
                                      .CrossProduct(carbonNitrogenVector)
                                      .Normalize();
            aminoAcidForces.CarbonAlphaForce += forceMagnitude2 * forceDirection2;
        }

        private void ApplyPhiDeviationForce(ApproximatedAminoAcid previousAminoAcid, 
            ApproximatedAminoAcid aminoAcid, 
            Dictionary<ApproximatedAminoAcid, ApproximateAminoAcidForces> forceDictionary, 
            double phiDeviation)
        {
            var nitrogenCarbonAlphaVector = aminoAcid.NitrogenPosition
                .VectorTo(aminoAcid.CarbonAlphaPosition)
                .In(SIPrefix.Pico, Unit.Meter);

            if (previousAminoAcid != null) // Should be redundant, because phi is undefined if no last amino acid
            {
                if (!forceDictionary.ContainsKey(previousAminoAcid))
                    forceDictionary.Add(previousAminoAcid, new ApproximateAminoAcidForces());
                var previousAminoAcidForces = forceDictionary[previousAminoAcid];

                var nitrogenCarbonVector = aminoAcid.NitrogenPosition
                    .VectorTo(previousAminoAcid.CarbonPosition)
                    .In(SIPrefix.Pico, Unit.Meter);
                var forceMagnitude1 = phiDeviation * 1.0.To(Unit.Newton);
                var forceDirection1 = nitrogenCarbonAlphaVector
                    .CrossProduct(nitrogenCarbonVector)
                    .Normalize();
                previousAminoAcidForces.CarbonForce += forceMagnitude1 * forceDirection1;
            }

            if (!forceDictionary.ContainsKey(aminoAcid))
                forceDictionary.Add(aminoAcid, new ApproximateAminoAcidForces());
            var aminoAcidForces = forceDictionary[aminoAcid];

            var carbonAlphaCarbonVector = aminoAcid.CarbonAlphaPosition
                .VectorTo(aminoAcid.CarbonPosition)
                .In(SIPrefix.Pico, Unit.Meter);
            var forceMagnitude2 = phiDeviation * 1.0.To(Unit.Newton);
            var forceDirection2 = nitrogenCarbonAlphaVector
                .CrossProduct(carbonAlphaCarbonVector)
                .Normalize();
            aminoAcidForces.CarbonForce += forceMagnitude2 * forceDirection2;
        }

        private void ApplyPsiDeviationForce(ApproximatedAminoAcid aminoAcid,
            ApproximatedAminoAcid nextAminoAcid,
            Dictionary<ApproximatedAminoAcid, ApproximateAminoAcidForces> forceDictionary,
            double psiDeviation)
        {
            var carbonAlphaCarbonVector = aminoAcid.CarbonAlphaPosition
                .VectorTo(aminoAcid.CarbonPosition)
                .In(SIPrefix.Pico, Unit.Meter);

            if (!forceDictionary.ContainsKey(aminoAcid))
                forceDictionary.Add(aminoAcid, new ApproximateAminoAcidForces());
            var aminoAcidForces = forceDictionary[aminoAcid];

            var carbonAlphaNitrogenVector = aminoAcid.CarbonAlphaPosition
                .VectorTo(aminoAcid.NitrogenPosition)
                .In(SIPrefix.Pico, Unit.Meter);
            var forceMagnitude1 = psiDeviation * 1.0.To(Unit.Newton);
            var forceDirection1 = carbonAlphaCarbonVector
                .CrossProduct(carbonAlphaNitrogenVector)
                .Normalize();
            aminoAcidForces.NitrogenForce += forceMagnitude1 * forceDirection1;

            if (nextAminoAcid != null) // Should be redundant, because psi is undefined if no next amino acid
            {
                if (!forceDictionary.ContainsKey(nextAminoAcid))
                    forceDictionary.Add(nextAminoAcid, new ApproximateAminoAcidForces());
                var nextAminoAcidForces = forceDictionary[nextAminoAcid];

                var carbonNitrogenVector = aminoAcid.CarbonPosition
                    .VectorTo(nextAminoAcid.NitrogenPosition)
                    .In(SIPrefix.Pico, Unit.Meter);
                var forceMagnitude2 = psiDeviation * 1.0.To(Unit.Newton);
                var forceDirection2 = carbonAlphaCarbonVector
                    .CrossProduct(carbonNitrogenVector)
                    .Normalize();
                nextAminoAcidForces.NitrogenForce += forceMagnitude2 * forceDirection2;
            }
        }
    }
}