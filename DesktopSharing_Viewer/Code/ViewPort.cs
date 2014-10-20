using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DesktopSharing_Viewer.Code
{
    public class ViewPort : Panel
    {
        private Image _Image = null;
        public Image Image
        {
            get
            {
                return _Image;
            }
            set
            {
                if(_Image != null)
                    _Image.Dispose();
                _Image = value;
                if(_Image != null)
                {
                    Size = _Image.Size;
                }

            }
        }
        public Image _Mouse_Image = null;
        public Image Mouse_Image
        {
            get
            {
                return _Mouse_Image;
            }
            set
            {
                if(_Mouse_Image != null)
                    _Mouse_Image.Dispose();
                _Mouse_Image = value;
            }
        }
        public Point Mouse_Position = new Point(0, 0);

        //protected override CreateParams CreateParams
        //{
        //    get
        //    {
        //        CreateParams cp = base.CreateParams;
        //        cp.ExStyle |= 0x00000020; //WS_EX_TRANSPARENT 

        //        return cp;
        //    }
        //}

        public ViewPort()
        {

            this.SetStyle(ControlStyles.Selectable, true);
            MouseEnter += ViewPort_MouseEnter;
            MouseLeave += ViewPort_MouseLeave;
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.ContainerControl, false);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.Opaque, true);
            
        }
        void ViewPort_MouseLeave(object sender, EventArgs e)
        {
            Cursor.Show();
        }

        void ViewPort_MouseEnter(object sender, EventArgs e)
        {
            Cursor.Hide();
        }
        protected override void OnClick(EventArgs e)
        {
            this.Focus();
            base.OnClick(e);
        }
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // Don't paint background
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var dt = DateTime.Now;
            if(Image != null)
            {
          
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Default;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

                e.Graphics.DrawImageUnscaled(_Image, new Point(0, 0));
                if(Mouse_Image != null)
                    e.Graphics.DrawImageUnscaled(Mouse_Image, Mouse_Position);
         
            }
         //   Debug.WriteLine("OnPaint (1): " + (DateTime.Now - dt).TotalMilliseconds + "ms");
        }
    }
}
