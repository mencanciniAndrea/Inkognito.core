using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Events
{
    public class PawnMovedEvent : IGameEvent
    {
        public Player WhoMoves { get; }
        public Pawn PawnMoved { get; }
        public Cell From { get; }
        public Cell To { get; }
        public MoveType MoveType { get; }

        public PawnMovedEvent(Player whoMoves, Pawn pawnMoved, Cell from, Cell to, MoveType moveType)
        {
            WhoMoves = whoMoves;
            PawnMoved = pawnMoved;
            From = from;
            To = to;
            MoveType = moveType;
        }

        public override string ToString()
        {
            return $"{WhoMoves.Name} - {WhoMoves.Color} muove {PawnMoved.Disguise} dalla cella {From.Id} alla cella {To.Id} (usando una mossa {MoveType})";
        }
    }
}
