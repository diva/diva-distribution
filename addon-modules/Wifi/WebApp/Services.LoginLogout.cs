using System;
using System.Collections.Generic;

using log4net;
using OpenMetaverse;

using OpenSim.Services.Interfaces;

namespace Diva.Wifi
{
    public partial class Services
    {
        public string LoginRequest(Environment env, string first, string last, string password)
        {
            if (!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[Wifi]: warning: someone is trying to access LoginRequest and Wifi isn't isntalled!");
                return m_WebApp.ReadFile(env, "index.html");
            }

            m_log.DebugFormat("[Wifi]: LoginRequest {0} {1}", first, last);
            Request request = env.Request;
            string encpass = OpenSim.Framework.Util.Md5Hash(password);

            UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, first, last);
            if (account == null)
            {
                env.State = State.FailedLogin;
                return m_WebApp.ReadFile(env, "index.html");
            }

            string authtoken = m_AuthenticationService.Authenticate(account.PrincipalID, encpass, 30);
            if (authtoken == string.Empty)
            {
                env.State = State.FailedLogin;
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
            env.Flags = Flags.IsLoggedIn;
            env.State = State.SuccessfulLogin;
            return PadURLs(env, authtoken, m_WebApp.ReadFile(env, "index.html"));
        }

        public string LogoutRequest(Environment env)
        {
            m_log.DebugFormat("[Wifi]: LogoutRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo))
            {
                m_Sessions.Remove(sinfo.Sid);
                m_AuthenticationService.Release(sinfo.Account.PrincipalID, sinfo.Sid);
            }

            env.State = State.Default;
            return m_WebApp.ReadFile(env, "index.html");
        }

    }
}
