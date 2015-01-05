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
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using Nini.Config;
using Mono.Addins;

using OpenSim.Server.Base;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;

using Diva.Interfaces;

namespace Diva.Wifi
{
    public class WifiMain : IServiceConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string m_ServePathPrefix = "ServePath_";
        private const string m_AddonPrefix = "WifiAddon_";
        private ISceneActor m_SceneActor;
        private IHttpServer m_Server;
        private List<IRequestHandler> m_RequestHandlers = new List<IRequestHandler>();

        private IConfigSource m_Config;
        private WebApp m_WebApp;

        private string ConfigName
        {
            get { return "WifiService"; }
        }

        // Robust addin calls this
        public WifiMain(IConfigSource config, IHttpServer server, string configName) :
            this(config, server, configName, null)
        {
        }

        // WifiModule calls this
        public WifiMain(IConfigSource config, IHttpServer server, string configName, ISceneActor sactor) 
        {
            m_Config = config;
            m_Server = server;
            m_SceneActor = sactor;

            m_log.DebugFormat("[Wifi]: WifiMain starting with config {0}", ConfigName);

            Initialize(server);

        }

        private void AddStreamHandler(IRequestHandler rh)
        {
            m_RequestHandlers.Add(rh);
            m_Server.AddStreamHandler(rh);
        }

        private void Initialize(IHttpServer server)
        {
            m_log.DebugFormat("[Wifi]: Initializing. Server at port {0}.", server.Port);

            IConfig serverConfig = m_Config.Configs[ConfigName];
            if (serverConfig == null)
                throw new Exception(String.Format("No section {0} in config file", ConfigName));

            // Launch the WebApp
            m_WebApp = new WebApp(m_Config, ConfigName, m_Server, m_SceneActor);

            // Register all the handlers
            BaseStreamHandler defaultHandler = new WifiDefaultHandler(m_WebApp);
            AddStreamHandler(defaultHandler);
            AddStreamHandler(new WifiRootHandler(defaultHandler));
            AddStreamHandler(new WifiHeadHandler(m_WebApp));
            AddStreamHandler(new WifiNotifyHandler(m_WebApp));
            AddStreamHandler(new WifiInstallGetHandler(m_WebApp));
            AddStreamHandler(new WifiInstallPostHandler(m_WebApp));
            AddStreamHandler(new WifiLoginHandler(m_WebApp));
            AddStreamHandler(new WifiLogoutHandler(m_WebApp));
            AddStreamHandler(new WifiForgotPasswordGetHandler(m_WebApp));
            AddStreamHandler(new WifiForgotPasswordPostHandler(m_WebApp));
            AddStreamHandler(new WifiPasswordRecoverGetHandler(m_WebApp));
            AddStreamHandler(new WifiPasswordRecoverPostHandler(m_WebApp));
            AddStreamHandler(new WifiUserAccountGetHandler(m_WebApp));
            AddStreamHandler(new WifiUserAccountPostHandler(m_WebApp));
            AddStreamHandler(new WifiUserManagementGetHandler(m_WebApp));
            AddStreamHandler(new WifiUserManagementPostHandler(m_WebApp));
            AddStreamHandler(new WifiConsoleHandler(m_WebApp));

            AddStreamHandler(new WifiInventoryLoadGetHandler(m_WebApp));
            AddStreamHandler(new WifiInventoryGetHandler(m_WebApp));
            AddStreamHandler(new WifiInventoryPostHandler(m_WebApp));

            AddStreamHandler(new WifiHyperlinkGetHandler(m_WebApp));
            AddStreamHandler(new WifiHyperlinkPostHandler(m_WebApp));

            AddStreamHandler(new WifiTOSGetHandler(m_WebApp));
            AddStreamHandler(new WifiTOSPostHandler(m_WebApp));

            AddStreamHandler(new WifiGroupsManagementGetHandler(m_WebApp));
            AddStreamHandler(new WifiGroupsManagementPostHandler(m_WebApp));

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
                        AddStreamHandler(new WifiGetHandler(parts[0], parts[1]));
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
                        object[] args = new object[] { m_Config, ConfigName, m_Server, m_WebApp };
                        IWifiAddon addon = ServerUtils.LoadPlugin<IWifiAddon>(addonDll, args);

                        if (addon == null)
                            m_log.WarnFormat("[Wifi]: Unable to load addon {0}", addonDll);
                    }
                }
            }

            // Load Wifi addons as mono addins, if they exist
            try
            {
                AddinManager.AddExtensionNodeHandler("/Diva/Wifi/Addon", OnExtensionChanged);
            }
            catch (InvalidOperationException e)
            {
                m_log.DebugFormat("[Wifi]: extension point /Diva/Wifi/Addon not found");
            }
        }

        private void OnExtensionChanged(object s, ExtensionNodeEventArgs args)
        {
            IWifiAddon addon = (IWifiAddon)args.ExtensionObject;
            if (args.Change == ExtensionChange.Add)
            {
                m_log.InfoFormat("[Wifi]: Detected addon {0}", addon.Name);
                if (addon.LoadConfig(m_Config))
                    addon.Initialize(m_Config, ConfigName, m_Server, m_WebApp);
            }
        }

        public void Unload()
        {
            foreach (IRequestHandler rh in m_RequestHandlers)
                m_Server.RemoveStreamHandler(rh.HttpMethod, rh.Path);

            // Tell the addons to unload too!
            m_RequestHandlers.Clear();
        }

    }
}
