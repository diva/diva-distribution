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
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using Nini.Config;
using log4net;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Services.InventoryService;

using Diva.Wifi.WifiScript;
using Diva.Utils;
using Diva.Interfaces;

using Environment = Diva.Utils.Environment;
using InventoryTreeNode = Diva.OpenSimServices.InventoryTreeNode;

namespace Diva.Wifi
{
    public class WifiScriptFace : IWifiScriptFace
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;

        public string DocsPath
        {
            get { return WebApp.DocsPath; }
        }

        #region Properties exposed to WifiScript scripts

        public int Port
        {
            get { return m_WebApp.Port; }
        }

        public string GridName
        {
            get { return m_WebApp.GridName; }
        }
        public string LoginURL
        {
            get { return m_WebApp.LoginURL; }
        }
        public string WebAddress
        {
            get { return m_WebApp.WebAddress; }
        }

        public string AdminFirst
        {
            get { return m_WebApp.AdminFirst; }
        }
        public string AdminLast
        {
            get { return m_WebApp.AdminLast; }
        }
        public string AdminEmail
        {
            get { return m_WebApp.AdminEmail; }
        }

        public string Version
        {
            get { return OpenSim.VersionInfo.Version; }
        }

        public string RuntimeInfo
        {
            get { return OpenSim.Framework.Util.GetRuntimeInformation(); }
        }

        public uint UsersInworld
        {
            get 
            {
                float value;
                m_WebApp.Statistics.TryGetValue("UsersInworld", out value);
                return (uint)value;
            }
        }
        public uint UsersTotal
        {
            get
            {
                float value;
                m_WebApp.Statistics.TryGetValue("UsersTotal", out value);
                return (uint)value;
            }
        }
        public uint UsersActive
        {
            get
            {
                float value;
                m_WebApp.Statistics.TryGetValue("UsersActive", out value);
                return (uint)value;
            }
        }
        public uint UsersActivePeriod
        {
            get { return (uint)m_WebApp.StatisticsActiveUsersPeriod; }
        }
        public uint RegionsTotal
        {
            get
            {
                float value;
                m_WebApp.Statistics.TryGetValue("RegionsTotal", out value);
                return (uint)value;
            }
        }
        
        #endregion

        public WifiScriptFace(WebApp webApp)
        {
            m_log.Debug("[WifiScriptFace]: Starting...");

            m_WebApp = webApp;
        }

        #region Methods exposed to WifiScript scripts

        public string GetContent(Environment env)
        {
            //m_log.DebugFormat("[WifiScriptFace]: GetContent, flags {0} ({1})", env.State, (uint)env.State);
            if ((uint)env.State < 50)
            {

                if (env.State == State.InstallForm)
                    return m_WebApp.ReadFile(env, "installform.html");

                if (env.State == State.ForgotPassword)
                    return m_WebApp.ReadFile(env, "forgotpasswordform.html");
                if (env.State == State.RecoveringPassword)
                    return m_WebApp.ReadFile(env, "recoveringpassword.html");
                //if (env.State == State.BadPassword)
                //    return "<p>The password must be at least 3 characters.</p>";

                if (env.State == State.NewAccountForm || env.State == State.NewAccountFormRetry)
                    return m_WebApp.ReadFile(env, "newaccountform.html", env.Data);

                if (env.State == State.Notification)
                    return m_WebApp.ReadFile(env, "notification.html", env.Data);

                if (env.State == State.GetTOS)
                    return m_WebApp.ReadFile(env, "tos.html", env.Data);

                if (env.State == State.AcceptTOS)
                    return "Thank you and enjoy your visit!";

                if ((env.Flags & Flags.IsLoggedIn) != 0)
                {
                    if (env.State == State.UserAccountForm)
                        return m_WebApp.ReadFile(env, "useraccountform.html", env.Data);

                    if (env.State == State.UserSearchForm)
                        return m_WebApp.ReadFile(env, "usersearchform.html", env.Data);
                    if (env.State == State.UserSearchFormResponse)
                        return GetUserList(env);

                    if (env.State == State.UserEditForm)
                        return m_WebApp.ReadFile(env, "usereditform.html", env.Data);

                    if (env.State == State.UserDeleteForm)
                        return m_WebApp.ReadFile(env, "userdeleteform.html", env.Data);

                    if (env.State == State.HyperlinkList)
                        return GetHyperlinks(env);
                    if (env.State == State.HyperlinkListForm)
                        return m_WebApp.ReadFile(env, "linkregionform.html", env.Data);
                    if (env.State == State.HyperlinkDeleteForm)
                        return m_WebApp.ReadFile(env, "linkregiondeleteform.html", env.Data);

                    if (env.State == State.RegionManagementForm)
                        return GetRegionManagementForm(env);
                    if (env.State == State.RegionManagementSuccessful)
                        return "Success! Back to <a href=\"/wifi/admin/regions\">Region Management Page</a>";
                    if (env.State == State.RegionManagementUnsuccessful)
                        return "Action could not be performed. Please check if the server is running.<br/>Back to <a href=\"/wifi/admin/regions\">Region Management Page</a>";

                    if (env.State == State.InventoryListForm)
                        return m_WebApp.ReadFile(env, "inventorylist.html", env.Data);

                    if (env.State == State.Console)
                        return m_WebApp.ReadFile(env, "console.html", env.Data);

                    if (env.State == State.GroupsList)
                        return m_WebApp.ReadFile(env, "groupslist.html", env.Data);
                    if (env.State == State.GroupEditForm)
                        return m_WebApp.ReadFile(env, "groupeditform.html", env.Data);
                    if (env.State == State.GroupDeleteForm)
                        return m_WebApp.ReadFile(env, "groupdeleteform.html", env.Data);

                }
            }
            else // try the addons
            {
                foreach (WifiAddon addon in m_WebApp.Addons)
                {
                    string result;
                    if ((result = addon.Addon.GetContent(env)) != null)
                        return result;
                }
            }

            return string.Empty;
        }

