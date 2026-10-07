using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Decisions
{
    public class InformationRequestDecision : PlayerDecision
    {
        public Dictionary<Pawn, PlayerInfoRequest> InfoToAsk { get; }
        public InformationRequestDecision(Dictionary<Pawn, PlayerInfoRequest> infoToAsk, bool needsInput) : base(needsInput)
        {
            InfoToAsk = infoToAsk;
        }
    }
}
