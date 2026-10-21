using System;
using Server;
using Server.Items;
using Server.Network;

namespace Server.Custom.MagicProfessions
{
    public class ArcaneLeylineNode : Item
    {
        private const int MaxCharges = 5;
        private int _charges;
        private DateTime _rechargeAt;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges { get => _charges; set { _charges = value; InvalidateProperties(); } }

        [Constructable]
        public ArcaneLeylineNode() : base(0x1F19)
        {
            Name = "Узел линии силы";
            Hue = 1165;
            Movable = false; // Всегда зафиксирован (и для спавна в данжах, и при установке админом)
            _charges = MaxCharges;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive || from.Map != Map || !from.InRange(Location, 2) || Map == null || Map == Map.Internal)
            {
                from?.SendLocalizedMessage(500295);
                return;
            }
            if (from.Skills[SkillName.Magery].Value < 50.0 || from.Skills[SkillName.Inscribe].Value < 40.0)
            {
                from.SendMessage(0x22, "Для сбора энергии нужны Magery 50.0 и Inscription 40.0.");
                return;
            }
            if (_charges <= 0)
            {
                if (DateTime.UtcNow < _rechargeAt)
                {
                    from.SendMessage(0x22, $"Узел истощен. Энергия восстановится: {_rechargeAt.ToLocalTime():g}.");
                    return;
                }
                _charges = MaxCharges;
            }

            int skill = (int)((from.Skills[SkillName.Magery].Value + from.Skills[SkillName.Inscribe].Value) / 2.0);
            int amount = Utility.RandomMinMax(1, skill >= 90.0 ? 3 : 2);
            LeylineFragment fragments = new LeylineFragment(amount);

            if (from.Backpack == null || !from.AddToBackpack(fragments))
            {
                fragments.Delete();
                from.SendMessage(0x22, "Освободите место в рюкзаке.");
                return;
            }

            _charges--;
            if (_charges == 0)
            {
                from.SendMessage(0x22, "Узел полностью истощился и растворился в линиях силы мира...");
                
                // Запускаем создание новой случайной ноды на этой же карте через 2 часа
                Map currentMap = this.Map;
                if (currentMap != null && currentMap != Map.Internal)
                {
                    Timer.DelayCall(TimeSpan.FromHours(2.0), () => 
                    {
                        MagicProfessionSpawner.SpawnSingleNode(currentMap);
                    });
                }

                this.Delete(); // Удаляем текущую пустую ноду из мира навсегда
                return;
            }

            from.PlaySound(0x1F5);
            from.SendMessage(0x59, $"Вы извлекли {amount} фрагм. линии силы. Зарядов узла осталось: {_charges}/{MaxCharges}.");
        }

        public ArcaneLeylineNode(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write(_charges); writer.Write(_rechargeAt); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); _charges = reader.ReadInt(); _rechargeAt = reader.ReadDateTime(); }
    }
}
