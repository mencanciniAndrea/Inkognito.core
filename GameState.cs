using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Inkognito.Core
{
    public sealed class GameState
    {
        public readonly ProphecyPhantom prophecyPhantom;
        public Board Board { get; }
        public int TurnNumber { get; private set; } = 1;
        /// <summary>Indice del posto corrente nella lista Players, inclusi i posti vuoti.</summary>
        public int CurrentPlayerIndex { get; private set; }
        public Player CurrentPlayer => Players[CurrentPlayerIndex]!;
        /// <summary>Cinque posti: Red, Blue, Green, Yellow, Black. I posti assenti sono null.</summary>
        public IReadOnlyList<Player?> Players { get; }
        /// <summary>Seed della partita; null se viene fornito direttamente un Random esterno.</summary>
        public int? Seed { get; }

        public Pawn AmbassadorPawn { get; } = null!;

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
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Debug);
            });

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
            var missions = new List<Mission> { Mission.Alfa, Mission.Bravo, Mission.Charlie, Mission.Delta };

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
            List<Pawn> pawns = new List<Pawn> { AmbassadorPawn};

            var players = new Player?[playerNames.Length];
            for (int i = 0; i < playerNames.Length; i++)
            {
                var playerName = playerNames[i];
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    continue;
                }
                players[i] = colors[i] == PlayerColor.Black
                    ? new Player(playerName!, colors[i], Identity.A, Disguise.Ambassador, Mission.Zero,
                        new[] { AmbassadorPawn }, loggerFactory)
                    : new Player(playerName!, colors[i],
                        Draw(identities, random), Draw(disguises, random), Draw(missions, random),
                        CreatePawns(colors[i], random), loggerFactory);
                pawns.AddRange(players[i]!.Pawns);
            }
            Board.Pawns = pawns;

            // Scelta del giocatore che inizia il turno: tra i posti occupati, uno a caso.
            Players = Array.AsReadOnly(players);
            var occupiedSlots = Enumerable.Range(0, Players.Count)
                .Where(index => Players[index] is not null).ToArray();

            CurrentPlayerIndex = occupiedSlots[random.Next(occupiedSlots.Length)];
            //TEST: partiamo sempre dallo yellow. Mi ha dato problemi con una configurazione di mosse WWA
            CurrentPlayerIndex = 3;


            // Creazione del ProphecyPhantom
            prophecyPhantom = new ProphecyPhantom(random);
        }

        public Player? GetPlayerByColor(PlayerColor color)
        {
            if(color == PlayerColor.Black)
            {
                //TODO! In questo caso bisogna restituire il giocatore ambasciatore
            }
            return Players.Single(p => p.Color == color);
        }

        /// <summary>Esegue un singolo turno e passa al giocatore successivo.</summary>
        public void PlayTurn()
        {
            CurrentPlayer.PlayTurn(this);
        }

        /// <summary>Stampa lo stato completo di debug, comprese le informazioni segrete dei giocatori.</summary>
        public void DumpState() => DumpState(Console.Out);

        public void DumpState(TextWriter writer)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));
            writer.WriteLine($"Seed: {Seed?.ToString() ?? "non disponibile (Random esterno)"}");
            writer.WriteLine($"Turno {TurnNumber} - Giocatore corrente: {CurrentPlayer.Name} ({CurrentPlayer.Color})");
            foreach (var player in Players)
            {
                if (player is null)
                    continue;
                writer.WriteLine($"{player.Name}: Type={player.Type}, Color={player.Color}, Identity={player.Identity}, Disguise={player.Disguise}, Mission={player.Mission}");
                if (player.AvailableMoves.Count > 0)
                    writer.WriteLine($"Ultima estrazione di {player.Name}: {string.Join(", ", player.AvailableMoves)}");
                foreach (var pawn in player.Pawns)
                    writer.WriteLine($"{pawn.Disguise}-{pawn.Color}: {pawn.Position.Id}");
            }
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

