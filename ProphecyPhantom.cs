using System;
using System.Collections.Generic;

namespace Inkognito.Core
{
    public sealed class ProphecyPhantom
    {
        private readonly Random random;
        private readonly Ruleset reference;

        public ProphecyPhantom(Random random)
            : this(random, Ruleset.RULESET_1988)
        {
        }

        public ProphecyPhantom(Random random, Ruleset r)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            reference = r;
        }

        /// <summary>Estrae tre voci senza reinserimento; a ogni chiamata riparte dalle dieci voci.</summary>
        public IReadOnlyList<MoveType> DrawMoves()
        {
            var remaining = GetAvailableMoves();
            var moves = new MoveType[3];
            for (int i = 0; i < moves.Length; i++)
            {
                int index = random.Next(remaining.Count);
                moves[i] = remaining[index];
                remaining.RemoveAt(index);
            }

            return Array.AsReadOnly(moves);
        }

        private List<MoveType> GetAvailableMoves()
        {
            switch (reference)
            {
                case Ruleset.RULESET_1988:
                    return new List<MoveType>
                    {
                        MoveType.None, MoveType.None, MoveType.None,
                        MoveType.Land, MoveType.Land,
                        MoveType.Water, MoveType.Water,
                        MoveType.LandOrWater,
                        MoveType.Ambassador, MoveType.Ambassador
                    };
                case Ruleset.RULESET_2022:
                    return  new List<MoveType>
                    {
                        MoveType.Land, MoveType.Land, MoveType.Land,
                        MoveType.Water, MoveType.Water, MoveType.Water,
                        MoveType.LandOrWater,
                        MoveType.AnotherPlayerPawn,
                        MoveType.Ambassador, MoveType.Ambassador
                    };
                default:
                    throw new ArgumentException("Ruleset non supportato!");
            }
        }
    }
}
