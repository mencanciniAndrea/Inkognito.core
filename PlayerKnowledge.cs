using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace Inkognito.Core
{
    public class PlayerKnowledge
    {
        public PlayerColor About { get; }

        public List<PlayerAnswer> AnswersReceived { get; private set; }

        public List<PlayerAnswer> AnswersGiven { get; }

        public List<PlayerAnswer> AnswersToIdentityToGive { get; }
        public List<PlayerAnswer> AnswersToDisguiseToGive { get; }

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

        public YES_OR_NO IsMyPartner { get; set; }
        public bool KnowsMyMission { get; set; }

        public PlayerKnowledge(PlayerColor p, Player me)
        {
            About = p;
            AnswersReceived = new List<PlayerAnswer>();
            AnswersGiven = new List<PlayerAnswer>();
            AnswersToIdentityToGive = new List<PlayerAnswer>();
            AnswersToDisguiseToGive = new List<PlayerAnswer>();
            IsMyPartner = YES_OR_NO.DONT_KNOW;
            if (me.Identity == Identity.A)
            {
                IsMyPartner = YES_OR_NO.NO;
                KnowsMyMission = true;
            }
            if(p == PlayerColor.Black)
            {
                IsMyPartner = YES_OR_NO.NO;
                AssignedMission = MissionPart.FindAllIdentities;
                InitGivableAnswersToAmbassador(me);
            }
            else
            {
                InitGivableIdentityAnswers(me);
                InitGivableDisguiseAnswers(me);
            }

            WhatIKnowAboutHim = new bool[5, 5];
            WhatHeKnowsAboutMe = new bool[5, 5];


            if (p != PlayerColor.Black)
            {
                AssuredIdentity = Identity.DON_T_KNOW;
                AssuredDisguise = Disguise.DON_T_KNOW;
                AssignedMission = MissionPart.DON_T_KNOW;

                for (int identity = 1; identity < (int)Identity.DON_T_KNOW; identity++)
                {
                    for (int disguise = 1; disguise < (int)Disguise.DON_T_KNOW; disguise++)
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
            for (int identity = 1; identity < (int)Identity.DON_T_KNOW; identity++)
            {
                for (int disguise = 1; disguise < (int)Disguise.DON_T_KNOW; disguise++)
                {
                    WhatHeKnowsAboutMe[identity, disguise] = true;
                }
            }
        }

        private void InitGivableAnswersToAmbassador(Player me)
        {
            List<Identity> otherIdentities = new();
            for (int id = 1; id < (int)Identity.DON_T_KNOW; id++)
            {
                if (id != (int)me.Identity)
                    otherIdentities.Add((Identity)id);
            }
            foreach(Identity id in otherIdentities)
            {
                AnswersToIdentityToGive.Add(new()
                {
                    Answers = new()
                    {
                        new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)id),
                        new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)me.Identity),
                    }
                });
            }

            List<Disguise> otherDisguises = new();
            for (int disg = 1; disg < (int)Disguise.DON_T_KNOW; disg++)
            {
                if (disg != (int)me.Disguise)
                {
                    otherDisguises.Add((Disguise)disg);
                }
            }
            foreach(Disguise d in otherDisguises)
            {
                AnswersToDisguiseToGive.Add(new()
                {
                    Answers = new()
                    {
                        new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)d),
                        new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)me.Disguise),
                    }
                });
            }
        }

        /// <summary>
        /// Genera le risposte fornibili ad una richiesta di identità
        /// </summary>
        /// <param name="me"></param>
        private void InitGivableIdentityAnswers(Player me)
        {
            // Inizializziamo la lista delle risposte che si possono dare
            // all'identity request:
            List<Identity> otherIdentities = new();
            for (int id = 1; id < (int)Identity.DON_T_KNOW; id++)
            {
                if (id != (int)me.Identity)
                    otherIdentities.Add((Identity)id);
            }
            // le prime 12, con la mia identità
            for (int disg = 1; disg < (int)Disguise.DON_T_KNOW; disg++)
            {
                foreach (Identity tId in otherIdentities)
                {
                    List<InkognitoCard> answers = new()
                    {
                        new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)me.Identity),
                        new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)tId),
                        new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)disg)
                    };
                    PlayerAnswer currAns = new() { Answers = answers };
                    AnswersToIdentityToGive.Add(currAns);
                }
            }

            // le ultime 3, con le identità che non mi appartengono
            PlayerAnswer cAns = new()
            {
                Answers = new()
                {
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)otherIdentities[0]),
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)otherIdentities[1]),
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)me.Disguise)
                }
            };
            AnswersToIdentityToGive.Add(cAns);

            cAns = new()
            {
                Answers = new()
                {
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)otherIdentities[1]),
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)otherIdentities[2]),
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)me.Disguise)
                }
            };
         
            AnswersToIdentityToGive.Add(cAns);

            cAns = new()
            {
                Answers = new List<InkognitoCard>()
                {
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)otherIdentities[0]),
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)otherIdentities[2]),
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)me.Disguise)
                }
            };
            
            AnswersToIdentityToGive.Add(cAns);
        }

        /// <summary>
        /// genera le risposte fornibili ad una richiesta di travestimento
        /// </summary>
        /// <param name="me"></param>
        private void InitGivableDisguiseAnswers(Player me)
        {
            List<Disguise> otherDisguises = new();
            for (int disg = 1; disg < (int)Disguise.DON_T_KNOW; disg++)
            {
                if (disg != (int)me.Disguise)
                {
                    otherDisguises.Add((Disguise)disg);
                }
            }
            // le prime 12, con il mio travestimento
            for (int currId = 1; currId < (int)Identity.DON_T_KNOW; currId++)
            {
                foreach (Disguise currDisguise in otherDisguises)
                {
                    PlayerAnswer currAns = new()
                    {
                        Answers = new()
                        {
                            new (InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)me.Disguise),
                            new (InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)currDisguise),
                            new (InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)currId)
                        }
                    };

                    AnswersToDisguiseToGive.Add(currAns);
                }
            }

            // le ultime 3, con i travestimenti falsi
            PlayerAnswer cAns = new()
            {
                Answers = new()
                {
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)otherDisguises[0]),
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)otherDisguises[1]),
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)me.Identity)
                }
            };
            AnswersToDisguiseToGive.Add(cAns);

            cAns = new()
            {
                Answers = new()
                {
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)otherDisguises[1]),
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)otherDisguises[2]),
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)me.Identity)
                }
            };
            AnswersToDisguiseToGive.Add(cAns);

            cAns = new()
            {
                Answers = new()
                {
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)otherDisguises[0]),
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.DISGUISE, (int)otherDisguises[2]),
                    new InkognitoCard(InkognitoCardVisibility.PUBLIC, InkognitoCardType.IDENTITY, (int)me.Identity)
                }
            };
            AnswersToDisguiseToGive.Add(cAns);
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
            sb.AppendLine($"{AssuredIdentity}, {AssuredDisguise}, is my partner: {IsMyPartner}; Mission: {AssignedMission}");

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
                    string value = WhatIKnowAboutHim[id, disg] ? "1" : "-";
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
                    string value = WhatHeKnowsAboutMe[id, disg] ? "1" : "-";
                    sb.Append(value.PadRight(columnWidth));
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
