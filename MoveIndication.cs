using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class MoveIndication
    {
        public MoveType Move { get; }
        public bool IsConsumed { get; set; }

        public MoveIndication(MoveType m)
        {
            Move = m;
            IsConsumed = false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{Move} - ");
            sb.Append(IsConsumed ? "CONSUMED" : "");
            return  sb.ToString();
        }
    }
}
