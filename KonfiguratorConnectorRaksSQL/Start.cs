using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Logowanie;
using Bukimedia.PrestaSharp;
using RestSharp;
using Bukimedia.PrestaSharp.Factories;
using Bukimedia.PrestaSharp.Entities;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Win32;
/*
 * C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\Profile\Client\System.ComponentModel.DataAnnotations.dll
 * C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\Profile\Client\System.Data.Entity.dll
 * C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\Profile\Client\System.Runtime.Serialization.dll
 * C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\Profile\Client\System.Security.dll
*/
//using LinqToDB.DataProvider.Firebird;
//using LinqToDB.Data;
//using LinqToDB.Mapping;

namespace KonfiguratorConnectorRaksSQL
{
    public partial class Start : Form
    {
        FBConn fbconn;
        string BaseUrl = "http://adresurlsklepupresta.pl/api/";
        string Account = "";
        string Password = "kodwygenerowanyprzezPrestaAPI";
        const string RegistryKey = "SOFTWARE\\Infido\\KonektorSQL";
        
        public Start()
        {
            InitializeComponent();
            this.BaseUrl = FBConn.GetKeyFromRegistry("http");
            this.Account = FBConn.GetKeyFromRegistry("userHttp");
            this.Password = FBConn.GetKeyFromRegistry("key");

            if (System.Environment.MachineName.Equals("WINVBOX"))
            {
                b123.Visible = true;
                bTestConnectionM123.Visible = true;
            }
            else
            {
                b123.Visible = false;
                bTestConnectionM123.Visible = false;
            }
        }

        private void bSave_Click(object sender, EventArgs e)
        {
            fbconn = new FBConn(tLogin.Text, tPass.Text, tPath.Text, tIP.Text, tKey.Text);
            lPathInfo.Text = fbconn.getPathInfo();
            tOutput.Text += fbconn.getBufforKomunikatu() + Environment.NewLine;
            tOutput.Text += fbconn.getConnectionState() + Environment.NewLine;
        }

