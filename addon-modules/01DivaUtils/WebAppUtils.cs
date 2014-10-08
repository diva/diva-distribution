﻿/**
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
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using log4net;

namespace Diva.Utils
{
    public class WebAppUtils
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static readonly char[] DirectorySeparatorChars = { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar };

        private static string m_UploadPath = "DivaUploads";
        public static string UploadPath
        {
            get { return m_UploadPath; }
        }

        public static string GetContentType(string resource)
        {
            resource = resource.ToLower();
            if (resource.EndsWith(".jpg"))
                return "image/jpeg";
            if (resource.EndsWith(".gif"))
                return "image/gif";
            if (resource.EndsWith(".png"))
                return "image/png";
            if (resource.EndsWith(".ico"))
                return "image/x-icon";
            if (resource.EndsWith(".css"))
                return "text/css";
            if (resource.EndsWith(".txt"))
                return "text/plain";
            if (resource.EndsWith(".xml"))
                return "text/xml";
            if (resource.EndsWith(".js"))
                return "application/javascript";
            return "text/html";
        }

        public static byte[] ReadBinaryResource(string resourceName)
        {
            try
            {
                using (BinaryReader sr = new BinaryReader(File.Open(resourceName, FileMode.Open)))
                {
                    byte[] buffer = new byte[32768];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        while (true)
                        {
                            int read = sr.Read(buffer, 0, buffer.Length);
                            if (read <= 0)
                                return ms.ToArray();
                            ms.Write(buffer, 0, read);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                m_log.DebugFormat("[Wifi]: Exception in ReadBinaryResource {0}", e.Message);
                return ReadBinaryResource(Path.Combine("..", Path.Combine("WifiPages", "404.html")));
            }

        }

        public static string ReadTextResource(string resourceName)
        {
            return ReadTextResource(resourceName, false);
        }

        public static string ReadTextResource(string resourceName, bool keepEndOfLines)
        {
            StringBuilder buffer = new StringBuilder();
            try
            {
                // Create an instance of StreamReader to read from a file.
                // The using statement also closes the StreamReader.
                using (StreamReader sr = new StreamReader(resourceName))
                {
                    if (keepEndOfLines)
                    {
                        buffer.Append(sr.ReadToEnd());
                    }
                    else
                    {
                        String line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            buffer.Append(line);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                m_log.DebugFormat("[Wifi]: Exception in ReadTextResource {0}", e.Message);
                return ReadTextResource(Path.Combine("..", Path.Combine("WifiPages", "404.html")));
            }

            return buffer.ToString();
        }
        
        public static byte[] StringToBytes(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public static byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        public static byte[] FailureResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "result", "");
            result.AppendChild(doc.CreateTextNode("Failure"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        public static byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        public static bool IsValidName(string name)
        {
            Regex re = new Regex("[^a-zA-Z0-9_]+");
            return !re.IsMatch(name);
        }

        public static bool IsValidEmail(string email)
        {
            string strRegex = @"^(([^<>()[\]\\.,;:\s@\""]+"
                + @"(\.[^<>()[\]\\.,;:\s@\""]+)*)|(\"".+\""))@"
                + @"((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}"
                + @"\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+"
                + @"[a-zA-Z]{2,}))$";
            Regex re = new Regex(strRegex);
                return re.IsMatch(email);
        }

        public static bool IsValidRegionAddress(string address)
        {
            string regionName = string.Empty;
            // Check format: <ServerURI> [<RemoteRegionName>]
            string[] parts = address.Split(new char[] { ' ' }, 2);
            if (parts.Length > 1)
                regionName = parts[1];
            // Check server URI
            Uri test;
            if (!Uri.TryCreate(parts[0], UriKind.Absolute, out test))
            {
                // Check format: <HostName>:<Port>[:<RemoteRegionName>]
                parts = parts[0].Split(':');
                if (parts.Length < 2 || parts.Length > 3)
                    return false;
                if (!Uri.TryCreate(parts[0] + ":" + parts[1], UriKind.Absolute, out test))
                    return false;
                if (parts.Length == 3)
                    regionName = parts[2];
            }
            // Check region name
            if (regionName != string.Empty)
            {
                // Quick & paranoid sanity check, feel free to suggest
                // a better one that prevents XSS and code/SQL injection
                Regex re = new Regex(@"[a-zA-Z0-9_=/\-\+\&\*\(\) ]+");
                return re.IsMatch(regionName);
            }
            return true;
        }

        // <a href="wifi/..." ...>
        static Regex href = new Regex("(<a\\s+.*href\\s*=\\s*\\\"(\\S+\\\")).*>");
        static Regex action = new Regex("(<form\\s+.*action\\s*=\\s*\\\"(\\S+\\\")).*>");
        static Regex xmlhttprequest = new Regex("(@@wifi@@(\\S+\\\"))");

        public static string PadURLs(string sid, string html)
        {
            HashSet<string> uris = new HashSet<string>();
            CollectMatches(uris, href.Matches(html));
            CollectMatches(uris, action.Matches(html));
            CollectMatches(uris, xmlhttprequest.Matches(html));

            foreach (string uri in uris)
            {
                string uri2 = uri.Substring(0, uri.Length - 1);
                //m_log.DebugFormat("[Wifi]: replacing {0} with {1}", uri, uri2 + "?sid=" + sid + "\"");
                if (!uri.EndsWith("/"))
                    html = html.Replace(uri, uri2 + "/?sid=" + sid + "\"");
                else
                    html = html.Replace(uri, uri2 + "?sid=" + sid + "\"");
            }
            // Remove any @@wifi@@
            html = html.Replace("@@wifi@@", string.Empty);

            return html;
        }

        public static string PadURLs(Environment env, string sid, string html)
        {
            if ((env.Flags & Flags.IsLoggedIn) == 0 && (env.Flags & Flags.IsValidSession) == 0)
                return html;

            return WebAppUtils.PadURLs(sid, html);
        }

        public static string PadURLs(Hashtable queryPairs, string html)
        {
            HashSet<string> uris = new HashSet<string>();
            CollectMatches(uris, href.Matches(html));
            CollectMatches(uris, action.Matches(html));
            CollectMatches(uris, xmlhttprequest.Matches(html));

            string query = string.Empty;
            foreach (DictionaryEntry kvp in queryPairs)
            {
                if (kvp.Key != null && kvp.Value != null)
                    query += kvp.Key.ToString() + "=" + kvp.Value.ToString() + "&";
            }

            foreach (string uri in uris)
            {
                string uri2 = uri.Substring(0, uri.Length - 1);
                //m_log.DebugFormat("[Wifi]: replacing {0} with {1}", uri, uri2 + "?sid=" + sid + "\"");
                if (!uri.EndsWith("/"))
                    html = html.Replace(uri, uri2 + "/?" + query + "\"");
                else
                    html = html.Replace(uri, uri2 + "?" + query + "\"");
            }
            // Remove any @@wifi@@
            html = html.Replace("@@wifi@@", string.Empty);

            return html;
        }

        private static void CollectMatches(HashSet<string> uris, MatchCollection matches)
        {
            foreach (Match match in matches)
            {
                // first group is always the total match
                if (match.Groups.Count > 2)
                {
                    string str = match.Groups[1].Value;
                    string uri = match.Groups[2].Value;
                    if (!uri.StartsWith("http") &&
                        !uri.StartsWith("mailto") &&
                        !uri.EndsWith(".html") &&
                        !uri.EndsWith(".css") &&
                        !uri.EndsWith(".js")
                       )
                        uris.Add(str);
                }
            }
        }

        public static List<object> Objectify<T>(IEnumerable<T> listOfThings)
        {
            List<object> listOfObjects = new List<object>();
            foreach (T thing in listOfThings)
                listOfObjects.Add(thing);

            return listOfObjects;
        }



    }
}
