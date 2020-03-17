using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Topshelf;
using Topshelf.Logging;

//using LinqToDB.DataProvider.Firebird;
//using LinqToDB.Data;
//using LinqToDB.Mapping;

using KonfiguratorConnectorRaksSQL;


namespace ConnectorRaksSQL
{
    public class Program
    {
        static void Main(string[] args)
        {
            logowanieDoPlikuConRaks("Uruchomienie usługi: Usługa wymiany danych RaksSQL z PrestaSHOP", "INFO");

            var host = HostFactory.New(x =>
            {
                x.Service<CronWebService>(s =>
                {
                    s.ConstructUsing(() => new CronWebService());
                    s.WhenStarted((CronWebService, hostControl) => CronWebService.Start(hostControl));
                    s.WhenStopped((CronWebService, hostControl) => CronWebService.Stop(hostControl));
                });

                x.RunAsLocalService();
                x.SetDescription("Usługa wymiany danych RaksSQL z PrestaSHOP");
                x.SetDisplayName("ConnectorRaksSQL");
                x.SetServiceName("ConnectorRaksSQL");
                //x.UseLog4Net("log4net.config");

            });

            host.Run();
        }

        public static void logowanieDoPlikuConRaks(string komunikat, string typLoga)
        {
            logowanieDoPlikuLocConRaks(komunikat, typLoga);
            string logpath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\logConnectorRaksSQL" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + ".log";
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

        public static void logowanieDoPlikuLocConRaks(string komunikat, string typLoga)
        {
            if (!Directory.Exists("C:\\imex"))
            {
                Directory.CreateDirectory("C:\\imex");
            }

            string logpath = @"C:\\imex\\logConnectorRaksSQL" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + ".log";
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
