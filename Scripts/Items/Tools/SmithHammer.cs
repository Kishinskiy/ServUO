using Server.Engines.Craft;

namespace Server.Items
{
    [FlipableAttribute(0x13E3, 0x13E4)]
    public class SmithHammer : BaseTool
    {
        public override CraftSystem CraftSystem => DefBlacksmithy.CraftSystem;

        [Constructable]
        public SmithHammer()
            : base(0x13E3)
        {
        }

        [Constructable]
        public SmithHammer(int uses)
            : base(uses, 0x13E3)
        {
            Weight = 8.0;
            Layer = Layer.OneHanded;
        }

        public SmithHammer(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class SmithyHammer : BaseBashing, ITool
    {
        public override WeaponAbility PrimaryAbility => WeaponAbility.CrushingBlow;

        public override WeaponAbility SecondaryAbility => WeaponAbility.ParalyzingBlow;

        public override int AosStrengthReq => 5;

        public override int AosMinDamage => 13;

        public override int AosMaxDamage => 17;

        public override int AosSpeed => 40;

        public override float MlSpeed => 3.25f;

        public override int OldStrengthReq => 5;

        public override int InitMinHits => 31;

        public override int InitMaxHits => 70;

        [Constructable]
        public SmithyHammer()
            : base(0x13E3)
        {
            Weight = 8.0;
            ShowUsesRemaining = true;
        }

        public SmithyHammer(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (CraftSystem != null && (IsChildOf(from.Backpack) || Parent == from))
            {
                var num = CraftSystem.CanCraft(from, this, null);

                if (num > 0 && (num != 1044267 ||
                                !Core.SE)) // Blacksmithing shows the gump regardless of proximity of an anvil and forge after SE
                {
                    from.SendLocalizedMessage(num);
                }
                else
                {
                    var context = CraftSystem.GetContext(from);

                    from.SendGump(new CraftGump(from, CraftSystem, this, null));
                }
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        #region ITool Members

        public CraftSystem CraftSystem => DefBlacksmithy.CraftSystem;
        public bool BreakOnDepletion => true;

        public bool CheckAccessible(Mobile m, ref int num)
        {
            if (!IsChildOf(m) && Parent != m)
            {
                num = 1044263;
                return false;
            }

            return true;
        }

        #endregion
    }
}
