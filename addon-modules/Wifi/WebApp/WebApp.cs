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
using OpenSim.Services.AuthenticationService;
using OpenSim.Services.UserAccountService;
using OpenSim.Services.InventoryService;

using Environment = Diva.Wifi.Environment;

namespace Diva.Wifi
{
    public class WebApp : IWebApp
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IUserAccountService m_UserAccountService;
        private IAuthenticationService m_AuthenticationService;
        private IInventoryService m_InventoryService;

        // Sessions
        private Dictionary<string, SessionInfo> m_Sessions = new Dictionary<string, SessionInfo>();

        public WebApp(IConfigSource config, IHttpServer server)
        {
            m_log.Debug("[WebApp]: Starting");
            m_UserAccountService = new UserAccountService(config);
            m_AuthenticationService = new PasswordAuthenticationService(config);
            m_InventoryService = new InventoryService(config);

            server.AddStreamHandler(new WifiGetHandler(this));
            server.AddStreamHandler(new WifiLoginHandler(this));
            server.AddStreamHandler(new WifiLogoutHandler(this));
            server.AddStreamHandler(new WifiUserAccountGetHandler(this));
            server.AddStreamHandler(new WifiUserAccountPostHandler(this));
            server.AddStreamHandler(new WifiNewAccountGetHandler(this));
            server.AddStreamHandler(new WifiNewAccountPostHandler(this));

        }

        #region IWebApp
        public string LoginRequest(Environment env, string first, string last, string password)
        {
            m_log.DebugFormat("[WebApp]: LoginRequest {0} {1}", first, last);
            Request request = env.Request;
            string encpass = Util.Md5Hash(password);

            UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, first, last);
            if (account == null)
            {
                env.Flags = StateFlags.FailedLogin;
                return ReadFile(env, "index.html");
            }

            string authtoken = m_AuthenticationService.Authenticate(account.PrincipalID, encpass, 30);
            if (authtoken == string.Empty)
            {
                env.Flags = StateFlags.FailedLogin;
                return ReadFile(env, "index.html");
            }

            // Successful login
            SessionInfo sinfo;
            sinfo.IpAddress = request.IPEndPoint.Address.ToString();
            sinfo.Sid = authtoken;
            sinfo.Account = account;
            m_Sessions.Add(authtoken, sinfo);
            env.Request.Query["sid"] = authtoken;

