using System;
using System.Linq;
using Server;
using Server.Mobiles;
using Server.Accounting; // Подключаем пространство имен аккаунтов сервера

namespace Server.Custom.EpicAlignment
{
    // =========================================================================
    // 📊 ДИНАМИЧЕСКИЙ МОДИФИКАТОР МИРОВОГО БАЛАНСА АВАТАРА (ИСПРАВЛЕННЫЙ)
    // =========================================================================
    public static class EpicAvatarWorldBalance
    {
        // 🔥 ИСПРАВЛЕНО: Сканируем абсолютно ВСЕХ персонажей на сервере (включая Offline),
        // чтобы проверить, занято ли место Аватара в истории этого мира!
        public static bool IsAvatarPlaceOccupied()
        {
            // Пробегаемся по всем зарегистрированным аккаунтам в базе ServUO
            foreach (Account acc in Accounts.GetAccounts().Cast<Account>())
            {
                if (acc == null) 
                    continue;

                // В каждом аккаунте проверяем всех созданных персонажей (до 5 слотов)
                for (int i = 0; i < acc.Length; ++i)
                {
                    Mobile m = acc[i];
                    
                    // Если персонаж существует и у него лимит Magery равен 130 — место Аватара занято!
                    if (m != null && !m.Deleted && m.Skills[SkillName.Magery].Cap >= 130.0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // 🔮 МЕТОД ДЛЯ ВАШЕЙ DIABLO-LOOT СИСТЕМЫ: Расчет модификатора шанса проклятий
        // Урезает шанс проклятий в 2 раза, если в мире в принципе существует Аватар (даже если он оффлайн)!
        public static double GetCurseChanceMultiplier()
        {
            if (IsAvatarPlaceOccupied())
            {
                return 0.50; // Проклятия сыплются в 2 раза реже, уступая место чистому луту!
            }

            return 1.00; // Обычный мир — стандартный шанс генерации проклятий
        }

        // 🏙️ МЕТОД ДЛЯ СИСТЕМЫ РАЗВИТИЯ ГОРОДОВ: Расчет бонуса снабжения
        // Дарует +50% к очкам BOD-снабжения светлым городам, если место Аватара занято
        public static double GetCityProgressMultiplier(Server.Engines.CityLoyalty.City city)
        {
            if (!IsAvatarPlaceOccupied())
                return 1.00; // Стандартная скорость, если Аватар еще не рожден

            string cityName = city.ToString().ToLower();
            
            // Если город является тёмным хабом порчи — Аватар накладывает на него штраф -30%
            if (cityName.Contains("umbra") || cityName.Contains("doom") || cityName.Contains("jhelom"))
            {
                return 0.70; 
            }

            // Всем остальным светлым и классическим городам даруется божественный буст +50% к прогрессу!
            return 1.50;
        }
    }
}
