using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Diva.Wifi
{
    public class Environment
    {
        private static Dictionary<string, object> m_StaticVariables = new Dictionary<string, object>();
        public static Dictionary<string, object> StaticVariables
        {
            get { return m_StaticVariables; }
        }

        private static IWebApp m_WebApp;
        public static IWebApp WebAppObj
        {
            get { return m_WebApp; }
        }

        public static Type WebAppType
        {
            get { return m_WebApp.GetType(); }
        }

        private Request m_Request;
        public Request Request
        {
            get { return m_Request; }
        }

        private StateFlags m_Flags;
        public StateFlags Flags
        {
            get { return m_Flags; }
            set { m_Flags = value; }
        }

        private static Dictionary<string, MethodInfo> m_Methods = new Dictionary<string, MethodInfo>();

        public Environment(Request req)
        {
            m_Request = req;
        }


        public static void InitializeWebApp(IWebApp webApp)
        {
            if (webApp == null)
                return;

            m_WebApp = webApp;
            foreach (MethodInfo minfo in m_WebApp.GetType().GetMethods())
                m_Methods[minfo.Name] = minfo;
        }

        public static MethodInfo GetMethod(string name)
        {
            if (m_Methods.ContainsKey(name))
                return m_Methods[name];

            return null;
        }
    }

    public enum StateFlags : int
    {
        FailedLogin = 1,
        SuccessfulLogin = 2,
        IsLoggedIn = 4,
        IsAdmin = 8,
        UserAccountForm = 16,
        UserAccountFormResponse = 32,
        NewAccountForm = 64,
        NewAccountFormResponse = 128
    }
}
