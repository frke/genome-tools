﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ChemistryLibrary;
using Commons;
using NUnit.Framework;
using Commons.Optimization;

namespace Studies
{
    [TestFixture]
    public class AlphaHelixStrengthStudy
    {
        /// <summary>
        /// Takes peptide sequences and optimizes amino acid-amino acid bonds
        /// such that amino acids known to be in helices are marked as helices
        /// and those who don't, don't.
        /// The two neighboring amino acids and those two amino acids at distance 4 are taken into account.
        /// </summary>
        [Test]
        public void EstimateAminoAcidHelixAffinity()
        {
            var annotatedSequencesFile = @"G:\Projects\HumanGenome\fullPdbSequencesHelixMarked.txt";
            var annotatedSequences = ParseHelixSequences(annotatedSequencesFile);
            //var aminoAcidPairs = GetAminoAcidPairs(annotatedSequences);
            //var leastCommonPair = aminoAcidPairs.OrderBy(kvp => kvp.Value).First();

            Func<double[], double> costFunc = parameters => HelixSequenceCostFunc(parameters, annotatedSequences);
            var parameterSettings = GeneratePairwiseAminoAcidParameters();

            var randomizedStartValueIterations = 100;
            //Parallel.For(0L, randomizedStartValueIterations, idx =>
            for (int idx = 0; idx < randomizedStartValueIterations; idx++)
            {
                RandomizeStartValues(parameterSettings, 2);
                var optimizationResult = GradientDescentOptimizer.Optimize(costFunc, parameterSettings, double.NegativeInfinity);
                WriteOptimizationResult(@"G:\Projects\HumanGenome\helixAffinityOptimizationResults.dat", optimizationResult);
            }
        }

        [Test]
        public void OutputClassification()
        {
            var annotatedSequencesFile = @"G:\Projects\HumanGenome\fullPdbSequencesHelixMarked.txt";
            var annotatedSequences = ParseHelixSequences(annotatedSequencesFile);

            GeneratePairwiseAminoAcidParameters();
            var bestOptimizationResult = File.ReadAllLines(@"G:\Projects\HumanGenome\helixAffinityOptimizationResults.dat")
                    .Where(line => line.Split(';').Length > 2)
                    .OrderBy(line => double.Parse(line.Split(';')[0], CultureInfo.InvariantCulture))
                    .First();
            var parameters = bestOptimizationResult.Split(';').Skip(2).Select(double.Parse).ToArray();
            var directNeighborInfluence = parameters[0];
            var fourthNeighborInfluence = parameters[1];
            var bondAffinityLookup = CreateBondAffinityLookup(parameters);
            var classifiedSequences = new List<string>();
            const int WindowSize = 7;
            foreach (var annotatedSequence in annotatedSequences)
            {
                classifiedSequences.Add(GenerateAnnotatedSequence(annotatedSequence.AminoAcidCodes, annotatedSequence.IsHelixSignal));
                var classificationResult = ClassifySequence(annotatedSequence.AminoAcidCodes, bondAffinityLookup, directNeighborInfluence, fourthNeighborInfluence);
                var filteredClassification = classificationResult.MovingAverage(WindowSize).ToList();
                var classifiedSequence = GenerateAnnotatedSequence(annotatedSequence.AminoAcidCodes, filteredClassification.Select(x => x > 0).ToList());
                classifiedSequences.Add(classifiedSequence);
                classifiedSequences.Add(string.Empty);
            }
            File.WriteAllLines(@"G:\Projects\HumanGenome\classifiedHelixSequences.txt", classifiedSequences);
        }

