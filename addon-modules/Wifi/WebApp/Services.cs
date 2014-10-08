﻿/**
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using Nini.Config;
using log4net;
using OpenMetaverse;
using Nwc.XmlRpc;

using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Services.AvatarService;
using OpenSim.Services.PresenceService;
using OpenSim.Server.Base;

using Diva.Utils;
using Diva.Wifi.WifiScript;
using Environment = Diva.Utils.Environment;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Request = Diva.Utils.Request;
using AssetService = OpenSim.Services.AssetService.AssetService;

using Diva.OpenSimServices;
using Diva.Interfaces;

namespace Diva.Wifi
{
    public partial class Services
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string m_PendingIdentifier = "*pending* ";

        private WebApp m_WebApp;
        private SmtpClient m_Client;

        private UserAccountService m_UserAccountService;
        private PasswordAuthenticationService m_AuthenticationService;
        private InventoryService m_InventoryService;
        private IAssetService m_AssetService;
        private GridService m_GridService;
        private GridUserService m_GridUserService;
        private IAvatarService m_AvatarService;
        private IPresenceService m_PresenceService;
        private IGroupsService m_GroupsService;

        private string m_ServerAdminPassword;
        private DateTime m_LastStatisticsUpdate;

        // Sessions
        //private Dictionary<string, SessionInfo> m_Sessions = new Dictionary<string, SessionInfo>();
        private ExpiringCache<string, SessionInfo> m_Sessions = new ExpiringCache<string, SessionInfo>();

        public Services(IConfigSource config, string configName, WebApp webApp)
        {
            m_log.Debug("[Wifi]: Services Starting...");

            m_WebApp = webApp;

            m_ServerAdminPassword = webApp.RemoteAdminPassword;
            //m_log.DebugFormat("[Services]: RemoteAdminPassword is {0}", m_ServerAdminPassword);
            m_LastStatisticsUpdate = new DateTime();

            // Create the necessary services
            m_UserAccountService = new UserAccountService(config);
            m_AuthenticationService = new PasswordAuthenticationService(config);
            m_InventoryService = new InventoryService(config);
            m_AssetService = new AssetService(config);
            m_GridService = new GridService(config);
            m_GridUserService = new GridUserService(config);
            m_AvatarService = new AvatarService(config);
            m_PresenceService = new PresenceService(config);
            m_GroupsService = new GroupsService(config);

            // Create the "God" account if it doesn't exist
            CreateGod();

            // Connect to our outgoing mail server for password forgetfulness
            m_Client = new SmtpClient(m_WebApp.SmtpHost, m_WebApp.SmtpPort);
            if (m_WebApp.SmtpPort == 25)
                m_Client.EnableSsl = false;
            else
                m_Client.EnableSsl = true;
            m_Client.Credentials = new NetworkCredential(m_WebApp.SmtpUsername, m_WebApp.SmtpPassword);
            m_Client.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);
        }

        private void CreateGod()
        {
            UserAccount god = m_UserAccountService.GetUserAccount(UUID.Zero, m_WebApp.AdminFirst, m_WebApp.AdminLast);
            if (god == null)
            {
                m_log.DebugFormat("[Wifi]: Administrator account {0} {1} does not exist. Creating it...", m_WebApp.AdminFirst, m_WebApp.AdminLast);
                // Doesn't exist. Create one
                god = m_UserAccountService.CreateUser(UUID.Zero, UUID.Random(), m_WebApp.AdminFirst, m_WebApp.AdminLast, m_WebApp.AdminPassword, m_WebApp.AdminEmail);

                god.UserLevel = m_WebApp.AdminUserLevel;
                god.UserTitle = "Administrator";
                god.UserFlags = 0;
                m_UserAccountService.StoreUserAccount(god);
                if (m_WebApp.AdminPassword == string.Empty)
                    // Signal that the App needs installation
                    m_WebApp.IsInstalled = false;
                else
                {
                    m_WebApp.IsInstalled = true;
                }
            }
            else
            {
                m_log.DebugFormat("[Wifi]: Administrator account {0} {1} exists.", m_WebApp.AdminFirst, m_WebApp.AdminLast);
                // Signal that the App has been previously installed
                m_WebApp.IsInstalled = true;
            }

            if (god.UserLevel < m_WebApp.AdminUserLevel)
            {
                // Might have existed but had wrong UserLevel
                god.UserLevel = m_WebApp.AdminUserLevel;
                m_UserAccountService.StoreUserAccount(god);
            }

        }

        private void PrepareHomeLocation()
        {
            if (m_WebApp.DefaultHome != string.Empty && Avatar.HomeRegion == UUID.Zero)
            {
                string[] parts = m_WebApp.DefaultHome.Split(new char[] { '/' });
                if (parts.Length > 0)
                {
                    GridRegion region = m_GridService.GetRegionByName(UUID.Zero, parts[0]);
                    if (region != null)
                    {
                        Avatar.HomeRegion = region.RegionID;
                        uint coordinate;
                        if (parts.Length > 1)
                        {
                            uint.TryParse(parts[1], out coordinate);
                            Avatar.HomeLocation.X = coordinate;
                        }
                        if (parts.Length > 2)
                        {
                            uint.TryParse(parts[2], out coordinate);
                            Avatar.HomeLocation.Y = coordinate;
                        }
                        if (parts.Length > 3)
                        {
                            uint.TryParse(parts[3], out coordinate);
                            Avatar.HomeLocation.Z = coordinate;
                        }
                        m_log.DebugFormat("[Wifi]: Default home location {0} parsed to region UUID {1} and coordinates {2}.", m_WebApp.DefaultHome, Avatar.HomeRegion, Avatar.HomeLocation);
                    }
                }
            }
        }

        public bool TryGetSessionInfo(Request request, out SessionInfo sinfo)
        {
            bool success = false;
            sinfo = new SessionInfo();
            if (request.Query.ContainsKey("sid"))
            {
                string sid = request.Query["sid"].ToString();
                if (m_Sessions.Contains(sid))
                {
                    SessionInfo session;
                    if (m_Sessions.TryGetValue(sid, out session) &&
                        session.IpAddress == request.IPEndPoint.Address.ToString())
                    {
                        sinfo = session;
                        m_Sessions.AddOrUpdate(sid, session, m_WebApp.SessionTimeout);
                        success = true;
                    }
                }
                else
                {
                    UUID sessionid = UUID.Zero;
                    if (UUID.TryParse(request.Query["sid"].ToString(), out sessionid))
                    {
                        PresenceInfo pinfo = m_PresenceService.GetAgent(sessionid);
                        if (pinfo != null)
                        {
                            m_log.DebugFormat("[Wifi]: User is present in the grid");
                            success = true;

                            UserAccount account = null;
                            if (request.Query.ContainsKey("uid"))
                            {
                                UUID userID = UUID.Zero;
                                if (UUID.TryParse(request.Query["uid"].ToString(), out userID))
                                    account = m_UserAccountService.GetUserAccount(UUID.Zero, userID);
                            }
                            else
                                m_log.DebugFormat("[Wifi]: No uid in Query");

                            sinfo.IpAddress = request.IPEndPoint.Address.ToString();
                            sinfo.Sid = request.Query["sid"].ToString();
                            sinfo.Account = account;
                            sinfo.Notify = new NotificationData();

                            m_Sessions.Add(sinfo.Sid, sinfo, m_WebApp.SessionTimeout);
                        }
                        else
                            m_log.DebugFormat("[Wifi]: User is not present in the grid");
                    }
                    else
                        m_log.DebugFormat("[Wifi]: Unable o parse sid {0}", request.Query["sid"].ToString());
                }
            }
            //else
            //    m_log.DebugFormat("[Wifi]: no sid in Query");

            return success;
        }

        public void ComputeStatistics()
        {
            // Users in world
            m_WebApp.Statistics["UsersInworld"] = m_GridUserService.GetOnlineUserCount();
            
            // For the other stats, let's check less often
            DateTime now = DateTime.Now;
            if (now - m_LastStatisticsUpdate < m_WebApp.StatisticsUpdateInterval)
                return;
            m_LastStatisticsUpdate = now;

            // Total users
            m_WebApp.Statistics["UsersTotal"] = m_UserAccountService.GetActiveAccountsCount(UUID.Zero, m_PendingIdentifier);

            // Active users
            m_WebApp.Statistics["UsersActive"] = m_GridUserService.GetActiveUserCount(m_WebApp.StatisticsActiveUsersPeriod);

            // Total local regions
            m_WebApp.Statistics["RegionsTotal"] = m_GridService.GetLocalRegionsCount(UUID.Zero);
        }


        private static string _(string textId, Environment env)
        {
            return Localization.Translate(env.LanguageInfo, textId);
        }
        private static string _(string textId, CultureInfo[] languages)
        {
            return Localization.Translate(languages, textId);
        }

        // <a href="wifi/..." ...>
        static Regex href = new Regex("(<a\\s+.*href\\s*=\\s*\\\"(\\S+\\\")).*>");
        static Regex action = new Regex("(<form\\s+.*action\\s*=\\s*\\\"(\\S+\\\")).*>");
        static Regex xmlhttprequest = new Regex("(@@wifi@@(\\S+\\\"))");


        /*
        private void PrintStr(string html)
        {
            foreach (char c in html)
                Console.Write(c);
        }
        */
        private List<object> GetUserList(Environment env, string terms)
        {
            List<UserAccount> accounts = m_UserAccountService.GetUserAccounts(UUID.Zero, terms);
            if (accounts != null && accounts.Count > 0)
            {
                return WebAppUtils.Objectify<UserAccount>(accounts);
            }
            else
            {
                return new List<object>();
            }

        }

        private List<object> GetGroupsList(Environment env)
        {
            if (m_GroupsService != null)
            {
                List<DirGroupsReplyData> groups = m_GroupsService.FindGroups(string.Empty, string.Empty);
                if (groups != null && groups.Count > 0)
                {
                    return WebAppUtils.Objectify<DirGroupsReplyData>(groups);
                }
            }
            return new List<object>();
        }
        
        private List<object> GetDefaultAvatarSelectionList()
        {
            // Present only default avatars of a non-empty type.
            // This allows to specify a default avatar which is used when none is selected
            // during account creation. Use the following configuration settings to enable
            // this ("Default Avatar" may be any avatar name or "" to create an empty
            // standard inventory):
            // [WifiService]
            //    AvatarAccount_ = "Default Avatar"
            //    AvatarPreselection = ""
            IEnumerable<Avatar> visibleAvatars = m_WebApp.DefaultAvatars.Where(avatar => !string.IsNullOrEmpty(avatar.Type));

            return WebAppUtils.Objectify<Avatar>(visibleAvatars);
        }

        private void SetServiceURLs(UserAccount account)
        {
            account.ServiceURLs = new Dictionary<string, object>();
            account.ServiceURLs["HomeURI"] = m_WebApp.LoginURL.ToString();
            account.ServiceURLs["InventoryServerURI"] = m_WebApp.LoginURL.ToString();
            account.ServiceURLs["AssetServerURI"] = m_WebApp.LoginURL.ToString();
        }

        public bool SendEMailSync(string to, string subject, string message)
        {
            bool success = true;
            try
            {
                MailMessage msg = new MailMessage();
                msg.From = new MailAddress(m_WebApp.SmtpUsername);
                msg.To.Add(new MailAddress(to));
                msg.Subject = "[" + m_WebApp.GridName + "] " + subject;
                msg.Body = message;
                m_Client.Send(msg);
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[Wifi]: Exception on sending mail to {0}: {1}", to, e);
                success = false;
            }
            return success;
        }

        public bool SendEMail(string to, string subject, string message)
        {
            bool success = true;
            try
            {
                MailMessage msg = new MailMessage();
                msg.From = new MailAddress(m_WebApp.SmtpUsername);
                msg.To.Add(new MailAddress(to));
                msg.Subject = "[" + m_WebApp.GridName + "] " + subject;
                msg.Body = message;
                m_Client.SendAsync(msg, to);
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[Wifi]: Exception on sending mail to {0}: {1}", to, e);
                success = false;
            }
            return success;
        }

        private void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            String token = (string)e.UserState;

            if (e.Cancelled)
                m_log.DebugFormat("[Wifi]: [{0}] Send cancelled.", token);

            if (e.Error != null)
                m_log.DebugFormat("[Wifi]: [{0}] {1}", token, e.Error.ToString());
            else
                m_log.DebugFormat("[Wifi]: Message sent to " + token + ".");

        }

        public T GetServiceObject<T>()
        {
            object obj = null;
            if (typeof(T) == typeof(GridUserService))
                obj = m_GridUserService;
            else if (typeof(T) == typeof(UserAccountService))
                obj = m_UserAccountService;
            else if (typeof(T) == typeof(PresenceService))
                obj = m_PresenceService;
            else if (typeof(T) == typeof(GridService))
                obj = m_GridService;
            else if (typeof(T) == typeof(InventoryService))
                obj = m_InventoryService;
            else if (typeof(T) == typeof(GroupsService))
                obj = m_GroupsService;

            return (T)obj;
        }
    }

}
