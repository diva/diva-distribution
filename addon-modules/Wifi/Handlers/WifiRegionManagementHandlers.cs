/**
 * Copyright (c) Crista Lopes (aka Diva) and Ryan Hsu. All rights reserved.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using Nini.Config;
using log4net;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Services.AuthenticationService;
using OpenSim.Services.InventoryService;

using Diva.Wifi.WifiScript;
using Environment = Diva.Wifi.Environment;
using OpenSim.Server.Base;
namespace Diva.Wifi
{

    /// <summary>
    /// The handler for the HTTP GET method on the resource /wifi/admin/regions
    /// </summary>
    public class WifiRegionManagementGetHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;

        public WifiRegionManagementGetHandler(WebApp webapp) :
            base("GET", "/wifi/admin/regions")
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
                result = m_WebApp.Services.RegionManagementGetRequest(env);

            return WifiUtils.StringToBytes(result);

        }

    }

    /// <summary>
    /// The handler for the HTTP POST method on the resource /wifi/admin/regions
    /// </summary>
    public class WifiRegionManagementPostHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;

        public WifiRegionManagementPostHandler(WebApp webapp) :
            base("POST", "/wifi/admin/regions")
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

                string broadcast_message = String.Empty;
                    if (postdata.ContainsKey("message"))
                        broadcast_message = postdata["message"].ToString();

                Request req = WifiUtils.CreateRequest(resource, httpRequest);
                Diva.Wifi.Environment env = new Diva.Wifi.Environment(req);

                string result = string.Empty;
                if (resource.Equals("/") || resource.Equals(string.Empty))
                {

                }
                else if (resource.StartsWith("/shutdown"))
                {
                    result = m_WebApp.Services.RegionManagementShutdownPostRequest(env);
                }
                else if (resource.StartsWith("/restart"))
                {
                    result = m_WebApp.Services.RegionManagementRestartPostRequest(env);
                }
                else if (resource.StartsWith("/broadcast"))
                {
                    result = m_WebApp.Services.RegionManagementBroadcastPostRequest(env, broadcast_message);
                }
                return WifiUtils.StringToBytes(result);

            }
            catch (Exception e)
            {
                m_log.DebugFormat("[REGION MANAGEMENT POST HANDLER]: Exception {0}", e);
            }

            return WifiUtils.FailureResult();

        }

    }

}
