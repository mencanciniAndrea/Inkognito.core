using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public PlayerMemory? Memory { get; internal set; } = null;

        //----------------------------------------------------------------------
        //
        // Functions
        //
        //----------------------------------------------------------------------

        internal Player(string name, PlayerColor color, Identity identity, Disguise disguise, MissionPart mission, Pawn[] pawns, ILoggerFactory loggerFactory)
        {
            Name = name;
            Color = color;
            Identity = identity;
            Disguise = disguise;
            Mission = mission;
            Pawns = Array.AsReadOnly(pawns);
            Brain = new LazyBrain(loggerFactory); //TODO: non ci deve essere solo un LazyBrain!!!
            _logger = loggerFactory.CreateLogger<Player>();
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
                // TODO: gestire il caso di mosse dell'ambasciatore come giocatore. Basta dire: moves = {Move.Ambassador, Move.Ambassador}?
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

                _logger.LogDebug($"Applico le mosse del piano {plan}");
                foreach ( var move in plan.Moves)
                {
                    gameState.Board.ApplyMove(move);
                }

                // fase 3: eseguite le mosse, si ottiene una lista di altri PEDONI (non player!) a cui chiedere le informazioni.
                // l'esecuzione delle mosse infatti è finalizzata ad ottenere questa lista oppure a spostare i propri pedoni.

                // Nota: può contenere l'ambasciatore e pedine colorate. Massimo 3, ma è un dettaglio
                List<Pawn> pawnList = new List<Pawn>();
                foreach ( var pawn in Pawns)
                {
                    Cell currentCell = pawn.Position;
                    var pawns = gameState.Board.GetPawnsOnCell(currentCell);
                    // aggiungi tutti i pedoni sulla cella, filtrando i pedoni del current player 
                    pawnList.AddRange(gameState.Board.GetPawnsOnCell(currentCell).Where(pawn => pawn.Color != Color).ToList());
                }

                // Sort pawnList
                var sortedPawnList = Brain.SortQuerablePawnList(pawnList, Memory);

                // fase 4: se la lista di player a cui chiedere informazioni non è vuota, si chiede a ciascuno di loro le informazioni richieste del tipo richiesto
                foreach(Pawn p in sortedPawnList)
                {
                    Player? otherPlayer = gameState.GetPlayerByColor(p.Color);

                    if (otherPlayer == null)
                    {
                        throw new ArgumentNullException("Impossibile decidere il giocatore a cui chiedere le informazioni!!!");
                    }


                    // 2. decide which information to ask, based on what you know about him and others
                    RequestType reqType = Brain.WhatToRequestTo(otherPlayer.Color, Memory);
                    // 3. ask information to the player (crea la request e mandagliela)
                    // 4. add answer to the memory
                    // 5. move the pawn somewhere else and apply move into the general gamestate
                }

                // fase 5: si termina il turno, dichiarando "endTurn" e lasciando il controllo al GameState
                // TODO: chiudi il turno
            }

            
        }

    }
}
