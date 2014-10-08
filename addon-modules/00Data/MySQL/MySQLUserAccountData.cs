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
using System.Data;
using System.Reflection;

using OpenMetaverse;
using OpenSim.Data;
using OpenSim.Data.MySQL;
using MySql.Data.MySqlClient;

namespace Diva.Data.MySQL
{
    public class MySQLUserAccountData : OpenSim.Data.MySQL.MySqlUserAccountData, IUserAccountData
    {
        private MySQLGenericTableHandler<UserAccountData> m_DatabaseHandler;

        protected override Assembly Assembly
        {
            get { return GetType().BaseType.Assembly; }
        }

        public MySQLUserAccountData(string connectionString, string realm)
                : base(connectionString, realm)
        {
            m_DatabaseHandler = new MySQLGenericTableHandler<UserAccountData>(connectionString, realm, "UserAccount");
        }

        public UserAccountData[] GetActiveAccounts(UUID scopeID, string query, string excludeTerm)
        {
            string[] words = query.Split(new char[] { ' ' });

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length < 3)
                {
                    if (i != words.Length - 1)
                        Array.Copy(words, i + 1, words, i, words.Length - i - 1);
                    Array.Resize(ref words, words.Length - 1);
                }
            }

            if (words.Length == 0)
                return new UserAccountData[0];

            if (words.Length > 2)
                return new UserAccountData[0];

            MySqlCommand cmd = new MySqlCommand();

            if (words.Length == 1)
            {
                cmd.CommandText = String.Format("select * from {0} where (ScopeID=?ScopeID or ScopeID='00000000-0000-0000-0000-000000000000') and (FirstName like ?search or LastName like ?search)", m_Realm);
                cmd.Parameters.AddWithValue("?search", "%" + words[0] + "%");
                cmd.Parameters.AddWithValue("?ScopeID", scopeID.ToString());
            }
            else
            {
                cmd.CommandText = String.Format("select * from {0} where (ScopeID=?ScopeID or ScopeID='00000000-0000-0000-0000-000000000000') and (FirstName like ?searchFirst or LastName like ?searchLast)", m_Realm);
                cmd.Parameters.AddWithValue("?searchFirst", "%" + words[0] + "%");
                cmd.Parameters.AddWithValue("?searchLast", "%" + words[1] + "%");
                cmd.Parameters.AddWithValue("?ScopeID", scopeID.ToString());
            }
            cmd.CommandText = cmd.CommandText + " and (FirstName not like ?exclude)";
            cmd.Parameters.AddWithValue("?exclude", excludeTerm + "%");

            return m_DatabaseHandler.DoQuery(cmd);
        }

        public long GetActiveAccountsCount(UUID scopeID, string excludeTerm)
        {
            string where = string.Format("(ScopeID='{0}' or ScopeID='00000000-0000-0000-0000-000000000000') and (FirstName not like '{1}%')", scopeID, excludeTerm);

            return m_DatabaseHandler.GetCount(where);
        }
    }
}
