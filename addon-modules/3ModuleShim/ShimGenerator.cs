using System;
using System.Collections.Generic;
using System.Reflection;

namespace Diva.Shim
{
    class ShimGenerator
    {
        private static string letters = "abcdefghijklmnopqrstuvwxyz";

        private static string GenerateShimCode(Type interfaceType, string classBase)
        {
            string c = String.Empty;

            c += "namespace Careminster\n{\n";

            c += String.Format("    class {0}_shim : {1}\n    {{\n", classBase, interfaceType.Name);
            c += String.Format("        {0} m_proxy = null;\n\n", interfaceType.Name);
            c += String.Format("        public void SetTarget({0} newTarget)\n        {{\n", interfaceType.Name);

            EventInfo[] events = interfaceType.GetEvents();

            foreach (EventInfo e in events)
            {
                if (!e.IsSpecialName)
                {
                    c += String.Format("            m_proxy.{0} -= Raise_{0};\n", e.Name);
                    c += String.Format("            newTarget.{0} += Raise_{0};\n", e.Name);
                }
            }
            c += String.Format("            m_proxy = newTarget;\n        }}\n\n");

            foreach (EventInfo e in events)
            {
                if (!e.IsSpecialName)
                {
                    Type ht = e.EventHandlerType;
                    MethodInfo hm = ht.GetMethod("Invoke");

                    ParameterInfo ret = hm.ReturnParameter;
                    Type rt = ret.ParameterType;

                    ParameterInfo[] parms = hm.GetParameters();

                    List<string> paramTypes = new List<string>();
                    List<string> paramNames = new List<string>();
                    int count = 0;

                    foreach (ParameterInfo pi in parms)
                    {
                        Type t = pi.ParameterType;
                        string pn = letters.Substring(count, 1);
                        count++;
                        paramTypes.Add(t.Name + " " + pn);
                        paramNames.Add(pn);
                    }

                    string p = String.Empty;
                    if (paramTypes.Count > 0)
                        p = String.Join(", ", paramTypes.ToArray());
                    string n = String.Empty;
                    if (paramNames.Count > 0)
                        n = String.Join(", ", paramNames.ToArray());

                    c += String.Format("        public event {0} {1};\n\n", ht.Name, e.Name);

                    c += String.Format("        private {0} Raise_{1}({2})\n", rt.Name, e.Name, p);
                    c += String.Format("        {{\n");
                    c += String.Format("            {0} handler = {1};\n", ht.Name, e.Name);
                    c += String.Format("            if (handler != null)\n");
                    c += String.Format("                handler({0});\n", n);
                    c += String.Format("        }}\n\n");

//                    Console.WriteLine("Event {3} {1} {0}({2})", e.Name, rt.Name, p, ht.Name);
                }
            }

            MethodInfo[] methods = interfaceType.GetMethods();

            foreach (MethodInfo m in methods)
            {
                if (!m.IsSpecialName)
                {
                    ParameterInfo ret = m.ReturnParameter;
                    Type rt = ret.ParameterType;

                    ParameterInfo[] parms = m.GetParameters();

                    List<string> paramTypes = new List<string>();
                    List<string> paramNames = new List<string>();
                    int count = 0;

                    foreach (ParameterInfo pi in parms)
                    {
                        Type t = pi.ParameterType;
                        string pn = letters.Substring(count, 1);
                        count++;
                        paramTypes.Add(t.Name + " " + pn);
                        paramNames.Add(pn);
                    }

                    string p = String.Empty;
                    if (paramTypes.Count > 0)
                        p = String.Join(", ", paramTypes.ToArray());
                    string n = String.Empty;
                    if (paramNames.Count > 0)
                        n = String.Join(", ", paramNames.ToArray());

                    c += String.Format("        public {0} {1}({2})\n        {{\n", rt.Name, m.Name, p);
                    if (rt == typeof(void))
                        c += String.Format("            m_proxy.{0}({1});\n", m.Name, n);
                    else
                        c += String.Format("            return m_proxy.{0}({1});\n", m.Name, n);
                    c += String.Format("        }}\n\n");

//                    Console.WriteLine("Method {1} {0}({2})", m.Name, rt.Name, p);
                }
            }

            PropertyInfo[] properties = interfaceType.GetProperties();

            foreach (PropertyInfo p in properties)
            {
                if (!p.IsSpecialName)
                {
                    Type pt = p.PropertyType;

                    c += String.Format("        public {0} {1}\n        {{\n", pt.Name, p.Name);
                    if (p.CanRead)
                        c += String.Format("            get {{ return m_proxy.{0}; }}\n", p.Name);
                    if (p.CanWrite)
                        c += String.Format("            set {{ m_proxy.{0} = value; }}\n", p.Name);
                    c += String.Format("        }}\n\n");
                }
            }

            c += String.Format("    }}\n}}\n");

            return c;
        }
    }
}
