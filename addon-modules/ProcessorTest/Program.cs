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

using log4net;
using log4net.Appender;
using log4net.Layout;

using Diva.Wifi.WifiScript;

namespace Diva.Wifi.ProcessorTest
{
    public class Program : IWifiScriptFace
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string DocsPath
        {
            // TODO Fix this
            get { return string.Empty; }
        }

        public static void Main(string[] args)
        {
            ConsoleAppender consoleAppender = new ConsoleAppender();
            consoleAppender.Layout =
                new PatternLayout("%date [%thread] %-5level %logger [%property{NDC}] - %message%newline");
            log4net.Config.BasicConfigurator.Configure(consoleAppender);

            if (args.Length == 0)
            {
                m_log.Debug("Please specify filename");
                return;
            }

            Program webApp = new Program();

            //string myStr = "<!--#include file=\"content-box.html\" -->";
            //string result = Processor.Processor.Process(myStr);
            //m_log.Debug(result);
            string fileName = args[0];
            using (StreamReader sr = new StreamReader(fileName))
            {
                string content = sr.ReadToEnd();
                Processor p = new Processor(webApp, null);
                string result = p.Process(content);

                m_log.Debug(result);
            }
        }
    }
}
