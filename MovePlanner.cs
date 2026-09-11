using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class MovePlanner
    {
        
        /// <summary>
        /// Questo metodo pianifica tutte le mosse per il giocatore corrente.
        /// </summary>
        /// <param name="gameBoard">La rappresentazione dello stato attuale del gioco.</param>
        /// <param name="moveTypes">I tipi di mosse disponibili per il giocatore corrente.</param>
        /// <param name="currentPlayer">Il giocatore corrente per il quale pianificare le mosse.</param>
        /// <returns>Una lista di piani di mosse possibili. </returns>
        public IReadOnlyList<Plan> PlanMoves(Board gameBoard, IEnumerable<MoveType> moveTypes, Player currentPlayer, TurnPhase turnPhase)
        {
            List<Plan> plans = new List<Plan>();

            // attenzione qui... le mosse possono essere scelte indipendentemente dall'ordine, quindi in realtà, bisogna fare in modo di ciclare su tutte le combinazioni.
            foreach (var moveType in moveTypes)
            {
                if (moveType == MoveType.Ambassador)
                {
                    var moves = RulesEngine.GetLegalMoves(gameBoard.AmbassadorPawn, moveType, gameBoard, currentPlayer, turnPhase);
                    Console.Out.WriteLine($"Planning moves for Ambassador:");
                    foreach (var move in moves)
                    {
                        Console.Out.WriteLine($"- Move Ambassador from {gameBoard.AmbassadorPawn.Position.Id} to {move.To.Id}");
                    }
                }else
                {
                    foreach (var pawn in currentPlayer.Pawns)
                    {
                        var moves = RulesEngine.GetLegalMoves(pawn, moveType, gameBoard, currentPlayer, turnPhase);

                        Console.Out.WriteLine($"Planning moves for pawn {pawn.Color} with disguise {pawn.Disguise} using move type {moveType}:");
                        foreach (var move in moves)
                        {
                            Console.Out.WriteLine($"- Move {move.Pawn.Disguise} from {move.Pawn.Position.Id} to {move.To.Id} using move type {moveType}");
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
    }
}
