using Inkognito.Core.Commands;
using Inkognito.Core.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Inkognito.Core
{
    public sealed class GameState
    {

        private readonly Channel<IGameCommand> _commands;
        private readonly Channel<IGameEvent> _events;

        private Task? _gameTask;


        public readonly ProphecyPhantom prophecyPhantom;
        public Board Board { get; }
        public int TurnNumber { get; private set; } = 1;
        /// <summary>Indice del posto corrente nella lista Players, inclusi i posti vuoti.</summary>
        public int CurrentPlayerIndex { get; private set; }
        public Player CurrentPlayer => Players[CurrentPlayerIndex]!;

        public Player? AmbassadorPlayer { get; private set; }
        /// <summary>Cinque posti: Red, Blue, Green, Yellow, Black. I posti assenti sono null.</summary>
        public IReadOnlyList<Player?> Players { get; }
        /// <summary>Seed della partita; null se viene fornito direttamente un Random esterno.</summary>
        public int? Seed { get; }

        public Pawn AmbassadorPawn { get; } = null!;

        public bool GameOver { get; private set; }

        public int ActivePlayers { get; private set; }

        public AmbassadorReport? AmbassadorReport { get; private set; }

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
            List<Pawn> pawns = new List<Pawn> { };

            var players = new Player?[playerNames.Length];
            CreatePlayers(playerNames, random, identities, disguises, missions, colors, pawns, players);

            if (GetPlayerByColor(PlayerColor.Black) == null)
            {
                pawns.Add(AmbassadorPawn);
            }

            Board.Pawns = pawns;


            // Creazione del ProphecyPhantom
            prophecyPhantom = new ProphecyPhantom(random);

            // Scelta del giocatore che inizia il turno: tra i posti occupati, uno a caso.
            Players = Array.AsReadOnly(players);
            var occupiedSlots = Enumerable.Range(0, Players.Count)
                .Where(index => Players[index] is not null).ToArray();

            CurrentPlayerIndex = occupiedSlots[random.Next(occupiedSlots.Length)];
            // TODO: mandare la carta PLAYER_START al giocatore che inizia il turno

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
            foreach(Player? p in Players)
            {
                if(p != null && p.Identity == id)
                {
                    // prendi il pawn con il travestimento del player
                    foreach (Pawn pa in p.Pawns)
                    {
                        if (pa.Disguise == p.Disguise) return pa;
                    }
                }
            }
            return null;
        }

        private Player CreateAmbassadorPlayer(string name, Random random)
        {
            AmbassadorPlayer = new Player(name, PlayerColor.Black, Identity.A, Disguise.Ambassador, MissionPart.FindAllIdentities,
                        new[] { AmbassadorPawn }, random);
            return AmbassadorPlayer;
        }

        public Player? GetPlayerByColor(PlayerColor color)
        {
            if(color == PlayerColor.Black)
            {
                return AmbassadorPlayer;
            }

            return Players.Single(p => p != null && p.Color == color);
        }

        /// <summary>Esegue un singolo turno e passa al giocatore successivo.</summary>
        public void PlayTurn()
        {
            // TODO questo deve diventare un comando passato al player
            CurrentPlayer.PlayTurn(this);
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
            do
            {
                CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
            }
            while (Players[CurrentPlayerIndex] is null);
            TurnNumber++;
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
            foreach(Player? p in Players)
            {
                if (p != null && p.Identity == id) return p;
            }
            return null;
        }

        private List<Player> GetEnemies(Identity id)
        {
            List<Player> result = new();
            HashSet<Identity> X_Z = new() { Identity.X, Identity.Z };
            HashSet<Identity> F_B = new() { Identity.F, Identity.B };

            if (X_Z.Contains(id))
            {
                foreach (Player? p in Players)
                {
                    if (p != null && F_B.Contains(p.Identity))
                    {
                        result.Add(p);
                    }
                }
            }
            if(F_B.Contains(id))
            {
                foreach(Player? p in Players)
                {
                    if(p != null && X_Z.Contains(p.Identity))
                    {
                        result.Add(p);
                    }
                }
            }
            return result;
        }

        public void DeclareLonelyMissionCompleted(Player whoDeclares)
        {
            Console.Out.WriteLine($"{whoDeclares.Name} dichiara missione compiuta da singolo");

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
            }
            else
            {
                Mission m = Mission.GetMissionForPlayerAlone(lonely, this);
                if (m.VerifyVictoryConditions(this))
                {
                    Console.Out.WriteLine($"Mission Completed! {whoDeclares.Name} wins, fleeing successfully!");
                }
                else
                {
                    StringBuilder sb = new();
                    sb.Append("Mission Failed!");
                    sb.AppendJoin(" and ", GetEnemies(lonely));
                    sb.Append(" win!");
                    Console.Out.WriteLine(sb);
                }
            }
            GameOver = true;
        }

        public void DeclareMissionComplete(Player whoDeclares, Player declaredPartner)
        {
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
                GameOver = true;
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
            }
            else
            {
                StringBuilder sb = new();
                sb.Append("Mission Completed!");
                sb.AppendJoin(" and ", GetEnemies(whoDeclares.Identity));
                sb.Append(" win!");
                Console.Out.WriteLine(sb);
            }



            foreach(Player? p in Players)
            {
                if (p == null) continue;
                StringBuilder sb = new();
                sb.Append($"{p.Name}, {p.Color}, {p.Identity}, {p.Disguise}, {p.Mission} - ");
                sb.AppendJoin(", ", p.Pawns);
                Console.Out.WriteLine(sb);
            }
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

