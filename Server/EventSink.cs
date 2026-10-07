#region References

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Server.Accounting;
using Server.Commands;
using Server.ContextMenus;
using Server.Guilds;
using Server.Items;
using Server.Network;

#endregion

namespace Server
{
    public delegate void OnItemObtainedEventHandler(OnItemObtainedEventArgs e);

    public delegate void CheckEquipItemEventHandler(CheckEquipItemEventArgs e);

    public delegate void ContextMenuEventHandler(ContextMenuEventArgs e);

    public delegate void WorldBroadcastEventHandler(WorldBroadcastEventArgs e);

    public delegate void CharacterCreatedEventHandler(CharacterCreatedEventArgs e);

    public delegate void OpenDoorMacroEventHandler(OpenDoorMacroEventArgs e);

    public delegate void SpeechEventHandler(SpeechEventArgs e);

    public delegate void LoginEventHandler(LoginEventArgs e);

    public delegate void ServerListEventHandler(ServerListEventArgs e);

    public delegate void MovementEventHandler(MovementEventArgs e);

    public delegate void HungerChangedEventHandler(HungerChangedEventArgs e);

    public delegate void CrashedEventHandler(CrashedEventArgs e);

    public delegate void ShutdownEventHandler(ShutdownEventArgs e);

    public delegate void HelpRequestEventHandler(HelpRequestEventArgs e);

    public delegate void DisarmRequestEventHandler(DisarmRequestEventArgs e);

    public delegate void StunRequestEventHandler(StunRequestEventArgs e);

    public delegate void OpenSpellbookRequestEventHandler(OpenSpellbookRequestEventArgs e);

    public delegate void CastSpellRequestEventHandler(CastSpellRequestEventArgs e);

    public delegate void BandageTargetRequestEventHandler(BandageTargetRequestEventArgs e);

    public delegate void AnimateRequestEventHandler(AnimateRequestEventArgs e);

    public delegate void LogoutEventHandler(LogoutEventArgs e);

    public delegate void SocketConnectEventHandler(SocketConnectEventArgs e);

    public delegate void ConnectedEventHandler(ConnectedEventArgs e);

    public delegate void DisconnectedEventHandler(DisconnectedEventArgs e);

    public delegate void RenameRequestEventHandler(RenameRequestEventArgs e);

    public delegate void PlayerDeathEventHandler(PlayerDeathEventArgs e);

    public delegate void CreatureDeathEventHandler(CreatureDeathEventArgs e);

    public delegate void VirtueGumpRequestEventHandler(VirtueGumpRequestEventArgs e);

    public delegate void VirtueItemRequestEventHandler(VirtueItemRequestEventArgs e);

    public delegate void VirtueMacroRequestEventHandler(VirtueMacroRequestEventArgs e);

    public delegate void AccountLoginEventHandler(AccountLoginEventArgs e);

    public delegate void PaperdollRequestEventHandler(PaperdollRequestEventArgs e);

    public delegate void ProfileRequestEventHandler(ProfileRequestEventArgs e);

    public delegate void ChangeProfileRequestEventHandler(ChangeProfileRequestEventArgs e);

    public delegate void AggressiveActionEventHandler(AggressiveActionEventArgs e);

    public delegate void GameLoginEventHandler(GameLoginEventArgs e);

    public delegate void DeleteRequestEventHandler(DeleteRequestEventArgs e);

    public delegate void WorldLoadEventHandler();

    public delegate void WorldSaveEventHandler(WorldSaveEventArgs e);

    public delegate void BeforeWorldSaveEventHandler(BeforeWorldSaveEventArgs e);

    public delegate void AfterWorldSaveEventHandler(AfterWorldSaveEventArgs e);

    public delegate void SetAbilityEventHandler(SetAbilityEventArgs e);

    public delegate void FastWalkEventHandler(FastWalkEventArgs e);

    public delegate void ServerStartedEventHandler();

    public delegate void CreateGuildHandler(CreateGuildEventArgs e);

    public delegate void GuildGumpRequestHandler(GuildGumpRequestArgs e);

    public delegate void QuestGumpRequestHandler(QuestGumpRequestArgs e);

    public delegate void ClientVersionReceivedHandler(ClientVersionReceivedArgs e);

    public delegate void ClientTypeReceivedHandler(ClientTypeReceivedArgs e);

    public delegate void OnKilledByEventHandler(OnKilledByEventArgs e);

    public delegate void OnItemUseEventHandler(OnItemUseEventArgs e);

    public delegate void OnEnterRegionEventHandler(OnEnterRegionEventArgs e);

    public delegate void OnConsumeEventHandler(OnConsumeEventArgs e);

    public delegate void OnPropertyChangedEventHandler(OnPropertyChangedEventArgs e);

    public delegate void BODUsedEventHandler(BODUsedEventArgs e);

    public delegate void BODOfferEventHandler(BODOfferEventArgs e);

    public delegate void ResourceHarvestAttemptEventHandler(ResourceHarvestAttemptEventArgs e);

    public delegate void ResourceHarvestSuccessEventHandler(ResourceHarvestSuccessEventArgs e);

    public delegate void CraftSuccessEventHandler(CraftSuccessEventArgs e);

    public delegate void SkillGainEventHandler(SkillGainEventArgs e);

    public delegate void SkillCheckEventHandler(SkillCheckEventArgs e);

    public delegate void SkillCapChangeEventHandler(SkillCapChangeEventArgs e);

    public delegate void StatCapChangeEventHandler(StatCapChangeEventArgs e);

    public delegate void QuestCompleteEventHandler(QuestCompleteEventArgs e);

    public delegate void ItemCreatedEventHandler(ItemCreatedEventArgs e);

    public delegate void ItemDeletedEventHandler(ItemDeletedEventArgs e);

    public delegate void MobileCreatedEventHandler(MobileCreatedEventArgs e);

    public delegate void MobileDeletedEventHandler(MobileDeletedEventArgs e);

    public delegate void TargetedSpellEventHandler(TargetedSpellEventArgs e);

    public delegate void TargetedSkillEventHandler(TargetedSkillEventArgs e);

    public delegate void TargetedItemUseEventHandler(TargetedItemUseEventArgs e);

    public delegate void EquipMacroEventHandler(EquipMacroEventArgs e);

    public delegate void UnequipMacroEventHandler(UnequipMacroEventArgs e);

    public delegate void TargetByResourceMacroEventHandler(TargetByResourceMacroEventArgs e);

    public delegate void JoinGuildEventHandler(JoinGuildEventArgs e);

    public delegate void TameCreatureEventHandler(TameCreatureEventArgs e);

    public delegate void ValidVendorPurchaseEventHandler(ValidVendorPurchaseEventArgs e);

    public delegate void ValidVendorSellEventHandler(ValidVendorSellEventArgs e);

    public delegate void CorpseLootEventHandler(CorpseLootEventArgs e);

    public delegate void RepairItemEventHandler(RepairItemEventArgs e);

    public delegate void AlterItemEventHandler(AlterItemEventArgs e);

    public delegate void PlacePlayerVendorEventHandler(PlacePlayerVendorEventArgs e);

    public delegate void FameChangeEventHandler(FameChangeEventArgs e);

