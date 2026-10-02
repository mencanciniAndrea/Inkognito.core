using Inkognito.Core.Brains;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        public IReadOnlyList<MoveType> AvailableMoves { get; private set; } = Array.Empty<MoveType>();

        public PlayerObjective CurrentObjective { get; private set; }

        /// <summary>
        /// Questa è generale: data un'identità, si sa l'identità del partner. Solo che all'inizio del gioco non si sa quale giocatore è il partner.
        /// </summary>
        private Identity MyPartnerIdentity { get; }

        public Mission? MissionToComplete { get; set; }

        public Player? MyPartner { get; set; }
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

        public void PlayTurn(GameState gameState)
        {
            if (gameState is null)
                throw new ArgumentNullException(nameof(gameState));
            if (!ReferenceEquals(gameState.CurrentPlayer, this))
                throw new InvalidOperationException("Può giocare soltanto il giocatore corrente della partita.");

            bool IAmAmbassador = Identity == Identity.A;
            AvailableMoves = IAmAmbassador ? new List<MoveType> { MoveType.Ambassador, MoveType.Ambassador } :
                gameState.prophecyPhantom.DrawMoves();

            StringBuilder sb = new();
            sb.Append("Mosse disponibili: {");
            sb.AppendJoin(", ",AvailableMoves);
            sb.Append("}");
            Console.Out.WriteLine($"{sb}");

            // fase 2: scegliere le mosse disponibili. Qui se il giocatore è umano bisogna trovare il modo di recuperare l'input
            // se invece è CPU, si chiama il suo Brain
            Plan movementsPlan = Brain.GetBestMovePlan(gameState, AvailableMoves, TurnPhase.Move);

            sb = new();
            sb.Append($"{Name} segue: ");
            sb.AppendJoin(", ", movementsPlan.Moves);
            Console.Out.WriteLine($"{sb}");
            foreach (var move in movementsPlan.Moves)
            {
                gameState.Board.ApplyMove(move);
            }

            // Fase 4.5: decidi se dichiarare la missione compiuta (ora sai qualcosa in più di prima, magari devi andare con la tua pedina su chi hai appena interrogato)
            if (Brain.ShouldDeclareMissionCompleted(this, gameState))
            {
                Console.Out.WriteLine($"{Name} dichiara Missione Compiuta con partner: {MyPartner}");
                DeclareMissionCompleted(gameState);
                if(Identity != Identity.A)
                {
                    return;
                }
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
                PlayerColor pColorToAsk = p.Color;
                // 2. a chi chiedo cosa? Cervello, aiutami tu...
                if(p == gameState.AmbassadorPawn)
                {
                    // scegli il giocatore a cui chiedere, tra quelli disponibili (che non sia black!
                    Player? [] availablePlayers = gameState.Players.Where(p => p != null && p.Color != PlayerColor.Black && p.Color != this.Color).ToArray();

                    pColorToAsk = Brain.WhoToAskInfoBetween(availablePlayers!, _random);
                }

                var reqType = Brain.WhatToRequestTo(pColorToAsk, Memory!, _random);

                // 3. ask information to the player (crea la request e mandagliela)
                PlayerInfoRequest request = new (Color, pColorToAsk, reqType, p.Color == PlayerColor.Black);

                Player? otherPlayer = gameState.GetPlayerByColor(pColorToAsk) ?? throw new ArgumentNullException($"Impossibile decidere il giocatore a cui chiedere le informazioni!!! Seed partita: {gameState.Seed}");

                var answer = otherPlayer.Ask(request);

                // regola del ruleset 2022: se l'ambasciatore è un giocatore giocante, deve vedere le carte della richiesta fatta attraverso di lui
                if(gameState.AmbassadorPlayer != null && request.ThroughAmbassador)
                {
                    Console.Out.WriteLine($"{Name} ha chiesto a {otherPlayer.Name} tramite l'ambasciatore {gameState.AmbassadorPlayer.Name} - faccio sapere la risposta all'ambasciatore");
                    gameState.AmbassadorPlayer.Brain.ManageAnswer(gameState.AmbassadorPlayer, answer, gameState);
                    Console.Out.WriteLine($"{gameState.AmbassadorPlayer.Name} ora sa: {gameState.AmbassadorPlayer.Memory!.GetPlayerKnowledge(pColorToAsk)}");
                }

                String come = request.ThroughAmbassador ? "tramite ambasciatore " : "direttamente";

                Console.Out.WriteLine($"{Name} ({Identity},{Disguise}) chiede {reqType} a {otherPlayer.Name} ({otherPlayer.Identity}, {otherPlayer.Disguise}) {come}: {answer}");

                Console.Out.WriteLine($"{Name} sapeva di {otherPlayer.Name}: {Memory!.GetPlayerKnowledge(pColorToAsk)}");


                // 4. add answer to the memory
                Brain.ManageAnswer(this, answer, gameState);

                Console.Out.WriteLine($"{Name} ora sa: {Memory!.GetPlayerKnowledge(pColorToAsk)}");
                Console.Out.WriteLine($"{Name} deve giocare da solo, secondo lui: {Memory.IMustPlayAlone}");

                foreach(var c in answer.Answers)
                {
                    if(c.Type == InkognitoCardType.MISSION)
                    {
                        Console.Out.WriteLine($"{otherPlayer.Name} ha notificato la sua missione {(MissionPart) c.Value} a {Name}");
                        // TODO Se ti fidi...
                        MissionToComplete = Inkognito.Core.Mission.GetMission(this, otherPlayer, gameState);
                        if(MissionToComplete != null)
                        {
                            Console.Out.WriteLine($"{Name} e {otherPlayer.Name} hanno come missione comune: {MissionToComplete.Description}");
                        }
                    }
                }

                // Fase 4.5: decidi se dichiarare la missione compiuta (ora sai qualcosa in più di prima, magari devi andare con la tua pedina su chi hai appena interrogato)
                if (Brain.ShouldDeclareMissionCompleted(this, gameState))
                {
                    Console.Out.WriteLine($"{Name} dichiara Missione Compiuta con partner: {MyPartner}");
                    DeclareMissionCompleted(gameState);
                    if (Identity != Identity.A)
                    {
                        break;
                    }
                }

                // 5. move the pawn somewhere else and apply move into the general gamestate
                Plan dismissionPlan = Brain.DismissPawn(p, gameState, this, Memory!);
                sb.Clear();
                sb.Append($"{Name} manda via {p}: ");
                sb.AppendJoin(", ", dismissionPlan.Moves);

                Console.Out.WriteLine(sb);
                foreach (var move in dismissionPlan.Moves)
                {
                    gameState.Board.ApplyMove(move);
                }

                // valuta se dichiarare missione compiuta
                if (Brain.ShouldDeclareMissionCompleted(this, gameState))
                {
                    DeclareMissionCompleted(gameState);
                    if (Identity != Identity.A)
                    {
                        break;
                    }
                }
            }

            if (!gameState.GameOver)
            {
                if (!RulesEngine.IsGameBoardStateLegal(gameState.Board, this, TurnPhase.Expulsion))
                {
                    throw new ArgumentException($"Errore! stato del gioco non legale! {gameState.Board}");
                }
                // Fase 6: decidi se dichiarare la missione compiuta (ora sai qualcosa in più di prima, magari devi mandare l'ambasciatore o un player da qualche parte)
                if (Brain.ShouldDeclareMissionCompleted(this, gameState)) DeclareMissionCompleted(gameState);

                // fase 5: si termina il turno, dichiarando "endTurn" e lasciando il controllo al GameState
                EndTurn();
            }
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

    }
}
