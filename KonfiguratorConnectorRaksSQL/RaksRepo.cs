using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FirebirdSql.Data.FirebirdClient;
using System.Windows.Forms;
using Bukimedia.PrestaSharp.Factories;
using Bukimedia.PrestaSharp.Entities;
using Logowanie;

namespace KonfiguratorConnectorRaksSQL
{
    public class RaksRepo
    {
        FBConn fbc;
        string BaseUrl = "http://adresurlsklepupresta.pl/api/";
        string Account = "";
        string Password = "kodwygenerowanyprzezPrestaAPI"; // To faktycznie jest użytkownik
        string Magazyn = "1";
        string StanyDlaMagnumer = "1";
        Logg logg;
        string resultMSGofStany;


        public RaksRepo(FBConn con, bool aktualizacjaTylkoStanowMagazynowych, bool trybGUI )
        {
            fbc = con;
            //int idzk_tmp;

            this.BaseUrl = FBConn.GetKeyFromRegistry("http");
            this.Account = FBConn.GetKeyFromRegistry("userHttp");
            this.Password = FBConn.GetKeyFromRegistry("key");
            this.Magazyn = FBConn.GetKeyFromRegistry("magazyn");
            this.StanyDlaMagnumer = FBConn.GetKeyFromRegistry("stanymagazynow");

            logg = new Logg(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Ustawiono klucze Presta i zmienne z rejestru Windows", true);

            if (aktualizacjaTylkoStanowMagazynowych)
            {
                logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "==================== Aktualizacja stanów magazynowych ====================", true);
                aktualizujStanyWPresta(trybGUI);
            }
            else
            {
                logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "==================== Import zamówień ====================", true);
                getZamFromPresta();
            }

        }

        public RaksRepo(FBConn con)
        {
            fbc = con;
            //int idzk_tmp;

            this.BaseUrl = FBConn.GetKeyFromRegistry("http");
            this.Account = FBConn.GetKeyFromRegistry("userHttp");
            this.Password = FBConn.GetKeyFromRegistry("key");
            this.Magazyn = FBConn.GetKeyFromRegistry("magazyn");
            this.StanyDlaMagnumer = FBConn.GetKeyFromRegistry("stanymagazynow");

            logg = new Logg(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Ustawiono klucze Presta i zmienne z rejestru Windows w trybie nonCron", true);

        }

        public void getZamFromPresta()
        {
            getZamFromPrestaByStatus("3"); // Przygotowanie w toku
            getZamFromPrestaByStatus("10"); //Oczekiwanie na płatnosc przelewem bankowym
            getZamFromPrestaByStatus("2"); //Płatnośc zaakceptowana
        }

        public void getZamFromPrestaByStatus(string status)
        {
            OrderFactory orderFactory = new OrderFactory(BaseUrl, Account, Password);

            //List<order> orders = orderFactory.GetAll();
            Dictionary<string, string> dtn = new Dictionary<string, string>();
            dtn.Add("current_state", status);
            List<order> orders = orderFactory.GetByFilter(dtn, null, null);

            List<Bukimedia.PrestaSharp.Entities.AuxEntities.language> valueLang = new List<Bukimedia.PrestaSharp.Entities.AuxEntities.language>();
            int idzk_tmp;

            logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Odnaleziono " + orders.Count + " zamówień o statusie: " + status +  ", do przeniesienia do Raks", true);

            foreach (order item in orders)
            {
                //if (item.id < 7745)
                if (1 == 1)
                {
                    //    //wykonuj tylko testowe

                    //sprawdzenie i odczytanie klienta
                    CustomerFactory customerFactory = new CustomerFactory(BaseUrl, Account, Password);
                    AddressFactory adressFactory = new AddressFactory(BaseUrl, Account, Password);
                    CountryFactory countryFactory = new CountryFactory(BaseUrl, Account, Password);
                    CarrierFactory carrierFactory = new CarrierFactory(BaseUrl, Account, Password);

                    customer klient;
                    address adrDost;
                    address adrPlatn;
                    country krajDost;
                    country krajPlat;
                    carrier sposobDostawy;
                    try
                    {
                        klient = customerFactory.Get((long)item.id_customer);
                        adrDost = adressFactory.Get((long)item.id_address_delivery);
                        krajDost = countryFactory.Get((long)adrDost.id_country);
                        adrPlatn = adressFactory.Get((long)item.id_address_invoice);
                        krajPlat = countryFactory.Get((long)adrPlatn.id_country);
                        sposobDostawy = carrierFactory.Get((long)item.id_carrier);
                    }
                    catch (Exception ex1)
                    {
                        logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Błąd przy pobieraniu klienta " + item.id_customer + ": " + ex1.Message, true);
                        throw;
                    }
                    string plKrajDostawy = "";
                    valueLang = krajDost.name;
                    foreach (Bukimedia.PrestaSharp.Entities.AuxEntities.language lang in valueLang)
                    {
                        plKrajDostawy += lang.Value + " ";
                    }
                    if (plKrajDostawy == null || plKrajDostawy == "") plKrajDostawy = "Polska";


                    string plKrajPlatnik = "";
                    valueLang = krajPlat.name;
                    foreach (Bukimedia.PrestaSharp.Entities.AuxEntities.language lang in valueLang)
                    {
                        plKrajPlatnik += lang.Value + " ";
                    }
                    if (plKrajPlatnik == null || plKrajPlatnik == "") plKrajPlatnik = "Polska";

                    Adres adresDostawy = new Adres((long)adrDost.id, adrDost.address1 + " " + adrDost.address2, "", "", adrDost.city, adrDost.city, adrDost.postcode, plKrajDostawy, adrDost.company + " " + adrDost.firstname + " " + adrDost.lastname + " (" + sposobDostawy.name + ")", adrDost.vat_number);
                    Adres adresPlatnika = new Adres((long)adrPlatn.id, adrPlatn.address1 + " " + adrPlatn.address2, "", "", adrPlatn.city, adrPlatn.city, adrPlatn.postcode, plKrajPlatnik, adrPlatn.company + " " + adrPlatn.firstname + " " + adrPlatn.lastname, adrPlatn.vat_number); ;
                    Klient kh = new Klient((long)klient.id, klient.lastname, klient.firstname, klient.email);

                    //powiązanie z kontrahentaem z Raks
                    // zmiana kodu na email
                    //kh.idRaks = setKontrahentInToRaks(Convert.ToString(kh.idPresta),kh.nazwaPelna(),adresPlatnika.nip,"PL","PLN",adresPlatnika.ulicaForRaks(), adresPlatnika.miejscowosc,adresPlatnika.panstwo,adresPlatnika.kod,adresPlatnika.poczta,kh.email);
                    kh.idRaks = setKontrahentInToRaks(kh.skrot(), kh.nazwaPelna(), adresPlatnika.nip, "PL", "PLN", adresPlatnika.ulica, adresPlatnika.nrBudynkuNrLokalu(), adresPlatnika.miejscowosc, adresPlatnika.panstwo, adresPlatnika.kod, adresPlatnika.poczta, kh.email);

                    SposobPlatnosci sp = getSposobPlatnosci(item.payment);

                    //Dodanei towarów do bazy
                    foreach (order_row pozzam in item.associations.order_rows)
                    {
                        getTowarByKodWithOutTrans(pozzam.product_reference, pozzam.product_name);
                    }

                    //zapis zamówinia
                    idzk_tmp = setNewZK(Convert.ToInt32(this.Magazyn), (int)item.id, "ZI/" + DateTime.Now.Year + "/" + item.id, sp, kh.idRaks, kh.skrot(), adresDostawy, adresPlatnika, item.reference, "", item.total_paid_tax_excl, item.total_paid_tax_incl);

                    //zapis pozycji zamówienia
                    if (idzk_tmp > 0)
                    {
                        FbTransaction myTransaction = fbc.getCurentConnection().BeginTransaction();
                        int lp = 0;
                        foreach (order_row pozzam in item.associations.order_rows)
                        {
                            lp += 1;
                            setAddNewPozZK(myTransaction, idzk_tmp, lp, pozzam.product_reference, pozzam.product_name, pozzam.product_quantity, pozzam.unit_price_tax_excl, pozzam.unit_price_tax_incl);
                        }
                        try
                        {
                            myTransaction.Commit();
                        }
                        catch (Exception TR)
                        {
                            logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "bŁĄD NA cOMIT TRANSACTON POZYCJI: " + TR.Message, true);
                            throw;
                        }
                    }
                    else
                    {
                        logg.setUstawienieLoga(Logg.RodzajLogowania.Warning, Logg.MediumLoga.File, "Wycofanie z zapisu pozycji zamówienia " + "ZI/" + DateTime.Now.Year + "/" + item.id + " otrzymało ID: " + idzk_tmp, true);
                    }
                }

            }

        }

