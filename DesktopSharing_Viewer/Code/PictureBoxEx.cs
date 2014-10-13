using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DesktopSharing_Viewer.Code
{
    public class PictureBoxEx : PictureBox
    {
        public PictureBoxEx()
        {
            this.SetStyle(ControlStyles.Selectable, true);
            MouseEnter += PictureBoxEx_MouseEnter;
            MouseLeave += PictureBoxEx_MouseLeave;
        }

        void PictureBoxEx_MouseLeave(object sender, EventArgs e)
        {
            Cursor.Show();
        }

        void PictureBoxEx_MouseEnter(object sender, EventArgs e)
        {
            Cursor.Hide();
        }

        protected override void OnClick(EventArgs e)
        {
            this.Focus();
            base.OnClick(e);
        }
    }
}
