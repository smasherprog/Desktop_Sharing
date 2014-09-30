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
          
        }

        protected override void OnClick(EventArgs e)
        {
            this.Focus();
            base.OnClick(e);
        }
    }
}
