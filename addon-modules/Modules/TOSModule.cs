/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using System.Web;

using OpenMetaverse;
using log4net;
using Nini.Config;
using Mono.Addins;

using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;

using Diva.OpenSimServices;

namespace Diva.Modules
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class TOSModule : ISharedRegionModule
    {
        class TOSTimer : Timer
        {
            public TOSTimer(int minutes)
                : base(minutes * 60 * 1000)
            {
            }

            public ScenePresence SP;
        }

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //
        // Module vars
        //
        private bool m_Enabled = false;
        private List<Scene> m_Scenes = new List<Scene>();

        private bool m_ShowToLocalUsers = false;
        private bool m_ShowToForeignUsers = true;
        private string m_TOS_URL = String.Empty;
        private string m_Message = "Please read and agree to the Terms of Service";
        private int m_Timeout = 5; // 5 minutes

        private GridUserService m_GridUserService;

        // Normally we wouldn't need a list/dictionary, but if we don't hold the rerefence
        // to these timers somehwre, they may be garbage collected
        private Dictionary<object, UUID> m_Timers = new Dictionary<object, UUID>();

        #region ISharedRegionModule

        public void Initialise(IConfigSource config)
        {
            IConfig tosModule = config.Configs["TOSModule"];
            if (tosModule != null)
            {
                m_Enabled = tosModule.GetBoolean("Enabled", false);
                if (m_Enabled)
                {
                    m_ShowToLocalUsers = tosModule.GetBoolean("ShowToLocalUsers", false);
                    m_ShowToForeignUsers = tosModule.GetBoolean("ShowToForeignUsers", true);
                    m_TOS_URL = tosModule.GetString("TOS_URL", string.Empty);
                    m_Message = tosModule.GetString("Message", m_Message);
                    m_Timeout = tosModule.GetInt("Timeout", m_Timeout);

                    m_GridUserService = new GridUserService(config);

                    m_log.Debug("[TOS MODULE]: Enabled");
                }
                else
                    m_log.Debug("[TOS MODULE]: Not Enabled");

            }
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            m_Scenes.Add(scene);
            scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            m_Scenes.Remove(scene);
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "TOSModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        void OnMakeRootAgent(ScenePresence sp)
        {
            if (sp.PresenceType == PresenceType.Npc)
                return;

            bool isLocalUser = sp.Scene.UserManagementModule.IsLocalGridUser(sp.UUID);

            if ((!m_ShowToLocalUsers && isLocalUser) || (!m_ShowToForeignUsers && !isLocalUser))
                return;

            if (m_TOS_URL == String.Empty)
                return;

            // Is it pending?
            foreach (UUID uuid in m_Timers.Values)
                if (uuid == sp.UUID)
                    return;

            // ok, show it if the user hasn't seen it yet
            string userId = sp.Scene.UserManagementModule.GetUserUUI(sp.UUID);
            DGridUserInfo gridUser = m_GridUserService.GetExtendedGridUserInfo(userId);

            // The user has already accepted the TOS before
            if (gridUser != null && gridUser.TOS != string.Empty)
                return;

            // We need to show the TOS

            AgentCircuitData aCircuit = sp.Scene.AuthenticateHandler.GetAgentCircuitData(sp.UUID);
            if (aCircuit != null)
            {
                // Show ToS 
                IDialogModule dm = sp.Scene.RequestModuleInterface<IDialogModule>();
                if (null != dm)
                {
                    string url = m_TOS_URL + "?uid=" + HttpUtility.UrlEncode(userId) + "&sid=" + aCircuit.SessionID;
                    dm.SendUrlToUser(sp.UUID, "License Agreement", sp.Scene.RegionInfo.EstateSettings.EstateOwner,
                        sp.Scene.RegionInfo.EstateSettings.EstateOwner, false, m_Message, url);

                    // Set the timer
                    TOSTimer t = new TOSTimer(1);
                    t.SP = sp;
                    t.Elapsed += new ElapsedEventHandler(t_Elapsed);
                    m_Timers.Add(t, sp.UUID);
                    t.Enabled = true;
                }
            }
        }

        void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (m_Timers.ContainsKey(sender))
            {
                TOSTimer t = (TOSTimer)sender;
                if (t.SP.ControllingClient != null)
                {
                    // Maybe the user TP'ed to another region on the same sim
                    ScenePresence theRoot = t.SP;
                    if (theRoot.IsChildAgent)
                        foreach (Scene s in m_Scenes)
                        {
                            ScenePresence sp = s.GetScenePresence(t.SP.UUID);
                            if (sp != null && !sp.IsChildAgent)
                                theRoot = sp;
                        }

                    if (!theRoot.IsChildAgent)
                    {
                        string userId = theRoot.Scene.UserManagementModule.GetUserUUI(theRoot.UUID);
                        DGridUserInfo info = m_GridUserService.GetExtendedGridUserInfo(userId);

                        if (info == null || (info != null && info.TOS == string.Empty))
                        {
                            theRoot.ControllingClient.SendAlertMessage("Your time to accept the TOS has expired. You have been disconnected. Please accept the TOS and try again.");
                            IEntityTransferModule mod = theRoot.Scene.RequestModuleInterface<IEntityTransferModule>();
                            if (mod != null)
                                mod.TeleportHome(theRoot.UUID, theRoot.ControllingClient);
                            theRoot.ClientView.Disconnect();
                        }
                        else
                            m_log.DebugFormat("[TOS MODULE]: user accepted TOS");
                    }
                    // else, it's gone
                }

                t.Enabled = false;
                m_Timers.Remove(t);
                t.Dispose();
            }
        }
    }
}

