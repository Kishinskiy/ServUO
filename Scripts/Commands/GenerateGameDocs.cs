using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server.Items;
using Server.Mobiles;

namespace Server.Commands
{
    public static class GenerateGameDocs
    {
        private static CsvFile csv;

        public static void Initialize()
        {
            CommandSystem.Register("GenGameDocs", AccessLevel.GameMaster, GenGameDocs_OnCommand);
        }

        private static void GenGameDocs_OnCommand(CommandEventArgs e)
        {
            csv = new CsvFile();
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.IsClass && t.Namespace == "Server.Mobiles" && typeof(BaseCreature).IsAssignableFrom(t))
                .ToList()
                .ForEach(t => ConsumeType(t, HandleBaseCreature));
            csv.Write("Creatures.csv");

            csv = new CsvFile();
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.IsClass && t.Namespace == "Server.Items" && typeof(BaseWeapon).IsAssignableFrom(t))
                .ToList()
                .ForEach(t => ConsumeType(t, HandleBaseWeapon));
            csv.Write("Weapons.csv");

            csv = new CsvFile();
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.IsClass && t.Namespace == "Server.Items" && typeof(BaseArmor).IsAssignableFrom(t))
                .ToList()
                .ForEach(t => ConsumeType(t, HandleBaseArmor));
            csv.Write("Armor.csv");
        }

        private static void HandleBaseArmor(object obj)
        {
            var arm = obj as BaseArmor;
            if (arm == null)
                return;

            csv.AddRow();
            csv.AddValue("Type", arm.GetType().Name);
            csv.AddValue("Name", arm.Name);
            csv.AddValue("Is Artifact?", arm.IsArtifact);
            csv.AddValue("Pysical Resist", arm.PhysicalResistance);
            csv.AddValue("Fire Resist", arm.FireResistance);
            csv.AddValue("Cold Resist", arm.ColdResistance);
            csv.AddValue("Poison Resist", arm.PoisonResistance);
            csv.AddValue("Energy Resist", arm.EnergyResistance);
            csv.AddValue("DCI", arm.Attributes.DefendChance);
        }

        private static void HandleBaseWeapon(object obj)
        {
            var wep = obj as BaseWeapon;
            if (wep == null)
                return;

            csv.AddRow();
            csv.AddValue("Type", wep.GetType().Name);
            csv.AddValue("Name", wep.Name);
            csv.AddValue("Is Artifact?", wep.IsArtifact);
            csv.AddValue("Str Requirement", wep.StrRequirement);
            csv.AddValue("Dex Requirement", wep.DexRequirement);
            csv.AddValue("Int Requirement", wep.IntRequirement);
            csv.AddValue("Skill", wep.Skill);
            csv.AddValue("Race", wep.RequiredRace);
            csv.AddValue("Speed", wep.MlSpeed);
            csv.AddValue("Min Damage", wep.MinDamage);
            csv.AddValue("Max Damage", wep.MaxDamage);
        }

        private static void HandleBaseCreature(object obj)
        {
            var creature = obj as BaseCreature;
            if (creature == null)
                return;

            var flute = new BambooFlute();

            csv.AddRow();
            csv.AddValue("Type", creature.GetType().Name);
            csv.AddValue("Name", creature.Name);
            csv.AddValue("Str", creature.Str);
            csv.AddValue("Dex", creature.Dex);
            csv.AddValue("Int", creature.Int);
            csv.AddValue("Hits", creature.HitsMax);
            csv.AddValue("Stam", creature.StamMax);
            csv.AddValue("Mana", creature.ManaMax);
            csv.AddValue("Physical Resist", creature.PhysicalResistance);
            csv.AddValue("Fire Resist", creature.FireResistance);
            csv.AddValue("Cold Resist", creature.ColdResistance);
            csv.AddValue("Poison Resist", creature.PoisonResistance);
            csv.AddValue("Energy Resist", creature.EnergyResistance);
            csv.AddValue("Physical Damage", creature.PhysicalDamage);
            csv.AddValue("Fire Damage", creature.FireDamage);
            csv.AddValue("Cold Damage", creature.ColdDamage);
            csv.AddValue("Poison Damage", creature.PoisonDamage);
            csv.AddValue("Energy Damage", creature.EnergyDamage);
            csv.AddValue("Taming Difficulty", creature.CurrentTameSkill);
            csv.AddValue("Barding Difficulty", flute.GetDifficultyFor(creature));
            csv.AddValue("TMap Level", creature.TreasureMapLevel);
            csv.AddValue("Wrestling Skill", creature.Skills.Wrestling.Base);
        }

        private static void ConsumeType(Type t, ProcessObject proc)
        {
            var ctor = t.GetConstructor(new Type[] { });
            if (ctor == null)
                return;

            object obj;
            try
            {
                obj = ctor.Invoke(new object[] { });
            }
            catch (Exception)
            {
                return;
            }

            if (obj == null)
                return;

            proc(obj);
        }

        private delegate void ProcessObject(object obj);

        private class CsvFile
        {
            private readonly List<string> allHeaders = new List<string>();
            private readonly HashSet<string> headerSet = new HashSet<string>();
            private readonly List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
            private Dictionary<string, string> currentRow;

            public void AddRow()
            {
                currentRow = new Dictionary<string, string>();
                rows.Add(currentRow);
            }

            public void AddValue(string name, object value)
            {
                if (name == null)
                    return;

                var v = "";
                if (value != null)
                    v = value.ToString();

                currentRow.Add(name, v);
                if (headerSet.Contains(name))
                    return;
                headerSet.Add(name);
                allHeaders.Add(name);
            }

            public void Write(string path)
            {
                var outf = new StreamWriter(path);
                bool first;

                first = true;
                foreach (var header in allHeaders)
                    if (first)
                    {
                        outf.Write("\"{0}\"", header);
                        first = false;
                    }
                    else
                    {
                        outf.Write(",\"{0}\"", header);
                    }

                outf.WriteLine("");

                foreach (var row in rows)
                {
                    first = true;
                    foreach (var header in allHeaders)
                    {
                        var value = "";
                        row.TryGetValue(header, out value);
                        if (first)
                        {
                            outf.Write("\"{0}\"", value);
                            first = false;
                        }
                        else
                        {
                            outf.Write(",\"{0}\"", value);
                        }
                    }

                    outf.WriteLine("");
                }

                outf.Close();
            }
        }
    }
}
