using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core.Events
{
    public class ReplyToInfoRequestInputNeededEvent : IGameEvent
    {
        public PlayerInfoRequest Request { get; }

        public ReplyToInfoRequestInputNeededEvent(PlayerInfoRequest req)
        {
            Request = req;
        }

        public override string ToString()
        {
            return $"{Request.Receiver}: decide what to answer to {Request.Type} request from {Request.Sender}";
        }

    }
}
