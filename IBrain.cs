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
        /// Si decide il piano per il giocatore Ambasciatore. L'ambasciatore non ha molto da fare,
        /// deve solo scoprire l'identità di tutti e dichiarare di averlo fatto prima che una coppia
        /// dichiari missione compiuta. 
        /// Se dichiara che ha capito l'identità di tutti, la deve comunicare (come se la comunicasse alla stampa...)
        /// e poi aspetta la fine della partita. 
        /// Alla fine della partita, se ha indovinato i 4 giocatori, vince l'ambasciatore, a prescindere dall'esito
        /// della missione vera e propria. Se ha sbagliato, o se non ha dichiarato niente, si valuta l'esito della missione
        /// lasciando fuori l'ambasciatore.
        /// 
        /// In ogni caso, il giocatore ambasciatore può effettuare 1 o 2 movimenti su qualunque percorso (non può saltare pedine).
        /// </summary>
        /// <param name="gameState"></param>
        /// <returns></returns>
        //Plan ChooseAmbassadorPlayerPlan(GameState gameState);

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
