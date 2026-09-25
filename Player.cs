using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Inkognito.Core
{
    public sealed class Player
    {
        //----------------------------------------------------------------------
        //
        // Properties
        //
        //----------------------------------------------------------------------

        private readonly ILogger<Player> _logger;

        private Random random;

        public string Name { get; }
        public PlayerType Type { get; internal set; } = PlayerType.Human;
        public PlayerColor Color { get; }
        public Identity Identity { get; }
        public Disguise Disguise { get; }
        public MissionPart Mission { get; }
        public IReadOnlyList<Pawn> Pawns { get; }
        public IReadOnlyList<MoveType> AvailableMoves { get; private set; } = Array.Empty<MoveType>();

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

        internal Player(string name, PlayerColor color, Identity identity, Disguise disguise, MissionPart mission, Pawn[] pawns, ILoggerFactory loggerFactory, Random r)
        {
            Name = name;
            Color = color;
            Identity = identity;
            Disguise = disguise;
            Mission = mission;
            Pawns = Array.AsReadOnly(pawns);
            Brain = new LazyBrain(loggerFactory); //TODO: non ci deve essere solo un LazyBrain!!!
            _logger = loggerFactory.CreateLogger<Player>();
            random = r;

        }

        public void InitMemory(List<Player> allPlayers)
        {
            Memory = new PlayerMemory(allPlayers.Where(p => p.Color != Color), this);
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
                AvailableMoves = new List<MoveType>{ MoveType.Ambassador, MoveType.Ambassador};
            }
            else
            {
                // fase 1: recuperare le mosse disponibili per il giocatore corrente
                AvailableMoves = gameState.prophecyPhantom.DrawMoves();
            }

            _logger.LogDebug($"Mosse disponibili:");
            foreach (var move in AvailableMoves)
            {
                // qui si potrebbe fare un controllo per vedere se il giocatore ha abbastanza pedoni per fare la mossa, ma per ora lo facciamo dopo
                _logger.LogDebug($"{move},");
            }

            // fase 2: scegliere le mosse disponibili. Qui se il giocatore è umano bisogna trovare il modo di recuperare l'input
            // se invece è CPU, si chiama il suo Brain
            Plan movementsPlan = Brain.GetBestMovePlan(gameState, AvailableMoves, TurnPhase.Move);

            _logger.LogDebug($"Applico le mosse del piano {movementsPlan}");
            foreach (var move in movementsPlan.Moves)
            {
                gameState.Board.ApplyMove(move);
            }

            // fase 3: eseguite le mosse, si ottiene una lista di altri PEDONI (non player!) a cui chiedere le informazioni.
            // l'esecuzione delle mosse infatti è finalizzata ad ottenere questa lista oppure a spostare i propri pedoni.

            // Nota: può contenere l'ambasciatore e pedine colorate. Massimo 3.
            List<Pawn> pawnList = new ();
            foreach (var pawn in Pawns)
            {
                Cell currentCell = pawn.Position;
                var pawns = gameState.Board.GetPawnsOnCell(currentCell);
                // aggiungi tutti i pedoni sulla cella, filtrando i pedoni del current player 
                pawnList.AddRange(gameState.Board.GetPawnsOnCell(currentCell).Where(pawn => pawn.Color != Color).ToList());
            }

            // Sort pawnList
            var sortedPawnList = Brain.SortQuerablePawnList(pawnList, Memory!);

            // Fase 3.5: decidi se dichiarare la missione compiuta

            // fase 4: se la lista di player a cui chiedere informazioni non è vuota, si chiede a ciascuno di loro le informazioni richieste del tipo richiesto
            foreach (Pawn p in sortedPawnList)
            {
                // 2. a chi chiedo cosa? Cervello, aiutami tu...
                var (otherPlayerColor, reqType) = Brain.WhatToRequestTo(p.Color, Memory!, random);

                // 3. ask information to the player (crea la request e mandagliela)
                PlayerInfoRequest request = new (Color, otherPlayerColor, reqType, p.Color == PlayerColor.Black);

                Player? otherPlayer = gameState.GetPlayerByColor(otherPlayerColor);

                if (otherPlayer == null)
                {
                    throw new ArgumentNullException($"Impossibile decidere il giocatore a cui chiedere le informazioni!!! Seed partita: {gameState.Seed}");
                }

                var answer = otherPlayer.Ask(request);

                // 4. add answer to the memory
                Brain.ManageAnswer(answer, Memory!);

                // Fase 4.5: decidi se dichiarare la missione compiuta (ora sai qualcosa in più di prima, magari devi andare con la tua pedina su chi hai appena interrogato)
                if (Brain.ShouldDeclareMissionCompleted(this, gameState, Memory!)) DeclareMissionCompleted();

                // 5. move the pawn somewhere else and apply move into the general gamestate
                Plan dismissionPlan = Brain.DismissPawn(p, gameState, this, Memory!);

                _logger.LogDebug($"Applico le mosse del piano {dismissionPlan}");
                foreach (var move in dismissionPlan.Moves)
                {
                    gameState.Board.ApplyMove(move);
                }

                // valuta se dichiarare missione compiuta
                if (Brain.ShouldDeclareMissionCompleted(this, gameState, Memory!)) DeclareMissionCompleted();
            }

            // Fase 6: decidi se dichiarare la missione compiuta (ora sai qualcosa in più di prima, magari devi mandare l'ambasciatore o un player da qualche parte)
            if (Brain.ShouldDeclareMissionCompleted(this, gameState, Memory!)) DeclareMissionCompleted();

            // fase 5: si termina il turno, dichiarando "endTurn" e lasciando il controllo al GameState
            EndTurn();
        }

        public PlayerAnswer Ask(PlayerInfoRequest req)
        {
            // Devo dare una risposta... che gli dico? Cervello, aiutami tu...
            return Brain.ReplyToRequest(this, req, Memory!);
        }

        /// <summary>
        /// Da usare quando decidi di dichiarare missione compiuta
        /// </summary>
        public void DeclareMissionCompleted()
        {
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

    }
}
