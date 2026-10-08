using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class PlayerInfoRequest
    {

        // da chi?
        public PlayerColor Sender { get; }
        public PlayerColor Receiver { get; }

        // tipo richiesta?
        public RequestType Type { get; }

        // tramite ambasciatore?
        public bool ThroughAmbassador { get; }

        public PlayerInfoRequest(PlayerColor from, PlayerColor to, RequestType t, bool throughAmbassador)
        {
            Sender = from;
            Receiver = to;
            Type = t;
            ThroughAmbassador = throughAmbassador;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append($"{Sender} chiede {Type} a {Receiver}");
            sb.Append(ThroughAmbassador ? " tramite ambasciatore" : " direttamente");
            return sb.ToString();
        }

    }
}
