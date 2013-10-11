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

using Diva.Shim;

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
        private static Dictionary<string, List<UUID>> m_SimEventSubscribers = new Dictionary<string, List<UUID>>();
        private Dictionary<string, List<UUID>> m_EventSubscribers = new Dictionary<string, List<UUID>>();

        private IScriptModuleComms m_ScriptComms;
        private static List<IScriptModuleComms> m_ScriptModules = new List<IScriptModuleComms>();

        #endregion

        #region INonSharedRegionModule

        public void Initialise(IConfigSource config)
        {
            m_log.Info("[Diva.ScriptEvents]: ScriptEventsModule is on.");
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
            m_log.DebugFormat("[Diva.ScriptEvents]: Adding region {0} to this module", scene.RegionInfo.RegionName);

        }

        public void RegionLoaded(Scene scene)
        {
            m_ScriptComms = scene.RequestModuleInterface<IScriptModuleComms>();
            if (m_ScriptComms != null)
            {
                m_ScriptComms.OnScriptCommand += OnScriptCommand;
                m_ScriptModules.Add(m_ScriptComms);
            }
        }

        public void RemoveRegion(Scene scene)
        {
            m_Scene.EventManager.OnMakeRootAgent -= OnMakeRootAgent;
            m_Scene.EventManager.OnClientClosed -= OnClientClosed;
            m_ScriptComms.OnScriptCommand -= OnScriptCommand;
            //scene.ForEachRootScenePresence(delegate(ScenePresence sp)
            //{
            //    sp.ControllingClient.OnAddPrim -= ControllingClient_OnAddPrim;
            //});
            m_log.DebugFormat("[Diva.ScriptEvents]: Removing region {0} from this module", scene.RegionInfo.RegionName);
        }

        public void Close()
        {
        }

        #endregion

        void OnScriptCommand(UUID scriptId, string reqid, string module, string input, string k)
        {
            if (module != Name)
                return;
            m_log.DebugFormat("[Diva.ScriptEvents]: script: {0} reqid: {1} input: {2} k: {3} in {4}", scriptId, reqid, input, k, m_Scene.RegionInfo.RegionName);

            // input expected: <cmd>|<parcelid>|<agentid>|<option>
            // <cmd>: Pub | Sub
            // <option>: ...

            string[] tokens
                = input.Split(new char[] { '|' }, StringSplitOptions.None);

            string command = tokens[0];
            string event_name = string.Empty;
            Dictionary<string, List<UUID>> theDict = null;
            if ((command == "subscribe" || command == "sim-subscribe") && tokens.Length >= 2)
            {
                event_name = tokens[1];

                if (command == "subscribe")
                    theDict = m_EventSubscribers;
                else
                    theDict = m_SimEventSubscribers;

                List<UUID> subs;
                if (theDict.ContainsKey(event_name))
                    subs = theDict[event_name];
                else
                {
                    subs = new List<UUID>();
                    theDict.Add(event_name, subs);
                }
                if (!subs.Contains(scriptId))
                    subs.Add(scriptId);
            }
            else if (command == "unsubscribe" && tokens.Length >= 2)
            {
                event_name = tokens[1];
                if (m_EventSubscribers.ContainsKey(event_name) && m_EventSubscribers[event_name].Contains(scriptId))
                    m_EventSubscribers[event_name].Remove(scriptId); 

                if (m_SimEventSubscribers.ContainsKey(event_name) && m_SimEventSubscribers[event_name].Contains(scriptId))
                    m_SimEventSubscribers[event_name].Remove(scriptId); 
            }
            else if (command == "publish" && tokens.Length >= 2)
            {
                event_name = tokens[1];
                Publish(event_name, tokens.Length >= 3 ? tokens.Skip(2).ToArray() : null, string.Empty);
            }
        }

        #region The events sent to the scripts

        /// <summary>
        /// Emits events of the kind event|AvatarArrived|<LocalTP_TrueFalse>, avatar_id
        /// </summary>
        /// <param name="sp"></param>
        void OnMakeRootAgent(ScenePresence sp)
        {
            string event_name = VOEvents.AvatarArrived.ToString();
            bool localTP = ((sp.TeleportFlags & Constants.TeleportFlags.ViaHGLogin) == 0);

            Publish(event_name, new string[] { localTP.ToString() }, sp.UUID.ToString());

            //if (m_EventSubscribers.ContainsKey(event_name) && m_EventSubscribers[event_name].Count > 0)
            //{
            //    Util.FireAndForget(delegate
            //    {
            //        foreach (UUID script in m_EventSubscribers[event_name])
            //            m_ScriptComms.DispatchReply(script, 0, "event|" + event_name + "|" + localTP, sp.UUID.ToString());
            //    });
            //}

            //if (m_SimEventSubscribers.ContainsKey(event_name) && m_SimEventSubscribers[event_name].Count > 0)
            //{
            //    Util.FireAndForget(delegate
            //    {
            //        foreach (IScriptModuleComms m in m_ScriptModules)
            //            foreach (UUID script in m_SimEventSubscribers[event_name])
            //                m_ScriptComms.DispatchReply(script, 0, "event|" + event_name + "|" + localTP, sp.UUID.ToString());
            //    });
            //}

        }


        /// <summary>
        /// Emits events of the kind event|LastAvatarLeft
        /// </summary>
        void OnClientClosed(UUID clientID, Scene scene)
        {
            if (scene != m_Scene)
                return;

            string event_name = VOEvents.LastAvatarLeft.ToString();
            if (m_EventSubscribers.ContainsKey(event_name) && m_EventSubscribers[event_name].Count > 0)
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
                        Publish(event_name, null, "");
                        //Util.FireAndForget(delegate
                        //{
                        //    List<UUID> subs = m_EventSubscribers[event_name];
                        //    foreach (UUID script in subs)
                        //        m_ScriptComms.DispatchReply(script, 0, "event|" + event_name, "");
                        //});
            }
        }


        #endregion

        private void Publish(string event_name, string[] args, string key)
        {
            if (m_EventSubscribers.ContainsKey(event_name) && m_EventSubscribers[event_name].Count > 0)
            {
                Util.FireAndForget(delegate
                {
                    string message = "event|" + event_name + 
                            ((args != null && args.Length > 0) ? "|" + string.Join("|", args) : string.Empty);
                    foreach (UUID script in m_EventSubscribers[event_name])
                        m_ScriptComms.DispatchReply(script, 0, message, key);
                });
            }

            if (m_SimEventSubscribers.ContainsKey(event_name) && m_SimEventSubscribers[event_name].Count > 0)
            {
                Util.FireAndForget(delegate
                {
                    string message = "event|" + event_name +
                            ((args != null && args.Length > 0) ? "|" + string.Join("|", args) : string.Empty);
                    foreach (IScriptModuleComms m in m_ScriptModules)
                        foreach (UUID script in m_SimEventSubscribers[event_name])
                            m.DispatchReply(script, 0, message, key);
                });
            }
        }
    }
}
