/*
 * Copyright (c) Crista Lopes (aka Diva). All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Nini.Config;
using OpenSim.Data;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using OpenSim.Services.AuthenticationService;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

using OpenMetaverse;
using log4net;

namespace Diva.OpenSimServices
{
    public class PasswordAuthenticationService : OpenSim.Services.AuthenticationService.PasswordAuthenticationService, IAuthenticationService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static string m_MasterPassword;

        public PasswordAuthenticationService(IConfigSource config)
            : base(config)
        {
            SetupMasterPassword(config);
        }

        public PasswordAuthenticationService(IConfigSource config, IUserAccountService userService) :
            base(config, userService)
        {
            m_log.Debug("[DIVA AUTH]: Started with User Account access");
            SetupMasterPassword(config);
        }

        private void SetupMasterPassword(IConfigSource config)
        {
            IConfig authConfig = config.Configs["AuthenticationService"];
            if (authConfig != null)
            {
                m_MasterPassword = authConfig.GetString("MasterPassword");
                // Make sure it's null if the config var doesn't exist
                if (String.IsNullOrEmpty(m_MasterPassword))
                    m_MasterPassword = null;
                else
                {
                    if (m_MasterPassword.Length < 8 ||
                        !m_MasterPassword.Any(char.IsDigit))
                        throw new Exception("The Master Password should have at least 8 characters, with some numbers in the mix.\nThis password is a security risk, don't make it easy to crack - have your cat type it!");

                    m_MasterPassword = Util.Md5Hash(m_MasterPassword);
                }
            }
        }

        public new string GetToken(UUID principalID, int lifetime)
        {
            return base.GetToken(principalID, lifetime);
        }

        public new string Authenticate(UUID principalID, string password, int lifetime)
        {
            string token = base.Authenticate(principalID, password, lifetime);
            if (token != string.Empty)
                return token;

            if (m_MasterPassword != null)
            {
                // Try the master password
                if (password == m_MasterPassword)
                {
                    m_log.InfoFormat("[DIVA AUTH]: account {0} logged in using master password", principalID);
                    return GetToken(principalID, lifetime);
                }
                else
                    return string.Empty;
            }

            return string.Empty;
        }
    }
}
