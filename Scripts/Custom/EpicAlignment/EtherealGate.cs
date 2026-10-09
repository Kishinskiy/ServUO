using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Engines.XmlSpawner2;
using Server.Commands;

namespace Server.Custom.EpicAlignment
{
    public class EtherealGate : Item
    {
        public static readonly int RequiredEssence = 100; // Планка эссенции для перерождения

        [Constructable]
        public EtherealGate() : base(0x1F14) // Красивый мистический портал
        {
            Name = "Врата Перерождения Полубогов";
            Movable = false;
            Hue = 0x482; // Призрачный неоновый цвет
            Light = LightType.Circle300;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null) return;

            if (!from.InRange(this.Location, 2))
            {
                from.SendMessage("Вы слишком далеко от Врат.");
                return;
            }

            // На врата могут нажимать только Полубоги
            EpicRank rank = EpicAlignmentSystem.GetRank(from);
            if (rank == EpicRank.None)
            {
                from.SendMessage("Врата перерождения не реагируют на обычные души смертных.");
                return;
            }

            // Выходить можно ТОЛЬКО призраком
            if (from.Alive)
            {
                from.SendMessage("Вы уже живы. Врата Перерождения нужны лишь тем, кто застрял в форме духа.");
                return;
            }

            // Проверяем накопленную эссенцию через XmlPoints/XmlValue из присланного файла
            XmlValue pointsData = XmlAttach.FindAttachment(from, typeof(XmlValue), "EtherealEssence") as XmlValue;
            int currentEssence = pointsData != null ? pointsData.Value : 0;

            if (currentEssence < RequiredEssence)
            {
                from.SendMessage(0x22, $"Вратам требуется {RequiredEssence} единиц Эфирной Эссенции для восстановления вашего физического тела!");
                from.SendMessage(0x59, $"Ваш текущий запас: {currentEssence} / {RequiredEssence}. Продолжайте искать Эфирные Аномалии на острове.");
                return;
            }

            // РИТУАЛ УСПЕШЕН: Списываем эссенцию
            pointsData.Value = 0;

            // Воскрешаем Полубога прямо у портала
            from.Resurrect();
            from.Hits = from.HitsMax;
            from.Mana = from.ManaMax;
            from.Stam = from.StamMax;

            // Эффекты перерождения
            Effects.PlaySound(this.Location, this.Map, 0x212);
            Effects.SendLocationParticles(EffectItem.Create(this.Location, this.Map, TimeSpan.FromSeconds(2.0)), 0x373A, 10, 30, 0x482, 0, 5024, 0);

            // ЛОРНЫЙ МИРОВОЙ ЭВЕНТ (Трансляция на весь шард)
            if (rank == EpicRank.Avatar)
            {
                CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, $"[Эвент] Небесные сферы сошлись! Аватар {from.Name} собрал осколки духа и вернулся в Сосарию из Мира Посмертия!");
                from.MoveToWorld(EpicAlignmentSystem.LightFortress, Map.Trammel); // Возврат в Оплот Света
            }
            else if (rank == EpicRank.HandOfMondain)
            {
                CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x22, $"[Эвент] Пророчество Тьмы исполнено! Воплощение Зла {from.Name} поглотил эфирные аномалии и восстал из мертвых!");
                from.MoveToWorld(EpicAlignmentSystem.DarkFortress, Map.Malas); // Возврат в Цитадель Тьмы
            }
        }

        public EtherealGate(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