        /// <summary>
        /// Train model to detect helixes from affinity data
        /// </summary>
        [Test]
        public void HelixDetectionFromAffinitySignal()
        {
            var annotatedSequencesFile = @"G:\Projects\HumanGenome\fullPdbSequencesHelixMarked.txt";
            var annotatedSequences = ParseHelixSequences(annotatedSequencesFile);

            var parameters = "0.1106;0.9919;0.3430;-0.7623;0.4367;0.9767;0.2249;-0.1872;-0.4937;0.6859;0.5956;0.9570;0.7493;-0.4094;0.1759;0.0552;-0.4439;-0.2630;-0.0084;-0.0330;-0.6137;0.8032;0.4869;0.2323;0.1990;-0.9081;0.3254;-0.0222;0.5678;-0.8612;0.2746;-0.3972;0.3848;-0.0041;-0.8000;-0.9773;-0.3651;-0.6519;-0.0924;-0.4000;-0.9199;-0.5798;0.7802;-0.2440;0.1588;-0.6501;-0.9806;0.1789;0.4659;-0.4533;0.1838;0.0945;-0.5493;-0.2312;-0.0415;-0.5031;-0.1840;-0.3565;-0.5748;-0.1580;0.1975;-0.3273;0.1078;-0.0411;0.3530;0.0551;0.3722;0.2613;-0.7588;0.9812;0.8230;-0.4283;-0.4397;-0.5715;-0.8356;-0.3967;0.1812;-0.7634;0.6722;0.1396;0.3678;0.3418;0.2815;0.2834;-0.5049;0.1079;-0.5224;-0.3072;-0.1836;0.3230;-0.3754;-0.0770;0.2765;-0.8504;-0.3216;0.1701;0.0830;-0.6153;-1.0000;-0.6588;-0.0010;-0.5026;-0.2770;-0.6294;-0.1488;-0.1783;-0.6307;0.7519;-0.8963;-0.3295;0.3237;0.1520;-0.1396;0.3858;-0.2411;-0.1798;-0.1718;0.4061;0.2582;-0.4542;-0.5300;-0.1715;-0.4985;0.7129;0.1070;-0.0785;-0.7031;-0.4475;0.5187;-0.6417;-0.8319;0.6978;-0.9373;0.2112;-0.2718;-0.1282;-0.7111;0.3974;-0.4363;0.4324;0.4828;-0.0412;-0.9848;0.0359;-1.0000;-0.7157;0.6524;0.8528;-0.8964;-0.1193;0.0904;0.2081;0.2406;-0.1359;0.3407;0.7633;-0.0734;0.6524;-0.4993;-0.1735;-0.0344;-0.3747;-0.1484;-0.4831;0.9864;-0.5330;0.3078;0.3048;-0.3350;-0.1173;0.6665;-0.6015;-0.3611;-0.3368;0.2906;0.3587;-0.6703;-0.6945;-0.1598;0.0346;-0.8609;-0.4516;0.0272;-0.2498;1.0000;-0.2340;-0.5437;-0.4774;0.0655;-0.4011;-0.8080;-0.3477;0.1031;0.6002;0.0629;-0.7963;0.6915;-0.1773;0.3334;-0.2751;0.1979;0.0940;-0.4829;0.2937;-0.0414;-0.2616;-0.2957;0.4042;-0.8820;-0.3575;0.2784;-1.0000"
                .Split(';').Select(double.Parse).ToArray();
            var directNeighborInfluence = parameters[0];
            var fourthNeighborInfluence = parameters[1];
            var bondAffinityLookup = CreateBondAffinityLookup(parameters);
            const int WindowSize = 7;
            foreach (var annotatedSequence in annotatedSequences)
            {
                var affinityVector = ClassifySequence(annotatedSequence.AminoAcidCodes, bondAffinityLookup, directNeighborInfluence, fourthNeighborInfluence);
                var filteredClassification = affinityVector.MovingAverage(WindowSize).ToList();
            }
        }

        private string GenerateAnnotatedSequence(IList<char> aminoAcidCodes, IList<bool> classificationResult)
        {
            var helixInProgress = false;
            var sequence = string.Empty;
            for (int codeIdx = 0; codeIdx < aminoAcidCodes.Count; codeIdx++)
            {
                var aminoAcidCode = aminoAcidCodes[codeIdx];
                var isHelix = classificationResult[codeIdx];
                if (!helixInProgress && isHelix)
                {
                    //sequence += "[";
                    helixInProgress = true;
                }
                else if (helixInProgress && !isHelix)
                {
                    //sequence += "]";
                    helixInProgress = false;
                }
                else
                {
                    //sequence += " ";
                }
                sequence += helixInProgress ? "#" : "_";
                //sequence += aminoAcidCode;
            }
            return sequence;
        }

