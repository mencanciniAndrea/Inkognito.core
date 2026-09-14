using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public interface IBrain
    {
        Plan ChoosePlan(GameState gameState, IEnumerable<MoveType> moveTypes, TurnPhase turnPhase);
    }
}
