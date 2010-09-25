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
using System.Collections.Generic;

using log4net;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Services.Interfaces;

using Diva.OpenSimServices;

namespace Diva.Wifi
{
    public partial class Services
    {
        public string InventoryLoadGetRequest(Environment env)
        {
            m_log.DebugFormat("[Wifi]: InventoryLoadGetRequest");
            SessionInfo sinfo;
            if (TryGetSessionInfo(env.Request, out sinfo))
            {
                env.Session = sinfo;
                env.Flags = Flags.IsLoggedIn;
                env.State = State.InventoryListLoad;
            }
            return m_WebApp.ReadFile(env, "index.html");
        }

        public string InventoryGetRequest(Environment env)
        {
            if (!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[Wifi]: warning: someone is trying to access UserAccountGetRequest and Wifi isn't isntalled!");
                return m_WebApp.ReadFile(env, "index.html");
            }

            m_log.DebugFormat("[Wifi]: InventoryGetRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo))
            {
                env.Session = sinfo;
                InventoryTreeNode tree = m_InventoryService.GetInventoryTree(sinfo.Account.PrincipalID);
                List<object> loo = new List<object>();
                //foreach (InventoryTreeNode n in tree.Children) // skip the artificial first level
                //{
                //    m_log.DebugFormat("[XXX] Adding {0}", n.Name);
                //    loo.Add(n);
                //}
                loo.Add(tree);

                env.Data = loo;
                env.Flags = Flags.IsLoggedIn;
                env.State = State.InventoryListForm;
                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            else
            {
                return m_WebApp.ReadFile(env, "index.html");
            }
        }

        public string InventoryPostRequest(Environment env, string action, string folder, string newFolderName, List<string> nodes, List<string> types)
        {
            if (!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[Wifi]: warning: someone is trying to access InventoryPostRequest and Wifi isn't installed!");
                return m_WebApp.ReadFile(env, "index.html");
            }

            m_log.DebugFormat("[Wifi]: InventoryPostRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo))
            {
                env.Session = sinfo;

                if (action.Contains("delete"))
                    Delete(sinfo.Account.PrincipalID, nodes, types);
                else if (action.Contains("move"))
                    Move(sinfo.Account.PrincipalID, nodes, types, folder);
                else if (action.Contains("new"))
                    NewFolder(sinfo.Account.PrincipalID, newFolderName, folder);

                // Send the [new] inventory list
                return InventoryLoadGetRequest(env);
            }
            else
            {
                return m_WebApp.ReadFile(env, "index.html");
            }

        }

        private void SplitNodes(UUID userID, List<string> nodes, List<string> types, List<UUID> items, List<UUID> folders)
        {
            InventoryFolderBase rootFolder = m_InventoryService.GetRootFolder(userID);
            int i = 0;
            foreach (string s in nodes)
            {
                UUID uuid = UUID.Zero;
                if (UUID.TryParse(s, out uuid))
                {
                    // Always ignore any actions pertaining to My Inventory
                    if (rootFolder != null && uuid == rootFolder.ID)
                        continue;

                    if (types[i++] == "folder")
                        folders.Add(uuid);
                    else
                        items.Add(uuid);
                }
            }
        }

        private void Delete(UUID userID, List<string> nodes, List<string> types)
        {
            List<UUID> items = new List<UUID>();
            List<UUID> folders = new List<UUID>();
            SplitNodes(userID, nodes, types, items, folders);

            InventoryFolderBase trash = m_InventoryService.GetFolderForType(userID, AssetType.TrashFolder);
            if (trash != null)
            {
                List<InventoryItemBase> its = new List<InventoryItemBase>();
                foreach (UUID uuid in items)
                {
                    InventoryItemBase itbase = new InventoryItemBase();
                    itbase.ID = uuid;
                    itbase.Folder = trash.ID;
                    its.Add(itbase);
                }
                m_InventoryService.MoveItems(userID, its);

                bool purgeTrash = false;
                foreach (UUID uuid in folders)
                {
                    InventoryFolderBase fbase = new InventoryFolderBase(uuid, userID);
                    if (uuid == trash.ID)
                        purgeTrash = true;
                    else
                    {
                        fbase.ParentID = trash.ID;
                        m_InventoryService.MoveFolder(fbase);
                    }
                }
                if (purgeTrash)
                {
                    InventoryFolderBase fbase = new InventoryFolderBase(trash.ID, userID);
                    m_InventoryService.PurgeFolder(fbase);
                }
            }
        }

        private void Move(UUID userID, List<string> nodes, List<string> types, string folder)
        {
            UUID folderID = UUID.Zero;
            if (UUID.TryParse(folder, out folderID))
            {
                List<UUID> items = new List<UUID>();
                List<UUID> folders = new List<UUID>();
                SplitNodes(userID, nodes, types, items, folders);

                List<InventoryItemBase> its = new List<InventoryItemBase>();
                foreach (UUID uuid in items)
                {
                    InventoryItemBase itbase = new InventoryItemBase();
                    itbase.ID = uuid;
                    itbase.Folder = folderID;
                    its.Add(itbase);
                }
                m_InventoryService.MoveItems(userID, its);

                foreach (UUID uuid in folders)
                {
                    InventoryFolderBase fbase = new InventoryFolderBase();
                    fbase.ID = uuid;
                    fbase.ParentID = folderID;
                    m_InventoryService.MoveFolder(fbase);
                }
            }
        }

        private void NewFolder(UUID userID, string newFolderName, string folder)
        {
            UUID folderID = UUID.Zero;
            if (UUID.TryParse(folder, out folderID))
            {
                InventoryFolderBase fbase = new InventoryFolderBase(UUID.Random(), newFolderName, userID, folderID);
                fbase.Type = (short)AssetType.Folder;
                m_InventoryService.AddFolder(fbase);
            }
        }
    }
}