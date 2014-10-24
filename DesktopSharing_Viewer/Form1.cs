using DesktopSharing_Viewer.Code;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace DesktopSharing
{
    public partial class Form1 : Form
    {
        DesktopSharing_Viewer.Code.Viewer_Loop _Viewer_Loop;

        public Form1(string ipaddress)
        {
            InitializeComponent();
            FormClosing += Form1_FormClosing;
            _Viewer_Loop = new DesktopSharing_Viewer.Code.Viewer_Loop();
            _Viewer_Loop.UpdateImageEvent += Update_Image;
            _Viewer_Loop.NewImageEvent += New_Image;
            _Viewer_Loop.MouseImageChangedEvent += _Viewer_Loop_MouseUpdateEvent;
            _Viewer_Loop.MousePositionChangedEvent += _Viewer_Loop_MousePositionChangedEvent;
            var t = new InputListener(pictureBox1.Handle);
            t.InputMouseEvent += _Viewer_Loop.OnMouseEvent;
            t.InputKeyEvent += _Viewer_Loop.OnKeyEvent;

            Application.AddMessageFilter(t);
            this.DragDrop += new DragEventHandler(this.Form1_DragDrop);
            this.DragEnter += new DragEventHandler(this.Form1_DragEnter);

            _Viewer_Loop.IPtoConnect = ipaddress;
            _Viewer_Loop.Start();
        }

        void _Viewer_Loop_MousePositionChangedEvent(Point tl)
        {

            pictureBox1.Mouse_Position = tl;


            pictureBox1.Invoke((MethodInvoker)delegate
            {
                try
                {
                    pictureBox1.Invalidate();
                } catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            });

        }

        void _Viewer_Loop_MouseUpdateEvent(Point tl, byte[] data)
        {


            pictureBox1.Invoke((MethodInvoker)delegate
            {
                try
                {
                    var dt = DateTime.Now;
                    using(var ms = new MemoryStream(data))
                    {
                        var b = (Bitmap)Bitmap.FromStream(ms);
                        pictureBox1.Mouse_Image = b;
                        pictureBox1.Mouse_Position = tl;
                    }
                    Debug.WriteLine("_Viewer_Loop_MouseUpdateEvent (1): " + (DateTime.Now - dt).TotalMilliseconds + "ms");
                     pictureBox1.Invalidate();
                } catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            });

        }



        private void Update_Image(Rectangle p, byte[] m)
        {

            pictureBox1.Invoke((MethodInvoker)delegate
            {
                try
                {
                    var dt = new Stopwatch();
                    dt.Start();
                    pictureBox1.UpdateRegion(p, m);
                    dt.Stop();
                    Debug.WriteLine("Update_Image (1): " + dt.ElapsedMilliseconds);
                    pictureBox1.Invalidate();
                } catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            });

        }

        private void New_Image(Point sz, byte[] m)
        {
            pictureBox1.Invoke((MethodInvoker)delegate
            {
                try
                {
                    var dt = DateTime.Now;
                    pictureBox1.New_Image(sz, m);
                    pictureBox1.Invalidate();
                } catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            });
        }


        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            // If the data is a file or a bitmap, display the copy cursor. 
            if(e.Data.GetDataPresent(DataFormats.Bitmap) ||
               e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            } else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        void Form1_DragDrop(object sender, DragEventArgs e)
        {
            _Viewer_Loop.SendFiles((string[])e.Data.GetData(DataFormats.FileDrop));
        }
        void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _Viewer_Loop.Stop();
            Application.Exit();
        }
    }
}
