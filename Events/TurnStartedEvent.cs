namespace Inkognito.Core.Events
{
    public sealed class TurnStartedEvent : IGameEvent
    {
        public Player Player { get; }
        public int TurnNumber { get; set; }

        public TurnStartedEvent(Player player, int turnNumber)
        {
            Player = player;
            TurnNumber = turnNumber;
        }

        public override string ToString()
        {
            return $"TurnStartedEvent: Turn #{TurnNumber} - {Player.Name}'s turn.";
        }
    }
}
