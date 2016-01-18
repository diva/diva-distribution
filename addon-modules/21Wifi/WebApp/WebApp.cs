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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using Nini.Config;
using log4net;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using OpenSim.Services.InventoryService;

using Diva.Interfaces;
using Diva.Utils;
using Diva.Wifi.WifiScript;
using Diva.OpenSimServices;
using Environment = Diva.Utils.Environment;

namespace Diva.Wifi
{
    using StatisticsDict = Dictionary<string, float>;

    public class WebApp : IWifiApp
    {
        private static string AssemblyDirectory
        {
            get
            {
                string location = Assembly.GetExecutingAssembly().Location;
                return Path.GetDirectoryName(location);
            }
        }

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static readonly string DocsPath = System.IO.Path.Combine(AssemblyDirectory, "WifiPages");
        private static readonly List<string> SpecialFiles = new List<string>(new [] { "fluid.css", "footer.html", "header.html", "links.html", "splash.html", "termsofservice.html", "welcome.html" });
        public static readonly string MissingPage = Path.Combine(DocsPath, "404.html");

        public static WebApp WebAppInstance;
        public static WifiScriptFace WifiScriptFaceInstance;

        public readonly Services Services;
        public readonly WifiScriptFace WifiScriptFace;

        #region Properties with default settings

        private readonly double m_SessionTimeout = 30*60; // 30 minutes
        public double SessionTimeout
        {
            get { return m_SessionTimeout; }
        }

        private readonly int m_AdminUserLevel = 100;
        public int AdminUserLevel
        {
            get { return m_AdminUserLevel; }
        }

        private bool m_Installed = false;
        public bool IsInstalled
        {
            get { return m_Installed; }
            set { m_Installed = value; }
        }

        private static string UserDocsPath = "..";

        #endregion

        #region Properties used by the WifiScript engine and Services

        private int m_Port;
        public int Port
        {
            get { return m_Port; }
        }

        private string m_GridName;
        public string GridName
        {
            get { return m_GridName; }
        }

        private string m_LoginURL;
        public string LoginURL
        {
            get { return m_LoginURL; }
        }

        private string m_WebAddress;
        public string WebAddress
        {
            get { return m_WebAddress; }
        }

        private string m_AdminFirst;
        public string AdminFirst
        {
            get { return m_AdminFirst; }
        }

        private string m_AdminLast;
        public string AdminLast
        {
            get { return m_AdminLast; }
        }

        private string m_AdminEmail;
        public string AdminEmail
        {
            get { return m_AdminEmail; }
        }

        private string m_ConsoleUser;
        public string ConsoleUser
        {
            get { return m_ConsoleUser; }
        }
        private string m_ConsolePass;
        public string ConsolePass
        {
            get { return m_ConsolePass; }
        }

        private int m_IARUserLevel = 0;
        public int IARUserLevel
        {
            get { return m_IARUserLevel; }
        }

        private StatisticsDict m_Statistics;
        public StatisticsDict Statistics
        {
            get { return m_Statistics; }
        }

        #endregion

        #region Properties used by Services

        private string m_AdminPassword;
        public string AdminPassword
        {
            get { return m_AdminPassword; }
        }

        private CultureInfo m_AdminLanguage;
        public CultureInfo[] AdminLanguage
        {
            get
            {
                if (m_LocalizationCachingPeriod == TimeSpan.Zero)
                    return null;
                else
                    return new CultureInfo[] { m_AdminLanguage };
            }
        }

        private CultureInfo m_FrontendLanguage;
        public CultureInfo FrontendLanguage
        {
            get
            {
                if (m_LocalizationCachingPeriod == TimeSpan.Zero)
                    return null;
                else
                    return  m_FrontendLanguage;
            }
        }

        private bool m_AccountConfirmationRequired;
        public bool AccountConfirmationRequired
        {
            get { return m_AccountConfirmationRequired; }
        }

        private string m_RemoteAdminPassword;
        public string RemoteAdminPassword
        {
            get { return m_RemoteAdminPassword; }
        }

        private TimeSpan m_StatisticsUpdateInterval;
        public TimeSpan StatisticsUpdateInterval
        {
            get { return m_StatisticsUpdateInterval; }
        }

        private int m_StatisticsActiveUsersPeriod;
        public int StatisticsActiveUsersPeriod
        {
            get { return m_StatisticsActiveUsersPeriod; }
        }

