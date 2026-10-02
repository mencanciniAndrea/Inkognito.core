namespace Inkognito.Core.Events
{
    public sealed class TurnStartedEvent : IGameEvent
    {
        public Player Player { get; }

        public TurnStartedEvent(Player player)
        {
            Player = player;
        }
    }
}
