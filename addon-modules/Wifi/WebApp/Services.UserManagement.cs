using System;
using System.Collections.Generic;

using log4net;
using OpenMetaverse;

using OpenSim.Services.Interfaces;

namespace Diva.Wifi
{
    public partial class Services
    {
        public string UserManagementGetRequest(Environment env)
        {
            m_log.DebugFormat("[WebApp]: UserManagementGetRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) && (sinfo.Account.UserLevel >= 200))
            {
                env.Session = sinfo;
                env.Flags = Flags.IsLoggedIn | Flags.IsAdmin;
                env.State = State.UserSearchForm;
                env.Data = GetUserList(env, "*pending*");
                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            else
            {
                return m_WebApp.ReadFile(env, "index.html");
            }
        }

        public string UserSearchPostRequest(Environment env, string terms)
        {
            m_log.DebugFormat("[WebApp]: UserSearchPostRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) && (sinfo.Account.UserLevel >= 200) && (terms != string.Empty))
            {
                env.Session = sinfo;
                env.Flags = Flags.IsLoggedIn | Flags.IsAdmin;
                env.State = State.UserSearchFormResponse;
                // Put the listr in the environment
                env.Data = GetUserList(env, terms);
            }

            return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
        }

        public string UserEditGetRequest(Environment env, UUID userID)
        {
            m_log.DebugFormat("[WebApp]: UserEditGetRequest {0}", userID);
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) && (sinfo.Account.UserLevel >= 200))
            {
                env.Session = sinfo;
                env.Flags = Flags.IsLoggedIn | Flags.IsAdmin ;
                env.State = State.UserEditForm;
                UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, userID);
                if (account != null)
                {
                    List<object> loo = new List<object>();
                    loo.Add(account);
                    env.Data = loo;
                }

                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            else
            {
                return m_WebApp.ReadFile(env, "index.html");
            }
        }

        public string UserEditPostRequest(Environment env, UUID userID, string first, string last, string email, int level, int flags, string title)
        {
            m_log.DebugFormat("[WebApp]: UserEditPostRequest {0}", userID);
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) && (sinfo.Account.UserLevel >= 200))
            {
                env.Session = sinfo;
                UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, userID);
                if (account != null)
                {
                    // Update the account
                    account.FirstName = first;
                    account.LastName = last;
                    account.Email = email;
                    account.UserFlags = flags;
                    account.UserLevel = level;
                    account.UserTitle = title;
                    m_UserAccountService.StoreUserAccount(account);

                    env.Flags = Flags.IsLoggedIn | Flags.IsAdmin;
                    env.State = State.UserEditFormResponse;
                    m_log.DebugFormat("[WebApp]: Updated account for user {0}", account.Name);
                }
                else
                    m_log.DebugFormat("[WebApp]: Attempt at updating an inexistent account");

                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }

            return m_WebApp.ReadFile(env, "index.html");

        }

        public string UserEditPostRequest(Environment env, UUID userID, string password)
        {
            m_log.DebugFormat("[WebApp]: UserEditPostRequest (passord) {0}", userID);
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) && (sinfo.Account.UserLevel >= 200))
            {
                UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, userID);
                if (account != null)
                {
                    if (password != string.Empty)
                        m_AuthenticationService.SetPassword(account.PrincipalID, password);

                    env.Flags = Flags.IsAdmin | Flags.IsLoggedIn;
                    env.State = State.UserEditFormResponse;
                    m_log.DebugFormat("[WebApp]: Updated account for user {0}", account.Name);
                }
                else
                    m_log.DebugFormat("[WebApp]: Attempt at updating an inexistent account");
            }

            return m_WebApp.ReadFile(env, "index.html");

        }


        public string UserActivateGetRequest(Environment env, UUID userID)
        {
            m_log.DebugFormat("[WebApp]: UserActivateGetRequest {0}", userID);
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) && (sinfo.Account.UserLevel >= 200))
            {
                env.Session = sinfo;
                env.Flags = Flags.IsLoggedIn | Flags.IsAdmin;
                env.State = State.UserActivateResponse;
                UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, userID);
                if (account != null)
                {
                    //remove pending identifier in name
                    account.FirstName = account.FirstName.Replace("*pending* ", "");

                    //set serviceURLs back to normal
                    string password = (string)account.ServiceURLs["Password"];
                    account.ServiceURLs.Remove("Password");

                    //save changes
                    m_UserAccountService.StoreUserAccount(account);

                    // Create the inventory
                    m_InventoryService.CreateUserInventory(account.PrincipalID);

                    // Set the password
                    m_AuthenticationService.SetPassword(account.PrincipalID, password);

                    // Set the avatar
                    if (account.ServiceURLs.ContainsKey("Avatar"))
                    {
                        string avatarType = (string)account.ServiceURLs["Avatar"];
                        account.ServiceURLs.Remove("Avatar");
                        if (avatarType == "Female")
                            SetAvatar(account.PrincipalID, AvatarType.Female);
                        else if (avatarType == "Male")
                            SetAvatar(account.PrincipalID, AvatarType.Male);
                        else 
                            SetAvatar(account.PrincipalID, AvatarType.Neutral);

                    }

                    if (account.Email != string.Empty)
                    {
                        string message = "Your account has been activated.\n";
                        message += "\nFirst name: " + account.FirstName;
                        message += "\nLast name: " + account.LastName;
                        message += "\nPassword: " + password;
                        SendEMail(account.Email, "Account activated", message);
                    }
                }

                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            else
            {
                return m_WebApp.ReadFile(env, "index.html");
            }
        }

        public string UserDeleteGetRequest(Environment env, UUID userID)
        {
            m_log.DebugFormat("[WebApp]: UserDeleteGetRequest {0}", userID);
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) && (sinfo.Account.UserLevel >= 200))
            {
                env.Session = sinfo;
                env.Flags = Flags.IsLoggedIn | Flags.IsAdmin;
                env.State = State.UserDeleteForm;
                UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, userID);
                if (account != null)
                {
                    List<object> loo = new List<object>();
                    loo.Add(account);
                    env.Data = loo;
                }

                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            else
            {
                return m_WebApp.ReadFile(env, "index.html");
            }
        }


        public string UserDeletePostRequest(Environment env, UUID userID)
        {
            m_log.DebugFormat("[WebApp]: UserDeletePostRequest {0}", userID);
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) && (sinfo.Account.UserLevel >= 200))
            {
                env.Session = sinfo;
                UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, userID);
                if (account != null)
                {

                    m_UserAccountService.DeleteAccount(UUID.Zero, userID);
                    m_InventoryService.DeleteUserInventory(userID);

                    env.Flags = Flags.IsAdmin | Flags.IsLoggedIn;
                    env.State = State.UserDeleteFormResponse ;
                    m_log.DebugFormat("[WebApp]: Deleted account for user {0}", account.Name);
                }
                else
                    m_log.DebugFormat("[WebApp]: Attempt at deleting an inexistent account");
            }

            return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));

        }
    }
}
