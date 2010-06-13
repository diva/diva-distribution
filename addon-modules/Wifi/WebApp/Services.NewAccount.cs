using System;
using System.Collections.Generic;

using log4net;
using OpenMetaverse;

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

        public string NewAccountPostRequest(Environment env, string first, string last, string email, string password, string password2)
        {
            if (!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[WebApp]: warning: someone is trying to access NewAccountPostRequest and Wifi isn't isntalled!");
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
    }
}
