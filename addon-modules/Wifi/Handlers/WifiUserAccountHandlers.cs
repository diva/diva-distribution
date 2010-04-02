/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using Nini.Config;
using log4net;
using System;
using System.Reflection;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Web;

using System.Collections.Generic;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;

using Environment = Diva.Wifi.Environment;


namespace Diva.Wifi
{
    public class WifiUserAccountGetHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;

        public WifiUserAccountGetHandler(WebApp webapp) :
                base("GET", "/wifi/user/account")
        {
            m_WebApp = webapp;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            // path = /wifi/...
            //m_log.DebugFormat("[Wifi]: path = {0}", path);
            //m_log.DebugFormat("[Wifi]: ip address = {0}", httpRequest.RemoteIPEndPoint);
            //foreach (object o in httpRequest.Query.Keys)
            //    m_log.DebugFormat("  >> {0}={1}", o, httpRequest.Query[o]);

            Request request = WifiUtils.CreateRequest(string.Empty, httpRequest);
            Diva.Wifi.Environment env = new Diva.Wifi.Environment(request);

            string result = m_WebApp.UserAccountGetRequest(env);

            return WifiUtils.StringToBytes(result);

        }

        private string GetResource(string path)
        {
            string[] paramArray = SplitParams(path);
            m_log.DebugFormat("[Wifi]: paramArray length = {0}", paramArray.Length);
            if (paramArray != null && paramArray.Length > 0)
                return paramArray[0];

            return string.Empty;
        }

    }

    public class WifiUserAccountPostHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;

        public WifiUserAccountPostHandler(WebApp webapp) :
            base("POST", "/wifi/user/account")
        {
            m_WebApp = webapp;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            m_log.DebugFormat("[XXX]: query String: {0}", body);
            string resource = GetParam(path);

            try
            {
                Dictionary<string, object> request =
                        ServerUtils.ParseQueryString(body);

                string email = String.Empty;
                string oldpassword = String.Empty;
                string newpassword = String.Empty;
                string newpassword2 = String.Empty;

                if (request.ContainsKey("email"))
                    email = request["email"].ToString();
                if (request.ContainsKey("oldpassword"))
                    oldpassword = request["oldpassword"].ToString();
                if (request.ContainsKey("newpassword"))
                    newpassword = request["newpassword"].ToString();
                if (request.ContainsKey("newpassword2"))
                    newpassword2 = request["newpassword2"].ToString();

                Request req = WifiUtils.CreateRequest(string.Empty, httpRequest);
                Diva.Wifi.Environment env = new Diva.Wifi.Environment(req);

                string result = m_WebApp.UserAccountPostRequest(env, email, oldpassword, newpassword, newpassword2);

                return WifiUtils.StringToBytes(result);

            }
            catch (Exception e)
            {
                m_log.DebugFormat("[USER ACCOUNT POST HANDLER]: Exception {0}",  e);
            }

            return WifiUtils.FailureResult();

        }

    }

}
