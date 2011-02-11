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
                m_log.DebugFormat("[Wifi]: warning: someone is trying to change the god password in InstallPostRequest!");
                return m_WebApp.ReadFile(env, "index.html");
            }

            m_log.DebugFormat("[Wifi]: InstallPostRequest");
            Request request = env.Request;

            if (password == password2)
            {
                UserAccount god = m_UserAccountService.GetUserAccount(UUID.Zero, m_WebApp.AdminFirst, m_WebApp.AdminLast);
                if (god != null)
                {
                    m_AuthenticationService.SetPassword(god.PrincipalID, password);
                    // And this finishes the installation procedure
                    m_WebApp.IsInstalled = true;
                    NotifyWithoutButton(env,
                        string.Format(_("Your Wifi has been installed. The administrator account is {0} {1}", env),
                        m_WebApp.AdminFirst, m_WebApp.AdminLast));
                }
            }
            return m_WebApp.ReadFile(env, "index.html");
        }
    }
}
