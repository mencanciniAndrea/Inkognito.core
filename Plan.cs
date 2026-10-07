using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Inkognito.Core
{
    public class Plan
    {
        public HashSet<PlanTraits> Traits { get; private set; }
        public List<Move> Moves { get; private set; }

        public Board ResultingBoard { get; private set; }

        public Plan(Board originalBoard)
        {
            Traits = new HashSet<PlanTraits>();
            Moves = new List<Move>();
            ResultingBoard = originalBoard.Clone();
        }

        public Plan Clone()
        {
            Plan result = new(ResultingBoard);
            // si fa così perché non voglio ri-applicare le mosse già esistenti
            result.Moves.AddRange(Moves);
            return result;
        }

        public void AddMove(Move m)
        {
            Moves.Add(m);
            ResultingBoard.ApplyMove(m);
        }

        public void AddRange(IEnumerable<Move> collection)
        {
            Moves.AddRange(collection);
            foreach(var m in collection)
            {
                ResultingBoard.ApplyMove(m);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new ();
            sb.Append("Moves: { ");
            foreach (var move in Moves)
            {
                sb.Append(move.ToString());
                sb.Append(" -> ");
            }
            
            if (Moves.Count > 0)
                sb.Length -= 4; // Remove the last " -> "

            sb.Append(" }");
            sb.AppendLine();
            sb.Append("Traits: ");
            sb.AppendJoin(",", Traits.ToArray());
            sb.AppendLine();
            sb.AppendLine("Brings to: ");
            sb.AppendLine(ResultingBoard.ToString());
            return sb.ToString();
        }
    }
}
