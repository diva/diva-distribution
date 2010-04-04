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
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using log4net;

namespace Diva.Wifi.WifiScript
{
    public class Processor
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // <!-- #directive [args] -->
        private static Regex ssi = new Regex("<!--\\s*\\#(\\S+)\\s+(\\S*)\\s*-->");
        // name="value"
        private static Regex args = new Regex("(\\w+)\\s*=\\s*(\\S+)");

        private IWifiScriptFace m_WebApp;
        private Type m_WebAppType;
 
        private IEnvironment m_Env;
        private List<object> m_ListOfObjects;
        private int m_Index;

        public Processor(IWifiScriptFace webApp, IEnvironment env) : this(webApp, env, null)
        {
        }

        public Processor(IWifiScriptFace webApp, IEnvironment env, List<object> lot)
        {
            m_WebApp = webApp;
            m_WebAppType = m_WebApp.GetType();
            m_Env = env;
            m_ListOfObjects = lot;
            m_Index = 0;
        }

        public string Process(string html)
        {
            string processedHtml = string.Empty;
            MatchCollection matches = ssi.Matches(html);
            //m_log.DebugFormat("Regex: {0}; matches = {1}", ssi.ToString(), matches.Count);

            int lastindex = 0;
            foreach (Match match in matches)
            {
                //m_log.DebugFormat("Match {0}", match.Value);
                string replacement = Process(match);
                string before = html.Substring(lastindex, match.Index - lastindex);
                string after = html.Substring(match.Index + match.Length);

                processedHtml = processedHtml + before + replacement;
                lastindex = match.Index + match.Length ;
            }
            if (matches.Count > 0)
            {
                string end = html.Substring(matches[matches.Count - 1].Index + matches[matches.Count - 1].Length);
                processedHtml = processedHtml + end;
                return processedHtml;
            }

            return html;
        }

        private string Process(Match match)
        {
            string directive = string.Empty;
            string argStr = string.Empty;
            //m_log.DebugFormat("Groups: {0}", match.Groups.Count);
            //foreach (Group g in match.Groups)
            //{
            //    m_log.DebugFormat(" --> {0} {1}", g.Value, g.Success);
            //}
            // The first group is always the overall match
            if (match.Groups.Count > 1)
                directive = match.Groups[1].Value;
            if (match.Groups.Count > 2)
                argStr = match.Groups[2].Value;

            if (directive != string.Empty)
            {
                return Eval(directive, argStr);
            }

            return string.Empty;
        }

        private string Eval(string directive, string argStr)
        {
            m_log.DebugFormat("[WifiScript]: Interpret {0} {1}", directive, argStr);

            if (directive.Equals("include"))
                return Include(argStr);

            if (directive.Equals("get"))
                return Get(argStr);

            if (directive.Equals("call"))
                return Call(argStr);

            return string.Empty;
        }

        private string Include(string argStr)
        {
            // Break the recursive includes
            if (m_ListOfObjects != null &&  m_Index == m_ListOfObjects.Count)
                return string.Empty;

            m_Index++;

            Match match = args.Match(argStr);
            //m_log.DebugFormat("Match {0} args? {1} {2}", args.ToString(), match.Success, match.Groups.Count);
            if (match.Groups.Count == 3)
            {
                string name = match.Groups[1].Value;
                string value = match.Groups[2].Value;
                // ignore the name which should be file
                string file = Path.Combine(m_WebApp.DocsPath, value);
                m_log.DebugFormat("[WifiScript]: Including file {0}", file);
                using (StreamReader sr = new StreamReader(file))
                {
                    // recurse!
                    return Process(sr.ReadToEnd());
                }
            }

            return string.Empty;
        }

        private string Get(string argStr)
        {
            Match match = args.Match(argStr);
            m_log.DebugFormat("[WifiScript]: Get macthed {0} groups", match.Groups.Count);
            if (match.Groups.Count == 3)
            {
                string kind = match.Groups[1].Value;
                string name = match.Groups[2].Value;
                string keyname = string.Empty;

                object value = null;

                if (kind == "var")
                {
                    PropertyInfo pinfo = m_WebAppType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.DeclaredOnly);
                    if (pinfo != null)
                        value = pinfo.GetValue(m_WebApp, null);
                    else
                        m_log.DebugFormat("[WifiScript]: Variable {0} not found", name);
                }
                else if (kind == "field")
                {
                    if (m_ListOfObjects != null)
                    {
                        // Let's search in the list of objects
                        object o = m_ListOfObjects[m_Index - 1];
                        Type type = o.GetType();
                        FieldInfo finfo = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                        if (finfo != null)
                            value = finfo.GetValue(o);
                        else
                            m_log.DebugFormat("[WifiScript]: Field {0} not found in type {1}", name, type);
                    }
                    else
                        m_log.DebugFormat("[WifiScript]: Field reference {0} to null list of objects", name);
                }
                
                if (value != null)
                    return value.ToString();
                else
                    return string.Empty;

            }

            return string.Empty;
        }

        private string Call(string argStr)
        {
            MatchCollection matches = args.Matches(argStr);
            List<String> arguments = new List<string>();
            String methodName = string.Empty;
            foreach (Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    string name = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    if (name.Equals("method"))
                        methodName = value;
                    else
                        arguments.Add(value);
                }
            }
            if (!methodName.Equals(string.Empty))
            {
                object[] arg = new object[] { m_Env };
                try
                {
                    String s = (String)m_WebAppType.InvokeMember(methodName,
                        BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
                        null, m_WebApp, arg);

                    return s;
                }
                catch (Exception e)
                {
                    m_log.DebugFormat("[WifiScript]: Exception in invoke {0}", e.Message);
                }
            }

            return string.Empty;
        }
    }
}
