using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Inkognito.Core
{
    public interface IBrain
    {
        Plan ChoosePlan(GameState gameState, IEnumerable<MoveType> moveTypes, TurnPhase turnPhase);

        /// <summary>
        /// Questo passaggio è importante
        /// Inoltre, l'ordine di interrogazione è fondamentale per non perdere tempo in certi casi.
        /// L'ordinamento tuttavia, dipende dalla conoscenza del Player, che potrebbe tranquillamente risiedere nel Brain, se si vuole
        /// fare in modo che il Brain sia il centro anche della memoria (cosa che normalmente è in un essere umano normale...)
        /// </summary>
        /// <param name="originalPawnList">La lista di pawn da interrogare</param>
        /// <param name="memory"></param>
        /// <returns></returns>
        IReadOnlyList<Pawn> SortQuerablePawnList(List<Pawn> originalPawnList, PlayerMemory? memory);

        RequestType WhatToRequestTo(PlayerColor pColor, PlayerMemory? memory);

        void EvaluatePlan(Plan plan);

    }
}
