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
using Environment = Diva.Wifi.Environment;

namespace Diva.Wifi
{
    public class WifiScriptFace : IWifiScriptFace
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;

        public string DocsPath
        {
            get { return m_WebApp.DocsPath; }
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

        #endregion

        public WifiScriptFace(WebApp webApp)
        {
            m_log.Debug("[WifiScriptFace]: Starting...");

            m_WebApp = webApp;
        }

        #region Methods exposed to WifiScript scripts

        public string GetContent(Environment env)
        {
            m_log.DebugFormat("[WifiScriptFace]: GetContent, flags {0} ({1})", env.Flags, (uint)env.Flags);

            //if (!Environment.IsInstalled)
            //    return "Welcome! Please install Wifi.";
            if ((env.Flags & StateFlags.InstallForm) != 0)
                return m_WebApp.ReadFile(env, "installform.html");
            if ((env.Flags & StateFlags.InstallFormResponse) != 0)
                return "Your Wifi has been installed. The administrator account is " + m_WebApp.AdminFirst + " " + m_WebApp.AdminLast;

            if ((env.Flags & StateFlags.ForgotPassword) != 0)
                return m_WebApp.ReadFile(env, "forgotpasswordform.html");
            if ((env.Flags & StateFlags.RecoveringPassword) != 0)
                return m_WebApp.ReadFile(env, "recoveringpassword.html");

            if ((env.Flags & StateFlags.FailedLogin) != 0)
                return "Login failed";
            if ((env.Flags & StateFlags.SuccessfulLogin) != 0)
            {
                return "Welcome to " + m_WebApp.GridName + "!";
            }

            if ((env.Flags & StateFlags.NewAccountForm) != 0)
                return m_WebApp.ReadFile(env, "newaccountform.html", env.Data);
            if ((env.Flags & StateFlags.NewAccountFormResponse) != 0)
                return "Your account has been created.";

            if ((env.Flags & StateFlags.IsLoggedIn) != 0)
            {
                if ((env.Flags & StateFlags.UserAccountForm) != 0)
                    return m_WebApp.ReadFile(env, "useraccountform.html", env.Data);
                if ((env.Flags & StateFlags.UserAccountFormResponse) != 0)
                    return "Your account has been updated.";

                if ((env.Flags & StateFlags.UserSearchForm) != 0)
                    return m_WebApp.ReadFile(env, "usersearchform.html", env.Data);
                if ((env.Flags & StateFlags.UserSearchFormResponse) != 0)
                    return GetUserList(env);

                if ((env.Flags & StateFlags.UserEditForm) != 0)
                    return m_WebApp.ReadFile(env, "usereditform.html", env.Data);
                if ((env.Flags & StateFlags.UserEditFormResponse) != 0)
                    return "The account has been updated.";
                
                if ((env.Flags & StateFlags.UserDeleteForm) != 0)
                    return m_WebApp.ReadFile(env, "userdeleteform.html", env.Data);
                if ((env.Flags & StateFlags.UserDeleteFormResponse) != 0)
                    return "The account has been deleted.";

                if ((env.Flags & StateFlags.RegionManagementForm) != 0)
                    return GetRegionManagementForm(env);
                if ((env.Flags & StateFlags.RegionManagementSuccessful) != 0)
                    return "Success! Back to <a href=\"/wifi/admin/regions\">Region Management Page</a>";
                if ((env.Flags & StateFlags.RegionManagementUnsuccessful) != 0)
                    return "Action could not be performed. Please check if the server is running.<br/>Back to <a href=\"/wifi/admin/regions\">Region Management Page</a>";
            }

            return string.Empty;
        }

        public string GetMainMenu(Environment env)
        {
            if (!m_WebApp.IsInstalled)
                return m_WebApp.ReadFile(env, "main-menu-install.html");

            SessionInfo sinfo = env.Session;
            if (sinfo.Account != null)
            {
                if (sinfo.Account.UserLevel >= 200) // Admin
                    return m_WebApp.ReadFile(env, "main-menu-admin.html");

                return m_WebApp.ReadFile(env, "main-menu-users.html", env.Data);
            }

            return m_WebApp.ReadFile(env, "main-menu.html", env.Data);
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

            return "Who are you?";
        }

        public string GetUserEmail(Environment env)
        {
            SessionInfo sinfo = env.Session;
            if (sinfo.Account != null)
            {
                if (sinfo.Account.Email == string.Empty)
                    return "No email on file";

                return sinfo.Account.Email;
            }

            return "Who are you?";
        }

        public string GetUserImage(Environment env)
        {
            SessionInfo sinfo = env.Session;
            if (sinfo.Account != null)
            {
                // TODO
                return "/wifi/images/temporaryphoto1.jpg";
            }

            // TODO
            return "/wifi/images/temporaryphoto1.jpg";
        }

        #endregion

        private string GetUserList(Environment env)
        {
            if (env.Data != null && env.Data.Count > 0)
                return m_WebApp.ReadFile(env, "userlist.html", env.Data);

            return "No users found";
        }

        private string GetRegionManagementForm(Environment env)
        {
            if (env.Data != null && env.Data.Count > 0)
                return m_WebApp.ReadFile(env, "region-form.html", env.Data);

            return "No regions found";
        }

    }

}
