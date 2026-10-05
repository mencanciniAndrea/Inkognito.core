using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Commands
{
    public sealed class StartGameCommand : IGameCommand
    {

        public StartGameCommand()
        {
        }

        public override string ToString()
        {
            return "StartGameCommand: Start the game.";
        }
    }
}
