using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
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

        public Plan GetBestMovePlan(GameState gameState, IEnumerable<MoveType> moveTypes, TurnPhase turnPhase)
        {
            var possiblePlans = Planner.GetAllPossiblePlans(gameState.Board, moveTypes, gameState.CurrentPlayer);

            Console.Out.WriteLine($"Numero di piani possibili: {possiblePlans.Count}");

            var plausiblePlans = new List<Plan>();
            int i = 1;
            foreach (var plan in possiblePlans)
            {
                // verificare se il piano è legale prima di valutarlo
                if (RulesEngine.IsGameBoardStateLegal(gameState.Board, gameState.CurrentPlayer, TurnPhase.Move))
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
        /// Decidi quali carte mostrare al player
        /// </summary>
        /// <param name="me"></param>
        /// <param name="request"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public PlayerAnswer ReplyToRequest(Player me, PlayerInfoRequest request, PlayerMemory memory)
        {
            PlayerAnswer result = new();

            // ora partono tutti i ragionamenti. 
            // un lazy brain non è che faccia tutti questi ragionamenti, in effetti... però qualcosa deve pur rispondere.

            // 1. stabiliamo intanto quante e quali carte mostrare

            int numberOfIdentityCardsToShow;
            int numberOfDisguiseCardsToShow;

            switch (request.Type)
            {
                case RequestType.IDENTITY:
                    numberOfIdentityCardsToShow = 2;
                    numberOfDisguiseCardsToShow = request.ThroughAmbassador ? 0 : 1;
                    break;
                case RequestType.DISGUISE:
                    numberOfIdentityCardsToShow = request.ThroughAmbassador ? 0 : 1;
                    numberOfDisguiseCardsToShow = 2;
                    break;
            }

            // 2. ora possiamo decidere quali carte estrarre
            // TODO: completare



            return result;
        }

        /// <summary>
        /// usato per decidere se dichiarare missione compiuta
        /// </summary>
        /// <param name="me"></param>
        /// <param name="gameState"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public bool ShouldDeclareMissionCompleted(Player me, GameState gameState, PlayerMemory memory)
        {
            //TODO implementare
            return false;
        }

        /// <summary>
        /// Riordina la lista dei pawns interrogabili.
        /// Il lazy brain non fa ragionamenti, la riporta così com'è.
        /// Un cervello più avanzato potrebbe fare ragionamenti, per massimizzare la quantità di informazioni certe raccoglibili
        /// </summary>
        /// <param name="originalPawnList"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public IReadOnlyList<Pawn> SortQuerablePawnList(List<Pawn> originalPawnList, PlayerMemory? memory)
        {
            // il lazy brain non fa niente. Come viene viene.
            return originalPawnList;
        }

        /// <summary>
        /// Gestisce la decisione di cosa chiedere a chi hai incontrato
        /// </summary>
        /// <param name="pColor"></param>
        /// <param name="memory"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        public (PlayerColor,RequestType) WhatToRequestTo(PlayerColor pColor, PlayerMemory? memory, Random random)
        {
            PlayerColor playerColor = pColor;
            if(pColor == PlayerColor.Black)
            {
                // se è l'ambasciatore scegli a caso... qui si potrebbe anche fare una cosa più strutturata, ma per un lazy brain...
                playerColor = (PlayerColor) (random.Next((int)PlayerColor.Yellow) + 1);
            }
            // se non conosci l'identità di questo signore, chiedigliela
            var (identity, disguise, mission) = memory!.GetKnownPlayerDetails(pColor);
            if (identity == Identity.DON_T_KNOW) return (playerColor, RequestType.IDENTITY);
            else if (disguise == Disguise.DON_T_KNOW) return (playerColor, RequestType.DISGUISE);
            return (playerColor, RequestType.IDENTITY);
        }

        /// <summary>
        /// Elabora un piano di mosse per mandare via il pawn corrente
        /// </summary>
        /// <param name="gameState"></param>
        /// <param name="p"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public Plan DismissPawn(Pawn p, GameState gameState, Player currentPlayer, PlayerMemory memory)
        {
            List<Plan> dismissionPlans = new();
            dismissionPlans.AddRange(Planner.GetDismissionPlans(p, gameState.Board, currentPlayer));

            List<Plan> legalPlans = new();
            foreach (Plan plan in dismissionPlans)
            {
                if(RulesEngine.IsGameBoardStateLegal(plan.resultingBoard, currentPlayer, TurnPhase.Expulsion))
                {
                    legalPlans.Add(plan);
                    EvaluatePlan(plan);
                }
            }
            return legalPlans[0];
        }

        public void ManageAnswer(PlayerAnswer answer, PlayerMemory memory)
        {
            var receiverColor = answer.Request.Receiver;
            PlayerKnowledge k = memory.GetPlayerKnowledge(receiverColor);
            k.AddAnswerReceived(answer);

            // questa è la bitmask creata dalle risposte
            var answerBitmask = answer.AsBitmask();

            // metti in AND logico la bitmask con WhatIKnowAboutHim. Se la sua bitmask ha uno zero, il mio diventa zero. Se il mio era zero, resta zero.
            for (int identity = 1; identity < (int)Identity.DON_T_KNOW; identity++)
            {
                for (int disguise = 1; disguise < (int)Disguise.DON_T_KNOW; disguise++)
                {
                    k.WhatIKnowAboutHim[identity, disguise] = k.WhatIKnowAboutHim[identity, disguise] && answerBitmask[identity, disguise];
                }
            }

            // gestisci eventuali carte segrete
            foreach (var a in answer.Answers)
            {
                if (a.Visibility == InkognitoCardVisibility.SECRET)
                {
                    // caso avanzato, ma lo gestiamo lo stesso
                    switch (a.Type)
                    {
                        case InkognitoCardType.IDENTITY:
                            // carta segreta identità: metti 0 a tutte le altre
                            k.AssuredIdentity = (Identity)a.Value;
                            for (int identity = 1; identity < (int)Identity.DON_T_KNOW; identity++)
                            {
                                for (int disguise = 1; disguise < (int)Disguise.DON_T_KNOW; disguise++)
                                {
                                    if (identity != a.Value)
                                    {
                                        k.WhatIKnowAboutHim[identity, disguise] = false;
                                    }
                                }
                            }
                            break;
                        case InkognitoCardType.DISGUISE:
                            // carta segreta travestimento: metti 0 a tutte quelle non corrispondenti
                            k.AssuredDisguise = (Disguise)a.Value;
                            for (int identity = 1; identity < (int)Identity.DON_T_KNOW; identity++)
                            {
                                for (int disguise = 1; disguise < (int)Disguise.DON_T_KNOW; disguise++)
                                {
                                    if (disguise != a.Value)
                                    {
                                        k.WhatIKnowAboutHim[identity, disguise] = false;
                                    }
                                }
                            }
                            break;
                        case InkognitoCardType.MISSION:
                            // Qui non c'è da fare ragionamenti. Mi ha fatto vedere la missione. Va bene così, sia che è mio compagno che non (nell'ultimo caso mi sta ingannando).
                            k.AssignedMission = (MissionPart)a.Value;
                            break;
                        default:
                            throw new ArgumentException($"Card Type {a.Type} not valid at this point!");
                    }
                }

                //Attenzione qui: questo è il momento di fare inferenza anche sulle altre conosceze: se hai beccato l'identità o il travestimento di uno, questo si ripercuote anche sugli altri
                // ma è il Brain che deve fare questo ragionamento
            }
        }
    }
}
