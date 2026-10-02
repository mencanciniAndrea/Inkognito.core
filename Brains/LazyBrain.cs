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

namespace Inkognito.Core.Brains
{
    public class LazyBrain : IBrain
    {
        public MovePlanner Planner { get; set; } = null!;

        public LazyBrain()
        {
            Planner = new MovePlanner();
        }

        public Plan GetBestMovePlan(GameState gameState, IEnumerable<MoveType> moveTypes, TurnPhase turnPhase)
        {
            Player me = gameState.CurrentPlayer;
            var possiblePlans = Planner.GetAllPossiblePlans(gameState.Board, moveTypes, me);

            Console.Out.WriteLine($"Numero di piani possibili: {possiblePlans.Count}");

            var plausiblePlans = new List<Plan>();
            int dummyPlans = 0;
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
                            dummyPlans++;
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
            }

            if(plausiblePlans.Count == 0)
            {
                Console.Out.WriteLine($"Nessun piano legale non dummy trovato!");
                Console.Out.WriteLine($"Devi scegliere tra {dummyPlans} piani dummy o non fare niente");
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
                EvaluatePlanForAmbassador(plan, gameState.CurrentPlayer, phase);
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

        private static void EvaluatePlanForAmbassador(Plan plan, Player me, TurnPhase phase)
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
            
            if(pk.IsMyPartner == YES_OR_NO.YES)
            {
                result.Answers.Add(new (InkognitoCardVisibility.SECRET, InkognitoCardType.MISSION, (int)me.Mission));
            }

            result.Request = request;
            pk.AddAnswerGiven(result);

            return result;
        }

        /// <summary>
        /// Risponde ad una richiesta di informazioni fatta tramite ambasciatore. In questo caso le carte fornite devono essere 2, di cui una vera.
        /// Inoltre, la coppia di carte non può essere ripetuta, nemmeno se faceva parte di un set da 3.
        /// 
        /// Nota tecnica: in pratica basta generare le possibili risposte. Ad ogni risposta generata (ne sono 3 per identità e 3 per travestimento)
        /// basta confrontare la coppia con le risposte già date, che vengono dalla PlayerKnowledge. Quindi, anche se la richiesta è fatta
        /// tramite ambasciatore ma per un giocatore colorato, si confrontano solo le coppie già fornite, anche come set di 3 carte.
        /// </summary>
        /// <param name="me"></param>
        /// <param name="request"></param>
        /// <param name="random"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        private static PlayerAnswer AnswerRequestFromAmbassador(Player me, PlayerInfoRequest request, Random random, PlayerKnowledge pk)
        {
            PlayerAnswer result;

            // Tutte le risposte disponibili:
            List<PlayerAnswer> risposteDisponibili = GetPossibleAnswers(request.Type, me, pk, random);

            // Scegli una risposta a caso tra quelle disponibili. Ce ne sta almeno una.
            result = risposteDisponibili[random.Next(risposteDisponibili.Count())];
            
            // setta la request, altrimenti non ti ricorderai di averla data
            result.Request = request;

            pk.AddAnswerGiven(result);
            
            return result;
        }

