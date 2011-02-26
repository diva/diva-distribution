/**
 * Copyright (c) Marcus Kirsch (aka Marck) and Crista Lopes (aka Diva). All rights reserved.
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

using log4net;
using System.Reflection;
using System.IO;

using OpenSim.Framework.Servers.HttpServer;

namespace Diva.Wifi
{
    public class WifiGetBinHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string m_BinPath = "data";
        //private WebApp m_WebApp;

        public WifiGetBinHandler(WebApp webapp) :
                base("GET", "/bin")
        {
            //m_WebApp = webapp;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            string resource = GetParam(path);
            //m_log.DebugFormat("[Wifi]: resource {0}", resource);
            resource = resource.Trim('/');

            string resourcePath = System.IO.Path.Combine(m_BinPath, resource);
            string type = WifiUtils.GetContentType(resource);
            httpResponse.ContentType = type;
            //m_log.DebugFormat("[Wifi]: ContentType {0}", type);
            if (type.StartsWith("image"))
                return WifiUtils.ReadBinaryResource(resourcePath);

            if (type.StartsWith("application") || type.StartsWith("text"))
            {
                string res = WifiUtils.ReadTextResource(resourcePath, true);
                return WifiUtils.StringToBytes(res);
            }

            httpResponse.ContentType = "text/plain";
            string result = "Boo!";
            return WifiUtils.StringToBytes(result);
        }
    }
}
