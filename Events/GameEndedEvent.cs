using System.Collections.Generic;

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
            return $"GameEndedEvent: The game has ended. Winners: {string.Join(", ", Winners)}";
        }
    }
}