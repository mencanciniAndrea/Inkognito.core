using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace Inkognito.Core
{
    public class PlayerKnowledge
    {
        public PlayerColor About { get; }

        List<PlayerAnswer> AnswersReceived { get; }

        List<PlayerAnswer> AnswersGiven { get; }

        /// <summary>
        /// rappresenta la matrice di accoppiamenti Identity, Disguise possibili che l'altro giocatore potrebbe assumere in funzione di quello che mi ha comunicato
        /// </summary>
        public bool[,] WhatIKnowAboutHim { get; private set; }

        /// <summary>
        /// rappresenta la matrice di possibilità che secondo me, l'altro player ha nella sua testa, in funzione di quello che ho comunicato
        /// </summary>
        public bool[,] WhatHeKnowsAboutMe { get; private set; }

        public Identity AssuredIdentity { get; set; }

        public Disguise AssuredDisguise { get; set; }

        public MissionPart AssignedMission { get; set; } 

        public PlayerKnowledge(PlayerColor p, Player me)
        {
            About = p;
            AnswersReceived = new List<PlayerAnswer>();
            AnswersGiven = new List<PlayerAnswer>();
            WhatIKnowAboutHim = new bool[5, 5];
            WhatHeKnowsAboutMe  = new bool[5, 5];

            if (p != PlayerColor.Black)
            {
                AssuredIdentity = Identity.DON_T_KNOW;
                AssuredDisguise = Disguise.DON_T_KNOW;
                AssignedMission = MissionPart.DON_T_KNOW;

                for(int identity = 1; identity < (int) Identity.DON_T_KNOW; identity++)
                {
                    for (int disguise = 1; disguise < (int) Disguise.DON_T_KNOW; disguise++)
                    {
                        if (identity == (int)me.Identity || disguise == (int)me.Disguise)
                        {
                            // lascio fuori tutta la colonna con la mia identità e la riga con il mio travestimento
                            WhatIKnowAboutHim[identity, disguise] = false;
                        }
                        else
                        {
                            WhatIKnowAboutHim[identity, disguise] = true;
                        }
                    }
                }
            }
            else
            {
                AssuredIdentity = Identity.A;
                AssuredDisguise = Disguise.Ambassador;
                AssignedMission = MissionPart.FindAllIdentities;
                WhatIKnowAboutHim[(int)Identity.A, (int)Disguise.Ambassador] = true;
            }

            // WhatHeKnowsAboutMe - vale per tutti, anche per l'ambasciatore
            for(int identity = 1; identity < (int)Identity.DON_T_KNOW; identity++)
            {
                for(int disguise = 1; disguise < (int) Disguise.DON_T_KNOW; disguise++)
                {
                    WhatHeKnowsAboutMe[identity, disguise] = true;
                }
            }
        }

        public void AddAnswerReceived(PlayerAnswer answer)
        {
            AnswersReceived.Add(answer);

            
        }

        public void AddAnswerGiven(PlayerAnswer answer)
        {
            AnswersGiven.Add(answer);
        }

        public override string ToString()
        {
            const int columnWidth = 10;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Knows About {About}:");
            sb.AppendLine($"Identity: {AssuredIdentity}, Disguise:{AssuredDisguise} Mission: {AssignedMission}");

            // Header
            sb.AppendLine("Possibilities:");
            sb.Append("".PadRight(columnWidth));
            for (int id = 1; id < (int)(Identity.DON_T_KNOW); id++)
            {
                sb.Append(((Identity)id).ToString().PadRight(columnWidth));
            }
            sb.AppendLine();
            // Separator
            sb.AppendLine(new string('-', columnWidth * (5)));
            for (int disg = 1; disg < (int) Disguise.DON_T_KNOW; disg++)
            {
                sb.Append(((Disguise)disg).ToString().PadRight(columnWidth));

                for (int id = 1; id < (int)(Identity.DON_T_KNOW); id++)
                {
                    string value = WhatIKnowAboutHim[id, disg] ? "True" : "False";
                    sb.Append(value.PadRight(columnWidth));
                }

                sb.AppendLine();
            }


            sb.AppendLine("What he knows about me:");
            // Header
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
                    string value = WhatHeKnowsAboutMe[id, disg] ? "True" : "False";
                    sb.Append(value.PadRight(columnWidth));
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
