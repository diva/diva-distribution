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
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using Nini.Config;
using log4net;
using OpenMetaverse;
using Nwc.XmlRpc;

using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Services.InventoryService;
using OpenSim.Services.GridService;

using Diva.Wifi.WifiScript;
using Environment = Diva.Wifi.Environment;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

using Diva.OpenSimServices;

namespace Diva.Wifi
{
    public partial class Services
    {
        public string ForgotPasswordGetRequest(Environment env)
        {
            m_log.DebugFormat("[WebApp]: ForgotPasswordGetRequest");
            env.Flags = StateFlags.ForgotPassword;
            return m_WebApp.ReadFile(env, "index.html");
        }

        public string ForgotPasswordPostRequest(Environment env, string email)
        {
            UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, email);
            if (account != null)
            {
                string token = m_AuthenticationService.GetToken(account.PrincipalID, 60);
                if (token != string.Empty)
                {
                    string url = m_WebApp.WebAddress + "/wifi/recover/" + token + "?email=" + HttpUtility.UrlEncode(email);

                    MailMessage msg = new MailMessage();
                    msg.From = new MailAddress(m_WebApp.SmtpUsername);
                    msg.To.Add(email);
                    msg.Subject = "[" + m_WebApp.GridName + "] Password Reset";
                    msg.Body = "Let's reset your password. Click here:\n\t";
                    msg.Body += url;
                    msg.Body += "\n\nDiva";
                    m_Client.SendAsync(msg, email);

                    return "<p>Check your email. You must reset your password within 60 minutes.</p>"; /// change this
                }
            }

            return m_WebApp.ReadFile(env, "index.html");
        }

        private static void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            String token = (string)e.UserState;

            if (e.Cancelled)
                m_log.DebugFormat("[ForgotPasswordService] [{0}] Send cancelled.", token);

            if (e.Error != null)
                m_log.DebugFormat("[ForgotPasswordService] [{0}] {1}", token, e.Error.ToString());
            else
                m_log.DebugFormat("[ForgotPasswordService] Password recovery message sent to " + token + ".");
        }

        public string RecoverPasswordGetRequest(Environment env, string email, string token)
        {
            UserAccount account = null;
            if (IsValidToken(email, token, out account))
            {
                PasswordRecoveryData precovery = new PasswordRecoveryData(email, token);
                env.Data = new List<object>();
                env.Data.Add(precovery);
                env.Flags = StateFlags.RecoveringPassword;
                return m_WebApp.ReadFile(env, "index.html");
            }
            else
            {
                return "<p>Invalid token.</p>";
            }
        }

        public string RecoverPasswordPostRequest(Environment env, string email, string token, string newPassword)
        {
            if (newPassword == null || newPassword.Length == 0)
            {
                return "<p>You must enter <strong>some</strong> password!</p>";
            }

            ResetPassword(email, token, newPassword);
            env.Flags = 0;
            return "<p>Great success?</p>";
            //return m_WebApp.ReadFile(env, "index.html");
        }

        public void ResetPassword(string email, string token, string newPassword)
        {
            bool success = false;
            UserAccount account = null;
            if (IsValidToken(email, token, out account))
                success = m_AuthenticationService.SetPassword(account.PrincipalID, newPassword);
        
            if (!success)
                m_log.ErrorFormat("[ForgotPasswordService]: Unable to reset password for account uuid:{0}.", account.PrincipalID);
            else
                m_log.InfoFormat("[ForgotPasswordService]: Password reset for account uuid:{0}", account.PrincipalID);
        }

        private bool IsValidToken(string email, string token, out UserAccount account)
        {
            account = m_UserAccountService.GetUserAccount(UUID.Zero, email);
            if (account != null)
                return (m_AuthenticationService.Verify(account.PrincipalID, token, 10));

            return false;
        }

    }

    class PasswordRecoveryData
    {
        public string Email;
        public string Token;

        public PasswordRecoveryData(string e, string t)
        {
            Email = e; Token = t;
        }
    }
}
