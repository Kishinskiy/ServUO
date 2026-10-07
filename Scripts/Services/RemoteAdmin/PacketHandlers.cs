using System;
using System.Collections;
using Server.Accounting;
using Server.Network;

namespace Server.RemoteAdmin
{
    public class RemoteAdminHandlers
    {
        public enum AcctSearchType : byte
        {
            Username = 0,
            IP = 1
        }

        private static readonly OnPacketReceive[] m_Handlers = new OnPacketReceive[256];

        static RemoteAdminHandlers()
        {
            //0x02 = login request, handled by AdminNetwork
            Register(0x04, ServerInfoRequest);
            Register(0x05, AccountSearch);
            Register(0x06, RemoveAccount);
            Register(0x07, UpdateAccount);
        }

        public static void Register(byte command, OnPacketReceive handler)
        {
            m_Handlers[command] = handler;
        }

        public static bool Handle(byte command, NetState state, PacketReader pvSrc)
        {
            if (m_Handlers[command] == null)
            {
                Console.WriteLine("ADMIN: Invalid packet 0x{0:X2} from {1}, disconnecting", command, state);
                return false;
            }

            m_Handlers[command](state, pvSrc);
            return true;
        }

        private static void ServerInfoRequest(NetState state, PacketReader pvSrc)
        {
            state.Send(AdminNetwork.Compress(new ServerInfo()));
        }

        private static void AccountSearch(NetState state, PacketReader pvSrc)
        {
            var type = (AcctSearchType)pvSrc.ReadByte();
            var term = pvSrc.ReadString();

            if (type == AcctSearchType.IP && !Utility.IsValidIP(term))
            {
                state.Send(new MessageBoxMessage("Invalid search term.\nThe IP sent was not valid.", "Invalid IP"));
                return;
            }

            term = term.ToUpper();

            var list = new ArrayList();

            foreach (Account a in Accounts.GetAccounts())
            {
                if (!CanAccessAccount(state.Account, a))
                    continue;

                switch (type)
                {
                    case AcctSearchType.Username:
                    {
                        if (a.Username.ToUpper().IndexOf(term) != -1)
                            list.Add(a);
                        break;
                    }
                    case AcctSearchType.IP:
                    {
                        for (var i = 0; i < a.LoginIPs.Length; i++)
                            if (Utility.IPMatch(term, a.LoginIPs[i]))
                            {
                                list.Add(a);
                                break;
                            }

                        break;
                    }
                }
            }

            if (list.Count > 0)
            {
                if (list.Count <= 25)
                    state.Send(AdminNetwork.Compress(new AccountSearchResults(list)));
                else
                    state.Send(new MessageBoxMessage(
                        "There were more than 25 matches to your search.\nNarrow the search parameters and try again.",
                        "Too Many Results"));
            }
            else
            {
                state.Send(new MessageBoxMessage("There were no results to your search.\nPlease try again.",
                    "No Matches"));
            }
        }

        private static bool CanAccessAccount(IAccount beholder, IAccount beheld)
        {
            return beholder.AccessLevel == AccessLevel.Owner ||
                   beheld.AccessLevel <
                   beholder.AccessLevel; // Cannot see accounts of equal or greater access level unless Owner
        }

        private static void RemoveAccount(NetState state, PacketReader pvSrc)
        {
            if (state.Account.AccessLevel < AccessLevel.Administrator)
            {
                state.Send(new MessageBoxMessage("You do not have permission to delete accounts.",
                    "Account Access Exception"));
                return;
            }

            var a = Accounts.GetAccount(pvSrc.ReadString());

            if (a == null)
            {
                state.Send(new MessageBoxMessage("The account could not be found (and thus was not deleted).",
                    "Account Not Found"));
            }
            else if (!CanAccessAccount(state.Account, a))
            {
                state.Send(new MessageBoxMessage(
                    "You cannot delete an account with an access level greater than or equal to your own.",
                    "Account Access Exception"));
            }
            else if (a == state.Account)
            {
                state.Send(new MessageBoxMessage("You may not delete your own account.", "Not Allowed"));
            }
            else
            {
                RemoteAdminLogging.WriteLine(state, "Deleted Account {0}", a);
                a.Delete();
                state.Send(new MessageBoxMessage("The requested account (and all it's characters) has been deleted.",
                    "Account Deleted"));
            }
        }

