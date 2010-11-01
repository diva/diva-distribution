/**
 * Copyright (c) Crista Lopes (aka Diva). All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, 
 * are permitted provided that the following conditions are met:
 * 
 *     * Redistributions of source code must retain the above copyright notice, 
 *       this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright notice, 
 *       this list of conditions and the following disclaimer in the documentation 
 *       and/or other materials provided with the distribution.
 *     * Neither the name of the Organizations nor the names of Individual
 *       Contributors may be used to endorse or promote products derived from 
 *       this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND 
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES 
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL 
 * THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, 
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED 
 * AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED 
 * OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 */
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
            m_log.DebugFormat("[Wifi]: UserManagementGetRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) &&
                (sinfo.Account.UserLevel >= WebApp.AdminUserLevel))
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
            m_log.DebugFormat("[Wifi]: UserSearchPostRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) &&
                (sinfo.Account.UserLevel >= WebApp.AdminUserLevel))
            {
                if (terms != string.Empty)
                {
                    env.Session = sinfo;
                    env.Flags = Flags.IsLoggedIn | Flags.IsAdmin;
                    env.State = State.UserSearchFormResponse;
                    // Put the list in the environment
                    env.Data = GetActiveUserList(env, terms);
                    return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
                }
                else
                    return UserManagementGetRequest(env);
            }

            return m_WebApp.ReadFile(env, "index.html");
        }

        public string UserEditGetRequest(Environment env, UUID userID)
        {
            m_log.DebugFormat("[Wifi]: UserEditGetRequest {0}", userID);
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) &&
                (sinfo.Account.UserLevel >= WebApp.AdminUserLevel))
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
            m_log.DebugFormat("[Wifi]: UserEditPostRequest {0}", userID);
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) &&
                (sinfo.Account.UserLevel >= WebApp.AdminUserLevel))
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
                    m_log.DebugFormat("[Wifi]: Updated account for user {0}", account.Name);
                }
                else
                    m_log.DebugFormat("[Wifi]: Attempt at updating an inexistent account");

                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }

            return m_WebApp.ReadFile(env, "index.html");

        }

        public string UserEditPostRequest(Environment env, UUID userID, string password)
        {
            m_log.DebugFormat("[Wifi]: UserEditPostRequest (passord) {0}", userID);
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) &&
                (sinfo.Account.UserLevel >= WebApp.AdminUserLevel))
            {
                UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, userID);
                if (account != null)
                {
                    if (password != string.Empty)
                        m_AuthenticationService.SetPassword(account.PrincipalID, password);

                    env.Flags = Flags.IsAdmin | Flags.IsLoggedIn;
                    env.State = State.UserEditFormResponse;
                    m_log.DebugFormat("[Wifi]: Updated account for user {0}", account.Name);
                }
                else
                    m_log.DebugFormat("[Wifi]: Attempt at updating an inexistent account");
            }

            return m_WebApp.ReadFile(env, "index.html");

        }


        public string UserActivateGetRequest(Environment env, UUID userID)
        {
            m_log.DebugFormat("[Wifi]: UserActivateGetRequest {0}", userID);
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) &&
                (sinfo.Account.UserLevel >= WebApp.AdminUserLevel))
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

                    Object value;
                    account.ServiceURLs.TryGetValue("Avatar", out value);
                    account.ServiceURLs.Remove("Avatar");
                    string avatarType = (string)value;

                    //save changes
                    m_UserAccountService.StoreUserAccount(account);

                    // Create the inventory
                    m_InventoryService.CreateUserInventory(account.PrincipalID);

                    // Set the password
                    m_AuthenticationService.SetPassword(account.PrincipalID, password);

                    // Set the avatar
                    if (avatarType != null)
                    {
                        SetAvatar(account.PrincipalID, avatarType);
                    }

                    if (account.Email != string.Empty)
                    {
                        string message = "Your account has been activated.\n";
                        message += "\nFirst name: " + account.FirstName;
                        message += "\nLast name: " + account.LastName;
                        message += "\n\nLoginURI: " + m_WebApp.LoginURL;
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
            m_log.DebugFormat("[Wifi]: UserDeleteGetRequest {0}", userID);
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) &&
                (sinfo.Account.UserLevel >= WebApp.AdminUserLevel))
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
            m_log.DebugFormat("[Wifi]: UserDeletePostRequest {0}", userID);
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) &&
                (sinfo.Account.UserLevel >= WebApp.AdminUserLevel))
            {
                env.Session = sinfo;
                UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, userID);
                if (account != null)
                {

                    m_UserAccountService.DeleteAccount(UUID.Zero, userID);
                    m_InventoryService.DeleteUserInventory(userID);

                    env.Flags = Flags.IsAdmin | Flags.IsLoggedIn;
                    env.State = State.UserDeleteFormResponse ;
                    m_log.DebugFormat("[Wifi]: Deleted account for user {0}", account.Name);
                }
                else
                    m_log.DebugFormat("[Wifi]: Attempt at deleting an inexistent account");
            }

            return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));

        }
    }
}
