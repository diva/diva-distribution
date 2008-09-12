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
 *     * Neither the name of the OpenSim Project nor the
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
using System.Reflection;
using System.Text.RegularExpressions;
using OpenMetaverse;
using log4net;
using OpenSim.Framework;
using OpenSim.Framework.Communications;

namespace OpenSim.Region.Communications.Local
{
    public delegate void LoginToRegionEvent(ulong regionHandle, Login login);

    public class LocalLoginService : LoginService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private CommunicationsLocal m_Parent;

        private NetworkServersInfo serversInfo;
        private uint defaultHomeX;
        private uint defaultHomeY;
        private bool authUsers = false;

        public event LoginToRegionEvent OnLoginToRegion;

        private LoginToRegionEvent handlerLoginToRegion = null; // OnLoginToRegion;

        public LocalLoginService(UserManagerBase userManager, string welcomeMess,
                                 CommunicationsLocal parent, NetworkServersInfo serversInfo,
                                 bool authenticate)
            : base(userManager, parent.UserProfileCacheService.libraryRoot, welcomeMess)
        {
            m_Parent = parent;
            this.serversInfo = serversInfo;
            defaultHomeX = this.serversInfo.DefaultHomeLocX;
            defaultHomeY = this.serversInfo.DefaultHomeLocY;
            authUsers = authenticate;
        }

        public override UserProfileData GetTheUser(string firstname, string lastname)
        {
            UserProfileData profile = m_userManager.GetUserProfile(firstname, lastname);
            if (profile != null)
            {
                return profile;
            }

            if (!authUsers)
            {
                //no current user account so make one
                m_log.Info("[LOGIN]: No user account found so creating a new one.");

                m_userManager.AddUserProfile(firstname, lastname, "test", defaultHomeX, defaultHomeY);

                profile = m_userManager.GetUserProfile(firstname, lastname);
                if (profile != null)
                {
                    m_Parent.InterServiceInventoryService.CreateNewUserInventory(profile.ID);
                }

                return profile;
            }
            return null;
        }

        public override bool AuthenticateUser(UserProfileData profile, string password)
        {
            if (!authUsers)
            {
                //for now we will accept any password in sandbox mode
                m_log.Info("[LOGIN]: Authorising user (no actual password check)");

                return true;
            }
            else
            {
                m_log.Info(
                    "[LOGIN]: Authenticating " + profile.FirstName + " " + profile.SurName);

                if (!password.StartsWith("$1$"))
                    password = "$1$" + Util.Md5Hash(password);

                password = password.Remove(0, 3); //remove $1$

                string s = Util.Md5Hash(password + ":" + profile.PasswordSalt);

                bool loginresult = (profile.PasswordHash.Equals(s.ToString(), StringComparison.InvariantCultureIgnoreCase)
                            || profile.PasswordHash.Equals(password, StringComparison.InvariantCultureIgnoreCase));
                return loginresult;
            }
        }

