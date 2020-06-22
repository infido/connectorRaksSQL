using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using KonfiguratorConnectorRaksSQL;
using FirebirdSql.Data.FirebirdClient;

namespace RaportyRaksSQL
{
    public partial class OknoZapisDoSchowkaRaks : Form
    {
        FBConn fbconn;
        DataGridView view;
        bool wszystkoJako1 = false;
        string currentTabName = "";

        public OknoZapisDoSchowkaRaks(FBConn fbc, ref DataGridView dgv, bool czyWartoscJeden, string tabName)
        {
            InitializeComponent();
            fbconn = new FBConn();
            fbconn = fbc;
            wszystkoJako1 = czyWartoscJeden;
            currentTabName = tabName;
            view = new DataGridView();
            view = dgv;
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void bSave_Click(object sender, EventArgs e)
        {
            string tmpLab = label3.Text;
            label3.Text += " WYKONUJE...";
            int count = 0; 

            if (tnameClipboard.Text.Length > 0 && tnameUser.Text.Length > 0)
            {
                //FbTransaction myTransaction = fbconn.getCurentConnection().BeginTransaction();
                //FbCommand gen_id_schowek = new FbCommand("SELECT GEN_ID(GM_SCHOWEK_POZYCJI_GEN,1) from rdb$database", fbconn.getCurentConnection(), myTransaction);
                FbCommand gen_id_schowek = new FbCommand("SELECT GEN_ID(GM_SCHOWEK_POZYCJI_GEN,1) from rdb$database", fbconn.getCurentConnection());
                int idscho = 0;


                foreach (DataGridViewRow row in view.Rows)
                {
                    if (row.Cells["SKROT"].Value != null)
                    {
                        #region pobranie nowego id z bazy
                        try
                        {
                            idscho = Convert.ToInt32(gen_id_schowek.ExecuteScalar());
                        }
                        catch (FbException exgen)
                        {
                            MessageBox.Show("Błąd pobierania nowego ID z bazy. Operacje przerwano! " + exgen.Message);
                            throw;
                        }
                        #endregion

                        #region obliczanie ID towaru z Indeksu
                        string idtow = "0";
                        FbCommand gen_id_towaru = new FbCommand("SELECT ID_TOWARU from GM_TOWARY where SKROT='" + row.Cells["SKROT"].Value.ToString() + "';", fbconn.getCurentConnection());
                        try
                        {
                            if (gen_id_towaru.ExecuteScalar() != null)
                            {
                                idtow = (gen_id_towaru.ExecuteScalar()).ToString();
                            }
                            else
                            {
                                tBrakiTowarow.Text += row.Cells["SKROT"].Value.ToString() + ";" + System.Environment.NewLine;
                            }
                        }
                        catch (FbException exgen)
                        {
                            MessageBox.Show("Błąd pobierania nowego ID_TOWARU na podstawie skrótu" + row.Cells["SKROT"].Value.ToString() + " . Operacje przerwano! " + exgen.Message);
                            throw;
                        }

                        #endregion

                        //Tutaj dopiero ilości z innej kolumny ILOSC
                        if (idtow != "0")
                        {
                            if (currentTabName.Contains("tabPageImportCSV"))
                            {
                                #region zapisywanie schowka w wariancie do importu cenników
                                double cena = Convert.ToDouble(row.Cells["CENA"].Value);
                                if (cena > 0)
                                {
                                    double cenaNet = (cena > 0 ? cena / 1.23 : 0);
                                    string sql = setSQLInsertSchowek(idscho, idtow, tnameClipboard.Text, tnameUser.Text, "1.0", cena.ToString(), (cena / 1.23).ToString());

                                    FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
                                    try
                                    {
                                        cdk.ExecuteScalar();
                                        count++;
                                    }
                                    catch (FbException ex)
                                    {
                                        MessageBox.Show("Błąd zapisu danych do schowka z okna z import z pliku CSV: " + ex.Message);
                                    }

                                }
                                #endregion
                            }
                            else
                            {
                                #region zapisywanie schowka w wariancie do importu zamówień
                                double dozam = wszystkoJako1 ? 1.0 : Convert.ToDouble(row.Cells["DO_ZAMOWIENIA"].Value);
                                if (dozam > 0)
                                {
                                    string sql = setSQLInsertSchowek(idscho, idtow, tnameClipboard.Text, tnameUser.Text, dozam.ToString("F"), "0.0", "0.0");

                                    FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
                                    try
                                    {
                                        cdk.ExecuteScalar();
                                        count++;
                                    }
                                    catch (FbException ex)
                                    {
                                        MessageBox.Show("Błąd zapisu danych do schowka z okna z Raportu 1: " + ex.Message);
                                    }

                                }
                                #endregion
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Nie uzupełniono obowiązkowych pól: Nazwa schowka i/lub Użytkownik! Proszę uzupełnić i ponownie zapisać do RaksSQL.");
            }
            label3.Text = "Status OK:" + tmpLab + " wykonano " + count;
            label3.ForeColor = Color.Green;
            bSave.Enabled = false;

        }

        private string setSQLInsertSchowek(int id, string indeks, string schoNaz, string user, string ilosc, string cena, string cenaNet)
        {
            StringBuilder myStringBuilder = new StringBuilder("INSERT INTO GM_SCHOWEK_POZYCJI (");
            myStringBuilder.Append("ID, ");  
            myStringBuilder.Append("OPERATOR, ");  
            myStringBuilder.Append("IDENTYFIKATOR, ");  
            myStringBuilder.Append("PUBLICZNA, ");  
            myStringBuilder.Append("ID_TOWARU, ");  
            myStringBuilder.Append("ILOSC, ");
            myStringBuilder.Append("CENA_SP_PLN_NETTO_PO_RAB, ");
            myStringBuilder.Append("CENA_SP_PLN_BRUTTO_PO_RAB, ");
            myStringBuilder.Append("CENA_SP_PLN_NETTO_PRZED_RAB, ");
            myStringBuilder.Append("CENA_SP_PLN_BRUTTO_PRZED_RAB, ");
            myStringBuilder.Append("CENA_SP_WAL_NETTO_PRZED_RAB, ");
            myStringBuilder.Append("CENA_SP_WAL_BRUTTO_PRZED_RAB, ");
            myStringBuilder.Append("CENA_SP_WAL_NETTO_PO_RAB, ");
            myStringBuilder.Append("CENA_SP_WAL_BRUTTO_PO_RAB, ");
            myStringBuilder.Append("CENA_KATALOGOWA_NETTO, ");
            myStringBuilder.Append("CENA_KATALOGOWA_BRUTTO, ");
            myStringBuilder.Append("ZNACZNIKI, ");  
            myStringBuilder.Append("UWAGI");  

            myStringBuilder.Append(") VALUES ( ");

            myStringBuilder.Append(id.ToString() + ",");  // ID
            myStringBuilder.Append("'" + user + "', ");  // OPERATOR
            myStringBuilder.Append("'" + schoNaz + "', ");  //IDENTYFIKATOR
            myStringBuilder.Append("1, ");  //PUBLICZNA

            myStringBuilder.Append(indeks + ", ");  //ID_TOWARU
            
            myStringBuilder.Append(ilosc.ToString().Replace(",", ".") + ", ");  //ILOSC
            myStringBuilder.Append(cenaNet.ToString().Replace(",", ".") + ", ");  //CENA_SP_PLN_NETTO_PO_RAB
            myStringBuilder.Append(cena.ToString().Replace(",", ".") + ", ");  //CENA_SP_PLN_BRUTTO_PO_RAB
            myStringBuilder.Append(cenaNet.ToString().Replace(",", ".") + ", ");  //CENA_SP_PLN_NETTO_PRZED_RAB
            myStringBuilder.Append(cena.ToString().Replace(",", ".") + ", ");  //CENA_SP_PLN_BRUTTO_PRZED_RAB
            myStringBuilder.Append(cenaNet.ToString().Replace(",", ".") + ", ");  //CENA_SP_WAL_NETTO_PRZED_RAB
            myStringBuilder.Append(cena.ToString().Replace(",", ".") + ", ");  //CCENA_SP_WAL_BRUTTO_PRZED_RAB
            myStringBuilder.Append(cenaNet.ToString().Replace(",", ".") + ", ");  //CENA_SP_WAL_NETTO_PO_RAB
            myStringBuilder.Append(cena.ToString().Replace(",", ".") + ", ");  //CCENA_SP_WAL_BRUTTO_PO_RAB

            myStringBuilder.Append(cenaNet.ToString().Replace(",", ".") + ", ");  //CENA_KATALOGOWA_NETTO
            myStringBuilder.Append(cena.ToString().Replace(",", ".") + ", ");  //CENA_KATALOGOWA_BRUTTO
            myStringBuilder.Append("NULL, ");  //ZNACZNIKI
            myStringBuilder.Append("NULL");  //UWAGI
            myStringBuilder.Append(");");  
            return myStringBuilder.ToString();
        }

        private void OknoZapisDoSchowkaRaks_Load(object sender, EventArgs e)
        {
            int size = view.RowCount-1;
            int lp = 0;
            string name = "";
            if (!currentTabName.Contains("tabPageImportCSV"))
            {
                name = "";
                while (name.Length == 0 && size > lp)
                {
                    name = view.Rows[lp].Cells["DOSTAWCA"].Value.ToString();
                    lp++;
                }
            }
            else
            {
                name = "CSV";
            }
            
            tnameUser.Text = Environment.UserName;
            tnameClipboard.Text = DateTime.Now.ToShortDateString()  + " " + DateTime.Now.ToShortTimeString() + " " + name.Replace(",","").Replace(".","").Replace("!","").Replace(";","");
            if (tnameClipboard.Text.Length > 25)
            {
                tnameClipboard.Text = tnameClipboard.Text.Substring(0, 25);
            }

            label3.Text = "Do zapisania " + size;
        }
    }
}
