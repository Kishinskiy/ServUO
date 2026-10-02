/*
 * Kam Region Spawner
 * Version 3.1
 *
 * Developed by Kamras
 * https://www.servuo.dev/members/kamras.776/
 * Free to use, please keep this header at the top
 *
 * ServUO edition targeting the current ServUO pub57 distribution.
 * Includes named-region and default overworld-region support.
 *
 * Required dependency:
 * PlayerAttatchmentExtensions by Kamras
 * Namespace: Server.Engines.PlayerAttatchmentExtensions
 * The dependency package must be installed before compiling this script.
 * XMLSpawner2 is not required for spawn tracking. Optional XmlSpawner
 * roster imports are detected through reflection when XmlSpawner is present.
 *
 * Commands: [KamRegionSpawner or [KRS
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Commands;
using Server.Engines.PlayerAttatchmentExtensions;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using Server.Targeting;
using Server.Engines.PartySystem;

namespace Server.Misc
{
	// ============================================================================
	// PLAYER ATTATCHMENT EXTENSION FOR TRACKING SPAWNED CREATURES
	// ============================================================================
	public class KamRegionSpawnAttachment : PlayerAttatchmentExtension
	{
		public bool Continuous;
		public DateTime LastSeenPlayer;
		public DateTime SpawnedAt;
		public string RegionName;
		public string RegionKey;
		public int DespawnMins;

		// Runtime behavior state for region roaming and leash recovery.
		public Point3D SpawnPoint;
		public Point3D RoamDestination;
		public DateTime NextRoamAt;
		public DateTime OutsideRegionSince;

		[PlayerAttatchable]
		public KamRegionSpawnAttachment(string name)
		{
			Name = name;
			LastSeenPlayer = DateTime.Now;
			SpawnedAt = DateTime.Now;
			RegionName = "Unknown";
			RegionKey = "";
			DespawnMins = 5;
			SpawnPoint = Point3D.Zero;
			RoamDestination = Point3D.Zero;
			NextRoamAt = DateTime.MinValue;
			OutsideRegionSince = DateTime.MinValue;
		}

		// Kept for compatibility with any older scripts that may still use this overload.
		[PlayerAttatchable]
		public KamRegionSpawnAttachment(string name, bool continuous, string regionName, int despawnMins)
			: this(name, continuous, regionName, "", despawnMins)
		{
		}

		[PlayerAttatchable]
		public KamRegionSpawnAttachment(string name, bool continuous, string regionName, string regionKey, int despawnMins)
		{
			Name = name;
			Continuous = continuous;
			LastSeenPlayer = DateTime.Now;
			SpawnedAt = DateTime.Now;
			RegionName = string.IsNullOrEmpty(regionName) ? "Unknown" : regionName;
			RegionKey = regionKey ?? "";
			DespawnMins = Math.Max(1, despawnMins);
			SpawnPoint = Point3D.Zero;
			RoamDestination = Point3D.Zero;
			NextRoamAt = DateTime.MinValue;
			OutsideRegionSince = DateTime.MinValue;
		}

		public KamRegionSpawnAttachment(PlayerAttatchmentSerial serial) : base(serial) { }

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);
			writer.Write((int)3); // Version 3
			writer.Write(Continuous);
			writer.Write(LastSeenPlayer);
			writer.Write(RegionName);
			writer.Write(DespawnMins);

			// V2
			writer.Write(RegionKey);
			writer.Write(SpawnedAt);

			// V3 - movement/roaming state
			writer.Write(SpawnPoint);
			writer.Write(RoamDestination);
			writer.Write(NextRoamAt);
			writer.Write(OutsideRegionSince);
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);
			int version = reader.ReadInt();
			Continuous = reader.ReadBool();
			LastSeenPlayer = reader.ReadDateTime();

			if (version >= 1)
			{
				RegionName = reader.ReadString();
				DespawnMins = reader.ReadInt();
			}
			else
			{
				RegionName = "Unknown";
				DespawnMins = 5;
			}

			if (version >= 2)
			{
				RegionKey = reader.ReadString();
				SpawnedAt = reader.ReadDateTime();
			}
			else
			{
				RegionKey = "";
				SpawnedAt = LastSeenPlayer;
			}

			if (version >= 3)
			{
				SpawnPoint = reader.ReadPoint3D();
				RoamDestination = reader.ReadPoint3D();
				NextRoamAt = reader.ReadDateTime();
				OutsideRegionSince = reader.ReadDateTime();
			}
			else
			{
				SpawnPoint = Point3D.Zero;
				RoamDestination = Point3D.Zero;
				NextRoamAt = DateTime.MinValue;
				OutsideRegionSince = DateTime.MinValue;
			}

			if (RegionName == null)
				RegionName = "Unknown";

			if (RegionKey == null)
				RegionKey = "";

			DespawnMins = Math.Max(1, DespawnMins);
		}

		public override string OnIdentify(Mobile from)
		{
			string keyText = string.IsNullOrEmpty(RegionKey) ? "Legacy/Unknown" : RegionKey;
			return $"Spawned By Region: {RegionName}\nRegion Key: {keyText}\nContinuous: {Continuous}\nDespawn Timer: {DespawnMins} Mins\nSpawned: {SpawnedAt}\nSpawn Point: {SpawnPoint}\nRoam Destination: {RoamDestination}";
		}
	}

	// ============================================================================
	// CREATURE ENTRY (NAME & WEIGHT)
	// ============================================================================
	public class KamRegionCreatureEntry
	{
		public string Name;
		public int Weight; // 0 = Off

		public KamRegionCreatureEntry() { Name = ""; Weight = 1; }
		public KamRegionCreatureEntry(string name, int weight) { Name = name; Weight = weight; }
	}

	// ============================================================================
	// DATA PROFILE FOR EACH REGION
	// ============================================================================
	public class KamRegionSpawnerProfile
	{
		public bool Enabled;
		public int SpawnChance;
		public int RandomizerPercent;
		public int HourlyMax;
		public int MaxSpawnsPerRegion;
		public int CooldownMins;
		public int PartyMultiplier;
		public int PlayerClusterRange;

		// XmlSpawner-inspired placement and creature behavior settings.
		public int SpawnRangeMin;
		public int SpawnRangeMax;
		public int SpawnSearchAttempts;
		public int HomeRange; // 0 = unrestricted roaming inside the source region
		public bool RegionRoaming;
		public bool SeekPlayers;
		public bool LeashToRegion;
		public int AggroRange;
		public int RoamIntervalSeconds;
		public bool SmartAI;
		public int WakeRange;
		public int DespawnPlayerRange;
		public int CreatureTeam; // -1 preserves each creature's native team

		public int MinPackSize;
		public int MaxPackSize;
		public bool ContinuousSpawns;
		public int DespawnMins;
		public List<KamRegionCreatureEntry> Creatures;

		// SpawnHistory remains region-wide so the hourly cap still protects the
		// whole region. Nearby-cluster cooldowns are tracked separately at runtime so
		// isolated players can each trigger their own encounters.
		public DateTime NextAllowedSpawn;
		public List<DateTime> SpawnHistory;
		public Dictionary<int, DateTime> PlayerNextAllowedSpawns;

		public KamRegionSpawnerProfile()
		{
			Enabled = true;
			SpawnChance = 100;
			RandomizerPercent = 10;
			HourlyMax = 15;
			MaxSpawnsPerRegion = 0;
			CooldownMins = 5;
			PartyMultiplier = 2;
			PlayerClusterRange = 60;

			SpawnRangeMin = 5;
			SpawnRangeMax = 45;
			SpawnSearchAttempts = 30;
			HomeRange = 0;
			RegionRoaming = false;
			SeekPlayers = false;
			LeashToRegion = false;
			AggroRange = 18;
			RoamIntervalSeconds = 20;
			SmartAI = true;
			WakeRange = 80;
			DespawnPlayerRange = 60;
			CreatureTeam = -1;

			MinPackSize = 1;
			MaxPackSize = 1;
			ContinuousSpawns = false;
			DespawnMins = 5;

			Creatures = new List<KamRegionCreatureEntry>();
            Creatures.Add(new KamRegionCreatureEntry("Orc", 1));
            Creatures.Add(new KamRegionCreatureEntry("OrcMage", 1));
            Creatures.Add(new KamRegionCreatureEntry("OrcCaptain", 1));
            Creatures.Add(new KamRegionCreatureEntry("Skeleton", 1));
            Creatures.Add(new KamRegionCreatureEntry("Zombie", 1));
            Creatures.Add(new KamRegionCreatureEntry("Lich", 1));
            Creatures.Add(new KamRegionCreatureEntry("Ogre", 1));
            Creatures.Add(new KamRegionCreatureEntry("Ettin", 1));
            Creatures.Add(new KamRegionCreatureEntry("HeadlessOne", 1));
            Creatures.Add(new KamRegionCreatureEntry("Ratman", 1));

            // Заполняем оставшиеся ячейки пустыми строками, чтобы в сумме было ровно 40 (требование интерфейса)
            while (Creatures.Count < 40)
            {
                Creatures.Add(new KamRegionCreatureEntry());
            }

			NextAllowedSpawn = DateTime.MinValue;
			SpawnHistory = new List<DateTime>();
			PlayerNextAllowedSpawns = new Dictionary<int, DateTime>();
		}

		public void Normalize()
		{
			SpawnChance = Math.Max(1, Math.Min(100, SpawnChance));
			RandomizerPercent = Math.Max(0, Math.Min(100, RandomizerPercent));
			HourlyMax = Math.Max(1, Math.Min(500, HourlyMax));
			MaxSpawnsPerRegion = Math.Max(0, Math.Min(500, MaxSpawnsPerRegion));
			CooldownMins = Math.Max(1, Math.Min(240, CooldownMins));
			PartyMultiplier = Math.Max(1, Math.Min(10, PartyMultiplier));
			PlayerClusterRange = Math.Max(10, Math.Min(200, PlayerClusterRange));

			SpawnRangeMin = Math.Max(0, Math.Min(200, SpawnRangeMin));
			SpawnRangeMax = Math.Max(1, Math.Min(200, SpawnRangeMax));
			if (SpawnRangeMin > SpawnRangeMax)
				SpawnRangeMin = SpawnRangeMax;
			SpawnSearchAttempts = Math.Max(5, Math.Min(100, SpawnSearchAttempts));
			HomeRange = Math.Max(0, Math.Min(500, HomeRange));
			AggroRange = Math.Max(5, Math.Min(200, AggroRange));
			RoamIntervalSeconds = Math.Max(5, Math.Min(120, RoamIntervalSeconds));
			WakeRange = Math.Max(AggroRange, Math.Max(20, Math.Min(300, WakeRange)));
			DespawnPlayerRange = Math.Max(10, Math.Min(300, DespawnPlayerRange));
			CreatureTeam = Math.Max(-1, Math.Min(1000, CreatureTeam));

			MinPackSize = Math.Max(1, Math.Min(50, MinPackSize));
			MaxPackSize = Math.Max(MinPackSize, Math.Min(50, MaxPackSize));
			DespawnMins = Math.Max(1, Math.Min(240, DespawnMins));

			if (Creatures == null)
				Creatures = new List<KamRegionCreatureEntry>();

			while (Creatures.Count < 40)
				Creatures.Add(new KamRegionCreatureEntry());

			if (Creatures.Count > 40)
				Creatures.RemoveRange(40, Creatures.Count - 40);

			for (int i = 0; i < Creatures.Count; i++)
			{
				if (Creatures[i] == null)
					Creatures[i] = new KamRegionCreatureEntry();

				if (Creatures[i].Name == null)
					Creatures[i].Name = "";

				Creatures[i].Weight = Math.Max(0, Creatures[i].Weight);
			}

			if (SpawnHistory == null)
				SpawnHistory = new List<DateTime>();

			if (PlayerNextAllowedSpawns == null)
				PlayerNextAllowedSpawns = new Dictionary<int, DateTime>();

			SpawnHistory.RemoveAll(t => t < DateTime.Now.AddHours(-1));

			if (NextAllowedSpawn < DateTime.MinValue.AddDays(1))
				NextAllowedSpawn = DateTime.MinValue;
		}

		public void Serialize(GenericWriter writer)
		{
			Normalize();
			writer.Write((int)7); // Version 7

			// V7 - XmlSpawner-inspired placement and behavior controls
			writer.Write(SpawnRangeMin);
			writer.Write(SpawnRangeMax);
			writer.Write(SpawnSearchAttempts);
			writer.Write(HomeRange);
			writer.Write(RegionRoaming);
			writer.Write(SeekPlayers);
			writer.Write(LeashToRegion);
			writer.Write(AggroRange);
			writer.Write(RoamIntervalSeconds);
			writer.Write(SmartAI);
			writer.Write(WakeRange);
			writer.Write(DespawnPlayerRange);
			writer.Write(CreatureTeam);

			// V6 - persistent per-player cooldowns used by local encounter clusters
			List<KeyValuePair<int, DateTime>> activeCooldowns = new List<KeyValuePair<int, DateTime>>();
			foreach (KeyValuePair<int, DateTime> kvp in PlayerNextAllowedSpawns)
			{
				if (kvp.Value > DateTime.Now)
					activeCooldowns.Add(kvp);
			}

			writer.Write(activeCooldowns.Count);
			foreach (KeyValuePair<int, DateTime> kvp in activeCooldowns)
			{
				writer.Write(kvp.Key);
				writer.Write(kvp.Value);
			}

			// V5 - configurable distance used to group nearby players
			writer.Write(PlayerClusterRange);

			// V4 - persistent runtime throttling
			writer.Write(NextAllowedSpawn);
			List<DateTime> recentHistory = SpawnHistory.FindAll(t => t >= DateTime.Now.AddHours(-1));
			writer.Write(recentHistory.Count);
			foreach (DateTime time in recentHistory)
				writer.Write(time);

			// V3
			writer.Write(RandomizerPercent);
			writer.Write(MaxSpawnsPerRegion);

			// V2
			writer.Write(DespawnMins);
			writer.Write(Creatures.Count);
			foreach (KamRegionCreatureEntry c in Creatures)
			{
				writer.Write(c.Name);
				writer.Write(c.Weight);
			}

			// V1
			writer.Write(MinPackSize);
			writer.Write(MaxPackSize);

			// V0
			writer.Write(Enabled);
			writer.Write(SpawnChance);
			writer.Write(HourlyMax);
			writer.Write(CooldownMins);
			writer.Write(PartyMultiplier);
			writer.Write(ContinuousSpawns);
		}

		public void Deserialize(GenericReader reader)
		{
			int version = reader.ReadInt();

			if (version >= 7)
			{
				SpawnRangeMin = reader.ReadInt();
				SpawnRangeMax = reader.ReadInt();
				SpawnSearchAttempts = reader.ReadInt();
				HomeRange = reader.ReadInt();
				RegionRoaming = reader.ReadBool();
				SeekPlayers = reader.ReadBool();
				LeashToRegion = reader.ReadBool();
				AggroRange = reader.ReadInt();
				RoamIntervalSeconds = reader.ReadInt();
				SmartAI = reader.ReadBool();
				WakeRange = reader.ReadInt();
				DespawnPlayerRange = reader.ReadInt();
				CreatureTeam = reader.ReadInt();
			}
			else
			{
				SpawnRangeMin = 5;
				SpawnRangeMax = 45;
				SpawnSearchAttempts = 30;
				HomeRange = 0;
				RegionRoaming = false;
				SeekPlayers = false;
				LeashToRegion = false;
				AggroRange = 18;
				RoamIntervalSeconds = 20;
				SmartAI = true;
				WakeRange = 80;
				DespawnPlayerRange = 60;
				CreatureTeam = -1;
			}

			if (version >= 6)
			{
				int cooldownCount = reader.ReadInt();
				PlayerNextAllowedSpawns = new Dictionary<int, DateTime>();
				for (int i = 0; i < cooldownCount; i++)
				{
					int serialValue = reader.ReadInt();
					DateTime nextAllowed = reader.ReadDateTime();
					if (nextAllowed > DateTime.Now)
						PlayerNextAllowedSpawns[serialValue] = nextAllowed;
				}
			}
			else
			{
				PlayerNextAllowedSpawns = new Dictionary<int, DateTime>();
			}

			if (version >= 5)
				PlayerClusterRange = reader.ReadInt();
			else
				PlayerClusterRange = 60;

			if (version >= 4)
			{
				NextAllowedSpawn = reader.ReadDateTime();
				int historyCount = reader.ReadInt();
				SpawnHistory = new List<DateTime>();
				for (int i = 0; i < historyCount; i++)
					SpawnHistory.Add(reader.ReadDateTime());
			}
			else
			{
				NextAllowedSpawn = DateTime.MinValue;
				SpawnHistory = new List<DateTime>();
			}

			if (version >= 3)
			{
				RandomizerPercent = reader.ReadInt();
				MaxSpawnsPerRegion = reader.ReadInt();
			}
			else
			{
				RandomizerPercent = 0;
				MaxSpawnsPerRegion = 0;
			}

			if (version >= 2)
			{
				DespawnMins = reader.ReadInt();
				int count = reader.ReadInt();
				Creatures = new List<KamRegionCreatureEntry>();
				for (int i = 0; i < count; i++)
					Creatures.Add(new KamRegionCreatureEntry(reader.ReadString(), reader.ReadInt()));
			}

			if (version >= 1)
			{
				MinPackSize = reader.ReadInt();
				MaxPackSize = reader.ReadInt();
			}
			else
			{
				MinPackSize = 1;
				MaxPackSize = 1;
			}

			Enabled = reader.ReadBool();
			SpawnChance = reader.ReadInt();
			HourlyMax = reader.ReadInt();
			CooldownMins = reader.ReadInt();
			PartyMultiplier = reader.ReadInt();
			ContinuousSpawns = reader.ReadBool();

			// Backwards compatibility for Version 0 and 1 profiles.
			if (version < 2)
			{
				DespawnMins = 5;
				int count = reader.ReadInt();
				Creatures = new List<KamRegionCreatureEntry>();
				for (int i = 0; i < count; i++)
					Creatures.Add(new KamRegionCreatureEntry(reader.ReadString(), 1));
			}

			Normalize();
		}
	}

	// ============================================================================
	// INVISIBLE CONTROLLER ITEM (HANDLES ALL SERIALIZATION)
	// ============================================================================
	public class KamRegionSpawnerController : Item
	{
		public Dictionary<string, KamRegionSpawnerProfile> Profiles = new Dictionary<string, KamRegionSpawnerProfile>();

		public KamRegionSpawnerController() : base(0x1F14)
		{
			Name = "Kam Region Spawner Core";
			Movable = false;
			Visible = false;
		}

		public KamRegionSpawnerController(Serial serial) : base(serial) { }

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);
			writer.Write((int)0);

			writer.Write(Profiles.Count);
			foreach (KeyValuePair<string, KamRegionSpawnerProfile> kvp in Profiles)
			{
				writer.Write(kvp.Key);
				kvp.Value.Serialize(writer);
			}
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);
			int version = reader.ReadInt();

			int count = reader.ReadInt();
			for (int i = 0; i < count; i++)
			{
				string key = reader.ReadString();
				KamRegionSpawnerProfile prof = new KamRegionSpawnerProfile();
				prof.Deserialize(reader);
				Profiles[key] = prof;
			}

			KamRegionSpawnerSystem.Controller = this;
		}
	}

	// ============================================================================
	// MAIN ENGINE
	// ============================================================================
	public static class KamRegionSpawnerSystem
	{
		private static KamRegionSpawnerController m_Controller;

		public static KamRegionSpawnerController Controller
		{
			get { if (m_Controller == null || m_Controller.Deleted) EnsureInitialized(); return m_Controller; }
			set { m_Controller = value; }
		}

		public static EngineTimer SpawnerTimer;
		public static BehaviorTimer CreatureBehaviorTimer;
		public static CleanupTimer DespawnTimer;

		public static void Initialize()
		{
			CommandSystem.Register("KamRegionSpawner", AccessLevel.Administrator, new CommandEventHandler(OnAdminCommand));
			CommandSystem.Register("KRS", AccessLevel.Administrator, new CommandEventHandler(OnAdminCommand));
			EventSink.WorldLoad += new WorldLoadEventHandler(OnWorldLoad);
		}

		private static void OnWorldLoad()
		{
			EnsureInitialized();
		}

		public static void EnsureInitialized()
		{
			if (m_Controller == null || m_Controller.Deleted)
			{
				foreach (Item item in World.Items.Values)
				{
					if (item is KamRegionSpawnerController)
					{
						m_Controller = (KamRegionSpawnerController)item;
						break;
					}
				}

				if (m_Controller == null)
				{
					m_Controller = new KamRegionSpawnerController();
					m_Controller.MoveToWorld(Point3D.Zero, Map.Internal);
				}
			}

			if (SpawnerTimer == null)
			{
				SpawnerTimer = new EngineTimer();
				SpawnerTimer.Start();
			}

			if (CreatureBehaviorTimer == null)
			{
				CreatureBehaviorTimer = new BehaviorTimer();
				CreatureBehaviorTimer.Start();
			}

			if (DespawnTimer == null)
			{
				DespawnTimer = new CleanupTimer();
				DespawnTimer.Start();
			}
		}

		private static void OnAdminCommand(CommandEventArgs e)
		{
			EnsureInitialized();
			e.Mobile.CloseGump(typeof(KamRegionSpawnerGump));
			e.Mobile.SendGump(new KamRegionSpawnerGump(0, "", "All"));
		}

		public static KamRegionSpawnerProfile GetProfile(string regionKey)
		{
			if (!Controller.Profiles.ContainsKey(regionKey))
				Controller.Profiles[regionKey] = new KamRegionSpawnerProfile();

			KamRegionSpawnerProfile profile = Controller.Profiles[regionKey];
			profile.Normalize();
			return profile;
		}

		public static List<Map> GetWorldMaps()
		{
			List<Map> maps = new List<Map>();
			object source = null;

			// ServUO has used both AllMaps and Maps across distributions. Reflection
			// keeps this release tolerant of either public static collection.
			PropertyInfo property = typeof(Map).GetProperty(
				"AllMaps",
				BindingFlags.Public | BindingFlags.Static);

			if (property == null)
			{
				property = typeof(Map).GetProperty(
					"Maps",
					BindingFlags.Public | BindingFlags.Static);
			}

			if (property != null)
			{
				try
				{
					source = property.GetValue(null, null);
				}
				catch
				{
					source = null;
				}
			}

			IEnumerable enumerable = source as IEnumerable;
			if (enumerable != null)
			{
				foreach (object value in enumerable)
				{
					Map map = value as Map;
					if (map != null && map != Map.Internal && !maps.Contains(map))
						maps.Add(map);
				}
			}

			return maps;
		}

		public static Map GetRegionMap(Region region, Map fallbackMap)
		{
			if (region != null && region.Map != null)
				return region.Map;

			if (fallbackMap != null && fallbackMap != Map.Internal)
				return fallbackMap;

			if (region != null)
			{
				foreach (Map map in GetWorldMaps())
				{
					if (map.DefaultRegion == region)
						return map;
				}
			}

			return null;
		}

		public static string GetRegionDisplayName(Region region, Map fallbackMap)
		{
			if (region == null)
				return "Unknown Region";

			if (!String.IsNullOrEmpty(region.Name))
				return region.Name;

			string text = region.ToString();
			if (!String.IsNullOrEmpty(text) &&
				!String.Equals(text, region.GetType().Name, StringComparison.OrdinalIgnoreCase) &&
				!String.Equals(text, "Region", StringComparison.OrdinalIgnoreCase))
			{
				return text;
			}

			Map map = GetRegionMap(region, fallbackMap);
			return map != null ? map.Name + " Overworld" : region.GetType().Name;
		}

		public static string GetRegionDisplayName(Region region)
		{
			return GetRegionDisplayName(region, GetRegionMap(region, null));
		}

		public static string GetRegionKey(Region region)
		{
			return GetRegionKey(region, GetRegionMap(region, null));
		}

		public static string GetRegionKey(Region region, Map fallbackMap)
		{
			if (region == null)
				return "Unknown|Unknown";

			Map map = GetRegionMap(region, fallbackMap);
			if (map == null || map == Map.Internal)
				return "Unknown|Unknown";

			string regionToken = map.DefaultRegion == region
				? "@DefaultRegion"
				: GetRegionDisplayName(region, map);

			if (String.IsNullOrEmpty(regionToken))
				return "Unknown|Unknown";

			return String.Format("{0}|{1}", map.Name, regionToken);
		}

		public static List<Region> GetUsableRegions()
		{
			List<Region> regions = new List<Region>();

			foreach (Region region in Region.Regions)
			{
				if (region == null || region.Map == null || region.Map == Map.Internal)
					continue;

				if (!regions.Contains(region))
					regions.Add(region);
			}

			foreach (Map map in GetWorldMaps())
			{
				if (map == null || map == Map.Internal || map.DefaultRegion == null)
					continue;

				if (!regions.Contains(map.DefaultRegion))
					regions.Add(map.DefaultRegion);
			}

			return regions;
		}

		public static Region GetRegionAt(Mobile mobile)
		{
			if (mobile == null || mobile.Map == null || mobile.Map == Map.Internal)
				return null;

			Region region = Region.Find(mobile.Location, mobile.Map);
			if (region == null)
				region = mobile.Region;
			if (region == null)
				region = mobile.Map.DefaultRegion;

			return region;
		}

		public static Region FindRegionByKey(string regionKey)
		{
			if (String.IsNullOrEmpty(regionKey))
				return null;

			int separator = regionKey.IndexOf('|');
			if (separator <= 0 || separator >= regionKey.Length - 1)
				return null;

			string mapName = regionKey.Substring(0, separator);
			string regionToken = regionKey.Substring(separator + 1);

			foreach (Map map in GetWorldMaps())
			{
				if (!String.Equals(map.Name, mapName, StringComparison.OrdinalIgnoreCase))
					continue;

				if (String.Equals(regionToken, "@DefaultRegion", StringComparison.OrdinalIgnoreCase))
					return map.DefaultRegion;
			}

			foreach (Region region in GetUsableRegions())
			{
				Map map = GetRegionMap(region, null);
				if (map == null || !String.Equals(map.Name, mapName, StringComparison.OrdinalIgnoreCase))
					continue;

				if (String.Equals(GetRegionDisplayName(region, map), regionToken, StringComparison.OrdinalIgnoreCase))
					return region;
			}

			return null;
		}

		private static bool RegionMatchesTeleportTarget(Region found, Region target, bool allowChildRegion)
		{
			if (found == null || target == null)
				return false;

			if (found == target)
				return true;

			return allowChildRegion && found.IsChildOf(target);
		}

		private static bool TryTeleportPoint(
			Region targetRegion,
			int x,
			int y,
			int preferredZ,
			bool allowChildRegion,
			out Point3D location)
		{
			location = Point3D.Zero;

			if (targetRegion == null || targetRegion.Map == null || targetRegion.Map == Map.Internal)
				return false;

			Map map = targetRegion.Map;
			if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
				return false;

			int averageZ = map.GetAverageZ(x, y);
			int[] zValues = preferredZ == averageZ
				? new int[] { averageZ }
				: new int[] { preferredZ, averageZ };

			for (int i = 0; i < zValues.Length; i++)
			{
				Point3D point = new Point3D(x, y, zValues[i]);

				// Default overworld regions may not expose a conventional Area array,
				// so Region.Find is the authoritative ownership check for them.
				if (map.DefaultRegion != targetRegion && !targetRegion.Contains(point))
					continue;

				Region found = Region.Find(point, map);
				if (!RegionMatchesTeleportTarget(found, targetRegion, allowChildRegion))
					continue;

				if (!map.CanSpawnMobile(point.X, point.Y, point.Z))
					continue;

				location = point;
				return true;
			}

			return false;
		}

		private static bool TryRectangleTeleportLocation(
			Region targetRegion,
			Rectangle3D rectangle,
			bool allowChildRegion,
			out Point3D location)
		{
			location = Point3D.Zero;

			Map map = targetRegion.Map;
			int minX = Math.Max(0, Math.Min(rectangle.Start.X, rectangle.End.X));
			int minY = Math.Max(0, Math.Min(rectangle.Start.Y, rectangle.End.Y));
			int maxX = Math.Min(map.Width - 1, Math.Max(rectangle.Start.X, rectangle.End.X) - 1);
			int maxY = Math.Min(map.Height - 1, Math.Max(rectangle.Start.Y, rectangle.End.Y) - 1);

			if (maxX < minX || maxY < minY)
				return false;

			int centerX = minX + ((maxX - minX) / 2);
			int centerY = minY + ((maxY - minY) / 2);
			int preferredZ = map.GetAverageZ(centerX, centerY);

			int minZ = Math.Min(rectangle.Start.Z, rectangle.End.Z);
			int maxZ = Math.Max(rectangle.Start.Z, rectangle.End.Z) - 1;
			if (preferredZ < minZ || preferredZ > maxZ)
				preferredZ = minZ + ((maxZ - minZ) / 2);

			if (TryTeleportPoint(targetRegion, centerX, centerY, preferredZ, allowChildRegion, out location))
				return true;

			// Search outward from the geometric center first. This normally finds a nearby
			// walkable tile while keeping the destination representative of the region.
			int maxRadius = Math.Min(48, Math.Max(maxX - minX, maxY - minY));
			for (int radius = 1; radius <= maxRadius; radius++)
			{
				for (int offset = -radius; offset <= radius; offset++)
				{
					int leftX = centerX - radius;
					int rightX = centerX + radius;
					int topY = centerY - radius;
					int bottomY = centerY + radius;

					if (leftX >= minX && leftX <= maxX && centerY + offset >= minY && centerY + offset <= maxY &&
						TryTeleportPoint(targetRegion, leftX, centerY + offset, preferredZ, allowChildRegion, out location))
						return true;

					if (rightX >= minX && rightX <= maxX && centerY + offset >= minY && centerY + offset <= maxY &&
						TryTeleportPoint(targetRegion, rightX, centerY + offset, preferredZ, allowChildRegion, out location))
						return true;

					if (centerX + offset >= minX && centerX + offset <= maxX && topY >= minY && topY <= maxY &&
						TryTeleportPoint(targetRegion, centerX + offset, topY, preferredZ, allowChildRegion, out location))
						return true;

					if (centerX + offset >= minX && centerX + offset <= maxX && bottomY >= minY && bottomY <= maxY &&
						TryTeleportPoint(targetRegion, centerX + offset, bottomY, preferredZ, allowChildRegion, out location))
						return true;
				}
			}

			// Large or irregular rectangles may have no usable tile near their center.
			// Sample the full rectangle as a bounded fallback instead of scanning millions
			// of map cells and freezing the shard.
			for (int attempt = 0; attempt < 300; attempt++)
			{
				int x = Utility.RandomMinMax(minX, maxX);
				int y = Utility.RandomMinMax(minY, maxY);

				if (TryTeleportPoint(targetRegion, x, y, targetRegion.Map.GetAverageZ(x, y), allowChildRegion, out location))
					return true;
			}

			return false;
		}

		public static bool TryGetRegionTeleportLocation(string regionKey, out Region region, out Point3D location)
		{
			region = FindRegionByKey(regionKey);
			location = Point3D.Zero;

			if (region == null || region.Map == null || region.Map == Map.Internal)
				return false;

			// A configured GoLocation is the region author's preferred destination.
			Point3D goLocation = region.GoLocation;
			if (TryTeleportPoint(region, goLocation.X, goLocation.Y, goLocation.Z, false, out location))
				return true;

			Rectangle3D[] area = region.Area;
			if (area == null || area.Length == 0)
			{
				// Some default overworld regions do not expose an Area array. Sample the
				// map directly while requiring Region.Find to resolve back to the default.
				if (region.Map.DefaultRegion == region)
				{
					for (int attempt = 0; attempt < 500; attempt++)
					{
						int x = Utility.Random(Math.Max(1, region.Map.Width));
						int y = Utility.Random(Math.Max(1, region.Map.Height));
						int z = region.Map.GetAverageZ(x, y);

						if (TryTeleportPoint(region, x, y, z, false, out location))
							return true;
					}
				}

				return false;
			}

			// Prefer a point owned directly by the selected region.
			for (int i = 0; i < area.Length; i++)
			{
				if (TryRectangleTeleportLocation(region, area[i], false, out location))
					return true;
			}

			// Parent regions can be almost completely covered by child regions. A child
			// still lies within the selected parent, so allow it only as a final fallback.
			if (TryTeleportPoint(region, goLocation.X, goLocation.Y, goLocation.Z, true, out location))
				return true;

			for (int i = 0; i < area.Length; i++)
			{
				if (TryRectangleTeleportLocation(region, area[i], true, out location))
					return true;
			}

			return false;
		}

		public static string GetRegionCategory(Region region)
		{
			if (region == null)
				return "Other";

			if (region.IsDefault || region.GetType() == typeof(Region) || region.GetType().Name == "BaseRegion")
				return "Wilderness";

			if (region is HouseRegion)
				return "House";

			if (region is DungeonRegion)
				return "Dungeon";

			if (region is TownRegion || region is GuardedRegion)
				return "Town";

			return MakeFriendlyRegionTypeName(region.GetType().Name);
		}

		private static string MakeFriendlyRegionTypeName(string typeName)
		{
			if (String.IsNullOrEmpty(typeName))
				return "Other";

			if (typeName.EndsWith("Region", StringComparison.OrdinalIgnoreCase) && typeName.Length > 6)
				typeName = typeName.Substring(0, typeName.Length - 6);

			List<char> result = new List<char>();
			for (int i = 0; i < typeName.Length; i++)
			{
				char current = typeName[i];
				if (i > 0 && Char.IsUpper(current) &&
					(Char.IsLower(typeName[i - 1]) ||
					(i + 1 < typeName.Length && Char.IsLower(typeName[i + 1]))))
				{
					result.Add(' ');
				}

				result.Add(current);
			}

			string friendly = new String(result.ToArray()).Trim();
			return friendly.Length == 0 ? "Other" : friendly;
		}

		public static List<string> GetRegionCategories()
		{
			HashSet<string> found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			found.Add("Wilderness");
			found.Add("Dungeon");
			found.Add("Town");
			found.Add("House");

			foreach (Region region in GetUsableRegions())
			{
				if (region == null || GetRegionMap(region, null) == null)
					continue;

				found.Add(GetRegionCategory(region));
			}

			List<string> extras = new List<string>();
			foreach (string category in found)
			{
				if (!String.Equals(category, "Wilderness", StringComparison.OrdinalIgnoreCase) &&
					!String.Equals(category, "Dungeon", StringComparison.OrdinalIgnoreCase) &&
					!String.Equals(category, "Town", StringComparison.OrdinalIgnoreCase) &&
					!String.Equals(category, "House", StringComparison.OrdinalIgnoreCase))
				{
					extras.Add(category);
				}
			}

			extras.Sort(StringComparer.OrdinalIgnoreCase);

			List<string> categories = new List<string>();
			categories.Add("All");
			categories.Add("Wilderness");
			categories.Add("Dungeon");
			categories.Add("Town");
			categories.Add("House");
			categories.AddRange(extras);
			return categories;
		}

		public static bool IsPlayerOwnedCreature(Mobile mobile)
		{
			BaseCreature creature = mobile as BaseCreature;
			return creature != null && (creature.Controlled || creature.IsStabled);
		}

		public static bool AttachmentMatchesRegion(KamRegionSpawnAttachment attachment, Mobile mobile, string regionKey)
		{
			if (attachment == null || mobile == null || mobile.Deleted || string.IsNullOrEmpty(regionKey))
				return false;

			if (!string.IsNullOrEmpty(attachment.RegionKey))
				return string.Equals(attachment.RegionKey, regionKey, StringComparison.OrdinalIgnoreCase);

			// Legacy attachment fallback: compare both the map and saved region name.
			int separator = regionKey.IndexOf('|');
			if (separator <= 0 || separator >= regionKey.Length - 1 || mobile.Map == null)
				return false;

			string mapName = regionKey.Substring(0, separator);
			string regionName = regionKey.Substring(separator + 1);

			return string.Equals(mobile.Map.Name, mapName, StringComparison.OrdinalIgnoreCase) &&
				string.Equals(attachment.RegionName, regionName, StringComparison.OrdinalIgnoreCase);
		}

		public static int CountActiveSpawns(string regionKey)
		{
			int count = 0;

			foreach (PlayerAttatchmentExtension attachment in PlayerAttatchmentExt.AllAttachments.Values)
			{
				KamRegionSpawnAttachment regionAttachment = attachment as KamRegionSpawnAttachment;
				if (regionAttachment == null)
					continue;

				Mobile mobile = regionAttachment.AttachedTo as Mobile;
				if (mobile == null || mobile.Deleted || !mobile.Alive || IsPlayerOwnedCreature(mobile))
					continue;

				if (AttachmentMatchesRegion(regionAttachment, mobile, regionKey))
					count++;
			}

			return count;
		}

		public static int GetRecentSpawnCount(KamRegionSpawnerProfile profile)
		{
			if (profile == null)
				return 0;

			if (profile.SpawnHistory == null)
				profile.SpawnHistory = new List<DateTime>();

			profile.SpawnHistory.RemoveAll(t => t < DateTime.Now.AddHours(-1));
			return profile.SpawnHistory.Count;
		}

		public static List<PlayerMobile> GetEncounterClusterMembers(
			PlayerMobile center,
			string regionKey,
			int clusterRange)
		{
			List<PlayerMobile> candidates = new List<PlayerMobile>();
			List<PlayerMobile> members = new List<PlayerMobile>();

			if (center == null || center.Map == null || String.IsNullOrEmpty(regionKey))
				return members;

			clusterRange = Math.Max(10, Math.Min(200, clusterRange));

			foreach (NetState state in NetState.Instances)
			{
				PlayerMobile player = state.Mobile as PlayerMobile;
				if (player == null || !player.Alive || player.Map != center.Map)
					continue;

				Region playerRegion = KamRegionSpawnerSystem.GetRegionAt(player);
				if (playerRegion == null ||
					!String.Equals(KamRegionSpawnerSystem.GetRegionKey(playerRegion, player.Map), regionKey, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				candidates.Add(player);
			}

			Queue<PlayerMobile> pending = new Queue<PlayerMobile>();
			HashSet<int> included = new HashSet<int>();

			pending.Enqueue(center);
			included.Add(center.Serial.Value);

			while (pending.Count > 0)
			{
				PlayerMobile current = pending.Dequeue();
				members.Add(current);

				foreach (PlayerMobile candidate in candidates)
				{
					if (included.Contains(candidate.Serial.Value))
						continue;

					if (Utility.InRange(current.Location, candidate.Location, clusterRange))
					{
						included.Add(candidate.Serial.Value);
						pending.Enqueue(candidate);
					}
				}
			}

			members.Sort((a, b) => a.Serial.Value.CompareTo(b.Serial.Value));
			return members;
		}

		public static string GetEncounterClusterKey(List<PlayerMobile> members)
		{
			if (members == null || members.Count == 0)
				return String.Empty;

			List<string> serials = new List<string>();
			foreach (PlayerMobile member in members)
			{
				if (member != null)
					serials.Add(member.Serial.Value.ToString());
			}

			return String.Join(":", serials.ToArray());
		}

		public static DateTime GetClusterNextAllowedSpawn(
			KamRegionSpawnerProfile profile,
			List<PlayerMobile> members)
		{
			if (profile == null || members == null || members.Count == 0)
				return DateTime.MinValue;

			if (profile.PlayerNextAllowedSpawns == null)
				profile.PlayerNextAllowedSpawns = new Dictionary<int, DateTime>();

			DateTime latest = DateTime.MinValue;
			foreach (PlayerMobile member in members)
			{
				if (member == null)
					continue;

				DateTime nextAllowed;
				if (!profile.PlayerNextAllowedSpawns.TryGetValue(member.Serial.Value, out nextAllowed))
					continue;

				if (nextAllowed <= DateTime.Now)
				{
					profile.PlayerNextAllowedSpawns.Remove(member.Serial.Value);
					continue;
				}

				if (nextAllowed > latest)
					latest = nextAllowed;
			}

			return latest;
		}

		public static void SetClusterNextAllowedSpawn(
			KamRegionSpawnerProfile profile,
			List<PlayerMobile> members,
			DateTime when)
		{
			if (profile == null || members == null || members.Count == 0)
				return;

			if (profile.PlayerNextAllowedSpawns == null)
				profile.PlayerNextAllowedSpawns = new Dictionary<int, DateTime>();

			foreach (PlayerMobile member in members)
			{
				if (member != null)
					profile.PlayerNextAllowedSpawns[member.Serial.Value] = when;
			}

			if (profile.PlayerNextAllowedSpawns.Count > 256)
			{
				List<int> expired = new List<int>();
				foreach (KeyValuePair<int, DateTime> kvp in profile.PlayerNextAllowedSpawns)
				{
					if (kvp.Value <= DateTime.Now)
						expired.Add(kvp.Key);
				}

				foreach (int key in expired)
					profile.PlayerNextAllowedSpawns.Remove(key);
			}
		}

		public static int ApplyVariation(int baseValue, int randomizerPercent, int minimum, int maximum)
		{
			baseValue = Math.Max(minimum, Math.Min(maximum, baseValue));
			randomizerPercent = Math.Max(0, Math.Min(100, randomizerPercent));

			if (randomizerPercent == 0 || baseValue == 0)
				return baseValue;

			int variation = Math.Max(1, (int)Math.Round(baseValue * (randomizerPercent / 100.0)));
			int variedValue = baseValue + Utility.RandomMinMax(-variation, variation);
			return Math.Max(minimum, Math.Min(maximum, variedValue));
		}


		public static bool MobileIsInRegionKey(Mobile mobile, string regionKey)
		{
			if (mobile == null || mobile.Deleted || mobile.Map == null || String.IsNullOrEmpty(regionKey))
				return false;

			Region region = GetRegionAt(mobile);
			return region != null &&
				String.Equals(GetRegionKey(region, mobile.Map), regionKey, StringComparison.OrdinalIgnoreCase);
		}

		public static bool TryFindSpawnPointNearPlayer(
			PlayerMobile player,
			Region sourceRegion,
			string regionKey,
			KamRegionSpawnerProfile profile,
			out Point3D location)
		{
			location = Point3D.Zero;
			if (player == null || sourceRegion == null || player.Map == null ||
				player.Map == Map.Internal || profile == null)
			{
				return false;
			}

			profile.Normalize();
			int minRange = profile.SpawnRangeMin;
			int maxRange = profile.SpawnRangeMax;

			for (int attempt = 0; attempt < profile.SpawnSearchAttempts; attempt++)
			{
				int distance = Utility.RandomMinMax(minRange, maxRange);
				int angle = Utility.Random(360);
				int x = player.X + (int)(Math.Cos(angle * Math.PI / 180.0) * distance);
				int y = player.Y + (int)(Math.Sin(angle * Math.PI / 180.0) * distance);

				if (x < 0 || y < 0 || x >= player.Map.Width || y >= player.Map.Height)
					continue;

				int z = player.Map.GetAverageZ(x, y);
				if (!player.Map.CanSpawnMobile(x, y, z))
					z = player.Z;

				if (!player.Map.CanSpawnMobile(x, y, z))
					continue;

				Point3D point = new Point3D(x, y, z);
				Region found = Region.Find(point, player.Map);
				if (found == null ||
					!String.Equals(GetRegionKey(found, player.Map), regionKey, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				location = point;
				return true;
			}

			return false;
		}

		private static bool HasNormalPlayerNear(Mobile mobile, int range, string regionKey)
		{
			if (mobile == null || mobile.Map == null || mobile.Map == Map.Internal)
				return false;

			IPooledEnumerable eable = mobile.Map.GetClientsInRange(mobile.Location, range);
			try
			{
				foreach (NetState state in eable)
				{
					PlayerMobile player = state.Mobile as PlayerMobile;
					if (player == null || !player.Alive || player.Hidden ||
						player.AccessLevel != AccessLevel.Player)
					{
						continue;
					}

					if (String.IsNullOrEmpty(regionKey) || MobileIsInRegionKey(player, regionKey))
						return true;
				}
			}
			finally
			{
				eable.Free();
			}

			return false;
		}

		private static PlayerMobile FindNearestPlayerTarget(
			BaseCreature creature,
			KamRegionSpawnerProfile profile,
			string regionKey)
		{
			if (creature == null || creature.Map == null ||
				creature.Map == Map.Internal || profile == null)
			{
				return null;
			}

			PlayerMobile nearest = null;
			double nearestDistance = Double.MaxValue;
			IPooledEnumerable eable = creature.Map.GetClientsInRange(
				creature.Location,
				profile.AggroRange);

			try
			{
				foreach (NetState state in eable)
				{
					PlayerMobile player = state.Mobile as PlayerMobile;
					if (player == null || !player.Alive || player.Hidden ||
						player.AccessLevel != AccessLevel.Player)
					{
						continue;
					}

					if (profile.LeashToRegion && !MobileIsInRegionKey(player, regionKey))
						continue;

					if (!creature.CanSee(player) || !creature.InLOS(player))
						continue;

					double distance = creature.GetDistanceToSqrt(player);
					if (distance < nearestDistance)
					{
						nearest = player;
						nearestDistance = distance;
					}
				}
			}
			finally
			{
				eable.Free();
			}

			return nearest;
		}

		private static bool TryFindRoamPoint(
			BaseCreature creature,
			KamRegionSpawnAttachment attachment,
			KamRegionSpawnerProfile profile,
			Region sourceRegion,
			bool returningHome,
			out Point3D point)
		{
			point = Point3D.Zero;
			if (creature == null || attachment == null || profile == null ||
				sourceRegion == null || creature.Map == null)
			{
				return false;
			}

			Point3D anchor;
			int radius;

			if (returningHome || profile.HomeRange > 0)
			{
				anchor = attachment.SpawnPoint;
				radius = profile.HomeRange > 0
					? profile.HomeRange
					: Math.Max(20, profile.SpawnRangeMax);
			}
			else
			{
				// Region-wide roaming progresses through local waypoints. This lets a
				// creature gradually cross a large region without requesting one enormous path.
				anchor = creature.Location;
				radius = Math.Max(20, Math.Min(60, profile.SpawnRangeMax));
			}

			if (anchor == Point3D.Zero)
				anchor = creature.Location;

			for (int attempt = 0; attempt < 40; attempt++)
			{
				int x = anchor.X + Utility.RandomMinMax(-radius, radius);
				int y = anchor.Y + Utility.RandomMinMax(-radius, radius);

				if (x < 0 || y < 0 || x >= creature.Map.Width || y >= creature.Map.Height)
					continue;

				if (Math.Abs(x - anchor.X) < 3 && Math.Abs(y - anchor.Y) < 3)
					continue;

				int z = creature.Map.GetAverageZ(x, y);
				if (!creature.Map.CanSpawnMobile(x, y, z))
					continue;

				Point3D candidate = new Point3D(x, y, z);
				Region found = Region.Find(candidate, creature.Map);
				if (found == null ||
					!String.Equals(
						GetRegionKey(found, creature.Map),
						attachment.RegionKey,
						StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				point = candidate;
				return true;
			}

			return false;
		}

		private static bool TryStepToward(BaseCreature creature, Point3D destination)
		{
			if (creature == null || creature.Deleted || creature.Map == null)
				return false;

			Direction desired = creature.GetDirectionTo(destination);
			int[] offsets = new int[] { 0, -1, 1, -2, 2, 4 };

			for (int i = 0; i < offsets.Length; i++)
			{
				Direction direction = (Direction)(((int)desired + offsets[i]) & 0x7);
				creature.Direction = direction;

				if (creature.Move(direction))
					return true;
			}

			return false;
		}

		private static void SetOptionalBoolProperty(object target, string propertyName, bool value)
		{
			if (target == null || String.IsNullOrEmpty(propertyName))
				return;

			try
			{
				BindingFlags flags =
					BindingFlags.Instance |
					BindingFlags.Public |
					BindingFlags.NonPublic;

				PropertyInfo property = target.GetType().GetProperty(propertyName, flags);
				if (property == null || !property.CanWrite || property.PropertyType != typeof(bool))
					return;

				if (property.CanRead)
				{
					object current = property.GetValue(target, null);
					if (current is bool && (bool)current == value)
						return;
				}

				property.SetValue(target, value, null);
			}
			catch
			{
				// SeeksHome exists on some ServUO/RunUO BaseCreature versions but not
				// every customized core. Home and RangeHome still provide bounded roaming.
			}
		}

		public static void ApplyBehaviorSettings(
			BaseCreature creature,
			KamRegionSpawnAttachment attachment,
			KamRegionSpawnerProfile profile)
		{
			if (creature == null || attachment == null || profile == null)
				return;

			if (attachment.SpawnPoint == Point3D.Zero)
				attachment.SpawnPoint = creature.Location;

			if (profile.CreatureTeam >= 0 && creature.Team != profile.CreatureTeam)
				creature.Team = profile.CreatureTeam;

			if (profile.SeekPlayers && creature.RangePerception < profile.AggroRange)
				creature.RangePerception = profile.AggroRange;

			if (profile.HomeRange > 0)
			{
				if (creature.Home != attachment.SpawnPoint)
					creature.Home = attachment.SpawnPoint;

				if (creature.RangeHome != profile.HomeRange)
					creature.RangeHome = profile.HomeRange;

				SetOptionalBoolProperty(creature, "SeeksHome", true);
			}
			else if (profile.RegionRoaming)
			{
				SetOptionalBoolProperty(creature, "SeeksHome", false);
			}
		}

		// ========================================================================
		// CREATURE BEHAVIOR TIMER
		// Adds optional region roaming, smart wake/sleep, region leashing, and
		// seek-and-destroy player acquisition without replacing native creature AI.
		// ========================================================================
		public class BehaviorTimer : Timer
		{
			public BehaviorTimer()
				: base(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
			{
			}

			protected override void OnTick()
			{
				foreach (PlayerAttatchmentExtension extension in PlayerAttatchmentExt.AllAttachments.Values)
				{
					KamRegionSpawnAttachment attachment = extension as KamRegionSpawnAttachment;
					if (attachment == null || String.IsNullOrEmpty(attachment.RegionKey))
						continue;

					BaseCreature creature = attachment.AttachedTo as BaseCreature;
					if (creature == null || creature.Deleted || !creature.Alive ||
						IsPlayerOwnedCreature(creature))
					{
						continue;
					}

					KamRegionSpawnerProfile profile;
					if (!Controller.Profiles.TryGetValue(attachment.RegionKey, out profile) ||
						profile == null)
					{
						continue;
					}

					profile.Normalize();
					ApplyBehaviorSettings(creature, attachment, profile);

					Region sourceRegion = FindRegionByKey(attachment.RegionKey);
					if (sourceRegion == null || sourceRegion.Map != creature.Map)
						continue;

					bool insideSourceRegion = MobileIsInRegionKey(
						creature,
						attachment.RegionKey);

					if (profile.LeashToRegion && !insideSourceRegion)
					{
						if (attachment.OutsideRegionSince == DateTime.MinValue)
							attachment.OutsideRegionSince = DateTime.Now;

						creature.Combatant = null;
						creature.Warmode = false;

						if (creature.AIObject != null)
							creature.AIObject.Action = ActionType.Wander;

						Point3D returnPoint;
						if (TryFindRoamPoint(
							creature,
							attachment,
							profile,
							sourceRegion,
							true,
							out returnPoint))
						{
							attachment.RoamDestination = returnPoint;

							if (DateTime.Now >= attachment.OutsideRegionSince.AddSeconds(30))
							{
								// Try normal walking first. Relocate only when the creature
								// remains trapped outside its source region for 30 seconds.
								creature.MoveToWorld(returnPoint, sourceRegion.Map);
								attachment.OutsideRegionSince = DateTime.MinValue;
							}
							else
							{
								TryStepToward(creature, returnPoint);
							}
						}

						continue;
					}

					attachment.OutsideRegionSince = DateTime.MinValue;

					// ServUO exposes Combatant as IDamageable rather than Mobile.
					// Preserve valid non-mobile combat targets, while applying the
					// region checks below only to Mobile combatants.
					IDamageable damageableCombatant = creature.Combatant;
					Mobile combatant = damageableCombatant as Mobile;

					if (damageableCombatant != null && combatant == null)
					{
						attachment.LastSeenPlayer = DateTime.Now;
						continue;
					}

					if (combatant != null)
					{
						bool validCombatant =
							!combatant.Deleted &&
							combatant.Alive &&
							combatant.Map == creature.Map;

						if (validCombatant && profile.LeashToRegion)
							validCombatant = MobileIsInRegionKey(
								combatant,
								attachment.RegionKey);

						if (validCombatant)
						{
							attachment.LastSeenPlayer = DateTime.Now;
							continue;
						}

						creature.Combatant = null;
					}

					if (profile.SmartAI &&
						!HasNormalPlayerNear(
							creature,
							profile.WakeRange,
							attachment.RegionKey))
					{
						continue;
					}

					if (profile.SeekPlayers)
					{
						PlayerMobile target = FindNearestPlayerTarget(
							creature,
							profile,
							attachment.RegionKey);

						if (target != null)
						{
							creature.Combatant = target;
							creature.Warmode = true;

							if (creature.AIObject != null)
								creature.AIObject.Action = ActionType.Combat;

							attachment.LastSeenPlayer = DateTime.Now;
							continue;
						}
					}

					if (!profile.RegionRoaming)
						continue;

					bool needDestination =
						attachment.RoamDestination == Point3D.Zero ||
						creature.InRange(attachment.RoamDestination, 2) ||
						DateTime.Now >= attachment.NextRoamAt;

					if (needDestination)
					{
						Point3D destination;
						if (TryFindRoamPoint(
							creature,
							attachment,
							profile,
							sourceRegion,
							false,
							out destination))
						{
							attachment.RoamDestination = destination;
						}

						attachment.NextRoamAt =
							DateTime.Now.AddSeconds(profile.RoamIntervalSeconds);
					}

					if (attachment.RoamDestination != Point3D.Zero)
					{
						if (creature.AIObject != null &&
							creature.AIObject.Action != ActionType.Wander)
						{
							creature.AIObject.Action = ActionType.Wander;
						}

						TryStepToward(creature, attachment.RoamDestination);
					}
				}
			}
		}

		// ========================================================================
		// SPAWNING ENGINE TIMER
		// ========================================================================
		public class EngineTimer : Timer
		{
			public EngineTimer() : base(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15)) { }

			protected override void OnTick()
			{
				HashSet<string> processedClusters = new HashSet<string>();

				foreach (NetState ns in NetState.Instances)
				{
					PlayerMobile pm = ns.Mobile as PlayerMobile;

					// The system is player-centric. Each isolated player can generate
					// encounters independently. Players inside the configured cluster range
					// are processed as one local group.
					if (pm == null || !pm.Alive || pm.Map == Map.Internal)
						continue;

					Region reg = KamRegionSpawnerSystem.GetRegionAt(pm);
					if (reg == null)
						continue;

					string regKey = KamRegionSpawnerSystem.GetRegionKey(reg, pm.Map);
					if (String.Equals(regKey, "Unknown|Unknown", StringComparison.OrdinalIgnoreCase))
						continue;
					KamRegionSpawnerProfile prof = KamRegionSpawnerSystem.GetProfile(regKey);

					if (!prof.Enabled)
						continue;

					List<PlayerMobile> clusterMembers = KamRegionSpawnerSystem.GetEncounterClusterMembers(
						pm,
						regKey,
						prof.PlayerClusterRange);

					string clusterKey = KamRegionSpawnerSystem.GetEncounterClusterKey(clusterMembers);
					if (String.IsNullOrEmpty(clusterKey) || !processedClusters.Add(clusterKey))
						continue;

					DateTime clusterNextAllowed = KamRegionSpawnerSystem.GetClusterNextAllowedSpawn(prof, clusterMembers);
					if (DateTime.Now < clusterNextAllowed)
						continue;

					int recentSpawnCount = KamRegionSpawnerSystem.GetRecentSpawnCount(prof);
					if (recentSpawnCount >= prof.HourlyMax)
						continue;

					int activeCount = KamRegionSpawnerSystem.CountActiveSpawns(regKey);
					if (prof.MaxSpawnsPerRegion > 0 && activeCount >= prof.MaxSpawnsPerRegion)
						continue;

					int effectiveChance = KamRegionSpawnerSystem.ApplyVariation(
						prof.SpawnChance,
						prof.RandomizerPercent,
						1,
						100);

					if (Utility.Random(100) >= effectiveChance)
						continue;

					PlayerMobile encounterPlayer = clusterMembers[Utility.Random(clusterMembers.Count)];
					Region sourceRegion = KamRegionSpawnerSystem.FindRegionByKey(regKey);
					if (sourceRegion == null)
						sourceRegion = KamRegionSpawnerSystem.GetRegionAt(encounterPlayer);

					SpawnEncounter(
						encounterPlayer,
						sourceRegion,
						regKey,
						prof,
						clusterMembers,
						activeCount,
						recentSpawnCount,
						effectiveChance);
				}
			}

			private void SpawnEncounter(
				PlayerMobile pm,
				Region sourceRegion,
				string regionKey,
				KamRegionSpawnerProfile prof,
				List<PlayerMobile> clusterMembers,
				int activeCount,
				int recentSpawnCount,
				int effectiveChance)
			{
				List<KamRegionCreatureEntry> validCreatures = new List<KamRegionCreatureEntry>();
				int totalWeight = 0;

				foreach (KamRegionCreatureEntry creature in prof.Creatures)
				{
					if (!string.IsNullOrEmpty(creature.Name) && creature.Name != "INVALID TYPE" && creature.Weight > 0)
					{
						validCreatures.Add(creature);
						totalWeight += creature.Weight;
					}
				}

				if (validCreatures.Count == 0 || totalWeight <= 0)
					return;

				Point3D spawnPoint;
				if (!KamRegionSpawnerSystem.TryFindSpawnPointNearPlayer(
					pm,
					sourceRegion,
					regionKey,
					prof,
					out spawnPoint))
				{
					return;
				}

				int baseCount = Utility.RandomMinMax(prof.MinPackSize, prof.MaxPackSize);

				Party party = Party.Get(pm);
				if (party != null)
				{
					int membersNearby = 0;
					foreach (PartyMemberInfo memberInfo in party.Members)
					{
						Mobile member = memberInfo.Mobile;
						if (member != null && member.Alive && member.Map == pm.Map && member.InRange(pm.Location, 30))
							membersNearby++;
					}

					if (membersNearby > 1)
						baseCount *= prof.PartyMultiplier;
				}

				// The randomizer varies encounter behavior without ever weakening the two
				// safety caps: hourly creatures and active creatures in the region.
				baseCount = KamRegionSpawnerSystem.ApplyVariation(baseCount, prof.RandomizerPercent, 1, 100);

				int hourlyRemaining = Math.Max(0, prof.HourlyMax - recentSpawnCount);
				baseCount = Math.Min(baseCount, hourlyRemaining);

				if (prof.MaxSpawnsPerRegion > 0)
				{
					int regionRemaining = Math.Max(0, prof.MaxSpawnsPerRegion - activeCount);
					baseCount = Math.Min(baseCount, regionRemaining);
				}

				if (baseCount <= 0)
					return;

				int effectiveCooldown = KamRegionSpawnerSystem.ApplyVariation(
					prof.CooldownMins,
					prof.RandomizerPercent,
					1,
					240);

				int effectiveDespawn = KamRegionSpawnerSystem.ApplyVariation(
					prof.DespawnMins,
					prof.RandomizerPercent,
					1,
					240);

				int actualSpawned = 0;

				for (int i = 0; i < baseCount; i++)
				{
					int roll = Utility.Random(totalWeight);
					string typeName = "";

					foreach (KamRegionCreatureEntry creature in validCreatures)
					{
						roll -= creature.Weight;
						if (roll < 0)
						{
							typeName = creature.Name;
							break;
						}
					}

					Type type = ScriptCompiler.FindTypeByName(typeName, true);
					if (type == null || type.IsAbstract || !type.IsSubclassOf(typeof(Mobile)))
						continue;

					Mobile mob = null;
					try
					{
						mob = (Mobile)Activator.CreateInstance(type);

						Point3D creaturePoint = spawnPoint;
						for (int placementAttempt = 0; placementAttempt < 8; placementAttempt++)
						{
							int ox = spawnPoint.X + Utility.RandomMinMax(-2, 2);
							int oy = spawnPoint.Y + Utility.RandomMinMax(-2, 2);

							if (ox < 0 || oy < 0 || ox >= pm.Map.Width || oy >= pm.Map.Height)
								continue;

							int oz = pm.Map.GetAverageZ(ox, oy);
							if (!pm.Map.CanSpawnMobile(ox, oy, oz))
								continue;

							Point3D candidate = new Point3D(ox, oy, oz);
							Region candidateRegion = Region.Find(candidate, pm.Map);
							if (candidateRegion == null ||
								!String.Equals(
									KamRegionSpawnerSystem.GetRegionKey(candidateRegion, pm.Map),
									regionKey,
									StringComparison.OrdinalIgnoreCase))
							{
								continue;
							}

							creaturePoint = candidate;
							break;
						}

						mob.MoveToWorld(creaturePoint, pm.Map);

						KamRegionSpawnAttachment spawnAttachment = new KamRegionSpawnAttachment(
							"KamRegionSpawn",
							prof.ContinuousSpawns,
							KamRegionSpawnerSystem.GetRegionDisplayName(sourceRegion, pm.Map),
							regionKey,
							effectiveDespawn);

						spawnAttachment.SpawnPoint = mob.Location;
						spawnAttachment.NextRoamAt =
							DateTime.Now.AddSeconds(prof.RoamIntervalSeconds);

						BaseCreature baseCreature = mob as BaseCreature;
						if (baseCreature != null)
						{
							KamRegionSpawnerSystem.ApplyBehaviorSettings(
								baseCreature,
								spawnAttachment,
								prof);
						}

						if (!PlayerAttatchmentExt.AttachTo(mob, spawnAttachment))
						{
							spawnAttachment.Delete();
							mob.Delete();
							continue;
						}

						actualSpawned++;
					}
					catch
					{
						// Never leave an untracked creature behind if construction, placement,
						// or attachment creation failed.
						if (mob != null && !mob.Deleted)
							mob.Delete();
					}
				}

				if (actualSpawned <= 0)
					return;

				DateTime nextAllowed = DateTime.Now.AddMinutes(effectiveCooldown);
				KamRegionSpawnerSystem.SetClusterNextAllowedSpawn(prof, clusterMembers, nextAllowed);
				prof.NextAllowedSpawn = nextAllowed;
				for (int i = 0; i < actualSpawned; i++)
					prof.SpawnHistory.Add(DateTime.Now);

			}
		}

		// ========================================================================
		// CLEANUP TIMER (despawns abandoned encounters)
		// ========================================================================
		public class CleanupTimer : Timer
		{
			public CleanupTimer() : base(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2)) { }

			protected override void OnTick()
			{
				List<Mobile> toDelete = new List<Mobile>();

				foreach (PlayerAttatchmentExtension attachment in PlayerAttatchmentExt.AllAttachments.Values)
				{
					KamRegionSpawnAttachment regionAttachment = attachment as KamRegionSpawnAttachment;
					if (regionAttachment == null || regionAttachment.Continuous)
						continue;

					Mobile mobile = regionAttachment.AttachedTo as Mobile;
					if (mobile == null || mobile.Deleted)
						continue;

					// Tamed or stabled creatures are now player property. They are never
					// despawned, never counted against a region cap, and never wiped.
					if (KamRegionSpawnerSystem.IsPlayerOwnedCreature(mobile))
						continue;

					if (mobile.Combatant != null)
					{
						regionAttachment.LastSeenPlayer = DateTime.Now;
						continue;
					}

					if (mobile.Map == null || mobile.Map == Map.Internal)
					{
						toDelete.Add(mobile);
						continue;
					}

					bool playerNear = false;
					int despawnRange = 60;
					KamRegionSpawnerProfile cleanupProfile;

					if (!String.IsNullOrEmpty(regionAttachment.RegionKey) &&
						Controller.Profiles.TryGetValue(
							regionAttachment.RegionKey,
							out cleanupProfile) &&
						cleanupProfile != null)
					{
						cleanupProfile.Normalize();
						despawnRange = cleanupProfile.DespawnPlayerRange;
					}

					IPooledEnumerable eable =
						mobile.Map.GetClientsInRange(mobile.Location, despawnRange);
					foreach (NetState state in eable)
					{
						if (state.Mobile != null && state.Mobile.AccessLevel == AccessLevel.Player)
						{
							playerNear = true;
							break;
						}
					}
					eable.Free();

					if (playerNear)
					{
						regionAttachment.LastSeenPlayer = DateTime.Now;
					}
					else if (DateTime.Now > regionAttachment.LastSeenPlayer.AddMinutes(Math.Max(1, regionAttachment.DespawnMins)))
					{
						toDelete.Add(mobile);
					}
				}

				foreach (Mobile mobile in toDelete)
				{
					if (mobile != null && !mobile.Deleted && !KamRegionSpawnerSystem.IsPlayerOwnedCreature(mobile))
						mobile.Delete();
				}
			}
		}

		// ========================================================================
		// IMPORT TARGETER
		// ========================================================================
		public class ImportSpawnerTarget : Target
		{
			private string m_Key;
			private int m_Page;
			private string m_Search;
			private string m_Filter;
			private int m_CreaturePage;

			public ImportSpawnerTarget(string key, int page, string search, string filter, int creaturePage)
				: base(15, false, TargetFlags.None)
			{
				m_Key = key;
				m_Page = page;
				m_Search = search;
				m_Filter = filter;
				m_CreaturePage = creaturePage;
			}

			protected override void OnTarget(Mobile from, object targeted)
			{
				int added = 0;
				KamRegionSpawnerProfile profile = KamRegionSpawnerSystem.GetProfile(m_Key);
				string sourceName = null;

				Spawner standardSpawner = targeted as Spawner;
				if (standardSpawner != null)
				{
					sourceName = "ServUO Spawner";

					if (standardSpawner.SpawnObjects != null)
					{
						foreach (SpawnObject spawnObject in standardSpawner.SpawnObjects)
						{
							if (spawnObject != null && TryAddCreature(profile, spawnObject.SpawnName))
								added++;
						}
					}
				}
				else if (IsXmlSpawner(targeted))
				{
					sourceName = targeted.GetType().Name;
					added = ImportXmlSpawnerRoster(targeted, profile);
				}
				else
				{
					from.SendMessage("That is not a recognized ServUO Spawner or XmlSpawner.");
					from.SendGump(new KamRegionSpawnerEditGump(m_Key, m_Page, m_Search, m_Filter, m_CreaturePage));
					return;
				}

				string settingsSummary = ApplyImportedSettings(targeted, profile);
				profile.Normalize();

				from.SendMessage($"Imported {added} new creature type(s) from the {sourceName}.");
				from.SendMessage(settingsSummary);
				from.SendGump(new KamRegionSpawnerEditGump(m_Key, m_Page, m_Search, m_Filter, m_CreaturePage));
			}

			private static bool IsXmlSpawner(object target)
			{
				if (target == null)
					return false;

				Type type = target.GetType();
				while (type != null)
				{
					if (String.Equals(type.Name, "XmlSpawner", StringComparison.OrdinalIgnoreCase))
						return true;

					type = type.BaseType;
				}

				return false;
			}

			private static int ImportXmlSpawnerRoster(object xmlSpawner, KamRegionSpawnerProfile profile)
			{
				object rawRoster;
				if (!TryReadValue(xmlSpawner, new string[] { "SpawnObjects", "SpawnNames" }, out rawRoster) || rawRoster == null)
					return 0;

				IEnumerable enumerable = rawRoster as IEnumerable;
				if (enumerable == null)
					return 0;

				int added = 0;
				foreach (object entry in enumerable)
				{
					string typeName = entry as string;

					if (String.IsNullOrEmpty(typeName) && entry != null)
					{
						object rawName;
						if (TryReadValue(entry, new string[] { "TypeName", "SpawnName", "Name" }, out rawName) && rawName != null)
							typeName = Convert.ToString(rawName);
					}

					if (TryAddCreature(profile, typeName))
						added++;
				}

				return added;
			}

			private static bool TryAddCreature(KamRegionSpawnerProfile profile, string name)
			{
				if (profile == null || String.IsNullOrWhiteSpace(name))
					return false;

				string cleanName = name.Trim();
				Type type = ScriptCompiler.FindTypeByName(cleanName, true);
				if (type == null || type.IsAbstract || !type.IsSubclassOf(typeof(Mobile)))
					return false;

				cleanName = type.Name;

				foreach (KamRegionCreatureEntry existing in profile.Creatures)
				{
					if (!String.IsNullOrEmpty(existing.Name) &&
						String.Equals(existing.Name, cleanName, StringComparison.OrdinalIgnoreCase))
					{
						return false;
					}
				}

				for (int i = 0; i < profile.Creatures.Count; i++)
				{
					if (String.IsNullOrEmpty(profile.Creatures[i].Name) || profile.Creatures[i].Name == "INVALID TYPE")
					{
						profile.Creatures[i].Name = cleanName;
						profile.Creatures[i].Weight = 1;
						return true;
					}
				}

				return false;
			}

			private static string ApplyImportedSettings(object spawner, KamRegionSpawnerProfile profile)
			{
				List<string> imported = new List<string>();

				bool running;
				if (TryReadBool(spawner, new string[] { "Running", "IsRunning", "Active" }, out running))
				{
					profile.Enabled = running;
					imported.Add("enabled state");
				}

				int count;
				if (TryReadInt(spawner, new string[] { "MaxCount", "Count", "MaxSpawnCount" }, out count) && count > 0)
				{
					count = Math.Max(1, Math.Min(500, count));
					profile.MaxSpawnsPerRegion = count;
					profile.MinPackSize = 1;
					profile.MaxPackSize = Math.Max(1, Math.Min(15, count));
					imported.Add("count/cap");
				}

				TimeSpan minDelay;
				TimeSpan maxDelay;
				bool hasMin = TryReadTimeSpan(spawner, new string[] { "MinDelay", "MinimumDelay" }, out minDelay);
				bool hasMax = TryReadTimeSpan(spawner, new string[] { "MaxDelay", "MaximumDelay" }, out maxDelay);

				if (hasMin || hasMax)
				{
					if (!hasMin)
						minDelay = maxDelay;
					if (!hasMax)
						maxDelay = minDelay;

					double averageMinutes =
						(minDelay.TotalMinutes + maxDelay.TotalMinutes) / 2.0;

					profile.CooldownMins = Math.Max(
						1,
						Math.Min(240, (int)Math.Round(averageMinutes)));

					imported.Add("average delay");
				}

				int spawnRange;
				if (TryReadInt(
					spawner,
					new string[] { "SpawnRange", "MaxSpawnRange" },
					out spawnRange) &&
					spawnRange > 0)
				{
					profile.SpawnRangeMax = Math.Max(1, Math.Min(200, spawnRange));
					profile.SpawnRangeMin = Math.Min(
						profile.SpawnRangeMin,
						profile.SpawnRangeMax);

					imported.Add("spawn range");
				}

				int homeRange;
				if (TryReadInt(
					spawner,
					new string[] { "HomeRange", "RangeHome" },
					out homeRange) &&
					homeRange >= 0)
				{
					profile.HomeRange = Math.Max(0, Math.Min(500, homeRange));
					imported.Add("home range");
				}

				int proximityRange;
				if (TryReadInt(
					spawner,
					new string[] { "ProximityRange", "TriggerRange" },
					out proximityRange) &&
					proximityRange > 0)
				{
					profile.WakeRange = Math.Max(20, Math.Min(300, proximityRange));
					profile.DespawnPlayerRange = profile.WakeRange;
					imported.Add("proximity/wake range");
				}

				int team;
				if (TryReadInt(
					spawner,
					new string[] { "Team", "SpawnerTeam" },
					out team))
				{
					profile.CreatureTeam = Math.Max(-1, Math.Min(1000, team));
					imported.Add("creature team");
				}

				bool smartSpawning;
				if (TryReadBool(
					spawner,
					new string[] { "SmartSpawning", "SmartSpawn", "SmartAI" },
					out smartSpawning))
				{
					profile.SmartAI = smartSpawning;
					imported.Add("smart AI state");
				}

				profile.SpawnChance = 100;
				imported.Add("spawn chance");

				return imported.Count == 0
					? "No compatible settings were found; only the creature roster was imported."
					: "Imported compatible settings: " + String.Join(", ", imported.ToArray()) + ".";
			}

			private static bool TryReadValue(object source, string[] names, out object value)
			{
				value = null;
				if (source == null)
					return false;

				Type type = source.GetType();
				BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

				foreach (string name in names)
				{
					try
					{
						PropertyInfo property = type.GetProperty(name, flags);
						if (property != null && property.GetIndexParameters().Length == 0)
						{
							value = property.GetValue(source, null);
							return true;
						}

						FieldInfo field = type.GetField(name, flags);
						if (field != null)
						{
							value = field.GetValue(source);
							return true;
						}
					}
					catch
					{
					}
				}

				return false;
			}

			private static bool TryReadInt(object source, string[] names, out int value)
			{
				value = 0;
				object raw;
				if (!TryReadValue(source, names, out raw) || raw == null)
					return false;

				try
				{
					value = Convert.ToInt32(raw);
					return true;
				}
				catch
				{
					return false;
				}
			}

			private static bool TryReadBool(object source, string[] names, out bool value)
			{
				value = false;
				object raw;
				if (!TryReadValue(source, names, out raw) || raw == null)
					return false;

				try
				{
					value = Convert.ToBoolean(raw);
					return true;
				}
				catch
				{
					return false;
				}
			}

			private static bool TryReadTimeSpan(object source, string[] names, out TimeSpan value)
			{
				value = TimeSpan.Zero;
				object raw;
				if (!TryReadValue(source, names, out raw) || raw == null)
					return false;

				if (raw is TimeSpan)
				{
					value = (TimeSpan)raw;
					return true;
				}

				return false;
			}
		}
	}

	// ============================================================================
	// ADMIN UI (MAIN GUMP)
	// ============================================================================
	public class KamRegionSpawnerGump : Gump
	{
		private const int RegionsPerPage = 10;
		private const int CategoriesPerPage = 5;

		private int m_Page;
		private int m_CategoryPage;
		private string m_Search;
		private string m_CategoryFilter;
		private List<string> m_Categories;

		private const string BlueColor = "#66CCFF";
		private const string GreenColor = "#66FF66";
		private const string RedColor = "#FF6666";
		private const string WhiteColor = "#FFFFFF";
		private const string YellowColor = "#FFFF66";

		public KamRegionSpawnerGump(int page, string search, string categoryFilter)
			: this(page, search, categoryFilter, -1)
		{
		}

		public KamRegionSpawnerGump(int page, string search, string categoryFilter, int categoryPage)
			: base(60, 55)
		{
			m_Page = Math.Max(0, page);
			m_Search = search == null ? "" : search.ToLowerInvariant();
			m_Categories = KamRegionSpawnerSystem.GetRegionCategories();
			m_CategoryFilter = NormalizeCategoryFilter(categoryFilter);

			int categoryPageCount = Math.Max(1, (m_Categories.Count + CategoriesPerPage - 1) / CategoriesPerPage);
			m_CategoryPage = categoryPage < 0 ? FindCategoryPage(m_CategoryFilter) : categoryPage;
			m_CategoryPage = Math.Max(0, Math.Min(categoryPageCount - 1, m_CategoryPage));

			Closable = true;
			Disposable = true;
			Dragable = true;
			Resizable = false;

			AddPage(0);
			AddBackground(0, 0, 760, 560, 9270);
			AddAlphaRegion(12, 12, 736, 536);
			AddHtmlColor(28, 22, 330, 24, "Kam Region Spawner", BlueColor);

			List<Region> filtered = BuildFilteredRegions();
			int totalPages = Math.Max(1, (filtered.Count + RegionsPerPage - 1) / RegionsPerPage);
			if (m_Page >= totalPages)
				m_Page = totalPages - 1;

			AddHtmlColor(365, 22, 370, 24,
				$"Filter: {m_CategoryFilter}  Regions {m_Page + 1}/{totalPages}",
				YellowColor);

			DrawCategoryFilters(categoryPageCount);

			AddButton(600, 56, 4005, 4007, 12, GumpButtonType.Reply, 0);
			AddHtmlColor(638, 56, 106, 22, "Current Region", YellowColor);

			AddHtmlColor(32, 88, 70, 22, "Search:", YellowColor);
			AddImageTiled(104, 86, 286, 24, 2624);
			AddAlphaRegion(104, 86, 286, 24);
			AddTextEntry(110, 88, 274, 20, 0x480, 1, m_Search);
			AddButton(404, 86, 4005, 4007, 10, GumpButtonType.Reply, 0);
			AddHtmlColor(442, 88, 64, 22, "Apply", WhiteColor);
			AddButton(518, 86, 4017, 4019, 11, GumpButtonType.Reply, 0);
			AddHtmlColor(552, 88, 64, 22, "Clear", WhiteColor);
			AddHtmlColor(632, 88, 106, 22, $"{filtered.Count} matches", BlueColor);

			AddHtmlColor(32, 124, 230, 20, "Region Name", BlueColor);
			AddHtmlColor(280, 124, 80, 20, "Map", BlueColor);
			AddHtmlColor(380, 124, 95, 20, "Category", BlueColor);
			AddHtmlColor(480, 124, 60, 20, "Status", BlueColor);
			AddHtmlColor(560, 124, 60, 20, "Edit", BlueColor);
			AddHtmlColor(640, 124, 80, 20, "Wipe", BlueColor);

			int start = m_Page * RegionsPerPage;
			int y = 152;

			for (int i = 0; i < RegionsPerPage && start + i < filtered.Count; i++)
			{
				Region region = filtered[start + i];
				string key = KamRegionSpawnerSystem.GetRegionKey(region);
				KamRegionSpawnerProfile profile = KamRegionSpawnerSystem.Controller.Profiles.ContainsKey(key)
					? KamRegionSpawnerSystem.Controller.Profiles[key]
					: null;

				string status = profile != null && profile.Enabled ? "On" : "Off";
				string statusColor = profile != null && profile.Enabled ? GreenColor : RedColor;
				Map displayMap = KamRegionSpawnerSystem.GetRegionMap(region, null);
				string parentText = region.Parent != null
					? " (" + KamRegionSpawnerSystem.GetRegionDisplayName(region.Parent, displayMap) + ")"
					: "";
				string category = KamRegionSpawnerSystem.GetRegionCategory(region);
				string displayName = KamRegionSpawnerSystem.GetRegionDisplayName(region, displayMap);

				AddHtmlColor(32, y, 240, 20, displayName + parentText, WhiteColor);
				AddHtmlColor(280, y, 90, 20, displayMap != null ? displayMap.Name : "Unknown", WhiteColor);
				AddHtmlColor(380, y, 95, 20, ShortLabel(category, 13), WhiteColor);
				AddHtmlColor(480, y, 70, 20, status, statusColor);
				AddButton(560, y, 4005, 4007, 10000 + start + i, GumpButtonType.Reply, 0);
				AddButton(640, y, 4017, 4019, 20000 + start + i, GumpButtonType.Reply, 0);
				y += 36;
			}

			if (m_Page > 0)
				AddButton(584, 510, 4014, 4016, 2, GumpButtonType.Reply, 0);

			if (m_Page + 1 < totalPages)
				AddButton(628, 510, 4005, 4007, 3, GumpButtonType.Reply, 0);

			AddButton(682, 510, 4017, 4019, 0, GumpButtonType.Reply, 0);
			AddHtmlColor(716, 510, 40, 22, "Close", WhiteColor);
		}

		private void DrawCategoryFilters(int pageCount)
		{
			if (m_CategoryPage > 0)
				AddButton(5, 56, 4014, 4016, 4, GumpButtonType.Reply, 0);

			int start = m_CategoryPage * CategoriesPerPage;
			for (int i = 0; i < CategoriesPerPage && start + i < m_Categories.Count; i++)
			{
				int categoryIndex = start + i;
				int x = 35 + (i * 105);
				string category = m_Categories[categoryIndex];
				AddFilterButton(x, 56, 1000 + categoryIndex, ShortLabel(category, 10),
					String.Equals(m_CategoryFilter, category, StringComparison.OrdinalIgnoreCase));
			}

			if (m_CategoryPage + 1 < pageCount)
				AddButton(560, 56, 4005, 4007, 5, GumpButtonType.Reply, 0);
		}

		private string NormalizeCategoryFilter(string categoryFilter)
		{
			if (String.IsNullOrEmpty(categoryFilter))
				return "All";

			foreach (string category in m_Categories)
			{
				if (String.Equals(category, categoryFilter, StringComparison.OrdinalIgnoreCase))
					return category;
			}

			return "All";
		}

		private int FindCategoryPage(string category)
		{
			for (int i = 0; i < m_Categories.Count; i++)
			{
				if (String.Equals(m_Categories[i], category, StringComparison.OrdinalIgnoreCase))
					return i / CategoriesPerPage;
			}

			return 0;
		}

		private List<Region> BuildFilteredRegions()
		{
			List<Region> filtered = new List<Region>();

			foreach (Region region in KamRegionSpawnerSystem.GetUsableRegions())
			{
				Map map = KamRegionSpawnerSystem.GetRegionMap(region, null);
				if (region == null || map == null || map == Map.Internal)
					continue;

				string category = KamRegionSpawnerSystem.GetRegionCategory(region);
				if (!String.Equals(m_CategoryFilter, "All", StringComparison.OrdinalIgnoreCase) &&
					!String.Equals(category, m_CategoryFilter, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				string displayName = KamRegionSpawnerSystem.GetRegionDisplayName(region, map);
				if (!String.IsNullOrEmpty(m_Search) &&
					displayName.IndexOf(m_Search, StringComparison.OrdinalIgnoreCase) < 0 &&
					map.Name.IndexOf(m_Search, StringComparison.OrdinalIgnoreCase) < 0 &&
					category.IndexOf(m_Search, StringComparison.OrdinalIgnoreCase) < 0)
				{
					continue;
				}

				filtered.Add(region);
			}

			filtered.Sort(delegate(Region left, Region right)
			{
				Map leftMap = KamRegionSpawnerSystem.GetRegionMap(left, null);
				Map rightMap = KamRegionSpawnerSystem.GetRegionMap(right, null);
				string leftMapName = leftMap != null ? leftMap.Name : "";
				string rightMapName = rightMap != null ? rightMap.Name : "";
				int mapCompare = String.Compare(leftMapName, rightMapName, StringComparison.OrdinalIgnoreCase);

				return mapCompare != 0
					? mapCompare
					: String.Compare(
						KamRegionSpawnerSystem.GetRegionDisplayName(left, leftMap),
						KamRegionSpawnerSystem.GetRegionDisplayName(right, rightMap),
						StringComparison.OrdinalIgnoreCase);
			});

			return filtered;
		}

		private static string ShortLabel(string text, int maxLength)
		{
			if (String.IsNullOrEmpty(text) || text.Length <= maxLength)
				return text ?? "";

			return text.Substring(0, Math.Max(1, maxLength - 2)) + "..";
		}

		private void AddFilterButton(int x, int y, int buttonId, string label, bool active)
		{
			AddButton(x, y, active ? 4006 : 4005, 4007, buttonId, GumpButtonType.Reply, 0);
			AddHtmlColor(x + 34, y, 69, 22, label, active ? GreenColor : WhiteColor);
		}

		private void AddHtmlColor(int x, int y, int width, int height, string text, string color)
		{
			AddHtml(x, y, width, height, $"<BASEFONT COLOR={color}>{text}</BASEFONT>", false, false);
		}

		public override void OnResponse(NetState state, RelayInfo info)
		{
			Mobile from = state.Mobile;
			if (from == null || from.AccessLevel < AccessLevel.Administrator)
				return;

			if (info.ButtonID >= 1000 && info.ButtonID < 2000)
			{
				int categoryIndex = info.ButtonID - 1000;
				if (categoryIndex >= 0 && categoryIndex < m_Categories.Count)
				{
					string category = m_Categories[categoryIndex];
					from.SendGump(new KamRegionSpawnerGump(0, m_Search, category, categoryIndex / CategoriesPerPage));
				}
				return;
			}

			if (info.ButtonID == 4)
			{
				from.SendGump(new KamRegionSpawnerGump(m_Page, m_Search, m_CategoryFilter, m_CategoryPage - 1));
				return;
			}

			if (info.ButtonID == 5)
			{
				from.SendGump(new KamRegionSpawnerGump(m_Page, m_Search, m_CategoryFilter, m_CategoryPage + 1));
				return;
			}

			if (info.ButtonID == 12)
			{
				if (from.Map == null || from.Map == Map.Internal)
				{
					from.SendMessage("Your current location is not on a usable world map.");
					from.SendGump(new KamRegionSpawnerGump(m_Page, m_Search, m_CategoryFilter, m_CategoryPage));
					return;
				}

				Region currentRegion = KamRegionSpawnerSystem.GetRegionAt(from);
				string currentKey = KamRegionSpawnerSystem.GetRegionKey(currentRegion, from.Map);

				if (currentRegion == null || String.Equals(currentKey, "Unknown|Unknown", StringComparison.OrdinalIgnoreCase))
				{
					from.SendMessage("Your current location does not have a usable region.");
					from.SendGump(new KamRegionSpawnerGump(m_Page, m_Search, m_CategoryFilter, m_CategoryPage));
					return;
				}

				from.SendGump(new KamRegionSpawnerEditGump(
					currentKey,
					m_Page,
					m_Search,
					m_CategoryFilter,
					0));
				return;
			}

			if (info.ButtonID == 10)
			{
				TextRelay relay = info.GetTextEntry(1);
				from.SendGump(new KamRegionSpawnerGump(0, relay != null ? relay.Text : "", m_CategoryFilter, m_CategoryPage));
				return;
			}

			if (info.ButtonID == 11)
			{
				from.SendGump(new KamRegionSpawnerGump(0, "", m_CategoryFilter, m_CategoryPage));
				return;
			}

			if (info.ButtonID == 2)
			{
				from.SendGump(new KamRegionSpawnerGump(m_Page - 1, m_Search, m_CategoryFilter, m_CategoryPage));
				return;
			}

			if (info.ButtonID == 3)
			{
				from.SendGump(new KamRegionSpawnerGump(m_Page + 1, m_Search, m_CategoryFilter, m_CategoryPage));
				return;
			}

			List<Region> filtered = BuildFilteredRegions();

			if (info.ButtonID >= 10000 && info.ButtonID < 20000)
			{
				int index = info.ButtonID - 10000;
				if (index >= 0 && index < filtered.Count)
				{
					Region region = filtered[index];
					from.SendGump(new KamRegionSpawnerEditGump(
						KamRegionSpawnerSystem.GetRegionKey(region),
						m_Page,
						m_Search,
						m_CategoryFilter,
						0));
				}
				return;
			}

			if (info.ButtonID >= 20000 && info.ButtonID < 30000)
			{
				int index = info.ButtonID - 20000;
				if (index < 0 || index >= filtered.Count)
					return;

				Region region = filtered[index];
				string targetKey = KamRegionSpawnerSystem.GetRegionKey(region);
				List<Mobile> toDelete = new List<Mobile>();

				foreach (PlayerAttatchmentExtension attachment in PlayerAttatchmentExt.AllAttachments.Values)
				{
					KamRegionSpawnAttachment regionAttachment = attachment as KamRegionSpawnAttachment;
					if (regionAttachment == null)
						continue;

					Mobile mobile = regionAttachment.AttachedTo as Mobile;
					if (mobile == null || mobile.Deleted || KamRegionSpawnerSystem.IsPlayerOwnedCreature(mobile))
						continue;

					if (KamRegionSpawnerSystem.AttachmentMatchesRegion(regionAttachment, mobile, targetKey))
						toDelete.Add(mobile);
				}

				int deleted = 0;
				foreach (Mobile mobile in toDelete)
				{
					if (mobile != null && !mobile.Deleted)
					{
						mobile.Delete();
						deleted++;
					}
				}

				from.SendMessage($"Wiped {deleted} active Kam Region Spawner creature(s) from {targetKey}.");
				from.SendGump(new KamRegionSpawnerGump(m_Page, m_Search, m_CategoryFilter, m_CategoryPage));
			}
		}
	}

	// ============================================================================
	// EDIT REGION GUMP
	// ============================================================================
	public class KamRegionSpawnerEditGump : Gump
	{
		private string m_RegionKey;
		private int m_ReturnPage;
		private string m_ReturnSearch;
		private string m_ReturnFilter;
		private int m_CreaturePage;
		private KamRegionSpawnerProfile m_Profile;

		private const string BlueColor = "#66CCFF";
		private const string GreenColor = "#66FF66";
		private const string RedColor = "#FF6666";
		private const string WhiteColor = "#FFFFFF";
		private const string YellowColor = "#FFFF66";

		public KamRegionSpawnerEditGump(string regionKey, int returnPage, string returnSearch, string returnFilter, int creaturePage)
			: base(50, 50)
		{
			m_RegionKey = regionKey;
			m_ReturnPage = returnPage;
			m_ReturnSearch = returnSearch;
			m_ReturnFilter = returnFilter;
			m_CreaturePage = Math.Max(0, Math.Min(1, creaturePage));
			m_Profile = KamRegionSpawnerSystem.GetProfile(regionKey);

			Closable = true;
			Disposable = true;
			Dragable = true;
			Resizable = false;

			AddPage(0);
			AddBackground(0, 0, 940, 700, 9270);
			AddAlphaRegion(12, 12, 916, 676);

			Region editRegion = KamRegionSpawnerSystem.FindRegionByKey(regionKey);
			Map editMap = KamRegionSpawnerSystem.GetRegionMap(editRegion, null);
			string displayKey = editRegion != null && editMap != null
				? editMap.Name + " - " + KamRegionSpawnerSystem.GetRegionDisplayName(editRegion, editMap)
				: regionKey.Replace("|", " - ");
			AddHtmlColor(28, 22, 650, 24, $"Editing Profile: {displayKey}", BlueColor);
			AddHtmlColor(28, 45, 680, 20, "Weight 0 disables a creature. Weight 1 is the default selection weight.", WhiteColor);

			AddButton(820, 22, 4005, 4007, 20, GumpButtonType.Reply, 0);
			AddHtmlColor(858, 22, 60, 20, "Help", YellowColor);

			// ==================
			// LEFT SETTINGS COLUMN
			// ==================
			AddHtmlColor(32, 80, 200, 20, "Settings", BlueColor);

			AddButton(32, 110, m_Profile.Enabled ? 4006 : 4005, 4007, 1, GumpButtonType.Reply, 0);
			AddHtmlColor(70, 110, 240, 20, "Spawns Enabled", m_Profile.Enabled ? GreenColor : RedColor);

			AddButton(32, 140, m_Profile.ContinuousSpawns ? 4006 : 4005, 4007, 2, GumpButtonType.Reply, 0);
			AddHtmlColor(70, 140, 260, 20, "Continuous (No Despawn)", m_Profile.ContinuousSpawns ? GreenColor : RedColor);

			AddAdjuster(32, 180, 3, 4, "Spawn Chance %", m_Profile.SpawnChance.ToString());
			AddAdjuster(32, 215, 21, 22, "Randomizer %", m_Profile.RandomizerPercent.ToString());
			AddAdjuster(32, 250, 5, 6, "Hourly Creature Max", m_Profile.HourlyMax.ToString());
			AddAdjuster(32, 285, 23, 24, "Max Spawns / Region", m_Profile.MaxSpawnsPerRegion.ToString());
			AddAdjuster(32, 320, 7, 8, "Cooldown (Mins)", m_Profile.CooldownMins.ToString());
			AddAdjuster(32, 355, 15, 16, "Despawn (Mins)", m_Profile.DespawnMins.ToString());

			AddAdjuster(32, 410, 11, 12, "Min Pack Size", m_Profile.MinPackSize.ToString());
			AddAdjuster(32, 445, 13, 14, "Max Pack Size", m_Profile.MaxPackSize.ToString());
			AddAdjuster(32, 480, 9, 10, "Party Multiplier", m_Profile.PartyMultiplier.ToString());

			AddHtmlColor(32, 525, 68, 20, "Presets:", BlueColor);
			AddButton(100, 525, 4005, 4007, 30, GumpButtonType.Reply, 0);
			AddHtmlColor(137, 525, 45, 20, "Easy", GreenColor);
			AddButton(190, 525, 4005, 4007, 31, GumpButtonType.Reply, 0);
			AddHtmlColor(227, 525, 62, 20, "Medium", YellowColor);
			AddButton(295, 525, 4005, 4007, 32, GumpButtonType.Reply, 0);
			AddHtmlColor(332, 525, 45, 20, "Hard", RedColor);

			int activeCount = KamRegionSpawnerSystem.CountActiveSpawns(m_RegionKey);
			int recentCount = KamRegionSpawnerSystem.GetRecentSpawnCount(m_Profile);
			string activeLimit = m_Profile.MaxSpawnsPerRegion == 0 ? "Unlimited" : m_Profile.MaxSpawnsPerRegion.ToString();
			string encounterMode = $"Per isolated player / shared inside {m_Profile.PlayerClusterRange} tiles";

			AddHtmlColor(32, 570, 330, 20, $"Active region creatures: {activeCount} / {activeLimit}", GreenColor);
			AddHtmlColor(32, 595, 330, 20, $"Spawned during last hour: {recentCount} / {m_Profile.HourlyMax}", WhiteColor);
			AddHtmlColor(32, 620, 350, 20, $"Encounter model: {encounterMode}", WhiteColor);
			AddButton(395, 620, 4005, 4007, 33, GumpButtonType.Reply, 0);
			AddHtmlColor(432, 620, 70, 20, "Refresh", BlueColor);

			AddButton(500, 620, 4005, 4007, 34, GumpButtonType.Reply, 0);
			AddHtmlColor(538, 620, 150, 20, "Go To Region", YellowColor);

			// ==================
			// RIGHT CREATURE COLUMNS (PAGINATED 40 SLOTS)
			// ==================
			AddHtmlColor(365, 80, 260, 20, $"Creature Roster (Page {m_CreaturePage + 1} of 2)", BlueColor);

			AddButton(690, 80, 4005, 4007, 19, GumpButtonType.Reply, 0);
			AddHtmlColor(728, 80, 185, 20, "Import from Spawner", YellowColor);

			AddHtmlColor(550, 105, 50, 20, "Wt.", BlueColor);
			AddHtmlColor(825, 105, 50, 20, "Wt.", BlueColor);

			int startIdx = m_CreaturePage * 20;
			int cx = 365;
			int cy = 125;

			for (int i = 0; i < 20; i++)
			{
				if (i == 10)
				{
					cx = 640;
					cy = 125;
				}

				int absoluteIndex = startIdx + i;
				KamRegionCreatureEntry entry = m_Profile.Creatures[absoluteIndex];
				int textHue = entry.Name == "INVALID TYPE" ? 0x22 : 0x480;

				AddImageTiled(cx, cy, 180, 24, 2624);
				AddAlphaRegion(cx, cy, 180, 24);
				AddTextEntry(cx + 4, cy + 2, 170, 20, textHue, 100 + i, entry.Name);

				AddImageTiled(cx + 185, cy, 40, 24, 2624);
				AddAlphaRegion(cx + 185, cy, 40, 24);
				AddTextEntry(cx + 190, cy + 2, 30, 20, 0x35, 200 + i, entry.Weight.ToString());

				cy += 30;
			}

			if (m_CreaturePage == 0)
			{
				AddHtmlColor(790, 445, 45, 20, "Next", WhiteColor);
				AddButton(835, 445, 4005, 4007, 18, GumpButtonType.Reply, 0);
			}
			else
			{
				AddButton(365, 445, 4014, 4016, 17, GumpButtonType.Reply, 0);
				AddHtmlColor(402, 445, 50, 20, "Prev", WhiteColor);
			}

			AddHtmlColor(365, 490, 530, 75,
				"The importer supports ServUO Spawners and common XmlSpawner variants. It adds unique creature types and imports compatible running, count, delay, spawn range, home range, proximity, team, smart-spawn, cap, and chance settings.",
				WhiteColor);

			AddAdjuster(
				365,
				580,
				25,
				26,
				"Player Cluster Range",
				m_Profile.PlayerClusterRange.ToString());

			AddButton(690, 580, 4005, 4007, 35, GumpButtonType.Reply, 0);
			AddHtmlColor(728, 580, 190, 20, "Spawn / AI Options", YellowColor);

			AddButton(32, 660, 4014, 4016, 99, GumpButtonType.Reply, 0);
			AddHtmlColor(70, 660, 65, 20, "Cancel", WhiteColor);

			AddButton(770, 660, 4005, 4007, 1000, GumpButtonType.Reply, 0);
			AddHtmlColor(808, 660, 110, 20, "Save Options", GreenColor);
		}

		private void AddAdjuster(int x, int y, int btnDown, int btnUp, string label, string val)
		{
			AddButton(x, y, 4014, 4016, btnDown, GumpButtonType.Reply, 0);
			AddButton(x + 36, y, 4005, 4007, btnUp, GumpButtonType.Reply, 0);
			AddHtmlColor(x + 78, y, 165, 20, label + ":", YellowColor);
			AddHtmlColor(x + 245, y, 65, 20, val, WhiteColor);
		}

		private void AddHtmlColor(int x, int y, int w, int h, string text, string color)
		{
			AddHtml(x, y, w, h, $"<BASEFONT COLOR={color}>{text}</BASEFONT>", false, false);
		}

		private void SaveCreatureEntries(RelayInfo info)
		{
			int startIdx = m_CreaturePage * 20;
			for (int i = 0; i < 20; i++)
			{
				TextRelay nameRelay = info.GetTextEntry(100 + i);
				TextRelay weightRelay = info.GetTextEntry(200 + i);

				if (nameRelay != null)
				{
					string val = nameRelay.Text == null ? "" : nameRelay.Text.Trim();
					if (string.IsNullOrEmpty(val))
					{
						m_Profile.Creatures[startIdx + i].Name = "";
					}
					else
					{
						Type type = ScriptCompiler.FindTypeByName(val, true);
						if (type != null && !type.IsAbstract && type.IsSubclassOf(typeof(Mobile)))
							m_Profile.Creatures[startIdx + i].Name = type.Name;
						else
							m_Profile.Creatures[startIdx + i].Name = "INVALID TYPE";
					}
				}

				if (weightRelay != null)
				{
					int weight;
					if (int.TryParse(weightRelay.Text, out weight))
						m_Profile.Creatures[startIdx + i].Weight = Math.Max(0, Math.Min(100000, weight));
				}
			}
		}

		private void ApplyPreset(int preset)
		{
			m_Profile.Enabled = true;
			m_Profile.ContinuousSpawns = false;
			m_Profile.PlayerClusterRange = 60;
			m_Profile.SpawnSearchAttempts = 30;
			m_Profile.LeashToRegion = true;
			m_Profile.SmartAI = true;
			m_Profile.WakeRange = 80;
			m_Profile.DespawnPlayerRange = 60;
			m_Profile.CreatureTeam = -1;

			switch (preset)
			{
				case 30: // Easy
					m_Profile.SpawnChance = 35;
					m_Profile.RandomizerPercent = 10;
					m_Profile.HourlyMax = 12;
					m_Profile.MaxSpawnsPerRegion = 15;
					m_Profile.CooldownMins = 8;
					m_Profile.DespawnMins = 8;
					m_Profile.MinPackSize = 1;
					m_Profile.MaxPackSize = 3;
					m_Profile.PartyMultiplier = 1;
					m_Profile.SpawnRangeMin = 10;
					m_Profile.SpawnRangeMax = 35;
					m_Profile.HomeRange = 20;
					m_Profile.RegionRoaming = false;
					m_Profile.SeekPlayers = false;
					m_Profile.AggroRange = 12;
					m_Profile.RoamIntervalSeconds = 30;
					break;

				case 31: // Medium
					m_Profile.SpawnChance = 60;
					m_Profile.RandomizerPercent = 15;
					m_Profile.HourlyMax = 24;
					m_Profile.MaxSpawnsPerRegion = 30;
					m_Profile.CooldownMins = 5;
					m_Profile.DespawnMins = 10;
					m_Profile.MinPackSize = 2;
					m_Profile.MaxPackSize = 5;
					m_Profile.PartyMultiplier = 2;
					m_Profile.SpawnRangeMin = 8;
					m_Profile.SpawnRangeMax = 40;
					m_Profile.HomeRange = 40;
					m_Profile.RegionRoaming = true;
					m_Profile.SeekPlayers = false;
					m_Profile.AggroRange = 18;
					m_Profile.RoamIntervalSeconds = 20;
					break;

				case 32: // Hard
					m_Profile.SpawnChance = 85;
					m_Profile.RandomizerPercent = 25;
					m_Profile.HourlyMax = 40;
					m_Profile.MaxSpawnsPerRegion = 50;
					m_Profile.CooldownMins = 3;
					m_Profile.DespawnMins = 15;
					m_Profile.MinPackSize = 3;
					m_Profile.MaxPackSize = 8;
					m_Profile.PartyMultiplier = 3;
					m_Profile.SpawnRangeMin = 5;
					m_Profile.SpawnRangeMax = 45;
					m_Profile.HomeRange = 0;
					m_Profile.RegionRoaming = true;
					m_Profile.SeekPlayers = true;
					m_Profile.AggroRange = 30;
					m_Profile.RoamIntervalSeconds = 12;
					break;
			}

			m_Profile.Normalize();
		}

		public override void OnResponse(NetState state, RelayInfo info)
		{
			Mobile from = state.Mobile;
			if (from == null || from.AccessLevel < AccessLevel.Administrator)
				return;

			SaveCreatureEntries(info);

			if (info.ButtonID == 99)
			{
				from.SendGump(new KamRegionSpawnerGump(m_ReturnPage, m_ReturnSearch, m_ReturnFilter));
				return;
			}

			if (info.ButtonID == 1000)
			{
				m_Profile.Normalize();
				from.SendMessage("Kam Region Spawner options saved.");
				from.SendGump(new KamRegionSpawnerEditGump(m_RegionKey, m_ReturnPage, m_ReturnSearch, m_ReturnFilter, m_CreaturePage));
				return;
			}

			if (info.ButtonID == 17)
			{
				from.SendGump(new KamRegionSpawnerEditGump(m_RegionKey, m_ReturnPage, m_ReturnSearch, m_ReturnFilter, 0));
				return;
			}

			if (info.ButtonID == 18)
			{
				from.SendGump(new KamRegionSpawnerEditGump(m_RegionKey, m_ReturnPage, m_ReturnSearch, m_ReturnFilter, 1));
				return;
			}

			if (info.ButtonID == 19)
			{
				from.SendMessage("Target an XmlSpawner or ServUO Spawner. The roster and compatible settings will be imported.");
				from.Target = new KamRegionSpawnerSystem.ImportSpawnerTarget(
					m_RegionKey,
					m_ReturnPage,
					m_ReturnSearch,
					m_ReturnFilter,
					m_CreaturePage);
				return;
			}

			if (info.ButtonID == 20)
			{
				from.SendGump(new KamRegionSpawnerHelpGump(
					m_RegionKey,
					m_ReturnPage,
					m_ReturnSearch,
					m_ReturnFilter,
					m_CreaturePage));
				return;
			}

			if (info.ButtonID >= 30 && info.ButtonID <= 32)
			{
				ApplyPreset(info.ButtonID);
				string presetName = info.ButtonID == 30 ? "Easy" : info.ButtonID == 31 ? "Medium" : "Hard";
				from.SendMessage($"{presetName} Kam Region Spawner preset applied.");
				from.SendGump(new KamRegionSpawnerEditGump(m_RegionKey, m_ReturnPage, m_ReturnSearch, m_ReturnFilter, m_CreaturePage));
				return;
			}

			if (info.ButtonID == 33)
			{
				from.SendGump(new KamRegionSpawnerEditGump(m_RegionKey, m_ReturnPage, m_ReturnSearch, m_ReturnFilter, m_CreaturePage));
				return;
			}

			if (info.ButtonID == 34)
			{
				Region targetRegion;
				Point3D destination;

				if (KamRegionSpawnerSystem.TryGetRegionTeleportLocation(
					m_RegionKey,
					out targetRegion,
					out destination))
				{
					from.MoveToWorld(destination, targetRegion.Map);
					from.SendMessage(
						$"Teleported to {KamRegionSpawnerSystem.GetRegionDisplayName(targetRegion)} at {destination}.");
				}
				else
				{
					from.SendMessage(
						33,
						"No safe walkable location could be found inside that region.");
				}

				from.SendGump(new KamRegionSpawnerEditGump(
					m_RegionKey,
					m_ReturnPage,
					m_ReturnSearch,
					m_ReturnFilter,
					m_CreaturePage));
				return;
			}

			if (info.ButtonID == 35)
			{
				from.SendGump(new KamRegionSpawnerBehaviorGump(
					m_RegionKey,
					m_ReturnPage,
					m_ReturnSearch,
					m_ReturnFilter,
					m_CreaturePage));
				return;
			}

			switch (info.ButtonID)
			{
				case 1: m_Profile.Enabled = !m_Profile.Enabled; break;
				case 2: m_Profile.ContinuousSpawns = !m_Profile.ContinuousSpawns; break;
				case 3: m_Profile.SpawnChance = Math.Max(1, m_Profile.SpawnChance - 5); break;
				case 4: m_Profile.SpawnChance = Math.Min(100, m_Profile.SpawnChance + 5); break;
				case 5: m_Profile.HourlyMax = Math.Max(1, m_Profile.HourlyMax - 1); break;
				case 6: m_Profile.HourlyMax = Math.Min(500, m_Profile.HourlyMax + 1); break;
				case 7: m_Profile.CooldownMins = Math.Max(1, m_Profile.CooldownMins - 1); break;
				case 8: m_Profile.CooldownMins = Math.Min(240, m_Profile.CooldownMins + 1); break;
				case 9: m_Profile.PartyMultiplier = Math.Max(1, m_Profile.PartyMultiplier - 1); break;
				case 10: m_Profile.PartyMultiplier = Math.Min(10, m_Profile.PartyMultiplier + 1); break;
				case 11: m_Profile.MinPackSize = Math.Max(1, m_Profile.MinPackSize - 1); break;
				case 12: m_Profile.MinPackSize = Math.Min(m_Profile.MaxPackSize, m_Profile.MinPackSize + 1); break;
				case 13: m_Profile.MaxPackSize = Math.Max(m_Profile.MinPackSize, m_Profile.MaxPackSize - 1); break;
				case 14: m_Profile.MaxPackSize = Math.Min(50, m_Profile.MaxPackSize + 1); break;
				case 15: m_Profile.DespawnMins = Math.Max(1, m_Profile.DespawnMins - 1); break;
				case 16: m_Profile.DespawnMins = Math.Min(240, m_Profile.DespawnMins + 1); break;
				case 21: m_Profile.RandomizerPercent = Math.Max(0, m_Profile.RandomizerPercent - 5); break;
				case 22: m_Profile.RandomizerPercent = Math.Min(100, m_Profile.RandomizerPercent + 5); break;
				case 23: m_Profile.MaxSpawnsPerRegion = Math.Max(0, m_Profile.MaxSpawnsPerRegion - 5); break;
				case 24: m_Profile.MaxSpawnsPerRegion = Math.Min(500, m_Profile.MaxSpawnsPerRegion + 5); break;
				case 25: m_Profile.PlayerClusterRange = Math.Max(10, m_Profile.PlayerClusterRange - 5); break;
				case 26: m_Profile.PlayerClusterRange = Math.Min(200, m_Profile.PlayerClusterRange + 5); break;
			}

			m_Profile.Normalize();
			from.SendGump(new KamRegionSpawnerEditGump(m_RegionKey, m_ReturnPage, m_ReturnSearch, m_ReturnFilter, m_CreaturePage));
		}
	}


	// ============================================================================
	// ADVANCED SPAWN PLACEMENT / CREATURE AI GUMP
	// ============================================================================
	public class KamRegionSpawnerBehaviorGump : Gump
	{
		private string m_RegionKey;
		private int m_ReturnPage;
		private string m_ReturnSearch;
		private string m_ReturnFilter;
		private int m_CreaturePage;
		private KamRegionSpawnerProfile m_Profile;

		private const string BlueColor = "#66CCFF";
		private const string GreenColor = "#66FF66";
		private const string RedColor = "#FF6666";
		private const string WhiteColor = "#FFFFFF";
		private const string YellowColor = "#FFFF66";

		public KamRegionSpawnerBehaviorGump(
			string regionKey,
			int returnPage,
			string returnSearch,
			string returnFilter,
			int creaturePage)
			: base(90, 65)
		{
			m_RegionKey = regionKey;
			m_ReturnPage = returnPage;
			m_ReturnSearch = returnSearch;
			m_ReturnFilter = returnFilter;
			m_CreaturePage = creaturePage;
			m_Profile = KamRegionSpawnerSystem.GetProfile(regionKey);

			Closable = true;
			Disposable = true;
			Dragable = true;
			Resizable = false;

			AddPage(0);
			AddBackground(0, 0, 820, 640, 9270);
			AddAlphaRegion(12, 12, 796, 616);

			AddHtmlColor(
				28,
				22,
				540,
				24,
				"Kam Region Spawner - Spawn Placement and Creature AI",
				BlueColor);

			AddHtmlColor(
				28,
				48,
				760,
				42,
				"These controls are inspired by XmlSpawner spawn range, home range, team, proximity, and smart-spawning options. They affect only creatures created by this regional profile.",
				WhiteColor);

			AddHtmlColor(32, 102, 300, 20, "Spawn Placement", BlueColor);
			AddAdjuster(
				32,
				132,
				5,
				6,
				"Minimum Spawn Distance",
				m_Profile.SpawnRangeMin.ToString());

			AddAdjuster(
				32,
				167,
				7,
				8,
				"Maximum Spawn Distance",
				m_Profile.SpawnRangeMax.ToString());

			AddAdjuster(
				32,
				202,
				9,
				10,
				"Spawn Search Attempts",
				m_Profile.SpawnSearchAttempts.ToString());

			AddHtmlColor(420, 102, 330, 20, "Creature Behavior", BlueColor);
			AddToggle(420, 132, 1, "Region Roaming", m_Profile.RegionRoaming);
			AddToggle(420, 162, 2, "Seek &amp; Destroy Players", m_Profile.SeekPlayers);
			AddToggle(420, 192, 3, "Stay Inside Source Region", m_Profile.LeashToRegion);
			AddToggle(420, 222, 4, "Smart AI Sleep", m_Profile.SmartAI);

			AddHtmlColor(32, 270, 300, 20, "Movement and Awareness", BlueColor);
			AddAdjuster(
				32,
				300,
				11,
				12,
				"Home Range (0 = Region)",
				m_Profile.HomeRange.ToString());

			AddAdjuster(
				32,
				335,
				13,
				14,
				"Seek / Aggro Range",
				m_Profile.AggroRange.ToString());

			AddAdjuster(
				32,
				370,
				15,
				16,
				"Roam Retarget Seconds",
				m_Profile.RoamIntervalSeconds.ToString());

			AddHtmlColor(420, 270, 330, 20, "Performance and Ownership", BlueColor);
			AddAdjuster(
				420,
				300,
				17,
				18,
				"AI Wake Range",
				m_Profile.WakeRange.ToString());

			AddAdjuster(
				420,
				335,
				19,
				20,
				"Despawn Player Range",
				m_Profile.DespawnPlayerRange.ToString());

			string teamText = m_Profile.CreatureTeam < 0
				? "Native"
				: m_Profile.CreatureTeam.ToString();

			AddAdjuster(
				420,
				370,
				21,
				22,
				"Creature Team",
				teamText);

			string roamText = m_Profile.RegionRoaming
				? (m_Profile.HomeRange == 0
					? "Region-wide roaming through short local waypoints"
					: "Roaming within " + m_Profile.HomeRange + " tiles of the original spawn")
				: "Native creature wandering only";

			string seekText = m_Profile.SeekPlayers
				? "Actively acquires visible players within " + m_Profile.AggroRange + " tiles"
				: "Uses each creature type's native aggression rules";

			AddHtmlColor(
				32,
				435,
				750,
				22,
				"Current roaming: " + roamText,
				GreenColor);

			AddHtmlColor(
				32,
				462,
				750,
				22,
				"Current combat behavior: " + seekText,
				YellowColor);

			AddHtmlColor(
				32,
				500,
				750,
				70,
				"Region roaming assigns nearby walkable destinations so creatures spread naturally instead of remaining near their original encounter point. Seek &amp; Destroy selects the nearest visible player and then allows the creature's native AI to chase and fight. The region leash first attempts normal walking and uses a safety relocation only after a creature remains trapped outside its source region for 30 seconds.",
				WhiteColor);

			AddButton(32, 595, 4014, 4016, 99, GumpButtonType.Reply, 0);
			AddHtmlColor(70, 595, 120, 20, "Back to Editor", WhiteColor);

			AddButton(665, 595, 4005, 4007, 1000, GumpButtonType.Reply, 0);
			AddHtmlColor(703, 595, 90, 20, "Save", GreenColor);
		}

		private void AddToggle(
			int x,
			int y,
			int buttonID,
			string label,
			bool enabled)
		{
			AddButton(
				x,
				y,
				enabled ? 4006 : 4005,
				4007,
				buttonID,
				GumpButtonType.Reply,
				0);

			AddHtmlColor(
				x + 38,
				y,
				340,
				20,
				label,
				enabled ? GreenColor : RedColor);
		}

		private void AddAdjuster(
			int x,
			int y,
			int downID,
			int upID,
			string label,
			string value)
		{
			AddButton(x, y, 4014, 4016, downID, GumpButtonType.Reply, 0);
			AddButton(x + 36, y, 4005, 4007, upID, GumpButtonType.Reply, 0);
			AddHtmlColor(x + 78, y, 220, 20, label + ":", YellowColor);
			AddHtmlColor(x + 300, y, 70, 20, value, WhiteColor);
		}

		private void AddHtmlColor(
			int x,
			int y,
			int width,
			int height,
			string text,
			string color)
		{
			AddHtml(
				x,
				y,
				width,
				height,
				"<BASEFONT COLOR=" + color + ">" + text + "</BASEFONT>",
				false,
				false);
		}

		public override void OnResponse(NetState state, RelayInfo info)
		{
			Mobile from = state.Mobile;
			if (from == null || from.AccessLevel < AccessLevel.Administrator)
				return;

			if (info.ButtonID == 99)
			{
				from.SendGump(new KamRegionSpawnerEditGump(
					m_RegionKey,
					m_ReturnPage,
					m_ReturnSearch,
					m_ReturnFilter,
					m_CreaturePage));
				return;
			}

			if (info.ButtonID == 1000)
			{
				m_Profile.Normalize();
				from.SendMessage("Kam Region Spawner placement and AI options saved.");
				from.SendGump(new KamRegionSpawnerBehaviorGump(
					m_RegionKey,
					m_ReturnPage,
					m_ReturnSearch,
					m_ReturnFilter,
					m_CreaturePage));
				return;
			}

			switch (info.ButtonID)
			{
				case 1:
					m_Profile.RegionRoaming = !m_Profile.RegionRoaming;
					break;
				case 2:
					m_Profile.SeekPlayers = !m_Profile.SeekPlayers;
					break;
				case 3:
					m_Profile.LeashToRegion = !m_Profile.LeashToRegion;
					break;
				case 4:
					m_Profile.SmartAI = !m_Profile.SmartAI;
					break;
				case 5:
					m_Profile.SpawnRangeMin = Math.Max(0, m_Profile.SpawnRangeMin - 1);
					break;
				case 6:
					m_Profile.SpawnRangeMin = Math.Min(
						m_Profile.SpawnRangeMax,
						m_Profile.SpawnRangeMin + 1);
					break;
				case 7:
					m_Profile.SpawnRangeMax = Math.Max(
						Math.Max(1, m_Profile.SpawnRangeMin),
						m_Profile.SpawnRangeMax - 5);
					break;
				case 8:
					m_Profile.SpawnRangeMax = Math.Min(
						200,
						m_Profile.SpawnRangeMax + 5);
					break;
				case 9:
					m_Profile.SpawnSearchAttempts = Math.Max(
						5,
						m_Profile.SpawnSearchAttempts - 5);
					break;
				case 10:
					m_Profile.SpawnSearchAttempts = Math.Min(
						100,
						m_Profile.SpawnSearchAttempts + 5);
					break;
				case 11:
					m_Profile.HomeRange = Math.Max(0, m_Profile.HomeRange - 5);
					break;
				case 12:
					m_Profile.HomeRange = Math.Min(500, m_Profile.HomeRange + 5);
					break;
				case 13:
					m_Profile.AggroRange = Math.Max(5, m_Profile.AggroRange - 5);
					break;
				case 14:
					m_Profile.AggroRange = Math.Min(200, m_Profile.AggroRange + 5);
					break;
				case 15:
					m_Profile.RoamIntervalSeconds = Math.Max(
						5,
						m_Profile.RoamIntervalSeconds - 1);
					break;
				case 16:
					m_Profile.RoamIntervalSeconds = Math.Min(
						120,
						m_Profile.RoamIntervalSeconds + 1);
					break;
				case 17:
					m_Profile.WakeRange = Math.Max(20, m_Profile.WakeRange - 5);
					break;
				case 18:
					m_Profile.WakeRange = Math.Min(300, m_Profile.WakeRange + 5);
					break;
				case 19:
					m_Profile.DespawnPlayerRange = Math.Max(
						10,
						m_Profile.DespawnPlayerRange - 5);
					break;
				case 20:
					m_Profile.DespawnPlayerRange = Math.Min(
						300,
						m_Profile.DespawnPlayerRange + 5);
					break;
				case 21:
					m_Profile.CreatureTeam = Math.Max(
						-1,
						m_Profile.CreatureTeam - 1);
					break;
				case 22:
					m_Profile.CreatureTeam = Math.Min(
						1000,
						m_Profile.CreatureTeam + 1);
					break;
			}

			m_Profile.Normalize();
			from.SendGump(new KamRegionSpawnerBehaviorGump(
				m_RegionKey,
				m_ReturnPage,
				m_ReturnSearch,
				m_ReturnFilter,
				m_CreaturePage));
		}
	}

	// ============================================================================
	// HELP GUMP
	// ============================================================================
	public class KamRegionSpawnerHelpGump : Gump
	{
		private string m_RegionKey;
		private int m_ReturnPage;
		private string m_ReturnSearch;
		private string m_ReturnFilter;
		private int m_CreaturePage;
		private int m_HelpPage;

		private const string BlueColor = "#66CCFF";
		private const string GreenColor = "#66FF66";
		private const string RedColor = "#FF6666";
		private const string WhiteColor = "#FFFFFF";
		private const string YellowColor = "#FFFF66";
		private const string GrayColor = "#CCCCCC";

		public KamRegionSpawnerHelpGump(
			string regionKey,
			int returnPage,
			string returnSearch,
			string returnFilter,
			int creaturePage)
			: this(
				regionKey,
				returnPage,
				returnSearch,
				returnFilter,
				creaturePage,
				0)
		{
		}

		public KamRegionSpawnerHelpGump(
			string regionKey,
			int returnPage,
			string returnSearch,
			string returnFilter,
			int creaturePage,
			int helpPage)
			: base(70, 50)
		{
			m_RegionKey = regionKey;
			m_ReturnPage = returnPage;
			m_ReturnSearch = returnSearch;
			m_ReturnFilter = returnFilter;
			m_CreaturePage = creaturePage;
			m_HelpPage = Math.Max(0, Math.Min(4, helpPage));

			Closable = true;
			Disposable = true;
			Dragable = true;
			Resizable = false;

			KamRegionSpawnerProfile profile =
				KamRegionSpawnerSystem.GetProfile(regionKey);

			AddPage(0);
			AddBackground(0, 0, 920, 690, 9270);
			AddAlphaRegion(12, 12, 896, 666);

			AddHtmlColor(
				28,
				20,
				520,
				26,
				"Kam Region Spawner Help - Version 3.1",
				BlueColor);

			AddHtmlColor(
				650,
				20,
				235,
				24,
				"Profile: " + ShortRegionKey(regionKey),
				GrayColor);

			DrawTab(28, 58, 100, "Overview", 0);
			DrawTab(190, 58, 101, "Encounters", 1);
			DrawTab(352, 58, 102, "Placement", 2);
			DrawTab(514, 58, 103, "AI / Roaming", 3);
			DrawTab(676, 58, 104, "Import / Safety", 4);

			AddImageTiled(26, 96, 868, 518, 2624);
			AddAlphaRegion(26, 96, 868, 518);

			AddHtmlColor(
				44,
				110,
				832,
				24,
				GetPageTitle(),
				YellowColor);

			// The native Ultima Online client is very strict about long HTML strings.
			// Each topic is deliberately drawn as its own independent HTML control.
			// This avoids nested tags, unsupported closing tags, and parser truncation.
			DrawHelpPage(profile);

			AddButton(28, 638, 4014, 4016, 1, GumpButtonType.Reply, 0);
			AddHtmlColor(66, 638, 130, 20, "Back to Editor", WhiteColor);

			AddHtmlColor(
				575,
				638,
				310,
				20,
				"Select a tab for complete system guidance.",
				GrayColor);
		}

		private void DrawTab(
			int x,
			int y,
			int buttonID,
			string label,
			int page)
		{
			bool selected = m_HelpPage == page;

			AddButton(
				x,
				y,
				selected ? 4006 : 4005,
				4007,
				buttonID,
				GumpButtonType.Reply,
				0);

			AddHtmlColor(
				x + 38,
				y,
				120,
				20,
				label,
				selected ? GreenColor : WhiteColor);
		}

		private string GetPageTitle()
		{
			switch (m_HelpPage)
			{
				case 1:
					return "Encounter Checks, Limits, Packs, and Clustering";
				case 2:
					return "Spawn Placement, Home Range, and Region Boundaries";
				case 3:
					return "Creature AI, Roaming, Seeking, and Leashing";
				case 4:
					return "Spawner Importing, Cleanup, Presets, and Safety";
				default:
					return "Complete System Overview and Recommended Workflow";
			}
		}

		private void DrawHelpPage(KamRegionSpawnerProfile profile)
		{
			switch (m_HelpPage)
			{
				case 1:
					DrawEncounterPage(profile);
					break;
				case 2:
					DrawPlacementPage(profile);
					break;
				case 3:
					DrawBehaviorPage(profile);
					break;
				case 4:
					DrawImportSafetyPage(profile);
					break;
				default:
					DrawOverviewPage(profile);
					break;
			}
		}

		private void DrawOverviewPage(KamRegionSpawnerProfile profile)
		{
			int leftY = 146;
			int rightY = 146;

			leftY = DrawSection(
				44,
				leftY,
				398,
				"PLAYER-CENTRIC ENGINE",
				"Every 15 seconds the system checks active players instead of running a physical spawner at every location. A regional encounter proceeds only when its profile is enabled and all cooldown and cap checks allow it.",
				68,
				BlueColor);

			leftY = DrawSection(
				44,
				leftY,
				398,
				"REGIONAL PROFILES",
				"Each region has its own enabled state, creature roster, limits, pack settings, placement settings, AI behavior, and cleanup rules. New regional profiles are disabled by default.",
				64,
				YellowColor);

			leftY = DrawSection(
				44,
				leftY,
				398,
				"PLAYER CLUSTERS",
				"Players connected within " + profile.PlayerClusterRange + " tiles form one encounter group. The connection is transitive, so nearby chains of players remain one cluster. Isolated players elsewhere in the same large region can still trigger separate encounters.",
				78,
				YellowColor);

			DrawSection(
				44,
				leftY,
				398,
				"SUCCESS-BASED COOLDOWNS",
				"A cooldown begins only after at least one creature is successfully created. A blocked placement, empty roster, invalid creature type, or failed attachment does not consume the player's encounter cooldown.",
				68,
				GreenColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"SUPPORTED REGIONS AND MAPS",
				"Supports named regions, default overworld regions, custom region classes, Ultima Live custom maps, and Regions in a Box when those areas are registered with ServUO's region system.",
				72,
				BlueColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"SPAWN OWNERSHIP TRACKING",
				"Every spawned mobile receives a Kam Region Spawner attachment containing its source map, source region key, spawn time, cleanup mode, and despawn duration. Counts and wipes use that saved ownership.",
				78,
				YellowColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"PLAYER-OWNED CREATURES",
				"Controlled or stabled creatures are protected. They stop counting against the regional cap and are excluded from roaming, seeking, leashing, abandonment cleanup, and region wipes.",
				70,
				GreenColor);

			DrawSection(
				462,
				rightY,
				414,
				"RECOMMENDED WORKFLOW",
				"Select a region, apply Easy or Medium, add or import creature types, verify placement, review Spawn / AI Options, then save. Use Refresh for live counts, Go To Region for testing, and Wipe only for this system's tracked creatures.",
				78,
				YellowColor);
		}

		private void DrawEncounterPage(KamRegionSpawnerProfile profile)
		{
			string activeLimit = profile.MaxSpawnsPerRegion == 0
				? "Unlimited"
				: profile.MaxSpawnsPerRegion.ToString();

			int leftY = 146;
			int rightY = 146;

			leftY = DrawSection(
				44,
				leftY,
				398,
				"SPAWNS ENABLED",
				"Master switch for this regional profile. Current state: " + OnOff(profile.Enabled) + ". Opening or editing a profile does not automatically enable it.",
				58,
				BlueColor);

			leftY = DrawSection(
				44,
				leftY,
				398,
				"SPAWN CHANCE AND RANDOMIZER",
				"Spawn Chance is currently " + profile.SpawnChance + " percent. Randomizer is " + profile.RandomizerPercent + " percent and varies chance, pack size, cooldown, and despawn time. It never raises the hourly or active hard caps.",
				78,
				YellowColor);

			leftY = DrawSection(
				44,
				leftY,
				398,
				"HOURLY CREATURE MAX",
				"Rolling one-hour limit based on creatures actually created, not encounter attempts. Current limit: " + profile.HourlyMax + ". Old entries automatically fall out of the one-hour history.",
				64,
				YellowColor);

			DrawSection(
				44,
				leftY,
				398,
				"MAX SPAWNS PER REGION",
				"Strict active-creature cap for the entire source region. Current value: " + activeLimit + ". A value of 0 disables this cap. Tamed and stabled creatures do not count.",
				68,
				GreenColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"COOLDOWN",
				"Current cooldown: " + profile.CooldownMins + " minute(s). It is tracked per isolated player or connected player cluster and begins only after a successful encounter.",
				52,
				BlueColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"PACK SIZE",
				"The engine selects a base pack between " + profile.MinPackSize + " and " + profile.MaxPackSize + ". Randomizer may vary the final count, but the hourly and regional caps are applied afterward.",
				58,
				YellowColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"PARTY MULTIPLIER",
				"When more than one living party member is within 30 tiles of the encounter player, the base pack is multiplied by " + profile.PartyMultiplier + ". The result is still restricted by both hard caps.",
				60,
				YellowColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"CREATURE ROSTER AND WEIGHTS",
				"Up to 40 Mobile-derived creature types may be configured. Weight 0 disables an entry. Higher weights make a valid type more likely relative to the other enabled entries.",
				54,
				GreenColor);

			DrawSection(
				462,
				rightY,
				414,
				"CONTINUOUS AND DESPAWN",
				"Continuous is currently " + OnOff(profile.ContinuousSpawns) + ". When off, abandoned creatures use a " + profile.DespawnMins + "-minute timer after no normal player remains within the configured cleanup range. Combat refreshes the timer.",
				62,
				profile.ContinuousSpawns ? GreenColor : YellowColor);
		}

		private void DrawPlacementPage(KamRegionSpawnerProfile profile)
		{
			int leftY = 146;
			int rightY = 146;

			leftY = DrawSection(
				44,
				leftY,
				398,
				"SPAWN DISTANCE",
				"Each encounter selects a random distance between " + profile.SpawnRangeMin + " and " + profile.SpawnRangeMax + " tiles from the chosen encounter player. This replaces the older fixed search rings.",
				66,
				BlueColor);

			leftY = DrawSection(
				44,
				leftY,
				398,
				"SEARCH ATTEMPTS",
				"The engine tests up to " + profile.SpawnSearchAttempts + " random positions. More attempts improve success in narrow dungeons and irregular regions but require additional map and region validation.",
				68,
				YellowColor);

			leftY = DrawSection(
				44,
				leftY,
				398,
				"PLACEMENT VALIDATION",
				"Every candidate must be inside the map, permit a mobile to spawn, and resolve to the same stable region key as the player. Each pack member is validated separately before placement.",
				72,
				GreenColor);

			DrawSection(
				44,
				leftY,
				398,
				"FAILED PLACEMENT",
				"If no legal point is found, nothing is spawned and no cooldown is consumed. Increase search attempts or adjust the minimum and maximum distance for difficult terrain.",
				64,
				YellowColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"HOME RANGE",
				"Current Home Range: " + profile.HomeRange + ". Above 0, the original spawn point becomes the creature's home and native idle movement is limited around that location.",
				54,
				BlueColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"HOME RANGE 0",
				"A value of 0 removes the spawn-centered radius. When Region Roaming is enabled, the creature may gradually move throughout the source region using short validated destinations.",
				56,
				YellowColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"REGION BOUNDARIES",
				"Normal named regions, default overworld regions, subregions, custom region classes, Ultima Live regions, and Regions in a Box use ServUO's Region.Find result and the saved region key.",
				60,
				GreenColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"GO TO REGION",
				"The editor searches the region's GoLocation and area rectangles for a safe walkable destination. Default overworld regions are sampled safely when they do not expose a normal area array.",
				56,
				YellowColor);

			DrawSection(
				462,
				rightY,
				414,
				"PACK OFFSETS",
				"Pack members are placed near the selected encounter point. Every offset is rechecked, preventing a member from being pushed into another region, blocked cell, or invalid map location.",
				54,
				GreenColor);
		}

		private void DrawBehaviorPage(KamRegionSpawnerProfile profile)
		{
			int leftY = 146;
			int rightY = 146;

			leftY = DrawSection(
				44,
				leftY,
				398,
				"REGION ROAMING",
				"Current state: " + OnOff(profile.RegionRoaming) + ". When enabled, idle tracked creatures receive changing walkable destinations instead of remaining near the original spawn tile.",
				62,
				profile.RegionRoaming ? GreenColor : BlueColor);

			leftY = DrawSection(
				44,
				leftY,
				398,
				"ROAM RETARGET",
				"Current interval: " + profile.RoamIntervalSeconds + " second(s). A new destination is chosen after the timer expires or when the current destination is reached. Combat movement always takes priority.",
				68,
				YellowColor);

			leftY = DrawSection(
				44,
				leftY,
				398,
				"SEEK AND DESTROY",
				"Current state: " + OnOff(profile.SeekPlayers) + ". When enabled, the nearest visible normal player within the configured aggression range may be assigned as the combat target.",
				68,
				profile.SeekPlayers ? GreenColor : YellowColor);

			DrawSection(
				44,
				leftY,
				398,
				"AGGRESSION RANGE",
				"Current range: " + profile.AggroRange + " tiles. The selected target must be alive, visible, on the same map, and inside the source region when region leashing is enabled.",
				66,
				BlueColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"STAY INSIDE SOURCE REGION",
				"Current state: " + OnOff(profile.LeashToRegion) + ". Targets outside the source region are released. A creature pulled outside first tries to walk back and receives a safe recovery only after remaining outside.",
				82,
				profile.LeashToRegion ? GreenColor : RedColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"SMART AI SLEEP",
				"Current state: " + OnOff(profile.SmartAI) + ". When enabled, custom roaming and seeking checks pause while no normal player is within " + profile.WakeRange + " tiles. Native ServUO sector activity still applies.",
				78,
				profile.SmartAI ? GreenColor : YellowColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"CREATURE TEAM",
				profile.CreatureTeam < 0
					? "Current value: Native. Each creature keeps the Team value defined by its own script."
					: "Current value: Team " + profile.CreatureTeam + ". Every creature spawned by this profile receives that team value.",
				60,
				BlueColor);

			DrawSection(
				462,
				rightY,
				414,
				"NATIVE AI IS PRESERVED",
				"The system does not replace Melee, Mage, Archer, Animal, Berserk, or other creature AI classes. It supplies validated roaming destinations and targets, then lets native AI handle pathfinding, spells, movement, and combat.",
				86,
				GreenColor);
		}

		private void DrawImportSafetyPage(KamRegionSpawnerProfile profile)
		{
			int leftY = 146;
			int rightY = 146;

			leftY = DrawSection(
				44,
				leftY,
				398,
				"IMPORT FROM SPAWNER",
				"Target a built-in ServUO Spawner or a recognized XmlSpawner variant. Valid unique Mobile-derived creature types are added to empty roster slots. Existing entries are preserved.",
				68,
				BlueColor);

			leftY = DrawSection(
				44,
				leftY,
				398,
				"IMPORTED SETTINGS",
				"When available, the importer reads running state, count or cap, minimum and maximum delay, spawn range, Home Range, proximity or trigger range, team, and smart-spawning state.",
				72,
				YellowColor);

			leftY = DrawSection(
				44,
				leftY,
				398,
				"PRESETS",
				"Easy favors small packs and native movement. Medium adds moderate roaming. Hard enables broader roaming and Seek and Destroy. Applying a preset changes the selected profile immediately; use Save Options afterward.",
				78,
				GreenColor);

			DrawSection(
				44,
				leftY,
				398,
				"REQUIRED DEPENDENCY",
				"PlayerAttatchmentExtensions by Kamras is required for spawn ownership tracking. XmlSpawner is optional and is only used as an import source when installed.",
				62,
				BlueColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"ABANDONMENT CLEANUP",
				"When Continuous is off and no normal player remains within " + profile.DespawnPlayerRange + " tiles, the " + profile.DespawnMins + "-minute cleanup timer begins. Combat or a nearby player refreshes it.",
				72,
				YellowColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"REGION WIPE",
				"Wipe removes only living, non-player-owned creatures carrying this system's matching source-region attachment. It does not delete built-in spawners, XmlSpawners, or creatures owned by other systems.",
				76,
				GreenColor);

			rightY = DrawSection(
				462,
				rightY,
				414,
				"OVERLAPPING SPAWN SYSTEMS",
				"Kam Region Spawner does not intentionally conflict with built-in spawners, XmlSpawner, Premium Spawner, or other encounter systems. However, each system still creates its own population.",
				76,
				RedColor);

			DrawSection(
				462,
				rightY,
				414,
				"CAPACITY WARNING",
				"Manage overlapping systems and combined caps carefully. A region with several enabled spawn systems can become overcrowded even when each individual system is functioning correctly.",
				70,
				RedColor);
		}

		private int DrawSection(
			int x,
			int y,
			int width,
			string title,
			string body,
			int bodyHeight,
			string titleColor)
		{
			AddHtmlColor(
				x,
				y,
				width,
				20,
				title,
				titleColor);

			AddHtmlColor(
				x,
				y + 22,
				width,
				bodyHeight,
				body,
				WhiteColor);

			return y + 22 + bodyHeight + 8;
		}

		private static string OnOff(bool value)
		{
			return value ? "Enabled" : "Disabled";
		}

		private static string ShortRegionKey(string regionKey)
		{
			if (String.IsNullOrEmpty(regionKey))
				return "Unknown";

			string display = regionKey.Replace("|", " - ");
			if (display.Length <= 30)
				return display;

			return display.Substring(0, 28) + "..";
		}

		private void AddHtmlColor(
			int x,
			int y,
			int width,
			int height,
			string text,
			string color)
		{
			AddHtml(
				x,
				y,
				width,
				height,
				"<BASEFONT COLOR=" + color + ">" + text,
				false,
				false);
		}

		public override void OnResponse(NetState state, RelayInfo info)
		{
			Mobile from = state.Mobile;
			if (from == null || from.AccessLevel < AccessLevel.Administrator)
				return;

			if (info.ButtonID == 1)
			{
				from.SendGump(new KamRegionSpawnerEditGump(
					m_RegionKey,
					m_ReturnPage,
					m_ReturnSearch,
					m_ReturnFilter,
					m_CreaturePage));
				return;
			}

			if (info.ButtonID >= 100 && info.ButtonID <= 104)
			{
				from.SendGump(new KamRegionSpawnerHelpGump(
					m_RegionKey,
					m_ReturnPage,
					m_ReturnSearch,
					m_ReturnFilter,
					m_CreaturePage,
					info.ButtonID - 100));
			}
		}
	}
}
