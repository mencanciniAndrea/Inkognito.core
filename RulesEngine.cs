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
        private static IReadOnlyList<Move> GetLegalMovesCurrentPlayerPawn(Pawn pawn, MoveType moveType, Board gameBoard)
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
                                    legalMoves.Add(new Move { Pawn = pawn, To = nextTarget, Jumping = target, Via = nextLink});
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
        private static IReadOnlyList<Move> GetLegalMovesForPlayerMovingAnotherPlayerPawn(Pawn pawn, Board gameBoard, Player currentPlayer)
        {
            var moves = new List<Move>();
            var currentCell = pawn.Position;

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

            return moves;
        }

        private static IReadOnlyList<Move> GetLegalMovesForPlayerMovingAmbassador(Board gameBoard, Player currentPlayer)
        {
            var moves = new List<Move>();

            Pawn pawn = gameBoard.AmbassadorPawn;

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
        /// Restituisce le mosse legali per il pawn dato, sulla board data, con la mossa del tipo dato, per il currentPlayer dato.
        /// Attenzione: vale solo come mosse legali in fase di movimento, non di allontanamento. Per l'allontamento
        /// utilizzare GetLegalDismissionMoves
        /// <param name="pawn"></param>
        /// <param name="moveType"></param>
        /// <param name="gameBoard"></param>
        /// <returns></returns>
        public static IReadOnlyList<Move> GetLegalMoves(Pawn pawn, MoveType moveType, Board gameBoard, Player currentPlayer)
        {
            List<Move> legalMoves = new List<Move>();

            bool currentPlayerIsAmbassador = currentPlayer.Disguise == Disguise.Ambassador;
            bool pawnIsAmbassador = pawn.Disguise == Disguise.Ambassador;

            // durante la fase di movimento, non puoi più spostare un pedone che sta su una casella occupata da un altro pedone
            if (gameBoard.GetPawnsOnCell(pawn.Position).Count > 1)
            {
                return legalMoves;
            }
            // player è ambasciatore
            if (currentPlayerIsAmbassador)
            {
                legalMoves.AddRange(GetLegalMovesForAmbassadorPlayer(gameBoard));
            }

            // il player è colorato e muove un suo pedone
            else if (currentPlayer.Color == pawn.Color)
            {
                legalMoves.AddRange(GetLegalMovesCurrentPlayerPawn(pawn, moveType, gameBoard));
            }

            // il player è colorato, ma muove l'ambasciatore
            else if (!currentPlayerIsAmbassador && pawnIsAmbassador && moveType == MoveType.Ambassador)
            {
                legalMoves.AddRange(GetLegalMovesForPlayerMovingAmbassador(gameBoard, currentPlayer));
            }

            // se il giocatore è colorato ma muove un pedone diverso dal suo
            // Questa regola vale nella versione RuleSet 2022
            else if (currentPlayer.Color != pawn.Color && !pawnIsAmbassador && moveType == MoveType.AnotherPlayerPawn)
            {
                legalMoves.AddRange(GetLegalMovesForPlayerMovingAnotherPlayerPawn(pawn, gameBoard, currentPlayer));
            }
            

            return legalMoves;
        }

        /// <summary>
        /// Ci sono 2 regole principali per stabilire se una gameboard è in uno stato legale nella fase MOVE:
        /// 1. Non ci possono essere più di 2 pedine su una stessa casella
        /// 2. il giocatore corrente non può incontrare più di 1 volta ciascun giocatore
        /// </summary>
        /// <param name="gameBoard"></param>
        /// <param name="currentPlayer"></param>
        /// <param name="turnPhase"></param>
        /// <returns></returns>
        public static bool IsGameBoardStateLegal(Board gameBoard, Player currentPlayer, TurnPhase turnPhase)
        {
            switch (turnPhase)
            {
                case TurnPhase.Move:
                case TurnPhase.InfoGathering:
                        return verifyBoardStateAtMovePhase(gameBoard, currentPlayer);
                case TurnPhase.Expulsion:
                    return verifyBoardStateAtExpulsionState(gameBoard, currentPlayer);
            }
            return true;
        }

        private static bool verifyBoardStateAtExpulsionState(Board gameBoard, Player currentPlayer)
        {
            foreach (var pawn in gameBoard.Pawns)
            {
                // verificare che non ci sia più di un pedone sulla stessa casella
                var pawnsOnCell = gameBoard.GetPawnsOnCell(pawn.Position);
                if (pawnsOnCell.Count > 1)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool verifyBoardStateAtMovePhase(Board gameBoard, Player currentPlayer)
        {
            int[] otherPlayerSeenCount = new int[5]; // nell'ordine: Black, Red, Blue, Green, Yellow

            // Implement the logic to check if a plan is legal
            foreach (var pawn in gameBoard.Pawns)
            {
                if(pawn.Color != currentPlayer.Color)
                {
                    continue;
                }
                // verificare che non ci siano più di due pedoni sulla stessa casella
                var pawnsOnCell = gameBoard.GetPawnsOnCell(pawn.Position);
                if (pawnsOnCell.Count > 2)
                {
                    return false;
                }
                // Il plan non è legale se il giocatore corrente finisce con più di uno dei suoi pawn sui pawn di un altro giocatore (un giocatore può essere interrogato solo una volta in un turno da un altro giocatore )

                if (pawnsOnCell.Count > 1)
                {
                    foreach (var p in pawnsOnCell)
                    {
                        if (p.Color != currentPlayer.Color && p.Color != PlayerColor.Black)
                        {
                            otherPlayerSeenCount[(int)p.Color]++;
                            if (otherPlayerSeenCount[(int)p.Color] > 1)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        internal static List<Move> GetLegalDismissionMoves(Pawn p, Board board, PlayerColor color)
        {
            if(p.Color == PlayerColor.Black && color == PlayerColor.Black)
            {
                throw new ArgumentException($"Non posso allontanare l'ambasciatore per il player ambasciatore! Hai fatto un errore da qualche parte.");
            }
            else if(p.Color == PlayerColor.Black)
            {
                // allontano l'ambasciatore dal giocatore
                return GetLegalDismissionMovesForAmbassador(p, board);
            }
            else if(p.Color != color)
            {
                return GetLegalDismissionsForPawn(p, board);
            }
            else
            {
                throw new NotImplementedException($"Imprevisto in GetLegalDismissionMoves!! pawn: {p}, board: {board}, playerColor: {color}");
            }
            
        }

        private static List<Move> GetLegalDismissionsForPawn(Pawn p, Board board)
        {
            List<Move> moves = new();
            var currentCell = p.Position;
            foreach (Edge e in currentCell.Edges)
            {
                var targetCell = e.Travel(currentCell);

                var pawnsOnCell = board.GetPawnsOnCell(targetCell);

                bool cellIsEmpty = pawnsOnCell.Count == 0;

                if (cellIsEmpty)
                {
                    moves.Add(new Move { Pawn = p, To = targetCell });
                }
            }
            return moves;
        }

        private static List<Move> GetLegalDismissionMovesForAmbassador(Pawn p, Board board)
        {
            List<Move> result = new();
            // in questa fase il giocatore deve mandare l'ambasciatore in una casella colorata libera oppure all'ambasciata
            // è l'unico caso di mossa che "salta" tutte le altre caselle
            foreach (int i in cellIdsForAmbassadorToReturn)
            {
                var targetCell = board.CellsById[i];
                bool targetCellIsEmpty = board.CellIsEmpty(targetCell);

                if (targetCellIsEmpty)
                {
                    result.Add(new Move { Pawn = p, To = targetCell });
                }
            }
            if (result.Count == 0)
            {
                // Qui abbiamo scoperto un bel problema
                Console.Error.WriteLine("Attenzione!!! Non posso mandare via l'ambasciatore!");
                throw new ApplicationException("Non posso mandare via l'ambasciatore!");
            }
            return result;
        }
    }
}
