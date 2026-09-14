using System;

namespace Inkognito.Core
{
    public sealed class Edge
    {
        public int Id { get; }
        public Cell From { get; }
        public Cell To { get; }
        public EdgeType Type { get; }

        internal Edge(int id, Cell from, Cell to, EdgeType type)
        {
            Id = id;
            From = from;
            To = to;
            Type = type;
        }

        public Cell Travel(Cell from)
        {
            if (from == From) return To;
            if (from == To) return From;
            throw new ArgumentException("The provided cell is not connected by this edge.", nameof(from));

        }

        public override string ToString()
        {
            return $"{From}-{Type}-{To} ({Id})";
        }
    }
}
