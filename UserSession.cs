using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public static class UserSession
    {
        // This will hold the ID of the user who logged in
        public static int LoggedInUserId { get; set; }
        public static string LoggedInUserName { get; set; }
        public static void ClearSession()
        {
            LoggedInUserId = 0;
            LoggedInUserName = null;
        }
    }
}
