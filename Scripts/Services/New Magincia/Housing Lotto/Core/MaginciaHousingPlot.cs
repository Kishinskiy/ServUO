using System;
using System.Collections.Generic;
using Server.Accounting;

namespace Server.Engines.NewMagincia
{
    [PropertyObject]
    public class MaginciaHousingPlot
    {
        private Rectangle2D m_Bounds;

        [CommandProperty(AccessLevel.GameMaster)]
        public string Identifier { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public WritOfLease Writ { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D Bounds => m_Bounds;

        [CommandProperty(AccessLevel.GameMaster)]
        public MaginciaPlotStone Stone { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsPrimeSpot { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Complete { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Winner { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map Map { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime Expires { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D RecallLoc => new Point3D(m_Bounds.X, m_Bounds.Y, Map.GetAverageZ(m_Bounds.X, m_Bounds.Y));

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsAvailable => !Complete;

        public MaginciaHousingPlot(string identifier, Rectangle2D bounds, bool prime, Map map)
        {
            Identifier = identifier;
            m_Bounds = bounds;
            IsPrimeSpot = prime;
            Map = map;
            Writ = null;
            Complete = false;
            Expires = DateTime.MinValue;
        }

        public MaginciaHousingPlot(GenericReader reader)
        {
            var version = reader.ReadInt();

            Identifier = reader.ReadString();
            Writ = reader.ReadItem() as WritOfLease;
            Stone = reader.ReadItem() as MaginciaPlotStone;
            LottoEnds = reader.ReadDateTime();
            m_Bounds = reader.ReadRect2D();
            Map = reader.ReadMap();
            IsPrimeSpot = reader.ReadBool();
            Complete = reader.ReadBool();
            Winner = reader.ReadMobile();
            Expires = reader.ReadDateTime();

            var c = reader.ReadInt();
            for (var i = 0; i < c; i++)
            {
                var m = reader.ReadMobile();
                var amount = reader.ReadInt();

                if (m != null)
                    Participants[m] = amount;
            }

            if ((Stone == null || Stone.Deleted) && LottoOngoing && MaginciaLottoSystem.IsRegisteredPlot(this))
                AddPlotStone();
            else if (Stone != null)
                Stone.Plot = this;

            if (Writ != null)
                Writ.Plot = this;
        }

        public void AddPlotStone()
        {
            AddPlotStone(MaginciaLottoSystem.GetPlotStoneLoc(this));
        }

        public void AddPlotStone(Point3D p)
        {
            Stone = new MaginciaPlotStone();
            Stone.Plot = this;
            Stone.MoveToWorld(p, Map);
        }

        public override string ToString()
        {
            return "...";
        }

        public bool CanPurchaseLottoTicket(Mobile from)
        {
            if (IsPrimeSpot)
            {
                var acct = from.Account as Account;

                if (acct == null)
                    return false;

                return CheckAccount(acct);
            }

            return true;
        }

        private bool CheckAccount(Account acct)
        {
            for (var i = 0; i < acct.Length; i++)
            {
                var m = acct[i];

                if (m == null)
                    continue;

                foreach (var plot in MaginciaLottoSystem.Plots)
                    if (plot.IsPrimeSpot && plot.Participants.ContainsKey(m))
                        return false;
            }

            return true;
        }

        public void PurchaseLottoTicket(Mobile from, int toBuy)
        {
            if (Participants.ContainsKey(from))
                Participants[from] += toBuy;
            else
                Participants[from] = toBuy;
        }

        public void EndLotto()
        {
            if (Participants.Count == 0)
            {
                ResetLotto();
                return;
            }

            var raffle = new List<Mobile>();

            foreach (var kvp in Participants)
            {
                if (kvp.Value == 0)
                    continue;

                for (var i = 0; i < kvp.Value; i++)
                    raffle.Add(kvp.Key);
            }

            var winner = raffle[Utility.Random(raffle.Count)];

            if (winner != null)
                OnLottoComplete(winner);
            else
                ResetLotto();

            Participants.Clear();
        }

        public void OnLottoComplete(Mobile winner)
        {
            Complete = true;
            Winner = winner;
            LottoEnds = DateTime.MinValue;
            Expires = DateTime.UtcNow + TimeSpan.FromDays(MaginciaLottoSystem.WritExpirePeriod);

            if (winner.HasGump(typeof(PlotWinnerGump)))
                return;

            var acct = winner.Account as Account;

            if (acct == null)
                return;

            for (var i = 0; i < acct.Length; i++)
            {
                var m = acct[i];

                if (m == null)
                    continue;

                if (m.NetState != null)
                {
                    winner.SendGump(new PlotWinnerGump(this));
                    break;
                }
            }
        }

        public void SendMessage_Callback(object o)
        {
            var obj = o as object[];

            if (obj != null)
            {
                var winner = obj[0] as Mobile;
                var message = obj[1] as NewMaginciaMessage;

                MaginciaLottoSystem.SendMessageTo(winner, message);
            }
        }

        public void ResetLotto()
        {
            if (MaginciaLottoSystem.AutoResetLotto && MaginciaLottoSystem.Instance != null)
                LottoEnds = DateTime.UtcNow + MaginciaLottoSystem.Instance.LottoDuration;
            else
                LottoEnds = DateTime.MinValue;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(0);

            writer.Write(Identifier);
            writer.Write(Writ);
            writer.Write(Stone);
            writer.Write(LottoEnds);
            writer.Write(m_Bounds);
            writer.Write(Map);
            writer.Write(IsPrimeSpot);
            writer.Write(Complete);
            writer.Write(Winner);
            writer.Write(Expires);

            writer.Write(Participants.Count);

            foreach (var kvp in Participants)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        #region Lotto Info

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LottoEnds { get; set; }

        public Dictionary<Mobile, int> Participants { get; } = new Dictionary<Mobile, int>();

        [CommandProperty(AccessLevel.GameMaster)]
        public int LottoPrice => IsPrimeSpot ? 10000 : 2000;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool LottoOngoing => IsAvailable && LottoEnds > DateTime.UtcNow && LottoEnds != DateTime.MinValue;

        #endregion
    }
}
