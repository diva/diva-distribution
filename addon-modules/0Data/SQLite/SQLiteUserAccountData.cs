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

using System;
using System.Collections;
using System.Collections.Generic;

using Mono.Data.Sqlite;

using OpenMetaverse;
using OpenSim.Data;

namespace Diva.Data.SQLite
{
    public class SQLiteUserAccountData : OpenSim.Data.SQLite.SQLiteUserAccountData, IUserAccountData
    {
        private SQLiteGenericTableHandler<UserAccountData> m_DatabaseHandler;

        public SQLiteUserAccountData(string connectionString, string realm) 
            : base(connectionString, realm)
        {
            m_DatabaseHandler = new SQLiteGenericTableHandler<UserAccountData>(connectionString, realm, "UserAccount");
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

            SqliteCommand cmd = new SqliteCommand();

            if (words.Length == 1)
            {
                cmd.CommandText = String.Format("select * from {0} where (ScopeID='{1}' or ScopeID='00000000-0000-0000-0000-000000000000') and (FirstName like '{2}%' or LastName like '{2}%') and (FirstName not like '{3}%')",
                    m_Realm, scopeID.ToString(), words[0], excludeTerm);
            }
            else
            {
                cmd.CommandText = String.Format("select * from {0} where (ScopeID='{1}' or ScopeID='00000000-0000-0000-0000-000000000000') and (FirstName like '{2}%' or LastName like '{3}%') and (FirstName not like '{4}%')",
                    m_Realm, scopeID.ToString(), words[0], words[1], excludeTerm);
            }

            return m_DatabaseHandler.DoQuery(cmd);
        }

        public long GetActiveAccountsCount(UUID scopeID, string excludeTerm)
        {
            string where = string.Format("(ScopeID='{0}' or ScopeID='00000000-0000-0000-0000-000000000000') and (FirstName not like '{1}%')", scopeID, excludeTerm);

            return m_DatabaseHandler.GetCount(where);
        }
    }
}