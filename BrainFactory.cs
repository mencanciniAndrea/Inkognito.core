using Inkognito.Core.Brains;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public static class BrainFactory
    {
        public static IBrain Create(BrainType type)
        {
            return type switch
            {
                BrainType.Lazy => new LazyBrain(),
                //BrainType.Random => new RandomBrain(), // TODO: prima o poi ci arriviamo
                //BrainType.Aggressive => new AggressiveBrain(), // TODO: prima o poi ci arriviamo
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }
    }
}
