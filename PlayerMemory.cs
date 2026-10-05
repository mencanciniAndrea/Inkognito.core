using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Inkognito.Core
{
    public class PlayerMemory
    {
        private Dictionary<PlayerColor, PlayerKnowledge> KnowledgeByColor { get; }

        public YES_OR_NO IMustPlayAlone { get; set; }

        public PlayerMemory(IEnumerable<Player> otherPlayers, Player me)
        {
            var _knowledgeAboutOtherPlayers = new List<PlayerKnowledge>();
            foreach(Player p in otherPlayers)
            {
                if (p != me) _knowledgeAboutOtherPlayers.Add(new PlayerKnowledge(p.Color, me));
            }

            KnowledgeByColor = otherPlayers.ToDictionary(k => k.Color, k => new PlayerKnowledge(k.Color, me));
            IMustPlayAlone = YES_OR_NO.DONT_KNOW;
        }

        public (Identity, Disguise, MissionPart) GetKnownPlayerDetails(PlayerColor pColor)
        {
            if (KnowledgeByColor.TryGetValue(pColor, out PlayerKnowledge? knowledge))
            {
                return (knowledge.AssuredIdentity, knowledge.AssuredDisguise, knowledge.AssignedMission);
            }
            return (Identity.DON_T_KNOW, Disguise.DON_T_KNOW, MissionPart.DON_T_KNOW);
               
        }

        public PlayerKnowledge? GetPlayerKnowledge(PlayerColor c)
        {
            if(KnowledgeByColor.TryGetValue(c, out PlayerKnowledge? knowledge))
            {
                return knowledge;
            }
            return null;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            foreach(var k in KnowledgeByColor)
            {
                sb.AppendLine($"{k.Key} - {k.Value}");
            }
            return sb.ToString();
        }

    }
}
