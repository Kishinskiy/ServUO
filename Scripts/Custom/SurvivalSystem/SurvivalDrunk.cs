using System;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.SurvivalSystem
{
    public static class CheckDrunk
    {
        private static Timer _drunk_check;

        public static void Initialize()
        {
            _drunk_check = Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), DrunkCheck);
        }

        private static void DrunkCheck()
        {
            foreach (var state in NetState.Instances)
            {
                if (state == null)
                    continue;

                var pm = state.Mobile as PlayerMobile;
                if (pm == null || !pm.Alive || pm.Deleted || pm.AccessLevel > AccessLevel.Player)
                    continue;

                var drunk = pm.BAC;

                // =========================================================================
                // ПРОДВИНУТАЯ СИСТЕМА ЭФФЕКТОВ ОПЬЯНЕНИЯ
                // =========================================================================

                if (drunk > 20)
                {
                    pm.AddStatMod(new StatMod(StatType.Int, "MinorMagicBuff", 10, TimeSpan.FromSeconds(120.0)));
                    BuffInfo.AddBuff(pm,
                        new BuffInfo(BuffIcon.Agility, 1060737, 1075841, TimeSpan.FromSeconds(120.0), pm,
                            "Ваша магическая сила возросла."));

                    if (drunk < 40 && Utility.RandomDouble() < 0.05)
                        pm.SendMessage(0x59, "Вы чуствуете прилив магических сил.");
                }
                else
                {
                    if (pm.GetStatMod("MinorMagicBuff") != null)
                    {
                        pm.RemoveStatMod("MinorMagicBuff");
                        BuffInfo.RemoveBuff(pm, BuffIcon.Agility);
                    }
                }

                if (drunk > 40)
                {
                    pm.AddStatMod(new StatMod(StatType.Int, "GreatestMagicBuff", 20, TimeSpan.FromSeconds(120.0)));
                    BuffInfo.AddBuff(pm,
                        new BuffInfo(BuffIcon.SpellPlague, 1060737, 1075841, TimeSpan.FromSeconds(120.0), pm,
                            "Ваша магическая сила огромна."));

                    if (drunk < 60 && Utility.RandomDouble() < 0.05)
                        pm.SendMessage(0x59, "Вы чуствуете невероятную магическую силу.");
                }
                else
                {
                    if (pm.GetStatMod("GreatestMagicBuff") != null)
                    {
                        pm.RemoveStatMod("GreatestMagicBuff");
                        BuffInfo.RemoveBuff(pm, BuffIcon.SpellPlague);
                    }
                }

                if (drunk == 60)
                {
                    pm.AddStatMod(new StatMod(StatType.Int, "HavyDrunk", -15, TimeSpan.FromSeconds(120.0)));
                    BuffInfo.AddBuff(pm,
                        new BuffInfo(BuffIcon.Mindrot, 1060737, 1075841, TimeSpan.FromSeconds(120.0), pm,
                            "Вы пьяны в стельку."));

                    if (Utility.RandomDouble() < 0.05) pm.SendMessage(0x59, "Вы невероятно нажрались.");
                }
                else
                {
                    if (pm.GetStatMod("HavyDrunk") != null)
                    {
                        pm.RemoveStatMod("HavyDrunk");
                        BuffInfo.RemoveBuff(pm, BuffIcon.Mindrot);
                    }
                }
            }
        }
    }
}
