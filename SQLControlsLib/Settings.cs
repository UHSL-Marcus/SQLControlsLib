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
