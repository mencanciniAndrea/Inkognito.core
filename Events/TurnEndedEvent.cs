namespace Inkognito.Core.Events
{
    public sealed class TurnEndedEvent : IGameEvent
    {
        public Player Player { get; }
        public int TurnNumber { get; set; }

        public TurnEndedEvent(Player player, int turnNumber)
        {
            Player = player;
            TurnNumber = turnNumber;
        }

        public override string ToString()
        {
            return $"TurnEndedEvent: Turn #{TurnNumber} - {Player.Name}'s turn ends.";
        }
    }
}
