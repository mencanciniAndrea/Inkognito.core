using Microsoft.Extensions.Logging;
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

        /// <summary>
        /// Questo metodo pianifica tutte le mosse per il giocatore corrente.
        /// </summary>
        /// <param name="gameBoard">La rappresentazione dello stato attuale del gioco.</param>
        /// <param name="moveTypes">I tipi di mosse disponibili per il giocatore corrente.</param>
        /// <param name="currentPlayer">Il giocatore corrente per il quale pianificare le mosse.</param>
        /// <returns>Una lista di piani di mosse possibili. </returns>
        /// lo lasciamo per ora, ma non viene usato
        public IReadOnlyList<Plan> PlanMoves(Board gameBoard, IEnumerable<MoveType> moveTypes, Player currentPlayer, TurnPhase turnPhase)
        {
            List<Plan> plans = new List<Plan>();

            // attenzione qui... le mosse possono essere scelte indipendentemente dall'ordine, quindi in realtà, bisogna fare in modo di ciclare su tutte le combinazioni.
            foreach (var moveType in moveTypes)
            {
                if (moveType == MoveType.Ambassador)
                {
                    var moves = RulesEngine.GetLegalMoves(gameBoard.AmbassadorPawn, moveType, gameBoard, currentPlayer, turnPhase);
                    _logger.LogDebug("Planning moves for Ambassador:");
                    foreach (var move in moves)
                    {
                        _logger.LogDebug($"- Move Ambassador from {gameBoard.AmbassadorPawn.Position.Id} to {move.To.Id}");
                    }
                    // Le mosse legali vengono calcolate correttamente. 
                    // Ora bisogna: generare una nuova board con l'applicazione della mossa, farsi dare le mosse legali per la nuova board con la nuova mossa e
                } else
                {
                    foreach (var pawn in currentPlayer.Pawns)
                    {
                        var moves = RulesEngine.GetLegalMoves(pawn, moveType, gameBoard, currentPlayer, turnPhase);

                        _logger.LogDebug($"Planning moves for pawn {pawn.Color} with disguise {pawn.Disguise} using move type {moveType}:");
                        foreach (var move in moves)
                        {
                            _logger.LogDebug($"- Move {move.Pawn.Disguise} from {move.Pawn.Position.Id} to {move.To.Id} using move type {moveType}");
                        }
                    }
                }

                // Implement the logic to plan moves for each pawn based on the game state and move types.
                // This is a placeholder implementation and should be replaced with actual planning logic.
                Plan plan = new Plan();
                plans.Add(plan);
            }

            // Implement the logic to plan moves based on the pawn, game state, and move types.
            // This is a placeholder implementation and should be replaced with actual planning logic.
            return plans;
        }

        private IReadOnlyList<IReadOnlyList<MoveType>> GenerateMoveTypeCombinations(MoveType [] moveTypes)
        {
            List<IReadOnlyList<MoveType>> combinations = new List<IReadOnlyList<MoveType>>();
            List<MoveType> l1 = new List<MoveType> { moveTypes[0], moveTypes[1], moveTypes[2] };
            combinations.Add(l1);
            List<MoveType> l2 = new List<MoveType> { moveTypes[0], moveTypes[2], moveTypes[1] };
            combinations.Add(l2);
            List<MoveType> l3 = new List<MoveType> { moveTypes[1], moveTypes[0], moveTypes[2] };
            combinations.Add(l3);
            List<MoveType> l4 = new List<MoveType> { moveTypes[1], moveTypes[2], moveTypes[0] };
            combinations.Add(l4);
            List<MoveType> l5 = new List<MoveType> { moveTypes[2], moveTypes[0], moveTypes[1] };
            combinations.Add(l5);
            List<MoveType> l6 = new List<MoveType> { moveTypes[2], moveTypes[1], moveTypes[0] };
            combinations.Add(l6);
            return combinations;
        }

        public IReadOnlyList<Plan> GetAllPossiblePlans(Board gameBoard, IEnumerable<MoveType> moveTypes, Player currentPlayer, TurnPhase turnPhase)
        {
            List<Plan> allPlans = new List<Plan>();

            MoveType[] moveTypeArray = new[] { moveTypes.ElementAt(0), moveTypes.ElementAt(1), moveTypes.ElementAt(2) };

            var moveTypeCombinations = GenerateMoveTypeCombinations(moveTypeArray);

            foreach (var combination in moveTypeCombinations)
            {
                var plans = ComposePlans(gameBoard, combination, currentPlayer, turnPhase);
                allPlans.AddRange(plans);
            }
            return allPlans;
        }

        public IReadOnlyList<Move> GenerateLegalMoves(Board gameBoard, MoveType moveType, Player currentPlayer, TurnPhase turnPhase)
        {
            List<Move> legalMoves = new List<Move>();
            if (moveType == MoveType.Ambassador)
            {
                var moves = RulesEngine.GetLegalMoves(gameBoard.AmbassadorPawn, moveType, gameBoard, currentPlayer, turnPhase);
                legalMoves.AddRange(moves);
            }
            else if(moveType == MoveType.AnotherPlayerPawn)
            {
                foreach (var pawn in gameBoard.Pawns)
                {
                    if (pawn.Color != currentPlayer.Color)
                    {
                        var moves = RulesEngine.GetLegalMoves(pawn, moveType, gameBoard, currentPlayer, turnPhase);
                        legalMoves.AddRange(moves);
                    }
                }
            }
            else if(moveType == MoveType.Land || moveType == MoveType.Water || moveType == MoveType.LandOrWater)
            {
                foreach (var pawn in gameBoard.Pawns)
                {
                    if (pawn.Color == currentPlayer.Color)
                    {
                        var moves = RulesEngine.GetLegalMoves(pawn, moveType, gameBoard, currentPlayer, turnPhase);
                        legalMoves.AddRange(moves);
                    }
                }
            }
            return legalMoves;
        }

        public IReadOnlyList<Plan> ComposePlans(Board gameBoard, IEnumerable<MoveType> moveTypes, Player currentPlayer, TurnPhase turnPhase)
        {
            List<Plan> candidatePlans = new List<Plan>();

            foreach (var moveType in moveTypes)
            {
                if(moveType == MoveType.None)
                {
                    continue;
                }
                
                if (candidatePlans.Count == 0)
                {
                    var legalMoves = GenerateLegalMoves(gameBoard, moveType, currentPlayer, turnPhase);
                    foreach (var move in legalMoves)
                    {
                        Plan plan = new Plan ();
                        plan.Moves.Add(move);
                        plan.resultingBoard = gameBoard.Clone();
                        plan.resultingBoard.ApplyMove(move);
                        candidatePlans.Add(plan);
                    }
                }
                else
                {
                    List<Plan> newPlans = new List<Plan>();
                    foreach (var existingPlan in candidatePlans)
                    {
                        Board newBoard = existingPlan.resultingBoard.Clone();
                        var legalMoves = GenerateLegalMoves(newBoard, moveType, currentPlayer, turnPhase);
                        if (legalMoves.Count == 0)
                        {
                            // non toccare niente, sennò si perde la lista delle mosse legali
                            continue;
                        }
                        foreach (var move in legalMoves)
                        {
                            Plan newPlan = new Plan();
                            newPlan.Moves.AddRange(existingPlan.Moves);
                            newPlan.Moves.Add(move);
                            newPlan.resultingBoard = existingPlan.resultingBoard.Clone();
                            newPlan.resultingBoard.ApplyMove(move);
                            newPlans.Add(newPlan);
                        }
                    }
                    candidatePlans = newPlans;
                }
                
            }


            // ultimo passaggio: il RulesEgine scarta i piani che non sono legali
            List<Plan> result = new List<Plan>();
            foreach (var plan in candidatePlans)
            {
                if(RulesEngine.IsGameBoardStateLegal(plan.resultingBoard, currentPlayer, turnPhase))
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
    }
}
