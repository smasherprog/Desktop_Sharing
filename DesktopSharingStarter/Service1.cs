using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Text;

namespace Client_Service
{
    public partial class Service1 : ServiceBase
    {
        private object _ServiceToRun;
        public Service1(object obj)
        {
            InitializeComponent();
            _ServiceToRun = obj;
        }
        protected override void OnStart(string[] args)
        {
            _ServiceToRun.GetType().GetMethod("OnStart").Invoke(_ServiceToRun, null);
        }

        protected override void OnStop()
        {
            _ServiceToRun.GetType().GetMethod("OnStop").Invoke(_ServiceToRun, null);
        }

    }
}
