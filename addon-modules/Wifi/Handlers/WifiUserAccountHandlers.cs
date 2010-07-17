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
            httpResponse.ContentType = "text/html";
            string resource = GetParam(path);
            //m_log.DebugFormat("[USER ACCOUNT HANDLER GET]: resource {0}", resource);

            Request request = WifiUtils.CreateRequest(string.Empty, httpRequest);
            Diva.Wifi.Environment env = new Diva.Wifi.Environment(request);

            string result = string.Empty;
            UUID userID = UUID.Zero;
            if (resource == string.Empty || resource == "/")
            {
                result = m_WebApp.Services.NewAccountGetRequest(env);
            }
            else
            {
                //UUID.TryParse(resource.Trim(new char[] {'/'}), out userID);
                if (resource.Trim(new char[] {'/'}).StartsWith("edit"))
                    result = m_WebApp.Services.UserAccountGetRequest(env, userID);
            }

            return WifiUtils.StringToBytes(result);

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

            httpResponse.ContentType = "text/html";

            string resource = GetParam(path);
            //m_log.DebugFormat("[USER ACCOUNT HANDLER POST]: resource {0} query: {1}", resource, body);

            Dictionary<string, object> request =
                    ServerUtils.ParseQueryString(body);

            if (SplitParams(path).Length >= 1) //  userID given, update account (PUT)
            {
                UUID userID = UUID.Zero;
                if (UUID.TryParse(resource.Trim(new char[] { '/' }), out userID))
                    return UpdateAccount(resource, httpRequest, userID, request);

                return WifiUtils.FailureResult();
            }

            // else create a new account (true POST)
            return CreateAccount(resource, httpRequest, request);
        }

        private byte[] CreateAccount(string resource, OSHttpRequest httpRequest, Dictionary<string, object> request)
        {
            string first = String.Empty;
            string last = String.Empty;
            string email = String.Empty;
            string password = String.Empty;
            string password2 = String.Empty;
            AvatarType avatar = AvatarType.Neutral;

            if (request.ContainsKey("first") && WifiUtils.IsValidName(request["first"].ToString()))
                first = request["first"].ToString();
            if (request.ContainsKey("last") && WifiUtils.IsValidName(request["last"].ToString()))
                last = request["last"].ToString();
            if (request.ContainsKey("email") && WifiUtils.IsValidEmail(request["email"].ToString()))
                email = request["email"].ToString();
            if (request.ContainsKey("password"))
                password = request["password"].ToString();
            if (request.ContainsKey("password2"))
                password2 = request["password2"].ToString();
            if (request.ContainsKey("avatar"))
            {
                if ((string)request["avatar"] == "Female")
                    avatar = AvatarType.Female;
                else if ((string)request["avatar"] == "Male")
                    avatar = AvatarType.Male;
                else
                    avatar = AvatarType.Neutral;
            }

            Request req = WifiUtils.CreateRequest(resource, httpRequest);
            Diva.Wifi.Environment env = new Diva.Wifi.Environment(req);

            string result = m_WebApp.Services.NewAccountPostRequest(env, first, last, email, password, password2, avatar);

            return WifiUtils.StringToBytes(result);
        }

        private byte[] UpdateAccount(string resource, OSHttpRequest httpRequest, UUID userID, Dictionary<string, object> request)
        {
            try
            {
                string email = String.Empty;
                string oldpassword = String.Empty;
                string newpassword = String.Empty;
                string newpassword2 = String.Empty;

                if (request.ContainsKey("email") && WifiUtils.IsValidEmail(request["email"].ToString()))
                    email = request["email"].ToString();
                if (request.ContainsKey("oldpassword"))
                    oldpassword = request["oldpassword"].ToString();
                if (request.ContainsKey("newpassword"))
                    newpassword = request["newpassword"].ToString();
                if (request.ContainsKey("newpassword2"))
                    newpassword2 = request["newpassword2"].ToString();

                Request req = WifiUtils.CreateRequest(resource, httpRequest);
                Diva.Wifi.Environment env = new Diva.Wifi.Environment(req);

                string result = m_WebApp.Services.UserAccountPostRequest(env, userID, email, oldpassword, newpassword, newpassword2);

                return WifiUtils.StringToBytes(result);

            }
            catch (Exception e)
            {
                Util.PrintCallStack();
                m_log.DebugFormat("[USER ACCOUNT POST HANDLER]: Exception {0}", e.StackTrace);
            }

            return WifiUtils.FailureResult();
        }

    }

}
