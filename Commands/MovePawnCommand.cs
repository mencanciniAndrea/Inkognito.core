using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Commands
{
    public class MovePawnCommand : IGameCommand
    {
        public IReadOnlyList<Move> Moves { get; set; }

        public MovePawnCommand(IReadOnlyList<Move> moves)
        {
            Moves = moves;
        }

        public void Execute(GameState g)
        {
            g.CurrentPlayersMoves = Moves;
            g.InternalStateMachine.Fire(StateTrigger.ApplyMove);
        }
    }
}
