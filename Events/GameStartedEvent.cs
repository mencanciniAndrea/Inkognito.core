using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Events
{
    public sealed class GameStartedEvent : IGameEvent
    {
        public override string ToString()
        {
            return "GameStartedEvent: The game has started.";
        }
    }
}
