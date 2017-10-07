using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Topshelf;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Win32;
using KonfiguratorConnectorRaksSQL;

namespace ConnectorRaksSQL
{
    class CronWebService : ServiceControl
    {
        private readonly Timer _timer;
        public HostControl hostControl;
        bool trwaEksportStanow = false;

        public CronWebService()
        {
            //10 sekund Timer(10000) 
            //_timer = new Timer(10000) { AutoReset = true };
            //1 minuta Timer(60000)
            //_timer = new Timer(60000) { AutoReset = true };
            //5 minuta Timer(60000)
            //_timer = new Timer(5 * 60000) { AutoReset = true };
            //10 minut Timer(600000)
            _timer = new Timer(600000) { AutoReset = true };
            _timer.Elapsed += (sender, eventArgs) => Console.WriteLine("Uruchomiono usługę wymiany danych ze sklepem {0} i wszystko wyglada OK", DateTime.Now);
            _timer.Elapsed += new ElapsedEventHandler(this.cronAction);
        }
        public bool Start(HostControl hostControl) 
        {
            ConnectorRaksSQL.Program.logowanieDoPlikuConRaks("Start timera", "INFO");
            this.hostControl = hostControl;
            _timer.Start();
            return true;
        }

        public bool Stop(HostControl hostControl) 
        {
            ConnectorRaksSQL.Program.logowanieDoPlikuConRaks("Stop timera", "INFO");
            _timer.Stop();
            return true;
        }

        public void cronAction(object sender, EventArgs e)
        {
            if (
                (DateTime.Now.Hour >= 7 && DateTime.Now.Hour < 22) && (DateTime.Now.Minute >= 0 && DateTime.Now.Minute < 10)
                )
            {
                ConnectorRaksSQL.Program.logowanieDoPlikuConRaks("cronAction Aktualizacja stanów magazynowych o na poczatku każdej godziny od 8 do 21", "INFO");
                ConnectorRaksSQL.Program.logowanieDoPlikuLocConRaks("cronAction Aktualizacja stanów magazynowych o na poczatku każdej godziny od 8 do 21", "INFO");

                FBConn fbc = new FBConn();
                ConnectorRaksSQL.Program.logowanieDoPlikuConRaks("cronAction (START) Po deklaracji połaczenia dla aktualizacji stanów magazynowych", "INFO");
                ConnectorRaksSQL.Program.logowanieDoPlikuLocConRaks("cronAction (START) Po deklaracji połaczenia dla aktualizacji stanów magazynowych", "INFO");
                trwaEksportStanow = true;
                RaksRepo rr = new RaksRepo(fbc, true, false);
                ConnectorRaksSQL.Program.logowanieDoPlikuConRaks("cronAction (STOP) Po wykonaniu dla aktualizacji stanów magazynowych", "INFO");
                ConnectorRaksSQL.Program.logowanieDoPlikuLocConRaks("cronAction (STOP) Po wykonaniu dla aktualizacji stanów magazynowych", "INFO");
                fbc.setConnectionOFF();
                trwaEksportStanow = false;
            }
            else if (
                (DateTime.Now.Hour >= 7 && DateTime.Now.Hour < 22) && (DateTime.Now.Minute >= 10)
                )
            {
                if (trwaEksportStanow)
                {
                    ConnectorRaksSQL.Program.logowanieDoPlikuConRaks("cronAction Wywołanie importu zamówień gdy trwa jeszcze synchronizacja stanów mafgazynowych!", "WARNING");
                    ConnectorRaksSQL.Program.logowanieDoPlikuLocConRaks("cronAction Wywołanie importu zamówień gdy trwa jeszcze synchronizacja stanów mafgazynowych!", "WARNING");
                }
                
                ConnectorRaksSQL.Program.logowanieDoPlikuConRaks("cronAction Import zamówień przy każdym odpaleniu za wyjątkiem poczatku godziny w godzinach od 7 do 22", "INFO");
                ConnectorRaksSQL.Program.logowanieDoPlikuLocConRaks("cronAction Import zamówień przy każdym odpaleniu za wyjątkiem poczatku godziny w godzinach od 7 do 22", "INFO");

                FBConn fbc = new FBConn();
                ConnectorRaksSQL.Program.logowanieDoPlikuConRaks("cronAction (START) Po deklaracji połaczenia, import zamówień", "INFO");
                ConnectorRaksSQL.Program.logowanieDoPlikuLocConRaks("cronAction (START) Po deklaracji połaczenia, import zamówień", "INFO");
                RaksRepo rr = new RaksRepo(fbc, false, false);
                ConnectorRaksSQL.Program.logowanieDoPlikuConRaks("cronAction (STOP) Po wykonaniu importu zamówień", "INFO");
                ConnectorRaksSQL.Program.logowanieDoPlikuLocConRaks("cronAction (STOP) Po wykonaniu importu zamówień", "INFO");
                fbc.setConnectionOFF();
            }
            else if (DateTime.Now.Hour >= 23 && DateTime.Now.Hour < 24)
            {
                //Wstrzymanie wykonywania w nocy
                //wykonanie raz w nocy
                ConnectorRaksSQL.Program.logowanieDoPlikuConRaks("cronAction Wstrzymane wykonywanie w nocy 23-24", "INFO");
                ConnectorRaksSQL.Program.logowanieDoPlikuLocConRaks("cronAction Wstrzymane wykonywanie w nocy 23-24", "INFO");
            }
            else if (DateTime.Now.Hour >= 0 && DateTime.Now.Hour < 7)
            {
                //Wstrzymanie wykonywania po północy
                ConnectorRaksSQL.Program.logowanieDoPlikuConRaks("cronAction Wstrzymane wykonywanie w nocy 0-7", "INFO");
                ConnectorRaksSQL.Program.logowanieDoPlikuLocConRaks("cronAction Wstrzymane wykonywanie w nocy 0-7", "INFO");
            }
            else
            {
                ConnectorRaksSQL.Program.logowanieDoPlikuConRaks("cronAction Wykonywane każdorazowo gdy inne nie są wykonywane", "INFO");
                ConnectorRaksSQL.Program.logowanieDoPlikuLocConRaks("cronAction Wykonywane każdorazowo gdy inne nie są wykonywane", "INFO");
                FBConn fbc = new FBConn();
                ConnectorRaksSQL.Program.logowanieDoPlikuConRaks("cronAction Po deklaracji połaczenia dla importu zamówień", "INFO");
                ConnectorRaksSQL.Program.logowanieDoPlikuLocConRaks("cronAction Po deklaracji połaczenia dla importu zamówień", "INFO");
                RaksRepo rr = new RaksRepo(fbc,false, false);
                ConnectorRaksSQL.Program.logowanieDoPlikuConRaks("cronAction Po wykonaniu dla importu zamówień", "INFO");
                ConnectorRaksSQL.Program.logowanieDoPlikuLocConRaks("cronAction Po wykonaniu dla importu zamówień", "INFO");
                fbc.setConnectionOFF();
            }
        }

       
    }
}
