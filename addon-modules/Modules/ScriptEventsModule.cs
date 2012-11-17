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
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenMetaverse;


using log4net;
using Nini.Config;

using Mono.Addins;

namespace Diva.Modules
{
    enum VOEvents
    {
        AvatarArrived,
        LastAvatarLeft
    }

    /// <summary>
    /// Captures interesting scene events and sends them to scripts that subscribe to them.
    /// </summary>
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "ScriptEventsModule")]
    public class ScriptEventsModule : INonSharedRegionModule
    {
        #region Class and Instance Members

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_Scene;
        private Dictionary<VOEvents, List<UUID>> m_EventSubscribers = new Dictionary<VOEvents, List<UUID>>();

        private IScriptModuleComms m_ScriptComms;

        #endregion

        #region INonSharedRegionModule

        public void Initialise(IConfigSource config)
        {
            m_log.Info("[Diva.ScriptEvents]: ScriptEventsModule is on.");
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        public string Name
        {
            get { return "Script Events"; }
        }

        public Type ReplaceableInterface 
        { 
            get { return null; }
        }

        public void AddRegion(Scene scene)
        {
            m_Scene = scene;
            m_Scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
            m_Scene.EventManager.OnClientClosed += OnClientClosed;
        }

        public void RemoveRegion(Scene scene)
        {
            m_Scene.EventManager.OnMakeRootAgent -= OnMakeRootAgent;
            m_Scene.EventManager.OnClientClosed -= OnClientClosed;
        }

        public void RegionLoaded(Scene scene)
        {
            m_ScriptComms = scene.RequestModuleInterface<IScriptModuleComms>();
            m_ScriptComms.OnScriptCommand += new ScriptCommand(OnScriptCommand);
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        #endregion

        void OnScriptCommand(UUID scriptId, string reqid, string module, string input, string k)
        {
            if (module != Name)
                return;
            m_log.DebugFormat("[Diva.ScriptEvents]: script: {0} reqid: {1} k: {2}", scriptId, reqid, k);

            // input expected: <cmd>|<parcelid>|<agentid>|<option>
            // <cmd>: Pub | Sub
            // <option>: ...

            string[] tokens
                = input.Split(new char[] { '|' }, StringSplitOptions.None);

            string command = tokens[0];
            if (command == "subscribe" && tokens.Length >= 2)
            {
                string event_name = tokens[1];
                try
                {
                    VOEvents e = (VOEvents)Enum.Parse(typeof(VOEvents), event_name);
                    List<UUID> subs;
                    if (m_EventSubscribers.ContainsKey(e))
                        subs = m_EventSubscribers[e];
                    else
                    {
                        subs = new List<UUID>();
                        m_EventSubscribers.Add(e, subs);
                    }
                    if (!subs.Contains(scriptId))
                        subs.Add(scriptId);
                }
                catch (Exception)
                {
                    m_log.DebugFormat("[Diva.ScriptEvents]: event name {0} not recognized", event_name);
                }
            }
            else if (command == "unsubscribe" && tokens.Length >= 2)
            {
                string event_name = tokens[1];
                try
                {
                    VOEvents e = (VOEvents)Enum.Parse(typeof(VOEvents), event_name);
                    if (m_EventSubscribers.ContainsKey(e) && m_EventSubscribers[e].Contains(scriptId))
                        m_EventSubscribers[e].Remove(scriptId); ;
                }
                catch (Exception)
                {
                    m_log.DebugFormat("[Diva.ScriptEvents]: event name {0} not recognized", event_name);
                }
            }
        }

        #region The events sent to the scripts

        /// <summary>
        /// Emits events of the kind event|AvatarArrived|<LocalTP_TrueFalse>, avatar_id
        /// </summary>
        /// <param name="sp"></param>
        void OnMakeRootAgent(ScenePresence sp)
        {
            if (m_EventSubscribers.ContainsKey(VOEvents.AvatarArrived) && m_EventSubscribers[VOEvents.AvatarArrived].Count > 0)
            {
                Util.FireAndForget(delegate
                {
                    bool localTP = ((sp.TeleportFlags & Constants.TeleportFlags.ViaHGLogin) == 0);
                    List<UUID> subs = m_EventSubscribers[VOEvents.AvatarArrived];
                    foreach (UUID script in subs)
                        m_ScriptComms.DispatchReply(script, 0, "event|" + VOEvents.AvatarArrived + "|" + localTP, sp.UUID.ToString());
                });
            }
        }

        /// <summary>
        /// Emits events of the kind event|LastAvatarLeft
        /// </summary>
        void OnClientClosed(UUID clientID, Scene scene)
        {
            if (scene != m_Scene)
                return;

            if (m_EventSubscribers.ContainsKey(VOEvents.LastAvatarLeft) && m_EventSubscribers[VOEvents.LastAvatarLeft].Count > 0)
            {
                bool no_avies = true;
                List<ScenePresence> presences = m_Scene.GetScenePresences();
                    foreach (ScenePresence sp in presences)
                        if (!sp.IsChildAgent)
                        {
                            no_avies = false;
                            break;
                        }
                    if (no_avies)
                        Util.FireAndForget(delegate
                        {
                            List<UUID> subs = m_EventSubscribers[VOEvents.LastAvatarLeft];
                            foreach (UUID script in subs)
                                m_ScriptComms.DispatchReply(script, 0, "event|" + VOEvents.LastAvatarLeft, "");
                        });
            }
        }


        #endregion

    }
}
