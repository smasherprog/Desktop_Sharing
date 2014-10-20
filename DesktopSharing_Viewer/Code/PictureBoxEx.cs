using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DesktopSharing_Viewer.Code
{
    public class PictureBoxEx : PictureBox
    {
        public Bitmap MouseImage = null;
        public Point MousePos;
        public PictureBoxEx()
        {
            this.SetStyle(ControlStyles.Selectable, true);
            MouseEnter += PictureBoxEx_MouseEnter;
            MouseLeave += PictureBoxEx_MouseLeave;
            Paint += PictureBoxEx_Paint;
        }

        void PictureBoxEx_Paint(object sender, PaintEventArgs e)
        {
            DrawMouse(e.Graphics);
        }

        void PictureBoxEx_MouseLeave(object sender, EventArgs e)
        {
            // Cursor.Show();
        }

        void PictureBoxEx_MouseEnter(object sender, EventArgs e)
        {

            // Cursor.Hide();
        }

        protected override void OnClick(EventArgs e)
        {
            this.Focus();
            base.OnClick(e);
        }
        private void DrawMouse(Graphics g)
        {
            if(MouseImage != null && Image != null)
            {
                var dt = DateTime.Now;
                var mouserect = new Rectangle(MousePos.X, MousePos.Y, 32, 32);
                var imgrect = new Rectangle(0, 0, Image.Width, Image.Height);

                if(imgrect.Contains(new Point(MousePos.X, MousePos.Y)))
                {
                    g.DrawImage(MouseImage, MousePos);
                }
                Debug.WriteLine("DrawMouse : " + (DateTime.Now - dt).TotalMilliseconds + "ms");
            }
        }
    }
}
