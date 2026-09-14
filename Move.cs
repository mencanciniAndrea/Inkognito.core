using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class Move
    {
        public Pawn Pawn { get;  set; }
        public Cell To { get; set; }

        public Cell? Jumping { get; set; } = null;

        public Edge? Via { get; set; } = null;

        public override string ToString()
        {
            if (Jumping == null) return $"Move {Pawn} to {To}";
            else return $"Move {Pawn} to {To} jumping {Jumping} via {Via}";
        }

    }
}
