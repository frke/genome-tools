﻿using System.Collections.Generic;
using System.Windows;
using ChemistryLibrary.IO;
using ChemistryLibrary.Simulation;
using Commons;
using MoleculeViewer.ViewModels;

namespace MoleculeViewer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty MoleculeViewModelProperty = DependencyProperty.Register("MoleculeViewModel",
            typeof(MoleculeViewModel), typeof(MainWindow), new PropertyMetadata(default(MoleculeViewModel)));
        public static readonly DependencyProperty SimulationViewModelProperty = DependencyProperty.Register("SimulationViewModel", 
            typeof(SimulationViewModel), typeof(MainWindow), new PropertyMetadata(default(SimulationViewModel)));

        public MainWindow()
        {
            InitializeComponent();

            //var filename = @"G:\Projects\HumanGenome\Protein-PDBs\5hhe.pdb";
            var filename = @"G:\Projects\HumanGenome\AminoseqFiles\CFTR.aminoseq";
            var peptide = PeptideLoader.Load(filename);

            var simulationRunner = new MoleculeDynamicsSimulator(peptide.Molecule, new List<CustomAtomForce>(), new MoleculeDynamicsSimulationSettings
            {
                TimeStep = 2.To(SIPrefix.Femto, Unit.Second),
                SimulationTime = 10.To(SIPrefix.Pico, Unit.Second)
            });
            MoleculeViewModel = new MoleculeViewModel(peptide.Molecule);
            SimulationViewModel = new SimulationViewModel(simulationRunner);
        }

        public SimulationViewModel SimulationViewModel
        {
            get { return (SimulationViewModel)GetValue(SimulationViewModelProperty); }
            set { SetValue(SimulationViewModelProperty, value); }
        }
        public MoleculeViewModel MoleculeViewModel
        {
            get { return (MoleculeViewModel) GetValue(MoleculeViewModelProperty); }
            set { SetValue(MoleculeViewModelProperty, value); }
        }
    }
}