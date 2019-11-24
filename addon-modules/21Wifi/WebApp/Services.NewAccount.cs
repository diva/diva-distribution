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
using System.Linq;

using log4net;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Services.Interfaces;

using Diva.Interfaces;
using Diva.Utils;
using Environment = Diva.Utils.Environment;

namespace Diva.Wifi
{
    public partial class Services
    {
        public string NewAccountGetRequest(Environment env)
        {
            m_log.DebugFormat("[Wifi]: NewAccountGetRequest");
            Request request = env.TheRequest;

            env.State = State.NewAccountForm;
            env.Data = GetDefaultAvatarSelectionList();

            return m_WebApp.ReadFile(env, "index.html");
        }

        public string NewAccountPostRequest(Environment env, string first, string last, string email, string password, string password2, string avatarType)
        {
            if (!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[Wifi]: warning: someone is trying to access NewAccountPostRequest and Wifi isn't installed!");
                return m_WebApp.ReadFile(env, "index.html");
            }


            m_log.DebugFormat("[Wifi]: NewAccountPostRequest");
            Request request = env.TheRequest;

            if ((password != string.Empty) && (password == password2) && (first != string.Empty) && (last != string.Empty))
            {
                UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, first, last);
                if (account == null)
                    account = m_UserAccountService.GetUserAccount(UUID.Zero, m_PendingIdentifier + first, last);
                if (account == null)
                {
                    Dictionary<string, object> urls = new Dictionary<string, object>();

                    if (m_WebApp.AccountConfirmationRequired)
                    {
                        //attach pending identifier to first name
                        first = m_PendingIdentifier + first;
                        // Store the password temporarily here
                        urls["Password"] = password;
                        urls["Avatar"] = avatarType;
                        if (env.LanguageInfo != null)
                            urls["Language"] = Localization.LanguageInfoToString(env.LanguageInfo);
                    }

                    // Create the account
                    account = new UserAccount(UUID.Zero, first, last, email);
                    account.ServiceURLs = urls;
                    account.UserTitle = "Local User";

                    m_UserAccountService.StoreUserAccount(account);

                    string notification = _("Your account has been created.", env);
                    if (!m_WebApp.AccountConfirmationRequired)
                    {
                        // Create the inventory
                        m_InventoryService.CreateUserInventory(account.PrincipalID);

                        // Store the password
                        m_AuthenticationService.SetPassword(account.PrincipalID, password);

                        // Set avatar
                        SetAvatar(env, account.PrincipalID, avatarType);
                    }
                    else if (m_WebApp.AdminEmail != string.Empty)
                    {
                        string message = string.Format(
                            _("New account {0} {1} created in {2} is awaiting your approval.",
                            m_WebApp.AdminLanguage),
                            first, last, m_WebApp.GridName);
                        message += "\n\n" + m_WebApp.WebAddress + "/wifi";
                        SendEMail(
                            m_WebApp.AdminEmail,
                            _("Account awaiting approval", m_WebApp.AdminLanguage),
                            message);
                        notification = _("Your account awaits administrator approval.", env);
                    }

                    NotifyWithoutButton(env, notification);
                    m_log.DebugFormat("[Wifi]: Created account for user {0}", account.Name);
                }
                else
                {
                    m_log.DebugFormat("[Wifi]: Attempt at creating an account that already exists");
                    env.State = State.NewAccountForm;
                    env.Data = GetDefaultAvatarSelectionList();
                }
            }
            else
            {
                m_log.DebugFormat("[Wifi]: did not create account because of password and/or user name problems");
                env.State = State.NewAccountForm;
                env.Data = GetDefaultAvatarSelectionList();
            }

            return m_WebApp.ReadFile(env, "index.html");

        }

