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

using Diva.Wifi.WifiScript;

namespace Diva.Wifi
{
    public class Environment : IEnvironment
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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

        private SessionInfo m_Session;
        public SessionInfo Session
        {
            get { return m_Session; }
            set { m_Session = value; }
        }

        private List<object> m_Data;
        public List<object> Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        public Environment(Request req)
        {
            m_Request = req;
        }

    }

    public enum StateFlags : uint
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
        NewAccountFormResponse = 512,
        UserSearchForm = 1024,
        UserSearchFormResponse = 2048,
        UserEditForm = 4096,
        UserEditFormResponse = 8192,
        RegionManagementForm = 16384,
        UserDeleteForm = 32768,
        UserDeleteFormResponse = 65536,
        RegionManagementShutdownSuccessful = 131072,
        RegionManagementShutdownUnsuccessful = 262144,
        ForgotPassword = 524288,
        RecoveringPassword = 1048576
    }
}
