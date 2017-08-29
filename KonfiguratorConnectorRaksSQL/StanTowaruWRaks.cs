using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KonfiguratorConnectorRaksSQL
{
    class StanTowaruWRaks
    {
        public double iloscRaks { get; set; }
        public int iloscPresta { get; set; }
        public string kodRaks { get; set; }
        public long idPresta { get; set; }

        public StanTowaruWRaks(long idInPresta, string kodTowaru, int wartoscPresta)
        {
            this.idPresta = idInPresta;
            this.kodRaks = kodTowaru;
            this.iloscPresta = wartoscPresta;
        }
    }
}
