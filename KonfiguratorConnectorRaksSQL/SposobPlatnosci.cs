using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KonfiguratorConnectorRaksSQL
{
    public class SposobPlatnosci
    {
        public int id { get; set; }
        public string nazwa { get; set; }

        public SposobPlatnosci() { }

        public SposobPlatnosci(int id, string nazwa)
        {
            this.id = id;
            this.nazwa = nazwa;
        }
    }
}
