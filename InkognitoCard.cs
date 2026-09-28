using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class InkognitoCard
    {
        public InkognitoCardVisibility Visibility { get; set; }
        public InkognitoCardType Type { get; set; }
        public int Value { get; set; }

        public InkognitoCard(InkognitoCardVisibility v, InkognitoCardType t, int val)
        {
            Visibility = v;
            Type = t;
            Value = val;
        }
    }
}
