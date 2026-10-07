using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Events
{
    public class DismissPawnInputRequestedEvent : IGameEvent
    {
        public Player Recipient { get; }
        public Pawn PawnToDismiss { get; }

        public DismissPawnInputRequestedEvent(Player pl, Pawn pa)
        {
            Recipient = pl;
            PawnToDismiss = pa;
        }

        public override string ToString()
        {
            return $"{Recipient.Name} - {Recipient.Color}: dismiss the pawn {PawnToDismiss}";
        }
    }
}
