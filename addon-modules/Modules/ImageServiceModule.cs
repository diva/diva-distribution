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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using log4net;
using Nini.Config;

using Mono.Addins;

[assembly: Addin("Diva.Modules", "0.1")]
[assembly: AddinDependency("OpenSim", "0.5")]

namespace MetaverseInk.ImageService
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]

    public class ImageServiceModule : ISharedRegionModule
    {
        #region Class and Instance Members

        private const string SeaWaterTextureID = "00000000-0000-2222-3333-100000001034";

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static bool m_initialized = false;
        private static object openjpeglock = new Object();

        private bool m_enabled = false;
        private string m_snapsDir = "DataSnapshot";
        private Scene m_scene;
        private Dictionary<string, List<string>> m_textures = new Dictionary<string, List<string>>();
        
        #endregion

        #region ISharedRegionModule

        public void Initialise(IConfigSource config)
        {
            m_log.Info("[MI IMAGESERVICE] Initializing...");

            try
            {
                m_enabled = config.Configs["DataSnapshot"].GetBoolean("index_sims", m_enabled);

                m_snapsDir = config.Configs["DataSnapshot"].GetString("snapshot_cache_directory", m_snapsDir);
            }
            catch (Exception)
            {
                m_log.Info("[MI IMAGESERVICE]: Could not load configuration. ImageService will be disabled.");
                m_enabled = false;
                return;
            }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        public string Name
        {
            get { return "Region Image Service"; }
        }

        public Type ReplaceableInterface 
        { 
            get { return null; }
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            // Store only the first scene
            if (m_scene == null)
                m_scene = scene;
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
            if (m_enabled && !m_initialized)
            {
                InitialiseDataRequestHandler();
                StartFileSystemWatcher();
                m_initialized = true;
            }
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
            m_scene = null;
            m_textures.Clear();
        }


        #endregion

        #region Web Face

        private void InitialiseDataRequestHandler()
        {
            if (MainServer.Instance.AddHTTPHandler("image", OnGetTexture))
            {
                m_log.Info("[MI IMAGESERVICE]: Set up image service");
            }
        }

        private Hashtable OnGetTexture(Hashtable keysvals)
        {
            Hashtable reply = new Hashtable();
            int statuscode = 200;

            string imgkey = (string)keysvals["key"];
            if (imgkey == null || imgkey.Equals(""))
            {
                imgkey = SeaWaterTextureID;
            }

            m_log.Info("[MI IMAGESERVICE] Request for image " + imgkey);

            byte[] data = GetTexture(imgkey);

            if (data.Length == 1)
            {
                reply["str_response_string"] = "Error";
                reply["int_response_code"] = 401;
                reply["content_type"] = "text/plain";

            }
            else
            {
                // What follows was bluntly copied from the WorldMap module

                MemoryStream imgstream = new MemoryStream();
                Bitmap imgTexture = new Bitmap(1, 1);
                ManagedImage managedImage;
                Image image = (Image)imgTexture;

                try
                {
                    // Taking our jpeg2000 data, decoding it, then saving it to a byte array with regular jpeg data

                    imgstream = new MemoryStream();

                    // Decode image to System.Drawing.Image
                    if (OpenJPEG.DecodeToImage(data, out managedImage, out image))
                    {
                        // Save to bitmap
                        imgTexture = new Bitmap(image, new Size(256, 256));

                        ImageCodecInfo myImageCodecInfo;

                        Encoder myEncoder;

                        EncoderParameter myEncoderParameter;
                        EncoderParameters myEncoderParameters = new EncoderParameters();

                        myImageCodecInfo = GetEncoderInfo("image/jpeg");

                        myEncoder = Encoder.Quality;

                        myEncoderParameter = new EncoderParameter(myEncoder, 95L);
                        myEncoderParameters.Param[0] = myEncoderParameter;

                        myEncoderParameter = new EncoderParameter(myEncoder, 95L);
                        myEncoderParameters.Param[0] = myEncoderParameter;

                        // Save bitmap to stream
                        imgTexture.Save(imgstream, myImageCodecInfo, myEncoderParameters);

                        // Write the stream to a byte array for output
                        data = imgstream.ToArray();
                    }
                }
                catch (Exception)
                {
                    m_log.Warn("[MI IMAGESERVICE]: Unable to generate image " + imgkey);
                }
                finally
                {
                    // Reclaim memory, these are unmanaged resources
                    imgTexture.Dispose();
                    image.Dispose();
                    imgstream.Close();
                    imgstream.Dispose();
                }
            }
            string dataStr = Convert.ToBase64String(data);
            reply["str_response_string"] = dataStr;
            reply["int_response_code"] = statuscode;
            reply["content_type"] = "image/jpeg";

            return reply;
        }

        private byte[] GetTexture(string key)
        {
            byte[] imgdata = new byte[1];
            UUID uuid = UUID.Zero;
            if (UUID.TryParse(key, out uuid))
            {
                if (AssertImageIsPublic(key))
                {
                    AssetBase asset = m_scene.AssetService.Get(uuid.ToString());

                    if (asset != null)
                    {
                        imgdata = asset.Data;
                    }
                }
            }
            if (imgdata.Length == 1) // Something wrong with the image; maybe it didn't exist or no perms. Send back the clear texture
            {
                m_log.Info("[MI IMAGESERVICE] Unable to serve image " + key);
                UUID.TryParse(SeaWaterTextureID, out uuid);
                AssetBase asset = m_scene.AssetService.Get(uuid.ToString());
                if (asset != null)
                    imgdata = asset.Data;
            }
            return imgdata;
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (int j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        // Make sure the requested image is referred to in the DataSnapshot's xml file
        private bool AssertImageIsPublic(string uuid)
        {
            foreach (List<string> lst in m_textures.Values)
                if (lst.Contains(uuid))
                    return true;
            return false;
        }

        #endregion

        #region DataSnapshot Files Monitoring

        private FileSystemWatcher watcher = new FileSystemWatcher();

        private void StartFileSystemWatcher()
        {
            m_log.Info("[MI IMAGESERVICE] Starting watcher for " + m_snapsDir);
            if (InitialRead(m_snapsDir))
            {
                // Try to create the directory.
                m_log.Info("[MI IMAGESERVICE]: Creating directory " + m_snapsDir);
                try
                {
                    Directory.CreateDirectory(m_snapsDir);
                }
                catch (Exception e)
                {
                    m_log.Error("[MI IMAGESERVICE]: Failed to create directory " + m_snapsDir, e);

                    //This isn't a horrible problem, just disable cacheing.
                    m_log.Error("[MI IMAGESERVICE]: Could not create directory, response cache has been disabled.");
                }
            }
            watcher.Path = m_snapsDir;
            watcher.Filter = "*.xml";
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;

            watcher.Changed += new FileSystemEventHandler(OnFileChanged);
            watcher.Created += new FileSystemEventHandler(OnFileChanged);
            watcher.Deleted += new FileSystemEventHandler(OnFileDeleted);

            watcher.EnableRaisingEvents = true;
        }


        void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Wait a little to avoid trying to open the files before DataSnapshot is done with them
            Thread.Sleep(3);
            AddImages(e.FullPath, e.Name);
            //DumpTextureUUIDs();
        }

        void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            m_textures.Remove(e.Name);
            //DumpTextureUUIDs();
        }


        private bool InitialRead(string path)
        {
            try
            {
                foreach (string file in Directory.GetFiles(path, "*.xml"))
                {
                    string name = Path.GetFileName(path);
                    AddImages(file, name);
                }
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[MI IMAGESERVICE] Could not read {0}: {1}", path, e.Message);
                return false;
            }
            return true;
        }

        private void AddImages(string fullPath, string name)
        {
            try
            {
                using (StreamReader reader = File.OpenText(fullPath))
                {
                    string line = reader.ReadToEnd();
                    FetchImageUUIDs(name, line);
                }
            }
            catch (Exception ex)
            {
                m_log.Warn("[IMAGESERVICE] Couldn't read xml file " + name + ". Reason: " + ex.Message);
            }
        }

        private const string ImageTagBegin = "<image>";
        private const string ImageTagEnd = "</image>";

        private void FetchImageUUIDs(string filename, string str)
        {
            if (str == "")
                return;
            int start_idx = str.IndexOf(ImageTagBegin);
            int stop_idx = str.IndexOf(ImageTagEnd) - 1;
            if ((start_idx == -1) || (stop_idx == -1)) // <image> or </image> not found
                return;
            start_idx += ImageTagBegin.Length;

            string uuid = str.Substring(start_idx, stop_idx - start_idx + 1);
            if (m_textures.ContainsKey(filename))
                m_textures[filename].Add(uuid);
            else
            {
                List<string> uuids = new List<string>();
                uuids.Add(uuid);
                m_textures.Add(filename, uuids);
            }

            // recurse
            FetchImageUUIDs(filename, str.Substring(stop_idx + ImageTagEnd.Length));
        }

        private void DumpTextureUUIDs()
        {
            Console.WriteLine("--- Begin DUMP ---");
            foreach (List<string> lst in m_textures.Values)
                foreach (string uuid in lst)
                    Console.WriteLine(uuid);
            Console.WriteLine("--- End DUMP ---");
        }
        #endregion 
    }
}
