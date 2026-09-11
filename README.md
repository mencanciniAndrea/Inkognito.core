# Inkognito.Core

Libreria C# destinata anche a Unity 6, con target .NET Standard 2.1,
linguaggio C# 9 e nullable reference types abilitati.

## Requisiti

- SDK .NET 10 per compilare la soluzione. Il target della DLL resta .NET Standard 2.1.
- Un editor per C#, ad esempio Visual Studio o Visual Studio Code.

## Compilazione

Dalla cartella del repository:

```powershell
dotnet build Inkognito.Core.slnx
```

Per compilare in configurazione Release:

```powershell
dotnet build Inkognito.Core.slnx --configuration Release
```

## Sviluppo

`GameState` accetta un array di esattamente 5 elementi, corrispondenti ai posti
Red, Blue, Green, Yellow, Black. Un nome null, vuoto o composto solo da spazi
indica un posto assente. Servono almeno 3 giocatori; Black può essere presente
solo insieme a tutti e quattro i giocatori colorati.
`Players` conserva i cinque posti, con null per quelli assenti: durante
l'iterazione occorre saltarli. Ogni `Player` espone anche `Identity`, `Disguise` e `Mission`.
I giocatori colorati ricevono estrazioni indipendenti senza ripetizioni da
F/B/X/Z, Tall/Thin/Fat/Small e Alfa/Bravo/Charlie/Delta.
Black riceve sempre A, Ambassador e Zero.

`TurnNumber` parte da 1. Il primo giocatore viene estratto uniformemente tra
i presenti usando il generatore della partita, quindi è riproducibile dal seed.
`CurrentPlayerIndex` indica il suo posto nella lista `Players` (da 0 a 4),
mentre `CurrentPlayer` restituisce il riferimento al giocatore.
`AdvanceTurn()` passa al posto occupato successivo, salta quelli vuoti e riparte
dall'inizio dopo l'ultimo, incrementando il numero di turno a ogni mano.
Questo metodo aggiorna solo il contatore e il giocatore corrente: le condizioni
per terminare la mano e l'esecuzione delle mosse non sono ancora implementate.
Il costruttore inizializza la partita e restituisce il controllo senza giocare.
`RollTurn()` chiama `CurrentPlayer.PlayTurn(this)` una sola volta, poi
`AdvanceTurn()`. Dopo la chiamata, numero di turno e giocatore corrente indicano
la prossima mano. Il ciclo della partita resta sotto il controllo del chiamante.

La selezione delle mosse è gestita da `ProphecyPhantom`: `Player.PlayTurn(gameState)`
estrae tre valori e li conserva in `Player.AvailableMoves`, inizialmente vuota.
L'ultima estrazione resta consultabile sul giocatore fino alla sua prossima mano.
L'estrazione usa il generatore della partita e avviene senza reinserimento da
`[None, None, None, Land, Land, Water, Water, LandOrWater, Ambassador, Ambassador]`.
Le dieci voci sono ripristinate a ogni estrazione. Leggere `AvailableMoves`
non ripete l'estrazione. L'esecuzione delle mosse verrà implementata in seguito.

Ogni `Player` ha un `Type` (`PlayerType.Human` o `PlayerType.CPU`), inizialmente
Human. `SetPlayerType(indicePosto, tipo)` configura il controllo dei posti occupati.
Per ora `PlayTurn` esegue solo l'estrazione e mantiene i placeholder delle altre
fasi, senza distinguere Human e CPU. Le precedenti API di scelta sul GameState
sono state rimosse; input umano, Brain e movimento verranno collegati in seguito.

`DumpState()` stampa seed, turno, giocatore corrente, dettagli dei giocatori,
ultime estrazioni e posizioni dei pedoni. `DumpState(TextWriter)` permette di
scrivere in un buffer o in un file senza dipendere dalla console. È un dump di
debug completo, che include anche identità e missioni segrete.

```csharp
game.DumpState();
game.RollTurn();
game.DumpState();
```

