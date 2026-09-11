using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Inkognito.Core
{
    public sealed class Board
    {
        public IReadOnlyList<Cell> Cells { get; }
        public IReadOnlyList<Edge> Edges { get; }

        public IReadOnlyList<Pawn> Pawns { get; set; }

        public Pawn AmbassadorPawn { get; set; }

        private Board(List<Cell> cells, List<Edge> edges, List<Pawn> pawns, Pawn ambassadorPawn)
        {
            Cells = cells.AsReadOnly();
            Edges = edges.AsReadOnly();
            Pawns = pawns.AsReadOnly();
            AmbassadorPawn = ambassadorPawn;
        }

        public IReadOnlyList<Pawn> GetPawnsOnCell(Cell cell)
        {
            if (cell is null)
                throw new ArgumentNullException(nameof(cell));
            var pawns = new List<Pawn>();
            foreach (var pawn in Pawns)
            {
                if (pawn.Position == cell)
                    pawns.Add(pawn);
            }
            return pawns;
        }

        internal Board Clone()
        {
            var cells = Cells.ToDictionary(cell => cell, cell => new Cell(cell.Id));
            var edges = Edges.ToDictionary(edge => edge,
                edge => new Edge(edge.Id, cells[edge.From], cells[edge.To], edge.Type));
            var pawns = Pawns.ToDictionary(pawn => pawn, pawn => new Pawn(pawn.Color, pawn.Disguise, cells[pawn.Position]));
            var ambassadorPawn = new Pawn(AmbassadorPawn.Color, AmbassadorPawn.Disguise, cells[AmbassadorPawn.Position]);
            foreach (var cell in Cells)
                foreach (var edge in cell.Edges)
                    cells[cell].AddEdge(edges[edge]);

            return new Board(Cells.Select(cell => cells[cell]).ToList(),
                Edges.Select(edge => edges[edge]).ToList(), Pawns.Select(pawn => pawns[pawn]).ToList(), ambassadorPawn);
        }

        public static Board LoadDefault()
        {
            using var stream = typeof(Board).Assembly.GetManifestResourceStream("Inkognito.Core.board.yaml")
                ?? throw new InvalidOperationException("Risorsa board.yaml non trovata nella libreria.");
            using var reader = new StreamReader(stream);
            return FromYaml(reader.ReadToEnd());
        }

        /// <summary>Carica e valida il grafo, condividendo le istanze di celle e archi.</summary>
        public static Board FromYaml(string yaml)
        {
            if (yaml is null)
                throw new ArgumentNullException(nameof(yaml));

            var document = new YamlStream();
            try
            {
                using var reader = new StringReader(yaml);
                document.Load(reader);
            }
            catch (YamlException exception)
            {
                throw new InvalidDataException("Il YAML del tabellone non è valido.", exception);
            }

            if (document.Documents.Count != 1)
                throw new InvalidDataException("Il tabellone richiede un solo documento YAML.");
            var root = Mapping(document.Documents[0].RootNode);
            if (Integer(Field(root, "version")) != 1)
                throw new InvalidDataException("Versione del tabellone non supportata.");

            var cells = new List<Cell>();
            var cellsById = new Dictionary<int, Cell>();
            var declaredEdges = new Dictionary<int, HashSet<int>>();
            foreach (var node in Sequence(Field(root, "cells")))
            {
                var data = Mapping(node);
                int id = Integer(Field(data, "id"));
                if (cellsById.ContainsKey(id))
                    throw new InvalidDataException($"Id cella duplicato: {id}.");
                var cell = new Cell(id);
                cells.Add(cell);
                cellsById.Add(id, cell);
                var references = new HashSet<int>();
                foreach (var reference in Sequence(Field(data, "edges")))
                {
                    int edgeId = Integer(reference);
                    if (!references.Add(edgeId))
                        throw new InvalidDataException($"La cella {id} ripete l'arco {edgeId}.");
                }
                declaredEdges.Add(id, references);
            }

            if (cells.Count == 0)
                throw new InvalidDataException("Il tabellone deve contenere almeno una cella.");

            var edges = new List<Edge>();
            var edgeIds = new HashSet<int>();
            foreach (var node in Sequence(Field(root, "edges")))
            {
                var data = Mapping(node);
                int id = Integer(Field(data, "id"));
                if (!edgeIds.Add(id))
                    throw new InvalidDataException($"Id arco duplicato: {id}.");
                int fromId = Integer(Field(data, "from"));
                int toId = Integer(Field(data, "to"));
                if (!cellsById.TryGetValue(fromId, out var from) || !cellsById.TryGetValue(toId, out var to))
                    throw new InvalidDataException($"L'arco {id} fa riferimento a una cella inesistente.");
                string? typeName = (Field(data, "type") as YamlScalarNode)?.Value;
                EdgeType type = typeName switch
                {
                    "LAND" => EdgeType.LAND,
                    "WATER" => EdgeType.WATER,
                    _ => throw new InvalidDataException($"Tipo non valido per l'arco {id}: usare LAND o WATER.")
                };
                var edge = new Edge(id, from, to, type);
                edges.Add(edge);
                from.AddEdge(edge);
                if (!ReferenceEquals(from, to))
                    to.AddEdge(edge);
            }

            foreach (var cell in cells)
            {
                if (!declaredEdges[cell.Id].SetEquals(cell.Edges.Select(edge => edge.Id)))
                    throw new InvalidDataException($"Gli archi dichiarati dalla cella {cell.Id} non corrispondono agli estremi degli archi.");
            }

            return new Board(cells, edges, new List<Pawn>(), null!);
        }

        private static YamlMappingNode Mapping(YamlNode node) => node as YamlMappingNode
            ?? throw new InvalidDataException("Attesa una mappa YAML.");

        private static YamlSequenceNode Sequence(YamlNode node) => node as YamlSequenceNode
            ?? throw new InvalidDataException("Attesa una lista YAML.");

        private static YamlNode Field(YamlMappingNode node, string name) =>
            node.Children.TryGetValue(new YamlScalarNode(name), out var value) ? value
                : throw new InvalidDataException($"Campo obbligatorio mancante: {name}.");

        private static int Integer(YamlNode node)
        {
            if (node is YamlScalarNode scalar &&
                int.TryParse(scalar.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                return value;
            throw new InvalidDataException("Atteso un identificativo o una versione intera.");
        }
    }
}
