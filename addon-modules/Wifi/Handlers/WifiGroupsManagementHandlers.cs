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

using Diva.Utils;

using Environment = Diva.Utils.Environment;


namespace Diva.Wifi
{
    /// <summary>
    /// The handler for the HTTP GET method on the resource /wifi/admin/groups
    /// </summary>
    public class WifiGroupsManagementGetHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;

        public WifiGroupsManagementGetHandler(WebApp webapp) :
                base("GET", "/wifi/admin/groups")
        {
            m_WebApp = webapp;
        }

        public override byte[] Handle(string path, Stream requestData,
                IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
        {
            // path = /wifi/...
            //m_log.DebugFormat("[Wifi]: path = {0}", path);

            // This is the content type of the response. Don't forget to set it to this in all your handlers.
            httpResponse.ContentType = "text/html";

            string resource = GetParam(path);
            //m_log.DebugFormat("[XXX]: resource {0}", resource);
            Request request = RequestFactory.CreateRequest(resource, httpRequest, Localization.GetLanguageInfo(httpRequest.Headers.Get("accept-language")));
            Environment env = new Environment(request);

            string result = string.Empty;
            if (resource.StartsWith("/edit"))
            {
                // client invoked /wifi/admin/groups/edit, possibly with the UUID parameter after
                UUID groupID = UUID.Zero;
                // SplitParams(path) returns an array of whatever parameters come after the path.
                // In this case it should return "edit" and "<uuid>"; we want "<uuid>", so [1]
                string[] pars = SplitParams(path);
                if (pars.Length >= 2)
                {
                    // indeed, client invoked /wifi/admin/groups/edit/<uuid>
                    // let's grab that uuid 
                    UUID.TryParse(pars[1], out groupID);
                    result = m_WebApp.Services.GroupsEditGetRequest(env, groupID);
                }
            }
            else if (resource.StartsWith("/delete"))
            {
                // client invoked /wifi/admin/groups/delete, possibly with the UUID parameter after
                UUID groupID = UUID.Zero;
                // SplitParams(path) returns an array of whatever parameters come after the path.
                // In this case it should return "delete" and "<uuid>"; we want "<uuid>", so [1]
                string[] pars = SplitParams(path);
                if (pars.Length >= 2)
                {
                    // indeed, client invoked /wifi/admin/groups/delete/<uuid>
                    // let's grab that uuid 
                    UUID.TryParse(pars[1], out groupID);
                    result = m_WebApp.Services.GroupsDeleteGetRequest(env, groupID);
                }
            }

            if (string.IsNullOrEmpty(result))
                result = m_WebApp.Services.GroupsManagementGetRequest(env);

            return WebAppUtils.StringToBytes(result);

        }

    }

    /// <summary>
    /// The handler for the HTTP POST method on the resource /wifi/admin/groups
    /// </summary>
    public class WifiGroupsManagementPostHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;

        public WifiGroupsManagementPostHandler(WebApp webapp) :
            base("POST", "/wifi/admin/groups")
        {
            m_WebApp = webapp;
        }

        public override byte[] Handle(string path, Stream requestData,
                IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
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

                Request req = RequestFactory.CreateRequest(resource, httpRequest, Localization.GetLanguageInfo(httpRequest.Headers.Get("accept-language")));
                Environment env = new Environment(req);

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
                    // The client invoked /wifi/admin/groups/edit, possibly with the UUID parameter after
                    UUID groupID = UUID.Zero;
                    string[] pars = SplitParams(path);
                    if ((pars.Length >= 2) && UUID.TryParse(pars[1], out groupID))
                    {
                        // Indeed the client invoked /wifi/admin/groups/edit/<uuid>, and we got it already in userID (above)
                        string name = string.Empty, charter = string.Empty;
                        if (postdata.ContainsKey("name"))
                            name = postdata["name"].ToString();
                        if (postdata.ContainsKey("charter"))
                            charter = postdata["charter"].ToString();

                        result = m_WebApp.Services.GroupsEditPostRequest(env, groupID, name, charter);
                    }
                }
                else if (resource.StartsWith("/delete"))
                {
                    // The client invoked /wifi/admin/groups/edit, possibly with the UUID parameter after
                    UUID groupID = UUID.Zero;
                    string[] pars = SplitParams(path);
                    if ((pars.Length >= 2) && UUID.TryParse(pars[1], out groupID))
                    {
                        // Indeed the client invoked /wifi/admin/groups/edit/<uuid>, and we got it already in userID (above)
                        string form = string.Empty;
                        result = m_WebApp.Services.GroupsDeletePostRequest(env, groupID);
                         
                    }
                }

                return WebAppUtils.StringToBytes(result);

            }
            catch (Exception e)
            {
                m_log.DebugFormat("[USER ACCOUNT POST HANDLER]: Exception {0}",  e);
            }

            return WebAppUtils.FailureResult();

        }

    }

}
