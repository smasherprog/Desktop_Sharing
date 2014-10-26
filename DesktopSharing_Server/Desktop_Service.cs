using Desktop_Sharing_Shared.Screen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DesktopSharing_Server
{
    public class Desktop_Service : IDisposable
    {
        private int _Intervalms;
        private bool _Running = false;

        private ScreenCapture _ScreenCapture;
        public Raw_Image _LastImage = null;

        private bool _RunningAsService = false;
        private Desktop_Sharing_Shared.Desktop.DesktopInfo _DesktopInfo;

        private object _InputLock = new object();
        private List<Desktop_Sharing_Shared.Keyboard.KeyboardEventStruct> _KeyboardEvents;
        private List<Desktop_Sharing_Shared.Mouse.MouseEventStruct> _MouseEvents;
        public Desktop_Sharing_Shared.Mouse.MouseCapture _MouseCapture;

        public event Desktop_Sharing_Shared.Mouse.MouseCapture.MousePositionChangedHandler MousePositionChangedEvent;
        public event Desktop_Sharing_Shared.Mouse.MouseCapture.MouseImageChangedHandler MouseImageChangedEvent;

        private AutoResetEvent ScreenScanEvent = new AutoResetEvent(true);
        private DateTime LastScreenUpdate = DateTime.Now.AddDays(-10);
        private DateTime LastDeskSwitch = DateTime.Now.AddDays(-10);

        public bool Capturing = false;
        public Desktop_Service(int updateinterval = 100)
        {
            _RunningAsService = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToLower().Contains(@"nt authority\system");
            _Intervalms = updateinterval;
        }
        public delegate void ScreenUpdateHandler(byte[] data, Rectangle r);
        public event ScreenUpdateHandler ScreenUpdateEvent;

        public void Dispose()
        {
            Stop();
        }
        public void Start()
        {
            _Running = true;
            _ScreenCapture = new Desktop_Sharing_Shared.Screen.ScreenCapture(80);
            _KeyboardEvents = new List<Desktop_Sharing_Shared.Keyboard.KeyboardEventStruct>();
            _MouseEvents = new List<Desktop_Sharing_Shared.Mouse.MouseEventStruct>();
            _DesktopInfo = new Desktop_Sharing_Shared.Desktop.DesktopInfo();
            _MouseCapture = new Desktop_Sharing_Shared.Mouse.MouseCapture();
            _MouseCapture.MouseImageChangedEvent += _MouseCapture_MouseImageChangedEvent;
            _MouseCapture.MousePositionChangedEvent += _MouseCapture_MousePositionChangedEvent;
            _LastImage = _ScreenCapture.GetScreen(new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));
            _Start();

        }

        void _MouseCapture_MousePositionChangedEvent(Point tl)
        {
            if(MousePositionChangedEvent != null)
                MousePositionChangedEvent(tl);
        }

        void _MouseCapture_MouseImageChangedEvent(Point tl, byte[] data)
        {
            if(MouseImageChangedEvent != null)
                MouseImageChangedEvent(tl, data);
        }
        public void Stop()
        {
            _Running = false;
            //if(_LastImage != null)
            //    _LastImage.Dispose();
            _LastImage = null;
            if(_DesktopInfo != null)
                _DesktopInfo.Dispose();
            _DesktopInfo = null;
            if(_MouseCapture != null)
                _MouseCapture.Dispose();
            _MouseCapture = null;
            if(_ScreenCapture != null)
                _ScreenCapture.Dispose();
            _ScreenCapture = null;
        }
  
        private void _Start()
        {

            while(_Running)
            {
                try
                {
                    if(!Capturing)
                    {
                        Thread.Sleep(10);
                        continue;
                    }
                    System.Diagnostics.Stopwatch SW = new Stopwatch();
                    SW.Start();
                    _MouseCapture.Update();

                    if((DateTime.Now - LastScreenUpdate).TotalMilliseconds > _Intervalms)
                    {
                        //prevent excessive checking
                        if(_RunningAsService && (DateTime.Now - LastDeskSwitch).TotalMilliseconds > 500)
                        {//only a program running as under the account     nt authority\system       is allowed to switch desktops
                            var d = _DesktopInfo.GetActiveDesktop();
                            _ScreenCapture.ReleaseHandles();//make sure to release any handles before switching
                            if(d != _DesktopInfo.Current_Desktop)
                            {
                                _DesktopInfo.SwitchDesktop(d);
                            }
                            LastDeskSwitch = DateTime.Now;
                        }
                        SW.Stop();
                        var img = _ScreenCapture.GetScreen(new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));
                
                        Debug.WriteLine("GetScreen time: " + SW.ElapsedMilliseconds +"ms");
                        SW.Start();
                        Rectangle rect = Desktop_Sharing_Shared.Bitmap_Helper.Get_Diff(_LastImage, img);
                        _LastImage = img;
                        if(rect.Width > 0 && rect.Height > 0)
                        {
                            var rawimg = new Raw_Image
                            {
                                Data = Desktop_Sharing_Shared.Bitmap_Helper.Copy(img, rect),
                                Dimensions = rect
                            };
                            SW.Stop();
             
                            Debug.WriteLine("GetScreen Copy time: " + SW.ElapsedMilliseconds + "ms");
                       
                            ScreenScanEvent.WaitOne();
                            System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(ScreenUpdate_ThreadProc), rawimg);
                        }
                        LastScreenUpdate = DateTime.Now;
                    }

                    lock(_InputLock)
                    {
                        foreach(var item in _KeyboardEvents)
                            _DesktopInfo.InputKeyEvent(item);
                        foreach(var item in _MouseEvents)
                            _DesktopInfo.InputMouseEvent(item);
                        _KeyboardEvents.Clear();
                        _MouseEvents.Clear();

                    }
             
                    Thread.Sleep(10);
                } catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

        }
        public void ScreenUpdate_ThreadProc(object i)
        {
            System.Diagnostics.Stopwatch SW = new Stopwatch();
            SW.Start();
            try
            {
                var r = (Raw_Image)i;
                if(ScreenUpdateEvent != null)
                    ScreenUpdateEvent(r.Data, r.Dimensions);

            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            SW.Stop();
            Debug.WriteLine("ScreenUpdate_ThreadProc time: " + SW.ElapsedMilliseconds);
            ScreenScanEvent.Set();
        }
        public void KeyEvent(Desktop_Sharing_Shared.Keyboard.KeyboardEventStruct k)
        {
            lock(_InputLock)
            {
                _KeyboardEvents.Add(k);
            }
        }
        public void MouseEvent(Desktop_Sharing_Shared.Mouse.MouseEventStruct m)
        {
            lock(_InputLock)
            {
                _MouseEvents.Add(m);
            }
        }
        public void FileEvent(string filename, byte[] file)
        {
            _DesktopInfo.FileEvent(filename, file);
        }

        public void FolderEvent(string relativefolderpath)
        {
            _DesktopInfo.FolderEvent(relativefolderpath);
        }
    }
}
