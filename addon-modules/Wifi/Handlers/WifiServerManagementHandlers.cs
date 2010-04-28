using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
using OpenSim.Services.AuthenticationService;
using OpenSim.Services.UserAccountService;
using OpenSim.Services.InventoryService;

using Diva.Wifi.WifiScript;
using Environment = Diva.Wifi.Environment;
using OpenSim.Server.Base;
namespace Diva.Wifi
{

    /// <summary>
    /// The handler for the HTTP GET method on the resource /wifi/admin/server
    /// </summary>
    public class WifiServerManagementGetHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;

        public WifiServerManagementGetHandler(WebApp webapp) :
            base("GET", "/wifi/admin/server")
        {
            m_WebApp = webapp;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            // path = /wifi/...
            //m_log.DebugFormat("[Wifi]: path = {0}", path);

            // This is the content type of the response. Don't forget to set it to this in all your handlers.
            httpResponse.ContentType = "text/html";

            string resource = GetParam(path);
            //m_log.DebugFormat("[XXX]: resource {0}", resource);
            Request request = WifiUtils.CreateRequest(resource, httpRequest);
            Diva.Wifi.Environment env = new Diva.Wifi.Environment(request);

            string result = string.Empty;
            if (resource.Equals("/") || resource.Equals(string.Empty))
                // client invoked /wifi/admin/users/ with no further parameters
                result = m_WebApp.Services.ServerManagementGetRequest(env);

            return WifiUtils.StringToBytes(result);

        }

    }

    /// <summary>
    /// The handler for the HTTP POST method on the resource /wifi/admin/users
    /// </summary>
    public class WifiServerManagementPostHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebApp m_WebApp;

        public WifiServerManagementPostHandler(WebApp webapp) :
            base("POST", "/wifi/admin/server")
        {
            m_WebApp = webapp;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            // It's a POST, so we need to read the data on the stream, the lines after the blank line
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            httpResponse.ContentType = "text/html";

            string resource = GetParam(path);
            //m_log.DebugFormat("[XXX]: query String: {0}; resource: {1}", body, resource);

            try
            {
                // Here the data on the stream is transformed into a nice dictionary of keys & values
                Dictionary<string, object> postdata =
                        ServerUtils.ParseQueryString(body);

                Request req = WifiUtils.CreateRequest(resource, httpRequest);
                Diva.Wifi.Environment env = new Diva.Wifi.Environment(req);

                string result = string.Empty;
                if (resource.Equals("/") || resource.Equals(string.Empty))
                {

                }
                else if (resource.StartsWith("/shutdown"))
                {
                    result = m_WebApp.Services.ServerManagementShutdownPostRequest(env);
                }

                return WifiUtils.StringToBytes(result);

            }
            catch (Exception e)
            {
                m_log.DebugFormat("[USER ACCOUNT POST HANDLER]: Exception {0}", e);
            }

            return WifiUtils.FailureResult();

        }

    }

}
