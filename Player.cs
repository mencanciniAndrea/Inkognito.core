using Inkognito.Core.Brains;
using Inkognito.Core.Decisions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Inkognito.Core
{
    public sealed class Player
    {
        //----------------------------------------------------------------------
        //
        // Properties
        //
        //----------------------------------------------------------------------

        private readonly Random _random;

        public string Name { get; }
        public PlayerType Type { get; internal set; } = PlayerType.Human;
        public PlayerColor Color { get; }
        public Identity Identity { get; }
        public Disguise Disguise { get; }
        public MissionPart Mission { get; }
        public IReadOnlyList<Pawn> Pawns { get; }

        public Dictionary <Disguise, Pawn> PawnsByDisguise { get; }

        public IReadOnlyList<MoveType> AvailableMoves { get; private set; } = Array.Empty<MoveType>();

        public PlayerObjective CurrentObjective { get; private set; }

        /// <summary>
        /// Questa è generale: data un'identità, si sa l'identità del partner. Solo che all'inizio del gioco non si sa quale giocatore è il partner.
        /// </summary>
        private Identity MyPartnerIdentity { get; }

        public Mission? MissionToComplete { get; set; }

        private Player? _myPartner;

        public Player? MyPartner { get => _myPartner;
            set
            {
                _myPartner = value;
                if(value != null)
                    Console.Out.WriteLine($"{Name} - {Color} ha come partner {_myPartner!.Name} {_myPartner!.Color}");
            }
        }
        //----------------------------------------------------------------------
        //
        // Intelligence Section
        //
        //----------------------------------------------------------------------

        public IBrain Brain { get; internal set; } = null!;

        public PlayerMemory? Memory { get; internal set; }

        //----------------------------------------------------------------------
        //
        // Functions
        //
        //----------------------------------------------------------------------

        internal Player(string name, PlayerColor color, Identity identity, Disguise disguise, MissionPart mission, Pawn[] pawns, Random r)
        {
            Name = name;
            Color = color;
            Identity = identity;
            Disguise = disguise;
            Mission = mission;
            Pawns = Array.AsReadOnly(pawns);
            PawnsByDisguise = pawns.ToDictionary(p => p.Disguise, p => p);
            Brain = new LazyBrain(); //TODO: non ci deve essere solo un LazyBrain!!!
            _random = r;
            CurrentObjective = PlayerObjective.FIND_PARTNER;
            MyPartnerIdentity = RulesEngine.GetMyPartnerIdentity(this);
            MyPartner = null;

            if(identity == Identity.A)
            {
                MissionToComplete = Inkognito.Core.Mission.GetAmbassadorMission();
            }
        }

        public void InitMemory(List<Player> allPlayers)
        {
            List<Player> otherPlayers = allPlayers.Where(p => p != null && p.Color != Color).ToList();
            Memory = new PlayerMemory(otherPlayers, this);
        }

        private bool PartnerFound()
        {
            return MyPartner != null;
        }

        private bool MissionDiscovered()
        {
            //TODO implementare
            return false;
        }

        private void UpdateObjective()
        {
            // TODO: implementare la macchina a stati finiti di come può evolvere l'assegnazione degli obiettivi.
            // Nel caso banale è lineare: Find Partner --> Discover Mission --> Complete Mission
            // ma si potrebbe fare in modo che durante un inganno, ci possano essere degli archi indietro
        }

        public MoveDecision DecideMoves(GameState gameState, IReadOnlyList<MoveType> availableMoves)
        {
            if (gameState is null)
                throw new ArgumentNullException(nameof(gameState));
            if (availableMoves is null)
                throw new ArgumentNullException(nameof(availableMoves));
            if (!ReferenceEquals(gameState.CurrentPlayer, this))
                throw new InvalidOperationException("Può giocare soltanto il giocatore corrente della partita.");


            if (this.Type == PlayerType.Human)
            {
                gameState.RequestMoveInput();
                return new MoveDecision(new List<Move>(), true);
            }

            //TODO: se avevi un piano in sospeso, restituisci la prossima lista di mosse. Ma lo devi chiedere al Brain, perché è lui che pensa
            //
            // if(Brain.IsExecutingPlan())
            // {
            //     return Brain.NextMove()
            // }
            // else ...
            //

            AvailableMoves = availableMoves;

            StringBuilder sb = new();
            sb.Append("Mosse disponibili: {");
            sb.AppendJoin(", ", availableMoves);
            sb.Append("}");
            Console.Out.WriteLine($"{sb}");

            // fase 2: scegliere le mosse disponibili. Qui se il giocatore è umano bisogna trovare il modo di recuperare l'input
            // se invece è CPU, si chiama il suo Brain
            List<Move> moves;
            if (Brain.IsFollowingAPlan())
            {
                moves = Brain.GetNextMove();
            }
            else
            {
                Brain.ElaboratePlan(gameState, availableMoves, GameInternalState.DecidingMove);
                moves = Brain.GetNextMove();
            }

            sb = new();
            sb.Append($"{Name} esegue: ");
            sb.AppendJoin(", ", moves);
            Console.Out.WriteLine($"{sb}");
            return new MoveDecision(moves, false);
            
        }

        public MoveDecision DismissPawn(GameState gameState, Pawn p)
        {
            if(Type == PlayerType.Human)
            {
                return new MoveDecision(new List<Move>(), true);
            }

            Plan dismissionPlan = Brain.DismissPawn(p, gameState, this, Memory!);
            StringBuilder sb = new();
            sb.Append($"{Name} manda via {p}: ");
            sb.AppendJoin(", ", dismissionPlan.Moves);

            Console.Out.WriteLine(sb);

            return new MoveDecision(dismissionPlan.Moves, false);
        }

        public void ManageAnswer(GameState gameState, PlayerAnswer answer)
        {
            Brain.ManageAnswer(this, answer, gameState);
        }

        public InformationReplyDecision AnswerTo(PlayerInfoRequest req)
        {
            if(Type == PlayerType.Human)
            {
                return new InformationReplyDecision(new PlayerAnswer(), true);
            }
            return new InformationReplyDecision(Brain.ReplyToRequest(this, req, Memory!, _random), false);
        }
        public PlayerAnswer Ask(PlayerInfoRequest req)
        {
            // Devo dare una risposta... che gli dico? Cervello, aiutami tu...
            return Brain.ReplyToRequest(this, req, Memory!, _random);
        }

        /// <summary>
        /// Da usare quando decidi di dichiarare missione compiuta
        /// </summary>
        public void DeclareMissionCompleted(GameState g) 
        {
            if(this.Identity == Identity.A)
            {
                var (rId, rDisg, _) = Memory!.GetKnownPlayerDetails(PlayerColor.Red);
                var (gId, gDisg, _) = Memory!.GetKnownPlayerDetails(PlayerColor.Green);
                var (bId, bDisg, _) = Memory!.GetKnownPlayerDetails(PlayerColor.Blue);
                var (yId, yDisg, _) = Memory!.GetKnownPlayerDetails(PlayerColor.Yellow);
                g.DepositReport(new(rId, rDisg, gId, gDisg, bId, bDisg, yId, yDisg));
                return;
            }
            if (this.Memory!.IMustPlayAlone == YES_OR_NO.YES)
            {
                g.DeclareLonelyMissionCompleted(this);
            }
            if(MyPartner == null)
            {
                return;
            }
            else g.DeclareMissionComplete(this, MyPartner);
            //TODO: per completare la missione devi essere nel tuo turno corrente.
            //TODO: la procedura è: 1. dichiara la vittoria dicendo "Missione Compiuta!" 2. scegli il giocatore a cui vuoi stringere la mano (dovrebbe essere il tuo alleato)
            //TODO: se il giocatore scelto rifiuta (sì, perché può rifiutare, se non è il tuo alleato) allora vince la squadra avversaria alla tua
            //TODO: se il giocatore ti stringe la mano ed è il tuo alleato, ma avete sbagliato missione: vincono gli altri
            //TODO: se il giocatore ti stringe la mano e non è il tuo alleato... patta.
        }

        public void EndTurn()
        {
            //TODO: fine turno dichiarata. Questo giocatore non può più dichiarare Missione Compiuta.
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{Name}: {Type}, {Color}, {Identity}, {Disguise}, {Mission}");
            
            sb.AppendJoin(",", Pawns);
            sb.AppendLine();
            sb.AppendLine("Memory: ");
            if (Memory != null)
            {
                sb.AppendLine($"{Memory}");
                
            }
            
            return sb.ToString();
        }

        internal InformationRequestDecision WhatDoYouWantToAsk(GameState gameState, List<Pawn> pawnList)
        {
            if(Type == PlayerType.Human)
            {
                return new InformationRequestDecision(new Dictionary<Pawn, PlayerInfoRequest>(), true);
            }

            Dictionary<Pawn, PlayerInfoRequest> pawnReqDict = new ();

            var sortedPawnList = Brain.SortQuerablePawnList(pawnList, Memory!);

            // fase 4: se la lista di player a cui chiedere informazioni non è vuota, si chiede a ciascuno di loro le informazioni richieste del tipo richiesto
            foreach (Pawn p in sortedPawnList)
            {
                PlayerColor pColorToAsk = p.Color;
                // 2. a chi chiedo cosa? Cervello, aiutami tu...
                if (p == gameState.AmbassadorPawn)
                {
                    // scegli il giocatore a cui chiedere, tra quelli disponibili (che non sia black!
                    Player?[] availablePlayers = gameState.Players.Where(p => p != null && p.Color != PlayerColor.Black && p.Color != this.Color).ToArray();

                    pColorToAsk = Brain.WhoToAskInfoBetween(availablePlayers!, _random);
                }

                var reqType = Brain.WhatToRequestTo(pColorToAsk, Memory!, _random);

                // 3. ask information to the player (crea la request e mandagliela)
                PlayerInfoRequest request = new(Color, pColorToAsk, reqType, p.Color == PlayerColor.Black);

                pawnReqDict[p] = request;
            }

            return new InformationRequestDecision(pawnReqDict, false);
        }

        internal void PrepareForTurn()
        {
            Brain.CleanCurrentMovePlan();
        }

        internal bool WantToDeclareMissionCompleted(GameState gameState)
        {
            return Brain.ShouldDeclareMissionCompleted(this, gameState);
        }

        internal Player? DeclareMissionPartner()
        {
            // Questa ha bisogno di input dall'esterno
            return MyPartner;
        }

        internal bool AcceptMissionCompleteRequest(GameState gameState)
        {
            //TODO questa ha bisogno di input dall'esterno
            return ReferenceEquals(MyPartner, gameState.CurrentPlayer);
        }

        internal bool WantAnotherReply(GameState gameState, PlayerAnswer answer)
        {
            // se sono già due volte che me la rimanda... basta.
            if (answer!.Request!.RetryTimes == 2) return false;

            var pk = Memory!.GetPlayerKnowledge(answer.Request!.Receiver);
            bool alreadyReceived = false;
            foreach(PlayerAnswer pa in pk!.AnswersReceived)
            {
                if(answer.Answers.All(ans => pa.Answers.Contains(ans)))
                {
                    alreadyReceived = true;
                    break;
                }
            }
            return alreadyReceived;
        }
    }
}
