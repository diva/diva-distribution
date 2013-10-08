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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenSim.Server.Base;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;

using Diva.Interfaces;
using Environment = Diva.Utils.Environment;

namespace Diva.Wifi
{
    public class WifiServerConnector : ServiceConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string m_ConfigName = "WifiService";
        private const string m_ServePathPrefix = "ServePath_";
        private const string m_AddonPrefix = "WifiAddon_";

        public WifiServerConnector(IConfigSource config, IHttpServer server, string configName) :
            this(config, server, configName, null)
        {
        }

        public WifiServerConnector(IConfigSource config, IHttpServer server, string configName, ISceneActor sactor) :
            base(config, server, configName)
        {
            m_log.DebugFormat("[Wifi]: WifiServerConnector starting");
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
            WebApp app = new WebApp(config, m_ConfigName, server, sactor);

            // Register all the handlers
            server.AddStreamHandler(new WifiDefaultHandler(app));
            server.AddStreamHandler(new WifiHeadHandler(app));
            server.AddStreamHandler(new WifiNotifyHandler(app));
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
            server.AddStreamHandler(new WifiConsoleHandler(app));

            server.AddStreamHandler(new WifiInventoryLoadGetHandler(app));
            server.AddStreamHandler(new WifiInventoryGetHandler(app));
            server.AddStreamHandler(new WifiInventoryPostHandler(app));

            server.AddStreamHandler(new WifiHyperlinkGetHandler(app));
            server.AddStreamHandler(new WifiHyperlinkPostHandler(app));

            server.AddStreamHandler(new WifiTOSGetHandler(app));
            server.AddStreamHandler(new WifiTOSPostHandler(app));

            server.AddStreamHandler(new WifiGroupsManagementGetHandler(app));
            server.AddStreamHandler(new WifiGroupsManagementPostHandler(app));

            //server.AddStreamHandler(new WifiRegionManagementPostHandler(app));
            //server.AddStreamHandler(new WifiRegionManagementGetHandler(app));

            // Add handlers for serving configured paths
            IEnumerable<string> servePaths = serverConfig.GetKeys().Where(option => option.StartsWith(m_ServePathPrefix));
            if (servePaths.Count() > 0)
            {
                foreach (string servePath in servePaths)
                {
                    string paths = serverConfig.GetString(servePath, string.Empty);
                    string[] parts = paths.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Count() == 2)
                        server.AddStreamHandler(new WifiGetHandler(parts[0], parts[1]));
                    else
                        m_log.WarnFormat("[Wifi]: Invalid format with configuration option {0}: {1}", servePath, paths);
                }
            }

            // Load addons
            IEnumerable<string> addonVars = serverConfig.GetKeys().Where(option => option.StartsWith(m_AddonPrefix));
            if (addonVars.Count() > 0)
            {
                foreach (string addonVar in addonVars)
                {
                    string addonDll = serverConfig.GetString(addonVar, string.Empty);
                    if (addonDll != string.Empty)
                    {
                        m_log.InfoFormat("[Wifi]: Loading addon {0}", addonDll);
                        object[] args = new object[] { config, m_ConfigName, server, app };
                        IWifiAddon addon = ServerUtils.LoadPlugin<IWifiAddon>(addonDll, args);

                        if (addon == null)
                            m_log.WarnFormat("[Wifi]: Unable to load addon {0}", addonDll);
                    }
                }
            }

        }
    }
}
