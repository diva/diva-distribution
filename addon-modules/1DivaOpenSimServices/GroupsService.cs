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
using log4net;
using Nini.Config;

using OpenMetaverse;
using OpenSim.Data;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using OpenSim.Groups;

using Diva.Interfaces;

namespace Diva.OpenSimServices
{
    public class GroupsService : OpenSim.Groups.GroupsService, IGroupsService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public GroupsService(IConfigSource config)
            : base(config)
        {
        }

        public GroupRecord GetGroupRecord(UUID GroupID)
        {
            return GetGroupRecord(string.Empty, GroupID);
        }

        public void UpdateGroup(UUID groupID, string name, string charter)
        {
            GroupData data = m_Database.RetrieveGroup(groupID);
            if (data == null)
                return;

            // No perms check!
            // This is called by Wifi Admin only

            data.Data["Name"] = name;
            data.Data["Charter"] = charter;

            m_Database.StoreGroup(data);

        }

        public void DeleteGroup(UUID groupID)
        {
            List<ExtendedGroupRoleMembersData> members = _GetGroupRoleMembers(groupID, true);
            foreach (ExtendedGroupRoleMembersData member in members)
                _RemoveAgentFromGroup(string.Empty, member.MemberID, groupID);

            List<GroupRolesData> roles = _GetGroupRoles(groupID);
            if (roles != null && roles.Count > 0)
                foreach (GroupRolesData r in roles)
                    m_Database.DeleteRole(groupID, r.RoleID);

            m_Database.DeleteGroup(groupID);
        }

        public void RemoveAgentFromGroup(string userID, UUID groupID)
        {
            _RemoveAgentFromGroup(string.Empty, userID, groupID);
        }
    }

}
