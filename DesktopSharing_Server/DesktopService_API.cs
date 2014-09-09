using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Pipes
{
    [ServiceBehavior
        (InstanceContextMode = InstanceContextMode.Single)]
    public class DesktopService_API : IDesktopService_API
    {
        public static string URI = "net.pipe://localhost/IDesktopService_API";
        public string PipeIn(string data)
        {
            if(DataReady != null)
               return DataReady(data);
            return "";
        }

        public delegate string DataIsReady(string hotData);
        public DataIsReady DataReady = null;
    }

}
