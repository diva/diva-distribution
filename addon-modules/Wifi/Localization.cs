/**
 * Copyright (c) Marcus Kirsch (aka Marck). All rights reserved.
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using log4net;
using OpenMetaverse;

// Set English as the neutral resources language for assembly.
// (This does not work with Mono, so we implement a work-around in Localization.GetLanguageInfo().)
//[assembly: NeutralResourcesLanguageAttribute("en", UltimateResourceFallbackLocation.Satellite)]

namespace Diva.Wifi
{
    /// <summary>
    /// Tools for localization.
    /// </summary>
    public static class Localization
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly CultureInfo m_FallbackLanguage = new CultureInfo("en");

        private static readonly ResourceManager m_Resources =
            new ResourceManager("Diva.Wifi", Assembly.GetExecutingAssembly());

        private static LocalizationPathCache m_PathCache =
            new LocalizationPathCache(WebApp.WebAppInstance.LocalizationCachingPeriod);


        /// <summary>
        /// Parses the parameters of HTTP header Accept-Language.
        /// </summary>
        /// <param name="acceptLanguage">The value of HTTP header Accept-Language</param>
        /// <returns>
        /// An array of language codes.
        /// Null, if header is empty or if localization is disabled.
        /// </returns>
        public static CultureInfo[] GetLanguageInfo(string acceptLanguage)
        {
            if (string.IsNullOrEmpty(acceptLanguage) ||
                TimeSpan.Zero == WebApp.WebAppInstance.LocalizationCachingPeriod)
            {
                // Don't do any localization
                return null;
            }

            // A "*" found in the header will stop the search for available languages
            int index = acceptLanguage.IndexOf('*');
            if (index != -1)
                acceptLanguage = acceptLanguage.Remove(index);

            // Assume languages are already ordered by priority and thus discard any
            // quality values that may be associated with the language codes
            List<CultureInfo> languageInfo = new List<CultureInfo>();
            string[] languages = acceptLanguage.Split(new char[] { ',', ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            char[] separator = { ';' };
            foreach (string language in languages)
            {
                string langCode = language.Split(separator, 2)[0];
                // Convert to valid culture name
                index = langCode.IndexOf('-');
                if (index != -1)
                {
                    ++index;
                    langCode = langCode.Substring(0, index) + langCode.Substring(index).ToUpperInvariant();
                }

                languageInfo.Add(new CultureInfo(langCode));
            }
            // Mono does not fully implement the resource fallback process, so we emulate it manually:
            // Add the English default if it is not already in the list or has not already been checked
            // as a parent of another language in the list.
            if (!languageInfo.Exists(ci => ci.TwoLetterISOLanguageName.Equals(m_FallbackLanguage.Name)))
                languageInfo.Add(m_FallbackLanguage);

            return languageInfo.ToArray();
        }

        /// <summary>
        /// Translates a text according to the language information of the given environment context.
        /// </summary>
        /// <param name="env">The environment data with information about preferred languages</param>
        /// <param name="textId">The text to be translated</param>
        /// <returns>
        /// A translation of textId. If no translation is available, then textId is returned
        /// </returns>
        public static string Translate(Environment env, string textId)
        {
            return Translate(env.LanguageInfo, textId);
        }
        public static string Translate(CultureInfo[] languages, string textId)
        {
            if (languages != null && textId != string.Empty)
            {
                List<string> missing = new List<string>();
                // Iterate through the accepted languages until we find one we have a resource for.
                string translation = null;
                foreach (CultureInfo language in languages)
                {
                    try
                    {
                        translation = m_Resources.GetString(textId, language);

                        if (translation != null)
                            break;
                        else if (WebApp.WebAppInstance.LogMissingTranslations > 1)
                            missing.Add(language.Name); // report missing translations
                    }
                    catch (MissingManifestResourceException)
                    {
                        m_log.DebugFormat("[Wifi]: Missing resource for culture {0} when translating: '{1}'",
                            language, textId);
                        if (WebApp.WebAppInstance.LogMissingTranslations > 0)
                            missing.Add(language.Name); // record missing languages
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat("[Wifi]: {0} when translating '{1}' for culture {2}: {3}",
                            e.GetType().Name, (textId == null) ? "(null)" : textId, language, e.Message);
                    }
                }
                if (missing.Count > 0)
                    if (translation == null || WebApp.WebAppInstance.LogMissingTranslations > 1)
                        m_log.WarnFormat("[Wifi]: No translation into language(s) {0} found for '{1}'",
                            string.Join(", ", missing.ToArray()), textId);

                if (translation != null)
                    return translation;
            }
            return textId;
        }

        /// <summary>
        /// Finds a localized file resource as a best match for the language information
        /// specified in the given environment context.
        /// </summary>
        /// <param name="env">The environment data with information about preferred languages</param>
        /// <param name="path">The file path of a resource</param>
        /// <returns>
        /// The complete file path to a localized version of the resource. If there is not any
        /// localized version available, then the complete path to the original resource is returned.
        /// </returns>
        public static string LocalizePath(Environment env, string path)
        {
            if (env.LanguageInfo != null)
            {
                // Try to find a file that is localized for one of the accepted languages
                char[] separator = { '-' };
                foreach (CultureInfo language in env.LanguageInfo)
                {
                    string localizedPath;
                    if (CheckPathExists(path, language.Name, out localizedPath))
                        return localizedPath;

                    // Check language code without country code
                    if (language.TwoLetterISOLanguageName != language.Name)
                        if (CheckPathExists(path, language.TwoLetterISOLanguageName, out localizedPath))
                            return localizedPath;
                }
            }
            return Path.Combine(WifiUtils.DocsPath, path);
        }

        private static bool CheckPathExists(string path, string language, out string localizedPath)
        {
            string languagePath;
            if (m_PathCache.TryGet(path, language, out languagePath))
            {
                localizedPath = Path.Combine(WifiUtils.DocsPath, languagePath);
                return true;
            }
            else if (language != m_FallbackLanguage.Name)
            {
                languagePath = Path.Combine(language, path);
                localizedPath = Path.Combine(WifiUtils.DocsPath, languagePath);
                if (File.Exists(localizedPath))
                {
                    //m_log.DebugFormat("[Wifi]: L10n for {0} results in path: {1}", language, localizedPath);
                    m_PathCache.AddOrUpdate(path, language, languagePath);
                    return true;
                }
            }
            else
            {
                localizedPath = Path.Combine(WifiUtils.DocsPath, path);
                //m_log.DebugFormat("[Wifi]: L10n for {0} results in path: {1}", language, localizedPath);
                m_PathCache.AddOrUpdate(path, language, path);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Cache for mappings from a resource path and a language code onto a path to the localized resource.
    /// </summary>
    public class LocalizationPathCache
    {
        private Dictionary<string, ExpiringCache<string, string>> m_Cache;
        private TimeSpan m_CachingTime;

        public LocalizationPathCache(TimeSpan expiration)
        {
            m_CachingTime = expiration;
            m_Cache = new Dictionary<string, ExpiringCache<string, string>>();
        }

        public void AddOrUpdate(string path, string language, string languagePath)
        {
            ExpiringCache<string, string> map;
            if (!m_Cache.TryGetValue(path, out map))
                map = new ExpiringCache<string, string>();
            map.AddOrUpdate(language, languagePath, m_CachingTime);
            m_Cache[path] = map;
        }

        public bool TryGet(string path, string language, out string localizedPath)
        {
            ExpiringCache<string, string> map;
            if (m_Cache.TryGetValue(path, out map))
            {
                return map.TryGetValue(language, out localizedPath);
            }
            localizedPath = null;
            return false;
        }
    }
}
