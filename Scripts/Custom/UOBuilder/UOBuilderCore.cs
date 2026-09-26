using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;

namespace Server.Custom.UOBuilder
{
    internal static class ArtMetrics
    {
        internal const int ArtMaxID = 46307;

        private const int DefaultWidth = 44;

        private static int[] _widths;

        private const string RemoteUrl = "http://utilitydrive.runasp.net/Utility/ArtWidths.CSV";

        public static void Initialize()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ArtWidths.CSV");

            try
            {
                if (!File.Exists(filePath))
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(RemoteUrl, filePath);
                    }
                }

                string[] lines = File.ReadAllLines(filePath);

                _widths = new int[lines.Length];

                for (int i = 0; i < lines.Length; i++)
                {
                    if (int.TryParse(lines[i], out int width))
                    {
                        _widths[i] = width;
                    }
                    else
                    {
                        _widths[i] = DefaultWidth;
                    }
                }

                Console.ForegroundColor = ConsoleColor.Magenta;

                Console.WriteLine("UOBuilder : Utility Loaded!");
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("UOBuilder : Utility Not Loaded!");
            }

            Console.ResetColor();
        }

        public static int GetWidth(int itemId)
        {
            if (itemId <= 0 || _widths == null || itemId > _widths.Length)
            {
                return DefaultWidth;
            }

            return _widths[itemId - 1];
        }
    }

    internal static class UOBuilderCore
    {
        internal static bool UOBuilderActive { get; set; } = true;

        private static readonly string BuilderFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOBuilderData.CSV");

        private static readonly Dictionary<Serial, List<UOBuilderEntity>> UOBuildsList = new Dictionary<Serial, List<UOBuilderEntity>>();

        private static readonly Dictionary<Serial, Point3D> BuildCenters = new Dictionary<Serial, Point3D>();

        private static readonly Dictionary<Serial, int> LastBuildIDs = new Dictionary<Serial, int>();

        internal static bool HasBuilds()
        {
            return UOBuildsList.Values.Any(build => build != null && build.Count > 0);
        }

        internal static bool HasBuilds(Serial builder)
        {
            if (UOBuildsList.ContainsKey(builder))
            {
                return UOBuildsList[builder].Count > 0;
            }

            return false;
        }

        internal static List<Serial> GetBuildList()
        {
            List<Serial> keys = new List<Serial>();

            foreach (var key in UOBuildsList.Keys)
            {
                keys.Add(key);
            }

            return keys;
        }

        internal static List<UOBuilderEntity> GetUniqueBuildList(Serial builder)
        {
            if (!HasBuilds(builder))
            {
                return null;
            }

            List<UOBuilderEntity> statics = new List<UOBuilderEntity>();

            foreach (var resource in UOBuildsList[builder])
            {
                statics.Add(resource);
            }

            return statics;
        }

        internal static string GetBuilder(Serial buildKey)
        {
            if (World.Mobiles.ContainsKey(buildKey))
            {
                return World.Mobiles[buildKey].Name;
            }

            return string.Empty;
        }

        internal static bool CanBuild(UOBuilderPermit permit)
        {
            if (permit == null || permit.BlessedFor == null || !UOBuilderActive)
            {
                return false;
            }

            if (UOBuildsList.ContainsKey(permit.BlessedFor.Serial))
            {
                return true;
            }
            else
            {
                if (UOBuildsList.Count >= UOBuilderConfig.MaxBuilds)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        internal static void AddStatic(UOBuilderPermit permit, UOBuilderStatic buildStatic)
        {
            UOBuilderEntity entity = new UOBuilderEntity()
            {
                E_ID = buildStatic.ItemID,
                E_Map = buildStatic.Map.ToString(),
                E_X = buildStatic.X,
                E_Y = buildStatic.Y,
                E_Z = buildStatic.Z,
                E_HUE = buildStatic.HUE
            };

            if (!UOBuildsList.ContainsKey(permit.BlessedFor.Serial))
            {
                UOBuildsList.Add(permit.BlessedFor.Serial, new List<UOBuilderEntity>() { entity });
            }
            else
            {
                UOBuildsList[permit.BlessedFor.Serial].Add(entity);
            }

            if (UOBuildsList[permit.BlessedFor.Serial].Count == 1)
            {
                permit.Center = buildStatic.Location;
            }

            BuildCenters[permit.BlessedFor.Serial] = permit.Center;
            LastBuildIDs[permit.BlessedFor.Serial] = permit.LastID;
        }

        internal static bool RemoveStatic(UOBuilderPermit permit, UOBuilderStatic buildStatic)
        {
            if (permit == null || permit.BlessedFor == null || buildStatic == null || !UOBuildsList.ContainsKey(permit.BlessedFor.Serial))
            {
                return false;
            }

            int entityToRemove = -1;

            foreach (var entity in UOBuildsList[permit.BlessedFor.Serial])
            {
                if (entity is UOBuilderEntity uobe &&
                    uobe.E_ID == buildStatic.ItemID &&
                    uobe.E_X == buildStatic.X &&
                    uobe.E_Y == buildStatic.Y &&
                    uobe.E_Z == buildStatic.Z &&
                    uobe.E_Map == buildStatic.Map.ToString())
                {
                    entityToRemove = UOBuildsList[permit.BlessedFor.Serial].IndexOf(uobe);
                    break;
                }
            }

            if (entityToRemove < 0)
            {
                return false;
            }

            List<UOBuilderEntity> build = UOBuildsList[permit.BlessedFor.Serial];
            build.RemoveAt(entityToRemove);

            if (build.Count == 0)
            {
                RemoveBuild(permit.BlessedFor.Serial);
                permit.Center = Point3D.Zero;
            }

            return true;
        }

        internal static bool UpdateStatic(UOBuilderPermit permit, UOBuilderStatic buildStatic, Point3D oldLocation, int oldHue)
        {
            if (permit == null || permit.BlessedFor == null || buildStatic == null || buildStatic.Map == null ||
                !UOBuildsList.TryGetValue(permit.BlessedFor.Serial, out List<UOBuilderEntity> build))
            {
                return false;
            }

            string mapName = buildStatic.Map.ToString();

            for (int i = 0; i < build.Count; i++)
            {
                UOBuilderEntity entity = build[i];

                if (entity.E_ID == buildStatic.ItemID &&
                    entity.E_Map == mapName &&
                    entity.E_X == oldLocation.X &&
                    entity.E_Y == oldLocation.Y &&
                    entity.E_Z == oldLocation.Z &&
                    entity.E_HUE == oldHue)
                {
                    entity.E_X = buildStatic.X;
                    entity.E_Y = buildStatic.Y;
                    entity.E_Z = buildStatic.Z;
                    entity.E_HUE = buildStatic.Hue;
                    build[i] = entity;
                    return true;
                }
            }

            return false;
        }

        internal static void PlaceBuild(UOBuilderPermit permit)
        {
            if (permit == null || permit.BlessedFor == null)
            {
                return;
            }

            RestorePermitState(permit);

            if (permit.Center == Point3D.Zero && UOBuildsList.ContainsKey(permit.BlessedFor.Serial))
            {
                foreach (var entity in UOBuildsList[permit.BlessedFor.Serial])
                {
                    UOBuilderStatic uobs = new UOBuilderStatic();

                    uobs.AddPermit(permit, false);

                    uobs.UpdateStats(entity);

                    if (permit.Center == Point3D.Zero)
                    {
                        permit.Center = uobs.Location;
                    }
                }
            }
        }

        internal static Serial LastSerialPlaced = 0;

        internal static bool ToggleUOBuilder()
        {
            UOBuilderActive = !UOBuilderActive;

            return UOBuilderActive;
        }

        internal static bool PreviewBuild(PlayerMobile staff, Serial serial)
        {
            if (staff == null || !UOBuildsList.ContainsKey(serial))
            {
                return false;
            }

            if (LastSerialPlaced != serial)
            {
                CleanBuild(staff);

                PlaceBuild(staff, serial);
            }

            return LastSerialPlaced == serial;
        }

        internal static void ClearPreviewBuild(PlayerMobile staff, Serial serial)
        {
            if (staff != null && LastSerialPlaced == serial)
            {
                CleanBuild(staff);
            }
        }

        internal static bool CommitPreviewBuild(PlayerMobile staff, Serial serial)
        {
            if (staff == null || LastSerialPlaced != serial)
            {
                return false;
            }

            List<Item> previewStatics = World.Items.Values
                .Where(item => item is UOBuilderStatic && item.BlessedFor == staff)
                .ToList();

            if (previewStatics.Count == 0)
            {
                return false;
            }

            for (int i = previewStatics.Count - 1; i >= 0; i--)
            {
                UOBuilderStatic buildStatic = previewStatics[i] as UOBuilderStatic;

                buildStatic.ConvertStatic();
            }

            RemoveBuild(serial);

            CleanBuild(staff);

            return true;
        }

        internal static bool DeletePreviewBuild(PlayerMobile staff, Serial serial)
        {
            if (staff == null || LastSerialPlaced != serial || !UOBuildsList.ContainsKey(serial))
            {
                return false;
            }

            RemoveBuild(serial);

            CleanBuild(staff);

            return true;
        }

        internal static void PlaceBuild(PlayerMobile staff, Serial serial)
        {
            if (UOBuildsList.ContainsKey(serial) && serial != LastSerialPlaced)
            {
                LastSerialPlaced = serial;

                bool isMoved = false;

                foreach (var entity in UOBuildsList[serial])
                {
                    if (!entity.IsPlaced())
                    {
                        UOBuilderStatic uobs = new UOBuilderStatic();

                        uobs.AddStaff(staff);

                        uobs.UpdateStats(entity);
                    }

                    if (!isMoved)
                    {
                        isMoved = true;

                        staff.MoveToWorld(new Point3D(entity.E_X, entity.E_Y, entity.E_Z), Map.Parse(entity.E_Map));
                    }
                }
            }
            else
            {
                staff.SendMessage(53, "Either the build has already been placed or the serial is missing!");
            }
        }

        internal static void ResetUOBuild(Mobile m)
        {
            var permit = m.Backpack.FindItemByType(typeof(UOBuilderPermit));

            if (permit != null && permit is UOBuilderPermit uobp)
            {
                int refundedResources = ResetBuild(uobp);

                uobp.RefundPlacementCharges(refundedResources);

                CleanBuild(m);

                m.SendMessage(53, "Your build was reset and {0} resources were returned.", refundedResources);
            }
            else
            {
                m.SendMessage(53, "Building Permit must be in pack in order to reset build!");
            }
        }

        internal static void CleanBuild(Mobile from)
        {
            var cleanupList = World.Items.Values.Where(i => i is UOBuilderStatic && i.BlessedFor == from)?.ToList();

            if (cleanupList != null && cleanupList.Count > 0)
            {
                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    cleanupList[i].Delete();
                }
            }

            if (from.AccessLevel > AccessLevel.Player)
            {
                LastSerialPlaced = 0;
            }
        }

        internal static void UpdatePermitState(UOBuilderPermit permit)
        {
            if (permit != null && permit.BlessedFor != null)
            {
                BuildCenters[permit.BlessedFor.Serial] = permit.Center;
                LastBuildIDs[permit.BlessedFor.Serial] = permit.LastID;
            }
        }

        internal static bool ConsumePlacementCharge(UOBuilderPermit permit)
        {
            if (permit == null || !UOBuilderActive)
            {
                return false;
            }

            return permit.ConsumePlacementCharge();
        }

        internal static bool TryChargeGold(Mobile from, int amount)
        {
            if (from == null || amount <= 0)
            {
                return false;
            }

            if (AccountGold.Enabled && from.Account != null)
            {
                return from.Account.WithdrawGold(amount);
            }

            Container backpack = from.Backpack;

            if (backpack == null)
            {
                return false;
            }

            int gold = backpack.GetAmount(typeof(Gold), true);
            int checks = backpack.GetChecksWorth(true);

            if (gold + checks < amount)
            {
                return false;
            }

            int goldToConsume = Math.Min(gold, amount);

            if (goldToConsume > 0)
            {
                backpack.ConsumeTotal(typeof(Gold), goldToConsume, true);
            }

            int remaining = amount - goldToConsume;

            if (remaining > 0)
            {
                backpack.TakeFromChecks(remaining, true);
            }

            return true;
        }

        internal static void RemoveBuild(Serial serial)
        {
            if (UOBuildsList.ContainsKey(serial))
            {
                UOBuildsList.Remove(serial);
            }

            BuildCenters.Remove(serial);
            LastBuildIDs.Remove(serial);
        }

        internal static int ResetBuild(UOBuilderPermit permit)
        {
            if (permit == null || permit.BlessedFor == null || !UOBuildsList.TryGetValue(permit.BlessedFor.Serial, out List<UOBuilderEntity> build))
            {
                return 0;
            }

            int count = build.Count;
            RemoveBuild(permit.BlessedFor.Serial);
            permit.Center = Point3D.Zero;
            return count;
        }

        internal static void RestorePermitState(UOBuilderPermit permit)
        {
            if (permit == null || permit.BlessedFor == null)
            {
                return;
            }

            if (BuildCenters.TryGetValue(permit.BlessedFor.Serial, out Point3D center))
            {
                permit.Center = center;
            }

            if (LastBuildIDs.TryGetValue(permit.BlessedFor.Serial, out int lastID))
            {
                permit.LastID = lastID;
            }
        }

        public static void Initialize()
        {
            EventSink.ServerStarted += EventSink_ServerStarted;

            EventSink.Logout += EventSink_Logout;

            EventSink.AfterWorldSave += EventSink_AfterWorldSave;
        }

        private static void EventSink_ServerStarted()
        {
            var cleanupList = World.Items.Values.Where(i => i is UOBuilderStatic)?.ToList();

            if (cleanupList != null && cleanupList.Count > 0)
            {
                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    cleanupList[i].Delete();
                }
            }

            LoadUOBuilder();
        }

        private static void EventSink_Logout(LogoutEventArgs e)
        {
            if (e.Mobile is PlayerMobile)
            {
                CleanBuild(e.Mobile);
            }
        }

        private static void EventSink_AfterWorldSave(AfterWorldSaveEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(UOBuilderActive.ToString());

            if (HasBuilds())
            {
                foreach (var user in UOBuildsList.Keys)
                {
                    if (UOBuildsList[user] == null || UOBuildsList[user].Count == 0)
                    {
                        continue;
                    }

                    sb.AppendLine(user.ToString());

                    Point3D center = BuildCenters.ContainsKey(user) ? BuildCenters[user] : Point3D.Zero;
                    int lastID = LastBuildIDs.ContainsKey(user) ? LastBuildIDs[user] : 2;
                    sb.AppendLine($"C,{center.X},{center.Y},{center.Z},{lastID}");

                    sb.AppendLine(UOBuildsList[user].Count.ToString());

                    foreach (var item in UOBuildsList[user])
                    {
                        if (item is UOBuilderEntity entity)
                        {
                            entity.SaveEntity(sb);
                        }
                    }
                }

                string temporaryFile = BuilderFile + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(BuilderFile));
                File.WriteAllText(temporaryFile, sb.ToString());

                if (File.Exists(BuilderFile))
                {
                    File.Delete(BuilderFile);
                }

                File.Move(temporaryFile, BuilderFile);
            }
            else if (File.Exists(BuilderFile))
            {
                File.Delete(BuilderFile);
            }
        }

        private static void LoadUOBuilder()
        {
            if (File.Exists(BuilderFile))
            {
                using (StreamReader reader = new StreamReader(BuilderFile))
                {
                    string line;

                    Serial currentUserSerial = Serial.Zero;

                    int itemCount = 0;

                    string activeLine = reader.ReadLine();

                    if (bool.TryParse(activeLine, out bool active))
                    {
                        UOBuilderActive = active;
                    }

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("0x"))
                        {
                            currentUserSerial = (int)(Convert.ToInt32(line, 16) & 0xFFFFFFFF);

                            if (!UOBuildsList.ContainsKey(currentUserSerial))
                            {
                                UOBuildsList[currentUserSerial] = new List<UOBuilderEntity>();
                            }
                        }
                        else if (line.StartsWith("C,"))
                        {
                            string[] values = line.Split(',');

                            if (values.Length == 5 &&
                                int.TryParse(values[1], out int centerX) &&
                                int.TryParse(values[2], out int centerY) &&
                                int.TryParse(values[3], out int centerZ) &&
                                int.TryParse(values[4], out int lastID))
                            {
                                BuildCenters[currentUserSerial] = new Point3D(centerX, centerY, centerZ);
                                LastBuildIDs[currentUserSerial] = lastID;
                            }
                        }
                        else if (int.TryParse(line, out itemCount))
                        {
                            if (itemCount > 0 && currentUserSerial != Serial.Zero)
                            {
                                for (int i = 0; i < itemCount; i++)
                                {
                                    UOBuilderEntity entity = new UOBuilderEntity();

                                    try
                                    {
                                        entity.LoadEntity(reader);

                                        if (entity.IsValid())
                                        {
                                            UOBuildsList[currentUserSerial].Add(entity);
                                        }
                                    }
                                    catch
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
