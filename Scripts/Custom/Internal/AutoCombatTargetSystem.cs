using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.AutoCombat
{
    // ГЛОБАЛЬНЫЙ ДВИЖОК АВТО-АТАКИ БЛИЖАЙШЕЙ ЦЕЛИ
    public static class AutoCombatTargetSystem
    {
        private static readonly Dictionary<PlayerMobile, Timer> _activeTimers = new Dictionary<PlayerMobile, Timer>();

        public static void Initialize()
        {
            // Хукаем смерть существ для авто-переключения на следующего врага
            EventSink.CreatureDeath += new CreatureDeathEventHandler(OnCreatureDeath);
            
            // Хукаем движение игроков, чтобы активировать интеллектуальный радар боя
            EventSink.Movement += new MovementEventHandler(OnPlayerMovement);
        }

        // ИНТЕЛЛЕКТУАЛЬНЫЙ РАДАР: Следит за статусом Warmode и автоматически дает сдачи в спину! 🛡️💥
        private static void OnPlayerMovement(MovementEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null || !pm.Alive || pm.AccessLevel > AccessLevel.Player) return;

            // Если игрок включил Warmode — запускаем для него индивидуальный фоновый радар боя
            if (pm.Warmode)
            {
                if (!_activeTimers.ContainsKey(pm))
                {
                    Timer combatTimer = Timer.DelayCall(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5), () => CheckCombatRadar(pm));
                    combatTimer.Start();
                    _activeTimers[pm] = combatTimer;
                }
            }
            else
            {
                // Если Warmode выключен — глушим радар, чтобы не тратить процессоры сервера
                if (_activeTimers.TryGetValue(pm, out Timer timer))
                {
                    timer.Stop();
                    _activeTimers.Remove(pm);
                }
            }
        }

        // ФОНОВЫЙ СКАНЕР: Ловит урон в спину и ищет обидчиков
        private static void CheckCombatRadar(PlayerMobile pm)
        {
            if (pm == null || !pm.Alive || !pm.Warmode)
            {
                if (pm != null && _activeTimers.TryGetValue(pm, out Timer timer))
                {
                    timer.Stop();
                    _activeTimers.Remove(pm);
                }
                return;
            }

            // 🔥 ТВОЁ ТЗ: Если игрока кто-то бьет, а у него НЕТ активной цели — включаем авто-контратаку!
            if (pm.Combatant == null && pm.Aggressors != null && pm.Aggressors.Count > 0)
            {
                Mobile closestAggressor = null;
                double closestDist = double.MaxValue;

                // Перебираем всех, кто прямо сейчас наносит урон игроку
                foreach (AggressorInfo info in pm.Aggressors)
                {
                    Mobile attacker = info.Attacker;
                    if (attacker != null && attacker.Alive && !attacker.Deleted && pm.InRange(attacker.Location, 6))
                    {
                        double d = pm.GetDistanceToSqrt(attacker.Location);
                        if (d < closestDist)
                        {
                            closestDist = d;
                            closestAggressor = attacker;
                        }
                    }
                }

                // Автоматически разворачиваем персонажа на обидчика! 🎉⚔️
                if (closestAggressor != null)
                {
                    pm.Combatant = closestAggressor;
                    pm.SendMessage(0x35, $"[Бой]: Контратака против агрессора {closestAggressor.Name}!");
                }
            }
        }

        // ТРИГГЕР 1: Текущая цель погибла — плавно ищем следующую живую мишень вокруг!
        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature victim = e.Creature as BaseCreature;
            if (victim == null) return;

            IPooledEnumerable eable = victim.GetMobilesInRange(12);
            foreach (Mobile m in eable)
            {
                if (m is PlayerMobile pm && pm.Alive && pm.Combatant == victim)
                {
                    PlayerMobile targetPlayer = pm;
                    Timer.DelayCall(TimeSpan.FromMilliseconds(100), () => ReacquireNextNearestTarget(targetPlayer));
                }
            }
            eable.Free();
        }

        // Сканируем 6 клеток вокруг игрока и вычисляем ближайшего моба-врага
        public static void ReacquireNextNearestTarget(PlayerMobile pm)
        {
            if (pm == null || !pm.Alive || !pm.Warmode) return;

            Mobile closestEnemy = null;
            double closestDistance = double.MaxValue;

            IPooledEnumerable eable = pm.GetMobilesInRange(6);
            foreach (Mobile m in eable)
            {
                if (m is BaseCreature bc && bc.Alive && !bc.Deleted && bc.IsEnemy(pm))
                {
                    if (pm.CanSee(bc) && pm.InLOS(bc))
                    {
                        double dist = pm.GetDistanceToSqrt(bc.Location);
                        if (dist < closestDistance)
                        {
                            closestDistance = dist;
                            closestEnemy = bc;
                        }
                    }
                }
            }
            eable.Free();

            // Если нашли живую мишень — переключаем меч на нее!
            if (closestEnemy != null)
            {
                pm.Combatant = closestEnemy;
                pm.SendMessage(0x59, $"[Бой]: Следующая цель — {closestEnemy.Name}!");
            }
            else
            {
                pm.Combatant = null;
                // Если врагов нет — мягко гасим радар
                if (_activeTimers.TryGetValue(pm, out Timer timer))
                {
                    timer.Stop();
                    _activeTimers.Remove(pm);
                }
            }
        }
    }
}
