using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Server.Base;

using Mono.Addins;
using log4net;
using Nini.Config;

// External library that doesn't exist in the OpenSim distribution
using CsvHelper;

namespace Diva.AddinExample
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "AddinsExampleModule")]
    public class AddinExampleModule : ISharedRegionModule
    {
        #region Class and Instance Members

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private bool m_Enabled;

        private static string AssemblyDirectory
        {
            get
            {
                string location = Assembly.GetExecutingAssembly().Location;
                return Path.GetDirectoryName(location);
            }
        }

        #endregion

        #region ISharedRegionModule

        public void Initialise(IConfigSource config)
        {
            // We only load the configuration file if the main config doesn't know about this module already
            IConfig cnf = config.Configs["AddinExample"];
            if (cnf == null)
            {
                LoadConfiguration(config);
                cnf = config.Configs["AddinExample"];
                if (cnf == null)
                {
                    m_log.WarnFormat("[Diva.AddinExample]: Configuration section AddinExample not found. Unable to proceed.");
                    return;
                }
            }

            m_Enabled = cnf.GetBoolean("enabled", m_Enabled);

            if (m_Enabled)
            {
                string pathToCsvFile = Path.Combine(AssemblyDirectory, "Names.csv");
                string pathToHtmlFile = Path.Combine(AssemblyDirectory, "AddinExample.html");
                m_log.InfoFormat("[Diva.AddinExample]: AddinExample is on. File is {0}", pathToCsvFile);

                if (!File.Exists(pathToCsvFile))
                {
                    using (TextWriter tr = new StreamWriter(pathToCsvFile))
                    using (var writer = new CsvWriter(tr))
                    {
                        writer.WriteField("First Name");
                        writer.WriteField("Last Name");
                        writer.NextRecord();
                    }
                }

                MainServer.Instance.AddStreamHandler(new FormUploadGetHandler(pathToHtmlFile));
                MainServer.Instance.AddStreamHandler(new FormUploadPostHandler(pathToCsvFile));
            }
        }

        public string Name
        {
            get { return "Diva Addin Example"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void PostInitialise() { }

        public void AddRegion(Scene scene) { }

        public void RegionLoaded(Scene scene) { }

        public void RemoveRegion(Scene scene) { }

        public void Close() { }

        #endregion

        private void LoadConfiguration(IConfigSource config)
        {
            string configPath = string.Empty;
            bool created;
            if (!Util.MergeConfigurationFile(config, "AddinExample.ini", Path.Combine(AssemblyDirectory, "AddinExample.ini"), out configPath, out created))
            {
                m_log.WarnFormat("[Diva.AddinExample]: Configuration file not merged");
                return;
            }

            if (created)
            {
                m_log.ErrorFormat("[Diva.AddinExample]: PLEASE EDIT {0} BEFORE RUNNING THIS ADDIN", configPath);
                throw new Exception("Addin must be configured prior to running");
            }

        }

    }

    public class FormUploadGetHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string m_pathToFile;

        public FormUploadGetHandler(string file) :
            base("GET", "/diva/addinexample")
        {
            m_pathToFile = file;
        }

        protected override byte[] ProcessRequest(string path, Stream requestData,
                IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
        {
            string result = string.Empty;
            string resource = GetParam(path);

            string html = string.Empty;
            using (StreamReader sr = new StreamReader(m_pathToFile))
                html = sr.ReadToEnd();

            httpResponse.ContentType = "text/html";
            return Encoding.UTF8.GetBytes(html);
        }
    }

    public class FormUploadPostHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string m_PathToFile;

        public FormUploadPostHandler(string path) :
            base("POST", "/diva/addinexample")
        {
            m_PathToFile = path;
        }

        protected override byte[] ProcessRequest(string path, Stream requestData,
                IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
        {
            // It's a POST, so we need to read the data on the stream, the lines after the blank line
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            // Here the data on the stream is transformed into a nice dictionary of keys & values
            Dictionary<string, object> postdata = ServerUtils.ParseQueryString(body);

            string firstname = string.Empty, lastname = string.Empty;
            if (postdata.ContainsKey("firstname") && !string.IsNullOrEmpty(postdata["firstname"].ToString()))
                firstname = postdata["firstname"].ToString();
            if (postdata.ContainsKey("lastname") && !string.IsNullOrEmpty(postdata["lastname"].ToString()))
                lastname = postdata["lastname"].ToString();

            AddToCSVFile(firstname, lastname);

            string result = "Thanks, your name has been recorded.";
            httpResponse.ContentType = "text/html";
            httpResponse.StatusCode = 200;

            return Encoding.UTF8.GetBytes(result);

        }

        private void AddToCSVFile(string first, string last)
        {
            using (TextWriter tr = new StreamWriter(m_PathToFile, true))
            using (var writer = new CsvWriter(tr))
            {
                writer.WriteField(first);
                writer.WriteField(last);
                writer.NextRecord();
            }
        }
    }

}
