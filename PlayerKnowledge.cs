using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Inkognito.Core
{
    public class PlayerKnowledge
    {
        Player about { get; set; }

        List<PlayerAnswer> answersReceived { get; }

        List<PlayerAnswer> answerGiven { get; }

        // Remaining possibilities ... ora ci arriviamo

        Identity assuredIdentity { get; }

        Disguise disguise { get; }

        Mission mission { get; }

        public PlayerKnowledge(Player p)
        {

        }
    }
}
