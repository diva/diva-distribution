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
    /// <summary>
    /// The handler for the HTTP GET method on the resource /wifi/admin/users
    /// </summary>
    public class WifiUserManagementGetHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;

        public WifiUserManagementGetHandler(WebApp webapp) :
                base("GET", "/wifi/admin/users")
        {
            m_WebApp = webapp;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            // path = /wifi/...
            //m_log.DebugFormat("[Wifi]: path = {0}", path);

            // This is the content type of the response. Don't forget to set it to this in all your handlers.
            httpResponse.ContentType = "text/html";

            string resource = GetParam(path);
            //m_log.DebugFormat("[XXX]: resource {0}", resource);
            Request request = WifiUtils.CreateRequest(resource, httpRequest);
            Diva.Wifi.Environment env = new Diva.Wifi.Environment(request);

            string result = string.Empty;
            if (resource.Equals("/") || resource.Equals(string.Empty))
                // client invoked /wifi/admin/users/ with no further parameters
                result = m_WebApp.Services.UserManagementGetRequest(env);

            else if (resource.StartsWith("/edit"))
            {
                // client invoked /wifi/admin/users/edit, possibly with the UUID parameter after
                UUID userID = UUID.Zero;
                // SplitParams(path) returns an array of whatever parameters come after the path.
                // In this case it should return "edit" and "<uuid>"; we want "<uuid>", so [1]
                string[] pars = SplitParams(path);
                if (pars.Length >= 2)
                {
                    // indeed, client invoked /wifi/admin/users/edit/<uuid>
                    // let's grab that uuid 
                    UUID.TryParse(pars[1], out userID);
                    result = m_WebApp.Services.UserEditGetRequest(env, userID);
                }
            }

            return WifiUtils.StringToBytes(result);

        }

    }

    /// <summary>
    /// The handler for the HTTP POST method on the resource /wifi/admin/users
    /// </summary>
    public class WifiUserManagementPostHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;

        public WifiUserManagementPostHandler(WebApp webapp) :
            base("POST", "/wifi/admin/users")
        {
            m_WebApp = webapp;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            // It's a POST, so we need to read the data on the stream, the lines after the blank line
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            httpResponse.ContentType = "text/html";

            string resource = GetParam(path);
            //m_log.DebugFormat("[XXX]: query String: {0}; resource: {1}", body, resource);

            try
            {
                // Here the data on the stream is transformed into a nice dictionary of keys & values
                Dictionary<string, object> postdata =
                        ServerUtils.ParseQueryString(body);

                Request req = WifiUtils.CreateRequest(resource, httpRequest);
                Diva.Wifi.Environment env = new Diva.Wifi.Environment(req);

                string result = string.Empty;
                if (resource.Equals("/") || resource.Equals(string.Empty))
                {
                    // The client invoked /wifi/admin/users/
                    string terms = String.Empty;
                    if (postdata.ContainsKey("terms"))
                        terms = postdata["terms"].ToString();

                    result = m_WebApp.Services.UserSearchPostRequest(env, terms);
                }
                else if (resource.StartsWith("/edit"))
                {
                    // The client invoked /wifi/admin/users/edit, possibly with the UUID parameter after
                    UUID userID = UUID.Zero;
                    string[] pars = SplitParams(path);
                    if ((pars.Length >= 2) && UUID.TryParse(pars[1], out userID))
                    {
                        // Indeed the client invoked /wifi/admin/users/edit/<uuid>, and we got it already in userID (above)
                        string form = string.Empty;
                        if (postdata.ContainsKey("form"))
                            form = postdata["form"].ToString();
                        if (form == "1")
                        {
                            string first = string.Empty, last = string.Empty, email = string.Empty, title = string.Empty;
                            int level = 0, flags = 0;
                            if (postdata.ContainsKey("first") && WifiUtils.IsValidName(postdata["first"].ToString()))
                                first = postdata["first"].ToString();
                            if (postdata.ContainsKey("last") && WifiUtils.IsValidName(postdata["last"].ToString()))
                                last = postdata["last"].ToString();
                            if (postdata.ContainsKey("email") && WifiUtils.IsValidEmail(postdata["email"].ToString()))
                                email = postdata["email"].ToString();
                            if (postdata.ContainsKey("title"))
                                title = postdata["title"].ToString();
                            if (postdata.ContainsKey("level"))
                                Int32.TryParse(postdata["level"].ToString(), out level);
                            if (postdata.ContainsKey("flags"))
                                Int32.TryParse(postdata["flags"].ToString(), out flags);

                            result = m_WebApp.Services.UserEditPostRequest(env, userID, first, last, email, level, flags, title);
                        }
                        else if (form == "2")
                        {
                            string password = string.Empty;
                            if (postdata.ContainsKey("password"))
                            {
                                password = postdata["password"].ToString();
                                result = m_WebApp.Services.UserEditPostRequest(env, userID, password);
                            }
                        }
                    }
                }

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
