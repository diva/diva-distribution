/*
 * Copyright (c) Crista Lopes (aka Diva). All rights reserved.
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

using System;
using Nini.Config;
using OpenSim.Server.Base;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;

using Environment = Diva.Wifi.Environment;

namespace Diva.Wifi
{
    public class WifiServerConnector : ServiceConnector
    {
        private string m_ConfigName = "WifiService";

        public WifiServerConnector(IConfigSource config, IHttpServer server, string configName) :
            base(config, server, configName)
        {
            IConfig serverConfig = config.Configs[m_ConfigName];
            if (serverConfig == null)
                throw new Exception(String.Format("No section {0} in config file", m_ConfigName));

            //
            // Leaving this here for educational purposes
            //
            //if (Environment.StaticVariables.ContainsKey("AppDll"))
            //{
            //    object[] args = new object[] { config, server };
            //    WebApp app = ServerUtils.LoadPlugin<IWebApp>(Environment.StaticVariables["AppDll"].ToString(), args);
            //    Environment.InitializeWebApp(app);
            //}

            // Launch the WebApp
            WebApp app = new WebApp(config, m_ConfigName, server);

            // Register all the handlers
            server.AddStreamHandler(new WifiGetHandler(app));
            server.AddStreamHandler(new WifiInstallGetHandler(app));
            server.AddStreamHandler(new WifiInstallPostHandler(app));
            server.AddStreamHandler(new WifiLoginHandler(app));
            server.AddStreamHandler(new WifiLogoutHandler(app));
            server.AddStreamHandler(new WifiForgotPasswordGetHandler(app));
            server.AddStreamHandler(new WifiForgotPasswordPostHandler(app));
            server.AddStreamHandler(new WifiPasswordRecoverGetHandler(app));
            server.AddStreamHandler(new WifiPasswordRecoverPostHandler(app));
            server.AddStreamHandler(new WifiUserAccountGetHandler(app));
            server.AddStreamHandler(new WifiUserAccountPostHandler(app));
            server.AddStreamHandler(new WifiUserManagementGetHandler(app));
            server.AddStreamHandler(new WifiUserManagementPostHandler(app));
            server.AddStreamHandler(new WifiRegionManagementPostHandler(app));
            server.AddStreamHandler(new WifiRegionManagementGetHandler(app));
        }
    }
}
