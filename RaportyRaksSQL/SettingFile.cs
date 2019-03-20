using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RaksForPoverbike
{
    public class SettingFile
    {
        private string adresFTP, userFTP, passFTP, database, dataSourcePath;
        private string konfIniName = "konf.ini";

        public string DataSourcePath
        {
            get { return dataSourcePath; }
            set { dataSourcePath = value; }
        }

        public string Database
        {
            get { return database; }
            set { database = value; }
        }

        public string PassFTP
        {
            get { return passFTP; }
            set { passFTP = value; }
        }

        public string UserFTP
        {
            get { return userFTP; }
            set { userFTP = value; }
        }

        public string AdresFTP
        {
            get { return adresFTP; }
            set { adresFTP = value; }
        }

        public SettingFile()
        {
            wycztajUstawienia();
        }

        public SettingFile(bool sprawdzCzyPlikJest)
        {
            FileStream plik;
            StreamWriter zapisz;

            if (sprawdzCzyPlikJest)
            {
                logowanieDoPlikuLoc("Sprawdzenie czy pliku konfiguracji programu istnieje", "INFO");
                try
                {
                    if (File.Exists(konfIniName) == false)
                    {
                        logowanieDoPlikuLoc(">>>>>>> Tworze nowy pliku konfiguracji programu przy sprawdzaniu i uruchomieniu", "INFO");

                        plik = new FileStream(konfIniName, FileMode.CreateNew, FileAccess.Write);
                        zapisz = new StreamWriter(plik);

                        zapisz.WriteLine("/usr/raks/Data/F00001.fdb;");
                        zapisz.WriteLine("10.0.0.100");
                        zapisz.WriteLine("adresF");
                        zapisz.WriteLine("userF");
                        zapisz.WriteLine("haslF");

                        zapisz.Close();
                        plik.Close();
                    }
                }
                catch (Exception e)
                {
                    logowanieDoPlikuLoc("Bład wczytywania przy sprawdzaniu czy plik konfiguracji istnieje:" + e.Message, "ERROR");
                }
            }
            else
            {
                logowanieDoPlikuLoc("Sprawdzenie czy pliku konfiguracji programu istnieje, w trybie bez sprawdzania", "INFO");
            }
        }

        private void wycztajUstawienia()
        {
            logowanieDoPlikuLoc("Wczytywanie pliku konfiguracji programu", "INFO");

            FileStream plik;
            StreamReader czytaj;
            StreamWriter zapisz;

            try
            {
                logowanieDoPlikuLoc("Aktualny katalog pracy " + Directory.GetCurrentDirectory(), "INFO");
                if (File.Exists(konfIniName))
                {
                    plik = new FileStream(konfIniName, FileMode.Open, FileAccess.Read);
                    czytaj = new StreamReader(plik);

                    dataSourcePath = czytaj.ReadLine();
                    database = czytaj.ReadLine();

                    adresFTP = czytaj.ReadLine();
                    userFTP = czytaj.ReadLine();
                    passFTP = czytaj.ReadLine();
                
                    czytaj.Close();
                    plik.Close();

                    logowanieDoPlikuLoc("Dane konfiguracji programu wczytano poprawnie", "INFO");
                }else{
                    logowanieDoPlikuLoc(">>>>>>> Tworze nowy pliku konfiguracji programu", "INFO");
                    
                    plik = new FileStream(konfIniName, FileMode.CreateNew, FileAccess.Write);
                    zapisz = new StreamWriter(plik);

                    zapisz.WriteLine("/usr/raks/Data/F00001.fdb;");
                    zapisz.WriteLine("10.0.0.100");
                    zapisz.WriteLine("adresF");
                    zapisz.WriteLine("userF");
                    zapisz.WriteLine("haslF");

                    zapisz.Close();
                    plik.Close();
                }
            }
            catch (IOException e)
            {
                logowanieDoPlikuLoc("Bład wczytywania pliku konfiguracji polaczenia:" + e.Message, "ERROR");
            }
        }

        public void logowanieDoPlikuLoc(string komunikat, string typLoga)
        {
            string logpath = @"C:\\imex\\logRaportyPowerBike" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + ".log";
            if (!File.Exists(logpath))
            {
                using (StreamWriter sw = File.CreateText(logpath))
                {
                    sw.WriteLine(typLoga + ";" + DateTime.Now.ToString() + ";" + komunikat);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(logpath))
                {
                    sw.WriteLine(typLoga + ";" + DateTime.Now.ToString() + ";" + komunikat);
                }
            }
        } 
    }
}
