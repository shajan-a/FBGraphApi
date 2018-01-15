using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FBGraphLib
{
    public class State
    {
        public CookieCollection Cookies { get; set; }
        public bool IsloggedIn { get; set; }
        public string AccessToken { get; set; }
    }
}
