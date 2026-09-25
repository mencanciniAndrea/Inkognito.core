using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Inkognito.Core
{
    public class MovePlanner
    {
        private readonly ILogger<MovePlanner> _logger;

        public MovePlanner(ILoggerFactory factory)
        {
            
            _logger = factory.CreateLogger<MovePlanner>();
        }

        private List<List<MoveType>> GenerateMoveTypeCombinations(MoveType [] moveTypes)
        {
            List<List<MoveType>> combinations = new()
            {
                new() { moveTypes[0] },
                new() { moveTypes[1] },
                new() { moveTypes[2] },
                new() { moveTypes[0], moveTypes[1] },
                new() { moveTypes[0], moveTypes[2] },
                new() { moveTypes[1], moveTypes[0] },
                new() { moveTypes[1], moveTypes[2] },
                new() { moveTypes[2], moveTypes[0] },
                new() { moveTypes[2], moveTypes[1] },
                new() { moveTypes[0], moveTypes[1], moveTypes[2] },
                new() { moveTypes[0], moveTypes[2], moveTypes[1] },
                new() { moveTypes[1], moveTypes[0], moveTypes[2] },
                new() { moveTypes[1], moveTypes[2], moveTypes[0] },
                new() { moveTypes[2], moveTypes[0], moveTypes[1] },
                new() { moveTypes[2], moveTypes[1], moveTypes[0] }
            };
            
            return combinations;
        }

        public IReadOnlyList<Plan> GetAllPossiblePlans(Board gameBoard, IEnumerable<MoveType> moveTypes, Player currentPlayer)
        {
            // allPlans: ha come primo piano possibile il piano vuoto: sto fermo.
            List<Plan> allPlans = new()
            {
                new(gameBoard)
            };

            List<List<MoveType>> moveTypeCombinations;

            bool currentPlayerIsAmbassador = currentPlayer.Identity == Identity.A;

            if (currentPlayerIsAmbassador)
            {
                moveTypeCombinations = new()
                {
                    new() { MoveType.Ambassador },                      // muoversi di 1
                    new() { MoveType.Ambassador, MoveType.Ambassador }  // muoversi di 2
                };
            }
            else
            {
                MoveType[] moveTypeArray = new[] { moveTypes.ElementAt(0), moveTypes.ElementAt(1), moveTypes.ElementAt(2) };
                moveTypeCombinations = GenerateMoveTypeCombinations(moveTypeArray);
            }
            
            foreach (var combination in moveTypeCombinations)
            {
                var plans = ComposePlans(gameBoard, combination, currentPlayer);
                allPlans.AddRange(plans);
            }
            return allPlans;
        }

        /// <summary>
        /// Restituisce una lista di mosse legali data una gameboard, il movetype, il currentplayer e la turnphase
        /// </summary>
        /// <param name="gameBoard"></param>
        /// <param name="moveType"></param>
        /// <param name="currentPlayer"></param>
        /// <param name="turnPhase"></param>
        /// <returns></returns>
        public IReadOnlyList<Move> GenerateLegalMovesForAllPawns(Board gameBoard, MoveType moveType, Player currentPlayer)
        {
            List<Move> legalMoves = new ();
            if (moveType == MoveType.Ambassador)
            {
                var moves = RulesEngine.GetLegalMoves(gameBoard.AmbassadorPawn, moveType, gameBoard, currentPlayer);
                legalMoves.AddRange(moves);
            }
            else if (moveType == MoveType.AnotherPlayerPawn)
            {
                foreach (var pawn in gameBoard.Pawns)
                {
                    if (pawn.Color != currentPlayer.Color)
                    {
                        var moves = RulesEngine.GetLegalMoves(pawn, moveType, gameBoard, currentPlayer);
                        legalMoves.AddRange(moves);
                    }
                }
            }
            else if (moveType == MoveType.Land || moveType == MoveType.Water || moveType == MoveType.LandOrWater)
            {
                foreach (var pawn in gameBoard.Pawns)
                {
                    if (pawn.Color == currentPlayer.Color)
                    {
                        var moves = RulesEngine.GetLegalMoves(pawn, moveType, gameBoard, currentPlayer);
                        legalMoves.AddRange(moves);
                    }
                }
            }

            return legalMoves;
        }

        /// <summary>
        /// Restituisce tutti i piani possibili date le 3 mosse
        /// </summary>
        /// <param name="gameBoard"></param>
        /// <param name="moveTypes"></param>
        /// <param name="currentPlayer"></param>
        /// <param name="turnPhase"></param>
        /// <returns></returns>
        public IReadOnlyList<Plan> ComposePlans(Board gameBoard, IEnumerable<MoveType> moveTypes, Player currentPlayer)
        {
            List<Plan> candidatePlans = new();

            foreach (var moveType in moveTypes)
            {
                if(moveType == MoveType.None)
                {
                    continue;
                }
                
                if (candidatePlans.Count == 0)
                {
                    var legalMoves = GenerateLegalMovesForAllPawns(gameBoard, moveType, currentPlayer);
                    foreach (var move in legalMoves)
                    {
                        Plan plan = new(gameBoard);
                        plan.AddMove(move);
                        candidatePlans.Add(plan);
                    }
                }
                else
                {
                    List<Plan> newPlans = new List<Plan>();
                    foreach (var existingPlan in candidatePlans)
                    {
                        Board newBoard = existingPlan.resultingBoard.Clone();
                        var legalMoves = GenerateLegalMovesForAllPawns(newBoard, moveType, currentPlayer);
                        if (legalMoves.Count == 0)
                        {
                            // non toccare niente, sennò si perde la lista delle mosse legali
                            continue;
                        }
                        foreach (var move in legalMoves)
                        {
                            Plan newPlan = existingPlan.Clone();
                            newPlan.AddMove(move);
                            newPlans.Add(newPlan);
                        }
                    }
                    candidatePlans = newPlans;    // --> questo eliminava tutti i piani precedenti, quelli da una mossa o da due.
                                                    // invece dobbiamo tenerli tutti, perché non sei obbligato a usare tutte le mosse.
                                                    // 2026.09.25 questa cosa l'ho spostata fuori
                    //candidatePlans.AddRange(newPlans);
                }
            }

            // ultimo passaggio: il RulesEgine scarta i piani che non sono legali
            List<Plan> result = new ();
            foreach (var plan in candidatePlans)
            {
                if(RulesEngine.IsGameBoardStateLegal(plan.resultingBoard, currentPlayer, TurnPhase.Move))
                {
                    result.Add(plan);
                }
                else
                {
                    _logger.LogDebug($"DEBUG: Plan not legal: {plan}");
                }
            }

            return candidatePlans;
        }

        internal List<Plan> GetDismissionPlans(Pawn p, Board board, Player currentPlayer)
        {
            List<Move> allAlternatives = RulesEngine.GetLegalDismissionMoves(p, board, currentPlayer.Color);
            List<Plan> allPlans = new();

            foreach(var m in allAlternatives)
            {
                Plan currentPlan = new(board);
                currentPlan.AddMove(m);
                allPlans.Add(currentPlan);

            }
            return allPlans;
        }
    }
}
