using System;
using System.Collections.Generic;
using System.Numerics;
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

        public Plan PlanMoves(GameState gameState, IEnumerable<MoveType> moveTypes, TurnPhase turnPhase)
        {
            var possiblePlans = Planner.GetAllPossiblePlans(gameState.Board, moveTypes, gameState.CurrentPlayer, turnPhase);

            Console.Out.WriteLine($"Numero di piani possibili: {possiblePlans.Count}");

            var plausiblePlans = new List<Plan>();
            foreach (var plan in possiblePlans)
            {
                // verificare se il piano è legale prima di valutarlo
                if (RulesEngine.IsLegalPlan(gameState.Board, plan, gameState.CurrentPlayer, turnPhase))
                {
                    Console.Out.WriteLine($"Piano disponibile: {plan}");

                    // controllare che il piano non ti faccia tornare da dove sei partito e che non ti faccia tornare su una casella già visitata
                    HashSet<int> visitedCells = new HashSet<int>();
                    plausiblePlans.Add(plan);

                    plan.Moves.ForEach(move =>
                    {
                        if (visitedCells.Contains(move.To.Id))
                        {
                            Console.Out.WriteLine($"Piano dummy. Casella già visitata: {move.To.Id}");
                            plausiblePlans.Remove(plan);
                        }
                        else
                        {
                            visitedCells.Add(move.To.Id);
                        }
                    });
                }
                else
                {
                    Console.Out.WriteLine($"Piano non legale: {plan}");
                }
            }

            // super dummy... bisogna implementare una logica di valutazione dei piani per scegliere il migliore
            return plausiblePlans[0];
        }
    }
}