        private readonly object fileLockObject = new object();
        private void WriteOptimizationResult(string filename, OptimizationResult optimizationResult)
        {
            lock (fileLockObject)
            {
                File.AppendAllText(filename,
                    optimizationResult.Cost + ";"
                    + optimizationResult.Iterations + ";"
                    + optimizationResult.Parameters
                        .Select(x => x.ToString("F4", CultureInfo.InvariantCulture))
                        .Aggregate((a, b) => a + ";" + b)
                    + Environment.NewLine
                );
            }
        }

        private void RandomizeStartValues(ParameterSetting[] parameterSettings, int startIdx)
        {
            for (int paramIdx = startIdx; paramIdx < parameterSettings.Length; paramIdx++)
            {
                parameterSettings[paramIdx].Start = 2*(StaticRandom.Rng.NextDouble() - 0.5);
            }
        }

        private List<string> AminoAcidCodePairs;
        private ParameterSetting[] GeneratePairwiseAminoAcidParameters()
        {
            var parameterSettings = new List<ParameterSetting>
            {
                new ParameterSetting("DirectNeighborWeight", -1e3, 1e3, 0.01, 0.1),
                new ParameterSetting("4thNeighborWeight", -1e3, 1e3, 0.01, 1.0)
            };
            var aminoAcids = (AminoAcidName[])Enum.GetValues(typeof(AminoAcidName));
            var singleLetterCodes = aminoAcids
                .Select(aa => aa.ToOneLetterCode())
                .OrderBy(x => x)
                .ToList();
            AminoAcidCodePairs = new List<string>();
            for (int idx1 = 0; idx1 < singleLetterCodes.Count; idx1++)
            {
                var code1 = singleLetterCodes[idx1];
                for (int idx2 = idx1; idx2 < singleLetterCodes.Count; idx2++)
                {
                    var code2 = singleLetterCodes[idx2];
                    var codePair = string.Empty + code1 + code2;
                    AminoAcidCodePairs.Add(codePair);
                    var parameterSetting = new ParameterSetting(
                        codePair,
                        -1,
                        1,
                        0.1,
                        1.0);
                    parameterSettings.Add(parameterSetting);
                }
            }
            return parameterSettings.ToArray();
        }

        private double HelixSequenceCostFunc(double[] parameters, List<HelixAnnotatedSequence> annotatedSequences)
        {
            var directNeighborInfluence = parameters[0];
            var fourthNeighborInfluence = parameters[1];
            var bondAffinityLookup = CreateBondAffinityLookup(parameters);
            var cost = 0.0;
            const int WindowSize = 21;
            foreach (var annotatedSequence in annotatedSequences)
            {
                var classificationResult = ClassifySequence(annotatedSequence.AminoAcidCodes, bondAffinityLookup, directNeighborInfluence, fourthNeighborInfluence);
                var filteredClassification = classificationResult.MovingAverage(WindowSize).ToList();
                var pairwiseCost = filteredClassification.PairwiseOperation(annotatedSequence.IsHelixSignal, EvaluateClassification);
                cost += pairwiseCost.Sum();
            }
            cost += directNeighborInfluence*directNeighborInfluence + fourthNeighborInfluence*fourthNeighborInfluence;
            return cost;
        }

        //private double EvaluateClassification(bool classificationValue, bool expected)
        //{
        //    return classificationValue != expected ? 1 : 0;
        //}
        private static double EvaluateClassification(double classificationValue, bool expected)
        {
            var actual = classificationValue > 0;
            if (actual == expected)
            {
                return -0.5*Math.Max(1, Math.Abs(classificationValue));
            }
            return 1+Math.Abs(classificationValue);
        }