        private static List<PlayerAnswer> GetPossibleAnswers(RequestType type, Player me, PlayerKnowledge pk, Random random)
        {
            List<PlayerAnswer> risposteDisponibili = new();

            int traitValue = type == RequestType.IDENTITY ? (int)me.Identity : (int)me.Disguise;
            InkognitoCardType cType = type == RequestType.IDENTITY ? InkognitoCardType.IDENTITY : InkognitoCardType.DISGUISE;

            List<Identity> otherIdentities = new();
            for (int i = 1; i < 5; i++)
            {
                if (i != traitValue)
                {
                    List<InkognitoCard> cards = new()
                    {
                        new(InkognitoCardVisibility.PUBLIC, cType, traitValue),
                        new(InkognitoCardVisibility.PUBLIC, cType, i)
                    };
                    PlayerAnswer answer = new()
                    {
                        Answers = cards
                    };
                    bool alreadyGiven = false;
                    
                    // Qui dobbiamo ciclare solo sulle risposte date al giusto tipo di richiesta
                    // perché quelle dell'altro tipo contengono solo una carta del tipo richiesto
                    foreach (PlayerAnswer used in pk.AnswersGiven.Where(a => a.Request!.Type == type).ToList())
                    {
                        //prendi le carte identità della risposta corrente e confrontale con quelle della risposta già data
                        var currentCardsOfCorrectType = answer.Answers.Where(a => a.Type == cType).Select(a => a.Value).ToList();
                        alreadyGiven = used.Answers.Where(a => a.Type == cType).Select(a => a.Value).ToList().All(id => currentCardsOfCorrectType.Contains(id));
                        if (alreadyGiven) break;
                    }
                    if (!alreadyGiven)
                    {
                        risposteDisponibili.Add(answer);
                    }
                }
            }

            Console.Out.WriteLine($"Risposte disponibili da dare a {pk.About} per {type} : {risposteDisponibili.Count}");
            if (risposteDisponibili.Count == 0)
            {
                // le hai già date tutte. Ne componi una a caso, tanto se ti obbligano a dire quale è quella vera, in teoria la saprebbero già... sono loro che sono tonti
                List<int> otherValues = new();
                for(int i = 1; i < 5; i++)
                {
                    if (i != traitValue)
                    {
                        otherValues.Add(i);
                    }
                }
                List<InkognitoCard> cards = new()
                {
                    new(InkognitoCardVisibility.PUBLIC, cType, traitValue),
                    new(InkognitoCardVisibility.PUBLIC, cType, otherValues[random.Next(otherValues.Count)])
                };
                PlayerAnswer answer = new()
                {
                    Answers = cards
                };
                risposteDisponibili.Add(answer);
                Console.Out.WriteLine($"Do una risposta a caso a {pk.About} per {type} : {answer}");
            }
            return risposteDisponibili;
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
        public bool ShouldDeclareMissionCompleted(Player me, GameState gameState)
        {
            if(me.Identity == Identity.A)
            {
                if(gameState.AmbassadorReport != null)
                {
                    // l'ambasciatore ha già depositato il report. Non c'è bisogno di dichiarare missione compiuta
                    return false;
                }
                return ShouldAmbassadorDepositReport(me);
            }
            
            Mission? m = me.MissionToComplete;
            bool result = false;
            if (m != null)
            {
                if (m.VerifyVictoryConditions(gameState)) result = true;
            }
            // o gioco da solo, oppure il mio partner deve essere non null (mi devo fidare di qualcuno)
            if(me.Memory!.IMustPlayAlone == YES_OR_NO.DONT_KNOW) return false;
            if(me.Memory!.IMustPlayAlone == YES_OR_NO.YES || me.MyPartner is not null)
            {
                return result;
            }
            return false;
        }

        private static bool ShouldAmbassadorDepositReport(Player me)
        {
            for (int i = 1; i <= (int)PlayerColor.Yellow; i++)
            {
                var (id, disg, _) = me.Memory!.GetKnownPlayerDetails((PlayerColor)i);
                if (id == Identity.DON_T_KNOW || disg == Disguise.DON_T_KNOW) return false;
            }
            // So tutto di tutti!
            return true;
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

        /// <summary>
        /// Si gestisce la risposta, sia per un giocatore normale che per l'ambasciatore
        /// </summary>
        /// <param name="brainOwner">il proprietario del Brain. Si aggiorna la memoria di questo player</param>
        /// <param name="answer">La risposta ricevuta</param>
        /// <param name="gameState">il gamestate corrente. Serve per recuperare gli altri giocatori in base al colore</param>
        /// <exception cref="ArgumentException"></exception>
        public void ManageAnswer(Player brainOwner, PlayerAnswer answer, GameState gameState)
        {
            PlayerMemory memory = brainOwner.Memory!;
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


            Identity myPartnerId = RulesEngine.GetMyPartnerIdentity(brainOwner);
            if (possibleIdentities.Count == 1)
            {
                k.AssuredIdentity = possibleIdentities.First();
                if (brainOwner.Identity != Identity.A)
                {
                    k.IsMyPartner = k.AssuredIdentity == myPartnerId ? YES_OR_NO.YES : YES_OR_NO.NO;

                    if (k.IsMyPartner == YES_OR_NO.YES)
                    {
                        brainOwner.MyPartner = gameState.GetPlayerByColor(k.About);
                        brainOwner.Memory!.IMustPlayAlone = YES_OR_NO.NO;
                    }
                }
                // aggiorna tutte le altre knowledge.
                // però... ora che ho aggiornato questa knowledge, in teoria, si verifica un effetto a cascata
                // che mi consente di continuare a fare inferenza sulle altre knowledges... 
                for (int c = 1; c < 5; c++)
                {
                    if (c != (int)brainOwner.Color && c != (int)k.About)
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
            if(brainOwner.Identity != Identity.A)
            {
                // piuttosto sveglio per un lazy brain...
                if (possibleIdentities.Count == 2)
                {
                    if (!possibleIdentities.Contains(myPartnerId))
                    {
                        k.IsMyPartner = YES_OR_NO.NO;
                    }
                }
            }
            
            if (possibleDisguises.Count == 1)
            {
                k.AssuredDisguise = possibleDisguises.First();
                for (int c = 1; c < 5; c++)
                {
                    if (c != (int)brainOwner.Color && c != (int)k.About)
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

            if(gameState.ActivePlayers == 3)
            {
                SetEventuallyPlayAlone(brainOwner, gameState);
                if(brainOwner.Memory!.IMustPlayAlone == YES_OR_NO.YES)
                {
                    // Se devi giocare da solo, aggiorna subito la missione e scappa!
                    brainOwner.MissionToComplete = Mission.GetMissionForPlayerAlone(brainOwner.Identity, gameState);
                    Console.Out.WriteLine($"{brainOwner.Name} deve: {brainOwner.MissionToComplete.Description}");
                }
            }
        }

        /// <summary>
        /// Da utilizzare se ci sono 3 giocatori. Stabilisce, in base a quello che il player sa, se deve giocare da solo o no
        /// </summary>
        /// <param name="me"></param>
        /// <param name="memory"></param>
        private void SetEventuallyPlayAlone(Player me, GameState g)
        {
            PlayerMemory? memory = me.Memory;
            if (memory == null) return;

            List<PlayerKnowledge> otherPlayersKnowledge = new();
            for (int c = 1; c <= (int)PlayerColor.Yellow; c++)
            {
                PlayerColor currentColor = (PlayerColor)c;
                if (currentColor == me.Color) continue;

                PlayerKnowledge? k = memory.GetPlayerKnowledge(currentColor);
                if (k == null) continue;
                otherPlayersKnowledge.Add(k);
            }

            bool someAreUnknown = false;

            foreach (var k in otherPlayersKnowledge)
            {
                if (k.IsMyPartner == YES_OR_NO.YES)
                {
                    memory.IMustPlayAlone = YES_OR_NO.NO;
                    me.MyPartner = g.GetPlayerByColor(k.About);
                    return;
                }
                else if (k.IsMyPartner == YES_OR_NO.DONT_KNOW)
                {
                    someAreUnknown = true;

                }
            }
            if (someAreUnknown)
            {
                memory.IMustPlayAlone = YES_OR_NO.DONT_KNOW;

            }
            else
            {
                // se sono arrivato qui, sono tutti noti, e nessuno è il mio partner. Quindi devo giocare da solo
                memory.IMustPlayAlone = YES_OR_NO.NO;
            }
        }
    }
}
