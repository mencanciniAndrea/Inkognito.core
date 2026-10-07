using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Events
{
    public class MoveInputRequestedEvent :IGameEvent
    {
        public IReadOnlyList<MoveType> AvailableMoves { get; }

        public readonly Player Recipient;

        public MoveInputRequestedEvent(Player recipient, IReadOnlyList<MoveType> availableMoves)
        {
            Recipient = recipient;
            AvailableMoves = availableMoves;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append($"Move Input Requested to {Recipient.Name} - {Recipient.Color}; Moves: ");
            sb.AppendJoin(", ", AvailableMoves);
            return sb.ToString();
        }

    }
}