        private static IList<double> ClassifySequence(
            IList<char> aminoAcids,
            Dictionary<string, double> bondAffinityLookup,
            double directNeighborInfluence,
            double fourthNeighborInfluence)
        {
            var totalAffinity = new List<double>();
            for (int aminoAcidIdx = 0; aminoAcidIdx < aminoAcids.Count; aminoAcidIdx++)
            {
                var aminoAcid = aminoAcids[aminoAcidIdx];
                //var neighborM4 = aminoAcidIdx < 4 ? (char) 0 : aminoAcids[aminoAcidIdx - 4];
                var neighborM1 = aminoAcidIdx < 4 ? (char) 0 : aminoAcids[aminoAcidIdx - 1];
                var neighborP1 = aminoAcidIdx + 4 >= aminoAcids.Count ? (char) 0 : aminoAcids[aminoAcidIdx + 1];
                var neighborP4 = aminoAcidIdx + 4 >= aminoAcids.Count ? (char) 0 : aminoAcids[aminoAcidIdx + 4];

                var localAffinity = 0.0;
                //if(neighborM4 != 0)
                //{
                //    var pairCode = BuildAminoAcidPair(aminoAcid, neighborM4);
                //    if(bondAffinityLookup.ContainsKey(pairCode))
                //    {
                //        localAffinity += fourthNeighborInfluence * bondAffinityLookup[pairCode];
                //    }
                //}
                if (neighborM1 != 0)
                {
                    var pairCode = BuildAminoAcidPair(aminoAcid, neighborM1);
                    if (bondAffinityLookup.ContainsKey(pairCode))
                    {
                        localAffinity += directNeighborInfluence*bondAffinityLookup[pairCode];
                    }
                }
                if (neighborP1 != 0)
                {
                    var pairCode = BuildAminoAcidPair(aminoAcid, neighborP1);
                    if (bondAffinityLookup.ContainsKey(pairCode))
                    {
                        localAffinity += directNeighborInfluence * bondAffinityLookup[pairCode];
                    }
                }
                if (neighborP4 != 0)
                {
                    var pairCode = BuildAminoAcidPair(aminoAcid, neighborP4);
                    if (bondAffinityLookup.ContainsKey(pairCode))
                    {
                        localAffinity += fourthNeighborInfluence * bondAffinityLookup[pairCode];
                    }
                }
                totalAffinity.Add(localAffinity);
            }
            return totalAffinity;
        }

        private static string BuildAminoAcidPair(char code1, char code2)
        {
            if (code1 < code2)
                return string.Empty + code1 + code2;
            return string.Empty + code2 + code1;
        }

        private Dictionary<string, double> CreateBondAffinityLookup(double[] parameters)
        {
            var lookup = new Dictionary<string, double>();
            for (int paramIdx = 2; paramIdx < parameters.Length; paramIdx++)
            {
                var parameter = parameters[paramIdx];
                lookup.Add(AminoAcidCodePairs[paramIdx-2], parameter);
            }
            return lookup;
        }

        private List<HelixAnnotatedSequence> ParseHelixSequences(string annotatedSequencesFile)
        {
            var annotatedSequences = new List<HelixAnnotatedSequence>();
            foreach (var line in File.ReadAllLines(annotatedSequencesFile))
            {
                if(line.StartsWith("#"))
                    continue;
                if(string.IsNullOrWhiteSpace(line))
                    continue;
                var aminoAcids = new List<char>();
                var annotation = new List<bool>();
                var isHelix = false;
                foreach (var c in line)
                {
                    if (c == '[')
                        isHelix = true;
                    else if (c == ']')
                        isHelix = false;
                    else
                    {
                        aminoAcids.Add(c);
                        annotation.Add(isHelix);
                    }
                }
                annotatedSequences.Add(new HelixAnnotatedSequence(annotation, aminoAcids));
            }
            return annotatedSequences;
        }
    }

    internal class HelixAnnotatedSequence
    {
        public HelixAnnotatedSequence(IList<bool> isHelixSignal, IList<char> aminoAcidCodes)
        {
            IsHelixSignal = isHelixSignal;
            AminoAcidCodes = aminoAcidCodes;
        }

        public IList<bool> IsHelixSignal { get; }
        public IList<char> AminoAcidCodes { get; }
    }
}
