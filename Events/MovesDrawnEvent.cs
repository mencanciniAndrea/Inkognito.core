using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Events
{
    public class MovesDrawnEvent : IGameEvent
    {
        public Player CurrentPlayer { get; }
        public IReadOnlyList<MoveType> Moves { get; }

        public MovesDrawnEvent(Player currentPlayer, IReadOnlyList<MoveType> moves)
        {
            CurrentPlayer = currentPlayer;
            List<MoveType> movesList = new(moves);
            Moves = movesList;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append($"MovesDrawnEvent: CurrentPlayer={CurrentPlayer.Name} ({CurrentPlayer.Color}), Moves: ");
            sb.AppendJoin(", ", Moves);
            return sb.ToString();
        }
    }
}
