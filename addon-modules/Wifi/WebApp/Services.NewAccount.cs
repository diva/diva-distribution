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
            m_log.DebugFormat("[WebApp]: NewAccountGetRequest");
            Request request = env.Request;

            env.State = State.NewAccountForm;
            return m_WebApp.ReadFile(env, "index.html");
        }

        public string NewAccountPostRequest(Environment env, string first, string last, string email, string password, string password2, AvatarType avatar)
        {
            if (!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[WebApp]: warning: someone is trying to access NewAccountPostRequest and Wifi isn't installed!");
                return m_WebApp.ReadFile(env, "index.html");
            }


            m_log.DebugFormat("[WebApp]: NewAccountPostRequest");
            Request request = env.Request;

            if ((password != string.Empty) && (password == password2))
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
                        urls["Avatar"] = avatar;
                    }

                    // Create the account
                    account = new UserAccount(UUID.Zero, first, last, email);
                    account.ServiceURLs = urls;

                    m_UserAccountService.StoreUserAccount(account);

                    if (!m_WebApp.AccountConfirmationRequired)
                    {
                        // Create the inventory
                        m_InventoryService.CreateUserInventory(account.PrincipalID);

                        // Store the password
                        m_AuthenticationService.SetPassword(account.PrincipalID, password);

                        // Set avatar
                        if (avatar != AvatarType.Neutral)
                        {
                            SetAvatar(account.PrincipalID, avatar);
                        }
                    }

                    env.State = State.NewAccountFormResponse;
                    m_log.DebugFormat("[WebApp]: Created account for user {0}", account.Name);
                }
                else
                    m_log.DebugFormat("[WebApp]: Attempt at creating an account that already exists");
            }
            else
            {
                m_log.DebugFormat("[WebApp]: did not create account because of password problems");
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
            else
                parts = m_WebApp.AvatarMaleAccount.Split(new char[] { ' ' });

            if (parts.Length != 2)
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

            // Get and replicate the attachments
            Dictionary<string, string> attchs = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> _kvp in avatar.Data)
                if (_kvp.Key.StartsWith("_ap_"))
                {
                    string itemID = CopyItem(_kvp.Value, account.PrincipalID);
                    if (itemID != string.Empty)
                        attchs[_kvp.Key] = itemID;
                }
            avatar.Data = attchs;

            foreach (KeyValuePair<string, string> _kvp in attchs)
            {

            }

            m_AvatarService.SetAvatar(newUser, avatar);
        }

        private string CopyItem(string itemID, UUID newUserID)
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
                copyItem = CopyFrom(retrievedItem, newUserID);
                return copyItem.ID.ToString();
            }

            return string.Empty;
        }

        private InventoryItemBase CopyFrom(InventoryItemBase from, UUID newUserID)
        {
            InventoryItemBase to = (InventoryItemBase)from.Clone();
            to.Owner = newUserID;
            to.ID = UUID.Random();

            return to;
        }
    }
}
