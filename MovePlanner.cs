using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class MovePlanner
    {
        
        public IReadOnlyList<Plan> PlanMoves(Board gameBoard, IEnumerable<MoveType> moveTypes, Player currentPlayer)
        {
            List<Plan> plans = new List<Plan>();

            foreach (var moveType in moveTypes)
            {
                if (moveType == MoveType.Ambassador)
                {
                    var moves = RulesEngine.GetLegalMoves(currentPlayer.Pawns[0], moveType, gameBoard);
                    Console.Out.WriteLine($"Planning moves for Ambassador:");
                    foreach (var move in moves)
                    {
                        Console.Out.WriteLine($"- Move Ambassador from {gameBoard.AmbassadorPawn.Position.Id} to {move.To.Id}");
                    }
                }else
                {
                    foreach (var pawn in currentPlayer.Pawns)
                    {
                        var moves = RulesEngine.GetLegalMoves(pawn, moveType, gameBoard);

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
