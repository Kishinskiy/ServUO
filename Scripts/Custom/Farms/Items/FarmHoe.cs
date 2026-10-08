using System;
using Server;
using Server.Targeting;
using Server.Network;
using Server.Custom.Farms;

namespace Server.Items
{
    public class FarmHoe : Item
    {
        [Constructable]
        public FarmHoe() : base(0xE86) // Оригинальный графический ID тяпки в UO
        {
            Name = "Фермерская тяпка для прополки";
            Weight = 4.0;
            Layer = Layer.TwoHanded; // Тяпка экипируется в две руки
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Player)
                return;

            // Тяпка должна быть либо в руках, либо в сумке игрока
            if (!this.IsChildOf(from.Backpack) && this.Parent != from)
            {
                from.SendLocalizedMessage(1042001); // This must be in your backpack или экипировано
                return;
            }

            from.SendMessage("Выберите грядку, которую необходимо прополоть от сорняков.");
            from.Target = new HoeTarget(this); // Запуск прицела
        }

        public FarmHoe(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // ТАРГЕТ ТЯПКИ
    public class HoeTarget : Target
    {
        private FarmHoe m_Hoe;

        public HoeTarget(FarmHoe hoe) : base(2, true, TargetFlags.None)
        {
            m_Hoe = hoe;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Hoe == null || m_Hoe.Deleted)
                return;

            if (targeted is FarmVegetablePlant plant)
            {
                if (!from.InRange(plant.GetWorldLocation(), 2))
                {
                    from.SendLocalizedMessage(500446); // Вы слишком далеко.
                    return;
                }

                if (plant.IsDead)
                {
                    from.SendMessage("Это растение уже погибло. Тяпка тут не поможет, выдерните его руками.");
                    return;
                }

                if (plant.Stage >= 2)
                {
                    from.SendMessage("Урожай созрел! Сорняки ему больше не мешают, собирайте плоды.");
                    return;
                }

                if (plant.WeedsLevel <= 0)
                {
                    from.SendMessage("Эта грядка абсолютно чистая, сорняков здесь нет.");
                    return;
                }

                // Пропалываем грядку, снижая уровень сорняков на 35% за один удар тяпки
                plant.RemoveWeeds(from, 35);
            }
            else
            {
                from.SendMessage("Тяпку можно использовать только для прополки сорняков на грядках!");
            }
        }
    }
}
