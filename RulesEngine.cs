using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Inkognito.Core
{
    public class RulesEngine
    {
        private static int[] cellIdsForAmbassadorToReturn = new[] { 9, 49, 44, 24, 26, 29, 55, 2, 6, 47, 39, 34, 10, 13, 37, 54, 33 };
        private static IReadOnlyList<Move> GetLegalMovesForSamePawnAsCurrentPlayer(Pawn pawn, MoveType moveType, Board gameBoard, Player currentPlayer, TurnPhase turnPhase)
        {
            List<Move> legalMoves = new List<Move>();

            foreach (var link in pawn.Position.Edges)
            {
                if (moveType == MoveType.LandOrWater || (link.Type == EdgeType.LAND && moveType == MoveType.Land) || (link.Type == EdgeType.WATER && moveType == MoveType.Water))
                {
                    // check: se la cella è occupata da un altro giocatore o dall'ambasciatore o è vuota, allora posso andarci. Se invece è occupata da un mio pedone, non posso andarci.
                    Cell target = link.Travel(pawn.Position);

                    var pawnsOnCell = gameBoard.GetPawnsOnCell(target);

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

            return legalMoves;
        }

        /// <summary>
        /// un giocatore che muove il pedone di un altro giocatore può mandarlo, in funzione della fase del turno:
        /// nella fase di movimento:
        /// - su una casella vuota
        /// - su una casella occupata da un proprio pedone
        /// 
        /// nella fase di allontanamento:
        /// - su una casella vuota e basta
        /// </summary>
        /// <param name="gameBoard"></param>
        /// <param name="currentPlayer"></param>
        /// <param name="turnPhase"></param>
        /// <returns></returns>
        private static IReadOnlyList<Move> GetLegalMovesForPlayerMovingAnotherPlayerPawn(Pawn pawn, Board gameBoard, Player currentPlayer, TurnPhase turnPhase)
        {
            var moves = new List<Move>();
            var currentCell = pawn.Position;

            switch (turnPhase)
            {
                case TurnPhase.Move:
                    // cella corrente dove sta il pedone:
                    
                    foreach(Edge e in currentCell.Edges)
                    {
                        var targetCell = e.Travel(currentCell);

                        var pawnsOnCell = gameBoard.GetPawnsOnCell(targetCell);

                        bool cellIsEmpty = pawnsOnCell.Count == 0;
                        bool cellHasOnlyOnePawn = pawnsOnCell.Count == 1;
                        var pawnOnCell = pawnsOnCell[0];
                        bool pawnIsCurrentPlayerPawn = pawnOnCell.Color == currentPlayer.Color;

                        if(cellIsEmpty || cellHasOnlyOnePawn && pawnIsCurrentPlayerPawn)
                        {
                            moves.Add(new Move { Pawn = pawn, To = targetCell });
                        }
                    }

                    break;
                case TurnPhase.Departure:
                    foreach (Edge e in currentCell.Edges)
                    {
                        var targetCell = e.Travel(currentCell);

                        var pawnsOnCell = gameBoard.GetPawnsOnCell(targetCell);

                        bool cellIsEmpty = pawnsOnCell.Count == 0;

                        if (cellIsEmpty)
                        {
                            moves.Add(new Move { Pawn = pawn, To = targetCell });
                        }
                    }
                    break;
            }

            return moves;
        }

        private static IReadOnlyList<Move> GetLegalMovesForPlayerMovingAmbassador(Board gameBoard, Player currentPlayer, TurnPhase turnPhase)
        {
            var moves = new List<Move>();

            Pawn pawn = gameBoard.AmbassadorPawn;

            switch (turnPhase)
            {
                case TurnPhase.Move:
                    // in questa fase, un player può muovere l'ambasciatore su qualunque casella libera oppure su una casella occupata da un suo pedone

                    foreach (var link in pawn.Position.Edges)
                    {
                        var targetCell = link.Travel(pawn.Position);

                        var pawnsOnCell = gameBoard.GetPawnsOnCell(targetCell);


                        bool targetCellIsEmpty = pawnsOnCell.Count == 0;
                        bool targetCellIsOccupiedByPawnOfCurrentPlayer = false;
                        bool thereIsOnlyOnePawn = pawnsOnCell.Count == 1;
                        foreach(Pawn p in pawnsOnCell)
                        {
                            if(p.Color == currentPlayer.Color)
                            {
                                targetCellIsOccupiedByPawnOfCurrentPlayer = true; 
                                break;
                            }

                        }
                        if (targetCellIsEmpty || (thereIsOnlyOnePawn && targetCellIsOccupiedByPawnOfCurrentPlayer)) 
                        {
                            moves.Add(new Move { Pawn = pawn, To = link.Travel(pawn.Position) });
                        }
                    }
                    break;
                case TurnPhase.Departure:
                    // in questa fase il giocatore deve mandare l'ambasciatore in una casella colorata libera oppure all'ambasciata
                    // è l'unico caso di mossa che "salta" tutte le altre caselle
                    foreach (int i in cellIdsForAmbassadorToReturn)
                    {
                        var targetCell = gameBoard.CellsById[i];
                        bool targetCellIsEmpty = gameBoard.CellIsEmpty(targetCell);

                        if (targetCellIsEmpty)
                        {
                            moves.Add(new Move { Pawn = pawn, To = targetCell });
                        }
                    }
                    if(moves.Count == 0)
                    {
                        // Qui abbiamo scoperto un bel problema
                        Console.Error.WriteLine("Attenzione!!! La lista delle mosse per mandare via l'ambasciatore è vuota!");
                        throw new ApplicationException("Lista per mandare via l'ambiasciatore vuota!");
                    }
                    break;
            }

            return moves;
        }
        private static IReadOnlyList<Move> GetLegalMovesForAmbassadorPlayer(Board gameBoard)
        {
            List<Move> legalMoves = new List<Move>();

            Pawn pawn = gameBoard.AmbassadorPawn;

            foreach (var link in pawn.Position.Edges)
            {
                legalMoves.Add(new Move { Pawn = pawn, To = link.Travel(pawn.Position) });
            }

            return legalMoves;
        }
        /// <summary>
        /// 
        /// <param name="pawn"></param>
        /// <param name="moveType"></param>
        /// <param name="gameBoard"></param>
        /// <returns></returns>
        public static IReadOnlyList<Move> GetLegalMoves(Pawn pawn, MoveType moveType, Board gameBoard, Player currentPlayer, TurnPhase turnPhase)
        {
            List<Move> legalMoves = new List<Move>();

            // durante la fase di movimento, non puoi più spostare un pedone che sta su una casella occupata da un altro pedone
            if (turnPhase == TurnPhase.Move)
            {
                if (gameBoard.GetPawnsOnCell(pawn.Position).Count > 1)
                {
                    return legalMoves;
                }
            }

            // player è ambasciatore
            if (currentPlayer.Disguise == Disguise.Ambassador)
            {
                legalMoves.AddRange(GetLegalMovesForAmbassadorPlayer(gameBoard));
            }

            // il player è colorato e muove un suo pedone
            else if(currentPlayer.Color == pawn.Color)
            {
                legalMoves.AddRange(GetLegalMovesForSamePawnAsCurrentPlayer(pawn, moveType, gameBoard, currentPlayer, turnPhase));
            }

            // il player è colorato, ma muove l'ambasciatore
            else if (currentPlayer.Color != PlayerColor.Black && pawn.Disguise == Disguise.Ambassador && moveType == MoveType.Ambassador)
            {
                legalMoves.AddRange(GetLegalMovesForPlayerMovingAmbassador(gameBoard, currentPlayer, turnPhase));
            }

            // se il giocatore è colorato ma muove un pedone diverso dal suo
            // Questa regola vale nella versione RuleSet 2022
            else if(currentPlayer.Color != pawn.Color && pawn.Color != PlayerColor.Black && moveType == MoveType.AnotherPlayerPawn)
            {
                legalMoves.AddRange(GetLegalMovesForPlayerMovingAnotherPlayerPawn(pawn, gameBoard, currentPlayer, turnPhase));
            }

            return legalMoves;
        }

        public static bool IsLegalPlan(Board gameBoard, Plan plan, Player currentPlayer, TurnPhase turnPhase)
        {
            // Implement the logic to check if a plan is legal
            foreach (var pawn in gameBoard.Pawns)
            {
                // verificare che non ci siano più di due pedoni sulla stessa casella
                var pawnsOnCell = gameBoard.GetPawnsOnCell(pawn.Position);
                if (pawnsOnCell.Count > 2)
                {
                    return false;
                }
            }
            return true; // Placeholder implementation
        }
    }
}