        private TimeSpan m_LocalizationCachingPeriod;
        public TimeSpan LocalizationCachingPeriod
        {
            get { return m_LocalizationCachingPeriod; }
        }
        private short m_LogMissingTranslations;
        public short LogMissingTranslations
        {
            get { return m_LogMissingTranslations; }
        }

        private string m_SmtpHost;
        public string SmtpHost
        {
            get { return m_SmtpHost; }
        }

        private int m_SmtpPort;
        public int SmtpPort
        {
            get { return m_SmtpPort; }
        }

        private string m_SmtpUsername;
        public string SmtpUsername
        {
            get { return m_SmtpUsername; }
        }

        private string m_SmtpPassword;
        public string SmtpPassword
        {
            get { return m_SmtpPassword; }
        }

        private bool m_BypassCertificateVerification;
        public bool BypassCertificateVerification
        {
            get { return m_BypassCertificateVerification; }
        }

        private Avatar[] m_DefaultAvatars;
        public Avatar[] DefaultAvatars
        {
            get { return m_DefaultAvatars; }
        }

        private string m_DefaultHome;
        public string DefaultHome
        {
            get { return m_DefaultHome; }
        }

        private int m_HyperlinksUserLevel;
        public int HyperlinksUserLevel
        {
            get { return m_HyperlinksUserLevel; }
        }
        private bool m_HyperlinksShowAll;
        public bool HyperlinksShowAll
        {
            get { return m_HyperlinksShowAll; }
        }
        
        #endregion

        private Type m_ExtensionMethods;
        private ISceneActor m_SceneActor;

        public WebApp(IConfigSource config, string configName, IHttpServer server, ISceneActor sactor)
        {
            m_SceneActor = sactor;

            ReadConfigs(config, configName);

            // Extract embedded resources
            ExtractResources();

            // Create the two parts
            Services = new Services(config, configName, this);
            WifiScriptFace = new WifiScriptFace(this);

            m_ExtensionMethods = typeof(ExtensionMethods);
            m_Statistics = new StatisticsDict();

            WebAppInstance = this;
            WifiScriptFaceInstance = WifiScriptFace;

            if (m_LocalizationCachingPeriod != TimeSpan.Zero && m_FrontendLanguage != null)
                Diva.Wifi.Localization.SetFrontendLanguage(m_FrontendLanguage);

            m_log.DebugFormat("[Wifi]: Starting with extension methods type {0}", m_ExtensionMethods);

        }

