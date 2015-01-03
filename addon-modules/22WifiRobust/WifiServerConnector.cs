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
    [Extension(Path = "/Robust/Connector")]
    public class WifiServerConnector : ServiceConnector, IRobustConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string m_ServePathPrefix = "ServePath_";
        private const string m_AddonPrefix = "WifiAddon_";
        private ISceneActor m_SceneActor;
        private IHttpServer m_Server;
        private List<IRequestHandler> m_RequestHandlers = new List<IRequestHandler>();
        private static string CONFIG_FILE = "Wifi.ini";

        private static string AssemblyDirectory
        {
            get
            {
                string location = Assembly.GetExecutingAssembly().Location;
                return Path.GetDirectoryName(location);
            }
        }

        public override string ConfigName
        {
            get { return "WifiService"; }
        }

        public bool Enabled
        {
            get;
            private set;
        }

        public string PluginPath
        {
            get;
            set;
        }

        // Called from the plugin loader (in ServerUtils)
        public WifiServerConnector()
        {
            m_log.DebugFormat("[Wifi]: Instance created");
        }

        public WifiServerConnector(IConfigSource config, IHttpServer server, string configName) :
            this(config, server, configName, null)
        {
        }

        public WifiServerConnector(IConfigSource config, IHttpServer server, string configName, ISceneActor sactor) :
            base(config, server, configName)
        {
            m_Server = server;
            Config = config;
            m_SceneActor = sactor;
            if (!string.IsNullOrEmpty(configName))
                ConfigName = configName;
            m_log.DebugFormat("[Wifi]: WifiServerConnector starting with config {0}", ConfigName);

            Initialize(server);

        }

        private void AddStreamHandler(IRequestHandler rh)
        {
            m_RequestHandlers.Add(rh);
            m_Server.AddStreamHandler(rh);
        }

        #region IRobustConnector
        public uint Configure(IConfigSource config)
        {
            // We've already been configured! (not mono addin)
            if (m_Server != null)
                return m_Server.Port;

            Config = config;

            IConfig startconfig = Config.Configs["Startup"];
            string configdirectory = startconfig.GetString("ConfigDirectory", ".");

            ConfigFile = Path.Combine(configdirectory, CONFIG_FILE);
            if (!File.Exists(ConfigFile))
            {
                // We need to copy the one that comes in the package

                if (!Directory.Exists(configdirectory))
                    Directory.CreateDirectory(configdirectory);

                string embeddedConfig = Path.Combine(AssemblyDirectory, CONFIG_FILE);
                File.Copy(embeddedConfig, ConfigFile);
                m_log.ErrorFormat("[Wifi]: PLEASE EDIT {0} BEFORE RUNNING THIS SERVICE", ConfigFile);
                throw new Exception("Wifi addin must be configured prior to running");
            }

            IConfig wifiConfig = Config.Configs[ConfigName];
            if (wifiConfig == null)
            {
                m_log.DebugFormat("[Wifi]: Configuring from {0}...", ConfigFile);

                if (File.Exists(ConfigFile))
                {
                    IConfigSource configsource = new IniConfigSource(ConfigFile);
                    AdjustStorageProvider(configsource);

                    wifiConfig = configsource.Configs[ConfigName];

                    // Merge everything and expand eventual key values used by our config
                    Config.Merge(configsource);
                    Config.ExpandKeyValues();
                }
                else
                    m_log.WarnFormat("[Wifi]: Config file {0} not found", ConfigFile);

                if (wifiConfig == null)
                    throw new Exception(string.Format("[Wifi]: Could not load configuration from {0}. Unable to proceed.", ConfigFile));

                Enabled = true;
            }

            // Let's look for the port in WifiService first, then look elsewhere
            int port = wifiConfig.GetInt("ServerPort", -1);
            if (port > 0)
                return (uint)port;

            IConfig section = Config.Configs["Const"];
            if (section != null)
            {
                port = section.GetInt("PublicPort", -1);
                if (port > 0)
                    return (uint)port;
            }

            section = Config.Configs["Network"];
            if (section != null)
            {
                port = section.GetInt("port", -1);
                if (port > 0)
                    return (uint)port;
            }

            if (port < 0)
                throw new Exception("[Wifi]: Could not find port in configuration file");

            return 0;
        }

        public void Initialize(IHttpServer server)
        {
            m_log.DebugFormat("[Wifi]: Initializing. Server at port {0}.", server.Port);

            IConfig serverConfig = Config.Configs[ConfigName];
            if (serverConfig == null)
                throw new Exception(String.Format("No section {0} in config file", ConfigName));

            m_Server = server;

            // Launch the WebApp
            WebApp app = new WebApp(Config, ConfigName, server, m_SceneActor);

            // Register all the handlers
            BaseStreamHandler defaultHandler = new WifiDefaultHandler(app);
            AddStreamHandler(defaultHandler);
            AddStreamHandler(new WifiRootHandler(defaultHandler));
            AddStreamHandler(new WifiHeadHandler(app));
            AddStreamHandler(new WifiNotifyHandler(app));
            AddStreamHandler(new WifiInstallGetHandler(app));
            AddStreamHandler(new WifiInstallPostHandler(app));
            AddStreamHandler(new WifiLoginHandler(app));
            AddStreamHandler(new WifiLogoutHandler(app));
            AddStreamHandler(new WifiForgotPasswordGetHandler(app));
            AddStreamHandler(new WifiForgotPasswordPostHandler(app));
            AddStreamHandler(new WifiPasswordRecoverGetHandler(app));
            AddStreamHandler(new WifiPasswordRecoverPostHandler(app));
            AddStreamHandler(new WifiUserAccountGetHandler(app));
            AddStreamHandler(new WifiUserAccountPostHandler(app));
            AddStreamHandler(new WifiUserManagementGetHandler(app));
            AddStreamHandler(new WifiUserManagementPostHandler(app));
            AddStreamHandler(new WifiConsoleHandler(app));

            AddStreamHandler(new WifiInventoryLoadGetHandler(app));
            AddStreamHandler(new WifiInventoryGetHandler(app));
            AddStreamHandler(new WifiInventoryPostHandler(app));

            AddStreamHandler(new WifiHyperlinkGetHandler(app));
            AddStreamHandler(new WifiHyperlinkPostHandler(app));

            AddStreamHandler(new WifiTOSGetHandler(app));
            AddStreamHandler(new WifiTOSPostHandler(app));

            AddStreamHandler(new WifiGroupsManagementGetHandler(app));
            AddStreamHandler(new WifiGroupsManagementPostHandler(app));

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
                        object[] args = new object[] { Config, ConfigName, server, app };
                        IWifiAddon addon = ServerUtils.LoadPlugin<IWifiAddon>(addonDll, args);

                        if (addon == null)
                            m_log.WarnFormat("[Wifi]: Unable to load addon {0}", addonDll);
                    }
                }
            }

        }

        public void Unload()
        {
            foreach (IRequestHandler rh in m_RequestHandlers)
                m_Server.RemoveStreamHandler(rh.HttpMethod, rh.Path);

            // Tell the addons to unload too!
            m_RequestHandlers.Clear();
        }

        #endregion IRobustConnector

        private void AdjustStorageProvider(IConfigSource configsource)
        {
            IConfig database = configsource.Configs["DatabaseService"];
            if (database == null)
            {
                m_log.WarnFormat("[Wifi]: DatabaseService section not found");
                return;
            }

            string dll = database.GetString("StorageProvider", string.Empty);
            if (dll == string.Empty)
            {
                m_log.WarnFormat("[Wifi]: StorageProvider not found");
                return;
            }

            dll = Path.Combine(AssemblyDirectory, dll);
            
            database.Set("StorageProvider", dll);
        }
    }
}
