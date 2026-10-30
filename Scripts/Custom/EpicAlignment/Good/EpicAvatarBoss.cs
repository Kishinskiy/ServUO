using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.EpicAlignment
{
    // =========================================================================
    // 👑 ВЫСШИЙ КАСТОМНЫЙ БОСС: ЭФИРНОЕ ЗЕРКАЛО ИСТИНЫ (ЭНДГЕЙМ)
    // =========================================================================
    public class EpicAvatarBoss : Travesty
    {
        [Constructable]
        public EpicAvatarBoss() : base()
        {
            // Настройки ультимативных характеристик эндгейм-босса
            Name = "Эфирное Зеркало Судьбы [ ВЫСШИЙ СУДИЯ ]";
            Hue = 2418; // Мерцающее сияние Аватара
            
            HitsMaxSeed = 35000; // 35,000 HP для сурового финального соло-кооп боя
            Hits = 35000;
            
            SetDamage(60, 95); // Высокий физический урон
            Tamable = false;   // Запрет на приручение
            Fame = 25000;
            Karma = -15000;    // Босс олицетворяет грехи, поэтому карма отрицательная

            // Генерируем рюкзак с сокровищами внутри босса
            Container pack = Backpack;
            if (pack == null)
            {
                pack = new Backpack();
                pack.Movable = false;
                AddItem(pack);
            }

            // Насыпаем золото и гарантированный топовый Diablo-лут
            pack.DropItem(new Gold(Utility.RandomMinMax(10000, 15000)));

            for (int i = 0; i < 6; i++)
            {
                Item magicItem = Loot.RandomArmorOrShieldOrJewelry();
                if (magicItem != null)
                {
                    // Накладываем высокие случайные Diablo-характеристики (AOS свойства)
                    BaseRunicTool.ApplyAttributesTo(magicItem, 5, 95, 100);
                    pack.DropItem(magicItem);
                }
            }
        }

        // Кастомная ИИ-способность: Взрыв ложной праведности (АоЕ урон по площади вокруг босса)
        public override void OnThink()
        {
            base.OnThink();

            if (Alive && Combatant != null && Utility.RandomDouble() < 0.05)
            {
                this.PlaySound(0x2F3); // Зловещий крик
                this.PublicOverheadMessage(Network.MessageType.Regular, 0x22, false, "*Зеркало искажает пространство, взрываясь световой волной!*");

                List<Mobile> targets = new List<Mobile>();
                IPooledEnumerable mobiles = Map.GetMobilesInRange(Location, 5);
                
                foreach (Mobile m in mobiles)
                {
                    if (m != this && m.Alive && (m.Player || (m is BaseCreature c && (c.Controlled || c.Summoned))))
                        targets.Add(m);
                }
                mobiles.Free();

                foreach (Mobile m in targets)
                {
                    m.Damage(Utility.RandomMinMax(30, 55), this);
                    m.FixedParticles(0x3709, 10, 30, 5052, 2418, 0, EffectLayer.Waist);
                    m.SendMessage(0x22, "Ваш разум атакован астральным отражением ваших собственных страхов!");
                }
            }
        }

        public EpicAvatarBoss(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}
