using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Key_Generator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var RSA = new RSACryptoServiceProvider(Convert.ToInt32(comboBox1.SelectedValue));
            var rootpath = textBox1.Text;
            if(!rootpath.EndsWith("\\"))rootpath+="\\";
            File.WriteAllText(rootpath + "publickey.xml", RSA.ToXmlString(false));
            File.WriteAllText(rootpath + "privatekey.xml", RSA.ToXmlString(true));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            textBox1.Text = folderBrowserDialog1.SelectedPath;
        }
    }
}
