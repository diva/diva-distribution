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
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using Nini.Config;
using log4net;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Services.InventoryService;

using Diva.Wifi.WifiScript;
using Environment = Diva.Wifi.Environment;

namespace Diva.Wifi
{
    public class WebApp
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string m_DocsPath = System.IO.Path.Combine("..", "WifiPages");
        public string DocsPath
        {
            get { return m_DocsPath; }
        }

        private Type m_ExtensionMethods;

        #region IWebApp variables accessible to the WifiScript engine

        private bool m_Installed = false;
        public bool IsInstalled
        {
            get { return m_Installed; }
            set { m_Installed = value; }
        }

        private int m_Port;
        public int Port
        {
            get { return m_Port; }
        }

        private string m_GridName;
        public string GridName
        {
            get { return m_GridName; }
        }
        private string m_LoginURL;
        public string LoginURL
        {
            get { return m_LoginURL; }
        }
        private string m_WebAddress;
        public string WebAddress
        {
            get { return m_WebAddress; }
        }

        private string m_AdminFirst;
        public string AdminFirst
        {
            get { return m_AdminFirst; }
        }
        private string m_AdminLast;
        public string AdminLast
        {
            get { return m_AdminLast; }
        }
        private string m_AdminEmail;
        public string AdminEmail
        {
            get { return m_AdminEmail; }
        }
        private string m_RemoteAdminPassword;
        public string RemoteAdminPassword
        {
            get { return m_RemoteAdminPassword; }
        }

        private string m_SmtpHost;
        public string SmtpHost
        {
            get { return m_SmtpHost; }
        }

        private int m_SmtpPort;
        public int SmtpPort
        {
            get { return m_SmtpPort; }
        }

        private string m_SmtpUsername;
        public string SmtpUsername
        {
            get { return m_SmtpUsername; }
        }

        private string m_SmtpPassword;
        public string SmtpPassword
        {
            get { return m_SmtpPassword; }
        }

        #endregion

        public readonly Services Services;
        public readonly WifiScriptFace WifiScriptFace;


        public WebApp(IConfigSource config, string configName, IHttpServer server)
        {

            ReadConfigs(config, configName);

            // Create the two parts
            Services = new Services(config, configName, this);
            WifiScriptFace = new WifiScriptFace(this);

            m_ExtensionMethods = typeof(ExtensionMethods);

            m_log.DebugFormat("[WebApp]: Starting with extension methods type {0}", m_ExtensionMethods);

        }

        public void ReadConfigs(IConfigSource config, string configName)
        {
            // Read config vars
            IConfig appConfig = config.Configs[configName];
            m_GridName = appConfig.GetString("GridName", "My World");
            m_LoginURL = appConfig.GetString("LoginURL", "http://localhost:9000");
            m_WebAddress = appConfig.GetString("WebAddress", "http://localhost:8080");
            m_WebAddress = m_WebAddress.Trim(new char[] { '/' }); 

            m_SmtpHost = appConfig.GetString("SmtpHost", "smtp.gmail.com");
            m_SmtpPort = Int32.Parse(appConfig.GetString("SmtpPort", "587"));
            m_SmtpUsername = appConfig.GetString("SmtpUsername", "ddistribution8@gmail.com");
            m_SmtpPassword = appConfig.GetString("SmtpPassword", "skeeterpants7");

            m_AdminFirst = appConfig.GetString("AdminFirst", string.Empty);
            m_AdminLast = appConfig.GetString("AdminLast", string.Empty);
            m_AdminEmail = appConfig.GetString("AdminEmail", string.Empty);

            m_RemoteAdminPassword = appConfig.GetString("RemoteAdminPassword", string.Empty);

            if (m_AdminFirst == string.Empty || m_AdminLast == string.Empty || m_AdminEmail == string.Empty)
                // Can't proceed
                throw new Exception("Can't proceed. Please specify the administrator account in Wifi.ini");

            IConfig serverConfig = config.Configs["Network"];
            if (serverConfig != null)
                m_Port = Int32.Parse(serverConfig.GetString("port", "80"));

            m_log.DebugFormat("[Environment]: Initialized. Admin account is {0} {1}", m_AdminFirst, m_AdminLast);
        }


        #region read html files

        public string ReadFile(Environment env, string path)
        {
            return ReadFile(env, path, env.Data);
        }

        public string ReadFile(Environment env, string path, List<object> lot)
        {
            string file = Path.Combine(WifiUtils.DocsPath, path);
            try
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    string content = sr.ReadToEnd();
                    Processor p = new Processor(WifiScriptFace, m_ExtensionMethods, env, lot);
                    return p.Process(content);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[WebApp]: Exception on ReadFile {0}: {1}", path, e);
                return string.Empty;
            }
        }

        #endregion

    }

    public struct SessionInfo
    {
        public string Sid;
        public string IpAddress;
        public UserAccount Account;
    }
}
