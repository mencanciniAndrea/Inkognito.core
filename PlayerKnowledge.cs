using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Inkognito.Core
{
    public class PlayerKnowledge
    {
        public PlayerColor About { get; }

        List<PlayerAnswer> AnswersReceived { get; }

        List<PlayerAnswer> AnswersGiven { get; }

        List<IdentityDisguisePair> PossibleInfo { get; }

        public Identity AssuredIdentity { get; }

        public Disguise AssuredDisguise { get; }

        public Mission AssignedMission { get; }

        public PlayerKnowledge(PlayerColor p, Player me)
        {
            About = p;
            AnswersReceived = new List<PlayerAnswer>();
            AnswersGiven = new List<PlayerAnswer>();
            PossibleInfo = new List<IdentityDisguisePair>();

            if (p != PlayerColor.Black)
            {
                AssuredIdentity = Identity.DON_T_KNOW;
                AssuredDisguise = Disguise.DON_T_KNOW;
                AssignedMission = Mission.DON_T_KNOW;

                // generare tutte le possibili coppie identità,travestimento togliendo i miei
                var availableDisguises = new List<Disguise>
                {
                    Disguise.Fat,
                    Disguise.Small,
                    Disguise.Tall,
                    Disguise.Thin
                }.Where(d => d != me.Disguise).ToList();

                var availableIdentities = new List<Identity>
                {
                    Identity.F,
                    Identity.B,
                    Identity.X,
                    Identity.Z
                }.Where(i => i != me.Identity).ToList();

                foreach(Identity i in availableIdentities)
                {
                    foreach(Disguise d in availableDisguises)
                    {
                        IdentityDisguisePair possibility = new () { Identity = i, Disguise = d };
                        PossibleInfo.Add(possibility);
                    }
                }
            }
            else
            {
                AssuredIdentity = Identity.A;
                AssuredDisguise = Disguise.Ambassador;
                AssignedMission = Mission.FindAllIdentities;
                PossibleInfo.Add(new() { Identity = Identity.A, Disguise = Disguise.Ambassador });
            }
        }

        public void AddAnswerReceived(PlayerAnswer answer)
        {
            AnswersReceived.Add(answer);

            // TODO: elimina tutte le possibilità che non rendono vera questa risposta
            // TODO: Scommenta questa, sembra OK
            // PossibleInfo = PossibleInfo.Where(pInfo => {
            //    bool atLeastOneFound = false;
            //    foreach (Card c in answer)
            //    {
            //        if (c.type == identity)
            //           if (c.value == pInfo.identity)
            //           {
            //               atLeastOneFound = true;
            //               break;
            //           }
            //        if (c.Type == Disguise)
            //        {
            //            if (c.value == pInfo.Disguise)
            //            {
            //                atLeastOneFound = true;
            //                break;
            //            }
            //        }
            //    }
            //    return atLeastOneFound;});

            //TODO: caso speciale: gestire le carte segrete! (magari questo lo facciamo dopo, visto che è un caso avanzato)
        }

        public void AddAnswerGiven(PlayerAnswer answer)
        {
            AnswersGiven.Add(answer);
        }
    }
}
