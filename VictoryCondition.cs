using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class VictoryCondition
    {
        public Pawn Pawn { get; }
        public TargetPosition TargetPosition { get; }

        public VictoryCondition(Pawn p, TargetPosition t)
        {
            Pawn = p;
            TargetPosition = t;
        }

        public bool IsVerified(GameState g)
        {
            var currentPawnPosition = Pawn.Position.Id;

            switch (TargetPosition)
            {
                case TargetPosition.A_POSITION:
                    if (currentPawnPosition == g.AmbassadorPawn.Position.Id) return true;
                    else return false;
                case TargetPosition.F_POSITION:
                    return VerifyPositionByIdentity(Identity.F, g);
                case TargetPosition.B_POSITION:
                    return VerifyPositionByIdentity(Identity.B, g);
                case TargetPosition.X_POSITION:
                    return VerifyPositionByIdentity(Identity.X, g);
                case TargetPosition.Z_POSITION:
                    return VerifyPositionByIdentity(Identity.Z, g);
                case TargetPosition.EMBASSY:
                    return currentPawnPosition == 33;
                case TargetPosition.POSITION_1:
                    return currentPawnPosition == 5;
                case TargetPosition.POSITION_2:
                    return currentPawnPosition == 23;
                case TargetPosition.POSITION_3:
                    return currentPawnPosition == 38;
                case TargetPosition.POSITION_4:
                    return currentPawnPosition == 56;
                case TargetPosition.POSITION_5:
                    return currentPawnPosition == 17;
                case TargetPosition.POSITION_6:
                    return currentPawnPosition == 4;
                case TargetPosition.POSITION_7:
                    return currentPawnPosition == 41;
                case TargetPosition.POSITION_8:
                    return currentPawnPosition == 45;
                default:
                    return false;

            }
        }

        private bool VerifyPositionByIdentity(Identity id, GameState g)
        {
            var currentPawnPosition = Pawn.Position.Id;
            Pawn? targetPawn = g.GetPawnOf(id);
            if (targetPawn == null) return false;
            else
            {
                if (currentPawnPosition == targetPawn.Position.Id) return true;
                else return false;
            }
        }
    }
}
