using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Events
{
    public class InfoRequestInputNeededEvent : IGameEvent
    {
        public readonly Player Recipient;

        public IReadOnlyList<Pawn> PawnsToInterrogate { get; }

        public InfoRequestInputNeededEvent(Player rep, IReadOnlyList<Pawn> pawns)
        {
            Recipient = rep;
            PawnsToInterrogate = pawns;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append($"Player {Recipient.Name} - {Recipient.Color}, decide what you want to ask to:");
            sb.AppendJoin(", ", PawnsToInterrogate);
            return sb.ToString();
        }
    }
}
