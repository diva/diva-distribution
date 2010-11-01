/**
 * Copyright (c) Marck. All rights reserved.
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
using System.IO;
using System.Reflection;

using log4net;
using OpenMetaverse;

using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Base;

namespace Diva.Wifi
{
    /// <summary>
    /// The handler for the HTTP GET method on the resource /wifi/linkregion
    /// </summary>
    public class WifiHyperlinkGetHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;

        public WifiHyperlinkGetHandler(WebApp webapp) :
                base("GET", "/wifi/linkregion")
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
            //m_log.DebugFormat("[HYPERLINK GET HANDLER]: resource {0}", resource);
            Request request = WifiUtils.CreateRequest(resource, httpRequest);
            Diva.Wifi.Environment env = new Diva.Wifi.Environment(request);

            string result = string.Empty;
            if (resource.StartsWith("/delete"))
            {
                // client invoked /wifi/linkregion/delete, possibly with the UUID parameter after
                UUID regionID = UUID.Zero;
                // SplitParams(path) returns an array of whatever parameters come after the path.
                // In this case it should return "delete" and "<uuid>"; we want "<uuid>", so [1]
                string[] pars = SplitParams(path);
                if (pars.Length >= 2)
                {
                    // indeed, client invoked /wifi/linkregion/delete/<uuid>
                    // let's grab that uuid 
                    UUID.TryParse(pars[1], out regionID);
                    result = m_WebApp.Services.HyperlinkDeleteGetRequest(env, regionID);
                }
            }

            if (string.IsNullOrEmpty(result))
                result = m_WebApp.Services.HyperlinkGetRequest(env);

            return WifiUtils.StringToBytes(result);
        }
    }

    /// <summary>
    /// The handler for the HTTP POST method on the resource /wifi/linkregion
    /// </summary>
    public class WifiHyperlinkPostHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;

        public WifiHyperlinkPostHandler(WebApp webapp) :
            base("POST", "/wifi/linkregion")
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
            //m_log.DebugFormat("[HYPERLINK POST HANDLER]: query String: {0}; resource: {1}", body, resource);

            try
            {
                // Here the data on the stream is transformed into a nice dictionary of keys & values
                Dictionary<string, object> postdata = ServerUtils.ParseQueryString(body);
                Request request = WifiUtils.CreateRequest(resource, httpRequest);
                Diva.Wifi.Environment env = new Diva.Wifi.Environment(request);

                string result = string.Empty;
                if (resource.StartsWith("/add"))
                {
                    // The client invoked /wifi/linkregion/add
                    string address = string.Empty;
                    uint xloc = 0, yloc = 0;
                    if (postdata.ContainsKey("address"))
                        address = postdata["address"].ToString();
                    if (postdata.ContainsKey("xloc"))
                        UInt32.TryParse(postdata["xloc"].ToString(), out xloc);
                    if (postdata.ContainsKey("yloc"))
                        UInt32.TryParse(postdata["yloc"].ToString(), out yloc);

                    result = m_WebApp.Services.HyperlinkAddRequest(env, address, xloc, yloc);
                }
                else if (resource.StartsWith("/delete"))
                {
                    // The client invoked /wifi/linkregion/delete, possibly with the UUID parameter after
                    UUID regionID = UUID.Zero;
                    string[] pars = SplitParams(path);
                    if ((pars.Length >= 2) && UUID.TryParse(pars[1], out regionID))
                    {
                        // Indeed the client invoked /wifi/linkregion/delete/<uuid>, and we got it already in regionID (above)
                        result = m_WebApp.Services.HyperlinkDeletePostRequest(env, regionID);
                    }
                }
                return WifiUtils.StringToBytes(result);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[HYPERLINK POST HANDLER]: Exception {0}",  e);
            }

            return WifiUtils.FailureResult();
        }

    }

}