        public void SetAvatar(IEnvironment e, UUID newUser, string avatarType)
        {
            Environment env = (Environment)e;
            UserAccount account = null;
            string[] parts = null;

            Avatar defaultAvatar = m_WebApp.DefaultAvatars.FirstOrDefault(av => av.Type.Equals(avatarType));
            if (defaultAvatar == null)
            {
                m_log.WarnFormat("[Wifi]: Avatar type {0} not found in configuration", avatarType);
                return;
            }

            if (defaultAvatar.Name != null)
            {
                m_log.DebugFormat("[Wifi]: Avatar type is {0}", defaultAvatar.Name);
                parts = defaultAvatar.Name.Split(new char[] { ' ' });
            }

            if (parts == null || (parts != null && parts.Length != 2))
                return;

            // "Unwear" anything that may be in Current Outfit folder
            InventoryFolderBase cof = m_InventoryService.GetFolderForType(newUser, FolderType.CurrentOutfit);
            if (cof != null)
            {
                InventoryCollection contents = m_InventoryService.GetFolderContent(newUser, cof.ID);
                if (contents.Items.Count > 0)
                {
                    List<UUID> cof_items = new List<UUID>();
                    foreach (InventoryItemBase item in contents.Items)
                    {
                        cof_items.Add(item.ID);
                    }
                    if (cof != null)
                    {
                        m_log.DebugFormat("[Wifi]: Deleting old items in Current Outfit Folder");
                        m_InventoryService.DeleteItems(newUser, cof_items);
                    }
                }
            }
            else
                m_log.DebugFormat("[Wifi]: COF doesn't exist?!");

            m_UserAccountService.EstablishAppearance(newUser, defaultAvatar.Name);

            // Set home and last location for new account
            // Config setting takes precedence over home location of default avatar
            PrepareHomeLocation();
            UUID homeRegion = Avatar.HomeRegion;
            Vector3 position = Avatar.HomeLocation;
            Vector3 lookAt = new Vector3();
            if (homeRegion == UUID.Zero && account != null)
            {
                GridUserInfo userInfo = m_GridUserService.GetGridUserInfo(account.PrincipalID.ToString());
                if (userInfo != null)
                {
                    homeRegion = userInfo.HomeRegionID;
                    position = userInfo.HomePosition;
                    lookAt = userInfo.HomeLookAt;
                }
            }
            if (homeRegion != UUID.Zero)
            {
                m_GridUserService.SetHome(newUser.ToString(), homeRegion, position, lookAt);
                m_GridUserService.SetLastPosition(newUser.ToString(), UUID.Zero, homeRegion, position, lookAt);
            }
        }

        private UUID CreateDefaultAvatarFolder(UUID newUserID, string folderName)
        {
            InventoryFolderBase clothing = m_InventoryService.GetFolderForType(newUserID, FolderType.Clothing);
            if (clothing == null)
            {
                clothing = m_InventoryService.GetRootFolder(newUserID);
                if (clothing == null)
                    return UUID.Zero;
            }

            InventoryFolderBase defaultAvatarFolder = new InventoryFolderBase(UUID.Random(), folderName, newUserID, clothing.ID);
            defaultAvatarFolder.Version = 1;
            defaultAvatarFolder.Type = (short)FolderType.None;

            if (!m_InventoryService.AddFolder(defaultAvatarFolder))
                m_log.DebugFormat("[Wifi]: Failed to store {0} folder", folderName);

            return defaultAvatarFolder.ID;
        }

        private string CreateItemFrom(string key, string value, UUID newUserID, UUID defaultFolderID)
        {
            InventoryItemBase retrievedItem = null;
            InventoryItemBase copyItem = null;

            if (key.Contains("Wearable"))
            {
                if (!value.Contains(':'))
                    return String.Empty;
            
                string[] parts = new string[2];
                parts = value.Split(new char[] { ':' });
                if (parts.Length != 2)
                    return String.Empty;

                UUID uuid = UUID.Zero;
                if (UUID.TryParse(parts[0], out uuid))
                {
                    retrievedItem = m_InventoryService.GetItem(newUserID, uuid);
                    if (retrievedItem != null)
                    {
                        copyItem = CopyFrom(retrievedItem, newUserID, defaultFolderID);
                        m_InventoryService.AddItem(copyItem);
                        return copyItem.ID.ToString() + ':' + retrievedItem.AssetID;
                    }
                }

            }
            else if (key.Contains("_ap_"))
            {
                string[] attachs = value.Split(new char[] { ',' });
                UUID uuid;
                List<String> attachids = new List<string>();
                foreach (string s in attachs)
                {
                    if (UUID.TryParse(s, out uuid))
                    {
                        retrievedItem = m_InventoryService.GetItem(newUserID, uuid);
                        if (retrievedItem != null)
                        {
                            copyItem = CopyFrom(retrievedItem, newUserID, defaultFolderID);
                            m_InventoryService.AddItem(copyItem);
                            attachids.Add(copyItem.ID.ToString());
                        }
                    }
                }
                return string.Join(",", attachids.ToArray());
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