        private void bClose_Click(object sender, EventArgs e)
        {
            try
            {
                fbconn.setConnectionOFF();
                //Close();
                tOutput.Text += fbconn.getConnectionState() + Environment.NewLine;
            }
            catch (Exception ex)
            {
                Logg logg = new Logg(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1007: Bład zamykania połaczenia do bazy: " + ex.Message);
                throw;
            }
        }


        private void bTestPresta_Click_1(object sender, EventArgs e)
        {
            //ManufacturerFactory ManufacturerFactory = new ManufacturerFactory(BaseUrl, Account, Password);

            StockAvailableFactory sotckAvailableFactory = new StockAvailableFactory(BaseUrl, Account, Password);
            List<stock_available> sotckAvailables = sotckAvailableFactory.GetAll();
            ProductFactory productFactory = new ProductFactory(BaseUrl, Account, Password);
            List<Bukimedia.PrestaSharp.Entities.AuxEntities.language> valueLang = new List<Bukimedia.PrestaSharp.Entities.AuxEntities.language>();



            foreach (stock_available item in sotckAvailables)
            {
                product oneProduct = productFactory.Get((long)item.id_product);
                tResponce.Text += item.id_product + ";" + item.quantity + ";" + oneProduct.reference;
                //valueLang = oneProduct.name;
                //foreach (Bukimedia.PrestaSharp.Entities.AuxEntities.language lang in valueLang)
                //{
                //    tResponce.Text += "[" + lang.Value + "] ";
                //}
                //tResponce.Text += ";";
                //valueLang = oneProduct.description_short;
                //foreach (Bukimedia.PrestaSharp.Entities.AuxEntities.language lang in valueLang)
                //{
                //    tResponce.Text += "[" + lang.Value + "] ";
                //}
                tResponce.Text += Environment.NewLine;

            }
        }

        private void bPrestaProdukty_Click(object sender, EventArgs e)
        {
            ProductFactory productFactory = new ProductFactory(BaseUrl, Account, Password);
            List<product> products = productFactory.GetAll();
            SpecificPriceFactory specificPriceFactory = new SpecificPriceFactory(BaseUrl, Account, Password);
            List<Bukimedia.PrestaSharp.Entities.AuxEntities.language> valueLang = new List<Bukimedia.PrestaSharp.Entities.AuxEntities.language>();
            
            foreach (product item in products)
            {

                tResponce.Text += item.id + ";" + item.reference + ";" + item.price + ";";

                Dictionary<string, string> dtn = new Dictionary<string, string>();
                dtn.Add("id", item.id.ToString());
                dtn.Add("reduction_type", "amount");
                List<specific_price> sp = specificPriceFactory.GetByFilter(dtn, null, null);
                if (sp.Count > 0)
                {
                    specific_price cena = sp.First();
                    tResponce.Text += (cena.price * cena.reduction) + ";";
                }
                else
                {
                    tResponce.Text += "0;";
                }

                tResponce.Text += item.depth + ";" + item.text_fields;
                valueLang = item.name;
                foreach (Bukimedia.PrestaSharp.Entities.AuxEntities.language lang in valueLang)
                {
                    tResponce.Text += "[" + lang.Value + "] ";
                }
                tResponce.Text += ";";

                foreach (Bukimedia.PrestaSharp.Entities.AuxEntities.language lang in valueLang)
                {
                    tResponce.Text += "[" + lang.Value + "] ";
                }
                tResponce.Text += ";";   
                valueLang = item.meta_title;

                tResponce.Text += Environment.NewLine;
            }
        }

        private void bClear_Click(object sender, EventArgs e)
        {
            tResponce.Text = "";
        }

        private void bPrestaOrders_Click(object sender, EventArgs e)
        {
            OrderFactory orderFactory = new OrderFactory(BaseUrl, Account, Password);
            //List<order> orders = orderFactory.GetAll();
            
            Dictionary<string, string> dtn3 = new Dictionary<string, string>();
            dtn3.Add("current_state", "3"); // Przygotowanie w toku
            List<order> orders3 = orderFactory.GetByFilter(dtn3, null, null);
            //List<Bukimedia.PrestaSharp.Entities.AuxEntities.language> valueLang3 = new List<Bukimedia.PrestaSharp.Entities.AuxEntities.language>();

            foreach (order item in orders3)
            {
                tResponce.Text += item.id + ";" + item.payment + ";" + item.reference + ";" + item.shipping_number + "; Status:" + item.current_state + "; Wartość:" + item.total_paid + Environment.NewLine;
            }

            Dictionary<string, string>  dtn10 = new Dictionary<string, string>();
            dtn10.Add("current_state", "10"); //Oczekiwanie na płatnosc przelewem bankowym
            List<order> orders10 = orderFactory.GetByFilter(dtn10, null, null);
            //List<Bukimedia.PrestaSharp.Entities.AuxEntities.language> valueLang10 = new List<Bukimedia.PrestaSharp.Entities.AuxEntities.language>();

            foreach (order item in orders10)
            {
                tResponce.Text += item.id + ";" + item.payment + ";" + item.reference + ";" + item.shipping_number + "; Status:" + item.current_state + "; Wartość:" + item.total_paid + Environment.NewLine;
            }

            Dictionary<string, string> dtn2 = new Dictionary<string, string>();
            dtn2.Add("current_state", "2"); //Płatnośc zaakceptowana
            List<order> orders2 = orderFactory.GetByFilter(dtn2, null, null);
            //List<Bukimedia.PrestaSharp.Entities.AuxEntities.language> valueLang2 = new List<Bukimedia.PrestaSharp.Entities.AuxEntities.language>();

            foreach (order item in orders2)
            {
                tResponce.Text += item.id + ";" + item.payment + ";" + item.reference + ";" + item.shipping_number + "; Status:" + item.current_state + "; Wartość:" + item.total_paid + Environment.NewLine;
            }

        }

        private void bSavePresta_Click(object sender, EventArgs e)
        {
            if (tKey.Text.Length > 0)
            {
                FBConn.SetKeyToRegisrty("key", tKey.Text);
            }
            if (thttp.Text.Length > 0)
            {
                FBConn.SetKeyToRegisrty("http", thttp.Text);
            }
            if (tUserHttp.Text.Length > 0)
            {
                FBConn.SetKeyToRegisrty("userHttp", tUserHttp.Text);
            }
            else
            {
                Console.WriteLine(FBConn.GetKeyFromRegistry("userHttp"));
            }
        }

        private void bSaveDafult_Click(object sender, EventArgs e)
        {
            FBConn.checkDefultKeysformRegistryForPresta();
            bSavePresta.PerformClick();
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void bTestLINQ_Click(object sender, EventArgs e)
        {
            RaksRepo rr = new RaksRepo(fbconn,false, true);
        }

        private void b123_Click(object sender, EventArgs e)
        {
            tIP.Text = "192.168.0.123";
            tPath.Text = "C:\\mm\\f00001.fdb";
        }

        private void bStanyZamowien_Click(object sender, EventArgs e)
        {
            //Zwraca statusy zamówień w Presta
            OrderStateFactory orderStateFactory = new OrderStateFactory(BaseUrl, Account, Password);
            List<order_state> ordersState = orderStateFactory.GetAll();
            List<Bukimedia.PrestaSharp.Entities.AuxEntities.language> valueLang = new List<Bukimedia.PrestaSharp.Entities.AuxEntities.language>();

            foreach (order_state item in ordersState)
            {
                tResponce.Text += item.id + ";" + item.module_name + Environment.NewLine;
            }
        }

        private void bMagazyny_Click(object sender, EventArgs e)
        {
            FbCommand fbcmag = new FbCommand("SELECT ID, NUMER,NAZWA from GM_MAGAZYNY;", fbconn.getCurentConnection());
            try
            {
                FbDataReader fdk = fbcmag.ExecuteReader();
                if (fdk.Read())
                {
                    do
                    {
                        tbMagazynyLista.Text += (int)fdk["ID"] + "; " + (string)fdk["NUMER"] + "; " + (string)fdk["NAZWA"] + Environment.NewLine;

                    } while (fdk.Read());
                }
            }
            catch (FbException exgen)
            {
                tResponce.Text = "Bład pobrania ID istniejącego zamówienia lub sprawdzenia czy zamówienie już istnieje: " + exgen.Message;
                throw;
            }
        }

        private void bStanyRun_Click(object sender, EventArgs e)
        {
            FBConn fbc = new FBConn();
            Console.WriteLine("cronAction Po deklaracji połaczenia dla aktualizacji stanów magazynowych");
            MessageBox.Show("START cronAction Po deklaracji połaczenia dla aktualizacji stanów magazynowych");
            RaksRepo rr = new RaksRepo(fbc, true, true);
            Console.WriteLine("cronAction Po wykonaniu dla aktualizacji stanów magazynowych");
            MessageBox.Show("KONIEC cronAction Po wykonaniu dla aktualizacji stanów magazynowych");
        }

        private void bTestCeny_Click(object sender, EventArgs e)
        {
            StockAvailableFactory sotckAvailableFactory = new StockAvailableFactory(BaseUrl, Account, Password);
            List<stock_available> sotckAvailables = sotckAvailableFactory.GetAll();
            ProductFactory productFactory = new ProductFactory(BaseUrl, Account, Password);
            List<Bukimedia.PrestaSharp.Entities.AuxEntities.language> valueLang = new List<Bukimedia.PrestaSharp.Entities.AuxEntities.language>();

            product oneProduct = productFactory.Get(4);
            if (oneProduct != null)
            {
                string indeks = oneProduct.reference;
                //int stan = item.quantity;
                tResponce.Text = "Index: " + indeks + "; prrice:" + oneProduct.price + "; show_price:" + oneProduct.show_price + "; unit_price_ratio:" + oneProduct.unit_price_ratio + "; wholesale_price:" + oneProduct.wholesale_price;
            }
            else
                tResponce.Text = "Nie odnaleziono produktu 4";
        }

        private void bTestStany_Click(object sender, EventArgs e)
        {
            RaksRepo rep = new RaksRepo(fbconn);
            rep.getCalculateStocInRaks("'6646-7AG','136-1A-1'");
            tResponce.Text = rep.getResultMSGofStany();
        }

        private void bReadMagList_Click(object sender, EventArgs e)
        {
            tListaMagDoStanow.Text = FBConn.GetKeyFromRegistry("stanymagazynow");
        }

        private void bSaveMagList_Click(object sender, EventArgs e)
        {
            if (tListaMagDoStanow.Text.Length<4)
                MessageBox.Show("Wstrzymano zapis, lista magazynów wyglada na pustą!");
            else
                FBConn.SetKeyToRegisrty("stanymagazynow", tListaMagDoStanow.Text);
        }

        private void bTestConnectionMarvest123_Click(object sender, EventArgs e)
        {

            tIP.Text = "192.168.0.123";
            tPath.Text = "C:\\Program1\\Data\\f00001.fdb";
        }

        private void bSetStan_Click(object sender, EventArgs e)
        {
            StockAvailableFactory sotckAvailableFactory = new StockAvailableFactory(BaseUrl, Account, Password);
            Dictionary<string, string> dtn = new Dictionary<string, string>();
            dtn.Add("id_product", tKodInPresta.Value.ToString() );
            List<stock_available> sotckAvailables = sotckAvailableFactory.GetByFilter(dtn, null, null);

            foreach (stock_available item in sotckAvailables)
            {
                item.quantity = (int)nStanInput.Value;
                sotckAvailableFactory.Update(item);
                break;  
            }
        }

        private void bCheckStan_Click(object sender, EventArgs e)
        {
            StockAvailableFactory sotckAvailableFactory = new StockAvailableFactory(BaseUrl, Account, Password);
            Dictionary<string, string> dtn = new Dictionary<string, string>();
            //dtn.Add("id_product", tKodInPresta.Text);
            dtn.Add("id_product", tKodInPresta.Value.ToString() );
            List<stock_available> sotckAvailables = sotckAvailableFactory.GetByFilter(dtn, null, null);

            foreach (stock_available item in sotckAvailables)
            {
                nStanInput.Value = item.quantity;
                  
            }
        }

        private void bZapiszFTP_Click(object sender, EventArgs e)
        {
            if (tAdresFTP.Text.Length > 0 && tUserFTP.Text.Length > 0)
            {
                try
                {
                    RegistryKey rejestr = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                    if (rejestr == null)
                    {
                        RegistryKey rejestrNew = Registry.CurrentUser.CreateSubKey(RegistryKey);
                        rejestr = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                    }
                    rejestr.SetValue("adresFTP", tAdresFTP.Text);
                    rejestr.SetValue("userFTP", tUserFTP.Text);
                    rejestr.SetValue("passFTP", tPassFTP.Text);
                }
                catch (Exception ex)
                {
                    Logg logg = new Logg(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1101: Błąd ustawienia wartości w rejestrze Windows dla połączenia FTP: " + ex.Message);
                    System.Windows.Forms.MessageBox.Show("1101: Błąd ustawienia wartości w rejestrze Windows dla połączenia FTP: " + ex.Message);
                }
            }
            else
            {
                Logg logg = new Logg(Logg.RodzajLogowania.Warning, Logg.MediumLoga.File, "1102: Próba zapisu ustawień bez wartości adres i użytkownik FTP w rejestrze Windows dla połączenia FTP: ");
                System.Windows.Forms.MessageBox.Show("1102: Próba zapisu ustawień bez wartości adres i użytkownik FTP w rejestrze Windows dla połączenia FTP");
            }
        }

        private void bOdcztyFTP_Click(object sender, EventArgs e)
        {
            RegistryKey rejestr;
            try
            {
                rejestr = Registry.CurrentUser.OpenSubKey(RegistryKey);
                tAdresFTP.Text = (String)rejestr.GetValue("adresFTP");
                tUserFTP.Text = (String)rejestr.GetValue("userFTP");
                tPassFTP.Text = (String)rejestr.GetValue("passFTP");
            }
            catch (Exception ex)
            {
                Logg logg = new Logg(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1103: Błąd odczytu klucza konfiguracji połączenia FTP z rejestru Windows: : " + ex.Message);
                System.Windows.Forms.MessageBox.Show("1103: Błąd odczytu klucza konfiguracji połączenia FTP z rejestru Windows: " + ex.Message);
            }
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
                Logg logg = new Logg(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1104: Błąd odczytu konfiguracji połączenia do bazy danych z rejestru Windows: : " + ex.Message);
                System.Windows.Forms.MessageBox.Show("1104: Błąd odczytu konfiguracji połączenia do bazy danych z rejestru Windows: " + ex.Message);
            }
        }

        private void bReadPrestaSetings_Click(object sender, EventArgs e)
        {
            RegistryKey rejestr;
            try
            {
                rejestr = Registry.CurrentUser.OpenSubKey(RegistryKey);
                tKey.Text = (String)rejestr.GetValue("key");
                thttp.Text = (String)rejestr.GetValue("http");
                tUserHttp.Text = (String)rejestr.GetValue("userHttp");
            }
            catch (Exception ex)
            {
                Logg logg = new Logg(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1105: Błąd odczytu konfiguracji połączenia do API Presta z rejestru Windows: : " + ex.Message);
                System.Windows.Forms.MessageBox.Show("1105: Błąd odczytu konfiguracji połączenia do API Presta z rejestru Windows: " + ex.Message);
            }
        }

        private void bReadMagForOrder_Click(object sender, EventArgs e)
        {
            RegistryKey rejestr;
            try
            {
                rejestr = Registry.CurrentUser.OpenSubKey(RegistryKey);
                numerMagForOrder.Value = Convert.ToInt16(rejestr.GetValue("magazyn"));
                numerMagForOrder.Visible = true;
            }
            catch (Exception ex)
            {
                Logg logg = new Logg(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1106: Błąd odczytu konfiguracji magazynu do zapisu zamówień z Presty z rejestru Windows: : " + ex.Message);
                System.Windows.Forms.MessageBox.Show("1106: Błąd odczytu konfiguracji magazynu do zapisu zamówień z Presty z rejestru Windows: " + ex.Message);
            }

        }

        private void bMagazyny_Click_1(object sender, EventArgs e)
        {
            FbCommand fbcmag;
            try
            {
                fbcmag = new FbCommand("SELECT ID, NUMER,NAZWA from GM_MAGAZYNY;", fbconn.getCurentConnection());
            }
            catch (FbException fbe)
            {
                MessageBox.Show("Bład połaczenia z bazą danych, sprawdź czy połaczona na pierwszej zakładce: " + fbe.Message);
                throw;
            }
            try
            {
                FbDataReader fdk = fbcmag.ExecuteReader();
                if (fdk.Read())
                {
                    do
                    {
                        tbMagazynyLista.Text += (int)fdk["ID"] + "; " + (string)fdk["NUMER"] + "; " + (string)fdk["NAZWA"] + Environment.NewLine;

                    } while (fdk.Read());
                }
            }
            catch (FbException exgen)
            {
                MessageBox.Show("Bład pobrania listy magazynów z Raks:: " + exgen.Message); 
                throw;
            }
        }

        private void bSaveMagForOrder_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey rejestr = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                if (rejestr == null)
                {
                    RegistryKey rejestrNew = Registry.CurrentUser.CreateSubKey(RegistryKey);
                    rejestr = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                }
                rejestr.SetValue("magazyn", numerMagForOrder.Text);
            }
            catch (Exception ex)
            {
                Logg logg = new Logg(Logg.RodzajLogowania.ErrorMSG, Logg.MediumLoga.File, "1108: Błąd ustawienia wartości w rejestrze Windows dla numeru magazynu do zapisu zamówień: " + ex.Message);
                System.Windows.Forms.MessageBox.Show("1108: Błąd ustawienia wartości w rejestrze Windows dla numeru magazynu do zapisu zamówień: " + ex.Message);
            }
        }

    }
}
