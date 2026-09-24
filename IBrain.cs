using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Inkognito.Core
{
    public interface IBrain
    {
        Plan ChoosePlan(GameState gameState, IEnumerable<MoveType> moveTypes, TurnPhase turnPhase);

        Plan DismissPawn(GameState gameState, Pawn p, PlayerMemory memory);

        /// <summary>
        /// gestisce le risposte da dare al giocatore che ti sta chiedendo informazioni
        /// </summary>
        /// <param name="me"></param>
        /// <param name="request"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        PlayerAnswer ReplyToRequest(Player me, PlayerInfoRequest request, PlayerMemory memory);

        void ManageAnswer(PlayerAnswer answer, PlayerMemory memory);

        /// <summary>
        /// Questo passaggio è importante
        /// Inoltre, l'ordine di interrogazione è fondamentale per non perdere tempo in certi casi.
        /// L'ordinamento tuttavia, dipende dalla conoscenza del Player, che potrebbe tranquillamente risiedere nel Brain, se si vuole
        /// fare in modo che il Brain sia il centro anche della memoria (cosa che normalmente è in un essere umano normale...)
        /// </summary>
        /// <param name="originalPawnList">La lista di pawn da interrogare</param>
        /// <param name="memory"></param>
        /// <returns></returns>
        IReadOnlyList<Pawn> SortQuerablePawnList(List<Pawn> originalPawnList, PlayerMemory memory);

        (PlayerColor, RequestType) WhatToRequestTo(PlayerColor pColor, PlayerMemory memory, Random random);

        bool ShouldDeclareMissionCompleted(Player me, GameState gameState, PlayerMemory memory);

        void EvaluatePlan(Plan plan);

    }
}
