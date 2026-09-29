using System;
using System.Collections.Generic;
using System.Text;

namespace Inkognito.Core
{
    public class PlayerAnswer
    {
        public PlayerInfoRequest? Request { get; set; }

        private List<InkognitoCard> _answers;

        private readonly bool[,] Bitmask = new bool[5, 5];


        public PlayerAnswer()
        {
            _answers = new List<InkognitoCard>();
        }
        public List<InkognitoCard> Answers 
        {
            get => _answers;
            set 
            {
                _answers = value;
                // costruisci la bitmask. Ci serve dopo, per fare il merge diretto
                int identity;
                int disguise;
                foreach(InkognitoCard c in value)
                {
                    switch(c.Type)
                    {
                        case InkognitoCardType.IDENTITY:
                            identity = c.Value;
                            for(disguise = 1; disguise < (int) Disguise.DON_T_KNOW; disguise++)
                            {
                                Bitmask[identity, disguise] = true;
                            }
                            break;
                        case InkognitoCardType.DISGUISE:
                            disguise = c.Value;
                            for(identity = 1; identity < (int) Identity.DON_T_KNOW; identity++)
                            {
                                Bitmask[identity, disguise] = true;
                            }
                            break;
                    }
                }
            } 
        }

        public bool[,] AsBitmask()
        {
            return Bitmask;
        }


        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append("Cards: ");
            foreach(var c in _answers)
            {
                switch(c.Type)
                {
                    case InkognitoCardType.IDENTITY:
                        sb.Append($"{c.Visibility} {(Identity)c.Value} ");
                        break;
                    case InkognitoCardType.DISGUISE:
                        sb.Append($"{c.Visibility} {(Disguise)c.Value} ");
                        break;
                    case InkognitoCardType.MISSION:
                        sb.Append($"{c.Visibility} {(MissionPart)c.Value} ");
                        break;
                }
            }
            sb.AppendLine();
            int columnWidth = 10;
            sb.AppendLine("Bitmask:");
            sb.Append("".PadRight(columnWidth));
            for (int id = 1; id < (int)(Identity.DON_T_KNOW); id++)
            {
                sb.Append(((Identity)id).ToString().PadRight(columnWidth));
            }
            sb.AppendLine();
            // Separator
            sb.AppendLine(new string('-', columnWidth * (5)));
            for (int disg = 1; disg < (int)Disguise.DON_T_KNOW; disg++)
            {
                sb.Append(((Disguise)disg).ToString().PadRight(columnWidth));

                for (int id = 1; id < (int)(Identity.DON_T_KNOW); id++)
                {
                    string value = Bitmask[id, disg] ? "True" : "False";
                    sb.Append(value.PadRight(columnWidth));
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
