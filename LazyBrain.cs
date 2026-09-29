using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Pipes;
using System.Linq;
using System.Numerics;
using System.Reflection;
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
            Player me = gameState.CurrentPlayer;
            var possiblePlans = Planner.GetAllPossiblePlans(gameState.Board, moveTypes, me);

            Console.Out.WriteLine($"Numero di piani possibili: {possiblePlans.Count}");

            var plausiblePlans = new List<Plan>();
            int i = 1;
            foreach (var plan in possiblePlans)
            {
                // verificare se il piano è legale prima di valutarlo
                if (RulesEngine.IsGameBoardStateLegal(gameState.Board, me, TurnPhase.Move))
                {
                    
                    EvaluatePlan(plan, gameState, turnPhase);

                    //Console.Out.WriteLine($"Piano #{i}: {plan}");

                    // controllare che il piano non ti faccia tornare da dove sei partito e che non ti faccia tornare su una casella già visitata
                    HashSet<int> visitedCells = new ();

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

            // super-lazy... scegli il primo che non sia vuoto, se c'è.
            Plan[] chooseFrom = plausiblePlans.Where(p => p.Moves.Count() > 0).ToArray();
            if (chooseFrom.Length == 0)
            {
                return plausiblePlans[0];
            }
            else
            {
                Plan result = chooseFrom[0];

                foreach (Plan p in chooseFrom)
                {
                    bool decisionMade = false;

                    if (result.Traits.Contains(PlanTraits.MEET_AMBASSADOR) || p.Traits.Contains(PlanTraits.MEET_AMBASSADOR))
                    {
                        if(result.Traits.Contains(PlanTraits.MEET_AMBASSADOR) && p.Traits.Contains(PlanTraits.MEET_AMBASSADOR))
                        {
                            decisionMade = false;
                        }
                        else if(!result.Traits.Contains(PlanTraits.MEET_AMBASSADOR) && p.Traits.Contains(PlanTraits.MEET_AMBASSADOR))
                        {
                            decisionMade = true;
                            result = p;
                        }else
                        {
                            decisionMade = true;
                        }
                    }
                    if (!decisionMade)
                    {
                        if(result.Traits.Contains(PlanTraits.MEET_UNKNOWN_PLAYER) || p.Traits.Contains(PlanTraits.MEET_UNKNOWN_PLAYER))
                        {
                            if(result.Traits.Contains(PlanTraits.MEET_UNKNOWN_PLAYER) && !p.Traits.Contains(PlanTraits.MEET_UNKNOWN_PLAYER))
                            {
                                decisionMade = true;
                            }
                            else
                            {
                                result = p;
                                decisionMade = true;
                            }
                        }
                    }
                    if (!decisionMade)
                    {
                        bool resultGoesNearAmbassador = result.Traits.Contains(PlanTraits.GET_NEAR_AMBASSADOR);
                        bool pGoesNearAmbassador = p.Traits.Contains(PlanTraits.GET_NEAR_AMBASSADOR);
                        if (resultGoesNearAmbassador || pGoesNearAmbassador)
                        {
                            if(resultGoesNearAmbassador && !pGoesNearAmbassador)
                            {
                                decisionMade = true;
                            }
                            else
                            {
                                result = p;
                                decisionMade = true;
                            }
                        }
                    }
                    if (!decisionMade)
                    {
                        bool resultGoesNearPlayer = result.Traits.Contains(PlanTraits.GET_NEAR_PLAYER);
                        bool pGoesNearPlayer = p.Traits.Contains(PlanTraits.GET_NEAR_PLAYER);
                        if(resultGoesNearPlayer || pGoesNearPlayer)
                        {
                            if(resultGoesNearPlayer && !pGoesNearPlayer)
                            {
                                decisionMade = true;
                            }
                            else
                            {
                                result = p;
                                decisionMade = true;
                            }
                        }
                    }
                    // TODO questo è da completare, ma per adesso vediamo se vengono scelti piani decenti
                }
                return result;
            }
                
        }

        public void EvaluatePlan(Plan plan, GameState gameState, TurnPhase phase)
        {
            Pawn ambassador = plan.resultingBoard.AmbassadorPawn;
            if(gameState.CurrentPlayer.Identity == Identity.A)
            {
                EvaluateForAmbassador(plan, gameState.CurrentPlayer, phase);
            }
            else
            {
                EvaluateForColoredPlayer(plan, gameState.CurrentPlayer, ambassador, phase);
            }
        }

        private static void EvaluateForColoredPlayer(Plan plan, Player me, Pawn ambassador, TurnPhase phase)
        {

            foreach (Pawn p in plan.resultingBoard.Pawns)
            {
                if (p != ambassador)
                {
                    if (p.Color == me.Color)
                    {
                        var pawnsOnCell = plan.resultingBoard.GetPawnsOnCell(p.Position);
                        if (pawnsOnCell.Count > 1)
                        {
                            foreach (Pawn x in pawnsOnCell)
                            {
                                if (x.Color != p.Color)
                                {
                                    if (x.Color == PlayerColor.Black)
                                    {
                                        plan.Traits.Add(PlanTraits.MEET_AMBASSADOR);
                                    }
                                    else
                                    {
                                        var otherPlayerColor = x.Color;
                                        var k = me.Memory!.GetPlayerKnowledge(otherPlayerColor);
                                        if(k != null) // potrei fare riferimento ad un giocatore assente
                                        {
                                            if (k.AssuredIdentity == Identity.DON_T_KNOW)
                                            {
                                                plan.Traits.Add(PlanTraits.MEET_UNKNOWN_PLAYER);
                                            }
                                            else
                                            {
                                                if (k.IsMyPartner == YES_OR_NO.YES)
                                                {
                                                    plan.Traits.Add(PlanTraits.MEET_PARTNER);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void EvaluateForAmbassador(Plan plan, Player me, TurnPhase phase)
        {
            var pawnsOnCell = plan.resultingBoard.GetPawnsOnCell(plan.resultingBoard.AmbassadorPawn.Position);
            if (pawnsOnCell.Count > 1)
            {
                foreach (Pawn x in pawnsOnCell)
                {
                    if (x.Color != PlayerColor.Black)
                    {
                        var otherPlayerColor = x.Color;
                        var k = me.Memory!.GetPlayerKnowledge(otherPlayerColor);
                        if(k != null)
                        {
                            if (k.AssuredIdentity == Identity.DON_T_KNOW)
                            {
                                plan.Traits.Add(PlanTraits.MEET_UNKNOWN_PLAYER);
                            }
                            else
                            {
                                if (k.IsMyPartner == YES_OR_NO.YES)
                                {
                                    plan.Traits.Add(PlanTraits.MEET_PARTNER);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Decidi quali carte mostrare al player. Non si fa molte domande e risponde sempre una risposta casuale tra quelle disponibili...
        /// è un po' stupido, ma lasciamolo così per ora.
        /// </summary>
        /// <param name="me"></param>
        /// <param name="request"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public PlayerAnswer ReplyToRequest(Player me, PlayerInfoRequest request, PlayerMemory memory, Random random)
        {
            // recupera la memoria che hai di quel giocatore
            PlayerKnowledge? pk = memory.GetPlayerKnowledge(request.Sender);

            if (pk == null)
            {
                throw new ArgumentNullException($"Giocatore {request.Sender} non può essere null qui!");
            }

            PlayerAnswer result;

            if (request.ThroughAmbassador || request.Sender == PlayerColor.Black)
            {
                result = AnswerRequestFromAmbassador(me, request, random, pk);
            }
            else
            {
                result = AnswerDirectRequest(me, request, random, pk);
            }
            

            // Il task di seguito verrà, per adesso lasciamolo da fare
            // TODO: se conosco l'identità e so che è mio compagno, aggiungo la missione


            result.Request = request;
            pk.AddAnswerGiven(result);

            return result;
        }

        private static PlayerAnswer AnswerRequestFromAmbassador(Player me, PlayerInfoRequest request, Random random, PlayerKnowledge pk)
        {
            PlayerAnswer result;

            // Qui bisogna controllare le richieste fatte e togliere dalle risposte disponibili quelle che sono state già usate...

            // Tutte le risposte disponibili:
            List<PlayerAnswer> risposteDisponibili = new();

            if (request.Type == RequestType.IDENTITY)
            {
                List<Identity> otherIdentities = new();
                for (int i = 1; i < (int)Identity.DON_T_KNOW; i++)
                {
                    if(i != (int)me.Identity)
                    {
                        List<InkognitoCard> answers = new()
                        {
                            new(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)me.Identity),
                            new(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, i)
                        };
                        PlayerAnswer one = new()
                        {
                            Request = request,
                            Answers = answers
                        };
                        
                        risposteDisponibili.Add(one);
                    }
                }
            }
            else
            {
                List<Disguise> otherDisguises = new();
                for(int d = 1; d < (int) Disguise.DON_T_KNOW; d++)
                {
                    if(d != (int)me.Disguise)
                    {
                        List<InkognitoCard> answers = new()
                        {
                            new(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)me.Disguise),
                            new(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, d)
                        };
                        PlayerAnswer two = new()
                        {
                            Request = request,
                            Answers = answers
                        };
                        
                        risposteDisponibili.Add(two);
                    }
                }
            }

            //TODO controllare se nelle risposte fornite c'è già quella corrente
            /*
            foreach(PlayerAnswer currentAnswer in risposteDisponibili)
            {
                bool alreadyGiven = false;
                foreach (PlayerAnswer givenAnswer in pk.AnswersGiven)
                {
                    
                }
            }*/

            result = risposteDisponibili[random.Next(risposteDisponibili.Count())];

            pk.AddAnswerGiven(result);
            

            return result;
        }

        private static PlayerAnswer AnswerDirectRequest(Player me, PlayerInfoRequest request, Random random, PlayerKnowledge pk)
        {
            PlayerAnswer result;

            //prendi la lista delle risposte disponibili
            List<PlayerAnswer> availableAnswers = request.Type == RequestType.IDENTITY ? pk.AnswersToIdentityToGive : pk.AnswersToDisguiseToGive;

            
            if(availableAnswers.Count == 0)
            {
                // le hai già date tutte...
                List<InkognitoCard> answers = new()
                {
                    new(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)me.Identity),
                    new(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)me.Disguise)
                };

                if (request.Type == RequestType.IDENTITY)
                {
                    // scegli a caso tra le identità restanti
                    List<Identity> otherIds = new();
                    for(int i = 1; i < (int)Identity.DON_T_KNOW; i++)
                    {
                        if(i != (int) me.Identity)
                        {
                            otherIds.Add((Identity)i);
                        }
                    }

                    answers.Add(new(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)otherIds[random.Next(otherIds.Count)]));
                }
                else
                {
                    List<Disguise> otherDisg = new();
                    for(int d = 1; d < (int)Disguise.DON_T_KNOW; d++)
                    {
                        if(d != (int)me.Disguise)
                        {
                            otherDisg.Add((Disguise)d);
                        }
                    }
                    answers.Add(new(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)otherDisg[random.Next(otherDisg.Count)]));
                }

                PlayerAnswer def = new()
                {
                    Request = request,
                    Answers = answers
                };
                result = def;
            }

            else
            {
                int index = random.Next(availableAnswers.Count);
                result = availableAnswers[index];
                availableAnswers.Remove(result);
            }

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
        public RequestType WhatToRequestTo(PlayerColor pColor, PlayerMemory? memory, Random random)
        {
            PlayerColor playerColor = pColor;
            // se non conosci l'identità di questo signore, chiedigliela
            var (identity, disguise, mission) = memory!.GetKnownPlayerDetails(pColor);
            if (identity == Identity.DON_T_KNOW) return RequestType.IDENTITY;
            else if (disguise == Disguise.DON_T_KNOW) return RequestType.DISGUISE;
            return RequestType.IDENTITY;
        }


        public PlayerColor WhoToAskInfoBetween(Player[] players, Random random)
        {
            return players[random.Next(players.Length)].Color;
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
                    EvaluatePlan(plan, gameState, TurnPhase.Expulsion);
                }
            }
            if (legalPlans.Count == 0)
            {
                return dismissionPlans[0];
            }
            return legalPlans[0];
        }

        public void ManageAnswer(Player me, PlayerAnswer answer, PlayerMemory memory)
        {
            var receiverColor = answer.Request!.Receiver;
            PlayerKnowledge? k = memory.GetPlayerKnowledge(receiverColor);
            k!.AddAnswerReceived(answer);

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
            }

            // controllare se, per tutta la bitmask risultante su quello che so di lui, c'è solo un'identità possibile. Se sì, l'ho beccato. 
            // Uguale per il travestimento
            HashSet<Identity> possibleIdentities = new();
            HashSet<Disguise> possibleDisguises = new();
            for (int i = 1; i < 5; i++)
            {
                for (int j = 1; j < 5; j++)
                {
                    if (k.WhatIKnowAboutHim[i, j])
                    {
                        possibleIdentities.Add((Identity)i);
                        possibleDisguises.Add((Disguise)j);
                    }
                }
            }
            Identity myPartnerId = GetMyPartnerIdentity(me);
            if (possibleIdentities.Count == 1)
            {
                k.AssuredIdentity = possibleIdentities.First();

                k.IsMyPartner = k.AssuredIdentity == myPartnerId ? YES_OR_NO.YES : YES_OR_NO.NO;

                // aggiorna tutte le altre knowledge.
                // però... ora che ho aggiornato questa knowledge, in teoria, si verifica un effetto a cascata
                // che mi consente di continuare a fare inferenza sulle altre knowledges... 
                for (int c = 1; c < 5; c++)
                {
                    if (c != (int)me.Color && c != (int)k.About)
                    {
                        var ok = memory.GetPlayerKnowledge((PlayerColor)c);
                        if (ok != null)
                        {
                            for (int disg = 1; disg < (int)Disguise.DON_T_KNOW; disg++)
                            {
                                ok.WhatIKnowAboutHim[(int)k.AssuredIdentity, disg] = false;
                            }
                        }
                    }
                }
            }
            // piuttosto sveglio per un lazy brain...
            if (possibleIdentities.Count == 2)
            {
                if (!possibleIdentities.Contains(myPartnerId))
                {
                    k.IsMyPartner = YES_OR_NO.NO;
                }
            }
            if (possibleDisguises.Count == 1)
            {
                k.AssuredDisguise = possibleDisguises.First();
                for (int c = 1; c < 5; c++)
                {
                    if (c != (int)me.Color && c != (int)k.About)
                    {
                        var ok = memory.GetPlayerKnowledge((PlayerColor)c);
                        if(ok != null)
                        {
                            for (int id = 1; id < (int)Identity.DON_T_KNOW; id++)
                            {
                                ok.WhatIKnowAboutHim[id, (int)k.AssuredDisguise] = false;
                            }
                        }
                    }
                }
            }
        }

        private static Identity GetMyPartnerIdentity(Player me)
        {
            return me.Identity == Identity.F ? Identity.B
                                : me.Identity == Identity.B ? Identity.F
                                : me.Identity == Identity.X ? Identity.Z
                                : me.Identity == Identity.Z ? Identity.X : Identity.DON_T_KNOW;
        }
    }
}
