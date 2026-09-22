using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class PlayerMemory
    {
        List<PlayerKnowledge> knowledgeAboutOtherPlayers { get; }

        public PlayerMemory(IEnumerable<Player> otherPlayers, Player me)
        {
            knowledgeAboutOtherPlayers = new List<PlayerKnowledge>();
            foreach(Player p in otherPlayers)
            {
                if (p != me) knowledgeAboutOtherPlayers.Add(new PlayerKnowledge(p.Color, me));
            }
        }

        public (Identity, Disguise, MissionPart) GetKnownPlayerDetails(PlayerColor pColor)
        {
            foreach(PlayerKnowledge k in knowledgeAboutOtherPlayers)
            {
                if (k.About == pColor)
                {
                    return (k.AssuredIdentity, k.AssuredDisguise, k.AssignedMission);
                }
            }
            return (Identity.DON_T_KNOW, Disguise.DON_T_KNOW, MissionPart.DON_T_KNOW);
        }

    }
}
