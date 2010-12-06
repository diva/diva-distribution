/**
 * Copyright (c) Marcus Kirsch (aka Marck). All rights reserved.
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

using System.Collections.Generic;

namespace Diva.Wifi
{
    public partial class Services
    {
        public string NotifyRequest(Environment env)
        {
            m_log.Debug("[Wifi]: NotifyRequest");

            SessionInfo sinfo;
            if (TryGetSessionInfo(env.Request, out sinfo))
            {
                env.Session = sinfo;
                env.Flags = Flags.IsLoggedIn;
                if (sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel)
                    env.Flags |= Flags.IsAdmin;

                return sinfo.Notify.FollowUp(env);
            }
            m_log.Debug("[Wifi]: No session info with NotifyRequest");
            return m_WebApp.ReadFile(env, "index.html");
        }

        public void NotifyWithoutButton(Environment env, string message)
        {
            Notify(env, message, string.Empty, null);
        }
        public void NotifyOK(Environment env, string message, ServiceCall followUp)
        {
            Notify(env, message, _("OK", env), followUp);
        }
        public void Notify(Environment env, string message, string buttonText, ServiceCall followUp)
        {
            NotifyWithRedirect(env, message, buttonText, -1, string.Empty, followUp);
        }
        public void NotifyWithRedirect(Environment env, string message, string buttonText,
            int redirectSeconds, string redirectUrl, ServiceCall followUp)
        {
            env.Data = new List<object>();
            env.Data.Add(new Notification(message, buttonText));
            env.State = State.Notification;
            SessionInfo sinfo = env.Session;
            if (sinfo.Sid != null && m_Sessions.Contains(sinfo.Sid))
            {
                sinfo.Notify.FollowUp = followUp;
                sinfo.Notify.RedirectUrl = redirectUrl; // used in WifiScriptFace.GetRefresh()
                sinfo.Notify.RedirectDelay = redirectSeconds;
                m_Sessions.Update(sinfo.Sid, sinfo, m_WebApp.SessionTimeout);
                env.Session = sinfo;
            }
        }

        public delegate string ServiceCall(Environment env);

        public struct NotificationData
        {
            public ServiceCall FollowUp;
            public string RedirectUrl;
            public int RedirectDelay;
        }

        class Notification
        {
            public string Message;
            public string ButtonText;

            public Notification(string message, string button)
            {
                Message = message;
                ButtonText = button;
            }
        }
    }
}