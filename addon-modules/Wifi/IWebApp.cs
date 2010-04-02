using System;
using System.Collections.Generic;
using System.Text;

using Environment = Diva.Wifi.Environment;

namespace Diva.Wifi
{
    public interface IWebApp
    {
        string LoginRequest(Environment env, string firstName, string lastName, string password);
        string LogoutRequest(Environment env);
    }
}
