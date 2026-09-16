using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inkognito.Core
{
    public sealed class Player
    {

        private readonly ILogger<Player> _logger;

        public string Name { get; }
        public PlayerType Type { get; internal set; } = PlayerType.Human;
        public PlayerColor Color { get; }
        public Identity Identity { get; }
        public Disguise Disguise { get; }
        public Mission Mission { get; }
        public IReadOnlyList<Pawn> Pawns { get; }
        public IReadOnlyList<MoveType> AvailableMoves { get; private set; } = Array.Empty<MoveType>();

        public IBrain Brain { get; internal set; } = null!;

        public PlayerMemory Memory { get; internal set; } = null;


        internal Player(string name, PlayerColor color, Identity identity, Disguise disguise, Mission mission, Pawn[] pawns, ILoggerFactory loggerFactory)
        {
            Name = name;
            Color = color;
            Identity = identity;
            Disguise = disguise;
            Mission = mission;
            Pawns = Array.AsReadOnly(pawns);
            Brain = new LazyBrain(loggerFactory);
            _logger = loggerFactory.CreateLogger<Player>();
        }

        public void InitMemory(List<Player> allPlayers)
        {
            Memory = new PlayerMemory(allPlayers.Where(p => p.Color != Color));
        }

       

        public void PlayTurn(GameState gameState)
        {
            if (gameState is null)
                throw new ArgumentNullException(nameof(gameState));
            if (!ReferenceEquals(gameState.CurrentPlayer, this))
                throw new InvalidOperationException("Può giocare soltanto il giocatore corrente della partita.");

            if (Identity == Identity.A)
            {
                // Ambasciatore. Il giocatore può muoversi di 1 o 2 spazi, a prescindere dal tipo, ed incontrare solo un altro giocatore. Se incontra un altro giocatore, può chiedere informazioni di qualsiasi tipo.
                // lo implementiamo dopo, tanto nel ruleset v1 non c'è la possibilità di giocare come ambasciatore. Per ora, se il giocatore è ambasciatore, non può fare nulla.
            }
            else
            {
                // fase 1: recuperare le mosse disponibili per il giocatore corrente
                AvailableMoves = gameState.prophecyPhantom.DrawMoves();
                _logger.LogDebug($"Mosse disponibili:");
                foreach (var move in AvailableMoves)
                {
                    // qui si potrebbe fare un controllo per vedere se il giocatore ha abbastanza pedoni per fare la mossa, ma per ora lo facciamo dopo
                    _logger.LogDebug($"{move},");
                }

                // fase 2: scegliere le mosse disponibili. Qui se il giocatore è umano bisogna trovare il modo di recuperare l'input
                // se invece è CPU, si chiama il suo Brain
                var plan = Brain.ChoosePlan(gameState, AvailableMoves, TurnPhase.Move);

                // TODO: applica le mosse alla game board.
                _logger.LogDebug($"Applico le mosse del piano {plan}");
                foreach ( var move in plan.Moves)
                {
                    gameState.Board.ApplyMove(move);
                }

                // fase 3: eseguite le mosse, si ottiene una lista di altri pedoni a cui chiedere le informazioni.
                // l'esecuzione delle mosse infatti è finalizzata ad ottenere questa lista oppure a spostare i propri pedoni.

                // Nota: può contenere l'ambasciatore e pedine colorate. Massimo 3, ma è un dettaglio
                List<Pawn> pawnList = new List<Pawn>();
                foreach ( var pawn in Pawns)
                {
                    Cell currentCell = pawn.Position;
                    var pawns = gameState.Board.GetPawnsOnCell(currentCell);

                    foreach(var p in pawns)
                    {
                        if (p.Color != Color)
                        {
                            pawnList.Add(p);
                        }
                    }
                }

                // Sort pawnList!
                // questo passaggio è importante, ed è definito dal Brain: Infatti, solo lui sa come ordinare la lista dei pawn da interrogare.
                // Inoltre, l'ordine di interrogazione è fondamentale per non perdere tempo in certi casi.
                // L'ordinamento tuttavia, dipende dalla conoscenza del Player, che potrebbe tranquillamente risiedere nel Brain, se si vuole
                // fare in modo che il Brain sia il centro anche della memoria (cosa che normalmente è in un essere umano normale...)

                // Soluzione delle ipotesi e della knowledge: Fare tutte le combinazioni possibili su identità e travestimento di un giocatore:
                // (Z, alto) (Z, basso)... e poi eliminare quelle combinazioni che rendono falsa una eventuale risposta data con le carte
                // Brainstorming: Secondo me qui si potrebbe fare tipo IssueRequest, e una Request può essere di tipo NORMAL o FROM_AMBASSADOR, per differenziare quante carte bisogna mostrare
                // Una Request è caratterizzata da chi la fa, chi la riceve, quante carte bisogna scambiare.

                // fase 4: se la lista di player a cui chiedere informazioni non è vuota, si chiede a ciascuno di loro le informazioni richieste del tipo richiesto
                // TODO: chiedi informazioni ai giocatori, che ti daranno le loro carte
                // TODO: prendi nota

                // fase 5: si termina il turno, dichiarando "endTurn" e lasciando il controllo al GameState
                // TODO: chiudi il turno
            }
        }
    }
}
