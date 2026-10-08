using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public enum GameInternalState
    {

        SettingUp,
        Playing,
        TurnStarted,
        DecidingMove,
        ApplyingMoves,
        DecidingQuery,
        AwaitingAnswer,
        DecidingAnswer,
        ElaboratingAnswer,
        DismissingPawn,
        PawnDismissed,
        EndingTurn,
        DeclaringMissionComplete,
        AskingMissionToPartner,
        EvaluatingMission,
        GameOver
    }
}