        /// <summary>
        /// Customises the login response and fills in missing values.
        /// </summary>
        /// <param name="response">The existing response</param>
        /// <param name="theUser">The user profile</param>
        /// <param name="startLocationRequest">The requested start location</param>
        public override bool CustomiseResponse(LoginResponse response, UserProfileData theUser, string startLocationRequest)
        {
            // HomeLocation
            RegionInfo homeInfo = null;
            // use the homeRegionID if it is stored already. If not, use the regionHandle as before
            if (theUser.HomeRegionID != UUID.Zero)
                homeInfo = m_Parent.GridService.RequestNeighbourInfo(theUser.HomeRegionID);
            else
                homeInfo = m_Parent.GridService.RequestNeighbourInfo(theUser.HomeRegion);
            if (homeInfo != null)
            {
                response.Home =
                    string.Format(
                        "{{'region_handle':[r{0},r{1}], 'position':[r{2},r{3},r{4}], 'look_at':[r{5},r{6},r{7}]}}",
                        (homeInfo.RegionLocX*Constants.RegionSize),
                        (homeInfo.RegionLocY*Constants.RegionSize),
                        theUser.HomeLocation.X, theUser.HomeLocation.Y, theUser.HomeLocation.Z,
                        theUser.HomeLookAt.X, theUser.HomeLookAt.Y, theUser.HomeLookAt.Z);
            }
            else
            {
                // Emergency mode: Home-region isn't available, so we can't request the region info.
                // Use the stored home regionHandle instead.
                // NOTE: If the home-region moves, this will be wrong until the users update their user-profile again
                ulong regionX = theUser.HomeRegion >> 32;
                ulong regionY = theUser.HomeRegion & 0xffffffff;
                response.Home =
                    string.Format(
                        "{{'region_handle':[r{0},r{1}], 'position':[r{2},r{3},r{4}], 'look_at':[r{5},r{6},r{7}]}}",
                        regionX, regionY,
                        theUser.HomeLocation.X, theUser.HomeLocation.Y, theUser.HomeLocation.Z,
                        theUser.HomeLookAt.X, theUser.HomeLookAt.Y, theUser.HomeLookAt.Z);
                m_log.InfoFormat("[LOGIN] Home region of user {0} {1} is not available; using computed region position {2} {3}",
                                 theUser.FirstName, theUser.SurName,
                                 regionX, regionY);
            }

            // StartLocation
            RegionInfo regionInfo = null;
            if (startLocationRequest == "home")
            {
                regionInfo = homeInfo;
                theUser.CurrentAgent.Position = theUser.HomeLocation;
                response.LookAt = "[r" + theUser.HomeLookAt.X.ToString() + ",r" + theUser.HomeLookAt.Y.ToString() + ",r" + theUser.HomeLookAt.Z.ToString() + "]";
            }
            else if (startLocationRequest == "last")
            {
                regionInfo = m_Parent.GridService.RequestNeighbourInfo(theUser.CurrentAgent.Region);
                response.LookAt = "[r" + theUser.CurrentAgent.LookAt.X.ToString() + ",r" + theUser.CurrentAgent.LookAt.Y.ToString() + ",r" + theUser.CurrentAgent.LookAt.Z.ToString() + "]";
            }
            else
            {
                Regex reURI = new Regex(@"^uri:(?<region>[^&]+)&(?<x>\d+)&(?<y>\d+)&(?<z>\d+)$");
                Match uriMatch = reURI.Match(startLocationRequest);
                if (uriMatch == null)
                {
                    m_log.InfoFormat("[LOGIN]: Got Custom Login URL {0}, but can't process it", startLocationRequest);
                }
                else
                {
                    string region = uriMatch.Groups["region"].ToString();
                    regionInfo = m_Parent.GridService.RequestClosestRegion(region);
                    if (regionInfo == null)
                    {
                        m_log.InfoFormat("[LOGIN]: Got Custom Login URL {0}, can't locate region {1}", startLocationRequest, region);
                    }
                    else
                    {
                        theUser.CurrentAgent.Position = new Vector3(float.Parse(uriMatch.Groups["x"].Value),
                            float.Parse(uriMatch.Groups["y"].Value), float.Parse(uriMatch.Groups["x"].Value));
                    }
                }
                response.LookAt = "[r0,r1,r0]";
                // can be: last, home, safe, url
                response.StartLocation = "url";
            }

            if ((regionInfo != null) && (PrepareLoginToRegion(regionInfo, theUser, response)))
            {
                    return true;
            }

            // StartLocation not available, send him to a nearby region instead
            // regionInfo = m_Parent.GridService.RequestClosestRegion("");
            //m_log.InfoFormat("[LOGIN]: StartLocation not available sending to region {0}", regionInfo.regionName);

            // Send him to default region instead
            ulong defaultHandle = (((ulong)defaultHomeX * Constants.RegionSize) << 32) |
                                  ((ulong)defaultHomeY * Constants.RegionSize);

            if ((regionInfo != null) && (defaultHandle == regionInfo.RegionHandle))
            {
                m_log.ErrorFormat("[LOGIN]: Not trying the default region since this is the same as the selected region");
                return false;
            }

            m_log.Error("[LOGIN]: Sending user to default region " + defaultHandle + " instead");
            regionInfo = m_Parent.GridService.RequestNeighbourInfo(defaultHandle);

            // Customise the response
            //response.Home =
            //    string.Format(
            //        "{{'region_handle':[r{0},r{1}], 'position':[r{2},r{3},r{4}], 'look_at':[r{5},r{6},r{7}]}}",
            //        (SimInfo.regionLocX * Constants.RegionSize),
            //        (SimInfo.regionLocY*Constants.RegionSize),
            //        theUser.HomeLocation.X, theUser.HomeLocation.Y, theUser.HomeLocation.Z,
            //        theUser.HomeLookAt.X, theUser.HomeLookAt.Y, theUser.HomeLookAt.Z);
            theUser.CurrentAgent.Position = new Vector3(128,128,0);
            response.StartLocation = "safe";
                
            return PrepareLoginToRegion(regionInfo, theUser, response);
        }

