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
using System.Reflection;
using System.Text;

using Nini.Config;
using log4net;

namespace Diva.Wifi
{
    public class Environment
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //
        // Public static Properties of the Environment serve
        // as global constants and variables
        //
        private static Dictionary<string, object> m_StaticVariables = new Dictionary<string, object>();
        public static Dictionary<string, object> StaticVariables
        {
            get { return m_StaticVariables; }
        }

        private static IWebApp m_WebApp;
        public static IWebApp WebAppObj
        {
            get { return m_WebApp; }
        }

        public static Type WebAppType
        {
            get { return m_WebApp.GetType(); }
        }

        private static bool m_Installed = false;
        public static bool IsInstalled
        {
            get { return m_Installed; }
            set { m_Installed = value; }
        }

        private static int m_Port;
        public static int Port
        {
            get { return m_Port; }
        }

        private static string m_GridName;
        public static string GridName
        {
            get { return m_GridName; }
        }
        private static string m_LoginURL;
        public static string LoginURL
        {
            get { return m_LoginURL; }
        }
        private static string m_WebAddress;
        public static string WebAddress
        {
            get { return m_WebAddress; }
        }

        private static string m_AdminFirst;
        public static string AdminFirst
        {
            get { return m_AdminFirst; }
        }
        private static string m_AdminLast;
        public static string AdminLast
        {
            get { return m_AdminLast; }
        }
        private static string m_AdminEmail;
        public static string AdminEmail
        {
            get { return m_AdminEmail; }
        }


        private static Dictionary<string, MethodInfo> m_Methods = new Dictionary<string, MethodInfo>();

        //
        // Instance variables are per request
        //

        private Request m_Request;
        public Request Request
        {
            get { return m_Request; }
        }

        private StateFlags m_Flags;
        public StateFlags Flags
        {
            get { return m_Flags; }
            set { m_Flags = value; }
        }

        public Environment(Request req)
        {
            m_Request = req;
        }


        public static void InitializeWebApp(IWebApp webApp, IConfigSource config, string configName)
        {
            if (webApp == null)
                return;

            m_WebApp = webApp;
            foreach (MethodInfo minfo in m_WebApp.GetType().GetMethods())
                m_Methods[minfo.Name] = minfo;

            // Read config vars
            IConfig appConfig = config.Configs[configName];
            m_GridName = appConfig.GetString("GridName", "My World");
            m_LoginURL = appConfig.GetString("LoginURL", "http://localhost:9000");
            m_WebAddress = appConfig.GetString("WebAddress", "http://localhost:8080");

            m_AdminFirst = appConfig.GetString("AdminFirst", string.Empty);
            m_AdminLast = appConfig.GetString("AdminLast", string.Empty);
            m_AdminEmail = appConfig.GetString("AdminEmail", string.Empty);

            if (m_AdminFirst == string.Empty || m_AdminLast == string.Empty || m_AdminEmail == string.Empty)
                // Can't proceed
                throw new Exception("Can't proceed. Please specify the administrator account in Wifi.ini");

            IConfig serverConfig = config.Configs["Network"];
            if (serverConfig != null)
                m_Port = Int32.Parse(serverConfig.GetString("port", "80"));

            m_log.DebugFormat("[Environment]: Initialized. Admin account is {0} {1}", m_AdminFirst, m_AdminLast);
        }

        public static MethodInfo GetMethod(string name)
        {
            if (m_Methods.ContainsKey(name))
                return m_Methods[name];

            return null;
        }
    }

    public enum StateFlags : int
    {
        InstallForm = 1,
        InstallFormResponse = 2,
        FailedLogin = 4,
        SuccessfulLogin = 8,
        IsLoggedIn = 16,
        IsAdmin = 32,
        UserAccountForm = 64,
        UserAccountFormResponse = 128,
        NewAccountForm = 256,
        NewAccountFormResponse = 512
    }
}
