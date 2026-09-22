using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Inkognito.Core
{
    public class Mission
    {
        public String Description{ get; private set; }
        public IReadOnlyList<VictoryCondition> VictoryConditions { get; private set; }

        public Mission(String d, List<VictoryCondition> v)
        {
            Description = d;
            VictoryConditions = v;
        }

        public static Mission getMissionForFB(MissionPart mF, MissionPart mB, GameState g)
        {
            switch(mF)
            {
                case MissionPart.Alfa:
                    switch(mB)
                    {
                        case MissionPart.Bravo:
                            return new Mission("Andate su X con qualunque pedina",F_A_B_B(g));
                        case MissionPart.Charlie:
                            return new Mission("Portate F sulla casella 5 (17)", F_A_B_C(g) );
                        case MissionPart.Delta:
                            return new Mission("Andate su Z con qualunque pedina", F_A_B_D(g) );
                        default:
                            return new Mission("Mission Unknown", new List<VictoryCondition>());
                    }
                case MissionPart.Bravo:
                    switch(mB)
                    {
                        case MissionPart.Alfa:
                            return new Mission("Andate su X con qualunque pedina", F_A_B_B(g));
                        case MissionPart.Charlie:
                            return new Mission("Andate su A con qualunque pedina", F_B_B_C(g) );
                        case MissionPart.Delta:
                            return new Mission("Portate B sulla casella 1", F_B_B_D(g) );
                        default:
                            return new Mission("Mission Unknown", new List<VictoryCondition>());
                    }
                case MissionPart.Charlie:
                    switch (mB)
                    {
                        case MissionPart.Alfa:
                            return new Mission("Portate F sulla casella 7",  F_C_B_A(g) );
                        case MissionPart.Bravo:
                            return new Mission("Andate su A con qualunque pedina", F_B_B_C(g) );
                        case MissionPart.Delta:
                            return new Mission("Portate A all'Ambasciata", F_C_B_D(g) );
                        default:
                            return new Mission("Mission Unknown", new List<VictoryCondition>());
                    }
                case MissionPart.Delta:
                    switch(mB)
                    {
                        case MissionPart.Alfa:
                            return new Mission("Andate su Z con qualunque pedina",  F_A_B_D(g) );
                        case MissionPart.Bravo:
                            return new Mission( "portate F su B o B su F", F_D_B_B(g) );
                        case MissionPart.Charlie:
                            return new Mission( "portate l'ambasciatore alla casella 8",  F_D_B_C(g) );
                        default:
                            return new Mission("Mission Unknown", new List<VictoryCondition>());
                    }
                default:
                    return new Mission("Mission Unknown", new List<VictoryCondition>());
            }
        }
        public static Mission GetMissionForZX(MissionPart mZ, MissionPart mX, GameState g)
        {
            switch(mZ)
            {
                case MissionPart.Alfa:
                    switch(mX)
                    {
                        case MissionPart.Bravo:
                            return new Mission("Andate su A con qualunque pedina", Z_A_X_B(g) );
                        case MissionPart.Charlie:
                            return new Mission("Andate su B con qualunque pedina", Z_A_X_C(g) );
                        case MissionPart.Delta:
                            return new Mission( "Portate Z su A o A su Z", Z_A_X_D(g) );
                        default:
                            return new Mission("Mission Unknown", new List<VictoryCondition>());
                    }
                case MissionPart.Bravo:
                    switch(mX)
                    {
                        case MissionPart.Alfa:
                            return new Mission( "Andate su A con qualunque pedina",  Z_A_X_B(g));
                        case MissionPart.Charlie:
                            return new Mission("portate Z su X o X su Z",  Z_B_X_C(g) );
                        case MissionPart.Delta:
                            return new Mission("Andate su F con qualunque pedina",  Z_B_X_D(g) );
                        default:
                            return new Mission("Mission Unknown", new List<VictoryCondition>());
                    }
                case MissionPart.Charlie:
                    switch (mX)
                    {
                        case MissionPart.Alfa:
                            return new Mission("Andate su B con qualunque pedina",  Z_A_X_C(g) );
                        case MissionPart.Bravo:
                            return new Mission("portate Z su 4 (56)", Z_C_X_B(g) );
                        case MissionPart.Delta:
                            return new Mission( "portate X su 2 (23)",  Z_C_X_D(g) );
                        default:
                            return new Mission("Mission Unknown", new List<VictoryCondition>());
                    }
                case MissionPart.Delta:
                    switch (mX)
                    {
                        case MissionPart.Alfa:
                            return new Mission("portate A su 3 (38)",  Z_D_X_A(g) );
                        case MissionPart.Bravo:
                            return new Mission("Andate su F con qualunque pedina", Z_B_X_D(g) );
                        case MissionPart.Charlie:
                            return new Mission("portate X su 6", Z_D_X_C(g) );
                        default:
                            return new Mission("Mission Unknown", new List<VictoryCondition>());
                    }
                default:
                    return new Mission("Mission Unknown", new List<VictoryCondition>());
            }    
        }

        /// <summary>
        /// restituisce 8 condizioni di vittoria, una per ciascun pawn di F o B, che devono essere sulla posizione di Mister X
        /// </summary>
        /// <param name="g"></param>
        /// <returns></returns>
        private static List<VictoryCondition> F_A_B_B(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();

            foreach(Player p in g.Players)
            {
                if(p.Identity == Identity.F || p.Identity == Identity.B)
                {
                    foreach(Pawn pa in p.Pawns)
                    {
                        vc.Add(new VictoryCondition(pa, TargetPosition.X_POSITION));
                    }
                }
            }

            return vc;
        }

        private static List<VictoryCondition> F_A_B_C(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();
            Pawn? F = g.GetPawnOf(Identity.F) ?? throw new ArgumentNullException("Il Pawn di F non può essere null a questo punto!");
            vc.Add(new VictoryCondition(F, TargetPosition.POSITION_5));
            return vc;
        }

        private static List<VictoryCondition> F_A_B_D(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();

            foreach (Player p in g.Players)
            {
                if (p.Identity == Identity.F || p.Identity == Identity.B)
                {
                    foreach (Pawn pa in p.Pawns)
                    {
                        vc.Add(new VictoryCondition(pa, TargetPosition.Z_POSITION));
                    }
                }
            }

            return vc;
        }

        private static List<VictoryCondition> F_B_B_C(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();

            foreach (Player p in g.Players)
            {
                if (p.Identity == Identity.F || p.Identity == Identity.B)
                {
                    foreach (Pawn pa in p.Pawns)
                    {
                        vc.Add(new VictoryCondition(pa, TargetPosition.A_POSITION));
                    }
                }
            }

            return vc;
        }

        private static List<VictoryCondition> F_B_B_D(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();
            Pawn? B = g.GetPawnOf(Identity.B) ?? throw new ArgumentNullException("Il Pawn di B non può essere null a questo punto!");
            vc.Add(new VictoryCondition(B, TargetPosition.POSITION_1));
            return vc;
        }

        private static List<VictoryCondition> F_C_B_A(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();
            Pawn? F = g.GetPawnOf(Identity.F) ?? throw new ArgumentNullException("Il Pawn di F non può essere null a questo punto!");
            vc.Add(new VictoryCondition(F, TargetPosition.POSITION_7));
            return vc;
        }

        private static List<VictoryCondition> F_C_B_D(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();
            Pawn? A = g.AmbassadorPawn ?? throw new ArgumentNullException("AmbassadorPawn non può essere null a questo punto!");
            vc.Add(new VictoryCondition(A, TargetPosition.EMBASSY));
            return vc;
        }

        private static List<VictoryCondition> F_D_B_B(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();
            Pawn? F = g.GetPawnOf(Identity.F) ?? throw new ArgumentNullException("Il Pawn di F non può essere null a questo punto!");
            vc.Add(new VictoryCondition(F, TargetPosition.B_POSITION));
            return vc;
        }

        private static List<VictoryCondition> F_D_B_C(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();
            Pawn? A = g.AmbassadorPawn ?? throw new ArgumentNullException("AmbassadorPawn non può essere null a questo punto!");
            vc.Add(new VictoryCondition(A, TargetPosition.POSITION_8));
            return vc;
        }

        private static List<VictoryCondition> Z_A_X_B(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();

            foreach (Player p in g.Players)
            {
                if (p.Identity == Identity.Z || p.Identity == Identity.X)
                {
                    foreach (Pawn pa in p.Pawns)
                    {
                        vc.Add(new VictoryCondition(pa, TargetPosition.A_POSITION));
                    }
                }
            }

            return vc;
        }

        private static List<VictoryCondition>  Z_A_X_C(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();

            foreach (Player p in g.Players)
            {
                if (p.Identity == Identity.Z || p.Identity == Identity.X)
                {
                    foreach (Pawn pa in p.Pawns)
                    {
                        vc.Add(new VictoryCondition(pa, TargetPosition.B_POSITION));
                    }
                }
            }

            return vc;
        }

        private static List<VictoryCondition> Z_A_X_D(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();
            Pawn? Z = g.GetPawnOf(Identity.Z) ?? throw new ArgumentNullException("Il Pawn di Z non può essere null a questo punto!");
            vc.Add(new VictoryCondition(Z, TargetPosition.A_POSITION));
            return vc;
        }

        
        private static List<VictoryCondition> Z_B_X_C(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();
            Pawn? Z = g.GetPawnOf(Identity.Z) ?? throw new ArgumentNullException("Il Pawn di Z non può essere null a questo punto!");
            vc.Add(new VictoryCondition(Z, TargetPosition.X_POSITION));
            return vc;
        }

        
        private static List<VictoryCondition> Z_B_X_D(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();

            foreach (Player p in g.Players)
            {
                if (p.Identity == Identity.Z || p.Identity == Identity.X)
                {
                    foreach (Pawn pa in p.Pawns)
                    {
                        vc.Add(new VictoryCondition(pa, TargetPosition.F_POSITION));
                    }
                }
            }

            return vc;
        }

        private static List<VictoryCondition> Z_C_X_B(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();
            Pawn? Z = g.GetPawnOf(Identity.Z) ?? throw new ArgumentNullException("Il Pawn di Z non può essere null a questo punto!");
            vc.Add(new VictoryCondition(Z, TargetPosition.POSITION_4));
            return vc;
        }

        private static List<VictoryCondition> Z_C_X_D(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();
            Pawn? X = g.GetPawnOf(Identity.X) ?? throw new ArgumentNullException("Il Pawn di Z non può essere null a questo punto!");
            vc.Add(new VictoryCondition(X, TargetPosition.POSITION_2));
            return vc;
        }

        private static List<VictoryCondition> Z_D_X_A(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();
            Pawn? A = g.AmbassadorPawn ?? throw new ArgumentNullException("AmbassadorPawn non può essere null a questo punto!");
            vc.Add(new VictoryCondition(A, TargetPosition.POSITION_3));
            return vc;
        }

        private static List<VictoryCondition>  Z_D_X_C(GameState g)
        {
            List<VictoryCondition> vc = new List<VictoryCondition>();
            Pawn? X = g.GetPawnOf(Identity.X) ?? throw new ArgumentNullException("Il Pawn di Z non può essere null a questo punto!");
            vc.Add(new VictoryCondition(X, TargetPosition.POSITION_6));
            return vc;
        }

        public bool VerifyVictoryConditions(GameState g)
        {
            foreach(VictoryCondition vc in VictoryConditions)
            {
                if (vc.IsVerified(g)) return true;
            }
            return false;
        }
    }
}
