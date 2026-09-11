using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class Plan
    {
        public List<Move> Moves { get; private set; }

        public Board resultingBoard { get; set; }

        public Plan()
        {
            Moves = new List<Move>();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var move in Moves)
            {
                sb.Append(move.ToString());
                sb.Append(" -> ");
            }
            if (Moves.Count > 0)
                sb.Length -= 4; // Remove the last " -> "
            sb.AppendLine();
            sb.Append("Brings to: ");
            sb.AppendLine(resultingBoard.ToString());
            return sb.ToString();
        }
    }
}
