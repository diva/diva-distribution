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
        private static string CONFIG_FILE = "Wifi.ini";
        private WifiMain m_WifiMain;

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
            m_log.DebugFormat("[Wifi]: Addin instance created");
        }

        #region IRobustConnector
        public uint Configure(IConfigSource config)
        {
            Config = config;

            IConfig startconfig = Config.Configs["Startup"];
            string configdirectory = startconfig.GetString("ConfigDirectory", ".");

            ConfigFile = Path.Combine(configdirectory, CONFIG_FILE);

            IConfig wifiConfig = Config.Configs[ConfigName];
            
            if (wifiConfig == null)
            {
                // No [WifiService] in the main configuration. We need to read it from its own file
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
                else
                {
                    m_log.DebugFormat("[Wifi]: Configuring from {0}...", ConfigFile);

                    IConfigSource configsource = new IniConfigSource(ConfigFile);
                    AdjustStorageProvider(configsource);

                    wifiConfig = configsource.Configs[ConfigName];

                    // Merge everything and expand eventual key values used by our config
                    Config.Merge(configsource);
                    Config.ExpandKeyValues();
                }

                if (wifiConfig == null)
                    throw new Exception(string.Format("[Wifi]: Could not load configuration from {0}. Unable to proceed.", ConfigFile));

            }

            Enabled = wifiConfig.GetBoolean("Enabled", false);

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

            if (port < 0)
                throw new Exception("[Wifi]: Could not find port in configuration file");

            return 0;
        }

        public void Initialize(IHttpServer server)
        {
            m_log.DebugFormat("[Wifi]: Initializing. Server at port {0}. Service is {1}", server.Port, Enabled ? "enabled" : "disabled");

            if (!Enabled)
                return;

            IConfig serverConfig = Config.Configs[ConfigName];
            if (serverConfig == null)
                throw new Exception(String.Format("No section {0} in config file", ConfigName));

            m_WifiMain = new WifiMain(Config, server, ConfigName);
        }

        public void Unload()
        {
            if (!Enabled)
                return;

            m_WifiMain.Unload();
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
