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
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;


using log4net;
using Nini.Config;

using Mono.Addins;

namespace Diva.Shim
{

    /// <summary>
    /// Serves as a shim for loading and reloading modules while they're in development
    /// without having to shutdown the simulator.
    /// </summary>
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "ModuleShim")]
    public class ModuleShim : ISharedRegionModule
    {
        #region Class and Instance Members

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<Scene> m_Scenes;
        private Dictionary<string, List<IRegionModuleBase>> m_ManagedModules;
        private bool m_Enabled;
        private IConfigSource m_Config;

        #endregion

        #region ISharedRegionModule

        public void Initialise(IConfigSource config)
        {
            IConfig cnf = config.Configs["ModuleShim"];
            if (cnf == null)
                return;

            m_Enabled = cnf.GetBoolean("Enabled", m_Enabled);
            if (m_Enabled)
            {
                m_log.Info("[Diva.ModuleShim]: ModuleShim is on.");
                m_Scenes = new List<Scene>();
                m_ManagedModules = new Dictionary<string, List<IRegionModuleBase>>();
                m_Config = config;
                MainConsole.Instance.Commands.AddCommand("ModuleShim", true, "shim load", "shim reload dllname[:class]", "Reloads a dll containing region modules", HandleReload);
                MainConsole.Instance.Commands.AddCommand("ModuleShim", true, "shim list", "shim list", "Lists all region modules under management", HandleList);
            }
        }

        public void PostInitialise()
        {
        }

        public string Name
        {
            get { return "Module Shim"; }
        }

        public Type ReplaceableInterface 
        { 
            get { return null; }
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            m_Scenes.Add(scene);
            scene.RegisterModuleInterface<ModuleShim>(this);
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            m_Scenes.Remove(scene);
            scene.UnregisterModuleInterface<ModuleShim>(this);
        }

        public void Close()
        {
        }

        #endregion

        public void Manage(Type t, IRegionModuleBase mod)
        {
            string typeName = t.ToString();
            if (!m_ManagedModules.ContainsKey(typeName))
                m_ManagedModules[typeName] = new List<IRegionModuleBase>();

            if (!m_ManagedModules[typeName].Contains(mod))
            {
                m_ManagedModules[typeName].Add(mod);
                m_log.DebugFormat("[Diva.ModuleShim]: Now managing {0} of type {1}", mod.Name, t.ToString());
            }
        }

        private void HandleList(string module, string[] args)
        {
            foreach (KeyValuePair<string, List<IRegionModuleBase>> kvp in m_ManagedModules)
            {
                foreach (IRegionModuleBase m in kvp.Value)
                {
                    bool isShared = false;
                    if (m is ISharedRegionModule)
                        isShared = true;
                    MainConsole.Instance.OutputFormat("Type: {0} Name: {1} Shared? {2}", kvp.Key, m.Name, isShared);
                }
            }
        }

        private void HandleReload(string module, string[] args)
        {
            // Possible forms:
            // # shim reload some.dll
            // # shim reload some.dll:SomeModule
            if (args.Length < 3)
            {
                MainConsole.Instance.Output("shim reload dll[:class]");
                return;
            }

            string dllName = args[2];

            Modules modules = LoadRegionModules(dllName, new object[] { });
            int count = modules.m_Shared.Count;
            foreach (KeyValuePair<Type, List<INonSharedRegionModule>> kvp in modules.m_NonShared)
                count += kvp.Value.Count;

            m_log.DebugFormat("[Diva.ModuleShim]: instantiated {0} module instances", count);

            // Deregister existing instances and register new ones
            foreach (ISharedRegionModule mod in modules.m_Shared)
            {
                RemoveOldModules(mod.GetType());
            }
            foreach (Type t in modules.m_NonShared.Keys)
            {
                if (modules.m_NonShared[t].Count > 0)
                    RemoveOldModules(t);
            }

            // Activate the new ones
            // 1. Initialise
            foreach (IRegionModuleBase mod in modules.m_Shared)
            {
                Manage(mod.GetType(), mod);
                mod.Initialise(m_Config);
            }
            foreach (List<INonSharedRegionModule> mods in modules.m_NonShared.Values)
            {
                foreach (INonSharedRegionModule mod in mods)
                {
                    Manage(mod.GetType(), mod);
                    mod.Initialise(m_Config);
                }
            }
            // 2. PostInitialise the shared modules
            foreach (IRegionModuleBase mod in modules.m_Shared)
            {
                ISharedRegionModule s = (ISharedRegionModule)mod;
                s.PostInitialise();
            }
            // 3. AddRegion
            foreach (IRegionModuleBase mod in modules.m_Shared)
            {
                foreach (Scene s in m_Scenes)
                {
                    mod.AddRegion(s);
                    s.AddRegionModule(mod.Name, mod);
                }
            }
            foreach (KeyValuePair<Type, List<INonSharedRegionModule>> kvp in modules.m_NonShared)
            {
                for (int i = 0; i < m_Scenes.Count; i++)
                {
                    kvp.Value[i].AddRegion(m_Scenes[i]);
                    m_Scenes[i].AddRegionModule(kvp.Value[i].Name, kvp.Value[i]);
                }
            }
            // 4. RegionLoaded
            foreach (IRegionModuleBase mod in modules.m_Shared)
            {
                foreach (Scene s in m_Scenes)
                    mod.RegionLoaded(s);
            }
            foreach (KeyValuePair<Type, List<INonSharedRegionModule>> kvp in modules.m_NonShared)
            {
                for (int i = 0; i < m_Scenes.Count; i++)
                    kvp.Value[i].RegionLoaded(m_Scenes[i]);
            }
        }

        private void RemoveOldModules(Type t)
        {
            string typeName = t.ToString();
            m_log.DebugFormat("[Diva.ModuleShim]: RemoveOldModules for type {0}", typeName);
            List<IRegionModuleBase> deletes = new List<IRegionModuleBase>();
            foreach (Scene scene in m_Scenes)
                foreach (IRegionModuleBase mod in scene.RegionModules.Values)
                {
                    //m_log.DebugFormat("[XXX]: Lookign at {0} vs {1}", mod.GetType().ToString(), typeName);
                    if (mod.GetType().ToString() == typeName)
                    {
                        mod.RemoveRegion(scene);
                        if (mod is INonSharedRegionModule)
                            mod.Close();
                        if (!deletes.Contains(mod))
                        {
                            deletes.Add(mod);
                        }
                    }
                }

            foreach (IRegionModuleBase m in deletes)
            {
                if (m_ManagedModules.ContainsKey(typeName) && m_ManagedModules[typeName].Contains(m))
                {
                    m_ManagedModules[typeName].Remove(m);
                }
                else
                    m_log.DebugFormat("[Diva.ModuleShim]: Module {0} of type {1} wasn't managed", m.Name, typeName);

                foreach (Scene scene in m_Scenes)
                    scene.RemoveRegionModule(m.Name);
            }

        }

        #region Load Modules from assemblies
        Modules LoadRegionModules(string dllName, Object[] args)
        {
            // This is good to debug configuration problems
            //if (dllName == string.Empty)
            //    Util.PrintCallStack();

            string[] parts = dllName.Split(new char[] { ':' });

            dllName = parts[0];

            string className = String.Empty;

            if (parts.Length > 1)
                className = parts[1];

            return LoadRegionModules(dllName, className, args);
        }

        /// <summary>
        /// Load a plugin from a dll with the given class or interface
        /// </summary>
        /// <param name="dllName"></param>
        /// <param name="className"></param>
        /// <param name="args">The arguments which control which constructor is invoked on the plugin</param>
        /// <returns></returns>
        Modules LoadRegionModules(string dllName, string className, Object[] args) 
        {
            string interfaceName = "IRegionModuleBase";
            Modules plugins = new Modules();

            try
            {
                Assembly pluginAssembly = Assembly.LoadFrom(dllName);

                foreach (Type pluginType in pluginAssembly.GetTypes())
                {
                    if (pluginType.IsPublic)
                    {
                        if (className != String.Empty
                            && pluginType.ToString() != pluginType.Namespace + "." + className)
                            continue;

                        Type typeInterface = pluginType.GetInterface(interfaceName, true);

                        if (typeInterface != null)
                        {
                            IRegionModuleBase plug = null;
                            Type shared = pluginType.GetInterface("ISharedRegionModule", true);
                            int n_instances = 1;
                            if (shared == null)
                                n_instances = m_Scenes.Count;
                            m_log.DebugFormat("[Diva.ModuleShim]: Found {0} {1}", pluginType.ToString(), (shared != null? "shared" : "non-shared"));
                            try
                            {
                                for (int i = 0; i < n_instances; i++)
                                {
                                    plug = (IRegionModuleBase)Activator.CreateInstance(pluginType,
                                            args);
                                    if (shared == null)
                                    {
                                        Type t = plug.GetType();
                                        if (!plugins.m_NonShared.ContainsKey(t))
                                            plugins.m_NonShared[t] = new List<INonSharedRegionModule>();
                                        plugins.m_NonShared[t].Add((INonSharedRegionModule)plug);
                                    }
                                    else
                                        plugins.m_Shared.Add((ISharedRegionModule)plug);
                                }
                            }
                            catch (Exception e)
                            {
                                if (!(e is System.MissingMethodException))
                                {
                                    m_log.ErrorFormat("Error loading plugin {0} from {1}. Exception: {2}",
                                        interfaceName, dllName, e.InnerException == null ? e.Message : e.InnerException.Message);
                                }
                                break;
                            }
                        }
                    }
                }

                return plugins;
            }
            catch (ReflectionTypeLoadException rtle)
            {
                m_log.Error(string.Format("Error loading plugin from {0}:\n{1}", dllName,
                    String.Join("\n", Array.ConvertAll(rtle.LoaderExceptions, e => e.ToString()))),
                    rtle);
                return plugins;
            }
            catch (Exception e)
            {
                m_log.Error(string.Format("Error loading plugin from {0}", dllName), e);
                return plugins;
            }
        }

        class Modules
        {
            public List<ISharedRegionModule> m_Shared;
            public Dictionary<Type, List<INonSharedRegionModule>> m_NonShared;

            public Modules()
            {
                m_Shared = new List<ISharedRegionModule>();
                m_NonShared = new Dictionary<Type, List<INonSharedRegionModule>>();
            }
        }

        #endregion
    }
}
