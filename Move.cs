using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class Move
    {
        public Pawn Pawn { get;  set; }
        public Cell To { get; set; }

        public override string ToString()
        {
            return $"Move {Pawn} to {To}";
        }

    }
}
