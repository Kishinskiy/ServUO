using Server.Items;
using Server.Network;

namespace Server.Gumps
{
    public class CannonGump : Gump
    {
        public static readonly int LabelColor = 0xFFFFFF;
        public static readonly int GreenHue = 5057;
        public static readonly int RedHue = 28776;

        private readonly Mobile m_From;

        public BaseCannon Cannon { get; }

        public CannonGump(BaseCannon cannon, Mobile from)
            : base(50, 50)
        {
            Cannon = cannon;
            m_From = from;

            AddBackground(0, 0, 300, 200, 2620);

            var charge = cannon is LightShipCannon ? typeof(LightPowderCharge) : typeof(HeavyPowderCharge);

            var cleaned = cannon.Cleaned;
            var charged = cannon.Charged;
            var primed = cannon.Primed;
            var loaded = cannon.AmmoType != AmmunitionType.Empty;

            AddHtmlLocalized(0, 10, 300, 16, 1149614 + (int)Cannon.Position, 21758, false, false);

            AddHtmlLocalized(45, 40, 100, 16, 1149626, LabelColor, false, false); //CLEAN

            if (!charged)
                AddHtmlLocalized(45, 60, 100, 16, 1149630, LabelColor, false, false); //CHARGE
            else
                AddHtmlLocalized(45, 60, 100, 16, 1149629, LabelColor, false, false); //REMOVE

            if (!loaded)
                AddHtmlLocalized(45, 80, 100, 16, 1149635, LabelColor, false, false); //LOAD
            else
                AddHtmlLocalized(45, 80, 100, 16, 1149629, LabelColor, false, false); //REMOVE

            if (!primed)
                AddHtmlLocalized(45, 100, 100, 16, 1149637, LabelColor, false, false); //PRIME
            else if (cannon.CanLight)
                AddHtmlLocalized(45, 100, 100, 16, 1149638, LabelColor, false, false); //FIRE
            else
                AddHtmlLocalized(45, 100, 100, 16, 1149629, LabelColor, false, false); //REMOVE

            if (!cleaned)
                AddHtmlLocalized(150, 40, 100, 16, 1149628, RedHue, false, false); //Dirty
            else
                AddHtmlLocalized(150, 40, 100, 16, 1149627, GreenHue, false, false); //Clean

            if (!charged)
                AddHtmlLocalized(150, 60, 100, 16, 1149632, RedHue, false, false); //Not Charged
            else
                AddHtmlLocalized(150, 60, 100, 16, 1149631, GreenHue, false, false); //Charged

            if (!loaded)
                AddHtmlLocalized(150, 80, 100, 16, 1149636, RedHue, false, false); //Not Loaded
            else
                AddHtmlLocalized(150, 80, 100, 16, 1114057, AmmoInfo.GetAmmoName(cannon).ToString(), GreenHue, false,
                    false);
            //AddHtmlLocalized(150, 80, 100, 16, cannon.AmmoType == AmmoType.Grapeshot ? 1095741 : 1116029, GreenHue, false, false);

            if (!primed)
                AddHtmlLocalized(150, 100, 100, 16, 1149639, RedHue, false, false); //No Fuse
            else
                AddHtmlLocalized(150, 100, 100, 16, 1149640, GreenHue, false, false); //Primed

            AddButton(10, 40, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0);
            AddButton(10, 60, 0xFA5, 0xFA7, 2, GumpButtonType.Reply, 0);
            AddButton(10, 80, 0xFA5, 0xFA7, 3, GumpButtonType.Reply, 0);
            AddButton(10, 100, 0xFA5, 0xFA7, 4, GumpButtonType.Reply, 0);

            if (!cannon.Actions.ContainsKey(from) || cannon.Actions[from].Count == 0)
                cannon.AddAction(from, 1149653); //You are now operating the cannon.

            var y = 170;
            var count = cannon.Actions[from].Count - 1;
            var hue = 0;

            for (var i = count; i >= 0; i--)
            {
                if (i < count - 3)
                    break;

                if (i == count)
                    hue = 29315; //0xFFFF00;
                else hue = 12684;

                AddHtmlLocalized(10, y, 385, 20, cannon.Actions[from][i], hue, false, false);
                y -= 16;
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = sender.Mobile;

            if (Cannon == null || Cannon.Deleted || !from.InRange(Cannon.Location, 3))
                return;

            switch (info.ButtonID)
            {
                default:
                case 0: return;
                case 1: //clean
                    Cannon.TryClean(from);
                    break;
                case 2: //Charge
                    if (!Cannon.Charged)
                        Cannon.TryCharge(from);
                    else
                        Cannon.RemoveCharge(from);
                    break;
                case 3: //load
                    if (Cannon.AmmoType == AmmunitionType.Empty)
                        Cannon.TryLoad(from);
                    else
                        Cannon.RemoveLoad(from);
                    break;
                case 4: //prime
                    if (!Cannon.Primed)
                        Cannon.TryPrime(from);
                    else if (Cannon.CanLight)
                        Cannon.TryLightFuse(from);
                    else
                        Cannon.RemovePrime(from);
                    break;
            }

            from.SendGump(new CannonGump(Cannon, m_From));
        }
    }
}