        public void ReadConfigs(IConfigSource config, string configName)
        {
            // Read config vars
            IConfig appConfig = config.Configs[configName];
            m_GridName = appConfig.GetString("GridName", "My World");
            m_LoginURL = appConfig.GetString("LoginURL", "http://localhost:9000");
            m_WebAddress = appConfig.GetString("WebAddress", "http://localhost:9000");
            m_WebAddress = m_WebAddress.Trim(new char[] { '/' }); 

            m_SmtpHost = appConfig.GetString("SmtpHost", "smtp.gmail.com");
            m_SmtpPort = Int32.Parse(appConfig.GetString("SmtpPort", "587"));
            m_SmtpUsername = appConfig.GetString("SmtpUsername", "your_email@gmail.com");
            m_SmtpPassword = appConfig.GetString("SmtpPassword", "your_password");
            m_BypassCertificateVerification = appConfig.GetBoolean("BypassCertificateVerification", false);

            m_AdminFirst = appConfig.GetString("AdminFirst", string.Empty);
            m_AdminLast = appConfig.GetString("AdminLast", string.Empty);
            m_AdminPassword = appConfig.GetString("AdminPassword", string.Empty);
            m_AdminEmail = appConfig.GetString("AdminEmail", string.Empty);
            m_AdminLanguage = new CultureInfo(appConfig.GetString("AdminLanguage", "en"));
            string lang = appConfig.GetString("FrontendLanguage", string.Empty);
            if (lang != string.Empty)
                m_FrontendLanguage = new CultureInfo(lang);

            UserDocsPath = appConfig.GetString("UserDocsPath", UserDocsPath);

            m_RemoteAdminPassword = appConfig.GetString("RemoteAdminPassword", string.Empty);

            m_AccountConfirmationRequired = appConfig.GetBoolean("AccountConfirmationRequired", true);

            m_StatisticsUpdateInterval = TimeSpan.FromSeconds(appConfig.GetInt("StatisticsUpdateInterval", 60));
            m_StatisticsActiveUsersPeriod = appConfig.GetInt("StatisticsActiveUsersPeriod", 30);

            m_LocalizationCachingPeriod = TimeSpan.FromHours(Math.Abs(appConfig.GetDouble("LocalizationCachingPeriod", 0.0)));
            m_LogMissingTranslations = (short)appConfig.GetInt("LogMissingTranslations", 1);

            m_IARUserLevel = appConfig.GetInt("IARUserLevel", 0);

            // Read list of default avatars and their account names
            const string avatarParamPrefix = "AvatarAccount_";
            IEnumerable<string> avatars = appConfig.GetKeys().Where(avatar => avatar.StartsWith(avatarParamPrefix));
            int avatarCount = avatars.Count();
            if (avatarCount > 0)
            {
                m_DefaultAvatars = new Avatar[avatarCount];
                foreach (string avatar in avatars)
                {
                    Avatar defaultAvatar = new Avatar();
                    if (avatar.Length > avatarParamPrefix.Length)
                        defaultAvatar.Type = avatar.Substring(avatarParamPrefix.Length);
                    defaultAvatar.Name = appConfig.GetString(avatar);
                    m_DefaultAvatars[m_DefaultAvatars.Length - avatarCount] = defaultAvatar;
                    avatarCount--;
                }
            }
            else
            {
                // Create empty default avatar
                Avatar defaultAvatar = new Avatar();
                defaultAvatar.Type = "Default";
                m_DefaultAvatars = new Avatar[] { defaultAvatar };
                m_log.Warn("[Wifi]: There aren't any default avatars defined in config");
            }

            // Preselection for default avatar list
            Avatar.DefaultType = appConfig.GetString("AvatarPreselection", null);
            if (Avatar.DefaultType == null)
                Avatar.DefaultType = m_DefaultAvatars[0].Type;

            // Default home location for new accounts
            m_DefaultHome = appConfig.GetString("HomeLocation", string.Empty);

            // Hyperlink service
            m_HyperlinksUserLevel = appConfig.GetInt("HyperlinkServiceUserLevel", 50);
            m_HyperlinksShowAll = appConfig.GetBoolean("HyperlinkServiceUsersSeeAll", true);

            if (m_AdminFirst == string.Empty || m_AdminLast == string.Empty)
                // Can't proceed
                throw new Exception("Can't proceed. Please specify the administrator account completely.");

            IConfig serverConfig = config.Configs["Network"];
            if (serverConfig != null)
            {
                m_Port = serverConfig.GetInt("port", 9000);
                m_ConsoleUser = serverConfig.GetString("ConsoleUser", string.Empty);
                m_ConsolePass = serverConfig.GetString("ConsolePass", string.Empty);
            }

            m_log.DebugFormat("[Wifi]: WebApp configs loaded. Admin account is {0} {1}. Localization is {2}.",
                m_AdminFirst, m_AdminLast,
                (m_LocalizationCachingPeriod == TimeSpan.Zero) ? "disabled" : "enabled");
        }

        private void ExtractResources()
        {
            if (!Directory.Exists(UserDocsPath))
            {
                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(UserDocsPath);
                if (di == null)
                    m_log.WarnFormat("[Wifi]: Unable to create folder {0}", UserDocsPath);
                else
                    Directory.CreateDirectory(Path.Combine(UserDocsPath, "images"));
            }

            foreach (string resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (!resourceName.StartsWith("Diva.Wifi"))
                    continue;

                string filePath = resourceName.Substring("Diva.Wifi.".Length);
                string rnameNoExt = Path.GetFileNameWithoutExtension(filePath);
                string ext = Path.GetExtension(filePath);
                filePath = rnameNoExt.Replace('.', Path.DirectorySeparatorChar) + ext;

                string destinationPath = string.Empty;

                if (SpecialFiles.Exists(s => filePath.Contains(s)))
                    destinationPath = Path.Combine(UserDocsPath, filePath);
                else
                    destinationPath = Path.Combine(AssemblyDirectory, filePath);
                if (File.Exists(destinationPath))
                    continue;

                m_log.DebugFormat("[Wifi]: Extracting {0}", filePath);

                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    string dirName = Path.GetDirectoryName(destinationPath);
                    if (!Directory.Exists(dirName))
                    {
                        // Try to create the directory.
                        DirectoryInfo di = Directory.CreateDirectory(dirName);
                        if (di == null)
                        {
                            m_log.WarnFormat("[Wifi]: Unable to create folder {0}", dirName);
                            continue;
                        }
                    }

                    using (var file = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                    {
                        resource.CopyTo(file);
                    }
                }

            }
        }

        #region read html files

