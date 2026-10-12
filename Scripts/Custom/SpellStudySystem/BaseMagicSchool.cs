using System;
using Server;
using Server.Items;

namespace Server.Custom.SpellStudySystem
{
    public abstract class BaseMagicSchool
    {
        // Имя школы для вывода игроку (например, "Некромантия")
        public abstract string SchoolName { get; }

        // Метод верификации: подходит ли эта школа под текущую книгу заклинаний
        public abstract bool MatchBook(Spellbook book);

        // Проверка требований для начала изучения конкретного заклинания
        public abstract bool CheckRequirements(Mobile from, Spellbook book, int spellID, int offsetID);

        // Какой вспомогательный навык защищает от физзла (EvalInt, SpiritSpeak, Focus и т.д.)
        public abstract SkillName SupportSkill { get; }

        // Звуки при начале и успешном завершении шага изучения
        public virtual int StartSound => 0x24D; // Перелистывание страниц
        public virtual int SuccessSound => 0x249; // Магический джингл
        public virtual int FailSound => 0x5C3; // Развеивание

        // Текстовые сообщения об успехе или провале
        public virtual string FailMessage => "Вы неверно истолковали тайные знаки! Свиток обратился в пепел.";
        public virtual string CompleteMessage => "Вы полностью расшифровали тайные руны и внесли заклинание в книгу!";

        // Динамический расчет шага прокачки (%) на основе ID заклинания
        public virtual void GetProgressGain(int offsetID, out int minGain, out int maxGain)
        {
            minGain = Math.Max(5, 25 - (offsetID / 2));
            maxGain = Math.Max(10, 40 - (offsetID / 2));
        }
    }
}
