using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Decisions
{
    public class InformationReplyDecision : PlayerDecision
    {
        public PlayerAnswer Answer { get; }

        public InformationReplyDecision(PlayerAnswer answer, bool needsInput) : base(needsInput)
        {
            Answer = answer;
        }
    }
}
