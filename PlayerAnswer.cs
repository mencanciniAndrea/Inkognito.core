using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class PlayerAnswer
    {
        public PlayerInfoRequest Request { get; set; }

        public List<InkognitoCard> Answers { get; set; }
    }
}
