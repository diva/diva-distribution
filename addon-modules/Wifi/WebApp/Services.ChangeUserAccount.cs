using System;
using System.Collections.Generic;

using log4net;
using OpenMetaverse;

using OpenSim.Services.Interfaces;

namespace Diva.Wifi
{
    public partial class Services
    {
        public string UserAccountGetRequest(Environment env, UUID userID)
        {
            if (!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[Wifi]: warning: someone is trying to access UserAccountGetRequest and Wifi isn't isntalled!");
                return m_WebApp.ReadFile(env, "index.html");
            }

            m_log.DebugFormat("[Wifi]: UserAccountGetRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo))
            {
                env.Session = sinfo;
                List<object> loo = new List<object>();
                loo.Add(sinfo.Account);
                env.Data = loo;
                env.Flags = Flags.IsLoggedIn;
                env.State = State.UserAccountForm;
                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            else
            {
                return m_WebApp.ReadFile(env, "index.html");
            }
        }

        public string UserAccountPostRequest(Environment env, UUID userID, string email, string oldpassword, string newpassword, string newpassword2)
        {
            if (!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[Wifi]: warning: someone is trying to access UserAccountPostRequest and Wifi isn't isntalled!");
                return m_WebApp.ReadFile(env, "index.html");
            }
            m_log.DebugFormat("[Wifi]: UserAccountPostRequest");
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
                    env.Flags = Flags.IsLoggedIn;
                    env.State = State.UserAccountFormResponse;
                    m_log.DebugFormat("[Wifi]: Updated account for user {0}", sinfo.Account.Name);
                    return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
                }

                // nothing was updated, really
                env.Flags = Flags.IsLoggedIn;
                env.State = State.UserAccountForm;
                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            else
            {
                m_log.DebugFormat("[Wifi]: Failed to get session info");
                return m_WebApp.ReadFile(env, "index.html");
            }
        }

    }
}
