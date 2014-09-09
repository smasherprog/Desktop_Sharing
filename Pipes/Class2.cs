using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Pipes
{  
    public class Sender
    {
        private string _PipeURI = string.Empty;
        private string _PipeName = string.Empty;
        private EndpointAddress _EndpointAddr = null;
        public Sender(string pipename, string pipeuri)
        {
            _PipeName = pipename;
            _PipeURI = pipeuri;
            _EndpointAddr = new EndpointAddress(string.Format("{0}/{1}", _PipeURI,_PipeName));
        }
        //public static void SendMessage(string messages)
        //{
            
        //    IPipeService proxy = ChannelFactory<IPipeService>.CreateChannel(new NetNamedPipeBinding(), ep);
        //    var s =proxy.PipeIn(messages);

        //}
    }
}