    public delegate void KarmaChangeEventHandler(KarmaChangeEventArgs e);

    public delegate void VirtueLevelChangeEventHandler(VirtueLevelChangeEventArgs e);

    public delegate void PlayerMurderedEventHandler(PlayerMurderedEventArgs e);

    public delegate void AccountGoldChangeEventHandler(AccountGoldChangeEventArgs e);

    public delegate void ContainerDroppedToEventHandler(ContainerDroppedToEventArgs e);

    public delegate void TeleportMovementEventHandler(TeleportMovementEventArgs e);

    public delegate void MultiDesignQueryHandler(MultiDesignQueryEventArgs e);

    public class OnItemObtainedEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public Item Item { get; }

        public OnItemObtainedEventArgs(Mobile from, Item item)
        {
            Mobile = from;
            Item = item;
        }
    }

    public class CheckEquipItemEventArgs : EventArgs
    {
        public Mobile Mobile { get; private set; }
        public Item Item { get; private set; }
        public bool Message { get; private set; }

        public bool Block { get; set; }

        public CheckEquipItemEventArgs(Mobile m, Item item, bool message)
        {
            Mobile = m;
            Item = item;
            Message = message;
        }
    }

    public class ContextMenuEventArgs : EventArgs
    {
        public Mobile Mobile { get; private set; }
        public IEntity Target { get; private set; }
        public List<ContextMenuEntry> Entries { get; private set; }

        public ContextMenuEventArgs(Mobile m, IEntity target, List<ContextMenuEntry> entries)
        {
            Mobile = m;
            Target = target;
            Entries = entries;
        }
    }

    public class WorldBroadcastEventArgs : EventArgs
    {
        public int Hue { get; set; }
        public bool Ascii { get; set; }
        public AccessLevel Access { get; set; }
        public string Text { get; set; }

        public WorldBroadcastEventArgs(int hue, bool ascii, AccessLevel access, string text)
        {
            Hue = hue;
            Ascii = ascii;
            Access = access;
            Text = text;
        }
    }

    public class ClientVersionReceivedArgs : EventArgs
    {
        public NetState State { get; }

        public ClientVersion Version { get; }

        public ClientVersionReceivedArgs(NetState state, ClientVersion cv)
        {
            State = state;
            Version = cv;
        }
    }

    public class ClientTypeReceivedArgs : EventArgs
    {
        public NetState State { get; }

        public ClientTypeReceivedArgs(NetState state)
        {
            State = state;
        }
    }

    public class CreateGuildEventArgs : EventArgs
    {
        public int Id { get; set; }

        public BaseGuild Guild { get; set; }

        public CreateGuildEventArgs(int id)
        {
            Id = id;
        }
    }

    public class GuildGumpRequestArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public GuildGumpRequestArgs(Mobile mobile)
        {
            Mobile = mobile;
        }
    }

    public class QuestGumpRequestArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public QuestGumpRequestArgs(Mobile mobile)
        {
            Mobile = mobile;
        }
    }

    public class SetAbilityEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public int Index { get; }

        public SetAbilityEventArgs(Mobile mobile, int index)
        {
            Mobile = mobile;
            Index = index;
        }
    }

    public class DeleteRequestEventArgs : EventArgs
    {
        public NetState State { get; }

        public int Index { get; }

        public DeleteRequestEventArgs(NetState state, int index)
        {
            State = state;
            Index = index;
        }
    }

    public class GameLoginEventArgs : EventArgs
    {
        public NetState State { get; }

        public string Username { get; }

        public string Password { get; }

        public bool Accepted { get; set; }
        public CityInfo[] CityInfo { get; set; }

        public GameLoginEventArgs(NetState state, string un, string pw)
        {
            State = state;
            Username = un;
            Password = pw;
        }
    }

    public class AggressiveActionEventArgs : EventArgs
    {
        private static readonly Queue<AggressiveActionEventArgs> m_Pool = new Queue<AggressiveActionEventArgs>();

        public Mobile Aggressed { get; private set; }

        public Mobile Aggressor { get; private set; }

        public bool Criminal { get; private set; }

        private AggressiveActionEventArgs(Mobile aggressed, Mobile aggressor, bool criminal)
        {
            Aggressed = aggressed;
            Aggressor = aggressor;
            Criminal = criminal;
        }

        public static AggressiveActionEventArgs Create(Mobile aggressed, Mobile aggressor, bool criminal)
        {
            AggressiveActionEventArgs args;

            if (m_Pool.Count > 0)
            {
                args = m_Pool.Dequeue();

                args.Aggressed = aggressed;
                args.Aggressor = aggressor;
                args.Criminal = criminal;
            }
            else
            {
                args = new AggressiveActionEventArgs(aggressed, aggressor, criminal);
            }

            return args;
        }

        public void Free()
        {
            m_Pool.Enqueue(this);
        }
    }

    public class ProfileRequestEventArgs : EventArgs
    {
        public Mobile Beholder { get; }

        public Mobile Beheld { get; }

        public ProfileRequestEventArgs(Mobile beholder, Mobile beheld)
        {
            Beholder = beholder;
            Beheld = beheld;
        }
    }

    public class ChangeProfileRequestEventArgs : EventArgs
    {
        public Mobile Beholder { get; }

        public Mobile Beheld { get; }

        public string Text { get; }

        public ChangeProfileRequestEventArgs(Mobile beholder, Mobile beheld, string text)
        {
            Beholder = beholder;
            Beheld = beheld;
            Text = text;
        }
    }

    public class PaperdollRequestEventArgs : EventArgs
    {
        public Mobile Beholder { get; }

        public Mobile Beheld { get; }

        public PaperdollRequestEventArgs(Mobile beholder, Mobile beheld)
        {
            Beholder = beholder;
            Beheld = beheld;
        }
    }

    public class AccountLoginEventArgs : EventArgs
    {
        public NetState State { get; }

        public string Username { get; }

        public string Password { get; }

        public bool Accepted { get; set; }
        public ALRReason RejectReason { get; set; }

        public AccountLoginEventArgs(NetState state, string username, string password)
        {
            State = state;
            Username = username;
            Password = password;
            Accepted = true;
        }
    }

    public class VirtueItemRequestEventArgs : EventArgs
    {
        public Mobile Beholder { get; }

        public Mobile Beheld { get; }

        public int GumpID { get; }

        public VirtueItemRequestEventArgs(Mobile beholder, Mobile beheld, int gumpID)
        {
            Beholder = beholder;
            Beheld = beheld;
            GumpID = gumpID;
        }
    }

    public class VirtueGumpRequestEventArgs : EventArgs
    {
        public Mobile Beholder { get; }

        public Mobile Beheld { get; }

        public VirtueGumpRequestEventArgs(Mobile beholder, Mobile beheld)
        {
            Beholder = beholder;
            Beheld = beheld;
        }
    }

    public class VirtueMacroRequestEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public int VirtueID { get; }

        public VirtueMacroRequestEventArgs(Mobile mobile, int virtueID)
        {
            Mobile = mobile;
            VirtueID = virtueID;
        }
    }

    public class PlayerDeathEventArgs : EventArgs
    {
        public Mobile Mobile { get; private set; }
        public Mobile Killer { get; private set; }
        public Container Corpse { get; private set; }

        public PlayerDeathEventArgs(Mobile mobile)
            : this(mobile, mobile.LastKiller, mobile.Corpse)
        {
        }

        public PlayerDeathEventArgs(Mobile mobile, Mobile killer, Container corpse)
        {
            Mobile = mobile;
            Killer = killer;
            Corpse = corpse;
        }
    }

    public class CreatureDeathEventArgs : EventArgs
    {
        public Mobile Creature { get; private set; }
        public Mobile Killer { get; private set; }
        public Container Corpse { get; private set; }

        public List<Item> ForcedLoot { get; private set; }

        public bool PreventDefault { get; set; }
        public bool PreventDelete { get; set; }
        public bool ClearCorpse { get; set; }

        public CreatureDeathEventArgs(Mobile creature)
            : this(creature, creature.LastKiller, creature.Corpse)
        {
        }

        public CreatureDeathEventArgs(Mobile creature, Mobile killer, Container corpse)
        {
            Creature = creature;
            Killer = killer;
            Corpse = corpse;

            ForcedLoot = new List<Item>();
        }

        public void ClearLoot(bool free)
        {
            if (free)
            {
                ForcedLoot.Clear();
                ForcedLoot.TrimExcess();
            }
            else
            {
                ForcedLoot = new List<Item>();
            }
        }
    }

    public class RenameRequestEventArgs : EventArgs
    {
        public Mobile From { get; }

        public Mobile Target { get; }

        public string Name { get; }

        public RenameRequestEventArgs(Mobile from, Mobile target, string name)
        {
            From = from;
            Target = target;
            Name = name;
        }
    }

    public class LogoutEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public LogoutEventArgs(Mobile m)
        {
            Mobile = m;
        }
    }

    public class SocketConnectEventArgs : EventArgs
    {
        public Socket Socket { get; }

        public bool AllowConnection { get; set; }

        public SocketConnectEventArgs(Socket s)
        {
            Socket = s;
            AllowConnection = true;
        }
    }

    public class ConnectedEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public ConnectedEventArgs(Mobile m)
        {
            Mobile = m;
        }
    }

    public class DisconnectedEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public DisconnectedEventArgs(Mobile m)
        {
            Mobile = m;
        }
    }

    public class AnimateRequestEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public string Action { get; }

        public AnimateRequestEventArgs(Mobile m, string action)
        {
            Mobile = m;
            Action = action;
        }
    }

    public class CastSpellRequestEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public Item Spellbook { get; }

        public int SpellID { get; }

        public CastSpellRequestEventArgs(Mobile m, int spellID, Item book)
        {
            Mobile = m;
            Spellbook = book;
            SpellID = spellID;
        }
    }

    public class BandageTargetRequestEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public Item Bandage { get; }

        public Mobile Target { get; }

        public BandageTargetRequestEventArgs(Mobile m, Item bandage, Mobile target)
        {
            Mobile = m;
            Bandage = bandage;
            Target = target;
        }
    }

    public class OpenSpellbookRequestEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public int Type { get; }

        public OpenSpellbookRequestEventArgs(Mobile m, int type)
        {
            Mobile = m;
            Type = type;
        }
    }

    public class StunRequestEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public StunRequestEventArgs(Mobile m)
        {
            Mobile = m;
        }
    }

    public class DisarmRequestEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public DisarmRequestEventArgs(Mobile m)
        {
            Mobile = m;
        }
    }

    public class HelpRequestEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public HelpRequestEventArgs(Mobile m)
        {
            Mobile = m;
        }
    }

    public class ShutdownEventArgs : EventArgs
    {
    }

    public class CrashedEventArgs : EventArgs
    {
        public Exception Exception { get; }

        public bool Close { get; set; }

        public CrashedEventArgs(Exception e)
        {
            Exception = e;
        }
    }

    public class HungerChangedEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public int OldValue { get; }

        public HungerChangedEventArgs(Mobile mobile, int oldValue)
        {
            Mobile = mobile;
            OldValue = oldValue;
        }
    }

    public class MovementEventArgs : EventArgs
    {
        private static readonly Queue<MovementEventArgs> m_Pool = new Queue<MovementEventArgs>();

        public Mobile Mobile { get; private set; }

        public Direction Direction { get; private set; }

        public bool Blocked { get; set; }

        public MovementEventArgs(Mobile mobile, Direction dir)
        {
            Mobile = mobile;
            Direction = dir;
        }

        public static MovementEventArgs Create(Mobile mobile, Direction dir)
        {
            MovementEventArgs args;

            if (m_Pool.Count > 0)
            {
                args = m_Pool.Dequeue();

                args.Mobile = mobile;
                args.Direction = dir;
                args.Blocked = false;
            }
            else
            {
                args = new MovementEventArgs(mobile, dir);
            }

            return args;
        }

        public void Free()
        {
            m_Pool.Enqueue(this);
        }
    }

    public class ServerListEventArgs : EventArgs
    {
        public NetState State { get; }

        public IAccount Account { get; }

        public bool Rejected { get; set; }
        public List<ServerInfo> Servers { get; }

        public ServerListEventArgs(NetState state, IAccount account)
        {
            State = state;
            Account = account;
            Servers = new List<ServerInfo>();
        }

        public void AddServer(string name, IPEndPoint address)
        {
            AddServer(name, 0, TimeZone.CurrentTimeZone, address);
        }

        public void AddServer(string name, int fullPercent, TimeZone tz, IPEndPoint address)
        {
            Servers.Add(new ServerInfo(name, fullPercent, tz, address));
        }
    }

    public struct SkillNameValue
    {
        public SkillName Name { get; }

        public int Value { get; }

        public SkillNameValue(SkillName name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    public class CharacterCreatedEventArgs : EventArgs
    {
        public NetState State { get; }

        public IAccount Account { get; }

        public Mobile Mobile { get; set; }
        public string Name { get; }

        public bool Female { get; }

        public int Hue { get; }

        public int Str { get; }

        public int Dex { get; }

        public int Int { get; }

        public CityInfo City { get; }

        public SkillNameValue[] Skills { get; }

        public int ShirtHue { get; }

        public int PantsHue { get; }

        public int HairID { get; }

        public int HairHue { get; }

        public int BeardID { get; }

        public int BeardHue { get; }

        public int Profession { get; set; }
        public Race Race { get; }

        public int FaceID { get; }

        public int FaceHue { get; }

        public CharacterCreatedEventArgs(
            NetState state,
            IAccount a,
            string name,
            bool female,
            int hue,
            int str,
            int dex,
            int intel,
            CityInfo city,
            SkillNameValue[] skills,
            int shirtHue,
            int pantsHue,
            int hairID,
            int hairHue,
            int beardID,
            int beardHue,
            int profession,
            Race race)
            : this(state, a, name, female, hue, str, dex, intel, city, skills, shirtHue, pantsHue, hairID, hairHue,
                beardID, beardHue, profession, race, 0, 0)
        {
        }

        public CharacterCreatedEventArgs(
            NetState state,
            IAccount a,
            string name,
            bool female,
            int hue,
            int str,
            int dex,
            int intel,
            CityInfo city,
            SkillNameValue[] skills,
            int shirtHue,
            int pantsHue,
            int hairID,
            int hairHue,
            int beardID,
            int beardHue,
            int profession,
            Race race,
            int faceID,
            int faceHue)
        {
            State = state;
            Account = a;
            Name = name;
            Female = female;
            Hue = hue;
            Str = str;
            Dex = dex;
            Int = intel;
            City = city;
            Skills = skills;
            ShirtHue = shirtHue;
            PantsHue = pantsHue;
            HairID = hairID;
            HairHue = hairHue;
            BeardID = beardID;
            BeardHue = beardHue;
            Profession = profession;
            Race = race;
            FaceID = faceID;
            FaceHue = faceHue;
        }
    }

    public class OpenDoorMacroEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public OpenDoorMacroEventArgs(Mobile mobile)
        {
            Mobile = mobile;
        }
    }

    public class SpeechEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public string Speech { get; set; }
        public MessageType Type { get; }

        public int Hue { get; }

        public int[] Keywords { get; }

        public bool Handled { get; set; }
        public bool Blocked { get; set; }

        public SpeechEventArgs(Mobile mobile, string speech, MessageType type, int hue, int[] keywords)
        {
            Mobile = mobile;
            Speech = speech;
            Type = type;
            Hue = hue;
            Keywords = keywords;
        }

        public bool HasKeyword(int keyword)
        {
            for (var i = 0; i < Keywords.Length; ++i)
                if (Keywords[i] == keyword)
                    return true;

            return false;
        }
    }

    public class LoginEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public LoginEventArgs(Mobile mobile)
        {
            Mobile = mobile;
        }
    }

    public class WorldSaveEventArgs : EventArgs
    {
        public bool Message { get; }

        public WorldSaveEventArgs(bool msg)
        {
            Message = msg;
        }
    }

    public class BeforeWorldSaveEventArgs : EventArgs
    {
    }


    public class AfterWorldSaveEventArgs : EventArgs
    {
    }


    public class FastWalkEventArgs : EventArgs
    {
        public NetState NetState { get; }

        public bool Blocked { get; set; }

        public FastWalkEventArgs(NetState state)
        {
            NetState = state;
            Blocked = false;
        }
    }

    public class OnKilledByEventArgs : EventArgs
    {
        public Mobile Killed { get; }

        public Mobile KilledBy { get; }

        public OnKilledByEventArgs(Mobile killed, Mobile killedBy)
        {
            Killed = killed;
            KilledBy = killedBy;
        }
    }

    public class OnItemUseEventArgs : EventArgs
    {
        public Mobile From { get; }

        public Item Item { get; }

        public OnItemUseEventArgs(Mobile from, Item item)
        {
            From = from;
            Item = item;
        }
    }

    public class OnEnterRegionEventArgs : EventArgs
    {
        public Mobile From { get; }

        public Region OldRegion { get; }

        public Region NewRegion { get; }

        public OnEnterRegionEventArgs(Mobile from, Region oldRegion, Region newRegion)
        {
            From = from;
            OldRegion = oldRegion;
            NewRegion = newRegion;
        }
    }

    public class OnConsumeEventArgs : EventArgs
    {
        public Mobile Consumer { get; }

        public Item Consumed { get; }

        public int Quantity { get; }

        public OnConsumeEventArgs(Mobile consumer, Item consumed)
            : this(consumer, consumed, 1)
        {
        }

        public OnConsumeEventArgs(Mobile consumer, Item consumed, int quantity)
        {
            Consumer = consumer;
            Consumed = consumed;
            Quantity = quantity;
        }
    }

    public class OnPropertyChangedEventArgs : EventArgs
    {
        public Mobile Mobile { get; private set; }
        public PropertyInfo Property { get; private set; }
        public object Instance { get; private set; }
        public object OldValue { get; private set; }
        public object NewValue { get; private set; }

        public OnPropertyChangedEventArgs(Mobile m, object instance, PropertyInfo prop, object oldValue,
            object newValue)
        {
            Mobile = m;
            Property = prop;
            Instance = instance;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public class BODUsedEventArgs : EventArgs
    {
        public Mobile User { get; private set; }
        public Item BODItem { get; private set; }

        public BODUsedEventArgs(Mobile m, Item i)
        {
            User = m;
            BODItem = i;
        }
    }

    public class BODOfferEventArgs : EventArgs
    {
        public Mobile Player { get; private set; }
        public Mobile Vendor { get; private set; }

        public BODOfferEventArgs(Mobile p, Mobile v)
        {
            Player = p;
            Vendor = v;
        }
    }

    public class ResourceHarvestAttemptEventArgs : EventArgs
    {
        public Mobile Harvester { get; private set; }
        public Item Tool { get; private set; }
        public object HarvestSystem { get; private set; }

        public ResourceHarvestAttemptEventArgs(Mobile m, Item i, object o)
        {
            Harvester = m;
            Tool = i;
            HarvestSystem = o;
        }
    }

    public class ResourceHarvestSuccessEventArgs : EventArgs
    {
        public Mobile Harvester { get; private set; }
        public Item Tool { get; private set; }
        public Item Resource { get; private set; }
        public Item BonusResource { get; private set; }
        public object HarvestSystem { get; private set; }

        public ResourceHarvestSuccessEventArgs(Mobile m, Item i, Item r, Item b, object o)
        {
            Harvester = m;
            Tool = i;
            Resource = r;
            BonusResource = b;
            HarvestSystem = o;
        }
    }

    public class CraftSuccessEventArgs : EventArgs
    {
        public Mobile Crafter { get; private set; }
        public Item Tool { get; private set; }
        public Item CraftedItem { get; private set; }

        public CraftSuccessEventArgs(Mobile m, Item i, Item t)
        {
            Crafter = m;
            Tool = t;
            CraftedItem = i;
        }
    }

    public class SkillGainEventArgs : EventArgs
    {
        public int Gained { get; private set; }
        public Mobile From { get; private set; }
        public Skill Skill { get; private set; }

        public SkillGainEventArgs(Mobile from, Skill skill, int toGain)
        {
            From = from;
            Skill = skill;
            Gained = toGain;
        }
    }

    public class SkillCheckEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public Mobile From { get; set; }
        public Skill Skill { get; set; }

        public SkillCheckEventArgs(Mobile from, Skill skill, bool success)
        {
            From = from;
            Skill = skill;
            Success = success;
        }
    }

    public class SkillCapChangeEventArgs : EventArgs
    {
        public Mobile Mobile { get; private set; }
        public Skill Skill { get; private set; }
        public double OldCap { get; private set; }
        public double NewCap { get; private set; }

        public SkillCapChangeEventArgs(Mobile from, Skill skill, double oldCap, double newCap)
        {
            Mobile = from;
            Skill = skill;
            OldCap = oldCap;
            NewCap = newCap;
        }
    }

    public class StatCapChangeEventArgs : EventArgs
    {
        public Mobile Mobile { get; private set; }
        public int OldCap { get; private set; }
        public int NewCap { get; private set; }

        public StatCapChangeEventArgs(Mobile from, int oldCap, int newCap)
        {
            Mobile = from;
            OldCap = oldCap;
            NewCap = newCap;
        }
    }

    public class QuestCompleteEventArgs : EventArgs
    {
        public Type QuestType { get; private set; }
        public Mobile Mobile { get; private set; }

        public QuestCompleteEventArgs(Mobile from, Type type)
        {
            Mobile = from;
            QuestType = type;
        }
    }

    public class ItemCreatedEventArgs : EventArgs
    {
        public Item Item { get; set; }

        public ItemCreatedEventArgs(Item item)
        {
            Item = item;
        }
    }

    public class ItemDeletedEventArgs : EventArgs
    {
        public Item Item { get; set; }

        public ItemDeletedEventArgs(Item item)
        {
            Item = item;
        }
    }

    public class MobileCreatedEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }

        public MobileCreatedEventArgs(Mobile mobile)
        {
            Mobile = mobile;
        }
    }

    public class MobileDeletedEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }

        public MobileDeletedEventArgs(Mobile mobile)
        {
            Mobile = mobile;
        }
    }

    public class TargetedSpellEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public IEntity Target { get; }

        public short SpellID { get; }

        public TargetedSpellEventArgs(Mobile m, IEntity target, short spellID)
        {
            Mobile = m;
            Target = target;
            SpellID = spellID;
        }
    }

    public class TargetedSkillEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public IEntity Target { get; }

        public short SkillID { get; }

        public TargetedSkillEventArgs(Mobile m, IEntity target, short skillID)
        {
            Mobile = m;
            Target = target;
            SkillID = skillID;
        }
    }

    public class TargetedItemUseEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public IEntity Source { get; }

        public IEntity Target { get; }

        public TargetedItemUseEventArgs(Mobile mobile, IEntity src, IEntity target)
        {
            Mobile = mobile;
            Source = src;
            Target = target;
        }
    }

    public class TargetByResourceMacroEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public Item Tool { get; }

        public int ResourceType { get; }

        public TargetByResourceMacroEventArgs(Mobile mobile, Item tool, int type)
        {
            Mobile = mobile;
            Tool = tool;
            ResourceType = type;
        }
    }

    public class EquipMacroEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public List<int> List { get; }

        public EquipMacroEventArgs(Mobile mobile, List<int> list)
        {
            Mobile = mobile;
            List = list;
        }
    }

    public class UnequipMacroEventArgs : EventArgs
    {
        public Mobile Mobile { get; }

        public List<int> List { get; }

        public UnequipMacroEventArgs(Mobile mobile, List<int> list)
        {
            Mobile = mobile;
            List = list;
        }
    }

    public class JoinGuildEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }
        public BaseGuild Guild { get; set; }

        public JoinGuildEventArgs(Mobile m, BaseGuild g)
        {
            Mobile = m;
            Guild = g;
        }
    }

    public class TameCreatureEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }
        public Mobile Creature { get; set; }

        public TameCreatureEventArgs(Mobile m, Mobile creature)
        {
            Mobile = m;
            Creature = creature;
        }
    }

    public class ValidVendorPurchaseEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }
        public Mobile Vendor { get; set; }
        public IEntity Bought { get; set; }
        public int AmountPerUnit { get; set; }

        public ValidVendorPurchaseEventArgs(Mobile m, Mobile vendor, IEntity bought, int costPer)
        {
            Mobile = m;
            Vendor = vendor;
            Bought = bought;
            AmountPerUnit = costPer;
        }
    }

    public class ValidVendorSellEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }
        public Mobile Vendor { get; set; }
        public IEntity Sold { get; set; }
        public int AmountPerUnit { get; set; }

        public ValidVendorSellEventArgs(Mobile m, Mobile vendor, IEntity sold, int costPer)
        {
            Mobile = m;
            Vendor = vendor;
            Sold = sold;
            AmountPerUnit = costPer;
        }
    }

    public class CorpseLootEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }
        public Container Corpse { get; set; }
        public Item Looted { get; set; }

        public CorpseLootEventArgs(Mobile m, Container c, Item looted)
        {
            Mobile = m;
            Corpse = c;
            Looted = looted;
        }
    }

    public class RepairItemEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }
        public Item Tool { get; set; }
        public IEntity Repaired { get; set; }

        public RepairItemEventArgs(Mobile m, Item tool, IEntity repaired)
        {
            Mobile = m;
            Tool = tool;
            Repaired = repaired;
        }
    }

    public class AlterItemEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }
        public Item Tool { get; set; }
        public Item OldItem { get; set; }
        public Item NewItem { get; set; }

        public AlterItemEventArgs(Mobile m, Item tool, Item oldItem, Item newItem)
        {
            Mobile = m;
            Tool = tool;
            OldItem = oldItem;
            NewItem = newItem;
        }
    }

    public class PlacePlayerVendorEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }
        public Mobile Vendor { get; set; }

        public PlacePlayerVendorEventArgs(Mobile m, Mobile vendor)
        {
            Mobile = m;
            Vendor = vendor;
        }
    }

    public class FameChangeEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }
        public int OldValue { get; set; }
        public int NewValue { get; set; }

        public FameChangeEventArgs(Mobile m, int oldValue, int newValue)
        {
            Mobile = m;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public class KarmaChangeEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }
        public int OldValue { get; set; }
        public int NewValue { get; set; }

        public KarmaChangeEventArgs(Mobile m, int oldValue, int newValue)
        {
            Mobile = m;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public class VirtueLevelChangeEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }
        public int Virtue { get; set; }

        public VirtueLevelChangeEventArgs(Mobile m, int oldLevel, int newLevel, int virtue)
        {
            Mobile = m;
            OldLevel = oldLevel;
            NewLevel = newLevel;
            Virtue = virtue;
        }
    }

    public class PlayerMurderedEventArgs : EventArgs
    {
        public Mobile Murderer { get; set; }
        public Mobile Victim { get; set; }

        public PlayerMurderedEventArgs(Mobile murderer, Mobile victim)
        {
            Murderer = murderer;
            Victim = victim;
        }
    }

    public class AccountGoldChangeEventArgs : EventArgs
    {
        public IAccount Account { get; set; }
        public double OldAmount { get; set; }
        public double NewAmount { get; set; }

        public AccountGoldChangeEventArgs(IAccount account, double oldAmount, double newAmount)
        {
            Account = account;
            OldAmount = oldAmount;
            NewAmount = newAmount;
        }
    }

    public class ContainerDroppedToEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }
        public Container Container { get; set; }
        public Item Dropped { get; set; }

        public ContainerDroppedToEventArgs(Mobile m, Container container, Item dropped)
        {
            Mobile = m;
            Container = container;
            Dropped = dropped;
        }
    }

    public class TeleportMovementEventArgs : EventArgs
    {
        public Mobile Mobile { get; set; }
        public Point3D OldLocation { get; set; }
        public Point3D NewLocation { get; set; }

        public TeleportMovementEventArgs(Mobile m, Point3D oldLoc, Point3D newLoc)
        {
            Mobile = m;
            OldLocation = oldLoc;
            NewLocation = newLoc;
        }
    }

    public class MultiDesignQueryEventArgs : EventArgs
    {
        public NetState State { get; set; }
        public BaseMulti Multi { get; set; }

        public MultiDesignQueryEventArgs(NetState state, BaseMulti multi)
        {
            State = state;
            Multi = multi;
        }
    }

    public static class EventSink
    {
        public static event OnItemObtainedEventHandler OnItemObtained;
        public static event CheckEquipItemEventHandler CheckEquipItem;
        public static event ContextMenuEventHandler ContextMenu;
        public static event WorldBroadcastEventHandler WorldBroadcast;
        public static event CharacterCreatedEventHandler CharacterCreated;
        public static event OpenDoorMacroEventHandler OpenDoorMacroUsed;
        public static event SpeechEventHandler Speech;
        public static event LoginEventHandler Login;
        public static event ServerListEventHandler ServerList;
        public static event MovementEventHandler Movement;
        public static event HungerChangedEventHandler HungerChanged;
        public static event CrashedEventHandler Crashed;
        public static event ShutdownEventHandler Shutdown;
        public static event HelpRequestEventHandler HelpRequest;
        public static event DisarmRequestEventHandler DisarmRequest;
        public static event StunRequestEventHandler StunRequest;
        public static event OpenSpellbookRequestEventHandler OpenSpellbookRequest;
        public static event CastSpellRequestEventHandler CastSpellRequest;
        public static event BandageTargetRequestEventHandler BandageTargetRequest;
        public static event AnimateRequestEventHandler AnimateRequest;
        public static event LogoutEventHandler Logout;
        public static event SocketConnectEventHandler SocketConnect;
        public static event ConnectedEventHandler Connected;
        public static event DisconnectedEventHandler Disconnected;
        public static event RenameRequestEventHandler RenameRequest;
        public static event PlayerDeathEventHandler PlayerDeath;
        public static event CreatureDeathEventHandler CreatureDeath;
        public static event VirtueGumpRequestEventHandler VirtueGumpRequest;
        public static event VirtueItemRequestEventHandler VirtueItemRequest;
        public static event VirtueMacroRequestEventHandler VirtueMacroRequest;
        public static event AccountLoginEventHandler AccountLogin;
        public static event PaperdollRequestEventHandler PaperdollRequest;
        public static event ProfileRequestEventHandler ProfileRequest;
        public static event ChangeProfileRequestEventHandler ChangeProfileRequest;
        public static event AggressiveActionEventHandler AggressiveAction;
        public static event CommandEventHandler Command;
        public static event GameLoginEventHandler GameLogin;
        public static event DeleteRequestEventHandler DeleteRequest;
        public static event WorldLoadEventHandler WorldLoad;
        public static event WorldSaveEventHandler WorldSave;
        public static event BeforeWorldSaveEventHandler BeforeWorldSave;
        public static event AfterWorldSaveEventHandler AfterWorldSave;
        public static event SetAbilityEventHandler SetAbility;
        public static event FastWalkEventHandler FastWalk;
        public static event CreateGuildHandler CreateGuild;
        public static event ServerStartedEventHandler ServerStarted;
        public static event GuildGumpRequestHandler GuildGumpRequest;
        public static event QuestGumpRequestHandler QuestGumpRequest;
        public static event ClientVersionReceivedHandler ClientVersionReceived;
        public static event ClientTypeReceivedHandler ClientTypeReceived;
        public static event OnKilledByEventHandler OnKilledBy;
        public static event OnItemUseEventHandler OnItemUse;
        public static event OnEnterRegionEventHandler OnEnterRegion;
        public static event OnConsumeEventHandler OnConsume;
        public static event OnPropertyChangedEventHandler OnPropertyChanged;
        public static event BODUsedEventHandler BODUsed;
        public static event BODOfferEventHandler BODOffered;
        public static event ResourceHarvestAttemptEventHandler ResourceHarvestAttempt;
        public static event ResourceHarvestSuccessEventHandler ResourceHarvestSuccess;
        public static event CraftSuccessEventHandler CraftSuccess;
        public static event SkillGainEventHandler SkillGain;
        public static event SkillCheckEventHandler SkillCheck;
        public static event SkillCapChangeEventHandler SkillCapChange;
        public static event StatCapChangeEventHandler StatCapChange;
        public static event QuestCompleteEventHandler QuestComplete;

        public static event ItemCreatedEventHandler ItemCreated;
        public static event ItemDeletedEventHandler ItemDeleted;
        public static event MobileCreatedEventHandler MobileCreated;
        public static event MobileDeletedEventHandler MobileDeleted;

        public static event TargetedSpellEventHandler TargetedSpell;
        public static event TargetedSkillEventHandler TargetedSkill;
        public static event TargetedItemUseEventHandler TargetedItemUse;
        public static event EquipMacroEventHandler EquipMacro;
        public static event UnequipMacroEventHandler UnequipMacro;
        public static event TargetByResourceMacroEventHandler TargetByResourceMacro;

        public static event JoinGuildEventHandler JoinGuild;
        public static event TameCreatureEventHandler TameCreature;
        public static event ValidVendorPurchaseEventHandler ValidVendorPurchase;
        public static event ValidVendorSellEventHandler ValidVendorSell;
        public static event CorpseLootEventHandler CorpseLoot;
        public static event RepairItemEventHandler RepairItem;
        public static event AlterItemEventHandler AlterItem;
        public static event PlacePlayerVendorEventHandler PlacePlayerVendor;
        public static event FameChangeEventHandler FameChange;
        public static event KarmaChangeEventHandler KarmaChange;
        public static event VirtueLevelChangeEventHandler VirtueLevelChange;
        public static event PlayerMurderedEventHandler PlayerMurdered;
        public static event AccountGoldChangeEventHandler AccountGoldChange;
        public static event ContainerDroppedToEventHandler ContainerDroppedTo;
        public static event TeleportMovementEventHandler TeleportMovement;
        public static event MultiDesignQueryHandler MultiDesign;

        public static void InvokeOnItemObtained(OnItemObtainedEventArgs e)
        {
            if (OnItemObtained != null) OnItemObtained(e);
        }

        public static void InvokeCheckEquipItem(CheckEquipItemEventArgs e)
        {
            if (CheckEquipItem != null) CheckEquipItem(e);
        }

        public static void InvokeContextMenu(ContextMenuEventArgs e)
        {
            if (ContextMenu != null) ContextMenu(e);
        }

        public static void InvokeWorldBroadcast(WorldBroadcastEventArgs e)
        {
            if (WorldBroadcast != null) WorldBroadcast(e);
        }

        public static void InvokeClientVersionReceived(ClientVersionReceivedArgs e)
        {
            if (ClientVersionReceived != null) ClientVersionReceived(e);
        }

        public static void InvokeClientTypeReceived(ClientTypeReceivedArgs e)
        {
            if (ClientTypeReceived != null) ClientTypeReceived(e);
        }

        public static void InvokeServerStarted()
        {
            if (ServerStarted != null) ServerStarted();
        }

        public static void InvokeCreateGuild(CreateGuildEventArgs e)
        {
            if (CreateGuild != null) CreateGuild(e);
        }

        public static void InvokeSetAbility(SetAbilityEventArgs e)
        {
            if (SetAbility != null) SetAbility(e);
        }

        public static void InvokeGuildGumpRequest(GuildGumpRequestArgs e)
        {
            if (GuildGumpRequest != null) GuildGumpRequest(e);
        }

        public static void InvokeQuestGumpRequest(QuestGumpRequestArgs e)
        {
            if (QuestGumpRequest != null) QuestGumpRequest(e);
        }

        public static void InvokeFastWalk(FastWalkEventArgs e)
        {
            if (FastWalk != null) FastWalk(e);
        }

        public static void InvokeDeleteRequest(DeleteRequestEventArgs e)
        {
            if (DeleteRequest != null) DeleteRequest(e);
        }

        public static void InvokeGameLogin(GameLoginEventArgs e)
        {
            if (GameLogin != null) GameLogin(e);
        }

        public static void InvokeCommand(CommandEventArgs e)
        {
            if (Command != null) Command(e);
        }

        public static void InvokeAggressiveAction(AggressiveActionEventArgs e)
        {
            if (AggressiveAction != null) AggressiveAction(e);
        }

        public static void InvokeProfileRequest(ProfileRequestEventArgs e)
        {
            if (ProfileRequest != null) ProfileRequest(e);
        }

        public static void InvokeChangeProfileRequest(ChangeProfileRequestEventArgs e)
        {
            if (ChangeProfileRequest != null) ChangeProfileRequest(e);
        }

        public static void InvokePaperdollRequest(PaperdollRequestEventArgs e)
        {
            if (PaperdollRequest != null) PaperdollRequest(e);
        }

        public static void InvokeAccountLogin(AccountLoginEventArgs e)
        {
            if (AccountLogin != null) AccountLogin(e);
        }

        public static void InvokeVirtueItemRequest(VirtueItemRequestEventArgs e)
        {
            if (VirtueItemRequest != null) VirtueItemRequest(e);
        }

        public static void InvokeVirtueGumpRequest(VirtueGumpRequestEventArgs e)
        {
            if (VirtueGumpRequest != null) VirtueGumpRequest(e);
        }

        public static void InvokeVirtueMacroRequest(VirtueMacroRequestEventArgs e)
        {
            if (VirtueMacroRequest != null) VirtueMacroRequest(e);
        }

        public static void InvokePlayerDeath(PlayerDeathEventArgs e)
        {
            if (PlayerDeath != null) PlayerDeath(e);
        }

        public static void InvokeCreatureDeath(CreatureDeathEventArgs e)
        {
            if (CreatureDeath != null) CreatureDeath(e);
        }

        public static void InvokeRenameRequest(RenameRequestEventArgs e)
        {
            if (RenameRequest != null) RenameRequest(e);
        }

        public static void InvokeLogout(LogoutEventArgs e)
        {
            if (Logout != null) Logout(e);
        }

        public static void InvokeSocketConnect(SocketConnectEventArgs e)
        {
            if (SocketConnect != null) SocketConnect(e);
        }

        public static void InvokeConnected(ConnectedEventArgs e)
        {
            if (Connected != null) Connected(e);
        }

        public static void InvokeDisconnected(DisconnectedEventArgs e)
        {
            if (Disconnected != null) Disconnected(e);
        }

        public static void InvokeAnimateRequest(AnimateRequestEventArgs e)
        {
            if (AnimateRequest != null) AnimateRequest(e);
        }

        public static void InvokeCastSpellRequest(CastSpellRequestEventArgs e)
        {
            if (CastSpellRequest != null) CastSpellRequest(e);
        }

        public static void InvokeBandageTargetRequest(BandageTargetRequestEventArgs e)
        {
            if (BandageTargetRequest != null) BandageTargetRequest(e);
        }

        public static void InvokeOpenSpellbookRequest(OpenSpellbookRequestEventArgs e)
        {
            if (OpenSpellbookRequest != null) OpenSpellbookRequest(e);
        }

        public static void InvokeDisarmRequest(DisarmRequestEventArgs e)
        {
            if (DisarmRequest != null) DisarmRequest(e);
        }

        public static void InvokeStunRequest(StunRequestEventArgs e)
        {
            if (StunRequest != null) StunRequest(e);
        }

        public static void InvokeHelpRequest(HelpRequestEventArgs e)
        {
            if (HelpRequest != null) HelpRequest(e);
        }

        public static void InvokeShutdown(ShutdownEventArgs e)
        {
            if (Shutdown != null) Shutdown(e);
        }

        public static void InvokeCrashed(CrashedEventArgs e)
        {
            if (Crashed != null) Crashed(e);
        }

        public static void InvokeHungerChanged(HungerChangedEventArgs e)
        {
            if (HungerChanged != null) HungerChanged(e);
        }

        public static void InvokeMovement(MovementEventArgs e)
        {
            if (Movement != null) Movement(e);
        }

        public static void InvokeServerList(ServerListEventArgs e)
        {
            if (ServerList != null) ServerList(e);
        }

        public static void InvokeLogin(LoginEventArgs e)
        {
            if (Login != null) Login(e);
        }

        public static void InvokeSpeech(SpeechEventArgs e)
        {
            if (Speech != null) Speech(e);
        }

        public static void InvokeCharacterCreated(CharacterCreatedEventArgs e)
        {
            if (CharacterCreated != null) CharacterCreated(e);
        }

        public static void InvokeOpenDoorMacroUsed(OpenDoorMacroEventArgs e)
        {
            if (OpenDoorMacroUsed != null) OpenDoorMacroUsed(e);
        }

        public static void InvokeWorldLoad()
        {
            if (WorldLoad != null) WorldLoad();
        }

        public static void InvokeWorldSave(WorldSaveEventArgs e)
        {
            if (WorldSave != null) WorldSave(e);
        }

        public static void InvokeBeforeWorldSave(BeforeWorldSaveEventArgs e)
        {
            if (BeforeWorldSave != null) BeforeWorldSave(e);
        }

        public static void InvokeAfterWorldSave(AfterWorldSaveEventArgs e)
        {
            if (AfterWorldSave != null) AfterWorldSave(e);
        }

        public static void InvokeOnKilledBy(OnKilledByEventArgs e)
        {
            if (OnKilledBy != null) OnKilledBy(e);
        }

        public static void InvokeOnItemUse(OnItemUseEventArgs e)
        {
            if (OnItemUse != null) OnItemUse(e);
        }

        public static void InvokeOnEnterRegion(OnEnterRegionEventArgs e)
        {
            if (OnEnterRegion != null) OnEnterRegion(e);
        }

        public static void InvokeOnConsume(OnConsumeEventArgs e)
        {
            if (OnConsume != null) OnConsume(e);
        }

        public static void InvokeOnPropertyChanged(OnPropertyChangedEventArgs e)
        {
            if (OnPropertyChanged != null) OnPropertyChanged(e);
        }

        public static void InvokeBODUsed(BODUsedEventArgs e)
        {
            if (BODUsed != null) BODUsed(e);
        }

        public static void InvokeBODOffered(BODOfferEventArgs e)
        {
            if (BODOffered != null) BODOffered(e);
        }

        public static void InvokeResourceHarvestAttempt(ResourceHarvestAttemptEventArgs e)
        {
            if (ResourceHarvestAttempt != null) ResourceHarvestAttempt(e);
        }

        public static void InvokeResourceHarvestSuccess(ResourceHarvestSuccessEventArgs e)
        {
            if (ResourceHarvestSuccess != null) ResourceHarvestSuccess(e);
        }

        public static void InvokeCraftSuccess(CraftSuccessEventArgs e)
        {
            if (CraftSuccess != null) CraftSuccess(e);
        }

        public static void InvokeSkillGain(SkillGainEventArgs e)
        {
            if (SkillGain != null) SkillGain(e);
        }

        public static void InvokeSkillCheck(SkillCheckEventArgs e)
        {
            if (SkillCheck != null) SkillCheck(e);
        }

        public static void InvokeSkillCapChange(SkillCapChangeEventArgs e)
        {
            if (SkillCapChange != null) SkillCapChange(e);
        }

        public static void InvokeStatCapChange(StatCapChangeEventArgs e)
        {
            if (StatCapChange != null) StatCapChange(e);
        }

        public static void InvokeQuestComplete(QuestCompleteEventArgs e)
        {
            if (QuestComplete != null) QuestComplete(e);
        }

        public static void InvokeItemCreated(ItemCreatedEventArgs e)
        {
            if (ItemCreated != null) ItemCreated(e);
        }

        public static void InvokeItemDeleted(ItemDeletedEventArgs e)
        {
            if (ItemDeleted != null) ItemDeleted(e);
        }

        public static void InvokeMobileCreated(MobileCreatedEventArgs e)
        {
            if (MobileCreated != null) MobileCreated(e);
        }

        public static void InvokeMobileDeleted(MobileDeletedEventArgs e)
        {
            if (MobileDeleted != null) MobileDeleted(e);
        }

        public static void InvokeTargetedSpell(TargetedSpellEventArgs e)
        {
            if (TargetedSpell != null) TargetedSpell(e);
        }

        public static void InvokeTargetedSkill(TargetedSkillEventArgs e)
        {
            if (TargetedSkill != null) TargetedSkill(e);
        }

        public static void InvokeTargetedItemUse(TargetedItemUseEventArgs e)
        {
            if (TargetedItemUse != null) TargetedItemUse(e);
        }

        public static void InvokeTargetByResourceMacro(TargetByResourceMacroEventArgs e)
        {
            if (TargetByResourceMacro != null) TargetByResourceMacro(e);
        }

        public static void InvokeEquipMacro(EquipMacroEventArgs e)
        {
            if (EquipMacro != null) EquipMacro(e);
        }

        public static void InvokeUnequipMacro(UnequipMacroEventArgs e)
        {
            if (UnequipMacro != null) UnequipMacro(e);
        }

        public static void InvokeJoinGuild(JoinGuildEventArgs e)
        {
            if (JoinGuild != null) JoinGuild(e);
        }

        public static void InvokeTameCreature(TameCreatureEventArgs e)
        {
            if (TameCreature != null) TameCreature(e);
        }

        public static void InvokeValidVendorPurchase(ValidVendorPurchaseEventArgs e)
        {
            if (ValidVendorPurchase != null) ValidVendorPurchase(e);
        }

        public static void InvokeValidVendorSell(ValidVendorSellEventArgs e)
        {
            if (ValidVendorSell != null) ValidVendorSell(e);
        }

        public static void InvokeCorpseLoot(CorpseLootEventArgs e)
        {
            if (CorpseLoot != null) CorpseLoot(e);
        }

        public static void InvokeRepairItem(RepairItemEventArgs e)
        {
            if (RepairItem != null) RepairItem(e);
        }

        public static void InvokeAlterItem(AlterItemEventArgs e)
        {
            if (AlterItem != null) AlterItem(e);
        }

        public static void InvokePlacePlayerVendor(PlacePlayerVendorEventArgs e)
        {
            if (PlacePlayerVendor != null) PlacePlayerVendor(e);
        }

        public static void InvokeFameChange(FameChangeEventArgs e)
        {
            if (FameChange != null) FameChange(e);
        }

        public static void InvokeKarmaChange(KarmaChangeEventArgs e)
        {
            if (KarmaChange != null) KarmaChange(e);
        }

        public static void InvokeVirtueLevelChange(VirtueLevelChangeEventArgs e)
        {
            if (VirtueLevelChange != null) VirtueLevelChange(e);
        }

        public static void InvokePlayerMurdered(PlayerMurderedEventArgs e)
        {
            if (PlayerMurdered != null) PlayerMurdered(e);
        }

        public static void InvokeAccountGoldChange(AccountGoldChangeEventArgs e)
        {
            if (AccountGoldChange != null) AccountGoldChange(e);
        }

        public static void InvokeContainerDroppedTo(ContainerDroppedToEventArgs e)
        {
            if (ContainerDroppedTo != null) ContainerDroppedTo(e);
        }

        public static void InvokeTeleportMovement(TeleportMovementEventArgs e)
        {
            if (TeleportMovement != null) TeleportMovement(e);
        }

        public static void InvokeMultiDesignQuery(MultiDesignQueryEventArgs e)
        {
            if (MultiDesign != null) MultiDesign(e);
        }

        public static void Reset()
        {
            OnItemObtained = null;
            CheckEquipItem = null;
            ContextMenu = null;
            WorldBroadcast = null;
            CharacterCreated = null;
            OpenDoorMacroUsed = null;
            Speech = null;
            Login = null;
            ServerList = null;
            Movement = null;
            HungerChanged = null;
            Crashed = null;
            Shutdown = null;
            HelpRequest = null;
            DisarmRequest = null;
            StunRequest = null;
            OpenSpellbookRequest = null;
            CastSpellRequest = null;
            BandageTargetRequest = null;
            AnimateRequest = null;
            Logout = null;
            SocketConnect = null;
            Connected = null;
            Disconnected = null;
            RenameRequest = null;
            PlayerDeath = null;
            CreatureDeath = null;
            VirtueGumpRequest = null;
            VirtueItemRequest = null;
            VirtueMacroRequest = null;
            AccountLogin = null;
            PaperdollRequest = null;
            ProfileRequest = null;
            ChangeProfileRequest = null;
            AggressiveAction = null;
            Command = null;
            GameLogin = null;
            DeleteRequest = null;
            WorldLoad = null;
            WorldSave = null;
            SetAbility = null;
            GuildGumpRequest = null;
            QuestGumpRequest = null;
            OnKilledBy = null;
            OnItemUse = null;
            OnEnterRegion = null;
            OnConsume = null;
            OnPropertyChanged = null;
            BODUsed = null;
            BODOffered = null;
            ResourceHarvestAttempt = null;
            ResourceHarvestSuccess = null;
            CraftSuccess = null;
            SkillGain = null;
            SkillCheck = null;

            ItemCreated = null;
            ItemDeleted = null;
            MobileCreated = null;
            MobileDeleted = null;

            TargetedSpell = null;
            TargetedSkill = null;
            TargetedItemUse = null;

            JoinGuild = null;
            TameCreature = null;
            ValidVendorPurchase = null;
            ValidVendorSell = null;
            CorpseLoot = null;
            RepairItem = null;
            AlterItem = null;
            PlacePlayerVendor = null;
            FameChange = null;
            KarmaChange = null;
            VirtueLevelChange = null;
            PlayerMurdered = null;
            AccountGoldChange = null;
            ContainerDroppedTo = null;
            TeleportMovement = null;

            MultiDesign = null;
        }
    }
}
