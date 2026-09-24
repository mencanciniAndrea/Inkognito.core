using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class PlayerAnswer
    {
        public PlayerInfoRequest Request { get; set; }

        private List<InkognitoCard>? _answers;
        public List<InkognitoCard> Answers 
        {
            get => _answers;
            set 
            {
                _answers = value;
                // costruisci la bitmask. Ci serve dopo, per fare il merge diretto
                int identity = 0;
                int disguise = 0;
                foreach(InkognitoCard c in value)
                {
                    switch(c.Type)
                    {
                        case InkognitoCardType.IDENTITY:
                            identity = c.Value;
                            for(disguise = 1; disguise < (int) Disguise.DON_T_KNOW; disguise++)
                            {
                                bitmask[identity, disguise] = true;
                            }
                            break;
                        case InkognitoCardType.DISGUISE:
                            disguise = c.Value;
                            for(identity = 1; identity < (int) Identity.DON_T_KNOW; identity++)
                            {
                                bitmask[identity, disguise] = true;
                            }
                            break;
                    }
                }
            } 
        }

        bool[,] bitmask = new bool[5,5];

        public bool[,] AsBitmask()
        {
            return bitmask;
        }
    }
}
