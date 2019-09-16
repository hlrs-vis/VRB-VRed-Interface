using System.Runtime.CompilerServices;

namespace BuisnessLayer.Manager
{
    public class LogHelper
    {
        public static log4net.ILog GetLogger([CallerFilePath] string filename = "")
        {
            return log4net.LogManager.GetLogger(filename);  
        }
    }
}
