using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.EpicAlignment
{
    // =========================================================================
    // 👹 ВЫСШИЙ ТЁМНЫЙ БОСС: АСТРАЛЬНЫЙ ЖНЕЦ БЕЗДНЫ (ХРАНИТЕЛЬ ПЕЧАТИ)
    // =========================================================================
    public class EpicMondainBoss : BaseCreature
    {
        [Constructable]
        public EpicMondainBoss() : base(AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "Астральный Жнец [ ХРАНИТЕЛЬ ПЕЧАТИ МОНДЕЙНА ]";
            Body = 310; // Графика величественного Демона Бездны / Экзекутора
            Hue = 1175; // Ядовито-зеленый цвет Хаоса

            // Ультимативные характеристики для хардкорного соло-кооп боя
            HitsMaxSeed = 35000;
            Hits = 35000;

            ManaMaxSeed = 5000;
            Mana = 5000;

            SetStr(600, 750);
            SetDex(150, 200);
            SetInt(300, 450);

            SetDamage(65, 100); // Огромный урон в ближнем бою

            // Защита и сопротивления (Резисты)
            SetResistance(ResistanceType.Physical, 75);
            SetResistance(ResistanceType.Fire, 60);
            SetResistance(ResistanceType.Cold, 60);
            SetResistance(ResistanceType.Poison, 90); // Высокий иммунитет к ядам
            SetResistance(ResistanceType.Energy, 65);

            // Навыки (Skills) Высшего Босса
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Swords, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Anatomy, 120.0);
            SetSkill(SkillName.MagicResist, 130.0);

            Fame = 25000;
            Karma = -25000;
            Tamable = false;

            // Генерируем рюкзак сокровищ с Diablo-лутом Тьмы
            Container pack = Backpack;
            if (pack == null)
            {
                pack = new Backpack();
                pack.Movable = false;
                AddItem(pack);
            }

            pack.DropItem(new Gold(Utility.RandomMinMax(10000, 15000)));

            for (int i = 0; i < 6; i++)
            {
                Item magicItem = Loot.RandomArmorOrShieldOrJewelry();
                if (magicItem != null)
                {
                    // Накладываем высокие случайные Diablo-свойства
                    BaseRunicTool.ApplyAttributesTo(magicItem, 5, 95, 100);
                    pack.DropItem(magicItem);
                }
            }
        }

        // 🔥 КАСТОМНЫЙ ИИ: Проклятие Крови (AoE-Вампиризм по площади)
        public override void OnThink()
        {
            base.OnThink();

            if (Alive && Combatant != null && Utility.RandomDouble() < 0.05)
            {
                this.PlaySound(0x1FB); // Потусторонний загробный звук
                this.PublicOverheadMessage(Network.MessageType.Regular, 0x22, false, "*Жнец высасывает жизненные соки живых существ вокруг!*");

                List<Mobile> targets = new List<Mobile>();
                IPooledEnumerable mobiles = Map.GetMobilesInRange(Location, 6);
                
                foreach (Mobile m in mobiles)
                {
                    // Целями становятся только игроки, их питомцы и наёмники
                    if (m != this && m.Alive && (m.Player || (m is BaseCreature c && (c.Controlled || c.Summoned))))
                        targets.Add(m);
                }
                mobiles.Free();

                int totalVampireHeal = 0;

                foreach (Mobile m in targets)
                {
                    int dmg = Utility.RandomMinMax(25, 45);
                    m.Damage(dmg, this);
                    
                    // Эффект кровавых нитей, тянущихся к боссу
                    m.FixedParticles(0x374A, 10, 30, 5052, 1175, 0, EffectLayer.Waist);
                    m.SendMessage(0x22, "Ваша жизненная энергия уходит на подпитку Жнеца Бездны!");
                    
                    totalVampireHeal += dmg;
                }

                // Босс исцеляет себя на сумму нанесенного урона
                if (totalVampireHeal > 0)
                {
                    this.Hits = Math.Min(this.HitsMax, this.Hits + totalVampireHeal);
                    this.FixedParticles(0x376A, 9, 32, 5005, 1175, 0, EffectLayer.Waist); // Зеленые искры на боссе
                }
            }
        }

        public EpicMondainBoss(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}