Ogni giocatore espone `Pawns`, una lista in sola lettura di pedoni del proprio
colore. I giocatori colorati hanno quattro pedoni con travestimenti Tall, Thin,
Fat e Small, indipendenti dal travestimento del giocatore. Black ha un solo
pedone Ambassador. Ogni `Pawn` espone `Color`, `Disguise` e `Position`, un
riferimento non nullable a una `Cell` del tabellone della partita.
Le celle iniziali sono assegnate senza ripetizioni usando lo stesso generatore
casuale della partita: Red 9/49/44/24, Blue 26/29/55/2, Green 6/47/39/34,
Yellow 10/13/37/54. Ambassador parte sempre dalla cella 33.
Lo stesso seed e gli stessi posti occupati riproducono anche le posizioni iniziali
nello stesso ambiente e con la stessa versione delle regole.

Ogni partita creata passando solo i nomi genera un seed casuale, disponibile in
`GameState.Seed`. Per riprodurre le assegnazioni nello stesso ambiente durante
test o simulazioni, si può fornire quel seed:

```csharp
var game = new GameState(new string?[] { "Andrea", "Marco", "Giulia", null, null }, seed: 42);
```

Resta disponibile il costruttore che accetta un `Random` esterno: in quel caso
`Seed` è null, perché non è possibile ricavarlo dal generatore fornito.

I test sono nel progetto adiacente `Inkognito.tests`: eseguire `dotnet test`
da quella cartella.

## Tabellone

Ogni `GameState` carica il proprio `Board` da `board.yaml`, incorporato nella
DLL come risorsa. Il caricamento non dipende dalla directory corrente;
dopo una modifica al YAML occorre ricompilare la libreria.

- `Board.Cells` e `Board.Edges` espongono liste in sola lettura.
- `Cell` contiene `Id` e gli archi incidenti in `Edges`.
- `Edge` contiene `Id`, `From`, `To` e `Type` (`EdgeType.LAND` o `EdgeType.WATER`).
- `From` e `To` sono riferimenti alle celle del board; lo stesso arco è
  presente nelle liste di entrambi gli estremi.

```csharp
var game = new GameState(new string?[] { "Andrea", "Marco", "Giulia", null, null });
Board board = game.Board;
Cell destination = board.Edges[0].To;
```

`Board.LoadDefault()` carica il tabellone incorporato; `Board.FromYaml(string)`
permette di caricare altri grafi nello stesso formato. Il loader verifica la
versione 1, gli ID unici, gli estremi esistenti, i tipi e la corrispondenza tra
`cells[].edges` e gli estremi in `edges`. I dati non validi generano
`InvalidDataException`.

Il parsing usa [YamlDotNet 16.3.0](https://www.nuget.org/packages/YamlDotNet/16.3.0).
Importando manualmente il core in Unity, includere anche la DLL di YamlDotNet.

## Organizzazione del codice

Aggiungi i file `.cs` nella cartella del progetto o in sottocartelle, usando
il namespace `Inkognito.Core`. I file vengono inclusi automaticamente nella compilazione.

Gli `using` vanno dichiarati esplicitamente nei file sorgenti.

Il progetto è una libreria: produce una DLL e non ha un punto di ingresso eseguibile.

## Compatibilità con Unity 6

Il target `netstandard2.1` corrisponde al profilo API predefinito di Unity 6.
Anche le eventuali dipendenze devono essere compatibili con quel profilo.
Mantieni la logica core indipendente da `UnityEngine`.

Dopo la compilazione Release, la DLL si trova in
`bin/Release/netstandard2.1/Inkognito.Core.dll` e può essere importata nel progetto Unity.
La compatibilità effettiva va verificata sulle piattaforme di destinazione, anche con
IL2CPP se utilizzato: il solo target framework non garantisce la compatibilità di ogni dipendenza.

Riferimenti: [profili API di Unity 6](https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html)
e [compilatore C# di Unity 6](https://docs.unity3d.com/6000.0/Documentation/Manual/csharp-compiler.html).
