using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Pipes
{
    [ServiceContract(Namespace = "http://www.mydohc.com/DesktopsharingNamespace")]
    public interface IDesktopService_API
    {
        [OperationContract]
        string PipeIn(string data);
    }
}
