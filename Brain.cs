using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class Brain
    {
        public MovePlanner Planner { get; set; } = null!;
        public Brain()
        {
            Planner = new MovePlanner();
        }

        public IReadOnlyList<Plan> PlanMoves(GameState gameState, IEnumerable<MoveType> moveTypes)
        {
            return Planner.PlanMoves(gameState.Board, moveTypes, gameState.CurrentPlayer);
        }
    }
}
