using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Web;

namespace Diva.Wifi
{
    public struct Request
    {
        public string Resource;
        public HttpCookieCollection Cookies;
        public IPEndPoint IPEndPoint;
        public Hashtable Query;
    }
}
