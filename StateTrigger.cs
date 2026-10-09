using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public enum StateTrigger
    {
        StartGame,
        DecideMoves,
        ApplyMove,
        DecideQuery,
        Query,
        DecideAnswer,
        AnswerDelivered,
        AnswerNoted,
        DismissPawn,
        EndTurn,
        DeclareMissionCompleted,
        AskMissionPartner,
        MissionCompletedAlone,
        ReplyToMissionParnter,
        EndGame,
        ReplyAgain
    }
}
