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
using System.Collections.Generic;

using log4net;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Services.Interfaces;

using Diva.Utils;
using Diva.OpenSimServices;
using Environment = Diva.Utils.Environment;

namespace Diva.Wifi
{
    public partial class Services
    {
        class TOSData
        {
            public string UserID = string.Empty;
            public string SessionID = string.Empty;
        }

        public string TOSGetRequest(Environment env, string userID)
        {
            if (!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[Wifi]: warning: someone is trying to access UserAccountGetRequest and Wifi isn't isntalled!");
                return m_WebApp.ReadFile(env, "index.html");
            }

            m_log.DebugFormat("[Wifi]: TOSGetRequest");
            Request request = env.TheRequest;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo))
            {
                env.Session = sinfo;

                TOSData d = new TOSData();
                d.UserID = userID;
                d.SessionID = sinfo.Sid;
                List<object> loo = new List<object>();
                loo.Add(d);
                env.Data = loo;

                if (sinfo.Account != null)
                    env.Flags = Flags.IsLoggedIn;
                else
                    env.Flags = Flags.IsValidSession;

                env.State = State.GetTOS;
                return WebAppUtils.PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            else
            {
                return m_WebApp.ReadFile(env, "index.html");
            }
        }

        public string TOSPostRequest(Environment env, string action, string userID, string sessionID)
        {
            if (!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[Wifi]: warning: someone is trying to access InventoryPostRequest and Wifi isn't installed!");
                return m_WebApp.ReadFile(env, "index.html");
            }

            m_log.DebugFormat("[Wifi]: TOSPostRequest {0} {1} {2}", action, userID, sessionID);
            Request request = env.TheRequest;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo))
            {
                env.Session = sinfo;

                if (action == "accept")
                {
                    AcceptTOS(env, userID);
                    if (sinfo.Account != null)
                        env.Flags = Flags.IsLoggedIn;
                    else
                        env.Flags = Flags.IsValidSession;

                    env.State = State.AcceptTOS;

                }
                else
                {
                    TOSData d = new TOSData();
                    d.UserID = userID;
                    d.SessionID = sinfo.Sid;
                    List<object> loo = new List<object>();
                    loo.Add(d);
                    env.Data = loo;

                    if (sinfo.Account != null)
                        env.Flags = Flags.IsLoggedIn;
                    else
                        env.Flags = Flags.IsValidSession;

                    
                    env.State = State.GetTOS;
                }

                if (sinfo.Account != null)
                    env.Flags = Flags.IsLoggedIn;

                return WebAppUtils.PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            else
            {
                m_log.DebugFormat("[Wifi]: no session found");
                return m_WebApp.ReadFile(env, "index.html");
            }

        }

        private void AcceptTOS(Environment env, string userID)
        {
            try
            {
                DGridUserInfo info = (DGridUserInfo)m_GridUserService.GetGridUserInfo(userID);
                if (info != null && info.TOS == string.Empty)
                {
                    DateTime dt = DateTime.Now;
                    info.TOS = env.Session.IpAddress + " " + dt.ToString("yyyy-MM-dd") + " " + dt.ToString("HH:mm:ss");
                    m_GridUserService.StoreTOS(info);
                }
                else if (info == null)
                    m_log.WarnFormat("[Wifi]: info not found for user {0}", userID);
            }
            catch (InvalidCastException)
            {
                m_log.Warn("[Wifi]: This module isn't properly configured. Use Diva.OpenSimServices");
            }
        }

    }
}