        /// <summary>
        /// Prepare a login to the given region.  This involves both telling the region to expect a connection
        /// and appropriately customising the response to the user.
        /// </summary>
        /// <param name="sim"></param>
        /// <param name="user"></param>
        /// <param name="response"></param>
        /// <returns>true if the region was successfully contacted, false otherwise</returns>
        private bool PrepareLoginToRegion(RegionInfo regionInfo, UserProfileData user, LoginResponse response)
        {
            response.SimAddress = regionInfo.ExternalEndPoint.Address.ToString();
            response.SimPort = (uint)regionInfo.ExternalEndPoint.Port;
            response.RegionX = regionInfo.RegionLocX;
            response.RegionY = regionInfo.RegionLocY;

            string capsPath = Util.GetRandomCapsPath();
            response.SeedCapability = regionInfo.ServerURI + "/CAPS/" + capsPath + "0000/";

            // Notify the target of an incoming user
            m_log.InfoFormat(
                "[LOGIN]: Telling {0} @ {1},{2} ({3}) to prepare for client connection",
                regionInfo.RegionName, response.RegionX, response.RegionY, regionInfo.ServerURI);
            // Update agent with target sim
            user.CurrentAgent.Region = regionInfo.RegionID;
            user.CurrentAgent.Handle = regionInfo.RegionHandle;
            // Prepare notification
            Login loginParams = new Login();
            loginParams.Session = user.CurrentAgent.SessionID.ToString();
            loginParams.SecureSession = user.CurrentAgent.SecureSessionID.ToString();
            loginParams.First = user.FirstName;
            loginParams.Last = user.SurName;
            loginParams.Agent = user.ID.ToString();
            loginParams.CircuitCode = Convert.ToUInt32(response.CircuitCode);
            loginParams.StartPos = user.CurrentAgent.Position;
            loginParams.CapsPath = capsPath;

            handlerLoginToRegion = OnLoginToRegion;
            if (handlerLoginToRegion == null)
                return false;

            handlerLoginToRegion(user.CurrentAgent.Handle, loginParams);
            return true;
        }

        // See LoginService
        protected override InventoryData GetInventorySkeleton(UUID userID)
        {
            List<InventoryFolderBase> folders = m_Parent.InterServiceInventoryService.GetInventorySkeleton(userID);

            // If we have user auth but no inventory folders for some reason, create a new set of folders.
            if (null == folders || 0 == folders.Count)
            {
                m_Parent.InterServiceInventoryService.CreateNewUserInventory(userID);
                folders = m_Parent.InterServiceInventoryService.GetInventorySkeleton(userID);
            }

            UUID rootID = UUID.Zero;
            ArrayList AgentInventoryArray = new ArrayList();
            Hashtable TempHash;
            foreach (InventoryFolderBase InvFolder in folders)
            {
                if (InvFolder.ParentID == UUID.Zero)
                {
                    rootID = InvFolder.ID;
                }
                TempHash = new Hashtable();
                TempHash["name"] = InvFolder.Name;
                TempHash["parent_id"] = InvFolder.ParentID.ToString();
                TempHash["version"] = (Int32) InvFolder.Version;
                TempHash["type_default"] = (Int32) InvFolder.Type;
                TempHash["folder_id"] = InvFolder.ID.ToString();
                AgentInventoryArray.Add(TempHash);
            }

            return new InventoryData(AgentInventoryArray, rootID);
        }
    }
}
