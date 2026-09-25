using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class Plan
    {
        public List<Move> Moves { get; private set; }

        public Board resultingBoard { get; private set; }

        public Plan(Board originalBoard)
        {
            Moves = new List<Move>();
            resultingBoard = originalBoard.Clone();
        }

        public Plan Clone()
        {
            Plan result = new(resultingBoard);
            // si fa così perché non voglio ri-applicare le mosse già esistenti
            result.Moves.AddRange(Moves);
            return result;
        }

        public void AddMove(Move m)
        {
            Moves.Add(m);
            resultingBoard.ApplyMove(m);
        }

        public void AddRange(IEnumerable<Move> collection)
        {
            Moves.AddRange(collection);
            foreach(var m in collection)
            {
                resultingBoard.ApplyMove(m);
            }
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