        private static void UpdateAccount(NetState state, PacketReader pvSrc)
        {
            if (state.Account.AccessLevel < AccessLevel.Administrator)
            {
                state.Send(new MessageBoxMessage("You do not have permission to edit accounts.",
                    "Account Access Exception"));
                return;
            }

            var username = pvSrc.ReadString();
            var pass = pvSrc.ReadString();

            var a = Accounts.GetAccount(username) as Account;

            if (a != null && !CanAccessAccount(state.Account, a))
            {
                state.Send(new MessageBoxMessage(
                    "You cannot edit an account with an access level greater than or equal to your own.",
                    "Account Access Exception"));
            }
            else
            {
                var CreatedAccount = false;
                var UpdatedPass = false;
                var oldbanned = a == null ? false : a.Banned;
                var oldAcessLevel = a == null ? 0 : a.AccessLevel;

                if (a == null)
                {
                    a = new Account(username, pass);
                    CreatedAccount = true;
                }
                else if (pass != "(hidden)")
                {
                    a.SetPassword(pass);
                    UpdatedPass = true;
                }

                if (a != state.Account)
                {
                    var newAccessLevel = (AccessLevel)pvSrc.ReadByte();
                    if (a.AccessLevel != newAccessLevel)
                    {
                        if (newAccessLevel >= state.Account.AccessLevel)
                            state.Send(new MessageBoxMessage(
                                "Warning: You may not set an access level greater than or equal to your own.",
                                "Account Access Level update denied."));
                        else
                            a.AccessLevel = newAccessLevel;
                    }

                    var newBanned = pvSrc.ReadBoolean();
                    if (newBanned != a.Banned)
                    {
                        oldbanned = a.Banned;
                        a.Banned = newBanned;
                        a.Comments.Add(new AccountComment(state.Account.Username,
                            newBanned ? "Banned via Remote Admin" : "Unbanned via Remote Admin"));
                    }
                }
                else
                {
                    pvSrc.ReadInt16(); //skip both
                    state.Send(new MessageBoxMessage(
                        "Warning: When editing your own account, Account Status and Access Level cannot be changed.",
                        "Editing Own Account"));
                }

                var list = new ArrayList();
                var length = pvSrc.ReadUInt16();
                var invalid = false;
                for (var i = 0; i < length; i++)
                {
                    var add = pvSrc.ReadString();
                    if (Utility.IsValidIP(add))
                        list.Add(add);
                    else
                        invalid = true;
                }

                if (list.Count > 0)
                    a.IPRestrictions = (string[])list.ToArray(typeof(string));
                else
                    a.IPRestrictions = new string[0];

                if (invalid)
                    state.Send(new MessageBoxMessage(
                        "Warning: one or more of the IP Restrictions you specified was not valid.",
                        "Invalid IP Restriction"));

                if (CreatedAccount)
                {
                    RemoteAdminLogging.WriteLine(state, "Created account {0} with Access Level {1}", a.Username,
                        a.AccessLevel);
                }
                else
                {
                    var changes = string.Empty;
                    if (UpdatedPass)
                        changes += " Password Changed.";
                    if (oldAcessLevel != a.AccessLevel)
                        changes = string.Format("{0} Access level changed from {1} to {2}.", changes, oldAcessLevel,
                            a.AccessLevel);
                    if (oldbanned != a.Banned)
                        changes += a.Banned ? " Banned." : " Unbanned.";
                    RemoteAdminLogging.WriteLine(state, "Updated account {0}:{1}", a.Username, changes);
                }

                state.Send(new MessageBoxMessage("Account updated successfully.", "Account Updated"));
            }
        }
    }
}
