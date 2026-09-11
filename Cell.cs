using System.Collections.Generic;

namespace Inkognito.Core
{
    public sealed class Cell
    {
        private readonly List<Edge> edges = new List<Edge>();

        public int Id { get; }
        public IReadOnlyList<Edge> Edges { get; }

        internal Cell(int id)
        {
            Id = id;
            Edges = edges.AsReadOnly();
        }

        internal void AddEdge(Edge edge) => edges.Add(edge);

        public override string ToString()
        {
            return $"{Id}";
        }
    }
}
