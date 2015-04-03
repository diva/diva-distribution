/**
 * Copyright (c) Crista Lopes (aka Diva) and Marck. All rights reserved.
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
using System.IO;
using Diva.Wifi.WifiScript;
using Diva.Utils;
using Environment = Diva.Utils.Environment;

namespace Diva.Wifi
{
    public partial class Services
    {
        public string DefaultRequest(Environment env)
        {
            //m_log.DebugFormat("[Wifi]: DefaultRequest from {0}", env.TheRequest.IPEndPoint.ToString());

            SessionInfo sinfo;
            if (TryGetSessionInfo(env.TheRequest, out sinfo))
            {
                env.Session = sinfo;
                env.Flags = Flags.IsLoggedIn;
                env.State = State.Default;
                return WebAppUtils.PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "splash.html"));
            }

            string resourcePath = Localization.LocalizePath(env, "splash.html");
            Processor p = new Processor(m_WebApp.WifiScriptFace, env);
            return p.Process(WebAppUtils.ReadTextResource(new string[] {resourcePath}, WebApp.MissingPage));
        }
    }
}
