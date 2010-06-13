using System;

using log4net;
using OpenMetaverse;

using OpenSim.Services.Interfaces;

namespace Diva.Wifi
{
    public partial class Services
    {
        public string InstallGetRequest(Environment env)
        {
            env.State = State.InstallForm;
            return m_WebApp.ReadFile(env, "index.html");
        }

        public string InstallPostRequest(Environment env, string password, string password2)
        {
            if (m_WebApp.IsInstalled)
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
                    env.State = State.InstallFormResponse;
                }

            }

            return m_WebApp.ReadFile(env, "index.html");
        }
    }
}
