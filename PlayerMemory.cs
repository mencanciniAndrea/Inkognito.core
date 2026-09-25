using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;

namespace Inkognito.Core
{
    public class PlayerMemory
    {
        IReadOnlyList<PlayerKnowledge> KnowledgeAboutOtherPlayers { get; }

        public PlayerMemory(IEnumerable<Player> otherPlayers, Player me)
        {
            var _knowledgeAboutOtherPlayers = new List<PlayerKnowledge>();
            foreach(Player p in otherPlayers)
            {
                if (p != me) _knowledgeAboutOtherPlayers.Add(new PlayerKnowledge(p.Color, me));
            }

            KnowledgeAboutOtherPlayers = _knowledgeAboutOtherPlayers;
        }

        public (Identity, Disguise, MissionPart) GetKnownPlayerDetails(PlayerColor pColor)
        {
            foreach(PlayerKnowledge k in KnowledgeAboutOtherPlayers)
            {
                if (k.About == pColor)
                {
                    return (k.AssuredIdentity, k.AssuredDisguise, k.AssignedMission);
                }
            }
            return (Identity.DON_T_KNOW, Disguise.DON_T_KNOW, MissionPart.DON_T_KNOW);
        }

        public PlayerKnowledge GetPlayerKnowledge(PlayerColor c)
        {
            foreach(var k in KnowledgeAboutOtherPlayers)
            {
                if (k.About == c) return k;
            }
            throw new ArgumentException($"Colore {c} non trovato!");
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            foreach(var k in KnowledgeAboutOtherPlayers)
            {
                sb.AppendLine($"{k}");
            }
            return sb.ToString();
        }

    }
}
