using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Decisions
{
    public class MoveDecision : PlayerDecision
    {
        public IReadOnlyList<Move> Moves { get; }
        public MoveDecision(IReadOnlyList<Move> moves, bool needsInput) : base(needsInput)
        {
            Moves = moves;
        }
    }
}
