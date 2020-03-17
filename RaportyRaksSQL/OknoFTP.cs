using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RaksForPoverbike;
using System.Net;
using System.IO;

namespace RaportyRaksSQL
{
    public partial class OknoFTP : Form
    {
        public OknoFTP()
        {
            InitializeComponent();
        }

        private void bClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OknoFTP_Load(object sender, EventArgs e)
        {
            try
            {
                SettingFile st = new SettingFile();

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + st.AdresFTP + "//");
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

                request.Credentials = new NetworkCredential(st.UserFTP, st.PassFTP);

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                textBox.Text = reader.ReadToEnd();

                reader.Close();
                response.Close();
            }
            catch (Exception ef)
            {
                MessageBox.Show("Błąd sprawdzania FTP: " + ef.Message);
                throw;
            }
        }
    }
}