            env.Flags = StateFlags.IsLoggedIn | StateFlags.SuccessfulLogin;
            return PadURLs(env, authtoken, ReadFile(env, "index.html"));
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
            return ReadFile(env, "index.html");
        }

        public string UserAccountGetRequest(Environment env)
        {
            m_log.DebugFormat("[WebApp]: UserAccountGetRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo))
            {
                env.Flags = StateFlags.IsLoggedIn | StateFlags.UserAccountForm;
                return PadURLs(env, sinfo.Sid, ReadFile(env, "index.html"));
            }
            else
            {
                return ReadFile(env, "index.html");
            }
        }

        public string UserAccountPostRequest(Environment env, string email, string oldpassword, string newpassword, string newpassword2)
        {
            m_log.DebugFormat("[WebApp]: UserAccountPostRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo))
            {
                bool updated = false;
                if (email != string.Empty && email.Contains("@") && sinfo.Account.Email != email)
                {
                    sinfo.Account.Email = email;
                    m_UserAccountService.StoreUserAccount(sinfo.Account);
                    updated = true;
                }

                string encpass = Util.Md5Hash(oldpassword);
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
                    return PadURLs(env, sinfo.Sid, ReadFile(env, "index.html"));
                }

                // nothing was updated, really
                env.Flags = StateFlags.IsLoggedIn | StateFlags.UserAccountForm;
                return PadURLs(env, sinfo.Sid, ReadFile(env, "index.html"));
            }
            else
            {
                m_log.DebugFormat("[WebApp]: Failed to get session info");
                return ReadFile(env, "index.html");
            }
        }

        public string NewAccountGetRequest(Environment env)
        {
            m_log.DebugFormat("[WebApp]: NewAccountGetRequest");
            Request request = env.Request;

            env.Flags = StateFlags.NewAccountForm;
            return ReadFile(env, "index.html");
        }

        public string NewAccountPostRequest(Environment env, string first, string last, string email, string password, string password2)
        {
            m_log.DebugFormat("[WebApp]: NewAccountPostRequest");
            Request request = env.Request;

            if ((password != string.Empty) && (password == password2))
            {
                UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, first, last);
                if (account == null)
                {
                    // Create the account
                    account = new UserAccount(UUID.Zero, first, last, email);
                    account.ServiceURLs = new Dictionary<string, object>();
                    account.ServiceURLs["HomeURI"] = Environment.StaticVariables["LoginURL"].ToString();
                    account.ServiceURLs["InventoryServerURI"] = Environment.StaticVariables["LoginURL"].ToString();
                    account.ServiceURLs["AssetServerURI"] = Environment.StaticVariables["LoginURL"].ToString();
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

            return ReadFile(env, "index.html");

        }

        #endregion

        public string GetContent(Environment env)
        {
            m_log.DebugFormat("[WebApp]: GetContent, flags {0}", env.Flags);
            if ((env.Flags & StateFlags.FailedLogin) != 0)
            {
                return "Login failed";
            }
            if ((env.Flags & StateFlags.SuccessfulLogin) != 0)
            {
                if (Environment.StaticVariables.ContainsKey("GridName"))
                    return "Welcome to " + Environment.StaticVariables["GridName"] + "!";
                else
                    return "Welcome!";
            }

            if ((env.Flags & StateFlags.NewAccountForm) != 0)
                return ReadFile(env, "newaccountform.html");
            if ((env.Flags & StateFlags.NewAccountFormResponse) != 0)
                return "Your account has been created.";

            if ((env.Flags & StateFlags.IsLoggedIn) != 0)
            {
                if ((env.Flags & StateFlags.UserAccountForm) != 0)
                    return ReadFile(env, "useraccountform.html");
                if ((env.Flags & StateFlags.UserAccountFormResponse) != 0)
                    return "Your account has been updated.";
            }

            return string.Empty;
        }

        public string GetMainMenu(Environment env)
        {
            SessionInfo sinfo;
            if (TryGetSessionInfo(env.Request, out sinfo))
            {
                if (sinfo.Account.UserLevel >= 200) // Admin
                    return ReadFile(env, "main-menu-admin.html");

                return ReadFile(env, "main-menu-users.html");
            }

            return ReadFile(env, "main-menu.html");
        }


        public string GetLoginLogout(Environment env)
        {
            SessionInfo sinfo;
            if (TryGetSessionInfo(env.Request, out sinfo))
                return ReadFile(env, "logout.html");

            return ReadFile(env, "login.html");
        }

        public string GetUserName(Environment env)
        {
            SessionInfo sinfo;
            if (TryGetSessionInfo(env.Request, out sinfo))
            {
                return sinfo.Account.FirstName + " " + sinfo.Account.LastName;
            }

            return "Who are you?";
        }

        public string GetUserEmail(Environment env)
        {
            SessionInfo sinfo;
            if (TryGetSessionInfo(env.Request, out sinfo))
            {
                if (sinfo.Account.Email == string.Empty)
                    return "No email on file";

                return sinfo.Account.Email ;
            }

            return "Who are you?";
        }

        public string GetUserImage(Environment env)
        {
            SessionInfo sinfo;
            if (TryGetSessionInfo(env.Request, out sinfo))
            {
                // TODO
                return "/wifi/images/temporaryphoto1.jpg";
            }

            // TODO
            return "/wifi/images/temporaryphoto1.jpg";
        }


        private bool TryGetSessionInfo(Request request, out SessionInfo sinfo)
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

        // <a href="wifi/..." ...>
        static Regex href = new Regex("(<a\\s+href\\s*=\\s*\\\"(\\S+))\\\">");
        static Regex action = new Regex("(<form\\s+action\\s*=\\s*\\\"(\\S+))\\\".*>");

        private string PadURLs(Environment env, string sid, string html)
        {
            if ((env.Flags & StateFlags.IsLoggedIn) == 0)
                return html;

            // The user is logged in
            HashSet<string> uris = new HashSet<string>();
            MatchCollection matches_href = href.Matches(html);
            m_log.DebugFormat("[WebApp]: Matched uris {0}", matches_href.Count);
            MatchCollection matches_action = action.Matches(html);
            m_log.DebugFormat("[WebApp]: Matched uris {0}", matches_action.Count);
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

            
            foreach (string uri in uris)
            {
                m_log.DebugFormat("[WebApp]: replacing {0} with {1}", uri, uri + "?sid=" + sid);
                if (!uri.EndsWith("/"))
                    html = html.Replace(uri, uri + "/?sid=" + sid);
                else
                    html = html.Replace(uri, uri + "?sid=" + sid);
            }
            return html;
        }

        #region read html files
       
        private string ReadFile(Environment env, string path)
        {
            string file = Path.Combine(WifiUtils.DocsPath, path);
            try
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    string content = sr.ReadToEnd();
                    Processor p = new Processor(env);
                    return p.Process(content);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[WebApp]: Exception on ReadFile {0}: {1}", path, e.Message);
                return string.Empty;
            }
        }
        #endregion
    }

    struct SessionInfo
    {
        public string Sid;
        public string IpAddress;
        public UserAccount Account;
    }
}
