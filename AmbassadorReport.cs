using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class AmbassadorReport
    {
        public Identity RedPlayerIdentity { get; }
        public Identity GreenPlayerIdentity { get; }
        public Identity BluePlayerIdentity { get; }
        public Identity YellowPlayerIdentity { get; }
        public Disguise RedPlayerDisguise { get; }
        public Disguise GreenPlayerDisguise { get; }
        public Disguise BluePlayerDisguise { get; }
        public Disguise YellowPlayerDisguise { get; }


        public AmbassadorReport(Identity redId, Disguise redDis, Identity greenId, Disguise greenDis, Identity blueId, Disguise blueDis, Identity yellowId, Disguise yellowDis)
        {
            RedPlayerIdentity = redId;
            RedPlayerDisguise = redDis;
            GreenPlayerIdentity = greenId;
            GreenPlayerDisguise = greenDis;
            BluePlayerIdentity = blueId;
            BluePlayerDisguise = blueDis;
            YellowPlayerIdentity = yellowId;
            YellowPlayerDisguise = yellowDis;
        }
    }
}
