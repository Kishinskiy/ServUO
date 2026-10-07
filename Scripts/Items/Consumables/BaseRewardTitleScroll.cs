using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public abstract class BaseRewardTitleToken : Item
    {
        public List<Tuple<TextDefinition, Type>> Titles { get; set; }

        public override int LabelNumber => 1156719; // Reward Title Scroll

        public BaseRewardTitleToken(int id) : base(id)
        {
            Titles = new List<Tuple<TextDefinition, Type>>();
            InitializeTitles();
        }

        public BaseRewardTitleToken(Serial serial) : base(serial)
        {
        }

        public abstract void InitializeTitles();

        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile && IsChildOf(from.Backpack) && Titles != null && Titles.Count > 0)
                from.SendGump(new InternalGump(from as PlayerMobile, this));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            Titles = new List<Tuple<TextDefinition, Type>>();
            InitializeTitles();
        }

        private class InternalGump : Gump
        {
            public BaseRewardTitleToken Token { get; }
            public PlayerMobile User { get; }

            public InternalGump(PlayerMobile pm, BaseRewardTitleToken token) : base(50, 50)
            {
                Token = token;
                User = pm;

                AddGumpLayout();
            }

            public void AddGumpLayout()
            {
                AddBackground(0, 0, 300, 350, 9270);
                AddHtmlLocalized(0, 25, 300, 16, 1154645, "#1156718", 0xFFFF, false,
                    false); // Select a reward title deed:

                var i = 0;
                Token.Titles.ForEach(tuple =>
                {
                    AddButton(23, 68 + i * 20, 1209, 1210, i + 1, GumpButtonType.Reply, 0);

                    var textdef = tuple.Item1;

                    if (textdef.Number > 0)
                        AddHtmlLocalized(50, 65 + i * 20, 240, 20, textdef.Number, 0xFFFF, false, false);
                    else if (!string.IsNullOrEmpty(textdef.String))
                        AddHtml(50, 65 + i * 20, 240, 20, string.Format("<basefond color=#FFFFFF>{0}", textdef.String),
                            false, false);

                    i++;
                });
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                if (Token != null && !Token.Deleted)
                {
                    var id = info.ButtonID - 1;

                    if (id >= 0 && id < Token.Titles.Count)
                    {
                        var tuple = Token.Titles[id];

                        var item = Loot.Construct(tuple.Item2);

                        if (item != null)
                        {
                            User.AddToBackpack(item);
                            Token.Delete();

                            //TODO: Message?
                        }
                    }
                }
            }
        }
    }
}
