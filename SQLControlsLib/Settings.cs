using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLControlsLib
{
    public class Settings
    {
        internal static string ConnectionString;
        public static void SetConnectionString(string conn)
        {
            ConnectionString = conn;
        }
    }
}
