using System;
using System.Collections.Generic;

namespace Inkognito.Core
{
    public sealed class ProphecyPhantom
    {
        private readonly Random random;

        public ProphecyPhantom(Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

       

        /// <summary>Estrae tre voci senza reinserimento; a ogni chiamata riparte dalle dieci voci.</summary>
        public IReadOnlyList<MoveType> DrawMoves()
        {
            var remaining = new List<MoveType>
            {
                MoveType.None, MoveType.None, MoveType.None,
                MoveType.Land, MoveType.Land,
                MoveType.Water, MoveType.Water,
                MoveType.LandOrWater,
                MoveType.Ambassador, MoveType.Ambassador
            };
            var moves = new MoveType[3];
            for (int i = 0; i < moves.Length; i++)
            {
                int index = random.Next(remaining.Count);
                moves[i] = remaining[index];
                remaining.RemoveAt(index);
            }

            // TEST
            moves = new MoveType[] { MoveType.Water, MoveType.Water, MoveType.Ambassador };
            return Array.AsReadOnly(moves);
        }
    }
}
