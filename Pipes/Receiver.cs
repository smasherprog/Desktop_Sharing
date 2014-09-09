using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Pipes
{

    public class Receiver : IDisposable
    {
        private object _Singleton = null;
        private ServiceHost _Host = null;
        private string _PipeName = string.Empty;
        private string _PipeURI = string.Empty;
        private Type _Contract_Type = null;

        public Receiver(string pipename, string pipeuri, Type contract_type, object singleton)
        {
            _PipeName = pipename;
            _PipeURI = pipeuri;
            _Contract_Type = contract_type;
            _Singleton = singleton;
        }
        public bool Start()
        {
            try
            {
                _Host = new ServiceHost(_Singleton, new Uri(_PipeURI));
                _Host.AddServiceEndpoint(_Contract_Type, new NetNamedPipeBinding(), _PipeName);
                _Host.Open();
                return true;
            } catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Stop();
                return false;
            }
        }

        public void Stop()
        {
            if(_Host != null)
                if(_Host.State != CommunicationState.Closed)
                    _Host.Close();
            _Host = null;
        }

        // A basic dispose.
        public void Dispose()
        {
            Stop();
            if(_Singleton != null)
                _Singleton = null;
        }


    }
}
