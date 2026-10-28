using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Commands
{
    internal class EndTurnCommand : IGameCommand
    {
        public void Execute(GameState g)
        {
            g.InternalStateMachine.Fire(StateTrigger.EndTurn);
        }
    }
}
