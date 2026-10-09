using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Events
{
    public class MaxTurnsReachedEvent : IGameEvent
    {
        public int MaxTurns { get; private set; }
        public MaxTurnsReachedEvent(int maxTurns)
        {
            MaxTurns = maxTurns;
        }
        public override string ToString()
        {
            return $"MAX_TURNS ({MaxTurns}) Reached";
        }
    }
}
