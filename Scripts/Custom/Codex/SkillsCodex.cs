#region Header
//   Voxpire     _,-'/-'/  SkillsCodex.cs
//   .      __,-; ,'( '/
//    \.    `-.__`-._`:_,-._       _ , . ``
//     `:-._,------' ` _,`--` -: `_ , ` ,' :
//        `---..__,,--'  (C) 2022  ` -'. -'
//        #  Vita-Nex [http://core.vita-nex.com]  #
//  {o)xxx|===============-   #   -===============|xxx(o}
//        #        The MIT License (MIT)          #
#endregion

#region References
using Server;
#endregion

namespace VitaNex.Items
{
    public class SkillsCodex : SkillCodex
    {
        [Constructable]
        public SkillsCodex()
        {
            OnCreated();
        }

        [Constructable]
        public SkillsCodex(int count)
            : base(count)
        {
            OnCreated();
        }

        [Constructable]
        public SkillsCodex(int count, double value)
            : base(count, value)
        {
            OnCreated();
        }

        [Constructable]
        public SkillsCodex(int count, double value, bool deleteWhenEmpty)
            : base(count, value, deleteWhenEmpty)
        {
            OnCreated();
        }

        [Constructable]
        public SkillsCodex(int count, double value, bool deleteWhenEmpty, SkillCodexMode mode)
            : base(count, value, deleteWhenEmpty, mode)
        {
            OnCreated();
        }

        [Constructable]
        public SkillsCodex(int count, double value, bool deleteWhenEmpty, SkillCodexMode mode, SkillCodexFlags flags)
            : base(count, value, deleteWhenEmpty, mode, flags)
        {
            OnCreated();
        }

        public SkillsCodex(Serial serial)
            : base(serial)
        { }

        protected virtual void OnCreated()
        { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.SetVersion(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            reader.GetVersion();
        }
    }
}
