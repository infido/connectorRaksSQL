using KonfiguratorConnectorRaksSQL;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RaportyRaksSQL
{
    public partial class OknoKonfiguracjiPolaczenia : Form
    {
        const string RegistryKey = "SOFTWARE\\Infido\\KonektorSQL";
        FBConn fbconn;
        public OknoKonfiguracjiPolaczenia()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            lPathInfo.Text = "Nazwa maszyny lokalnej: " + System.Environment.MachineName;
            RegistryKey rejestr;
            try
            {
                rejestr = Registry.CurrentUser.OpenSubKey(RegistryKey);
                tIP.Text = (String)rejestr.GetValue("IP");
                tPath.Text = (String)rejestr.GetValue("Path");
                tLogin.Text = (String)rejestr.GetValue("User");
                tPass.Text = (String)rejestr.GetValue("Pass");
            }
            catch (Exception ex)
            {
                MessageBox.Show("1104: Błąd odczytu konfiguracji połączenia do bazy danych z rejestru Windows: " + ex.Message);
            }
        }

        private void bSave_Click(object sender, EventArgs e)
        {
            if (tPath.Text.Length == 0 || tIP.Text.Length < 9)
            {
                MessageBox.Show("Pola IP serwera Firebird i Ścieżka do bazy muszą być wypełnione.","Błąd wprowadzania konfiguracji");
            }
            else
            {
                string locLogin = tLogin.Text.Length == 0 ? "SYSDBA" : tLogin.Text;
                string locPass = tPass.Text.Length == 0 ? "masterke" : tPass.Text;
                fbconn = new FBConn(locLogin, locPass, tPath.Text, tIP.Text);
                lPathInfo.Text = fbconn.getPathInfo();
                tOutput.Text += fbconn.getBufforKomunikatu() + Environment.NewLine;
                tOutput.Text += fbconn.getConnectionState() + Environment.NewLine;
                try
                {
                    fbconn.setConnectionOFF();
                    tOutput.Text += fbconn.getConnectionState() + Environment.NewLine;
                    tOutput.Text += "Zakończono testowanie połączenia " + Environment.NewLine;
                }
                catch (Exception)
                {
                    throw;
                }
            }

        }

        private void bClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OknoKonfiguracjiPolaczenia_Load(object sender, EventArgs e)
        {
            button1.PerformClick();
        }
    }
}
