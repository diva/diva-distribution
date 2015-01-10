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
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Server.Base;
using OpenSim.Server.Handlers.Base;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using log4net;
using Nini.Config;

using Diva.Interfaces;

using Mono.Addins;

namespace Diva.Wifi
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "WifiModule")]
    public class WifiModule : ISharedRegionModule, ISceneActor
    {
        #region Class and Instance Members

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private bool m_enabled = false;
        private List<Scene> m_Scenes = new List<Scene>();
        private WifiMain m_WifiMain;

        #endregion

        #region ISharedRegionModule

        public void Initialise(IConfigSource config)
        {
            m_log.Info("[Wifi Module]: Initializing...");

            // We only load the configuration file if the config doesn't know about this module already
            IConfig wifiConfig = config.Configs["WifiService"];
            if (wifiConfig == null)
                LoadConfiguration(config);

            wifiConfig = config.Configs["WifiService"];
            if (wifiConfig == null)
                throw new Exception("[Wifi Module]: Unable to find configuration. Service disabled.");

            try
            {
                m_enabled = wifiConfig.GetBoolean("Enabled", m_enabled);
                if (m_enabled)
                {
                    m_WifiMain = new WifiMain(config, MainServer.Instance, string.Empty, this);
                    m_log.Debug("[Wifi Module]: Wifi enabled.");
                }
                else
                    m_log.Debug("[Wifi Module]: Wifi disabled.");

            }
            catch (Exception e)
            {
                m_log.ErrorFormat(e.StackTrace);
                m_log.ErrorFormat("[Wifi Module]: Could not load Wifi: {0}. ", e.Message);
                m_enabled = false;
                return;
            }

        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        public string Name
        {
            get { return "Wifi Service Module"; }
        }

        public Type ReplaceableInterface 
        { 
            get { return null; }
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            m_Scenes.Add(scene);
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            m_Scenes.Remove(scene);
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        #endregion

        public void ForEachScene(SceneAction d)
        {
            foreach (Scene s in m_Scenes)
                d(s);
        }

        private void LoadConfiguration(IConfigSource config)
        {
            string configPath = string.Empty;
            bool created;
            if (!Util.MergeConfigurationFile(config, "Wifi.ini", Path.Combine(Diva.Wifi.Info.AssemblyDirectory, "Wifi.ini"), out configPath, out created))
            {
                m_log.WarnFormat("[Wifi Module]: Configuration file not merged.");
                return;
            }

            AdjustStorageProvider(config);

            if (created)
            {
                m_log.ErrorFormat("[Wifi Module]: PLEASE EDIT {0} BEFORE RUNNING THIS SERVICE", configPath);
                throw new Exception("Wifi addin must be configured prior to running");
            }
        }

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

            dll = Path.Combine(Diva.Wifi.Info.AssemblyDirectory, dll);

            database.Set("StorageProvider", dll);
        }

    }
}
