﻿using System;
using System.Collections.Generic;
using System.Linq;
using Commons;

namespace ChemistryLibrary
{
    public class Molecule
    {
        public IEnumerable<Atom> Atoms => MoleculeStructure.Vertices.Values.Select(v => (Atom) v.Object);
        public Graph MoleculeStructure { get; } = new Graph();
        public UnitValue Charge => Atoms.Sum(atom => atom.FormalCharge, Unit.ElementaryCharge);
        public bool IsPositioned { get; private set; }

        public uint AddAtom(Atom atom, uint existingAtomId = uint.MaxValue, BondMultiplicity bondMultiplicity = BondMultiplicity.Single)
        {
            // If no atoms in the molecule yet, just add the atom
            if (!MoleculeStructure.Vertices.Any())
            {
                var firstVertex = new Vertex(MoleculeStructure.GetUnusedVertexId())
                {
                    Object = atom
                };
                MoleculeStructure.AddVertex(firstVertex);
                return firstVertex.Id;
            }

            if(!MoleculeStructure.Vertices.ContainsKey(existingAtomId))
                throw new KeyNotFoundException("Adding atom to molecule failed, because the reference to an existing atom was not found");
            var existingAtom = (Atom)MoleculeStructure.Vertices[existingAtomId].Object;
            var vertex = new Vertex(MoleculeStructure.GetUnusedVertexId())
            {
                Object = atom
            };
            MoleculeStructure.AddVertex(vertex);

            var bonds = AtomConnector.CreateBonds(atom, existingAtom, bondMultiplicity);
            foreach (var bond in bonds)
            {
                var edge = new Edge(MoleculeStructure.GetUnusedEdgeId(), existingAtomId, vertex.Id)
                {
                    Object = bond
                };
                MoleculeStructure.AddEdge(edge);
            }
            return vertex.Id;
        }

        public MoleculeReference AddMolecule(MoleculeReference moleculeToBeAdded,
            uint firstAtomId,
            uint connectionAtomId,
            BondMultiplicity bondMultiplicity = BondMultiplicity.Single)
        {
            MoleculeReference temp;
            return AddMolecule(moleculeToBeAdded, firstAtomId, connectionAtomId, out temp, bondMultiplicity);
        }
        public MoleculeReference AddMolecule(MoleculeReference moleculeToBeAdded, 
            uint firstAtomId,
            uint connectionAtomId, 
            out MoleculeReference convertedInputMoleculeReference,
            BondMultiplicity bondMultiplicity = BondMultiplicity.Single)
        {
            var mergeInfo = MoleculeStructure.AddGraph(moleculeToBeAdded.Molecule.MoleculeStructure);
            var vertex1 = connectionAtomId;
            var vertex2 = mergeInfo.VertexIdMap[moleculeToBeAdded.FirstAtomId];
            var atom1 = (Atom)MoleculeStructure.Vertices[vertex1].Object;
            var atom2 = (Atom)MoleculeStructure.Vertices[vertex2].Object;
            var bonds = AtomConnector.CreateBonds(atom1, atom2, bondMultiplicity);
            foreach (var bond in bonds)
            {
                var edge = MoleculeStructure.ConnectVertices(vertex1, vertex2);
                edge.Object = bond;
            }
            convertedInputMoleculeReference = new MoleculeReference(this, 
                mergeInfo.VertexIdMap[moleculeToBeAdded.FirstAtomId],
                mergeInfo.VertexIdMap[moleculeToBeAdded.LastAtomId]);
            return new MoleculeReference(this, firstAtomId, mergeInfo.VertexIdMap[moleculeToBeAdded.LastAtomId]);
        }

        public void UpdateBonds()
        {
            throw new NotImplementedException();
        }

        public void PositionAtoms(uint firstAtomId = uint.MaxValue, uint lastAtomId = uint.MaxValue)
        {
            MoleculePositioner.PositionAtoms(this, firstAtomId, lastAtomId);
            IsPositioned = true;
        }

        public Atom GetAtom(uint atomId)
        {
            return (Atom) MoleculeStructure.Vertices[atomId].Object;
        }

        public void ConnectAtoms(uint atomId1, uint atomId2, BondMultiplicity bondMultiplicity = BondMultiplicity.Single)
        {
            var atom1 = GetAtom(atomId1);
            var atom2 = GetAtom(atomId2);
            var bonds = AtomConnector.CreateBonds(atom1, atom2, bondMultiplicity);
            foreach (var bond in bonds)
            {
                var edge = MoleculeStructure.ConnectVertices(atomId1, atomId2);
                edge.Object = bond;
            }
        }
    }
}
