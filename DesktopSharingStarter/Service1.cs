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
        public Service1()
        {
            InitializeComponent();
        }
        protected override void OnStart(string[] args)
        {
            DesktopSharingServiceMonitor.Toolkit.ApplicationLoader.PROCESS_INFORMATION proc;
            DesktopSharingServiceMonitor.Toolkit.ApplicationLoader.StartProcessAndBypassUAC(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)+"\\DesktopSharing_Server.exe", out proc);
        }

        protected override void OnStop()
        {

        }

    }
}
