using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using log4net;
using log4net.Appender;
using log4net.Layout;

using Diva.Wifi;

namespace Diva.Wifi.ProcessorTest
{
    public class Program
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
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

            //string myStr = "<!--#include file=\"content-box.html\" -->";
            //string result = Processor.Processor.Process(myStr);
            //m_log.Debug(result);
            string fileName = args[0];
            using (StreamReader sr = new StreamReader(fileName))
            {
                string content = sr.ReadToEnd();
                Processor p = new Diva.Wifi.Processor(null);
                string result = p.Process(content);

                m_log.Debug(result);
            }
        }
    }
}
