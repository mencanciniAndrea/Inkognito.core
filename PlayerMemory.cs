using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class PlayerMemory
    {
        List<PlayerKnowledge> knowledgeAboutOtherPlayers { get; }

        public PlayerMemory(IEnumerable<Player> otherPlayers)
        {
            knowledgeAboutOtherPlayers = new List<PlayerKnowledge>();
            foreach(Player p in otherPlayers)
            {
                knowledgeAboutOtherPlayers.Add(new PlayerKnowledge(p));
            }
        }
    }
}
