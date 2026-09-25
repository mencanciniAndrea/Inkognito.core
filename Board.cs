using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Inkognito.Core
{
    public sealed class Board
    {
        public IReadOnlyDictionary<int, Cell> CellsById { get; }

        public IReadOnlyDictionary<int, Edge> EdgesById { get; }

        public IReadOnlyList<Pawn> Pawns { get; set; }

        public Pawn AmbassadorPawn { get; set; }

        private Board(List<Cell> cells, List<Edge> edges, List<Pawn> pawns, Pawn ambassadorPawn)
        {
            CellsById = cells.ToDictionary(cell => cell.Id);
            EdgesById = edges.ToDictionary(edge => edge.Id);
            Pawns = pawns.AsReadOnly();
            AmbassadorPawn = ambassadorPawn;
        }

        public bool CellIsEmpty(Cell cell)
        {
            return GetPawnsOnCell(cell).Count == 0;
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

        public void ApplyMove(Move move)
        {
            if (move is null)
                throw new ArgumentNullException(nameof(move));
            var pawns = GetPawnsOnCell(CellsById[move.Pawn.Position.Id]);
            foreach (var pawn in pawns)
            {
                if (pawn.Color == move.Pawn.Color && pawn.Disguise == move.Pawn.Disguise)
                {
                    pawn.Position = CellsById[move.To.Id];
                    break;
                }
            }
        }

        public void MovePawn(Pawn pawn, Cell to)
        {
            if (pawn is null)
                throw new ArgumentNullException(nameof(pawn));
            if (to is null)
                throw new ArgumentNullException(nameof(to));
            if (!Pawns.Contains(pawn))
                throw new InvalidOperationException("Il pedone non appartiene al tabellone.");
            pawn.Position = to;
        }

        internal Board Clone()
        {
            List<Pawn> pawns = new List<Pawn>();
            var ambassadorPawn = new Pawn(PlayerColor.Black, Disguise.Ambassador, CellsById[33]);
            foreach (var pawn in Pawns)
            {
                pawns.Add(new Pawn(pawn.Color, pawn.Disguise, CellsById[pawn.Position.Id]));
                if (pawn == AmbassadorPawn)
                    ambassadorPawn = pawns.Last();
            }

            return new Board(CellsById.Values.ToList(),
                EdgesById.Values.ToList(), pawns, ambassadorPawn);
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

        public override string ToString()
        {
            StringBuilder sb = new();

            for(int c = 0; c <= (int) PlayerColor.Yellow; c++)
            {
                foreach (var p in Pawns)
                {
                    if (p.Color == (PlayerColor)c)
                    {
                        sb.Append($"{p}".PadRight(15));
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
