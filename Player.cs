using System;
using System.Collections.Generic;
using System.Linq;

namespace Inkognito.Core
{
    public sealed class Player
    {
        public string Name { get; }
        public PlayerType Type { get; internal set; } = PlayerType.Human;
        public PlayerColor Color { get; }
        public Identity Identity { get; }
        public Disguise Disguise { get; }
        public Mission Mission { get; }
        public IReadOnlyList<Pawn> Pawns { get; }
        public IReadOnlyList<MoveType> AvailableMoves { get; private set; } = Array.Empty<MoveType>();

        public Brain Brain { get; internal set; } = null!;


        internal Player(string name, PlayerColor color, Identity identity, Disguise disguise, Mission mission, Pawn[] pawns)
        {
            Name = name;
            Color = color;
            Identity = identity;
            Disguise = disguise;
            Mission = mission;
            Pawns = Array.AsReadOnly(pawns);
            Brain = new Brain();
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
                Console.Out.WriteLine($"Mosse disponibili:");
                foreach (var move in AvailableMoves)
                {
                    // qui si potrebbe fare un controllo per vedere se il giocatore ha abbastanza pedoni per fare la mossa, ma per ora lo facciamo dopo
                    Console.Out.WriteLine($"{move},");
                }

                // fase 2: scegliere le mosse disponibili. Qui se il giocatore è umano bisogna trovare il modo di recuperare l'input
                // se invece è CPU, si chiama il suo Brain
                var plans = Brain.PlanMoves(gameState, AvailableMoves, TurnPhase.Move);

                

                // fase 3: eseguite le mosse, si ottiene una lista di altri player a cui chiedere le informazioni.
                // l'esecuzione delle mosse infatti è finalizzata ad ottenere questa lista oppure a spostare i propri pedoni.

                // fase 4: se la lista di player a cui chiedere informazioni non è vuota, si chiede a ciascuno di loro le informazioni richieste del tipo richiesto

                // fase 5: si termina il turno, dichiarando "endTurn" e lasciando il controllo al GameState
            }
        }
    }
}