        public static string GetPath(string resource)
        {
            string resourcePath = string.Empty;
            if (SpecialFiles.Exists(s => resource.Contains(s)))
                resourcePath = System.IO.Path.Combine(Path.Combine(WebApp.UserDocsPath, "WifiPages"), resource);
            else
                resourcePath = System.IO.Path.Combine(WebApp.DocsPath, resource);

            return resourcePath;
        }

        public static string[] GetPaths(string resource)
        {
            // UserDocsPath (external) comes first
            return new string[] {System.IO.Path.Combine(Path.Combine(WebApp.UserDocsPath, "WifiPages"), resource), System.IO.Path.Combine(WebApp.DocsPath, resource)};
        }

        public string ReadFile(IEnvironment env, string path)
        {
            return ReadFile((Environment)env, path, ((Environment)env).Data);
        }

        public string ReadFile(IEnvironment env, string path, List<object> lot)
        {
            string file = Localization.LocalizePath(env, path);
            try
            {
                string content = string.Empty;
                using (StreamReader sr = new StreamReader(file))
                {
                    content = sr.ReadToEnd();
                }
                Processor p = new Processor(WifiScriptFace, m_ExtensionMethods, env, lot);
                return p.Process(content);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[Wifi]: Exception on ReadFile {0}: {1}", path, e);
                return WebAppUtils.ReadTextResource(new string[] {WebApp.MissingPage}, "");
            }
        }
        #endregion

        #region IWifiApp

        private List<WifiAddon> m_Addons = new List<WifiAddon>();
        public List<WifiAddon> Addons
        {
            get { return m_Addons; }
        }

        public void Register(IWifiAddon addon, string menuAnchor, string path, PrivilegeLevel level = PrivilegeLevel.Admins)
        {
            WifiAddon a = new WifiAddon();
            a.Addon = addon;
            a.MenuAnchor = menuAnchor;
            a.Path = path;
            a.Privilege = level;
            m_Addons.Add(a);
        }

        public void RegisterEstateServiceObject(Object estateService)
        {
            Services.RegisterEstateServiceObject((IEstateDataService)estateService);
        }

        public T GetServiceObject<T>()
        {
            if (typeof(T) == typeof(ISceneActor))
            {
                return (T)m_SceneActor;
            }

            return Services.GetServiceObject<T>();
        }

        public bool TryGetSessionInfo(IRequest req, out ISessionInfo sinfo)
        {
            SessionInfo si;
            bool success = Services.TryGetSessionInfo((Request)req, out si);
            sinfo = si;
            return success;
        }

        public bool IsValidSessionForUser(string uid, string sid)
        {
            return Services.IsValidSessionForUser(uid, sid);
        }

        public void NotifyWithoutButton(IEnvironment env, string message)
        {
            Services.NotifyWithoutButton(env, message);
        }

        public void SetAvatar(IEnvironment env, UUID userID, string avatarType)
        {
            Services.SetAvatar(env, userID, avatarType);
        }

        public bool SendEMail(string to, string subject, string message)
        {
            return Services.SendEMail(to, subject, message);
        }

        public bool SendEMailSync(string to, string cc, string bcc, string subject, string message)
        {
            return Services.SendEMailSync(to, cc, bcc, subject, message);
        }
        #endregion IWifiApp
    }

    public class Avatar
    {
        public static string DefaultType;
        public static UUID HomeRegion = UUID.Zero;
        public static Vector3 HomeLocation;

        public string Type = string.Empty;
        public string Name;
        [Translate]
        public string PrettyType
        {
            get { return Type.Replace('_', ' '); }
        }
        public bool isDefault
        {
            get { return Type.Equals(DefaultType); }
        }
    }

    public class RegionInfo
    {
        private UUID m_OwnerID;
        private string m_Owner;

        public UUID RegionID
        {
            get { return Region.RegionID; }
        }
        public string RegionName
        {
            get { return Region.RegionName; }
        }
        public string RegionAddress
        {
            get { return Region.RegionName; }
        }
        public uint RegionLocX
        {
            get { return (uint)Region.RegionLocX / Constants.RegionSize; }
        }
        public uint RegionLocY
        {
            get { return (uint)Region.RegionLocY / Constants.RegionSize; }
        }
        public UUID RegionOwnerID
        {
            get { return m_OwnerID; }
            set { m_OwnerID = value; }
        }
        [Translate]
        public string RegionOwner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }
        public GridRegion Region;

        public RegionInfo(GridRegion region)
        {
            Region = region;
            m_OwnerID = region.EstateOwner;
            m_Owner = "nobody";
        }
    }
}
