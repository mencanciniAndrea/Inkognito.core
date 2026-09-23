using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Inkognito.Core
{
    public class LazyBrain : IBrain
    {
        public MovePlanner Planner { get; set; } = null!;
        public LazyBrain(ILoggerFactory factory)
        {
            Planner = new MovePlanner(factory);
        }

        public Plan ChoosePlan(GameState gameState, IEnumerable<MoveType> moveTypes, TurnPhase turnPhase)
        {
            var possiblePlans = Planner.GetAllPossiblePlans(gameState.Board, moveTypes, gameState.CurrentPlayer, turnPhase);

            Console.Out.WriteLine($"Numero di piani possibili: {possiblePlans.Count}");

            var plausiblePlans = new List<Plan>();
            int i = 1;
            foreach (var plan in possiblePlans)
            {
                // verificare se il piano è legale prima di valutarlo
                if (RulesEngine.IsGameBoardStateLegal(gameState.Board, gameState.CurrentPlayer, turnPhase))
                {
                    
                    EvaluatePlan(plan);

                    Console.Out.WriteLine($"Piano #{i}: {plan}");

                    // controllare che il piano non ti faccia tornare da dove sei partito e che non ti faccia tornare su una casella già visitata
                    HashSet<int> visitedCells = new HashSet<int>();

                    bool dummyPlan = false;

                    foreach(var move in plan.Moves)
                    {
                        if (visitedCells.Contains(move.To.Id))
                        {
                            Console.Out.WriteLine($"Piano dummy. Casella già visitata: {move.To.Id}");
                            dummyPlan = true;
                        }
                        else
                        {
                            visitedCells.Add(move.To.Id);
                        }
                    }

                    if (!dummyPlan)
                    {
                        plausiblePlans.Add(plan);
                    }
                }
                else
                {
                    Console.Out.WriteLine($"Piano non legale: {plan}");
                }
                i++;
            }

            return plausiblePlans[0];
        }

        public void EvaluatePlan(Plan plan)
        {
            //TODO implementare!
        }

        /// <summary>
        /// Bisogna rispondere ad una richiesta fatta direttamente da un altro giocatore.
        /// Bisogna dare 3 carte (almeno), di cui almeno una vera. 2 carte del tipo della richiesta, 1 dell'altro.
        /// 
        /// Se il giocatore richiedente è l'ambasciatore, bisogna dare 2 carte del tipo della richiesta e basta.
        /// 
        /// Non si può fornire una risposta già data in precedenza, Se tutte le possibili risposte sono state già
        /// date, allora OK, se ne sceglie una a caso.
        /// 
        /// Attenzione! la coppia di carte del tipo della richiesta non deve essere stato fornito nemmeno come set da 2!
        /// 
        /// avanzato: nel caso in cui sappiamo che il player è un alleato, sarebbe il caso di farglielo sapere, ed
        /// anche di comunicargli la missione, visto che così possiamo saltare alla fase di Compimento Missione
        /// </summary>
        /// <param name="me"></param>
        /// <param name="reqType"></param>
        /// <param name="sender"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public RequestAnswer AnswerToDirectQuestion(Player me, RequestType reqType, Player sender, PlayerMemory? memory)
        {
            List<InkognitoCard> answers = new List<InkognitoCard>();

            // TODO terminare!
            switch (reqType)
            {
                case RequestType.IDENTITY:
                    break;
                case RequestType.DISGUISE:
                    break;
            }

            answers.Add(new InkognitoCard() { 
                Type = InkognitoCardType.IDENTITY,
                Visibility = InkognitoCardVisibility.PUBLIC,
                Value = (int)Identity.B });

            return new RequestAnswer() { cards = answers };
        }

        /// <summary>
        /// Per rispondere ad una richiesta che viene da un Player ma è fatta attravesrso l'ambasciatore.
        /// Bisogna restituire 2 carte del tipo della richiesta, di cui una per forza vera.
        /// L'ordine non conta.
        /// La coppia di carte però, non deve essere già stata mostrata. Se non ci sono possibilità, allora mostrare
        /// una coppia già mostrata (non c'è altra via).
        /// </summary>
        /// <param name="me"></param>
        /// <param name="reqType"></param>
        /// <param name="sender"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public RequestAnswer AnswerToQuestionThorughAmbassador(Player me, RequestType reqType, Player sender, PlayerMemory? memory) 
        {
            List<InkognitoCard> answers = new List<InkognitoCard>();

            // TODO terminare
            switch (reqType)
            {
                case RequestType.IDENTITY:
                    break;
                case RequestType.DISGUISE:
                    break;
            }

            answers.Add(new InkognitoCard()
            {
                Type = InkognitoCardType.IDENTITY,
                Visibility = InkognitoCardVisibility.PUBLIC,
                Value = (int)Identity.B
            });

            return new RequestAnswer() { cards = answers };
        }

        public IReadOnlyList<Pawn> SortQuerablePawnList(List<Pawn> originalPawnList, PlayerMemory? memory)
        {
            // il lazy brain non fa niente. Come viene viene.
            return originalPawnList;
        }

        public RequestType WhatToRequestTo(PlayerColor pColor, PlayerMemory? memory)
        {
            // se non conosci l'identità di questo signore, chiedigliela
            var (identity, disguise, mission) = memory!.GetKnownPlayerDetails(pColor);
            if (identity == Identity.DON_T_KNOW) return RequestType.IDENTITY;
            else if (disguise == Disguise.DON_T_KNOW) return RequestType.DISGUISE;
            return RequestType.IDENTITY;
        }
    }
}
