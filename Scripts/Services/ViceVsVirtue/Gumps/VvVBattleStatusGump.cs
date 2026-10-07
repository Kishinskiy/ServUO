using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Engines.VvV
{
    public class VvVBattleStatusGump : BaseGump
    {
        public VvVBattle Battle { get; set; }

        public VvVBattleStatusGump(PlayerMobile pm, VvVBattle battle) : base(pm)
        {
            Battle = battle;
        }

        public override int GetTypeID()
        {
            return 0xF3ECC;
        }

        public override void AddGumpLayout()
        {
            AddPage(0);
            AddImage(0, 0, 30566);

            if (DateTime.UtcNow >= Battle.NextSigilSpawn && Battle.Sigil != null && !Battle.Sigil.Deleted)
                AddImage(200, 300, 30583);

            var teams = new List<BattleTeam>(Battle.Teams);
            teams.Sort();

            var offset = 216 / VvVBattle.ScoreToWin;

            for (var i = 0; i < teams.Count; i++)
            {
                var team = teams[i];

                if (team.Guild != null)
                    AddHtml(87, 115 + 31 * i, 50, 20,
                        string.Format("<basefont color=#FFFFFF>{0}", team.Guild.Abbreviation), false, false);

                AddBackground(145, 120 + 31 * i, (int)Math.Min(216, team.Score * offset), 12, 30584);

                if (i == 2) // stupid gump only allows 3 to be shown
                    break;
            }

            var count = Battle.Messages.Count - 1;
            var y = 206;

            for (var i = count; i >= 0; i--)
            {
                if (i <= count - 3)
                    break;

                AddHtml(98, y, 250, 16, string.Format("<basefont color=#80BFFF>{0}", Battle.Messages[i]), false, false);

                y += 16;
            }

            var gu = User.Guild as Guild;

            if (gu != null)
            {
                var t = Battle.GetTeam(gu);

                AddHtml(87, 268, 50, 20, string.Format("<basefont color=#FFFFFF>{0}", gu.Abbreviation), false, false);
                AddBackground(145, 271, (int)Math.Min(216, t.Score * offset), 12, 30584);
            }

            var left = Battle.StartTime + TimeSpan.FromMinutes(VvVBattle.Duration) - DateTime.UtcNow;
            AddHtml(210, 21, 100, 20, "<basefont color=#FF0000>" + string.Format("{0:mm\\:ss}", left), false, false);
        }

        /*public void Refresh(bool recompile = true)
        {
            Entries.Clear();
            Entries.TrimExcess();
            AddGumpLayout();

            if (User.NetState != null)
                User.NetState.RemoveGump(this);

            User.SendGump(this, false);
        }*/
    }
}
