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
using System.ComponentModel;
using System.IO;
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
using OpenSim.Services.InventoryService;
using OpenSim.Services.GridService;

using Diva.Wifi.WifiScript;
using Environment = Diva.Wifi.Environment;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

using Diva.OpenSimServices;

namespace Diva.Wifi
{
    public class Services
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;
        private SmtpClient m_Client;

        private UserAccountService m_UserAccountService;
        private PasswordAuthenticationService m_AuthenticationService;
        private IInventoryService m_InventoryService;
        private IGridService m_GridService;

        private string m_ServerAdminPassword;

        // Sessions
        private Dictionary<string, SessionInfo> m_Sessions = new Dictionary<string, SessionInfo>();

        public Services(IConfigSource config, string configName, WebApp webApp)
        {
            m_log.Debug("[Services]: Starting...");

            m_WebApp = webApp;

            m_ServerAdminPassword = webApp.RemoteAdminPassword;
            m_log.DebugFormat("[Services]: RemoteAdminPassword is {0}", m_ServerAdminPassword);

            // Create the necessary services
            m_UserAccountService = new UserAccountService(config);
            m_AuthenticationService = new PasswordAuthenticationService(config);
            m_InventoryService = new InventoryService(config);
            m_GridService = new GridService(config);

            // Create the "God" account if it doesn't exist
            CreateGod();

            // Connect to our outgoing mail server for password forgetfulness
            m_Client = new SmtpClient(m_WebApp.SmtpHost, m_WebApp.SmtpPort);
            m_Client.EnableSsl = true;
            m_Client.Credentials = new NetworkCredential(m_WebApp.SmtpUsername, m_WebApp.SmtpPassword);
            m_Client.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);
        }

        private void CreateGod()
        {
            UserAccount god = m_UserAccountService.GetUserAccount(UUID.Zero, m_WebApp.AdminFirst, m_WebApp.AdminLast);
            if (god == null)
            {
                m_log.DebugFormat("[WebApp]: Administrator account {0} {1} does not exist. Creating it...", m_WebApp.AdminFirst, m_WebApp.AdminLast);
                // Doesn't exist. Create one
                god = new UserAccount(UUID.Zero, m_WebApp.AdminFirst, m_WebApp.AdminLast, m_WebApp.AdminEmail);
                god.UserLevel = 500;
                god.UserTitle = "Administrator";
                god.UserFlags = 0;
                SetServiceURLs(god);
                m_UserAccountService.StoreUserAccount(god);
                m_InventoryService.CreateUserInventory(god.PrincipalID);
                // Signal that the App needs installation
                m_WebApp.IsInstalled = false;
            }
            else
            {
                m_log.DebugFormat("[WebApp]: Administrator account {0} {1} exists.", m_WebApp.AdminFirst, m_WebApp.AdminLast);
                // Signal that the App has been previously installed
                m_WebApp.IsInstalled = true;
            }

            if (god.UserLevel < 200)
            {
                // Might have existed but had wrong UserLevel
                god.UserLevel = 500;
                m_UserAccountService.StoreUserAccount(god);
            }

        }

        public bool TryGetSessionInfo(Request request, out SessionInfo sinfo)
        {
            bool success = false;
            sinfo = new SessionInfo();
            if (request.Query.ContainsKey("sid"))
            {
                string sid = request.Query["sid"].ToString();
                if (m_Sessions.ContainsKey(sid))
                {
                    if (m_Sessions[sid].IpAddress == request.IPEndPoint.Address.ToString())
                    {
                        sinfo = m_Sessions[sid];
                        success = true;
                    }
                }
            }

            return success;
        }

        public string InstallGetRequest(Environment env)
        {
            env.Flags = StateFlags.InstallForm;
            return m_WebApp.ReadFile(env, "index.html");
        }

        public string InstallPostRequest(Environment env, string password, string password2)
        {
            if(m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[WebApp]: warning: someone is trying to change the god password in InstallPostRequest!");
                return m_WebApp.ReadFile(env, "index.html");
            }    

            m_log.DebugFormat("[WebApp]: UserAccountPostRequest");
            Request request = env.Request;

            if (password == password2)
            {
                UserAccount god = m_UserAccountService.GetUserAccount(UUID.Zero, m_WebApp.AdminFirst, m_WebApp.AdminLast);
                if (god != null)
                {
                    m_AuthenticationService.SetPassword(god.PrincipalID, password);
                    // And this finishes the installation procedure
                    m_WebApp.IsInstalled = true;
                    env.Flags = StateFlags.InstallFormResponse;
                }

            }

            return m_WebApp.ReadFile(env, "index.html");
        }

        public string LoginRequest(Environment env, string first, string last, string password)
        {
            if(!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[WebApp]: warning: someone is trying to access LoginRequest and Wifi isn't isntalled!");
                return m_WebApp.ReadFile(env, "index.html");
            }

            m_log.DebugFormat("[WebApp]: LoginRequest {0} {1}", first, last);
            Request request = env.Request;
            string encpass = OpenSim.Framework.Util.Md5Hash(password);

            UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, first, last);
            if (account == null)
            {
                env.Flags = StateFlags.FailedLogin;
                return m_WebApp.ReadFile(env, "index.html");
            }

            string authtoken = m_AuthenticationService.Authenticate(account.PrincipalID, encpass, 30);
            if (authtoken == string.Empty)
            {
                env.Flags = StateFlags.FailedLogin;
                return m_WebApp.ReadFile(env, "index.html");
            }

            // Successful login
            SessionInfo sinfo;
            sinfo.IpAddress = request.IPEndPoint.Address.ToString();
            sinfo.Sid = authtoken;
            sinfo.Account = account;
            m_Sessions.Add(authtoken, sinfo);
            env.Request.Query["sid"] = authtoken;
            env.Session = sinfo;

            List<object> loo = new List<object>();
            loo.Add(account);
            env.Data = loo;
            env.Flags = StateFlags.IsLoggedIn | StateFlags.SuccessfulLogin;
            return PadURLs(env, authtoken, m_WebApp.ReadFile(env, "index.html"));
        }

        public string LogoutRequest(Environment env)
        {
            m_log.DebugFormat("[WebApp]: LogoutRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo))
            {
                m_Sessions.Remove(sinfo.Sid);
                m_AuthenticationService.Release(sinfo.Account.PrincipalID, sinfo.Sid);
            }

            env.Flags = 0;
            return m_WebApp.ReadFile(env, "index.html");
        }

        public string UserAccountGetRequest(Environment env, UUID userID)
        {
            if(!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[WebApp]: warning: someone is trying to access UserAccountGetRequest and Wifi isn't isntalled!");
                return m_WebApp.ReadFile(env, "index.html");
            }    

            m_log.DebugFormat("[WebApp]: UserAccountGetRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo))
            {
                env.Session = sinfo;
                List<object> loo = new List<object>();
                loo.Add(sinfo.Account);
                env.Data = loo;
                env.Flags = StateFlags.IsLoggedIn | StateFlags.UserAccountForm;
                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            else
            {
                return m_WebApp.ReadFile(env, "index.html");
            }
        }

        public string UserAccountPostRequest(Environment env, UUID userID, string email, string oldpassword, string newpassword, string newpassword2)
        {
			if(!m_WebApp.IsInstalled)
			{
                m_log.DebugFormat("[WebApp]: warning: someone is trying to access UserAccountPostRequest and Wifi isn't isntalled!");
				return m_WebApp.ReadFile(env, "index.html");
			}
            m_log.DebugFormat("[WebApp]: UserAccountPostRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo))
            {
                env.Session = sinfo;
                // We get the userID, but we only allow changes to the account of this session
                List<object> loo = new List<object>();
                loo.Add(sinfo.Account);
                env.Data = loo;

                bool updated = false;
                if (email != string.Empty && email.Contains("@") && sinfo.Account.Email != email)
                {
                    sinfo.Account.Email = email;
                    m_UserAccountService.StoreUserAccount(sinfo.Account);
                    updated = true;
                }

                string encpass = OpenSim.Framework.Util.Md5Hash(oldpassword);
                if ((newpassword != string.Empty) && (newpassword == newpassword2) &&
                    m_AuthenticationService.Authenticate(sinfo.Account.PrincipalID, encpass, 30) != string.Empty)
                {
                    m_AuthenticationService.SetPassword(sinfo.Account.PrincipalID, newpassword);
                    updated = true;
                }

                if (updated)
                {
                    env.Flags = StateFlags.IsLoggedIn | StateFlags.UserAccountFormResponse;
                    m_log.DebugFormat("[WebApp]: Updated account for user {0}", sinfo.Account.Name);
                    return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
                }

                // nothing was updated, really
                env.Flags = StateFlags.IsLoggedIn | StateFlags.UserAccountForm;
                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            else
            {
                m_log.DebugFormat("[WebApp]: Failed to get session info");
                return m_WebApp.ReadFile(env, "index.html");
            }
        }

        public string NewAccountGetRequest(Environment env)
        {
            m_log.DebugFormat("[WebApp]: NewAccountGetRequest");
            Request request = env.Request;

            env.Flags = StateFlags.NewAccountForm;
            return m_WebApp.ReadFile(env, "index.html");
        }

        public string NewAccountPostRequest(Environment env, string first, string last, string email, string password, string password2)
        {
            if(!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[WebApp]: warning: someone is trying to access NewAccountPostRequest and Wifi isn't isntalled!");
                return m_WebApp.ReadFile(env, "index.html");
            }    


            m_log.DebugFormat("[WebApp]: NewAccountPostRequest");
            Request request = env.Request;

            if ((password != string.Empty) && (password == password2))
            {
                UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, first, last);
                if (account == null)
                {
                    // Create the account
                    account = new UserAccount(UUID.Zero, first, last, email);
                    SetServiceURLs(account);
                    m_UserAccountService.StoreUserAccount(account);

                    // Create the inventory
                    m_InventoryService.CreateUserInventory(account.PrincipalID);

                    // Store the password
                    m_AuthenticationService.SetPassword(account.PrincipalID, password);

                    env.Flags = StateFlags.NewAccountFormResponse;
                    m_log.DebugFormat("[WebApp]: Created account for user {0}", account.Name);
                }
                else
                    m_log.DebugFormat("[WebApp]: Attempt at creating an account that already exists");
            }
            else
            {
                m_log.DebugFormat("[WebApp]: did not create account because of password problems");
                env.Flags = StateFlags.NewAccountForm;
            }

            return m_WebApp.ReadFile(env, "index.html");

        }

        public string UserManagementGetRequest(Environment env)
        {
            m_log.DebugFormat("[WebApp]: UserManagementGetRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) && (sinfo.Account.UserLevel >= 200))
            {
                env.Session = sinfo;
                env.Flags = StateFlags.IsLoggedIn | StateFlags.IsAdmin | StateFlags.UserSearchForm;
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
                env.Flags = StateFlags.IsLoggedIn | StateFlags.IsAdmin | StateFlags.UserSearchFormResponse;
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
                env.Flags = StateFlags.IsLoggedIn | StateFlags.IsAdmin | StateFlags.UserEditForm;
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

                    env.Flags = StateFlags.UserEditFormResponse | StateFlags.IsAdmin | StateFlags.IsLoggedIn;
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

                    env.Flags = StateFlags.UserEditFormResponse | StateFlags.IsAdmin | StateFlags.IsLoggedIn;
                    m_log.DebugFormat("[WebApp]: Updated account for user {0}", account.Name);
                }
                else
                    m_log.DebugFormat("[WebApp]: Attempt at updating an inexistent account");
            }

            return m_WebApp.ReadFile(env, "index.html");

        }

        public string UserDeleteGetRequest(Environment env, UUID userID)
        {
            m_log.DebugFormat("[WebApp]: UserDeleteGetRequest {0}", userID);
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) && (sinfo.Account.UserLevel >= 200))
            {
                env.Session = sinfo;
                env.Flags = StateFlags.IsLoggedIn | StateFlags.IsAdmin | StateFlags.UserDeleteForm;
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

                    env.Flags = StateFlags.UserDeleteFormResponse | StateFlags.IsAdmin | StateFlags.IsLoggedIn;
                    m_log.DebugFormat("[WebApp]: Deleted account for user {0}", account.Name);
                }
                else
                    m_log.DebugFormat("[WebApp]: Attempt at deleting an inexistent account");
            }

            return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));

        }

        public string ForgotPasswordGetRequest(Environment env)
        {
            m_log.DebugFormat("[WebApp]: ForgotPasswordGetRequest");
            env.Flags = StateFlags.ForgotPassword;
            return m_WebApp.ReadFile(env, "index.html");
        }

        public string ForgotPasswordPostRequest(Environment env, string email)
        {
            UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, email);
            if (account != null)
            {
                string token = m_AuthenticationService.GetToken(account.PrincipalID, 60);
                if (token != string.Empty)
                {
                    string url = m_WebApp.WebAddress + "/wifi/recover/" + token + "?email=" + HttpUtility.UrlEncode(email);

                    MailMessage msg = new MailMessage();
                    msg.From = new MailAddress(m_WebApp.SmtpUsername);
                    msg.To.Add(email);
                    msg.Subject = "[" + m_WebApp.GridName + "] Password Reset";
                    msg.Body = "Let's reset your password. Click here:\n\t";
                    msg.Body += url;
                    msg.Body += "\n\nDiva";
                    m_Client.SendAsync(msg, email);

                    return "<p>Check your email. You must reset your password within 60 minutes.</p>"; /// change this
                }
            }

            return m_WebApp.ReadFile(env, "index.html");
        }

        private static void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            String token = (string)e.UserState;

            if (e.Cancelled)
                m_log.DebugFormat("[ForgotPasswordService] [{0}] Send cancelled.", token);

            if (e.Error != null)
                m_log.DebugFormat("[ForgotPasswordService] [{0}] {1}", token, e.Error.ToString());
            else
                m_log.DebugFormat("[ForgotPasswordService] Password recovery message sent to " + token + ".");
        }

        public string RecoverPasswordGetRequest(Environment env, string email, string token)
        {
            UserAccount account = null;
            if (IsValidToken(email, token, out account))
            {
                PasswordRecoveryData precovery = new PasswordRecoveryData(email, token);
                env.Data = new List<object>();
                env.Data.Add(precovery);
                env.Flags = StateFlags.RecoveringPassword;
                return m_WebApp.ReadFile(env, "index.html");
            }
            else
            {
                return "<p>Invalid token.</p>";
            }
        }

        public string RecoverPasswordPostRequest(Environment env, string email, string token, string newPassword)
        {
            if (newPassword == null || newPassword.Length == 0)
            {
                return "<p>You must enter <strong>some</strong> password!</p>";
            }

            ResetPassword(email, token, newPassword);
            env.Flags = 0;
            return "<p>Great success?</p>";
            //return m_WebApp.ReadFile(env, "index.html");
        }

        public void ResetPassword(string email, string token, string newPassword)
        {
            bool success = false;
            UserAccount account = null;
            if (IsValidToken(email, token, out account))
                success = m_AuthenticationService.SetPassword(account.PrincipalID, newPassword);
        
            if (!success)
                m_log.ErrorFormat("[ForgotPasswordService]: Unable to reset password for account uuid:{0}.", account.PrincipalID);
            else
                m_log.InfoFormat("[ForgotPasswordService]: Password reset for account uuid:{0}", account.PrincipalID);
        }

        private bool IsValidToken(string email, string token, out UserAccount account)
        {
            account = m_UserAccountService.GetUserAccount(UUID.Zero, email);
            if (account != null)
                return (m_AuthenticationService.Verify(account.PrincipalID, token, 1));

            return false;
        }

        public string RegionManagementShutdownPostRequest(Environment env)
        {
            m_log.DebugFormat("[WebApp]: RegionManagementShutdownPostRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) && (sinfo.Account.UserLevel >= 200))
            {
                env.Session = sinfo;
                env.Flags = StateFlags.RegionManagementShutdownSuccessful | StateFlags.IsAdmin | StateFlags.IsLoggedIn;

                //FIXME: don't hardcode url, get it from m_GridService
                //TODO: check if server is actually running first
                //TODO: add support for shutdown message parameter from html form
                string url = "http://localhost:9000";
                Hashtable hash = new Hashtable();
                if (m_ServerAdminPassword == null)
                {
                    m_log.Debug("[RegionManagementShutdownPostRequest] No remote admin password was set in .ini file");
                }

                hash["password"] = m_ServerAdminPassword;
                IList paramList = new ArrayList();
                paramList.Add(hash);
                XmlRpcRequest xmlrpcReq = new XmlRpcRequest("admin_shutdown", paramList);

                XmlRpcResponse response = null;
                try
                {
                    response = xmlrpcReq.Send(url, 10000);
                }
                catch (Exception e)
                {
                    m_log.Debug("[WebApp]: Exception " + e.Message);
                }

                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }

            return m_WebApp.ReadFile(env, "index.html");

        }

        public string RegionManagementGetRequest(Environment env)
        {
            m_log.DebugFormat("[Services]: RegionManagementGetRequest()");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo) && (sinfo.Account.UserLevel >= 200))
            {
                List<GridRegion> regions = m_GridService.GetRegionsByName(UUID.Zero, "", 200);

                m_log.DebugFormat("[Services]: There are {0} regions", regions.Count);
                regions.ForEach(delegate(GridRegion gg)
                {
                    m_log.DebugFormat("[Services] {0}", gg.RegionName);
                });

                env.Session = sinfo;
                env.Data = Objectify(regions);
                env.Flags = StateFlags.IsLoggedIn | StateFlags.IsAdmin | StateFlags.RegionManagementForm;
                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            else
            {
                return m_WebApp.ReadFile(env, "index.html");
            }
        }


        // <a href="wifi/..." ...>
        static Regex href = new Regex("(<a\\s+href\\s*=\\s*\\\"(\\S+\\\"))>");
        static Regex action = new Regex("(<form\\s+action\\s*=\\s*\\\"(\\S+\\\")).*>");
        static Regex xmlhttprequest = new Regex("(@@wifi@@(\\S+\\\"))");

        private string PadURLs(Environment env, string sid, string html)
        {
            if ((env.Flags & StateFlags.IsLoggedIn) == 0)
                return html;

            // The user is logged in
            HashSet<string> uris = new HashSet<string>();
            MatchCollection matches_href = href.Matches(html);
            MatchCollection matches_action = action.Matches(html);
            MatchCollection matches_xmlhttprequest = xmlhttprequest.Matches(html);
            m_log.DebugFormat("[WebApp]: Matched uris href={0}, action={1}, xmlhttp={2}", matches_href.Count, matches_action.Count, matches_xmlhttprequest.Count);

            foreach (Match match in matches_href)
            {
                // first group is always the total match
                if (match.Groups.Count > 2)
                {
                    string str = match.Groups[1].Value;
                    string uri = match.Groups[2].Value;
                    if (!uri.StartsWith("http") && !uri.EndsWith(".html") && !uri.EndsWith(".css"))
                        uris.Add(str);
                }
            }
            foreach (Match match in matches_action)
            {
                // first group is always the total match
                if (match.Groups.Count > 2)
                {
                    string str = match.Groups[1].Value;
                    string uri = match.Groups[2].Value;
                    if (!uri.StartsWith("http") && !uri.EndsWith(".html") && !uri.EndsWith(".css"))
                        uris.Add(str);
                }
            }
            foreach (Match match in matches_xmlhttprequest)
            {
                // first group is always the total match
                if (match.Groups.Count > 2)
                {
                    string str = match.Groups[1].Value;
                    string uri = match.Groups[2].Value;
                    if (!uri.StartsWith("http") && !uri.EndsWith(".html") && !uri.EndsWith(".css"))
                        uris.Add(str);
                }
            }

            foreach (string uri in uris)
            {
                string uri2 = uri.Substring(0, uri.Length - 1);
                m_log.DebugFormat("[WebApp]: replacing {0} with {1}", uri, uri2 + "?sid=" + sid + "\"");
                if (!uri.EndsWith("/"))
                    html = html.Replace(uri, uri2 + "/?sid=" + sid + "\"");
                else
                    html = html.Replace(uri, uri2 + "?sid=" + sid + "\"");
            }
            // Remove any @@wifi@@
            html = html.Replace("@@wifi@@", string.Empty);

            return html;
        }

        private void PrintStr(string html)
        {
            foreach (char c in html)
                Console.Write(c);
        }

        private List<object> GetUserList(Environment env, string terms)
        {
            List<UserAccount> accounts = m_UserAccountService.GetUserAccounts(UUID.Zero, terms);
            if (accounts != null)
            {
                m_log.DebugFormat("[WebApp]: GetUserList found {0} users in DB", accounts.Count);
                return Objectify<UserAccount>(accounts);
            }
            else
            {
                m_log.DebugFormat("[WebApp]: GetUserList got null users from DB");
                return null;
            }

        }


        #region Misc

        private void SetServiceURLs(UserAccount account)
        {
            account.ServiceURLs = new Dictionary<string, object>();
            account.ServiceURLs["HomeURI"] = m_WebApp.LoginURL.ToString();
            account.ServiceURLs["InventoryServerURI"] = m_WebApp.LoginURL.ToString();
            account.ServiceURLs["AssetServerURI"] = m_WebApp.LoginURL.ToString();
        }

        private List<object> Objectify<T>(List<T> listOfThings)
        {
            List<object> listOfObjects = new List<object>();
            foreach (T thing in listOfThings)
                listOfObjects.Add(thing);

            return listOfObjects;
        }

        #endregion Misc

    }

    class PasswordRecoveryData
    {
        public string Email;
        public string Token;

        public PasswordRecoveryData(string e, string t)
        {
            Email = e; Token = t;
        }
    }
}
