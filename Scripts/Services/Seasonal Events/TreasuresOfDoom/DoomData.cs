using System;
using System.Collections.Generic;
using Server.Engines.SeasonalEvents;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Points
{
    public class DoomData : PointsSystem
    {
        private readonly TextDefinition m_Name = null;
        public override PointsType Loyalty => PointsType.Doom;
        public override TextDefinition Name => m_Name;
        public override bool AutoAdd => true;
        public override double MaxPoints => double.MaxValue;
        public override bool ShowOnLoyaltyGump => false;

        public bool InSeason => SeasonalEventSystem.IsActive(EventType.TreasuresOfDoom);

        public Dictionary<Mobile, int> DungeonPoints { get; set; }
        public bool Enabled { get; set; }

        public DoomData()
        {
            DungeonPoints = new Dictionary<Mobile, int>();
        }

        public override void SendMessage(PlayerMobile from, double old, double points, bool quest)
        {
            from.SendLocalizedMessage(1155590,
                ((int)points).ToString()); // You have turned in ~1_COUNT~ artifacts of Doom
        }

        public override void ProcessKill(Mobile victim, Mobile damager)
        {
            var bc = victim as BaseCreature;

            if (!InSeason || bc == null || bc.Controlled || bc.Summoned || !damager.Alive || damager.Deleted ||
                bc.IsChampionSpawn)
                return;

            var r = bc.Region;

            if (damager is PlayerMobile && r.IsPartOf("Doom"))
            {
                if (!DungeonPoints.ContainsKey(damager))
                    DungeonPoints[damager] = 0;

                var fame = bc.Fame / 2;
                var luck = Math.Max(0, ((PlayerMobile)damager).RealLuck);

                DungeonPoints[damager] += (int)(fame * (1 + Math.Sqrt(luck) / 100));

                var x = DungeonPoints[damager];
                const double A = 0.000863316841;
                const double B = 0.00000425531915;

                var chance = A * Math.Pow(10, B * x);

                if (chance > Utility.RandomDouble())
                {
                    var i = Loot.RandomArmorOrShieldOrWeaponOrJewelry(LootPackEntry.IsInTokuno(bc),
                        LootPackEntry.IsMondain(bc), LootPackEntry.IsStygian(bc));

                    if (i != null)
                    {
                        RunicReforging.GenerateRandomItem(i, damager,
                            Math.Max(100, RunicReforging.GetDifficultyFor(bc)), RunicReforging.GetLuckForKiller(bc),
                            ReforgedPrefix.None, ReforgedSuffix.Doom);

                        damager.PlaySound(0x5B4);
                        damager.SendLocalizedMessage(
                            1155588); // You notice the crest of Doom on your fallen foe's equipment and decide it may be of some value...

                        if (!damager.PlaceInBackpack(i))
                        {
                            if (damager.BankBox != null && damager.BankBox.TryDropItem(damager, i, false))
                            {
                                damager.SendLocalizedMessage(1079730); // The item has been placed into your bank box.
                            }
                            else
                            {
                                damager.SendLocalizedMessage(
                                    1072523); // You find an artifact, but your backpack and bank are too full to hold it.
                                i.MoveToWorld(damager.Location, damager.Map);
                            }
                        }

                        DungeonPoints.Remove(damager);
                    }
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(Enabled);

            writer.Write(DungeonPoints.Count);
            foreach (var kvp in DungeonPoints)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Enabled = reader.ReadBool();

            var count = reader.ReadInt();
            for (var i = 0; i < count; i++)
            {
                var m = reader.ReadMobile();
                var points = reader.ReadInt();

                if (m != null && points > 0)
                    DungeonPoints[m] = points;
            }
        }
    }
}
