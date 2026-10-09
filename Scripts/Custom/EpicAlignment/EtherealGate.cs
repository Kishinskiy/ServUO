using System;
using Server;
using Server.Mobiles;
using Server.Gumps;
using Server.Commands; // Подключили для обработки системного вещания
using Server.Custom.EpicAlignment;

namespace Server.Items
{
    public class EtherealGate : Item
    {
        [Constructable]
        public EtherealGate() : base(0x1FD4) // Задали графику огромных Звездных Врат (Moongate)
        {
            Name = "Врата Перерождения Полубогов";
            Hue = 2418; // Свечение Аватара
            Movable = false;
            Light = LightType.Circle300; // Сильное круговое свечение вокруг портала
        }

        public EtherealGate(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Player) return;

            PlayerMobile pm = from as PlayerMobile;
            if (pm == null) return;

            EpicRank rank = EpicAlignmentSystem.GetRank(pm);
            if (rank == EpicRank.None)
            {
                pm.SendMessage(0x22, "Эти врата созданы из чистой эфирной энергии. Смертным запрещено прикасаться к ним!");
                return;
            }

            if (pm.Alive)
            {
                pm.SendMessage(0x35, "Вы уже находитесь в мире живых.");
                return;
            }

            var pointsData = Server.Engines.XmlSpawner2.XmlAttach.FindAttachment(pm, typeof(Server.Engines.XmlSpawner2.XmlValue), "EtherealEssence") as Server.Engines.XmlSpawner2.XmlValue;
            int currentEssence = pointsData != null ? pointsData.Value : 0;

            if (currentEssence < 100)
            {
                pm.SendMessage(0x22, $"Врата безмолвствуют. Вам не хватает призрачной энергии! Собрано: {currentEssence} / 100 ед.");
                pm.SendMessage(0x5D, "Охотьтесь на Эфирные Аномалии на острове, чтобы впитать их силу.");
                return;
            }

            // УСПЕШНОЕ ВОСКРЕСЕНИЕ ПОЛУБОГА 🎉
            if (pointsData != null) pointsData.Value = 0; 

            pm.Resurrect(); 
            pm.Hits = pm.HitsMax;
            pm.Mana = pm.ManaMax;
            pm.Stam = pm.StamMax;

            Point3D spawnFort = (rank == EpicRank.Avatar) ? EpicAlignmentSystem.LightFortress : EpicAlignmentSystem.DarkFortress;
            Map targetMap = (rank == EpicRank.Avatar) ? Map.Felucca : Map.Malas;
            pm.MoveToWorld(spawnFort, targetMap);

            Effects.PlaySound(pm.Location, pm.Map, 0x212);
            Effects.SendLocationParticles(EffectItem.Create(pm.Location, pm.Map, TimeSpan.FromSeconds(2.0)), 0x375A, 9, 20, 2418, 0, 5027, 0);

            // ИСПРАВЛЕНО: Используем нативное глобальное вещание ServUO на весь сервер
            string rankName = (rank == EpicRank.Avatar) ? "Аватар Света" : "Рука Мондейна";
            string broadcastText = $"[Триумф] Полубог {pm.Name} накопил 100 эссенций, активировал Врата Перерождения и вернулся в мир живых как {rankName}!";
            
            World.Broadcast(0x35, true, broadcastText);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
