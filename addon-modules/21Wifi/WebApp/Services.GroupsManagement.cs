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
using System.Globalization;

using log4net;
using OpenMetaverse;

using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Groups;

using Diva.Utils;
using Environment = Diva.Utils.Environment;
using Diva.OpenSimServices;
using GroupsService = Diva.OpenSimServices.GroupsService;

namespace Diva.Wifi
{
    public partial class Services
    {
        public string GroupsManagementGetRequest(Environment env)
        {
            m_log.DebugFormat("[Wifi]: GroupsManagementGetRequest");
            Request request = env.TheRequest;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) &&
                (sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel))
            {
                env.Session = sinfo;
                env.Flags = Flags.IsLoggedIn | Flags.IsAdmin;
                env.State = State.GroupsList;
                env.Data = GetGroupsList(env);
                return WebAppUtils.PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            
            return m_WebApp.ReadFile(env, "index.html");
        }

        public string GroupsSearchPostRequest(Environment env, string terms)
        {
            m_log.DebugFormat("[Wifi]: GroupsManagementGetRequest");
            Request request = env.TheRequest;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) &&
                (sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel))
            {
                if (terms != string.Empty)
                {
                    env.Session = sinfo;
                    env.Flags = Flags.IsLoggedIn | Flags.IsAdmin;
                    env.State = State.UserSearchFormResponse;
                    // Put the list in the environment
                    List<UserAccount> accounts = m_UserAccountService.GetActiveAccounts(UUID.Zero, terms, m_PendingIdentifier);
                    env.Data = WebAppUtils.Objectify<UserAccount>(accounts);

                    return WebAppUtils.PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
                }
                else
                    return GroupsManagementGetRequest(env);
            }

            return m_WebApp.ReadFile(env, "index.html");
        }

        public string GroupsEditGetRequest(Environment env, UUID groupID)
        {
            m_log.DebugFormat("[Wifi]: GroupsEditGetRequest {0}", groupID);
            Request request = env.TheRequest;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) &&
                (sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel))
            {
                if (m_GroupsService != null)
                {
                    env.Session = sinfo;
                    env.Flags = Flags.IsLoggedIn | Flags.IsAdmin;
                    env.State = State.GroupEditForm;
                    GroupRecord group = m_GroupsService.GetGroupRecord(groupID);
                    if (group != null)
                    {
                        List<object> loo = new List<object>();
                        loo.Add(group);
                        env.Data = loo;
                    }
                }
                else
                    m_log.WarnFormat("[Wifi]: No Groups service");

                return WebAppUtils.PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            
            return m_WebApp.ReadFile(env, "index.html");
        }

        public string GroupsEditPostRequest(Environment env, UUID groupID, string name, string charter)
        {
            m_log.DebugFormat("[Wifi]: GroupsEditPostRequest {0} {1}", groupID, name);
            Request request = env.TheRequest;


            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) &&
                (sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel))
            {
                env.Session = sinfo;
                if (m_GroupsService != null)
                {
                    GroupRecord group = m_GroupsService.GetGroupRecord(groupID);
                    if (group != null)
                    {
                        // Update the group
                        group.GroupName = name;
                        group.Charter = charter;
                        m_GroupsService.UpdateGroup(groupID, name, charter);

                        env.Flags = Flags.IsLoggedIn | Flags.IsAdmin;
                        NotifyWithoutButton(env, _("The group has been updated.", env));
                        m_log.DebugFormat("[Wifi]: Updated group {0}", group.GroupName);
                    }
                    else
                    {
                        NotifyWithoutButton(env, _("The group does not exist.", env));
                        m_log.DebugFormat("[Wifi]: Attempt at updating an inexistent group");
                    }

                    return WebAppUtils.PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
                }
                else
                    m_log.WarnFormat("[Wifi]: No Groups service");
            }

            return m_WebApp.ReadFile(env, "index.html");

        }

        public string GroupsDeleteGetRequest(Environment env, UUID groupID)
        {
            m_log.DebugFormat("[Wifi]: GroupsDeleteGetRequest {0}", groupID);
            Request request = env.TheRequest;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) &&
                (sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel))
            {
                if (m_GroupsService != null)
                {
                    env.Session = sinfo;
                    env.Flags = Flags.IsLoggedIn | Flags.IsAdmin;
                    env.State = State.GroupDeleteForm;
                    GroupRecord group = m_GroupsService.GetGroupRecord(groupID);
                    if (group != null)
                    {
                        List<object> loo = new List<object>();
                        loo.Add(group);
                        env.Data = loo;
                    }
                }
                else
                    m_log.WarnFormat("[Wifi]: No Groups service");

                return WebAppUtils.PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }

            return m_WebApp.ReadFile(env, "index.html");
        }


        public string GroupsDeletePostRequest(Environment env, UUID groupID)
        {
            m_log.DebugFormat("[Wifi]: GroupsDeletePostRequest {0}", groupID);
            Request request = env.TheRequest;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) &&
                (sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel))
            {
                env.Session = sinfo;
                m_GroupsService.DeleteGroup(groupID);

                env.Flags = Flags.IsAdmin | Flags.IsLoggedIn;
                NotifyWithoutButton(env, _("The group has been deleted.", env));
                m_log.DebugFormat("[Wifi]: Deleted group {0}", groupID);

                return WebAppUtils.PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }

            return m_WebApp.ReadFile(env, "index.html");
        }

    }

}
