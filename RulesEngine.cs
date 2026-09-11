using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class RulesEngine
    {
        /// <summary>
        /// 
        /// <param name="pawn"></param>
        /// <param name="moveType"></param>
        /// <param name="gameBoard"></param>
        /// <returns></returns>
        public static IReadOnlyList<Move> GetLegalMoves(Pawn pawn, MoveType moveType, Board gameBoard)
        {
            List<Move> legalMoves = new List<Move>();

            // check: se il pedone è su una cella dove c'è già un altro pedone, non si può muovere.
            var pawnsOnCell = gameBoard.GetPawnsOnCell(pawn.Position);
            if (pawnsOnCell.Count > 1)
            {
                return legalMoves; // No legal moves if the pawn is on a cell with another pawn.
            }
            // Implement the logic to determine if the move is legal based on the player's identity, disguise, and mission.
            // This is a placeholder implementation; you will need to fill in the actual rules.
            if (pawn.Color == PlayerColor.Black)
            {
                foreach (var link in pawn.Position.Edges)
                {
                   legalMoves.Add(new Move { Pawn = pawn, To = link.Travel(pawn.Position) });
                }
            }
            else
            {
                if (moveType == MoveType.Ambassador)
                {
                    Pawn amb = gameBoard.AmbassadorPawn;
                    foreach (var link in amb.Position.Edges)
                    {

                        Cell target = link.Travel(amb.Position);

                        pawnsOnCell = gameBoard.GetPawnsOnCell(target);
                        
                        if (pawnsOnCell.Count == 1)
                        {
                            Pawn p = pawnsOnCell[0];
                            if (p.Color == pawn.Color)
                            {
                                legalMoves.Add(new Move { Pawn = amb, To = link.Travel(amb.Position) });
                            }
                        }
                        else if (pawnsOnCell.Count == 0)
                        {
                            legalMoves.Add(new Move { Pawn = amb, To = link.Travel(amb.Position) });
                        }
                        else
                        {
                            // More than one pawn on the cell, cannot move there
                            continue;
                        }
                    }
                }
                else if (moveType == MoveType.Land || moveType == MoveType.Water || moveType == MoveType.LandOrWater)
                {
                    foreach (var link in pawn.Position.Edges)
                    {
                        if (moveType == MoveType.LandOrWater || (link.Type == EdgeType.LAND && moveType == MoveType.Land) || (link.Type == EdgeType.WATER && moveType == MoveType.Water))
                        {
                            // check: se la cella è occupata da un altro giocatore o dall'ambasciatore o è vuota, allora posso andarci. Se invece è occupata da un mio pedone, non posso andarci.
                            Cell target = link.Travel(pawn.Position);

                            pawnsOnCell = gameBoard.GetPawnsOnCell(target);

                            if (pawnsOnCell.Count == 1)
                            {
                                Pawn p = pawnsOnCell[0];
                                if (p.Color != pawn.Color)
                                {
                                    legalMoves.Add(new Move { Pawn = pawn, To = target });

                                    // check: altre mosse legali sono il salto della pedina su un posto vuoto successivo, a prescindere che il prossimo sia acqua o terra
                                    foreach (var nextLink in target.Edges)
                                    {
                                        Cell nextTarget = nextLink.Travel(target);
                                        var pawnsOnNextCell = gameBoard.GetPawnsOnCell(nextTarget);
                                        if (pawnsOnNextCell.Count == 0)
                                        {
                                            legalMoves.Add(new Move { Pawn = pawn, To = nextTarget });
                                        }
                                    }
                                }
                            }
                            else if (pawnsOnCell.Count == 0)
                            {
                                legalMoves.Add(new Move { Pawn = pawn, To = target });
                            }
                            else
                            {
                                // More than one pawn on the cell, cannot move there
                                continue;
                            }
                        }
                    }
                }
            }
            return legalMoves;
        }
    }
}
