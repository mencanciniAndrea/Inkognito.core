using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class Move
    {
        public Pawn Pawn { get;  set; }
        public Cell To { get; set; }

        public MoveType MoveTypeConsumed { get; set; }

        public Cell? Jumping { get; set; } = null;

        public Edge? Via { get; set; } = null;

        public Move(Pawn pawn, Cell to, MoveType moveType, Cell? jumping = null, Edge? via = null)
        {
            Pawn = pawn;
            To = to;
            MoveTypeConsumed = moveType;
            Jumping = jumping;
            Via = via;
        }

        public override string ToString()
        {
            if (Jumping == null) return $"Move {Pawn} to {To} ({MoveTypeConsumed})";
            else return $"Move {Pawn} to {To} jumping {Jumping} via {Via} ({MoveTypeConsumed})";
        }

    }
}
