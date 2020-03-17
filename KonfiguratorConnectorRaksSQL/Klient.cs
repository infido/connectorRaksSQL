using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KonfiguratorConnectorRaksSQL
{
    public class Klient
    {
        public string nazwisko {get; set;}
        public string imie { get; set; }
        public string email { get; set; }
        public string telefon { get; set; }
        public string komorka { get; set; }
        public long idPresta { get; set; }
        public long idRaks { get; set; }

        public Klient()
        {
        }

        public Klient(long id_presta, string nazwisko, string imie, string email)
        {
            this.idPresta = id_presta;
            this.nazwisko = nazwisko;
            this.imie = imie;
            this.email = email;
        }

        public string skrot()
        {
            //return nazwisko + " " + imie;
            return email;
        }

        public string nazwaPelna()
        {
            return imie + " " + nazwisko;
        }
    }
}