        public int setNewZK(int MAGNUM, int NR, string SYMBOL, SposobPlatnosci sposobPlatnosci, long idPlatnikaRaks, string NAZWA_SKROCONA_PLATNIKA, Adres adresDostawy, Adres adresPlatnika, string NR_ZAMOWIENIA_NABYWCY, string SYGNATURA, decimal NETTO, decimal BRUTTO)
        {
            Int32 zk_id = 0;

            FbCommand zk_in_raks = new FbCommand("SELECT ID from GM_ZK where NUMER='" + SYMBOL + "';", fbc.getCurentConnection());
            try
            {
                zk_id = Convert.ToInt32(zk_in_raks.ExecuteScalar());
            }
            catch (FbException exgen)
            {
                logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Bład pobrania ID istniejącego zamówienia lub sprawdzenia czy zamówienie już istnieje: " + exgen.Message, true);
                throw;
            }


            if (zk_id == 0)
            {

                FbTransaction myTransaction = fbc.getCurentConnection().BeginTransaction();
                FbCommand gen_id_zk = new FbCommand("SELECT GEN_ID(GM_ZK_GEN,1) from rdb$database", fbc.getCurentConnection(), myTransaction);
                try
                {
                    zk_id = Convert.ToInt32(gen_id_zk.ExecuteScalar());
                }
                catch (FbException exgen)
                {
                    logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Bład pobrania generatora: " + exgen.Message, true);
                    myTransaction.Rollback();
                    throw;
                }

                FbCommand cdk = new FbCommand();
                //cdk.CommandText = "UPDATE OR INSERT INTO GM_ZK ";
                cdk.CommandText = "INSERT INTO GM_ZK ";
                cdk.CommandText += "(ID,MAGNUM, KOD, ROK, MIESIAC, NR, NUMER, NAZWA_DOKUMENTU,ID_SPOSOBU_PLATNOSCI,NAZWA_SPOSOBU_PLATNOSCI,ID_WALUTY,";
                cdk.CommandText += "ID_PLATNIKA,NAZWA_SKROCONA_PLATNIKA,NAZWA_PELNA_PLATNIKA,ULICA_PLATNIKA,NRDOMU_PLATNIKA,NRLOKALU_PLATNIKA,KOD_PLATNIKA,MIEJSCOWOSC_PLATNIKA,PANSTWO_PLATNIKA,NIP_PLATNIKA,";
                cdk.CommandText += "ID_ODBIORCY,NAZWA_SKROCONA_ODBIORCY,NAZWA_PELNA_ODBIORCY,ULICA_ODBIORCY,NRDOMU_ODBIORCY,NRLOKALU_ODBIORCY,KOD_ODBIORCY,MIEJSCOWOSC_ODBIORCY,PANSTWO_ODBIORCY,NIP_ODBIORCY,";
                cdk.CommandText += "DOSTAWA_ULICA,DOSTAWA_NR_DOMU,DOSTAWA_NR_LOKALU,DOSTAWA_KOD_POCZTOWY,DOSTAWA_MIEJSCOWOSC,DOSTAWA_PANSTWO,";
                cdk.CommandText += "SYGNATURA,OPERATOR,ZMIENIL,NR_ZAMOWIENIA_NABYWCY,";
                cdk.CommandText += "PLN_WARTOSC_NETTO, WAL_WARTOSC_NETTO, WAL_KWOTA_VAT,WAL_WARTOSC_BRUTTO, GUID, DATA_WAZNOSCI_REZERWACJI) ";
                cdk.CommandText += " values (";

                cdk.CommandText += zk_id + ","; //Ustawienie ID dla insert
                cdk.CommandText += MAGNUM + ","; // wypełnia pole not null: MAGNUM
                cdk.CommandText += "  'ZI',"; // wypełnia pole not null: Kod numeracji
                cdk.CommandText += DateTime.Now.Year + ","; // wypełnia pole not null: Rok
                cdk.CommandText += DateTime.Now.Month + ","; // wypełnia pole not null: Miesiąc
                cdk.CommandText += NR + ","; // wypełnia pole not null: Numer zamówienia as int
                cdk.CommandText += "'" + SYMBOL + "',"; // wypełnia pole not null: Symbol zamówienia
                cdk.CommandText += "'Zamówienie internetowe' ,"; // wypełnia pole not null: Nazwa dokumentu
                cdk.CommandText += sposobPlatnosci.id + ","; // wypełnia pole not null: Wyliczone ID sposobu płatnosci 
                //TODO: obsługa tekstu płatnosci
                cdk.CommandText += "'" + sposobPlatnosci.nazwa + "'  ,"; // wypełnia pole not null: Nazwa rodzaju płatnosci
                cdk.CommandText += "0,"; // ID Waluty PLN to 0

                //Płatnik
                cdk.CommandText += idPlatnikaRaks.ToString() + ","; //ID płatnika - Sprzedaż Detaliczna
                cdk.CommandText += "'" + (NAZWA_SKROCONA_PLATNIKA.Length > 49 ? NAZWA_SKROCONA_PLATNIKA.Substring(0, 49) : NAZWA_SKROCONA_PLATNIKA) + "',"; // wypełnia pole not null: Nazwa skrócona płatnika
                cdk.CommandText += "'" + (adresPlatnika.osKontakt.Length > 249 ? adresPlatnika.osKontakt.Substring(0, 249) : adresPlatnika.osKontakt) + "',"; // wypełnia pole not null: Nazwa pełna

                cdk.CommandText += "'" + (adresPlatnika.ulica.Length > 49 ? adresPlatnika.ulica.Substring(0, 49) : adresPlatnika.ulica) + "',"; // wypełnia pole: Ulica płatnika
                cdk.CommandText += "'" + (adresPlatnika.nrBud.Length > 9 ? adresPlatnika.nrBud.Substring(0, 9) : adresPlatnika.nrBud) + "',"; // wypełnia pole: Numer domu
                cdk.CommandText += "'" + (adresPlatnika.nrLok.Length > 9 ? adresPlatnika.nrLok.Substring(0, 9) : adresPlatnika.nrLok) + "'  ,"; // wypełnia pole: numer lokalu
                cdk.CommandText += "'" + (adresPlatnika.kod.Length > 9 ? adresPlatnika.kod.Substring(0, 9) : adresPlatnika.kod) + "',"; // wypełnia pole: kod pocztowy
                cdk.CommandText += "'" + (adresPlatnika.poczta.Length > 49 ? adresPlatnika.poczta.Substring(0, 49) : adresPlatnika.poczta) + "',"; // wypełnia pole: miejscowość lub miasto
                cdk.CommandText += "'" + (adresPlatnika.panstwo.Length > 49 ? adresPlatnika.panstwo.Substring(0, 49) : adresPlatnika.panstwo) + "',"; // wypełnia pole: Państwo
                cdk.CommandText += "'" + (adresPlatnika.nip.Length > 25 ? adresPlatnika.nip.Substring(0, 25) : adresPlatnika.nip) + "',"; // wypełnia pole: NIP płatnika

                //Odbiorca
                cdk.CommandText += idPlatnikaRaks.ToString() + ","; //ID płatnika - Sprzedaż Detaliczna
                cdk.CommandText += "'" + (NAZWA_SKROCONA_PLATNIKA.Length > 49 ? NAZWA_SKROCONA_PLATNIKA.Substring(0, 49) : NAZWA_SKROCONA_PLATNIKA) + "',"; // wypełnia pole not null: NAZWA_SKROCONA_ODBIORCY z danych o płatniku
                cdk.CommandText += "'" + (adresDostawy.osKontakt.Length > 249 ? adresDostawy.osKontakt.Substring(0, 249) : adresDostawy.osKontakt) + "',"; // wypełnia pole not null: NAZWA_PELNA_ODBIORCY  z danych o płatniku

                cdk.CommandText += "'" + (adresDostawy.ulica.Length > 49 ? adresDostawy.ulica.Substring(0, 49) : adresDostawy.ulica) + "',"; // wypełnia pole: Ulica dostawy
                cdk.CommandText += "'" + (adresDostawy.nrBud.Length > 9 ? adresDostawy.nrBud.Substring(0, 9) : adresDostawy.nrBud) + "',"; // wypełnia pole: Numer domu
                cdk.CommandText += "'" + (adresDostawy.nrLok.Length > 9 ? adresDostawy.nrLok.Substring(0, 9) : adresDostawy.nrLok) + "'  ,"; // wypełnia pole: numer lokalu
                cdk.CommandText += "'" + (adresDostawy.kod.Length > 9 ? adresDostawy.kod.Substring(0, 9) : adresDostawy.kod) + "',"; // wypełnia pole: kod pocztowy
                cdk.CommandText += "'" + (adresDostawy.poczta.Length > 49 ? adresDostawy.poczta.Substring(0, 49) : adresDostawy.poczta) + "',"; // wypełnia pole: miejscowość lub miasto
                cdk.CommandText += "'" + (adresDostawy.panstwo.Length > 49 ? adresDostawy.panstwo.Substring(0, 49) : adresDostawy.panstwo) + "',"; // wypełnia pole: Państwo
                cdk.CommandText += "'" + (adresDostawy.nip.Length > 25 ? adresDostawy.nip.Substring(0, 25) : adresDostawy.nip) + "',"; // wypełnia pole: NIP dostawy

                //Dane dostawy
                cdk.CommandText += "'" + (adresDostawy.ulica.Length > 49 ? adresDostawy.ulica.Substring(0, 49) : adresDostawy.ulica) + "',"; // wypełnia pole: Ulica dostawy
                cdk.CommandText += "'" + (adresDostawy.nrBud.Length > 9 ? adresDostawy.nrBud.Substring(0, 9) : adresDostawy.nrBud) + "',"; // wypełnia pole: Numer domu
                cdk.CommandText += "'" + (adresDostawy.nrLok.Length > 9 ? adresDostawy.nrLok.Substring(0, 9) : adresDostawy.nrLok) + "'  ,"; // wypełnia pole: numer lokalu
                cdk.CommandText += "'" + (adresDostawy.kod.Length > 9 ? adresDostawy.kod.Substring(0, 9) : adresDostawy.kod) + "',"; // wypełnia pole: kod pocztowy
                cdk.CommandText += "'" + (adresDostawy.poczta.Length > 49 ? adresDostawy.poczta.Substring(0, 49) : adresDostawy.poczta) + "',"; // wypełnia pole: miejscowość lub miasto
                cdk.CommandText += "'" + (adresDostawy.panstwo.Length > 49 ? adresDostawy.panstwo.Substring(0, 49) : adresDostawy.panstwo) + "',"; // wypełnia pole: Państwo

                cdk.CommandText += "'" + (SYGNATURA.Length > 99 ? SYGNATURA.Substring(0, 99) : SYGNATURA) + "',"; // wypełnia pole: SYGNATURA sposobem dostawy
                cdk.CommandText += "'WWW',"; // wypełnia pole: Operator wartośc stała
                cdk.CommandText += "'WWW',"; // wypełnia pole: Zmienił wartośc stała
                cdk.CommandText += "'" + (NR_ZAMOWIENIA_NABYWCY.Length > 19 ? NR_ZAMOWIENIA_NABYWCY.Substring(0, 19) : NR_ZAMOWIENIA_NABYWCY) + "',"; // wypełnia pole not null: >>>>>>>>>>>>> Ref ID zamówienia ze sklepu internetowego

                cdk.CommandText += NETTO.ToString().Replace(",", ".") + "," + NETTO.ToString().Replace(",", ".") + "," + (BRUTTO - NETTO).ToString().Replace(",", ".") + "," + BRUTTO.ToString().Replace(",", ".") + ",";
                cdk.CommandText += "'" + Guid.NewGuid() + "',";
                cdk.CommandText += "'" + DateTime.Now.AddDays(3).Year + "-" + DateTime.Now.AddDays(3).Month + "-" + DateTime.Now.AddDays(3).Day + "' "; //DATA_WAZNOSCI_REZERWACJI DATA_D NOT NULL,

                cdk.CommandText += " ) ";
                //cdk.CommandText += " matching (NR_ZAMOWIENIA_NABYWCY) ";
                cdk.CommandText += " returning ID;";

                try
                {
                    cdk.Connection = fbc.getCurentConnection();
                    cdk.Transaction = myTransaction;

                    //Zapis nagłówka zamówienia do bazy
                    zk_id = (int)cdk.ExecuteScalar();
                    logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Zapis nagłówka zamówinienia do bazy: " + SYMBOL + " ," + NAZWA_SKROCONA_PLATNIKA, true);
                    myTransaction.Commit();
                    return zk_id;
                }
                catch (FbException ex)
                {
                    myTransaction.Rollback();
                    logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "bBłąd wczytywania listy magazynów do słownika parametrów: " + ex.Message);
                    return 0;
                }
            }
            else
            {
                logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Zamówienie " + SYMBOL + " już istnieje i zostało pominiete w zapisie do RaksSQL");
                return -1;
            }
        }

        private void setAddNewPozZK(FbTransaction trans, Int32 ID_GLOWKI, int lp, string KOD_TOWARU, string NAZWA_TOWARU, decimal ILOSC, decimal CENA_NETTO, decimal CENA_BRUTTO)
        {

            Int32 zkpoz_id = 0;
            FbCommand gen_id_zkpoz = new FbCommand("SELECT GEN_ID(GM_ZKPOZ_GEN,1) from rdb$database", fbc.getCurentConnection(), trans);
            try
            {
                zkpoz_id = Convert.ToInt32(gen_id_zkpoz.ExecuteScalar());
            }
            catch (FbException exgen)
            {
                logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Bład pobrania generatora dla pozycji: " + exgen.Message, true);
                trans.Rollback();
                throw;
            }
            
            FbCommand cdk = new FbCommand();
            cdk.CommandText = "INSERT INTO GM_ZKPOZ ";
            cdk.CommandText += "(ID,ID_GLOWKI, ID_TOWARU, ILOSC_PO, ILOSC_ALT_PO, ";
            cdk.CommandText += "DATA_WAZNOSCI_REZERWACJI, CENA_KATALOGOWA, CENA_SPRZEDAZY, CENA_SP_WAL_NETTO,ID_STAWKI,STAWKA,CENA_SP_WAL_BRUTTO,";
            cdk.CommandText += "CENA_SP_PLN_NETTO,KURS_WAL_CENY_KAT,ID_WAL_CENY_KAT,SKROT_ORYGINALNY,NAZWA_ORYGINALNA,SKROT_ALTERNATYWNY,NAZWA_ALTERNATYWNA,LP,";
            cdk.CommandText += "CENA_SP_WAL_NETTO_ALT, CENA_SP_WAL_BRUTTO_ALT, CENA_SP_PLN_NETTO_ALT, CENA_SP_PLN_BRUTTO, CENA_SP_PLN_BRUTTO_ALT, GUID";
            cdk.CommandText += ")";
            cdk.CommandText += " values (";

            cdk.CommandText += zkpoz_id + ","; //ID ID_D DEFAULT 0 NOT NULL,
            cdk.CommandText += ID_GLOWKI + ","; //ID_GLOWKI ID_D DEFAULT 0 NOT NULL,
            
            //wyliczene kodu towaru
            cdk.CommandText += getTowarByKod(KOD_TOWARU, NAZWA_TOWARU, trans) + " ,"; //ID_TOWARU ID_D DEFAULT 0 NOT NULL,

            cdk.CommandText += ILOSC.ToString().Replace(",", ".") + " ,"; //ILOSC_PO ILOSC_D DEFAULT 0 NOT NULL,
            cdk.CommandText += ILOSC.ToString().Replace(",", ".") + " ,"; // ILOSC_ALT_PO
            cdk.CommandText += "'" + DateTime.Now.AddDays(3).Year + "-" + DateTime.Now.AddDays(3).Month + "-" + DateTime.Now.AddDays(3).Day + "' ,"; //DATA_WAZNOSCI_REZERWACJI DATA_D NOT NULL,
            cdk.CommandText += CENA_NETTO.ToString().Replace(",",".") + " ,"; //CENA_KATALOGOWA CENA_D DEFAULT 0 NOT NULL,
            cdk.CommandText += CENA_NETTO.ToString().Replace(",", ".") + " ,"; //CENA_SPRZEDAZY CENA_D DEFAULT 0,
            cdk.CommandText += CENA_NETTO.ToString().Replace(",", ".") + " ,"; //CENA_SP_WAL_NETTO CENA_D DEFAULT 0 NOT NULL,
            cdk.CommandText += "12 ,"; //ID_STAWKI ID_D DEFAULT 0 NOT NULL,
            cdk.CommandText += "23.00 ,"; //STAWKA PROCENT_D DEFAULT 0 NOT NULL,
            cdk.CommandText += CENA_BRUTTO.ToString().Replace(",", ".") + " ,"; //CENA_SP_WAL_BRUTTO CENA_D DEFAULT 0 NOT NULL,
            cdk.CommandText += CENA_NETTO.ToString().Replace(",", ".") + " ,"; //CENA_SP_PLN_NETTO CENA_D DEFAULT 0 NOT NULL,
            cdk.CommandText += " 1,"; //KURS_WAL_CENY_KAT Numeric(15,8) DEFAULT 0,
            cdk.CommandText += " 0,"; //ID_WAL_CENY_KAT ID_D DEFAULT 0 NOT NULL,
            cdk.CommandText += " '" + (KOD_TOWARU.Length > 24 ? KOD_TOWARU.Substring(0, 24) : KOD_TOWARU) + "',"; //SKROT_ORYGINALNY STRING25_D NOT NULL,
            cdk.CommandText += " '" + (NAZWA_TOWARU.Length > 249 ? NAZWA_TOWARU.Substring(0, 249) : NAZWA_TOWARU) + "',"; //NAZWA_ORYGINALNA STRING250_D NOT NULL,
            cdk.CommandText += " '" + (KOD_TOWARU.Length > 24 ? KOD_TOWARU.Substring(0, 24) : KOD_TOWARU) + "',"; //SKROT_ALTERNATYWNY STRING25_D NOT NULL,
            cdk.CommandText += " '" + (NAZWA_TOWARU.Length > 249 ? NAZWA_TOWARU.Substring(0, 249) : NAZWA_TOWARU) + "',"; //NAZWA_ALTERNATYWNA STRING250_D NOT NULL,
            cdk.CommandText += lp + ","; //LP Integer,

            cdk.CommandText += CENA_NETTO.ToString().Replace(",", ".") + " ,"; //CENA_SP_WAL_NETTO_ALT
            cdk.CommandText += CENA_BRUTTO.ToString().Replace(",", ".") + " ,"; //CENA_SP_WAL_BRUTTO_ALT
            cdk.CommandText += CENA_NETTO.ToString().Replace(",", ".") + " ,"; //CENA_SP_PLN_NETTO_ALT
            cdk.CommandText += CENA_BRUTTO.ToString().Replace(",", ".") + " ,"; //CENA_SP_PLN_BRUTTO
            cdk.CommandText += CENA_BRUTTO.ToString().Replace(",", ".") + ", "; //CENA_SP_PLN_BRUTTO_ALT
            cdk.CommandText += "'" + Guid.NewGuid() + "'";
            
            
            cdk.CommandText += " ) ";
            
            try
            {
                cdk.Connection = fbc.getCurentConnection();
                cdk.Transaction = trans;
                cdk.ExecuteScalar();
                //cdk.Transaction.Commit();
                logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Do zamówienia ID " + ID_GLOWKI + " dodano pozycję : " + KOD_TOWARU, true);
            }
            catch (Exception ex)
            {
                logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Błąd przy dodawaniu pozycji " + KOD_TOWARU + " do zamówienia ID " + ID_GLOWKI + " ,ROLLBACK: " + ex.Message, true);
                trans.Rollback();
                throw;
            }
        }

        private int getTowarByKod(string KOD_TOWARU, string NAZWA, FbTransaction trans)
        {
            FbCommand getTow = new FbCommand("SELECT ID_TOWARU FROM GM_TOWARY Where SKROT = '" + KOD_TOWARU + "';", fbc.getCurentConnection(),trans);
            try
            {
                FbDataReader fdk = getTow.ExecuteReader();
                if (fdk.Read())
                {
                    return (int)fdk["ID_TOWARU"];
                }
                else
                {
                    //return setNewTowar(KOD_TOWARU, NAZWA);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Błąd wyszukania towaru" + KOD_TOWARU + " : " + ex.Message, true);
                return 0;
            }
        }

        private int getTowarByKodWithOutTrans(string KOD_TOWARU, string NAZWA)
        {
            FbCommand getTow = new FbCommand("SELECT ID_TOWARU FROM GM_TOWARY Where SKROT = '" + KOD_TOWARU + "';", fbc.getCurentConnection());
            try
            {
                FbDataReader fdk = getTow.ExecuteReader();
                if (fdk.Read())
                {
                    return (int)fdk["ID_TOWARU"];
                }
                else
                {
                    return setNewTowar(KOD_TOWARU, NAZWA);
                }
            }
            catch (Exception ex)
            {
                logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Błąd wyszukania towaru" + KOD_TOWARU + " : " + ex.Message, true);
                return 0;
            }
        }

        public int setNewTowar(string SKROT, string NAZWA)
        {
            Int32 towar_id = 0;
            FbCommand gen_id_towar = new FbCommand("SELECT GEN_ID(GM_TOWARY_GEN,1) from rdb$database", fbc.getCurentConnection());
            try
            {
                towar_id = Convert.ToInt32(gen_id_towar.ExecuteScalar());
            }
            catch (FbException exgen)
            {
                logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Bład pobrania generatora towaru: " + exgen.Message, true);
                throw;
            }

            FbCommand cdk = new FbCommand();
            cdk.CommandText = "INSERT INTO GM_TOWARY ";
            cdk.CommandText += "(ID,ID_TOWARU, ARCHIWALNY, TYP, SKROT,";
            cdk.CommandText += "NAZWA, STAWKAVAT, JEDNOSTKA,PRZELICZNIK_CN,BEZ_MASY, OPERATOR, ZMIENIL, GUID";
            cdk.CommandText += ")";
            cdk.CommandText += " values (";
            cdk.CommandText += towar_id + ","; //ID
            cdk.CommandText += towar_id + ","; //ID_TOWARU
            cdk.CommandText += "0,"; //ARCHIWALNY
            cdk.CommandText += "'Towar',"; //Typ
            cdk.CommandText += "'" + (SKROT.Length > 24 ? SKROT.Substring(0, 24) : SKROT) + "',"; // SKROT
            cdk.CommandText += "'" + (NAZWA.Length > 249 ? NAZWA.Substring(0, 249) : NAZWA) + "',"; // Nazwa
            cdk.CommandText += "12,"; //Stawka VAT 23%
            cdk.CommandText += "1,"; //Jedn. miary. szt
            cdk.CommandText += "0.00,"; //przelicznik CN
            cdk.CommandText += "0,"; //BEZ_MASY

            cdk.CommandText += "'WWW',"; // wypełnia pole: Operator wartośc stała
            cdk.CommandText += "'WWW',"; // wypełnia pole: Zmienił wartośc stała
            cdk.CommandText += "'" + Guid.NewGuid() + "'";
            cdk.CommandText += ");";
            try
            {
                cdk.Connection = fbc.getCurentConnection();
                cdk.ExecuteScalar();
                return towar_id;
            }
            catch (Exception ext)
            {
                logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Bład zapisu towaru:" + ext.Message, true);
                return 0;
                throw;
            }
        }

        public SposobPlatnosci getSposobPlatnosci(string SposobWSklepie)
        {
            FbCommand sp = new FbCommand("SELECT ID,NAZWA from GM_SPOSOBY_ZAP where NAZWA='" + SposobWSklepie +"';", fbc.getCurentConnection());
            try
            {
                FbDataReader fdk = sp.ExecuteReader();
                if (fdk.Read())
                {
                    SposobPlatnosci spRaks = new SposobPlatnosci((int)fdk["ID"], (string)fdk["NAZWA"]);
                    return spRaks;
                }
                else
                {
                    sp.CommandText = "SELECT ID,NAZWA from GM_SPOSOBY_ZAP where NAZWA='Pobranie';";
                    fdk = sp.ExecuteReader();
                    if (fdk.Read())
                    {
                        SposobPlatnosci spRaks = new SposobPlatnosci((int)fdk["ID"], (string)fdk["NAZWA"]);
                        return spRaks;
                    }
                    else
                    {
                        SposobPlatnosci spRaks = new SposobPlatnosci(1, "Gotówka");
                        return spRaks;
                    }
                }
            }
            catch (FbException exgen)
            {
                logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Bład pobrania sposobu płatności: " + exgen.Message, true);
                SposobPlatnosci spRaks = new SposobPlatnosci(1, "Gotówka");
                return spRaks;    
            }

        }

        public long setKontrahentInToRaks(string kodPresta, string pelnaNazwa, string nip, string kodEU, string kodWal, string ulica, string nrBudynku, string miejscowosc, string kraj, string kodPoczta, string poczta, string email)
        {
            kodPresta = (kodPresta.Length > 49 ? kodPresta.Substring(0, 49) : kodPresta);
            Int32 kh_id = 0;

            FbCommand sp = new FbCommand("SELECT ID from R3_CONTACTS where SHORT_NAME='" + kodPresta + "';", fbc.getCurentConnection());
            try
            {
                FbDataReader fdk = sp.ExecuteReader();
                if (fdk.Read())
                {
                    kh_id = (int)fdk["ID"];
                }
                fdk.Close();
            }
            catch (FbException exgen)
            {
                logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Bład odnajdywania lub zakładania kontrahenta w RaksSQL: " + exgen.Message, true);
                kh_id = 1;
            };

            if (kh_id == 0)
            {
                FbCommand gen_id_kh = new FbCommand("SELECT GEN_ID(R3_CONTACTS_ID_GEN,1) from rdb$database", fbc.getCurentConnection());
                try
                {
                    kh_id = Convert.ToInt32(gen_id_kh.ExecuteScalar());
                }
                catch (FbException exgen)
                {
                    logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Bład pobrania generatora kontrahenta w Raks: " + exgen.Message, true);
                    throw;
                }

                string sql = "insert into R3_CONTACTS (ID,SHORT_NAME, FULL_NAME, TAXID, EU_CODE, CURRENCY_CODE, STREET, BUILDING_NUMBER, PLACE, COUNTRY, ZIPCODE, POCZTA, C_IDENT, M_IDENT, C_DATE, PURCHASER, GUID) ";
                sql += "values (" + kh_id + ", ";
                sql += "'" + kodPresta + "', ";
                sql += "'" + (pelnaNazwa.Length > 199 ? pelnaNazwa.Substring(0, 199) : pelnaNazwa) + "', ";
                sql += "'" + (nip.Length > 24 ? nip.Substring(0, 24) : nip) + "', ";
                sql += "'" + (kodEU.Length > 2 ? kodEU.Substring(0, 2) : kodEU) + "', ";
                sql += "'" + (kodWal.Length > 3 ? kodWal.Substring(0, 3) : kodWal) + "', ";
                sql += "'" + (ulica.Length > 39 ? ulica.Substring(0, 39) : ulica) + "', ";
                sql += "'" + (nrBudynku.Length > 9 ? nrBudynku.Substring(0, 9) : nrBudynku) + "', ";
                sql += "'" + (miejscowosc.Length > 39 ? miejscowosc.Substring(0, 39) : miejscowosc) + "', ";
                sql += "'" + (kraj.Length > 39 ? kraj.Substring(0, 39) : kraj) + "', ";
                sql += "'" + (kodPoczta.Length > 9 ? kodPoczta.Substring(0, 9) : kodPoczta) + "', ";
                sql += "'" + (poczta.Length > 39 ? poczta.Substring(0, 39) : poczta) + "', ";
                sql += "'WWW', ";
                sql += "'WWW', ";
                sql += "'" + DateTime.Now.ToString("dd.MM.yyyy, HH.mm.ss.fff") + "', ";
                sql += "1, ";
                sql += "'" + Guid.NewGuid() + "') ";

                FbCommand new_kh = new FbCommand(sql, fbc.getCurentConnection());
                try
                {
                    new_kh.ExecuteScalar();
                    logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Dodano nowego kontrahenta w RaksSQL: " + kodPresta + " ," + pelnaNazwa, true);
                }
                catch (FbException exgen)
                {
                    logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Bład pobrania generatora kontrahenta w Raks: " + exgen.Message, true);
                    throw;
                }
            }
            else
            {
                logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Znaleziono kontrahenta w RaksSQL: " + kodPresta + " ," + pelnaNazwa, true);
            }

            return (long)kh_id;
        }

        public string getResultMSGofStany()
        {
            return resultMSGofStany;
        }

        public void aktualizujStanyWPresta(bool trybGUI)
        {
            bool czykontynulowac = true;
            
            SortedList<string, StanTowaruWRaks> stanyInRaks = new SortedList<string, StanTowaruWRaks>();

            StockAvailableFactory sotckAvailableFactory = new StockAvailableFactory(BaseUrl, Account, Password);
            List<stock_available> sotckAvailables = sotckAvailableFactory.GetAll();
            ProductFactory productFactory = new ProductFactory(BaseUrl, Account, Password);
            List<Bukimedia.PrestaSharp.Entities.AuxEntities.language> valueLang = new List<Bukimedia.PrestaSharp.Entities.AuxEntities.language>();

            logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Rozpoczęcie przepisywania stanów z Presta do kolekcji", true);

            Int32 lp = 0;
            Int32 counter = 0;
            StringBuilder listaIndeksow = new StringBuilder();

            try
            {
                foreach (stock_available item in sotckAvailables)
                {
                    product oneProduct = productFactory.Get((long)item.id_product);
                    string indeks = oneProduct.reference;
                    counter++;

                    //if (!indeks.Contains("SNK050K1"))
                    //{
                    //    //ograniczenie dla testów
                    //    Console.WriteLine("Pominięto: " + indeks + " Lp.: " + counter);
                    //    continue;
                    //}

                    int stan = item.quantity;
                    long product_id = (long)item.id_product;
                    if (stanyInRaks.ContainsKey(indeks))
                    {
                        Console.WriteLine("Dubel kod: " + indeks + " Stan: " + stan);
                        logg.setUstawienieLoga(Logg.RodzajLogowania.Warning, Logg.MediumLoga.File, "W Presta jest dubel indeksu towaru: " + indeks, true);
                    }
                    else
                    {
                        if (listaIndeksow.Length > 0)
                        {
                            listaIndeksow.Append(",'").Append(indeks).Append("'");
                        }
                        else
                        {
                            listaIndeksow.Append("'").Append(indeks).Append("'");
                        }

                        stanyInRaks.Add(indeks, new StanTowaruWRaks(product_id, indeks, stan));
                        Console.WriteLine("lp:" + lp + " kod: " + indeks + " Stan: " + stan + " product_id: " + product_id.ToString());
                        logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, ">>>>;lp:;" + lp + ";kod:;" + indeks + ";Stan: " + stan + " product_id: " + product_id.ToString(), true);
                    }
                    lp++;
                }
                logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Z Presta odczytano: " + lp + " kodów towarów, oraz ilość kodów w tmp to : " + stanyInRaks.Count , true);
            }
            catch (Exception exp)
            {
                logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "BŁĄD przepisywania stanów z Presta do kolekcji: " + exp.Message, true);
                czykontynulowac = false;
                throw;
            }

            logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Zakończono przepisywania stanów z Presta do kolekcji", true);


            if (czykontynulowac)
            {
                string sql = getPrepareSQLStatmentForSock(listaIndeksow.ToString());
                string currIndex;
                decimal currStock;
                int licznik = 0;

                //Blok tetowy do wykrycia błędu
                //FbCommand stanymag_test = new FbCommand(sql, fbc.getCurentConnection());
                //try
                //{
                //    FbDataReader fdk = stanymag_test.ExecuteReader();
                //    while (fdk.Read())
                //    {
                //        licznik++;
                //        logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "INFO;TEST;" + licznik + ";INFO o stanach z Raks;Indeks;" + (string)fdk["SKROT"] + ";Stan;" + (decimal)fdk["STAN"]);
                //    }
                //    fdk.Close();
                //}
                //catch (Exception test_ex)
                //{
                //    logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "BŁĄD;" + licznik +";Wykonanie testowego zapytania do Raks;" + test_ex.Message);
                //}
                //licznik = 0;

                FbCommand stanymag = new FbCommand(sql, fbc.getCurentConnection());
                try
                {
                    FbDataReader fdk = stanymag.ExecuteReader();
                    while (fdk.Read())
                    {
                        currIndex = (string)fdk["SKROT"];
                        currStock = (decimal)fdk["STAN"];
                        licznik++;

                        //if (!currIndex.Contains("SNK050K1"))
                        //{
                        //    //ograniczenie dla testów
                        //    Console.WriteLine("Pominięto oblicznie stanów w Raks: " + currIndex);
                        //    continue;
                        //}

                        //logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Kontrola czy w Presta jest stan dla indeksu:;" + currIndex);
                        
                        if (!stanyInRaks.ContainsKey(currIndex))
                        {
                            logg.setUstawienieLoga(Logg.RodzajLogowania.Warning, Logg.MediumLoga.File, ">>>>>>>> DO POPRAWY W PRESTA >>>>>>;Nie odnaleziono w stanach odczytanych z Presty indeksu:;" + currIndex );
                            continue;
                        }

                        StanTowaruWRaks stanTowaru = stanyInRaks[currIndex];

                        StockAvailableFactory sotckAvailableFactorySet = new StockAvailableFactory(BaseUrl, Account, Password);
                        Dictionary<string, string> dtnSet = new Dictionary<string, string>();
                        dtnSet.Add("id_product", stanTowaru.idPresta.ToString());
                        List<stock_available> sotckAvailablesSet = sotckAvailableFactorySet.GetByFilter(dtnSet, null, null);

                        bool ustwionoWPresta = false;
                        foreach (stock_available item in sotckAvailablesSet)
                        {
                            if (item.quantity != (int)currStock)
                            {
                                logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, ";" + licznik +";Ustawinie dla indeksu;" + currIndex + "; starta ilość;" + item.quantity.ToString() + "; nowa ilość;" + currStock);
                                Console.Write(currIndex + " Stan przed: " + item.quantity);
                                item.quantity = (int)currStock;
                                Console.WriteLine(currIndex + "  Stan po: " + item.quantity);
                                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                try
                                {
                                    sotckAvailableFactorySet.Update(item);
                                }
                                catch (Exception e1)
                                {
                                    logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "BŁĄD;" + licznik +";Ustawinie dla indeksu;" + currIndex + ";" + e1.Message);
                                }
                            }
                            else
                            {
                                logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "STAN;" + licznik + ";Stan dla indeksu;" + currIndex + "; bez zmian ilość;" + currStock);
                            }

                            //Usuwanie z listy
                            try
                            {
                                //logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Usuwanie z listy;" + currIndex); 
                                stanyInRaks.Remove(currIndex);
                            }
                            catch (Exception e2)
                            {
                                logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "BŁĄD;Usuwanie z listy indeksu;" + currIndex + ";" + e2.Message);
                            }
                            ustwionoWPresta = true;
                            break;
                        }

                        //logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Zmiana trybu na towary, których nie ma w Presta");

                        //brak towaru w sklepie Presta
                        if (!ustwionoWPresta)
                        {
                            FbCommand ustawBrakWPresta = new FbCommand("UPDATE GM_TOWARY SET PKWIU='Brak kodu w Presta' where SKROT='" + currIndex + "';", fbc.getCurentConnection());
                            try
                            {
                                ustawBrakWPresta.ExecuteScalar();
                                stanyInRaks.Remove(currIndex);
                                logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Brak w sklepie Presta indeksu;" + currIndex + "; Ustwiono znaczniku w polus PKWIU");
                            }
                            catch (Exception fuex)
                            {
                                logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Błąd przy ustawianiu w Raks dla indeksu;" + currIndex + " znacznika w polus PKWIU;" + fuex.Message);
                                throw;
                            }
                        }
                    }

                    logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, "Czyszczenie indeksów, których nie ma na stanie w Raks >> jest do przetworzenia: " + stanyInRaks.Count, true);
                    //Czyszczenie indexów, których nie ma na stanie
                    int coount = 0;
                    licznik = 0;
                    while (stanyInRaks.Count>0)
                    {
                        StanTowaruWRaks st = (StanTowaruWRaks)stanyInRaks.Values[0];
                        coount++;
                        licznik++;
                        
                        StockAvailableFactory sotckAvailableFactorySet = new StockAvailableFactory(BaseUrl, Account, Password);
                        Dictionary<string, string> dtnSet = new Dictionary<string, string>();
                        dtnSet.Add("id_product", st.idPresta.ToString());
                        List<stock_available> sotckAvailablesSet = sotckAvailableFactorySet.GetByFilter(dtnSet, null, null);

                        foreach (stock_available item in sotckAvailablesSet)
                        {
                            logg.setUstawienieLoga(Logg.RodzajLogowania.Info, Logg.MediumLoga.File, ";" + licznik + ";Zerowanie stanu dla indeksu: " + st.kodRaks + "; starta ilość: " + item.quantity.ToString() + " Lp. " + coount);
                            Console.WriteLine("Zerowanie kod: " + st.kodRaks + " stan przed: " + item.quantity + " counter: " + coount);
                            item.quantity = (int)0;
                            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                            sotckAvailableFactorySet.Update(item);
                        }
                        stanyInRaks.RemoveAt(0);
                    }
 
                }
                catch (FbException exgen)
                {
                    logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Bład błąd wyliczania stanów magazynowych w RaksSQL (cron): " + exgen.Message, true);
                    throw;
                }
            }
        }

        private string getPrepareSQLStatmentForSock(string listaIndeksow)
        {
            string magazyny = FBConn.GetKeyFromRegistry("stanymagazynow");
            string sql = "";
            //if (listaIndeksow.Count() < 1500)
            //{
            //    sql = "SELECT GM_TOWARY.SKROT, (sum(GM_MAGAZYN.ILOSC) - sum(GM_MAGAZYN.ILOSC_ZAREZERWOWANA)) as STAN ";
            //    sql += " FROM GM_MAGAZYN";
            //    sql += " join GM_TOWARY on GM_MAGAZYN.ID_TOWAR=GM_TOWARY.ID_TOWARU";
            //    sql += " join GM_MAGAZYNY on GM_MAGAZYN.MAGNUM=GM_MAGAZYNY.ID";
            //    sql += " where GM_TOWARY.SKROT in (" + listaIndeksow + ")";
            //    sql += " and GM_MAGAZYNY.NUMER in (" + magazyny + ")";
            //    sql += " group by GM_TOWARY.SKROT;";
            //}
            //else if (listaIndeksow.Count() >= 1500 && listaIndeksow.Count() < 3000)
            //{

                sql = "SELECT GM_TOWARY.SKROT, (sum(GM_MAGAZYN.ILOSC) - sum(GM_MAGAZYN.ILOSC_ZAREZERWOWANA)) as STAN ";
                sql += " FROM GM_MAGAZYN";
                sql += " join GM_TOWARY on GM_MAGAZYN.ID_TOWAR=GM_TOWARY.ID_TOWARU";
                sql += " join GM_MAGAZYNY on GM_MAGAZYN.MAGNUM=GM_MAGAZYNY.ID";
                sql += " where GM_TOWARY.SKROT in (" + set1499elements(listaIndeksow,0,1499) + ")";
                sql += " and GM_MAGAZYNY.NUMER in (" + magazyny + ")";
                sql += " group by GM_TOWARY.SKROT";

                sql += " union all ";

                sql += "SELECT GM_TOWARY.SKROT, (sum(GM_MAGAZYN.ILOSC) - sum(GM_MAGAZYN.ILOSC_ZAREZERWOWANA)) as STAN ";
                sql += " FROM GM_MAGAZYN";
                sql += " join GM_TOWARY on GM_MAGAZYN.ID_TOWAR=GM_TOWARY.ID_TOWARU";
                sql += " join GM_MAGAZYNY on GM_MAGAZYN.MAGNUM=GM_MAGAZYNY.ID";
                sql += " where GM_TOWARY.SKROT in (" + set1499elements(listaIndeksow, 1500, 2999) + ")";
                sql += " and GM_MAGAZYNY.NUMER in (" + magazyny + ")";
                sql += " group by GM_TOWARY.SKROT;";
            //}
            return sql;
        }

        private StringBuilder set1499elements(string lista, int odElementu, int doElementu)
        {
            StringBuilder listaWynik = new StringBuilder();
            int licznik = 0;
            int size = lista.Length;

            for (int i = 0; licznik < doElementu; i++)
            {
                if (i == size)
                {
                    licznik = doElementu;
                }
                else if (lista.ElementAt(i) == Convert.ToChar(","))
                {
                    licznik++;
                }
                
                if (licznik >= odElementu && licznik < doElementu)
                {
                    if (listaWynik.Length == 0 && lista.ElementAt(i) == Convert.ToChar(","))
                    {
                        //nie rób nic bo to poczatek drugiej listy
                    }else
                    {
                        listaWynik.Append(lista.ElementAt(i));
                    }
                }
                
            }

            return listaWynik;
        }

        public void getCalculateStocInRaks(string listaIndeksow)
        {
            string sql = getPrepareSQLStatmentForSock(listaIndeksow);

            FbCommand stanymag = new FbCommand(sql, fbc.getCurentConnection());
            try
            {
                FbDataReader fdk = stanymag.ExecuteReader();
                    while (fdk.Read())
                    {
                        resultMSGofStany += (string)fdk["SKROT"] + "; " + (decimal)fdk["STAN"] + Environment.NewLine;

                    }
            }
            catch (FbException exgen)
            {
                resultMSGofStany = "Bład!: " + exgen.Message;
                logg.setUstawienieLoga(Logg.RodzajLogowania.Error, Logg.MediumLoga.File, "Bład błąd wyliczania stanów magazynowych w RaksSQL (GUI): " + exgen.Message, true);
                throw;
            }
        }
    }
}
