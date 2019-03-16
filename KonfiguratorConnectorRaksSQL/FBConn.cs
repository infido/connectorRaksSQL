using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using Logowanie;
using FirebirdSql.Data.FirebirdClient;

namespace KonfiguratorConnectorRaksSQL
{
    public class FBConn
    {
        const string RegistryKey = "SOFTWARE\\Infido\\KonektorSQL";
        public static FbConnection conn;
        private string bufforMSG="";
        private string pathInfo;

        public FBConn() 
        {
            setConnectionON(true);
            checkDefultKeysformRegistryForPresta();
        }

        public FBConn(string user,string pass,string path, string ip, string kluczPresta)
        {
            setConnectionStringToRegistry(user, pass, path, ip, kluczPresta);
            setConnectionON(true);
            checkDefultKeysformRegistryForPresta();
        }

        public static void checkDefultKeysformRegistryForPresta()
        {
            string test = FBConn.GetKeyFromRegistry("userHttp");
            if (test==null) 
            {
                FBConn.SetKeyToRegisrty("userHttp", "kodwygenerowanyprzezPrestaAPI"); 
            };
            if (FBConn.GetKeyFromRegistry("http") == null)
            {
                FBConn.SetKeyToRegisrty("http", "http://adresurlsklepupresta.pl/api"); 
            }
            if (FBConn.GetKeyFromRegistry("key") == null)
            {
                FBConn.SetKeyToRegisrty("key", "");
            }
            if (FBConn.GetKeyFromRegistry("magazyn") == null)
            {
                FBConn.SetKeyToRegisrty("magazyn", "1");
            }
            if (FBConn.GetKeyFromRegistry("stanymagazynow") == null)
            {
                FBConn.SetKeyToRegisrty("stanymagazynow", "'NS','KR'");
            }
        }

        private void setConnectionON(Boolean _trybTest)
        {
            
            conn = new FbConnection(getConnectionString());
            Logg logg = new Logg(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "9001: Ustawiono parametry połaczenia. ", true);

            try
            {
                conn.Open();
                if (conn.State > 0)
                {
                    if (_trybTest)
                    {
                        logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "9002: Nawiązano połaczenie. " + conn.DataSource + "  " + conn.Database + " Status=" + conn.State, true);
                    }
                    else
                    {
                        logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "9003: Nawiązano połaczenie! " + conn.DataSource + "  " + conn.Database + " Status=" + conn.State, true);
                    }
                }
                else
                {
                    if (_trybTest)
                    {
                        logg.setUstawienieLoga(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1003: Nie połączono! Status=" + conn.State, true);
                    }
                    else
                    {
                        logg.setUstawienieLoga(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1004: Błąd połączenia z bazą!", true); 
                    }
                }
            }
            catch (Exception ex)
            {
                if (_trybTest)
                {
                    logg.setUstawienieLoga(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1005: Błąd: " + ex.Message, true); 
                }
                else
                {
                    logg.setUstawienieLoga(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1006: Błąd: " + ex.Message, true);
                    setConnectionStringToRegistry("", "", "", "", "");
                }
            }
            bufforMSG = logg.getBuforKomunikatu();
        }

        public void setConnectionOFF()
        {
            conn.Close();
            Logg logg = new Logg(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "9003: Rozłaczono! Status=" + conn.State);
        }

        public void setConnectionStringToRegistry(string user,string pass,string path, string ip, string kluczPresta)
        {
            try
            {
                RegistryKey rejestr = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                if (rejestr == null)
                {
                    RegistryKey rejestrNew = Registry.CurrentUser.CreateSubKey(RegistryKey);
                    rejestr = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                }

                rejestr.SetValue("User", (user.Length>0) ? user : "SYSDBA");
                rejestr.SetValue("Pass", (pass.Length>0) ? pass : "masterkey");
                rejestr.SetValue("Path", (path.Length>0) ? path : "C:\\Program Files\\Raks\\Data\\F00001.FDB" );
                rejestr.SetValue("IP", (ip.Length>0) ? ip : "127.0.0.1");
                rejestr.SetValue("val", (kluczPresta.Length > 0) ? kluczPresta : "C1279M52HVGJ7RMAM2N19V78FAL1NTZ8");
            }
            catch (Exception ex)
            {
                Logg logg = new Logg(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1001: Błąd ustawienia wartości w rejestrze Windows: " + ex.Message);
                System.Windows.Forms.MessageBox.Show("1001: Błąd ustawienia wartości w rejestrze Windows: " + ex.Message);
            }

        }

        public string getConnectionString()
        {
            RegistryKey rejestr;
            string connectionString = "";
            try
            {
                rejestr = Registry.CurrentUser.OpenSubKey(RegistryKey);
                if (rejestr == null)
                {
                    setConnectionStringToRegistry("", "", "", "", "");
                }

                connectionString =
                    "User=" +(String)rejestr.GetValue("User") + ";" +
                    "Password=" + (String)rejestr.GetValue("Pass") + ";" +
                    "Database=" + (String)rejestr.GetValue("Path") + ";" +
                    "Datasource=" + (String)rejestr.GetValue("IP") + ";" +
                    "Port=3050;" +
                    "Dialect=3;" +
                    //"Charset=NONE;" +
                    "Charset=WIN1250;" +
                    "Role=;" +
                    "Connection lifetime=15;" +
                    "Pooling=true;" +
                    "MinPoolSize=0;" +
                    "MaxPoolSize=50;" +
                    "Packet Size=8192;" +
                    "ServerType=0";

                pathInfo = (String)rejestr.GetValue("IP") + ":" + (String)rejestr.GetValue("Path");
            }
            catch (Exception ex)
            {
                Logg logg = new Logg(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1002: Błąd odczytu constr z rejestrze Windows: " + ex.Message);
                System.Windows.Forms.MessageBox.Show("1002: Błąd odczytu constr z rejestrze Windows: " + ex.Message);
            }

            return connectionString;
        }

        public string getConnectionState()
        {
            return "Status połaczenia do IP:" + conn.DataSource + " do bazy: " + conn.Database + " ze statusem: " + conn.State;
        }

        public string getBufforKomunikatu()
        {
            return bufforMSG;
        }

        public static string GetKeyFromRegistry(string keyName)
        {
            RegistryKey rejestr;
            try
            {
                rejestr = Registry.CurrentUser.OpenSubKey(RegistryKey);
                return (String)rejestr.GetValue(keyName);
            }
            catch (Exception ex)
            {
                Logg logg = new Logg(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1005: Błąd odczytu klucza " + keyName + " z rejestrze Windows: : " + ex.Message);
                System.Windows.Forms.MessageBox.Show("1005: Błąd odczytu klucza " + keyName + " z rejestrze Windows: " + ex.Message);
                return "";
            }
        }

        public static void SetKeyToRegisrty(string keyName, string keyValue)
        {
            try
            {
                RegistryKey rejestr = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                if (rejestr == null)
                {
                    RegistryKey rejestrNew = Registry.CurrentUser.CreateSubKey(RegistryKey);
                    rejestr = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                }
                rejestr.SetValue(keyName,keyValue);
            }
            catch (Exception ex)
            {
                Logg logg = new Logg(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1001: Błąd ustawienia wartości w rejestrze Windows: " + ex.Message);
                System.Windows.Forms.MessageBox.Show("1001: Błąd ustawienia wartości w rejestrze Windows: " + ex.Message);
            }
        }

        public FbConnection getCurentConnection()
        {
            return conn;
        }

        public string getPathInfo()
        {
            return pathInfo;
        }

        public bool getConectedToFB()
        {
            return (conn.State > 0 ? true : false);
        }
    }
}
