using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KonfiguratorConnectorRaksSQL
{
    public class Adres
    {
        public long idPresta { get; set; }
        public string ulica { get; set; }
        public string nrBud { get; set; }
        public string nrLok { get; set; }
        public string miejscowosc { get; set; }
        public string poczta { get; set; }
        public string kod { get; set; }
        public string panstwo { get; set; }
        public string osKontakt { get; set; }
        public string nip { get; set; }

        public Adres() { }

        public Adres(long id_presta, string ulica, string nrBud, string nrLok, string miejscowosc, string poczta, string kod, string panstwo, string os_kontaktowa, string nip)
        {
            this.idPresta = id_presta;
            this.ulica = ulica;
            this.nrBud = nrBud;
            this.nrLok = nrLok;
            this.miejscowosc = miejscowosc;
            this.poczta = poczta;
            this.kod = kod;
            this.panstwo = panstwo;
            this.osKontakt = os_kontaktowa;
            this.nip = nip;
        }

        public string streetWithNumber()
        {
            string ul = this.ulica + " " + this.nrBud;
            if (this.nrLok != null && this.nrLok.Length > 0)
            {
                ul += "/" + this.nrLok;
            }
            return ul;
        }

        public string nrBudynkuNrLokalu()
        {
            string ul = this.nrBud;
            if (this.nrLok != null && this.nrLok.Length > 0)
            {
                ul += "/" + this.nrLok;
            }
            return ul;
        }
    }
}
