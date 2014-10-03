using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace DesktopSharing_Viewer
{
    public partial class Connect : Form
    {
        public Connect()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string addrString = "192.168.0.1";
            IPAddress address;
            if(IPAddress.TryParse(textBox1.Text, out address))
            {
                addrString = address.ToString();
            } else
            {
                IPAddress[] addresslist = Dns.GetHostAddresses(textBox1.Text);
                if(addresslist.Length > 0)
                    addrString=addresslist[0].ToString();
                else
                {
                    MessageBox.Show("Address entered is invalid, please try again. You can enter a valid FQDN or an IPV4 Address.");
                    return;
                }
            }
            var f = new DesktopSharing.Form1(addrString);
            f.Show();
            this.Hide();
        }
    }
}
