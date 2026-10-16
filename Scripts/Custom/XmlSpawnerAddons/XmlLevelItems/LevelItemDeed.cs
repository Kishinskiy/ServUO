using System;
using Server.Network;
using Server.Prompts;
using Server.Items;
using Server.Targeting;
using Server;
using Server.Engines.XmlSpawner2;

namespace Server.Items
{
	public class LevelItemTarget : Target 
	{
		private LevelItemDeed m_Deed;

		public LevelItemTarget( LevelItemDeed deed ) : base( 1, false, TargetFlags.None )
		{
			m_Deed = deed;
		}

		protected override void OnTarget( Mobile from, object target )
		{
			if (m_Deed == null || m_Deed.Deleted || !m_Deed.IsChildOf(from.Backpack))
			{
				from.SendMessage("The levelable deed must remain in your backpack while you use it.");
				return;
			}

            // 🔥 ИСПРАВЛЕНО: Задействован собственный автономный сканер наковален во избежание ошибки CS1061! 🎉⚙️🛠️
            if (!IsNearAnvil(from, 3))
            {
                from.SendMessage(0x22, "Для проведения ритуала зачарования предмета вы обязаны находиться в кузнице рядом с наковальней!");
                return;
            }

            if (target is Item item && LevelItemManager.IsLevelableItem(item))
			{
				XmlLevelItem a = (XmlLevelItem)XmlAttach.FindAttachment(item, typeof(XmlLevelItem));

				if ( a != null )
				{
					from.SendMessage( "Этот предмет уже обладает возможностью прокачки!");
					return;
				}
				else
				{
					if( item.RootParent != from ) 
					{
						from.SendMessage( "Предмет должен находиться у вас в руках или сумке!" );
					}
					else
					{
						XmlAttach.AttachTo(item, new XmlLevelItem());
						from.SendMessage( 0x58, "Вы успешно перековали предмет на наковальне, наделив его потенциалом развития (Уровень 1)!" );
						
                        from.PlaySound(0x2A); 
                        m_Deed.Delete();
					}
				}
			}
			else
			{
				from.SendMessage( "Вы не можете наложить чары прокачки на эту вещь." );
			}
		}

        // Автономный сканер наковален в радиусе видимости игрока
        private bool IsNearAnvil(Mobile from, int range)
        {
            if (from == null || from.Map == null || from.Map == Map.Internal) return false;

            IPooledEnumerable items = from.Map.GetItemsInRange(from.Location, range);
            foreach (Item item in items)
            {
                // Нативно проверяем любые типы стационарных наковален и кузнечных инструментов ядра
                string name = item.GetType().Name.ToLower();
                if (name.Contains("anvil") || item.ItemID == 0xFAF || item.ItemID == 0xFB0)
                {
                    items.Free();
                    return true;
                }
            }
            items.Free();
            return false;
        }
	}

	public class LevelItemDeed : Item 
	{
		[Constructable]
		public LevelItemDeed() : base( 0x14F0 )
		{
			Weight = 1.0;
			Name = "Свиток Зачарования: Прокачка предметов [Levelable Deed]";
			LootType = LootType.Blessed;
			Hue = 1171;
		}

		public LevelItemDeed( Serial serial ) : base( serial ) { }

		public override void Serialize( GenericWriter writer ) { base.Serialize( writer ); writer.Write( (int) 0 ); }
		public override void Deserialize( GenericReader reader ) { base.Deserialize( reader ); int version = reader.ReadInt(); }

		public override bool DisplayLootType{ get{ return false; } }

		public override void OnDoubleClick( Mobile from ) 
		{
			if ( !IsChildOf( from.Backpack ) ) 
			{
				 from.SendLocalizedMessage( 1042001 ); 
			}
			else
			{
				from.SendMessage("Выберите оружие или броню, которую хотите сделать прокачиваемой:" );
				from.Target = new LevelItemTarget( this ); 
			}
		}	
	}
}
