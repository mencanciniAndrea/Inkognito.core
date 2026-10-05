using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Events
{
    public class GameEndedEvent : IGameEvent
    {
        public List<Player> Winners { get; }

        public GameEndedEvent(List<Player> winners)
        {
            Winners = winners;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append($"GameEndedEvent: The game has ended. Winners: ");
            foreach(var winner in Winners)
            {
                sb.Append($"{winner.Name} ({winner.Color}) e ");
            }
            sb.Length -= 3; // Remove the last " e "
            return  sb.ToString();
        }
    }
}