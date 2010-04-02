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

namespace Diva.Wifi
{
    public class Environment
    {
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

        private static Dictionary<string, MethodInfo> m_Methods = new Dictionary<string, MethodInfo>();

        public Environment(Request req)
        {
            m_Request = req;
        }


        public static void InitializeWebApp(IWebApp webApp)
        {
            if (webApp == null)
                return;

            m_WebApp = webApp;
            foreach (MethodInfo minfo in m_WebApp.GetType().GetMethods())
                m_Methods[minfo.Name] = minfo;
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
        FailedLogin = 1,
        SuccessfulLogin = 2,
        IsLoggedIn = 4,
        IsAdmin = 8,
        UserAccountForm = 16,
        UserAccountFormResponse = 32,
        NewAccountForm = 64,
        NewAccountFormResponse = 128
    }
}