        public string GetRefresh(Environment env)
        {
            const string redirect = "<meta http-equiv=\"refresh\" content=\"{0}; URL={1}/?sid={2}\" />";

            if (env.State == State.Notification)
            {
                SessionInfo sinfo = env.Session;
                if (sinfo.Sid != null && sinfo.Notify.RedirectDelay >= 0)
                    return string.Format(redirect, sinfo.Notify.RedirectDelay, sinfo.Notify.RedirectUrl, sinfo.Sid);
            }
            return string.Empty;
        }

        public string GetNotificationType(Environment env)
        {
            SessionInfo sinfo = env.Session;
            if (sinfo.Notify.FollowUp == null)
                return "hidden";

            return "submit";
        }

        public string GetMainMenu(Environment env)
        {
            if (!m_WebApp.IsInstalled)
                return m_WebApp.ReadFile(env, "main-menu-install.html");

            SessionInfo sinfo = env.Session;
            if (sinfo.Account != null)
            {
                if (sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel) // Admin
                    return m_WebApp.ReadFile(env, "main-menu-admin.html");
                else if (sinfo.Account.UserLevel >= m_WebApp.HyperlinksUserLevel) // Privileged user
                    return m_WebApp.ReadFile(env, "main-menu-privileged.html");

                return m_WebApp.ReadFile(env, "main-menu-users.html", env.Data);
            }

            return m_WebApp.ReadFile(env, "main-menu.html", env.Data);
        }

        public string GetAddonsMenu(Environment env)
        {
            if (!m_WebApp.IsInstalled)
                return string.Empty;

            // are there any addons?
            if (m_WebApp.Addons.Count > 0)
            {
                SessionInfo sinfo = env.Session;
                if (sinfo.Account != null)
                {
                    if (sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel) // Admin
                    {
                        StringBuilder str = new StringBuilder("<p class=\"nav-headline\">Addons menu</p><div id=\"addons-menu\">  <ul>");
                        str.Append(System.Environment.NewLine);
                        foreach (WifiAddon a in m_WebApp.Addons)
                            str.AppendFormat("  <li><a href=\"{0}\">{1}</a></li>{2}", a.Path, a.MenuAnchor, System.Environment.NewLine);

                        str.Append("  </ul></div>");

                        return str.ToString();
                    }

                }
            }

            return string.Empty;
        }

        public string GetLoginLogout(Environment env)
        {
            if (!m_WebApp.IsInstalled)
                return string.Empty;

            SessionInfo sinfo = env.Session;
            if (sinfo.Account != null)
                return m_WebApp.ReadFile(env, "logout.html", env.Data);

            return m_WebApp.ReadFile(env, "login.html", env.Data);
        }

        public string GetUserName(Environment env)
        {
            SessionInfo sinfo = env.Session;
            if (sinfo.Account != null)
            {
                return sinfo.Account.FirstName + " " + sinfo.Account.LastName;
            }

            return _("Who are you?", env);
        }

        public string GetUserEmail(Environment env)
        {
            SessionInfo sinfo = env.Session;
            if (sinfo.Account != null)
            {
                if (sinfo.Account.Email == string.Empty)
                    return _("No email on file", env);

                return sinfo.Account.Email;
            }

            return _("Who are you?", env);
        }

        public string GetUserImage(Environment env)
        {
            SessionInfo sinfo = env.Session;
            if (sinfo.Account != null)
            {
                // TODO
                return "/wifi/images/abstract-cool.jpg";
            }

            // TODO
            return "/wifi/images/abstract-cool.jpg";
        }

        public string GetConsoleUser(Environment env)
        {
            SessionInfo sinfo = env.Session;
            if (sinfo.Account != null && sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel)
                return m_WebApp.ConsoleUser;

            return string.Empty;
        }
        public string GetConsolePass(Environment env)
        {
            SessionInfo sinfo = env.Session;
            if (sinfo.Account != null && sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel)
                return m_WebApp.ConsolePass;

            return string.Empty;
        }

        public string GetPendingUserList(Environment env)
        {
            SessionInfo sinfo = env.Session;
            if (env.Data != null && env.Data.Count > 0 &&
                sinfo.Account != null && sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel)
                return m_WebApp.ReadFile(env, "userpendinglist.html", env.Data);

            return string.Empty;
        }

        public string GetHyperlinks(Environment env)
        {
            if (env.Data != null && env.Data.Count > 0)
                return m_WebApp.ReadFile(env, "linkregionlist.html", env.Data);

            return _("No linked regions found", env);
        }

        public string GetIARTransfers(Environment env)
        {
            if (env.Session.Account.UserLevel >= m_WebApp.IARUserLevel)
                return m_WebApp.ReadFile(env, "iars.html", env.Data);

            return string.Empty;
        }


        public string LocalizePath(IEnvironment env, string path)
        {
            return Localization.LocalizePath(env, path);
        }
        public string Translate(IEnvironment env, string textId)
        {
            return Localization.Translate(env, textId);
        }

        #endregion

        private static string _(string textId, Environment env)
        {
            return Localization.Translate(env, textId);
        }

        private string GetUserList(Environment env)
        {
            if (env.Data != null && env.Data.Count > 0)
                return m_WebApp.ReadFile(env, "userlist.html", env.Data);

            return _("No users found", env);
        }

        private string GetRegionManagementForm(Environment env)
        {
            if (env.Data != null && env.Data.Count > 0)
                return m_WebApp.ReadFile(env, "region-form.html", env.Data);

            return _("No regions found", env);
        }

    }

}
