using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Inkognito.Core
{
    public class LazyBrain : IBrain
    {
        public MovePlanner Planner { get; set; } = null!;
        public LazyBrain(ILoggerFactory factory)
        {
            Planner = new MovePlanner(factory);
        }

        public Plan ChoosePlan(GameState gameState, IEnumerable<MoveType> moveTypes, TurnPhase turnPhase)
        {
            var possiblePlans = Planner.GetAllPossiblePlans(gameState.Board, moveTypes, gameState.CurrentPlayer, turnPhase);

            Console.Out.WriteLine($"Numero di piani possibili: {possiblePlans.Count}");

            var plausiblePlans = new List<Plan>();
            foreach (var plan in possiblePlans)
            {
                // verificare se il piano è legale prima di valutarlo
                if (RulesEngine.IsGameBoardStateLegal(gameState.Board, gameState.CurrentPlayer, turnPhase))
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

            return plausiblePlans[0];
        }

        public IReadOnlyList<Pawn> SortQuerablePawnList(List<Pawn> originalPawnList, PlayerMemory? memory)
        {
            // il lazy brain non fa niente. Come viene viene.
            return originalPawnList;
        }

        public RequestType WhatToRequestTo(PlayerColor pColor, PlayerMemory? memory)
        {
            // se non conosci l'identità di questo signore, chiedigliela
            var (identity, disguise, mission) = memory.GetKnownPlayerDetails(pColor);
            if (identity == Identity.DON_T_KNOW) return RequestType.IDENTITY;
            else if (disguise == Disguise.DON_T_KNOW) return RequestType.DISGUISE;
            return RequestType.IDENTITY;
        }
    }
}
