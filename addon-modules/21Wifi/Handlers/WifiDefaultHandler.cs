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

using Diva.Wifi.WifiScript;
using Diva.Utils;

using Processor = Diva.Wifi.WifiScript.Processor;
using Environment = Diva.Utils.Environment;

namespace Diva.Wifi
{
    public class WifiDefaultHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected WebApp m_WebApp;

        public WifiDefaultHandler(WebApp webapp) :
                base("GET", "/wifi")
        {
            m_WebApp = webapp;
        }

        public WifiDefaultHandler(string verb, string path) :
            base(verb, path)
        {
        }

        public override byte[] Handle(string path, Stream requestData,
                IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
        {
            // path = /wifi/...
            //m_log.DebugFormat("[Wifi]: path = {0}", path);
            //m_log.DebugFormat("[Wifi]: ip address = {0}", httpRequest.RemoteIPEndPoint);
            //foreach (object o in httpRequest.Query.Keys)
            //    m_log.DebugFormat("  >> {0}={1}", o, httpRequest.Query[o]);

            string resource = GetParam(path);
            resource = Uri.UnescapeDataString(resource).Trim(WebAppUtils.DirectorySeparatorChars);
            //m_log.DebugFormat("[Wifi]: resource {0}", resource);

            Request request = RequestFactory.CreateRequest(resource, httpRequest, Localization.GetLanguageInfo(httpRequest.Headers.Get("accept-language")));
            Environment env = new Environment(request);

            if (resource == string.Empty || resource.StartsWith("index."))
            {
                if (m_WebApp.StatisticsUpdateInterval != TimeSpan.Zero)
                    m_WebApp.Services.ComputeStatistics();

                httpResponse.ContentType = "text/html";

                return WebAppUtils.StringToBytes(m_WebApp.Services.DefaultRequest(env));
            }
            else
            {
                string resourcePath = WebApp.GetPath(resource);
                string[] resourcePaths = WebApp.GetPaths(resource);

                string type = WebAppUtils.GetContentType(resource);
                httpResponse.ContentType = type;
                //m_log.DebugFormat("[Wifi]: ContentType {0}", type);
                //m_log.DebugFormat("[XXX]: path {0}", resourcePath);
                if (type.StartsWith("image"))
                    return WebAppUtils.ReadBinaryResource(resourcePaths);

                if (type.StartsWith("application"))
                {
                    string res = WebAppUtils.ReadTextResource(resourcePaths, WebApp.MissingPage, true);
                    return WebAppUtils.StringToBytes(res);
                }
                if (type.StartsWith("text"))
                {
                    if (m_WebApp.StatisticsUpdateInterval != TimeSpan.Zero)
                        m_WebApp.Services.ComputeStatistics();

                    resourcePath = Localization.LocalizePath(env, resource);
                    Processor p = new Processor(m_WebApp.WifiScriptFace, env);
                    string res = p.Process(WebAppUtils.ReadTextResource(resourcePaths, WebApp.MissingPage));
                    if (res == string.Empty)
                        res = m_WebApp.Services.DefaultRequest(env);
                    return WebAppUtils.StringToBytes(res);
                }
            }

            httpResponse.ContentType = "text/plain";
            string result = "Boo!";
            return WebAppUtils.StringToBytes(result);
        }

        /*
        private string GetResource(string path)
        {
            string[] paramArray = SplitParams(path);
            m_log.DebugFormat("[Wifi]: paramArray length = {0}", paramArray.Length);
            if (paramArray != null && paramArray.Length > 0)
                return paramArray[0];

            return string.Empty;
        }
        */
    }

    public class WifiHeadHandler : WifiDefaultHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly byte[] m_EmptyBody = new byte[0];

        public WifiHeadHandler(WebApp webapp) :
            base("HEAD", "/wifi")
        {
            m_WebApp = webapp;
        }

        public override byte[] Handle(string path, Stream requestData,
                                      IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
        {
            //m_log.DebugFormat("[Wifi]: HEAD request {0}", path);
            byte[] result = base.Handle(path, requestData, httpRequest, httpResponse);
            httpResponse.ContentLength = result.Length;
            httpResponse.ContentLength64 = result.Length;
            return m_EmptyBody;
        }
    }

    public class WifiRootHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected BaseStreamHandler m_DefaultHandler;

        public WifiRootHandler(BaseStreamHandler defaultHandler) :
            base("GET", "/")
        {
            m_DefaultHandler = defaultHandler;
        }

        public override byte[] Handle(string path, Stream requestData,
                              IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
        {
            return m_DefaultHandler.Handle("/wifi" + path, requestData, httpRequest, httpResponse);
        }

    }
}
