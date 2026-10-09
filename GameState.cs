using Inkognito.Core.Commands;
using Inkognito.Core.Decisions;
using Inkognito.Core.Events;
using Stateless;
using Stateless.Graph;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Inkognito.Core
{
    public sealed class GameState
    {

        public const int MAX_TURNS = 2000;

        public ConcurrentQueue<IGameCommand> Commands { get; } = new();
        public ConcurrentQueue<IGameEvent> Events { get; } = new();

        private Task? _gameTask;

        public List<MoveIndication> AvailableMoves { get; private set; } = new();

        public IReadOnlyList<Move> CurrentPlayersMoves { get; set; }

        public readonly ProphecyPhantom prophecyPhantom;
        public Board Board { get; }
        public int TurnNumber { get; private set; } = 1;
        /// <summary>Indice del posto corrente nella lista Players, inclusi i posti vuoti.</summary>
        public int CurrentPlayerIndex { get; private set; }
        public Player CurrentPlayer => Players[CurrentPlayerIndex]!;

        public Player? AmbassadorPlayer { get; private set; }
        /// <summary>Cinque posti: Red, Blue, Green, Yellow, Black. I posti assenti sono null.</summary>
        public IReadOnlyList<Player?> Players { get; }

        public Dictionary<PlayerColor, Player> PlayersByColor { get; }
        public Dictionary<Identity, Player> PlayersByIdentity { get; }


        /// <summary>Seed della partita; null se viene fornito direttamente un Random esterno.</summary>
        public int? Seed { get; }

        public Pawn AmbassadorPawn { get; } = null!;

        public bool GameOver { get; private set; }

        public int ActivePlayers { get; private set; }

        public AmbassadorReport? AmbassadorReport { get; private set; }
        public Pawn? PawnToAsk { get; private set; }
        public InformationReplyDecision? CurrentAnswer { get; private set; }
        public bool MissionAccepted { get; private set; }

        private StateMachine<GameInternalState, StateTrigger> InternalStateMachine;
        private InformationRequestDecision? CurrentPlayerQuery;
        private Player? DeclaredPartner;

        public GameState(params string?[] playerNames)
            : this(playerNames, GenerateSeed())
        {
        }

        public GameState(string?[] playerNames, int seed)
            : this(playerNames, new Random(seed))
        {
            Seed = seed;
        }

        public GameState(string?[] playerNames, Random random)
        {
            // Controlli di validità dei parametri
            if (playerNames is null)
                throw new ArgumentNullException(nameof(playerNames));

            if (playerNames.Length != 5)
                throw new ArgumentException("Sono richiesti esattamente 5 posti.", nameof(playerNames));

            if (random is null)
                throw new ArgumentNullException(nameof(random));

            int nullPlayerCount = 0;
            foreach (string? name in playerNames)
            {
                if (string.IsNullOrWhiteSpace(name))
                    nullPlayerCount++;
            }

            if (nullPlayerCount > 2)
                throw new ArgumentException("Solo due giocatori possono avere nomi null, vuoti o composti solo da spazi", nameof(playerNames));

            if (!string.IsNullOrWhiteSpace(playerNames[4]) && nullPlayerCount > 0)
                throw new ArgumentException("Il quinto giocatore può essere presente solo se gli altri 4 giocatori hanno nomi validi", nameof(playerNames));

            // Inizializzazione dei giocatori
            var identities = new List<Identity> { Identity.F, Identity.B, Identity.X, Identity.Z };
            var disguises = new List<Disguise> { Disguise.Tall, Disguise.Thin, Disguise.Fat, Disguise.Small };
            var missions = new List<MissionPart> { MissionPart.Alfa, MissionPart.Bravo, MissionPart.Charlie, MissionPart.Delta };

            var colors = new[]
            {
                PlayerColor.Red,
                PlayerColor.Blue,
                PlayerColor.Green,
                PlayerColor.Yellow,
                PlayerColor.Black
            };

            Board = Board.LoadDefault();

            AmbassadorPawn = new Pawn(PlayerColor.Black, Disguise.Ambassador, Board.CellsById[33]);

            Board.AmbassadorPawn = AmbassadorPawn;
            List<Pawn> pawns = new() { };

            var players = new Player?[playerNames.Length];
            CreatePlayers(playerNames, random, identities, disguises, missions, colors, pawns, players);
            Players = Array.AsReadOnly(players);
            PlayersByColor = Players.Where(p => p != null).ToDictionary(p => p!.Color, p => p!);
            PlayersByIdentity = Players.Where(p => p != null).ToDictionary(p => p!.Identity, p => p!);

            if (PlayersByColor.TryGetValue(PlayerColor.Black, out var blackPlayer) && blackPlayer == null)
            {
                pawns.Add(AmbassadorPawn);
            }

            Board.Pawns = pawns;

            // Creazione del ProphecyPhantom
            prophecyPhantom = new ProphecyPhantom(random);

            // Scelta del giocatore che inizia il turno: tra i posti occupati, uno a caso.

            var occupiedSlots = Enumerable.Range(0, Players.Count)
                .Where(index => Players[index] is not null).ToArray();

            CurrentPlayerIndex = occupiedSlots[random.Next(occupiedSlots.Length)];

            //-----------------------------------------------------------------
            //
            // SETUP INTERNAL STATE MACHINE
            //
            //-----------------------------------------------------------------

            InternalStateMachine = SetupStateMachine();

            _gameTask = Task.Run(async () =>
            {
                while (!GameOver)
                {
                    if (Commands.TryDequeue(out var command))
                    {
                        try
                        {
                            switch (command)
                            {
                                case StartGameCommand startGameCommand:
                                    this.StartGame();
                                    break;
                                case IGameCommand gameCommand:
                                    throw new ArgumentException($"Comando non implementato: {command.GetType().Name}");
                                    // Esegui il comando specifico
                                    // gameCommand.Execute(this);
                                    break;
                                default:
                                    throw new ArgumentException($"Comando sconosciuto: {command.GetType().Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Errore durante l'esecuzione del comando {command}: {ex}");
                        }
                    }
                    else
                    {
                        await Task.Delay(10); // Attendere un breve periodo prima di controllare nuovamente la coda dei comandi
                    }
                }
            });
        }

        public void RequestMoveInput()
        {
            Events.Enqueue(new MoveInputRequestedEvent(CurrentPlayer, AvailableMoves.Where(m => !m.IsConsumed && m.Move != MoveType.None).Select(m => m.Move).ToList()));
        }

        /// <summary>
        /// Definisce la state machine dello stato interno
        /// </summary>
        private StateMachine<GameInternalState, StateTrigger> SetupStateMachine()
        {
            // creazione
            var m = new StateMachine<GameInternalState, StateTrigger>(GameInternalState.SettingUp);

            // stato SettingUp - il gioco non è ancora ufficialmente partito
            m.Configure(GameInternalState.SettingUp)
                .Permit(StateTrigger.StartGame, GameInternalState.Playing);

            // stato Playing - si parte. Da adesso in poi, fino al gameover, si rimane in stato Playing
            m.Configure(GameInternalState.Playing)
                .InitialTransition(GameInternalState.TurnStarted)
                .OnEntry(t => OnGameStarted())
                .Permit(StateTrigger.EndGame, GameInternalState.GameOver);

            // stato TurnStarted - sottostato di Playing (da adesso sono tutti sottostati di playing)
            // si estraggono le mosse disponibili e si passa direttamente a DecidingMove
            m.Configure(GameInternalState.TurnStarted)
                .SubstateOf(GameInternalState.Playing)
                .OnEntry(a => OnTurnStarted())
                .Permit(StateTrigger.DecideMoves, GameInternalState.DecidingMove);

            // stato DecidingMove - Qui il Player decide come muovere i suoi pedoni. Può dare le mosse tutte insieme, oppure una alla volta. Oppure non fare niente.
            m.Configure(GameInternalState.DecidingMove)
                .SubstateOf(GameInternalState.Playing)
                .Permit(StateTrigger.EndTurn, GameInternalState.EndingTurn)
                .Permit(StateTrigger.DeclareMissionCompleted, GameInternalState.DeclaringMissionComplete)
                .Permit(StateTrigger.ApplyMove, GameInternalState.ApplyingMoves)
                .OnEntry(a => OnDecideMove());

            // stato Moved - onentry si applicano le mosse dichiarate in CurrentPlayersMoves
            m.Configure(GameInternalState.ApplyingMoves)
                .SubstateOf(GameInternalState.Playing)
                .Permit(StateTrigger.EndTurn, GameInternalState.EndingTurn)
                .Permit(StateTrigger.DecideQuery, GameInternalState.DecidingQuery)
                .PermitIf(StateTrigger.DecideMoves, GameInternalState.DecidingMove, () => AvailableMoves.Count(m => !m.IsConsumed && m.Move != MoveType.None) > 0)
                .OnEntry(a => OnApplyMoves());

            // Stato Deciding Query - qui si decide cosa chiedere al tizio appena incontrato
            m.Configure(GameInternalState.DecidingQuery)
                .SubstateOf(GameInternalState.Playing)
                .Permit(StateTrigger.EndTurn, GameInternalState.EndingTurn)
                .Permit(StateTrigger.DeclareMissionCompleted, GameInternalState.DeclaringMissionComplete)
                .Permit(StateTrigger.Query, GameInternalState.AwaitingAnswer)
                .OnEntry(_ => OnDecidingQueryEntry());

            m.Configure(GameInternalState.AwaitingAnswer)
                .SubstateOf(GameInternalState.Playing)
                .Permit(StateTrigger.AnswerDelivered, GameInternalState.ElaboratingAnswer)
                .OnEntry(_ => NotifyPlayerToReply());

            m.Configure(GameInternalState.ElaboratingAnswer)
                .SubstateOf(GameInternalState.Playing)
                .Permit(StateTrigger.AnswerNoted, GameInternalState.DismissingPawn)
                .Permit(StateTrigger.DeclareMissionCompleted, GameInternalState.DeclaringMissionComplete)
                .Permit(StateTrigger.ReplyAgain, GameInternalState.AwaitingAnswer) // questa serve quando ti hanno dato una risposta già ricevuta
                .OnEntry(_ => ElaborateAnswer());

            m.Configure(GameInternalState.DismissingPawn)
                .SubstateOf(GameInternalState.Playing)
                .Permit(StateTrigger.DismissPawn, GameInternalState.PawnDismissed)
                .Permit(StateTrigger.DeclareMissionCompleted, GameInternalState.DeclaringMissionComplete)
                .OnEntry(_ => OnDismissPawnEntry());

            m.Configure(GameInternalState.PawnDismissed)
                .SubstateOf(GameInternalState.Playing)
                .PermitIf(StateTrigger.DecideMoves, GameInternalState.DecidingMove, () => AvailableMoves.Count(m => !m.IsConsumed && m.Move != MoveType.None) > 0)
                .Permit(StateTrigger.EndTurn, GameInternalState.EndingTurn)
                .OnEntry(_ => OnPawnDismissedEntry());

            m.Configure(GameInternalState.DeclaringMissionComplete)
                .SubstateOf(GameInternalState.Playing)
                .Permit(StateTrigger.AskMissionPartner, GameInternalState.AskingMissionToPartner)
                .Permit(StateTrigger.MissionCompletedAlone, GameInternalState.EvaluatingMission)
                .OnEntry(_ => OnDeclaringMissionCompleteEntry());

            m.Configure(GameInternalState.AskingMissionToPartner)
                .SubstateOf(GameInternalState.Playing)
                .Permit(StateTrigger.ReplyToMissionParnter, GameInternalState.EvaluatingMission)
                .OnEntry(_ => OnAskingMissionToPartnerEntry());

            m.Configure(GameInternalState.EvaluatingMission)
                .SubstateOf(GameInternalState.Playing)
                .Permit(StateTrigger.EndGame, GameInternalState.GameOver)
                .OnEntry(_ => EvaluateMissionCompleted());

            m.Configure(GameInternalState.EndingTurn)
                .SubstateOf(GameInternalState.Playing)
                .Permit(StateTrigger.DeclareMissionCompleted, GameInternalState.DeclaringMissionComplete)
                .Permit(StateTrigger.EndTurn, GameInternalState.TurnStarted)
                .Permit(StateTrigger.EndGame, GameInternalState.GameOver) // questa in teoria non ci dovrebbe essere, la metto per mettere un cap al numero di turni
                .OnEntry(a => AdvanceTurn());

            m.Configure(GameInternalState.GameOver)
                .OnEntry(_ => GameOver = true);

            //TODO valutare se mettere un reset per rigiocare da capo, ma non credo serva qui...

            return m;
        }

        private void OnDecideMove()
        {
            List<MoveType> availableMoves = AvailableMoves.Where(m => !m.IsConsumed && m.Move != MoveType.None).Select(m => m.Move).ToList();
            MoveDecision d = CurrentPlayer.DecideMoves(this, availableMoves);
            if (d.NeedsInput)
            {
                Events.Enqueue(new MoveInputRequestedEvent(CurrentPlayer, availableMoves));
            }
            else if(d.Moves.Count > 0)
            {
                CurrentPlayersMoves = d.Moves;
                InternalStateMachine.Fire(StateTrigger.ApplyMove);
            }
            else
            {
                InternalStateMachine.Fire(StateTrigger.EndTurn);
            }
        }

        private void OnGameStarted()
        {
            Events.Enqueue(new GameStartedEvent());
        }

        private void EvaluateMissionCompleted()
        {
            //TODO Implementare tutti i casi di valutazione della missione
            InternalStateMachine.Fire(StateTrigger.EndGame);
        }

        private void OnAskingMissionToPartnerEntry()
        {
            MissionAccepted = DeclaredPartner!.AcceptMissionCompleteRequest(this);

            InternalStateMachine.Fire(StateTrigger.ReplyToMissionParnter);
        }

        private void OnDeclaringMissionCompleteEntry()
        {
            DeclaredPartner = CurrentPlayer.DeclareMissionPartner();
            if(DeclaredPartner is null)
            {
                InternalStateMachine.Fire(StateTrigger.MissionCompletedAlone);
            }
            else
            {
                InternalStateMachine.Fire(StateTrigger.AskMissionPartner);
            }
        }

        private void OnPawnDismissedEntry()
        {
            if (CurrentPlayer.WantToDeclareMissionCompleted(this))
            {
                InternalStateMachine.Fire(StateTrigger.DeclareMissionCompleted);
            }
            else
            {
                if (AvailableMoves.Count(m => !m.IsConsumed && m.Move != MoveType.None) > 0)
                {
                    InternalStateMachine.Fire(StateTrigger.DecideMoves);
                }
                else
                {
                    InternalStateMachine.Fire(StateTrigger.EndTurn);
                }
            }
        }

        private void OnDismissPawnEntry()
        {
            MoveDecision howToDismissPawn = CurrentPlayer.DismissPawn(this, PawnToAsk!);
            foreach(Move m in howToDismissPawn.Moves)
            {
                SubmitMove(m);
            }
            if (CurrentPlayer.WantToDeclareMissionCompleted(this))
            {
                InternalStateMachine.Fire(StateTrigger.DeclareMissionCompleted);
            }
            else
            {
                InternalStateMachine.Fire(StateTrigger.DismissPawn);
            }
        }

        private void ElaborateAnswer()
        {
            if(CurrentPlayer.WantAnotherReply(this, CurrentAnswer!.Answer))
            {
                CurrentPlayerQuery!.InfoToAsk[PawnToAsk!].RetryTimes++;
                InternalStateMachine.Fire(StateTrigger.ReplyAgain);
            }
            CurrentPlayer.ManageAnswer(this, CurrentAnswer!.Answer);
            if (CurrentPlayer.WantToDeclareMissionCompleted(this))
            {
                InternalStateMachine.Fire(StateTrigger.DeclareMissionCompleted);
            }
            else
            {
                InternalStateMachine.Fire(StateTrigger.AnswerNoted);
            }
        }

        private void NotifyPlayerToReply()
        {
            CurrentAnswer = null;
            foreach (var info in CurrentPlayerQuery!.InfoToAsk)
            {
                var otherPlayer = PlayersByColor[info.Key.Color];
                CurrentAnswer = otherPlayer.AnswerTo(info.Value);

                Console.Out.WriteLine($"{CurrentAnswer.Answer}");
            }
            if(CurrentAnswer is not null)
            {
                InternalStateMachine.Fire(StateTrigger.AnswerDelivered);
            }
        }

        private void OnDecidingQueryEntry()
        {
            CurrentPlayerQuery = CurrentPlayer.WhatDoYouWantToAsk(this, new() { PawnToAsk! });
            Console.Out.WriteLine($"{CurrentPlayerQuery.InfoToAsk[PawnToAsk!]}");
            InternalStateMachine.Fire(StateTrigger.Query);
        }

        private void OnApplyMoves()
        {
            foreach(Move m in CurrentPlayersMoves)
            {
                SubmitMove(m);
                AvailableMoves.FindLast(p => p.Move == m.MoveTypeConsumed).IsConsumed = true;
            }

            //Ora: se sulla board ci sono pupazzi sulla stessa casella, vai in query.
            // Altrimenti, se ci sono ancora mosse disponibili, vai di nuovo in decidemoves.
            // Altrimenti vai a end turn

            // cicla solo sui pawn del player corrente
            PawnToAsk = null;
            foreach(Pawn p in CurrentPlayer.Pawns){
                var availablePawns = Board.GetPawnsOnCell(p.Position).Where(otherPawn => otherPawn.Color != p.Color).ToList();
                if(availablePawns.Count == 1)
                {
                    PawnToAsk = availablePawns.First();
                    break;
                }
            }
            if(PawnToAsk is not null)
            {
                InternalStateMachine.Fire(StateTrigger.DecideQuery);
            }
            else if(AvailableMoves.Count(m => !m.IsConsumed && m.Move != MoveType.None) > 0)
            {
                InternalStateMachine.Fire(StateTrigger.DecideMoves);
            }
            else
            {
                InternalStateMachine.Fire(StateTrigger.EndTurn);
            }
        }

        private void OnTurnStarted()
        {
            Events.Enqueue(new TurnStartedEvent(CurrentPlayer, TurnNumber));
            DrawMoves();
            CurrentPlayer.PrepareForTurn();
            InternalStateMachine.Fire(StateTrigger.DecideMoves);
        }

        public void StartGame()
        {
            InternalStateMachine.Fire(StateTrigger.StartGame);

            /*

            while(!GameOver)
            {
                //TODO sostituire con PlayTurn() una volta che ci sono tutti i comandi e gli eventi a disposizione
                PlayTurnOldStyle();
                
                AdvanceTurn();
            }

            */
        }

        private void DrawMoves()
        {
            bool IAmAmbassador = CurrentPlayer.Identity == Identity.A;
            AvailableMoves = IAmAmbassador ? new List<MoveIndication> { new(MoveType.Ambassador), new(MoveType.Ambassador) } :
                            prophecyPhantom.DrawMoves();

            Events.Enqueue(new MovesDrawnEvent(CurrentPlayer, AvailableMoves));
        }

        public void DepositReport(AmbassadorReport report)
        {
            if (report is null)
                throw new ArgumentNullException(nameof(report));
            if (AmbassadorReport is not null)
                throw new InvalidOperationException("Il report dell'ambasciatore è già stato depositato.");
            AmbassadorReport = report;
            Console.Out.WriteLine($"{AmbassadorPlayer!.Name} dice: \"So chi siete.\"");
        }
        private void CreatePlayers(string?[] playerNames, Random random, List<Identity> identities, List<Disguise> disguises, List<MissionPart> missions, PlayerColor[] colors, List<Pawn> pawns, Player?[] players)
        {
            ActivePlayers = 0;
            for (int i = 0; i < playerNames.Length; i++)
            {
                var playerName = playerNames[i];
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    continue;
                }

                // Conta i player attivi
                ActivePlayers++;
                players[i] = colors[i] == PlayerColor.Black
                    ? CreateAmbassadorPlayer(playerName!, random)
                    : new Player(playerName!, colors[i],
                        Draw(identities, random), Draw(disguises, random), Draw(missions, random),
                        CreatePawns(colors[i], random), random);
                pawns.AddRange(players[i]!.Pawns);
            }
            foreach(Player? p in players)
            {
                p?.InitMemory(new(players!));
            }


        }

        public Pawn? GetPawnOf(Identity id)
        {
            // prendi il player con l'identità id
            Player pl = GetPlayerById(id)!;
            Pawn pa = pl!.PawnsByDisguise[pl.Disguise];
            return pa;
        }

        private Player CreateAmbassadorPlayer(string name, Random random)
        {
            AmbassadorPlayer = new Player(name, PlayerColor.Black, Identity.A, Disguise.Ambassador, MissionPart.FindAllIdentities,
                        new[] { AmbassadorPawn }, random);
            return AmbassadorPlayer;
        }

        public Player? GetPlayerByColor(PlayerColor color)
        {
            return PlayersByColor.TryGetValue(color, out var player) ? player : null;
        }

        public void SubmitMove(Move move)
        {
            if (move is null)
                throw new ArgumentNullException(nameof(move));
            
            Events.Enqueue(new PawnMovedEvent(CurrentPlayer, move.Pawn, move.Pawn.Position, move.To, move.MoveTypeConsumed));

            Board.ApplyMove(move);
        }

        /// <summary>Stampa lo stato completo di debug, comprese le informazioni segrete dei giocatori.</summary>
        public void DumpState() => DumpState(Console.Out);

        public void DumpState(TextWriter writer)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));
            writer.WriteLine($"Seed: {Seed?.ToString() ?? "non disponibile (Random esterno)"}");
            writer.WriteLine($"Turno {TurnNumber} - Giocatore corrente: {CurrentPlayer.Name}");
            
            /*
            foreach (var player in Players)
            {
                if (player is null)
                    continue;
                writer.WriteLine($"{player}");
                
            }
            */
        }

        public void SetPlayerType(int playerIndex, PlayerType type)
        {
            if (playerIndex < 0 || playerIndex >= Players.Count)
                throw new ArgumentOutOfRangeException(nameof(playerIndex));
            if (type != PlayerType.Human && type != PlayerType.CPU)
                throw new ArgumentOutOfRangeException(nameof(type));
            var player = Players[playerIndex]
                ?? throw new ArgumentException("Il posto indicato non contiene un giocatore.", nameof(playerIndex));
            player.Type = type;
        }

        /// <summary>Avanza al prossimo posto occupato; non verifica le condizioni di fine turno.</summary>
        public void AdvanceTurn()
        {
            if (CurrentPlayer.WantToDeclareMissionCompleted(this))
            {
                InternalStateMachine.Fire(StateTrigger.DeclareMissionCompleted);
                return;
            }

            Events.Enqueue(new TurnEndedEvent(CurrentPlayer, TurnNumber));
            do
            {
                CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
            }
            while (Players[CurrentPlayerIndex] is null);
            TurnNumber++;

            if(TurnNumber < MAX_TURNS)
            {
                InternalStateMachine.Fire(StateTrigger.EndTurn);
            }
            else 
            {
                Events.Enqueue(new MaxTurnsReachedEvent(MAX_TURNS));
                InternalStateMachine.Fire(StateTrigger.EndGame);
            }
        }

        private Pawn[] CreatePawns(PlayerColor color, Random random)
        {
            // check inutile, ma lo tengo per sicurezza
            if (color == PlayerColor.Black)
                return new[] { new Pawn(color, Disguise.Ambassador, Board.CellsById[33]) };

            int[] cellIds = color switch
            {
                PlayerColor.Red => new[] { 9, 49, 44, 24 },
                PlayerColor.Blue => new[] { 26, 29, 55, 2 },
                PlayerColor.Green => new[] { 6, 47, 39, 34 },
                PlayerColor.Yellow => new[] { 10, 13, 37, 54 },
                _ => throw new ArgumentOutOfRangeException(nameof(color))
            };
            var remaining = cellIds.Select(id => Board.CellsById[id]).ToList();
            var disguises = new[] { Disguise.Tall, Disguise.Thin, Disguise.Fat, Disguise.Small };
            var pawns = new Pawn[disguises.Length];
            for (int i = 0; i < pawns.Length; i++)
                pawns[i] = new Pawn(color, disguises[i], Draw(remaining, random));
            return pawns;
        }

        public bool CelllIsEmpty(Cell cell)
        {
            if (cell is null)
                throw new ArgumentNullException(nameof(cell));
            return !Players.Any(player => player?.Pawns.Any(pawn => pawn.Position == cell) ?? false);
        }

        private Player? GetPlayerById(Identity id)
        {
            return PlayersByIdentity.TryGetValue(id, out var player) ? player : null;
        }

        private List<Player> GetEnemies(Identity id)
        {
            List<Player> result = new();
            HashSet<Identity> X_Z = new() { Identity.X, Identity.Z };
            HashSet<Identity> F_B = new() { Identity.F, Identity.B };

            if (X_Z.Contains(id))
            {
                if(PlayersByIdentity.TryGetValue(Identity.F, out var fPlayer))
                {
                    result.Add(fPlayer);
                }
                if(PlayersByIdentity.TryGetValue(Identity.B, out var bPlayer))
                {
                    result.Add(bPlayer);
                }
            }
            if(F_B.Contains(id))
            {
                if(PlayersByIdentity.TryGetValue(Identity.X, out var xPlayer))
                {
                    result.Add(xPlayer);
                }
                if(PlayersByIdentity.TryGetValue(Identity.Z, out var zPlayer))
                {
                    result.Add(zPlayer);
                }
            }
            return result;
        }

        public void DeclareLonelyMissionCompleted(Player whoDeclares)
        {
            Console.Out.WriteLine($"{whoDeclares.Name} dichiara missione compiuta da singolo");

            GameEndedEvent gameEndEvent;

            if(whoDeclares.Identity == Identity.A)
            {
                throw new ArgumentException("L'ambasciatore non deve usare questo metodo per dichiarare missione compiuta, ma DepositAmbassadorReport()!");
            }
            // Verifica!
            // 1. who declares, chi è?
            // se è l'ambasciatore fai un'altra verifica
            Identity lonely = whoDeclares.Identity;
            Identity partner = RulesEngine.GetPartnerOf(lonely);
            Player? realPartner = GetPlayerById(partner);
            // sei solo, quindi il tuo vero partner deve essere null.
            if (realPartner != null)
            {
                StringBuilder sb = new();
                sb.Append("Mission Failed!");
                sb.AppendJoin(" and ", GetEnemies(lonely));
                sb.Append(" win!");
                Console.Out.WriteLine(sb);

                gameEndEvent = new GameEndedEvent( GetEnemies(lonely) );
            }
            else
            {
                Mission m = Mission.GetMissionForPlayerAlone(lonely, this);
                if (m.VerifyVictoryConditions(this))
                {
                    Console.Out.WriteLine($"Mission Completed! {whoDeclares.Name} wins, fleeing successfully!");
                    gameEndEvent = new GameEndedEvent(new () { whoDeclares });
                }
                else
                {
                    StringBuilder sb = new();
                    sb.Append("Mission Failed!");
                    sb.AppendJoin(" and ", GetEnemies(lonely));
                    sb.Append(" win!");
                    Console.Out.WriteLine(sb);
                    gameEndEvent = new GameEndedEvent(GetEnemies(lonely));
                }
            }
            GameOver = true;
            Events.Enqueue(gameEndEvent);
        }

        public void DeclareMissionComplete(Player whoDeclares, Player declaredPartner)
        {
            GameEndedEvent geev;
            if(whoDeclares == null && declaredPartner == null)
            {
                throw new ArgumentNullException("Non puoi dichiarare una missione compiuta con un partner null!");
            }

            if(whoDeclares!.Identity == Identity.A || declaredPartner.Identity == Identity.A)
            {
                throw new ArgumentException("L'ambasciatore non deve usare questo metodo per dichiarare missione compiuta, ma DepositAmbassadorReport()!");
            }

            if(Mission.AmbassadorMissionIsCompleted(this))
            {
                StringBuilder sb = new();
                sb.Append($"L'Ambasciatore vi ha smascherati!{AmbassadorPlayer!.Name} vince!");
                Console.Out.WriteLine(sb);

                geev = new GameEndedEvent(new() { AmbassadorPlayer });
                
                GameOver = true;
                Events.Enqueue(geev);
                return;
            }

            Console.Out.WriteLine($"{whoDeclares!.Name} dichiara Missione Compiuta con {declaredPartner!.Name}");
            Player? realPartner = GetPlayerById(RulesEngine.GetPartnerOf(whoDeclares.Identity));

            Mission realMission = Mission.GetMission(whoDeclares, realPartner, this);
            if (realMission.VerifyVictoryConditions(this))
            {
                StringBuilder sb = new();
                sb.Append("Mission Completed!");
                sb.AppendJoin(" and ", new[]{ whoDeclares.Name,declaredPartner.Name});
                sb.Append(" win!");
                Console.Out.WriteLine(sb);
                geev = new GameEndedEvent(new() { whoDeclares, declaredPartner });
            }
            else
            {
                StringBuilder sb = new();
                sb.Append("Mission Completed!");
                sb.AppendJoin(" and ", GetEnemies(whoDeclares.Identity));
                sb.Append(" win!");
                Console.Out.WriteLine(sb);
                geev = new GameEndedEvent(GetEnemies(whoDeclares.Identity));
            }

            foreach(Player? p in Players)
            {
                if (p == null) continue;
                StringBuilder sb = new();
                sb.Append($"{p.Name}, {p.Color}, {p.Identity}, {p.Disguise}, {p.Mission} - ");
                sb.AppendJoin(", ", p.Pawns);
                Console.Out.WriteLine(sb);
            }
            if(AmbassadorPlayer == null)
            {
                Console.Out.WriteLine($"AmbassadorPawn: {AmbassadorPawn}");
            }
            Events.Enqueue(geev);
            GameOver = true;
        }

        private static int GenerateSeed()
        {
            var bytes = new byte[sizeof(int)];
            using (var generator = RandomNumberGenerator.Create())
                generator.GetBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        private static T Draw<T>(List<T> remaining, Random random)
        {
            int index = random.Next(remaining.Count);
            T value = remaining[index];
            remaining.RemoveAt(index);
            return value;
        }
    }
}

