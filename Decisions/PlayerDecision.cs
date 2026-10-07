using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Decisions
{
    public class PlayerDecision
    {
        public bool NeedsInput { get; }

        public PlayerDecision(bool needsInput)
        {
            NeedsInput = needsInput;
        }
    }
}
