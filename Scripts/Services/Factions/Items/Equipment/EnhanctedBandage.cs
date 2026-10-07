using System;
using System.Collections.Generic;
using Server.Items;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Mysticism;
using Server.Spells.Necromancy;
using Server.Targeting;

namespace Server.Factions
{
    public class EnchantedBandage : Item, IFactionItem
    {
        public static Dictionary<Mobile, Timer> Context = new Dictionary<Mobile, Timer>();
        public override int LabelNumber => 1094712; // Enchanted Bandage

        public EnchantedBandage()
            : base(0xE21)
        {
            Stackable = true;
        }

        public EnchantedBandage(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            FactionEquipment.AddFactionProperties(this, list);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(GetWorldLocation(), 2))
            {
                if (FactionEquipment.CanUse(this, from))
                {
                    from.RevealingAction();

                    from.SendLocalizedMessage(500948); // Who will you use the bandages on?

                    from.BeginTarget(-1, false, TargetFlags.Beneficial, (healer, targeted) =>
                    {
                        var patient = targeted as Mobile;

                        if (patient != null)
                        {
                            if (EnchantedApple.GetTotalCurses(patient) == 0)
                            {
                                healer.SendLocalizedMessage(500955); // That being is not damaged!
                            }
                            else if (!Deleted && healer.CanBeBeneficial(patient, true, true))
                            {
                                healer.DoBeneficial(patient);

                                var onSelf = healer == patient;
                                var dex = healer.Dex;

                                double seconds;
                                var resDelay = patient.Alive ? 0.0 : 5.0;

                                if (onSelf)
                                {
                                    seconds = 9.4 + 0.6 * ((double)(120 - dex) / 10);
                                }
                                else
                                {
                                    seconds = Math.Ceiling((double)4 - healer.Dex / 60);
                                    seconds = Math.Max(seconds, 2);
                                }

                                if (Context.ContainsKey(healer)) Context[healer].Stop();

                                Context[healer] = new InternalTimer(this, patient, healer, seconds);

                                if (!onSelf)
                                    patient.SendLocalizedMessage(1008078, false,
                                        healer.Name); //  : Attempting to heal you.

                                healer.SendLocalizedMessage(500956); // You begin applying the bandages.

                                if (healer.NetState != null && healer.NetState.IsEnhancedClient)
                                    healer.NetState.Send(new BandageTimerPacket((int)seconds));

                                Consume();
                            }
                        }
                    });
                }
            }
            else
            {
                from.SendLocalizedMessage(500295); // You are too far away to do that.
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public class InternalTimer : Timer
        {
            public EnchantedBandage Bandage { get; set; }
            public Mobile Patient { get; set; }
            public Mobile Healer { get; set; }

            public long Expires { get; set; }

            public InternalTimer(EnchantedBandage bandage, Mobile patient, Mobile healer, double seconds)
                : base(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250))
            {
                Bandage = bandage;
                Patient = patient;
                Healer = healer;

                Expires = Core.TickCount + (long)(seconds * 1000);

                Start();
            }

            protected override void OnTick()
            {
                if (Core.TickCount >= Expires)
                {
                    EndHeal();
                    Stop();
                }
            }

            private void EndHeal()
            {
                if (Context.ContainsKey(Healer))
                    Context.Remove(Healer);

                if (Patient != Healer && Patient.InRange(Healer.Location, 2))
                {
                    Healer.PlaySound(0x57);

                    if (EnchantedApple.GetTotalCurses(Patient) == 0)
                        Healer.SendLocalizedMessage(500968); // You apply the bandages, but they barely help.
                    else
                        Healer.SendLocalizedMessage(500969); // You finish applying the bandages.

                    EvilOmenSpell.TryEndEffect(Patient);
                    StrangleSpell.RemoveCurse(Patient);
                    CorpseSkinSpell.RemoveCurse(Patient);
                    WeakenSpell.RemoveEffects(Patient);
                    FeeblemindSpell.RemoveEffects(Patient);
                    ClumsySpell.RemoveEffects(Patient);
                    CurseSpell.RemoveEffect(Patient);
                    MortalStrike.EndWound(Patient);
                    BloodOathSpell.RemoveCurse(Patient);
                    MindRotSpell.ClearMindRotScalar(Patient);
                    SpellPlagueSpell.RemoveFromList(Patient);
                    SleepSpell.EndSleep(Patient);

                    BuffInfo.RemoveBuff(Patient, BuffIcon.MassCurse);
                }
                else
                {
                    Healer.SendLocalizedMessage(500295); // You are too far away to do that.
                }
            }
        }

        #region Factions

        public FactionItem FactionItemState { get; set; }

        #endregion
    }
}
