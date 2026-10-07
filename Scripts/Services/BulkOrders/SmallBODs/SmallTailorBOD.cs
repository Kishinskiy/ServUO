using System;
using System.Collections.Generic;
using Server.Engines.Craft;

namespace Server.Engines.BulkOrders
{
    public class SmallTailorBOD : SmallBOD
    {
        public static double[] m_TailoringMaterialChances =
        {
            0.857421875, // None
            0.125000000, // Spined
            0.015625000, // Horned
            0.001953125 // Barbed
        };

        public override BODType BODType => BODType.Tailor;

        [Constructable]
        public SmallTailorBOD()
        {
            SmallBulkEntry[] entries;
            bool useMaterials;

            if (useMaterials = Utility.RandomBool())
                entries = SmallBulkEntry.TailorLeather;
            else
                entries = SmallBulkEntry.TailorCloth;

            if (entries.Length > 0)
            {
                var hue = 0x483;
                var amountMax = Utility.RandomList(10, 15, 20);

                BulkMaterialType material;

                if (useMaterials)
                    material = GetRandomMaterial(BulkMaterialType.Spined, m_TailoringMaterialChances);
                else
                    material = BulkMaterialType.None;

                var reqExceptional = Utility.RandomBool() || material == BulkMaterialType.None;

                var entry = entries[Utility.Random(entries.Length)];

                Hue = hue;
                AmountMax = amountMax;
                Type = entry.Type;
                Number = entry.Number;
                Graphic = entry.Graphic;
                RequireExceptional = reqExceptional;
                Material = material;
                GraphicHue = entry.Hue;
            }
        }

        public SmallTailorBOD(int amountCur, int amountMax, Type type, int number, int graphic, bool reqExceptional,
            BulkMaterialType mat, int hue)
        {
            Hue = 0x483;
            AmountMax = amountMax;
            AmountCur = amountCur;
            Type = type;
            Number = number;
            Graphic = graphic;
            RequireExceptional = reqExceptional;
            Material = mat;
            GraphicHue = hue;
        }

        public SmallTailorBOD(Serial serial)
            : base(serial)
        {
        }

        private SmallTailorBOD(SmallBulkEntry entry, BulkMaterialType material, int amountMax, bool reqExceptional)
        {
            Hue = 0x483;
            AmountMax = amountMax;
            Type = entry.Type;
            Number = entry.Number;
            Graphic = entry.Graphic;
            RequireExceptional = reqExceptional;
            Material = material;
        }

        public static SmallTailorBOD CreateRandomFor(Mobile m)
        {
            SmallBulkEntry[] entries;
            var useMaterials = Utility.RandomBool();

            var theirSkill = BulkOrderSystem.GetBODSkill(m, SkillName.Tailoring);
            if (useMaterials &&
                theirSkill >=
                6.2) // Ugly, but the easiest leather BOD is Leather Cap which requires at least 6.2 skill.
                entries = SmallBulkEntry.TailorLeather;
            else
                entries = SmallBulkEntry.TailorCloth;

            if (entries.Length > 0)
            {
                int amountMax;

                if (theirSkill >= 70.1)
                    amountMax = Utility.RandomList(10, 15, 20, 20);
                else if (theirSkill >= 50.1)
                    amountMax = Utility.RandomList(10, 15, 15, 20);
                else
                    amountMax = Utility.RandomList(10, 10, 15, 20);

                var material = BulkMaterialType.None;

                if (useMaterials && theirSkill >= 70.1)
                    for (var i = 0; i < 20; ++i)
                    {
                        var check = GetRandomMaterial(BulkMaterialType.Spined, m_TailoringMaterialChances);
                        var skillReq = GetRequiredSkill(check);

                        if (theirSkill >= skillReq)
                        {
                            material = check;
                            break;
                        }
                    }

                var excChance = 0.0;

                if (theirSkill >= 70.1)
                    excChance = (theirSkill + 80.0) / 200.0;

                var reqExceptional = excChance > Utility.RandomDouble();

                var system = DefTailoring.CraftSystem;

                var validEntries = new List<SmallBulkEntry>();

                for (var i = 0; i < entries.Length; ++i)
                {
                    var item = system.CraftItems.SearchFor(entries[i].Type);

                    if (item != null)
                    {
                        var allRequiredSkills = true;
                        var chance = item.GetSuccessChance(m, null, system, false, ref allRequiredSkills);

                        if (allRequiredSkills && chance >= 0.0)
                        {
                            if (reqExceptional)
                                chance = item.GetExceptionalChance(system, chance, m);

                            if (chance > 0.0)
                                validEntries.Add(entries[i]);
                        }
                    }
                }

                if (validEntries.Count > 0)
                {
                    var entry = validEntries[Utility.Random(validEntries.Count)];
                    return new SmallTailorBOD(entry, material, amountMax, reqExceptional);
                }
            }

            return null;
        }

        public override int ComputeFame()
        {
            return TailorRewardCalculator.Instance.ComputeFame(this);
        }

        public override int ComputeGold()
        {
            return TailorRewardCalculator.Instance.ComputeGold(this);
        }

        public override List<Item> ComputeRewards(bool full)
        {
            var list = new List<Item>();

            var rewardGroup =
                TailorRewardCalculator.Instance.LookupRewards(TailorRewardCalculator.Instance.ComputePoints(this));

            if (rewardGroup != null)
            {
                if (full)
                {
                    for (var i = 0; i < rewardGroup.Items.Length; ++i)
                    {
                        var item = rewardGroup.Items[i].Construct();

                        if (item != null)
                            list.Add(item);
                    }
                }
                else
                {
                    var rewardItem = rewardGroup.AcquireItem();

                    if (rewardItem != null)
                    {
                        var item = rewardItem.Construct();

                        if (item != null)
                            list.Add(item);
                    }
                }
            }

            return list;
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
}
