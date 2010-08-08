/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
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
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

using log4net;
using Nini.Config;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;
using OpenSim.Services.Connectors.Hypergrid;

using OpenSim.Services.LLLoginService;

namespace Diva.LoginService
{
    /// <summary>
    /// This class extends OpenSim's LLLoginService. It is intended to support more login methods.
    /// </summary>
    public class LLLoginService : OpenSim.Services.LLLoginService.LLLoginService, ILoginService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LLLoginService(IConfigSource config, ISimulationService simService, ILibraryService libraryService)
            : base(config, simService, libraryService)
        {
            m_log.Debug("[DIVA LLOGIN SERVICE]: Starting...");
        }

        public LLLoginService(IConfigSource config)
            : this(config, null, null)
        {
        }

        /// <summary>
        /// Login procedure, as copied from superclass.
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="passwd"></param>
        /// <param name="startLocation"></param>
        /// <param name="scopeID"></param>
        /// <param name="clientVersion"></param>
        /// <param name="clientIP">The very important TCP/IP EndPoint of the client</param>
        /// <returns></returns>
        /// <remarks>You need to change bits and pieces of this method</remarks>
        public new LoginResponse Login(string firstName, string lastName, string passwd, string startLocation, UUID scopeID, string clientVersion, IPEndPoint clientIP)
        {
            bool success = false;
            UUID session = UUID.Random();

            try
            {
                //
                // Get the account and check that it exists
                //
                UserAccount account = m_UserAccountService.GetUserAccount(scopeID, firstName, lastName);
                if (account == null)
                {
                    // Account doesn't exist. Is this a user from an external ID provider?
                    //
                    // <your code here>
                    //
                    // After verification, your code should create a UserAccount object, filled out properly.
                    // Do not store that account object persistently; we don't want to be creating local accounts
                    // for external users! Create and fill out a UserAccount object, because it has the information
                    // that the rest of the code needs.

                    m_log.InfoFormat("[DIVA LLOGIN SERVICE]: Login failed, reason: user not found");
                    return LLFailedLoginResponse.UserProblem;
                }

                if (account.UserLevel < m_MinLoginLevel)
                {
                    m_log.InfoFormat("[DIVA LLOGIN SERVICE]: Login failed, reason: login is blocked for user level {0}", account.UserLevel);
                    return LLFailedLoginResponse.LoginBlockedProblem;
                }

                // If a scope id is requested, check that the account is in
                // that scope, or unscoped.
                //
                if (scopeID != UUID.Zero)
                {
                    if (account.ScopeID != scopeID && account.ScopeID != UUID.Zero)
                    {
                        m_log.InfoFormat("[DIVA LLOGIN SERVICE]: Login failed, reason: user not found");
                        return LLFailedLoginResponse.UserProblem;
                    }
                }
                else
                {
                    scopeID = account.ScopeID;
                }

                //
                // Authenticate this user
                //
                // Local users and external users will need completely different authentication procedures.
                // The piece of code below is for local users who authenticate with a password.
                //
                if (!passwd.StartsWith("$1$"))
                    passwd = "$1$" + Util.Md5Hash(passwd);
                passwd = passwd.Remove(0, 3); //remove $1$
                string token = m_AuthenticationService.Authenticate(account.PrincipalID, passwd, 30);
                UUID secureSession = UUID.Zero;
                if ((token == string.Empty) || (token != string.Empty && !UUID.TryParse(token, out secureSession)))
                {
                    m_log.InfoFormat("[DIVA LLOGIN SERVICE]: Login failed, reason: authentication failed");
                    return LLFailedLoginResponse.UserProblem;
                }

                //
                // Get the user's inventory
                //
                // m_RequireInventory is set to false in .ini, therefore inventory is not required for login.
                // If you want to change this state of affairs and let external users have local inventory,
                // you need to think carefully about how to do that.
                //
                if (m_RequireInventory && m_InventoryService == null)
                {
                    m_log.WarnFormat("[DIVA LLOGIN SERVICE]: Login failed, reason: inventory service not set up");
                    return LLFailedLoginResponse.InventoryProblem;
                }
                List<InventoryFolderBase> inventorySkel = m_InventoryService.GetInventorySkeleton(account.PrincipalID);
                if (m_RequireInventory && ((inventorySkel == null) || (inventorySkel != null && inventorySkel.Count == 0)))
                {
                    m_log.InfoFormat("[DIVA LLOGIN SERVICE]: Login failed, reason: unable to retrieve user inventory");
                    return LLFailedLoginResponse.InventoryProblem;
                }

                if (inventorySkel == null)
                    inventorySkel = new List<InventoryFolderBase>();

                // Get active gestures
                List<InventoryItemBase> gestures = m_InventoryService.GetActiveGestures(account.PrincipalID);
                m_log.DebugFormat("[LLOGIN SERVICE]: {0} active gestures", gestures.Count);

                //
                // From here on, things should be exactly the same for all users
                //

                //
                // Login the presence
                //
                if (m_PresenceService != null)
                {
                    success = m_PresenceService.LoginAgent(account.PrincipalID.ToString(), session, secureSession);
                    if (!success)
                    {
                        m_log.InfoFormat("[DIVA LLOGIN SERVICE]: Login failed, reason: could not login presence");
                        return LLFailedLoginResponse.GridProblem;
                    }
                }

                //
                // Change Online status and get the home region
                //
                GridRegion home = null;
                GridUserInfo guinfo = m_GridUserService.LoggedIn(account.PrincipalID.ToString());
                if (guinfo != null && (guinfo.HomeRegionID != UUID.Zero) && m_GridService != null)
                {
                    home = m_GridService.GetRegionByUUID(scopeID, guinfo.HomeRegionID);
                }
                if (guinfo == null)
                {
                    // something went wrong, make something up, so that we don't have to test this anywhere else
                    guinfo = new GridUserInfo();
                    guinfo.LastPosition = guinfo.HomePosition = new Vector3(128, 128, 30);
                }

                //
                // Find the destination region/grid
                //
                string where = string.Empty;
                Vector3 position = Vector3.Zero;
                Vector3 lookAt = Vector3.Zero;
                GridRegion gatekeeper = null;
                GridRegion destination = FindDestination(account, scopeID, guinfo, session, startLocation, home, out gatekeeper, out where, out position, out lookAt);
                if (destination == null)
                {
                    m_PresenceService.LogoutAgent(session);
                    m_log.InfoFormat("[DIVA LLOGIN SERVICE]: Login failed, reason: destination not found");
                    return LLFailedLoginResponse.GridProblem;
                }

                //
                // Get the avatar
                //
                AvatarData avatar = null;
                if (m_AvatarService != null)
                {
                    avatar = m_AvatarService.GetAvatar(account.PrincipalID);
                }

                //
                // Instantiate/get the simulation interface and launch an agent at the destination
                //
                string reason = string.Empty;
                AgentCircuitData aCircuit = LaunchAgentAtGrid(gatekeeper, destination, account, avatar, session, secureSession, position, where, clientVersion, clientIP, out where, out reason);

                if (aCircuit == null)
                {
                    m_PresenceService.LogoutAgent(session);
                    m_log.InfoFormat("[DIVA LLOGIN SERVICE]: Login failed, reason: {0}", reason);
                    return LLFailedLoginResponse.AuthorizationProblem;

                }
                // Get Friends list 
                FriendInfo[] friendsList = new FriendInfo[0];
                if (m_FriendsService != null)
                {
                    friendsList = m_FriendsService.GetFriends(account.PrincipalID);
                    m_log.DebugFormat("[DIVA LLOGIN SERVICE]: Retrieved {0} friends", friendsList.Length);
                }

                //
                // Finally, fill out the response and return it
                //
                LLLoginResponse response = new LLLoginResponse(account, aCircuit, guinfo, destination, inventorySkel, friendsList, m_LibraryService,
                    where, startLocation, position, lookAt, gestures, m_WelcomeMessage, home, clientIP, m_MapTileURL, m_SearchURL);

                m_log.DebugFormat("[DIVA LLOGIN SERVICE]: All clear. Sending login response to client.");
                return response;
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[DIVA LLOGIN SERVICE]: Exception processing login for {0} {1}: {2} {3}", firstName, lastName, e.ToString(), e.StackTrace);
                if (m_PresenceService != null)
                    m_PresenceService.LogoutAgent(session);
                return LLFailedLoginResponse.InternalError;
            }
        }

    }
}
