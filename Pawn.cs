using System;

namespace Inkognito.Core
{
    public sealed class Pawn
    {
        public PlayerColor Color { get; }
        public Disguise Disguise { get; }
        private Cell position;
        /// <summary>Riferimento alla cella occupata sul tabellone della partita.</summary>
        public Cell Position
        {
            get => position;
            internal set => position = value ?? throw new ArgumentNullException(nameof(value));
        }

        internal Pawn(PlayerColor color, Disguise disguise, Cell position)
        {
            Color = color;
            Disguise = disguise;
            this.position = position ?? throw new ArgumentNullException(nameof(position));
        }
    }
}
