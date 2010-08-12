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

namespace Diva.Wifi
{
    public partial class Services
    {
        public string NewAccountGetRequest(Environment env)
        {
            m_log.DebugFormat("[Wifi]: NewAccountGetRequest");
            Request request = env.Request;

            env.State = State.NewAccountForm;
            return m_WebApp.ReadFile(env, "index.html");
        }

        public string NewAccountPostRequest(Environment env, string first, string last, string email, string password, string password2, AvatarType avatar)
        {
            if (!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[Wifi]: warning: someone is trying to access NewAccountPostRequest and Wifi isn't installed!");
                return m_WebApp.ReadFile(env, "index.html");
            }


            m_log.DebugFormat("[Wifi]: NewAccountPostRequest");
            Request request = env.Request;

            if ((password != string.Empty) && (password == password2) && (first != string.Empty) && (last != string.Empty))
            {
                UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, first, last);
                if (account == null)
                {

                    Dictionary<string, object> urls = new Dictionary<string, object>();
                    urls["HomeURI"] = m_WebApp.LoginURL.ToString();
                    urls["InventoryServerURI"] = m_WebApp.LoginURL.ToString();
                    urls["AssetServerURI"] = m_WebApp.LoginURL.ToString();

                    if (m_WebApp.AccountConfirmationRequired)
                    {
                        //attach pending identifier to first name
                        first = "*pending* " + first;
                        // Store the password temporarily here
                        urls["Password"] = password;
                        urls["Avatar"] = avatar.ToString();
                    }

                    // Create the account
                    account = new UserAccount(UUID.Zero, first, last, email);
                    account.ServiceURLs = urls;
                    account.UserTitle = "Local User";

                    m_UserAccountService.StoreUserAccount(account);

                    if (!m_WebApp.AccountConfirmationRequired)
                    {
                        // Create the inventory
                        m_InventoryService.CreateUserInventory(account.PrincipalID);

                        // Store the password
                        m_AuthenticationService.SetPassword(account.PrincipalID, password);

                        // Set avatar
                        SetAvatar(account.PrincipalID, avatar);
                    }
                    else if (m_WebApp.AdminEmail != string.Empty)
                    {
                        string message = "New account " + first + " " + last + " created in " + m_WebApp.GridName;
                        message += " is waiting your approval.";
                        message += "\n\n" + m_WebApp.WebAddress + "/wifi";
                        SendEMail(m_WebApp.AdminEmail, "Account waiting approval", message);
                    }

                    env.State = State.NewAccountFormResponse;
                    m_log.DebugFormat("[Wifi]: Created account for user {0}", account.Name);
                }
                else
                    m_log.DebugFormat("[Wifi]: Attempt at creating an account that already exists");
            }
            else
            {
                m_log.DebugFormat("[Wifi]: did not create account because of password and/or user name problems");
                env.State = State.NewAccountForm;
            }

            return m_WebApp.ReadFile(env, "index.html");

        }

        private void SetAvatar(UUID newUser, AvatarType avatarType)
        {
            UserAccount account = null;
            string[] parts = null;
            
            if (avatarType == AvatarType.Female)
                parts = m_WebApp.AvatarFemaleAccount.Split(new char[] { ' ' });
            else if (avatarType == AvatarType.Male)
                parts = m_WebApp.AvatarMaleAccount.Split(new char[] { ' ' });
            else
                parts = m_WebApp.AvatarNeutralAccount.Split(new char[] { ' ' });

            if (parts == null || (parts != null && parts.Length != 2))
                return;

            account = m_UserAccountService.GetUserAccount(UUID.Zero, parts[0], parts[1]);
            if (account == null)
            {
                m_log.WarnFormat("[Wifi]: Tried to get avatar of account {0} {1} but that account does not exist", parts[0], parts[1]);
                return;
            }

            AvatarData avatar = m_AvatarService.GetAvatar(account.PrincipalID);

            if (avatar == null)
            {
                m_log.WarnFormat("[Wifi]: Avatar of account {0} {1} is null", parts[0], parts[1]);
                return;
            }

            m_log.DebugFormat("[Wifi]: Creating {0} avatar (account {1} {2})", avatarType, parts[0], parts[1]);
            
            // Get and replicate the attachments
            // and put them in a folder called Default Avatar under Clothing
            UUID defaultFolderID = CreateDefaultAvatarFolder(newUser);

            if (defaultFolderID != UUID.Zero)
            {
                Dictionary<string, string> attchs = new Dictionary<string, string>();
                foreach (KeyValuePair<string, string> _kvp in avatar.Data)
                {
                    if (_kvp.Value != null)
                    {
                        string itemID = CreateItemFrom(_kvp.Value, newUser, defaultFolderID);
                        if (itemID != string.Empty)
                            attchs[_kvp.Key] = itemID;
                    }
                }

                foreach (KeyValuePair<string, string> _kvp in attchs)
                    avatar.Data[_kvp.Key] = _kvp.Value;

                m_AvatarService.SetAvatar(newUser, avatar);
            }
            else
                m_log.DebugFormat("[Wifi]: could not create Default Avatar folder");
        }

        private UUID CreateDefaultAvatarFolder(UUID newUserID)
        {
            InventoryFolderBase clothing = m_InventoryService.GetFolderForType(newUserID, AssetType.Clothing);
            if (clothing == null)
            {
                clothing = m_InventoryService.GetRootFolder(newUserID);
                if (clothing == null)
                    return UUID.Zero;
            }

            InventoryFolderBase defaultAvatarFolder = new InventoryFolderBase(UUID.Random(), "Default Avatar", newUserID, clothing.ID);
            defaultAvatarFolder.Version = 1;
            defaultAvatarFolder.Type = (short)AssetType.Clothing;

            if (!m_InventoryService.AddFolder(defaultAvatarFolder))
                m_log.DebugFormat("[Wifi]: Failed to store Default Avatar folder");

            return defaultAvatarFolder.ID;
        }

        private string CreateItemFrom(string itemID, UUID newUserID, UUID defaultFolderID)
        {
            InventoryItemBase item = new InventoryItemBase();
            item.Owner = newUserID;
            InventoryItemBase retrievedItem = null;
            InventoryItemBase copyItem = null;

            UUID uuid = UUID.Zero;
            if (UUID.TryParse(itemID, out uuid))
            {
                item.ID = uuid;
                retrievedItem = m_InventoryService.GetItem(item);
                if (retrievedItem != null)
                {
                    copyItem = CopyFrom(retrievedItem, newUserID, defaultFolderID);
                    m_InventoryService.AddItem(copyItem);
                    return copyItem.ID.ToString();
                }
            }

            return string.Empty;
        }

        private InventoryItemBase CopyFrom(InventoryItemBase from, UUID newUserID, UUID defaultFolderID)
        {
            InventoryItemBase to = (InventoryItemBase)from.Clone();
            to.Owner = newUserID;
            to.Folder = defaultFolderID;
            to.ID = UUID.Random();

            return to;
        }
    }
}
