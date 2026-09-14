using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public static class BrainFactory
    {
        public static IBrain Create(BrainType type, ILoggerFactory factory)
        {
            return type switch
            {
                BrainType.Lazy => new LazyBrain(factory),
                //BrainType.Random => new RandomBrain(), // TODO: prima o poi ci arriviamo
                //BrainType.Aggressive => new AggressiveBrain(), // TODO: prima o poi ci arriviamo
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }
    }
}
