/*
 * Copyright (c) Marcus Kirsch (aka Marck). All rights reserved.
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

using System.Collections.Generic;
using OpenSim.Data;
using OpenMetaverse;

namespace Diva.Data
{
    public class GroupData
    {
        public UUID GroupID;
        public Dictionary<string, string> Data;
    }

    public class MembershipData
    {
        public UUID GroupID;
        public UUID PrincipalID;
        public Dictionary<string, string> Data;
    }

    public class RoleData
    {
        public UUID GroupID;
        public UUID RoleID;
        public Dictionary<string, string> Data;
    }

    public class RoleMembershipData
    {
        public UUID GroupID;
        public UUID RoleID;
        public string PrincipalID;
    }

    public class PrincipalData
    {
        public string PrincipalID;
        public UUID ActiveGroupID;
    }

    public interface IGroupsData 
    {
        // groups table
        bool StoreGroup(GroupData data);
        GroupData RetrieveGroup(UUID groupID);
        GroupData[] RetrieveGroups(string pattern);
        bool DeleteGroup(UUID groupID);

        // membership table
        MembershipData[] RetrieveMembers(UUID groupID);
        bool StoreMember(MembershipData data);
        bool DeleteMember(UUID groupID, string pricipalID);

        // roles table
        bool StoreRole(RoleData data);
        RoleData[] RetrieveRoles(UUID groupID);
        bool DeleteRole(UUID groupID, UUID roleID);

        // rolememberhip table
        RoleMembershipData[] RetrieveRoleMembers(UUID groupID);
        bool StoreRoleMember(RoleMembershipData data);
        bool DeleteRoleMember(RoleMembershipData data);

        // principals table
        bool StorePrincipal(PrincipalData data);
        PrincipalData RetrievePrincipal(string principalID);
        bool DeletePrincipal(string principalID);

        // invites table

        // notices table

        // combinations
        MembershipData RetrievePrincipalGroupMembership(string principalID, UUID groupID);
        MembershipData[] RetrievePrincipalGroupMemberships(string principalID);
    }
}
