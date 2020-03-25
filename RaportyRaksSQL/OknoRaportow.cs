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
using System.IO;
using System.Net;
using Microsoft.Win32;

namespace RaportyRaksSQL
{
    public partial class OknoRaportow : Form
    {
        FBConn fbconn;
        string magazyny = "";
        string podstawoweGT = "";
        string dowolneGT = "";
        string dostawcy = "";
        string producenci = "";
        string search1 = "";
        string search2 = "";
        DateTime mark;
        DataView fDataView;
        bool stanSaveClip = false;
        Int32 currUserId = 0;
        bool czyUserWczytany = false;
        bool czyAdmin = false;

        public OknoRaportow()
        {
            InitializeComponent();
            Text += " " + Application.ProductVersion;
            fbconn = new FBConn();
            
        }

        private void OknoRaportow_Load(object sender, EventArgs e)
        {
            if (fbconn.getConectedToFB())
            {
                int tryLogin = 3;
                while (tryLogin > 0)
                {
                    Autentykacja logToSys = new Autentykacja(fbconn);
                    if (logToSys.GetAutoryzationResult().Equals(AutoryzationType.Uzytkownik))
                    {
                        //poprawne logowanie uzytkownika
                        tabControlParametry.TabPages.Remove((TabPage)tabControlParametry.TabPages["tabAdmin"]);
                        logToSys.SetTimestampLastLogin();
                        currUserId = logToSys.GetCurrentUserID();
                        magazyny = logToSys.GetMagazyny();
                        czyAdmin = false;
                        tryLogin = -1;
                        break;
                    }else if (logToSys.GetAutoryzationResult().Equals(AutoryzationType.Administartor))
                    {
                        //poprawne logowanie administrator
                        logToSys.SetTimestampLastLogin();
                        currUserId = logToSys.GetCurrentUserID();
                        magazyny = logToSys.GetMagazyny();
                        bChangeMyPass.Enabled = false;
                        czyAdmin = true;
                        tryLogin = -1;
                        break;
                    }
                    tryLogin--;
                }
                if (tryLogin == -1)
                {
                    onLoadWindow();
                }
                else {
                    MessageBox.Show("Nieudane logowanie do programu! Program zostanie zamkniety ", "Bład logowania");
                    fbconn.setConnectionOFF();
                    Application.Exit();
                }
            }
            else
            {
                MessageBox.Show("Brak połączenia do bazy danych RaksSQL");
            }
        }

        private void onLoadWindow()
        {
            if (magazyny.Length == 0)
            {
                bWykonajRaport1.Enabled = false;
                bRaport2.Enabled = false;
                bRaport3.Enabled = false;
                bRaport4DlaPB.Enabled = false;
                bRaportSprzedazyStary.Enabled = false;
            }
            FbCommand cdk = new FbCommand("SELECT NUMER from GM_MAGAZYNY order by NUMER;", fbconn.getCurentConnection());
            try
            {
                FbDataReader fdk = cdk.ExecuteReader();
                while (fdk.Read())
                {
                    if (czyAdmin || magazyny.Contains((string)fdk["NUMER"]))
                    {
                        chMagazyny1.Items.Add((string)fdk["NUMER"]);
                    }
                    chMagazynyAdmin.Items.Add((string)fdk["NUMER"]);
                }
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd wczytywania listy magazynów do słownika parametrów: " + ex.Message);
            }

            cdk.CommandText = "SELECT NAZWA FROM GM_GRUPYT where NAZWA<>'Wszystkie' order by NAZWA";
            try
            {
                FbDataReader fdk = cdk.ExecuteReader();
                while (fdk.Read())
                {
                    chPodstawoweGT1.Items.Add((string)fdk["NAZWA"]);
                }
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd wczytywania listy podstawowych grup towarowych do słownika parametrów: " + ex.Message);
            }


            cdk.CommandText = "SELECT NAZWA FROM GM_GRUPYT_EXT where NAZWA<>'Wszystkie' order by NAZWA";
            try
            {
                FbDataReader fdk = cdk.ExecuteReader();
                while (fdk.Read())
                {
                    chDowolneGT1.Items.Add((string)fdk["NAZWA"]);
                }
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd wczytywania listy dowolnych grup towarowych do słownika parametrów: " + ex.Message);
            }

            cdk.CommandText = "SELECT SHORT_NAME FROM GM_TOWARY join R3_CONTACTS on GM_TOWARY.DOSTAWCA=R3_CONTACTS.ID group by SHORT_NAME";
            try
            {
                FbDataReader fdk = cdk.ExecuteReader();
                while (fdk.Read())
                {
                    chDostawcy.Items.Add((string)fdk["SHORT_NAME"]);
                }
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd wczytywania listy dostawców do słownika parametrów: " + ex.Message);
            }

            cdk.CommandText = "SELECT SHORT_NAME FROM GM_TOWARY join R3_CONTACTS on GM_TOWARY.PRODUCENT=R3_CONTACTS.ID group by SHORT_NAME";
            try
            {
                FbDataReader fdk = cdk.ExecuteReader();
                while (fdk.Read())
                {
                    chProducenci.Items.Add((string)fdk["SHORT_NAME"]);
                }
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd wczytywania listy producentów do słownika parametrów: " + ex.Message);
            }
        }

        private void bWykonajRaport1_Click(object sender, EventArgs e)
        {
            toSearch.ReadOnly = true;
            toSearch2.ReadOnly = true;
            SetStatusStartuRaportu(DateTime.Now);
            SetWartosciParametrowDlaWhere();
            bSaveToRaksSQLClipboard.Enabled = false;
            checkBoxIlosc1.Enabled = false;

            string sql = "select * from (";
            sql += " select ";
            sql += " GM_FSPOZ.SKROT_ORYGINALNY INDEKS,";
            sql += " GM_FSPOZ.NAZWA_ORYGINALNA NAZWA,";
            sql += " DOSTAWCY.SHORT_NAME as DOSTAWCA,";
            sql += " PRODUCENCI.SHORT_NAME as PRODUCENT,";
            sql += " GM_RABATY.NAZWA RODZAJ_RABATU, ";
            sql += " GM_FSPOZ.RABAT, ";
            sql += " GM_FSPOZ.ILOSC,";
            sql += " GM_WZPOZ.CENA_ZAKUPU_PO CENA_ZAKUPU_NETTO," ;
            sql += " GM_FSPOZ.CENA_SP_PLN_NETTO CENA_SPRZEDAZY_NETTO,";
            sql += " IIF (GM_FSPOZ.CENA_SP_PLN_NETTO = 0, -1,((GM_FSPOZ.CENA_SP_PLN_NETTO-GM_WZPOZ.CENA_ZAKUPU_PO)/GM_FSPOZ.CENA_SP_PLN_NETTO)) MARZA,";
            sql += " ((GM_FSPOZ.ILOSC*GM_FSPOZ.CENA_SP_PLN_NETTO)-(GM_FSPOZ.ILOSC*GM_WZPOZ.CENA_ZAKUPU_PO)) ZYSK_NETTO,";
            sql += " gm_fs.OPERATOR,";
            sql += " GM_MAGAZYNY.NUMER MAGAZYN, ";
            sql += " GM_RABATY2.NAZWA RODZAJ ";
            sql += " from GM_FSPOZ";
            sql += " left join GM_WZPOZ on GM_FSPOZ.ID = GM_WZPOZ.ID_FSPOZ";
            sql += " left join GM_RABATY on GM_FSPOZ.RODZAJ_RABATU = GM_RABATY.ID";
            sql += " join gm_fs on gm_fspoz.id_glowki=gm_fs.id ";
            sql += " join GM_TOWARY on GM_TOWARY.ID_TOWARU=GM_FSPOZ.ID_TOWARU ";
            sql += " left join GM_RABATY GM_RABATY2 on GM_RABATY2.ID = GM_TOWARY.RABAT ";
            sql += " left join R3_CONTACTS as DOSTAWCY on GM_TOWARY.DOSTAWCA=DOSTAWCY.ID ";
            sql += " left join R3_CONTACTS as PRODUCENCI on GM_TOWARY.PRODUCENT=PRODUCENCI.ID ";
            if (podstawoweGT.Length != 0)
            {
                sql += " left join GM_GRUPYT on GM_GRUPYT.ID=GM_TOWARY.GRUPA";
            }
            if (dowolneGT.Length != 0)
            {
                sql += " left join GM_GRUPYT_EXT_POW on GM_GRUPYT_EXT_POW.ID_TOWARU=GM_FSPOZ.ID_TOWARU ";
                sql += " left join GM_GRUPYT_EXT on GM_GRUPYT_EXT_POW.ID_GRUPY=GM_GRUPYT_EXT.ID ";
            }
            sql += " left join GM_MAGAZYNY on GM_FS.MAGNUM=GM_MAGAZYNY.ID ";
            if (dostawcy.Length != 0)
            {
                sql += " left join R3_CONTACTS R3DOST on R3DOST.ID=GM_TOWARY.DOSTAWCA ";
            }
            if (producenci.Length != 0)
            {
                sql += " left join R3_CONTACTS R3PRODU on R3PRODU.ID=GM_TOWARY.PRODUCENT ";
            }
            sql += " where gm_fs.data_wystawienia>='" + dateOD1.Text.ToString() + "' and gm_fs.data_wystawienia<='" + dateDO1.Text.ToString() + "'";
            if (chPominArchiwalne.Checked)
                sql += " and GM_TOWARY.ARCHIWALNY=0 ";
            if (chTylkoTowar.Checked)
                sql += " and GM_TOWARY.TYP='Towar' ";
            if (magazyny.Length != 0)
                sql += " and GM_MAGAZYNY.NUMER in (" + magazyny + ")";
            if (dowolneGT.Length != 0)
                sql += " and GM_GRUPYT_EXT.NAZWA in (" + dowolneGT + ")";
            if (podstawoweGT.Length != 0)
                sql += " and GM_GRUPYT.NAZWA in (" + podstawoweGT + ")";
            if (dostawcy.Length != 0)
                sql += " and R3DOST.SHORT_NAME in (" + dostawcy + ")";
            if (producenci.Length != 0)
                sql += " and R3PRODU.SHORT_NAME in (" + producenci + ")";

            sql += " union all ";

            sql += " select ";
            sql += " GM_KSPOZ.SKROT_ORYGINALNY INDEKS,";
            sql += " GM_KSPOZ.NAZWA_ORYGINALNA NAZWA,";
            sql += " DOSTAWCY.SHORT_NAME as DOSTAWCA,";
            sql += " PRODUCENCI.SHORT_NAME as PRODUCENT,";
            sql += " '' RODZAJ_RABATU,";
            sql += " 0 as RABAT,";
            sql += " GM_KSPOZ.ILOSC_PO - GM_KSPOZ.ILOSC_PRZED as ILOSC,";
            //
            sql += " GM_WZPOZ.CENA_ZAKUPU_PO CENA_ZAKUPU_NETTO,";
            
            sql += " GM_KSPOZ.CENA_SP_PLN_NETTO_PO - GM_KSPOZ.CENA_SP_PLN_NETTO_PRZED CENA_SPRZEDAZY_NETTO,";
            
            sql += " IIF (GM_KSPOZ.CENA_SP_PLN_NETTO_PO = 0, -1,(";
            sql += "((GM_KSPOZ.CENA_SP_PLN_NETTO_PO-GM_WZPOZ.CENA_ZAKUPU_PO) /GM_KSPOZ.CENA_SP_PLN_NETTO_PO)";
            sql += " - ";
            sql += "((GM_KSPOZ.CENA_SP_PLN_NETTO_PRZED-GM_WZPOZ.CENA_ZAKUPU_PO) /GM_KSPOZ.CENA_SP_PLN_NETTO_PRZED)";
            sql += ")) MARZA,";
            
            sql += " (";
            //zysk po
            sql += " ((GM_KSPOZ.ILOSC_PO*GM_KSPOZ.CENA_SP_PLN_NETTO_PO)-(GM_KSPOZ.ILOSC_PO*GM_WZPOZ.CENA_ZAKUPU_PO)) ";
            sql += "-";
            //zysk przed
            sql += "((GM_KSPOZ.ILOSC_PRZED*GM_KSPOZ.CENA_SP_PLN_NETTO_PRZED)-(GM_KSPOZ.ILOSC_PRZED*GM_WZPOZ.CENA_ZAKUPU_PO))";
            sql += " ) ZYSK_NETTO,";
            //sql += " 0 ZYSK_NETTO,";

            sql += " gm_ks.OPERATOR,";
            sql += " GM_MAGAZYNY.NUMER MAGAZYN, ";
            sql += " GM_RABATY2.NAZWA RODZAJ ";
            sql += " from GM_KSPOZ";
            //
            sql += " left join GM_WZPOZ on GM_KSPOZ.ID = GM_WZPOZ.ID_KSPOZ";
            sql += " join gm_ks on gm_kspoz.id_glowki=gm_ks.id ";
            sql += " join GM_TOWARY on GM_TOWARY.ID_TOWARU=GM_KSPOZ.ID_TOWARU ";
            sql += " left join GM_RABATY GM_RABATY2 on GM_RABATY2.ID = GM_TOWARY.RABAT ";
            sql += " join R3_CONTACTS as DOSTAWCY on GM_TOWARY.DOSTAWCA=DOSTAWCY.ID ";
            sql += " join R3_CONTACTS as PRODUCENCI on GM_TOWARY.PRODUCENT=PRODUCENCI.ID ";
            if (podstawoweGT.Length != 0)
            {
                sql += " left join GM_GRUPYT on GM_GRUPYT.ID=GM_TOWARY.GRUPA";
            }
            if (dowolneGT.Length != 0)
            {
                sql += " left join GM_GRUPYT_EXT_POW on GM_GRUPYT_EXT_POW.ID_TOWARU=GM_KSPOZ.ID_TOWARU ";
                sql += " left join GM_GRUPYT_EXT on GM_GRUPYT_EXT_POW.ID_GRUPY=GM_GRUPYT_EXT.ID ";
            }
            sql += " left join GM_MAGAZYNY on GM_KS.MAGNUM=GM_MAGAZYNY.ID ";
            if (dostawcy.Length != 0)
            {
                sql += " join R3_CONTACTS R3DOST on R3DOST.ID=GM_TOWARY.DOSTAWCA ";
            }
            if (producenci.Length != 0)
            {
                sql += " join R3_CONTACTS R3PRODU on R3PRODU.ID=GM_TOWARY.PRODUCENT ";
            }
            sql += " where gm_ks.data_wystawienia>='" + dateOD1.Text.ToString() + "' and gm_ks.data_wystawienia<='" + dateDO1.Text.ToString() + "'";
            if (chPominArchiwalne.Checked)
                sql += " and GM_TOWARY.ARCHIWALNY=0 ";
            if (chTylkoTowar.Checked)
                sql += " and GM_TOWARY.TYP='Towar' ";
            if (magazyny.Length != 0)
                sql += " and GM_MAGAZYNY.NUMER in (" + magazyny + ")";
            if (dowolneGT.Length != 0)
                sql += " and GM_GRUPYT_EXT.NAZWA in (" + dowolneGT + ")";
            if (podstawoweGT.Length != 0)
                sql += " and GM_GRUPYT.NAZWA in (" + podstawoweGT + ")";
            if (dostawcy.Length != 0)
                sql += " and R3DOST.SHORT_NAME in (" + dostawcy + ")";
            if (producenci.Length != 0)
                sql += " and R3PRODU.SHORT_NAME in (" + producenci + ")";

            sql += " ) ;";

            FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
            try
            {
                FbDataAdapter adapter = new FbDataAdapter(cdk);
                DataTable dt = new DataTable("RESULT");
                adapter.Fill(dt);
                fDataView = new DataView();
                fDataView.Table = dt;
                dataGridView1.DataSource = fDataView;
            }
            catch (FbException ex)
            {
                statusLable.Text = "Błąd wykonania raportu!";
                MessageBox.Show("Błąd wczytywania danych do okna z Raportu 1: " + ex.Message);
            }

            SetStatusKońcaRaportuNaPasku();
        }

        private void bRaportSprzedazyStary_Click(object sender, EventArgs e)
        {
            SetStatusStartuRaportu(DateTime.Now);

            string sql = " select * from ( ";
            sql += "select gm_fs.OPERATOR,gm_fs.DATA_WYSTAWIENIA,gm_fs.NUMER, gm_fs.NAZWA_SPOSOBU_PLATNOSCI, gm_towary.SKROT, COALESCE(gm_towary.SKROT2,'') as SKROT2, ";
            sql += "gm_towary.NAZWA,GM_FSPOZ.ILOSC,GM_FSPOZ.CENA_SP_PLN_NETTO, GM_FSPOZ.ILOSC * GM_FSPOZ.CENA_SP_PLN_NETTO as WARTOSC ";
            sql += "from gm_fspoz ";
            sql += " join gm_fs on gm_fspoz.id_glowki=gm_fs.id ";
            sql += "join gm_towary on gm_fspoz.id_towaru=gm_towary.id ";
            sql += " where gm_fs.data_wystawienia>='" + dateOd2.Text.ToString() + "' and gm_fs.data_wystawienia<='" + dateDO2.Text.ToString() + "'";

            sql += " union all ";

            sql += "select  ";
            sql += "gm_ks.OPERATOR, ";
            sql += "gm_ks.DATA_WYSTAWIENIA, ";
            sql += "gm_ks.NUMER,  ";
            sql += "gm_ks.NAZWA_SPOSOBU_PLATNOSCI,  ";
            sql += "gm_towary.SKROT,  ";
            sql += "COALESCE(gm_towary.SKROT2,'') as SKROT2,  ";
            sql += "gm_towary.NAZWA, ";
            sql += "GM_KSPOZ.ILOSC_PO - GM_KSPOZ.ILOSC_PRZED as ILOSC, ";
            sql += "GM_KSPOZ.CENA_SP_PLN_NETTO_PO - GM_KSPOZ.CENA_SP_PLN_NETTO_PRZED as CENA_SP_PLN_NETTO,  ";
            sql += "(GM_KSPOZ.ILOSC_PO * GM_KSPOZ.CENA_SP_PLN_NETTO_PO) - (GM_KSPOZ.ILOSC_PRZED * GM_KSPOZ.CENA_SP_PLN_NETTO_PRZED) as WARTOSC  ";
            sql += "from gm_kspoz   ";
            sql += "join gm_ks on gm_kspoz.id_glowki=gm_ks.id  ";   
            sql += "join gm_towary on gm_kspoz.id_towaru=gm_towary.id   ";
            sql += "where gm_ks.data_wystawienia>='" + dateOd2.Text.ToString() + "' and gm_ks.data_wystawienia<='" + dateDO2.Text.ToString() + "' ";

            sql += " ) order by DATA_WYSTAWIENIA, NUMER;";

            FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
            try
            {
                FbDataAdapter adapter = new FbDataAdapter(cdk);
                DataTable dt = new DataTable("RESULT");
                adapter.Fill(dt);
                fDataView = new DataView();
                fDataView.Table = dt;
                dataGridView1.DataSource = fDataView;
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd wczytywania danych do okna z Raportu " + sender.ToString() +  " : " + ex.Message);
            }

            SetStatusKońcaRaportuNaPasku();
        }

        private void bSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "csv|*.csv|Text|*.txt";
            saveFileDialog1.Title = "Zapis raportu do pliku";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {

                StringBuilder builder = new StringBuilder();
                if (CzyDodacNaglowek.Checked)
                {
                    for (int i = 0; i < dataGridView1.ColumnCount; i++)
                        if (checkBoxKwalifik.Checked)
                        {
                            builder.AppendFormat(i == (dataGridView1.Columns.Count - 1) ? " \"{0}\"" : "\"{0}\";", dataGridView1.Columns[i].HeaderText);
                        }
                        else
                        {
                            builder.AppendFormat(i == (dataGridView1.Columns.Count - 1) ? " {0}" : "{0};", dataGridView1.Columns[i].HeaderText);
                        }
                    builder.AppendLine();
                }

                for (int i = 0; i < dataGridView1.RowCount-1; i++)
                {

                    foreach (DataGridViewCell cell in dataGridView1.Rows[i].Cells)
                    {
                        if (cell.ValueType.ToString().Equals("System.Decimal"))
                        {
                            if (checkBoxKwalifik.Checked)
                            {
                                builder.AppendFormat(cell.ColumnIndex == (dataGridView1.Columns.Count - 1) ? "\"{0}\"" : "\"{0}\";", cell.Value.ToString().Replace(",", "."));
                            }
                            else
                            {
                                builder.AppendFormat(cell.ColumnIndex == (dataGridView1.Columns.Count - 1) ? "{0}" : "{0};", cell.Value.ToString().Replace(",", "."));
                            }
                        }
                        else
                        {
                            if (checkBoxKwalifik.Checked)
                            {
                                builder.AppendFormat(cell.ColumnIndex == (dataGridView1.Columns.Count - 1) ? "\"{0}\"" : "\"{0}\";", cell.Value);
                            }
                            else
                            {
                                builder.AppendFormat(cell.ColumnIndex == (dataGridView1.Columns.Count - 1) ? "{0}" : "{0};", cell.Value);
                            }
                        }
                    }
                    builder.AppendLine();
                }

                try
                {
                    if (File.Exists(saveFileDialog1.FileName))
                        File.Delete(saveFileDialog1.FileName);
                    File.WriteAllText(saveFileDialog1.FileName, builder.ToString());
                    MessageBox.Show("Poprawnie zapisano plik z raportem: " + saveFileDialog1.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Bład zapisu pliku:" + saveFileDialog1.FileName +  " z raportem:" + ex.Message);
                    throw;
                }
            }
        }

        private void bSetYear2009_Click(object sender, EventArgs e)
        {
            dateOD1.Text = dateOd2.Text = "2018-10-01";
        }

        private void SetWartosciParametrowDlaWhere()
        {
            magazyny = "";
            foreach (var item in chMagazyny1.CheckedItems)
            {
                if (magazyny.Length == 0)
                    magazyny = "'" + item.ToString() + "'";
                else
                    magazyny += ",'" + item.ToString() + "'";
            }
            if (magazyny.Length==0 && czyAdmin == false)
            {
                foreach (var item in chMagazyny1.Items)
                {
                    if (magazyny.Length == 0)
                        magazyny = "'" + item.ToString() + "'";
                    else
                        magazyny += ",'" + item.ToString() + "'";
                }
            }

            podstawoweGT = "";
            foreach (var item in chPodstawoweGT1.CheckedItems)
            {
                if (podstawoweGT.Length == 0)
                    podstawoweGT = "'" + item.ToString() + "'";
                else
                    podstawoweGT += ",'" + item.ToString() + "'";
            }

            dowolneGT = "";
            foreach (var item in chDowolneGT1.CheckedItems)
            {
                if (dowolneGT.Length == 0)
                    dowolneGT = "'" + item.ToString() + "'";
                else
                    dowolneGT += ",'" + item.ToString() + "'";
            }

            dostawcy = "";
            foreach (var item in chDostawcy.CheckedItems)
            {
                if (dostawcy.Length == 0)
                    dostawcy = "'" + item.ToString() + "'";
                else
                    dostawcy += ",'" + item.ToString() + "'";
            }

            producenci = "";
            foreach (var item in chProducenci.CheckedItems)
            {
                if (producenci.Length == 0)
                    producenci = "'" + item.ToString() + "'";
                else
                    producenci += ",'" + item.ToString() + "'";
            }
        }

        private void SetStatusKońcaRaportuNaPasku()
        {
            statusLable.Text = "Wykonano raport, rekordów:" + (dataGridView1.RowCount-1) + " w czasie: " + (DateTime.Now - mark);
        }

        private void SetStatusStartuRaportu(DateTime timestart)
        {
            bSaveFTPPowerbike.Enabled = false;
            mark = timestart;
            statusLable.Text = "Wykonuje raport... Start: " + mark;
        }

        private void bRaport2_Click(object sender, EventArgs e)
        {
            toSearch.ReadOnly = true;
            toSearch2.ReadOnly = true;
            SetStatusStartuRaportu(DateTime.Now);
            SetWartosciParametrowDlaWhere();
            bSaveToRaksSQLClipboard.Enabled = false;
            checkBoxIlosc1.Enabled = false;

            string sql = " select MAGAZYN, INDEKS, NAZWA, sum(ILOSC) as ILOSC, sum(STANMIN) as STANMIN, sum(STANMAX) as STANMAX, DOSTAWCA, PRODUCENT, RODZAJ from ( ";

            sql += "select ";
            sql += " GM_MAGAZYNY.NUMER MAGAZYN, ";
            sql += " GM_TOWARY.SKROT INDEKS,";
            sql += " GM_TOWARY.NAZWA,";
            sql += " GM_FSPOZ.ILOSC,";
            //sql += " 0 STAN_W_MAGAZYNIE,";
            sql += " GM_TOWARY.STANMIN, ";
            sql += " GM_TOWARY.STANMAX, ";
            //sql += " 0 DO_ZAMOWIENIA, ";
            sql += " R3DOST.SHORT_NAME DOSTAWCA, ";
            sql += " R3PRODU.SHORT_NAME PRODUCENT, ";
            sql += " GM_RABATY.NAZWA RODZAJ";
            sql += " from GM_TOWARY";
            sql += " left join GM_FSPOZ on GM_TOWARY.ID_TOWARU=GM_FSPOZ.ID_TOWARU ";
            sql += " left join GM_WZPOZ on GM_FSPOZ.ID = GM_WZPOZ.ID_FSPOZ";
            sql += " left join gm_fs on gm_fspoz.id_glowki=gm_fs.id ";
            sql += " left join GM_MAGAZYNY on GM_FS.MAGNUM=GM_MAGAZYNY.ID ";
            sql += " left join R3_CONTACTS R3DOST on R3DOST.ID=GM_TOWARY.DOSTAWCA ";
            sql += " left join R3_CONTACTS R3PRODU on R3PRODU.ID=GM_TOWARY.PRODUCENT ";
            sql += " left join GM_RABATY on GM_RABATY.ID = GM_TOWARY.RABAT ";

            if (podstawoweGT.Length != 0)
                sql += " left join GM_GRUPYT on GM_GRUPYT.ID=GM_TOWARY.GRUPA";
            if (dowolneGT.Length != 0)
            {
                sql += " left join GM_GRUPYT_EXT_POW on GM_GRUPYT_EXT_POW.ID_TOWARU=GM_TOWARY.ID_TOWARU ";
                sql += " left join GM_GRUPYT_EXT on GM_GRUPYT_EXT_POW.ID_GRUPY=GM_GRUPYT_EXT.ID ";
            }
            
            sql += " where gm_fs.data_wystawienia>='" + dateOD1.Text.ToString() + "' and gm_fs.data_wystawienia<='" + dateDO1.Text.ToString() + "'";
            if (chPominArchiwalne.Checked)
                sql += " and GM_TOWARY.ARCHIWALNY=0 ";
            if (chTylkoTowar.Checked)
                sql += " and GM_TOWARY.TYP='Towar' "; 
            if (magazyny.Length != 0)
                sql += " and GM_MAGAZYNY.NUMER in (" + magazyny + ")";
            if (dowolneGT.Length != 0)
                sql += " and GM_GRUPYT_EXT.NAZWA in (" + dowolneGT + ")";
            if (podstawoweGT.Length != 0)
                sql += " and GM_GRUPYT.NAZWA in (" + podstawoweGT + ")";
            if (dostawcy.Length != 0)
                sql += " and R3DOST.SHORT_NAME in (" + dostawcy + ")";
            if (producenci.Length != 0)
                sql += " and R3PRODU.SHORT_NAME in (" + producenci + ")";

            sql += " union all ";

            sql += "select ";
            sql += " GM_MAGAZYNY.NUMER MAGAZYN, ";
            sql += " GM_TOWARY.SKROT INDEKS,";
            sql += " GM_TOWARY.NAZWA,";
            sql += " GM_KSPOZ.ILOSC_PO - GM_KSPOZ.ILOSC_PRZED as ILOSC,";
            //sql += " 0 STAN_W_MAGAZYNIE,";
            sql += " 0 as STANMIN, ";
            sql += " 0 as STANMAX, ";
            //sql += " 0 DO_ZAMOWIENIA, ";
            sql += " R3DOST.SHORT_NAME DOSTAWCA, ";
            sql += " R3PRODU.SHORT_NAME PRODUCENT, ";
            sql += " GM_RABATY.NAZWA RODZAJ";
            sql += " from GM_TOWARY";
            sql += " left join GM_KSPOZ on GM_TOWARY.ID_TOWARU=GM_KSPOZ.ID_TOWARU ";
            //sql += " left join GM_WZPOZ on GM_KSPOZ.ID = GM_WZPOZ.ID_KSPOZ";
            sql += " left join gm_ks on gm_kspoz.id_glowki=gm_ks.id ";
            sql += " left join GM_MAGAZYNY on GM_KS.MAGNUM=GM_MAGAZYNY.ID ";
            sql += " left join R3_CONTACTS R3DOST on R3DOST.ID=GM_TOWARY.DOSTAWCA ";
            sql += " left join R3_CONTACTS R3PRODU on R3PRODU.ID=GM_TOWARY.PRODUCENT ";
            sql += " left join GM_RABATY on GM_RABATY.ID = GM_TOWARY.RABAT ";

            if (podstawoweGT.Length != 0)
                sql += " left join GM_GRUPYT on GM_GRUPYT.ID=GM_TOWARY.GRUPA";
            if (dowolneGT.Length != 0)
            {
                sql += " left join GM_GRUPYT_EXT_POW on GM_GRUPYT_EXT_POW.ID_TOWARU=GM_TOWARY.ID_TOWARU ";
                sql += " left join GM_GRUPYT_EXT on GM_GRUPYT_EXT_POW.ID_GRUPY=GM_GRUPYT_EXT.ID ";
            }

            sql += " where gm_ks.data_wystawienia>='" + dateOD1.Text.ToString() + "' and gm_ks.data_wystawienia<='" + dateDO1.Text.ToString() + "'";
            if (chPominArchiwalne.Checked)
                sql += " and GM_TOWARY.ARCHIWALNY=0 ";
            if (chTylkoTowar.Checked)
                sql += " and GM_TOWARY.TYP='Towar' ";
            if (magazyny.Length != 0)
                sql += " and GM_MAGAZYNY.NUMER in (" + magazyny + ")";
            if (dowolneGT.Length != 0)
                sql += " and GM_GRUPYT_EXT.NAZWA in (" + dowolneGT + ")";
            if (podstawoweGT.Length != 0)
                sql += " and GM_GRUPYT.NAZWA in (" + podstawoweGT + ")";
            if (dostawcy.Length != 0)
                sql += " and R3DOST.SHORT_NAME in (" + dostawcy + ")";
            if (producenci.Length != 0)
                sql += " and R3PRODU.SHORT_NAME in (" + producenci + ")";

            sql += " ) group by MAGAZYN, INDEKS, NAZWA, DOSTAWCA, PRODUCENT, RODZAJ; ";

            FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
            try
            {
                FbDataAdapter adapter = new FbDataAdapter(cdk);
                DataTable dt = new DataTable("RESULT");
                adapter.Fill(dt);
                fDataView = new DataView();
                fDataView.Table = dt;
                dataGridView1.DataSource = fDataView;
            }
            catch (FbException ex)
            {
                statusLable.Text = "Błąd wykonania raportu!";
                MessageBox.Show("Błąd wczytywania danych do okna z Raportu 1: " + ex.Message);
            }

            SetStatusKońcaRaportuNaPasku();
        }

        private void bRaport3_Click(object sender, EventArgs e)
        {
            //Analiza stanow minimalnych dla wszystkich
            toSearch.ReadOnly = true;
            toSearch2.ReadOnly = true;
            SetStatusStartuRaportu(DateTime.Now);
            SetWartosciParametrowDlaWhere();
            bSaveToRaksSQLClipboard.Enabled = true;
            checkBoxIlosc1.Enabled = true;
            checkBoxIlosc1.Checked = false;

            string sql = " ";
            sql += " select SKROT, RODZAJ, KOD_KRESKOWY, NAZWA, sum(STAN_MAG) STAN_MAG, sum(STANMIN) STAN_MIN, sum(STANMAX) STAN_MAX,  ";
            sql += " IIF((sum(STANMIN)-sum(STAN_MAG))<0, ";
            sql += " IIF((sum(STANMAX)-sum(STAN_MAG))<0,(sum(STANMAX)-sum(STAN_MAG)),0),(sum(STANMIN)-sum(STAN_MAG)) ) DO_ZAMOWIENIA,DOST DOSTAWCA, PRODU PRODUCENT, LOKALIZACJA ";
            sql += " FROM ( ";
            sql += " SELECT GM_TOWARY.SKROT, GM_RABATY.NAZWA RODZAJ, GM_TOWARY.KOD_KRESKOWY, GM_TOWARY.NAZWA, 0 STAN_MAG, GM_TOWARY.STANMIN, GM_TOWARY.STANMAX, R3DOST.SHORT_NAME DOST, R3PRODU.SHORT_NAME PRODU, GM_LOKALIZACJE.NAZWA LOKALIZACJA ";
            sql += " FROM GM_TOWARY   ";
            sql += " left join GM_RABATY on GM_RABATY.ID = GM_TOWARY.RABAT ";
            sql += " left join R3_CONTACTS R3DOST on R3DOST.ID=GM_TOWARY.DOSTAWCA ";
            sql += " left join R3_CONTACTS R3PRODU on R3PRODU.ID=GM_TOWARY.PRODUCENT ";
            sql += " left join GM_LOKALIZACJE on GM_LOKALIZACJE.ID=GM_TOWARY.LOKALIZACJA ";
            if (podstawoweGT.Length != 0)
                sql += " left join GM_GRUPYT on GM_GRUPYT.ID=GM_TOWARY.GRUPA ";
            if (dowolneGT.Length != 0)
            {
                sql += " left join GM_GRUPYT_EXT_POW on GM_GRUPYT_EXT_POW.ID_TOWARU=GM_TOWARY.ID_TOWARU ";
                sql += " left join GM_GRUPYT_EXT on GM_GRUPYT_EXT_POW.ID_GRUPY=GM_GRUPYT_EXT.ID ";
            }
            string tmpsql = "";
            //Tutaj nie ma wogóle magazynu
            //if (magazyny.Length != 0)
            //{
            //    if (tmpsql.Length != 0)
            //        tmpsql += " and GM_MAGAZYNY.NUMER in (" + magazyny + ")";
            //    else
            //        tmpsql += " GM_MAGAZYNY.NUMER in (" + magazyny + ")";
            //}
            if (dowolneGT.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and GM_GRUPYT_EXT.NAZWA in (" + dowolneGT + ")";
                else
                    tmpsql += " GM_GRUPYT_EXT.NAZWA in (" + dowolneGT + ")";
            }
            if (podstawoweGT.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and GM_GRUPYT.NAZWA in (" + podstawoweGT + ")";
                else
                    tmpsql += " GM_GRUPYT.NAZWA in (" + podstawoweGT + ")";
            }
            if (dostawcy.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and R3DOST.SHORT_NAME in (" + dostawcy + ")";
                else
                    tmpsql += " R3DOST.SHORT_NAME in (" + dostawcy + ")";
            }
            if (producenci.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and R3PRODU.SHORT_NAME in (" + producenci + ")";
                else
                    tmpsql += " R3PRODU.SHORT_NAME in (" + producenci + ")";
            }
            if (chPominArchiwalne.Checked)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and GM_TOWARY.ARCHIWALNY=0 ";
                else
                    tmpsql += " GM_TOWARY.ARCHIWALNY=0 ";
            }
            if (chTylkoTowar.Checked)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and GM_TOWARY.TYP='Towar' ";
                else
                    tmpsql += " GM_TOWARY.TYP='Towar' ";
            }
            if (tmpsql.Length > 0)
                sql += " where " + tmpsql;

            sql += " union ";
            sql += " SELECT GM_TOWARY.SKROT , GM_RABATY.NAZWA RODZAJ, GM_TOWARY.KOD_KRESKOWY, GM_TOWARY.NAZWA, sum(GM_MAGAZYN.ILOSC) STAN_MAG, 0 STANMIN, 0 STANMAX, R3DOST.SHORT_NAME DOST, R3PRODU.SHORT_NAME PRODU, GM_LOKALIZACJE.NAZWA LOKALIZACJA  ";
            sql += " FROM GM_MAGAZYN ";
            sql += " join GM_TOWARY on GM_TOWARY.ID_TOWARU=GM_MAGAZYN.ID_TOWAR ";
            sql += " left join GM_RABATY on GM_RABATY.ID = GM_TOWARY.RABAT ";
            sql += " join GM_MAGAZYNY on GM_MAGAZYNY.ID=GM_MAGAZYN.MAGNUM ";
            sql += " left join R3_CONTACTS R3DOST on R3DOST.ID=GM_TOWARY.DOSTAWCA ";
            sql += " left join R3_CONTACTS R3PRODU on R3PRODU.ID=GM_TOWARY.PRODUCENT ";
            sql += " left join GM_LOKALIZACJE on GM_LOKALIZACJE.ID=GM_TOWARY.LOKALIZACJA ";
            if (podstawoweGT.Length != 0)
                sql += " left join GM_GRUPYT on GM_GRUPYT.ID=GM_TOWARY.GRUPA ";
            if (dowolneGT.Length != 0)
            {
                sql += " left join GM_GRUPYT_EXT_POW on GM_GRUPYT_EXT_POW.ID_TOWARU=GM_TOWARY.ID_TOWARU ";
                sql += " left join GM_GRUPYT_EXT on GM_GRUPYT_EXT_POW.ID_GRUPY=GM_GRUPYT_EXT.ID ";
            }
            tmpsql = "";
            if (magazyny.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and GM_MAGAZYNY.NUMER in (" + magazyny + ")";
                else
                    tmpsql += " GM_MAGAZYNY.NUMER in (" + magazyny + ")";
            }
            if (dowolneGT.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and GM_GRUPYT_EXT.NAZWA in (" + dowolneGT + ")";
                else
                    tmpsql += " GM_GRUPYT_EXT.NAZWA in (" + dowolneGT + ")";
            }
            if (podstawoweGT.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and GM_GRUPYT.NAZWA in (" + podstawoweGT + ")";
                else
                    tmpsql += " GM_GRUPYT.NAZWA in (" + podstawoweGT + ")";
            }
            if (dostawcy.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and R3DOST.SHORT_NAME in (" + dostawcy + ")";
                else
                    tmpsql += " R3DOST.SHORT_NAME in (" + dostawcy + ")";
            }
            if (producenci.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and R3PRODU.SHORT_NAME in (" + producenci + ")";
                else
                    tmpsql += " R3PRODU.SHORT_NAME in (" + producenci + ")";
            }
            if (tmpsql.Length>0)
                sql += " where " + tmpsql;

            sql += " group by  GM_MAGAZYN.MAGNUM, GM_TOWARY.SKROT, GM_RABATY.NAZWA, GM_TOWARY.KOD_KRESKOWY, GM_TOWARY.NAZWA,R3DOST.SHORT_NAME, R3PRODU.SHORT_NAME, GM_LOKALIZACJE.NAZWA) a ";
            sql += " group by SKROT, RODZAJ, KOD_KRESKOWY, NAZWA, DOSTAWCA, PRODUCENT, LOKALIZACJA ";
            sql += " order by SKROT ";

            FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
            try
            {
                FbDataAdapter adapter = new FbDataAdapter(cdk);
                DataTable dt = new DataTable("RESULT");
                adapter.Fill(dt);
                fDataView = new DataView();
                fDataView.Table = dt;
                dataGridView1.DataSource = fDataView;
            }
            catch (FbException ex)
            {
                statusLable.Text = "Błąd wykonania raportu!";
                MessageBox.Show("Błąd wczytywania danych do okna z Raportu 1: " + ex.Message);
            }

            SetStatusKońcaRaportuNaPasku();
        }

        private void bRaport4DlaPB_Click(object sender, EventArgs e)
        {
            toSearch.ReadOnly = true;
            toSearch2.ReadOnly = true;
            SetStatusStartuRaportu(DateTime.Now);
            SetWartosciParametrowDlaWhere();
            bSaveToRaksSQLClipboard.Enabled = false;
            checkBoxIlosc1.Enabled = false;

            string sql = " ";
            if (checkBoxPBindex.Checked) 
            {
                sql += " select SKROT, sum(AKTUALNY_STAN) STAN_MAGAZYNU, sum(ILOSC_SPRZEDANA) SPRZEDANE_DNIA ";
                sql += " from ( ";
                sql += " select GM_TOWARY.SKROT,";
            }else
            {
                sql += " select KONTOFK, sum(AKTUALNY_STAN) STAN_MAGAZYNU, sum(ILOSC_SPRZEDANA) SPRZEDANE_DNIA ";
                sql += " from ( ";
                sql += " select GM_TOWARY.KONTOFK,";
            }

            sql += " 0 AKTUALNY_STAN, 0 ILOSC_SPRZEDANA ";
            sql += " FROM GM_TOWARY ";
            sql += " left join R3_CONTACTS R3DOST on R3DOST.ID=GM_TOWARY.DOSTAWCA ";
            sql += " left join R3_CONTACTS R3PRODU on R3PRODU.ID=GM_TOWARY.PRODUCENT ";
            if (podstawoweGT.Length != 0)
                sql += " left join GM_GRUPYT on GM_GRUPYT.ID=GM_TOWARY.GRUPA ";
            if (dowolneGT.Length != 0)
            {
                sql += " left join GM_GRUPYT_EXT_POW on GM_GRUPYT_EXT_POW.ID_TOWARU=GM_TOWARY.ID_TOWARU ";
                sql += " left join GM_GRUPYT_EXT on GM_GRUPYT_EXT_POW.ID_GRUPY=GM_GRUPYT_EXT.ID ";
            }
            string tmpsql = "";
            if (dowolneGT.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and GM_GRUPYT_EXT.NAZWA in (" + dowolneGT + ")";
                else
                    tmpsql += " GM_GRUPYT_EXT.NAZWA in (" + dowolneGT + ")";
            }
            if (podstawoweGT.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and GM_GRUPYT.NAZWA in (" + podstawoweGT + ")";
                else
                    tmpsql += " GM_GRUPYT.NAZWA in (" + podstawoweGT + ")";
            }
            if (dostawcy.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and R3DOST.SHORT_NAME in (" + dostawcy + ")";
                else
                    tmpsql += " R3DOST.SHORT_NAME in (" + dostawcy + ")";
            }
            if (producenci.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and R3PRODU.SHORT_NAME in (" + producenci + ")";
                else
                    tmpsql += " R3PRODU.SHORT_NAME in (" + producenci + ")";
            }
            if (chPominArchiwalne.Checked)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and GM_TOWARY.ARCHIWALNY=0 ";
                else
                    tmpsql += " GM_TOWARY.ARCHIWALNY=0 ";
            }
            if (chTylkoTowar.Checked)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and GM_TOWARY.TYP='Towar' ";
                else
                    tmpsql += " GM_TOWARY.TYP='Towar' ";
            }
            if (tmpsql.Length > 0)
                sql += " where " + tmpsql;

            if (checkBoxPBindex.Checked)
            {
                sql += " group by  GM_TOWARY.SKROT ";
            }
            else
            {
                sql += " group by  GM_TOWARY.KONTOFK ";
            }


            sql += " union ";

            if (checkBoxPBindex.Checked)
            {
                sql += " select GM_TOWARY.SKROT, sum(GM_MAGAZYN.ILOSC) AKTUALNY_STAN, 0 ILOSC_SPRZEDANA ";
            }
            else
            {
                sql += " select GM_TOWARY.KONTOFK, sum(GM_MAGAZYN.ILOSC) AKTUALNY_STAN, 0 ILOSC_SPRZEDANA ";
            }
            sql += " FROM GM_TOWARY ";
            sql += " left join GM_MAGAZYN on GM_TOWARY.ID_TOWARU=GM_MAGAZYN.ID_TOWAR ";
            sql += " left join GM_MAGAZYNY on GM_MAGAZYNY.ID=GM_MAGAZYN.MAGNUM ";
            sql += " left join R3_CONTACTS R3DOST on R3DOST.ID=GM_TOWARY.DOSTAWCA ";
            sql += " left join R3_CONTACTS R3PRODU on R3PRODU.ID=GM_TOWARY.PRODUCENT ";
            if (podstawoweGT.Length != 0)
	            sql += " left join GM_GRUPYT on GM_GRUPYT.ID=GM_TOWARY.GRUPA ";
            if (dowolneGT.Length != 0)
            {
                sql += " left join GM_GRUPYT_EXT_POW on GM_GRUPYT_EXT_POW.ID_TOWARU=GM_TOWARY.ID_TOWARU ";
                sql += " left join GM_GRUPYT_EXT on GM_GRUPYT_EXT_POW.ID_GRUPY=GM_GRUPYT_EXT.ID ";
            }
            
            tmpsql = "";
            if (magazyny.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and GM_MAGAZYNY.NUMER in (" + magazyny + ")";
                else
                    tmpsql += " GM_MAGAZYNY.NUMER in (" + magazyny + ")";
            }
            if (dowolneGT.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and GM_GRUPYT_EXT.NAZWA in (" + dowolneGT + ")";
                else
                    tmpsql += " GM_GRUPYT_EXT.NAZWA in (" + dowolneGT + ")";
            }
            if (podstawoweGT.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and GM_GRUPYT.NAZWA in (" + podstawoweGT + ")";
                else
                    tmpsql += " GM_GRUPYT.NAZWA in (" + podstawoweGT + ")";
            }
            if (dostawcy.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and R3DOST.SHORT_NAME in (" + dostawcy + ")";
                else
                    tmpsql += " R3DOST.SHORT_NAME in (" + dostawcy + ")";
            }
            if (producenci.Length != 0)
            {
                if (tmpsql.Length != 0)
                    tmpsql += " and R3PRODU.SHORT_NAME in (" + producenci + ")";
                else
                    tmpsql += " R3PRODU.SHORT_NAME in (" + producenci + ")";
            }
            if (tmpsql.Length > 0)
                sql += " where " + tmpsql;

            if (checkBoxPBindex.Checked)
            {
                sql += " group by  GM_TOWARY.SKROT ";
            }
            else
            {
                sql += " group by  GM_TOWARY.KONTOFK ";
            }

            sql += " union ";

            if (checkBoxPBindex.Checked)
            {
                sql += " select GM_TOWARY.SKROT, 0 AKTUALNY_STAN, sum(GM_FSPOZ.ILOSC) ILOSC_SPRZEDANA ";
            }
            else
            {
                sql += " select GM_TOWARY.KONTOFK, 0 AKTUALNY_STAN, sum(GM_FSPOZ.ILOSC) ILOSC_SPRZEDANA ";
            }
            sql += " from GM_FSPOZ ";
            sql += " join GM_TOWARY on GM_TOWARY.ID_TOWARU=GM_FSPOZ.ID_TOWARU ";
            sql += " join gm_fs on gm_fspoz.id_glowki=gm_fs.id ";
            sql += " left join GM_MAGAZYNY on GM_FS.MAGNUM=GM_MAGAZYNY.ID ";
            if (dowolneGT.Length != 0)
            {
            sql += " left join GM_GRUPYT_EXT_POW on GM_GRUPYT_EXT_POW.ID_TOWARU=GM_FSPOZ.ID_TOWARU ";
            sql += " left join GM_GRUPYT_EXT on GM_GRUPYT_EXT_POW.ID_GRUPY=GM_GRUPYT_EXT.ID ";
            }
            if (podstawoweGT.Length != 0)
                sql += " left join GM_GRUPYT on GM_GRUPYT.ID=GM_TOWARY.GRUPA ";
            sql += " left join R3_CONTACTS R3DOST on R3DOST.ID=GM_TOWARY.DOSTAWCA ";
            sql += " left join R3_CONTACTS R3PRODU on R3PRODU.ID=GM_TOWARY.PRODUCENT ";
            sql += " where gm_fs.data_wystawienia>='" + dateOD1.Text.ToString() + "' and gm_fs.data_wystawienia<='" + dateDO1.Text.ToString() + "'";
            if (magazyny.Length != 0)
                sql += " and GM_MAGAZYNY.NUMER in (" + magazyny + ")";
            if (dowolneGT.Length != 0)
                sql += " and GM_GRUPYT_EXT.NAZWA in (" + dowolneGT + ")";
            if (podstawoweGT.Length != 0)
                sql += " and GM_GRUPYT.NAZWA in (" + podstawoweGT + ")";
            if (dostawcy.Length != 0)
                sql += " and R3DOST.SHORT_NAME in (" + dostawcy + ")";
            if (producenci.Length != 0)
                sql += " and R3PRODU.SHORT_NAME in (" + producenci + ")";

            if (checkBoxPBindex.Checked)
            {
                sql += " group by  GM_TOWARY.SKROT ";
                sql += " ) a ";
                sql += " group by  SKROT ";
            }
            else
            {
                sql += " group by  GM_TOWARY.KONTOFK ";
                sql += " ) a "; 
                sql += " group by  KONTOFK ";
            }

            FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
            try
            {
                FbDataAdapter adapter = new FbDataAdapter(cdk);
                DataTable dt = new DataTable("RESULT");
                adapter.Fill(dt);
                fDataView = new DataView();
                fDataView.Table = dt;
                dataGridView1.DataSource = fDataView;

                bSaveFTPPowerbike.Enabled = true;
            }
            catch (FbException ex)
            {
                statusLable.Text = "Błąd wykonania raportu!";
                MessageBox.Show("Błąd wczytywania danych do okna z Raportu 1: " + ex.Message);
            }

            SetStatusKońcaRaportuNaPasku();
        }

        private void bSaveFTPPowerbike_Click(object sender, EventArgs e)
        {
            string wybrany_magazyn = "";
            bool zaDuzoMagazynow = false;

            foreach (var item in chMagazyny1.CheckedItems)
            {
                if (wybrany_magazyn.Length == 0)
                    wybrany_magazyn = "'" + item.ToString() + "'";
                else if (item.ToString().Equals("CENTR") && wybrany_magazyn.Contains("NOWY"))
                {
                    //jest ok
                }
                else if (item.ToString().Equals("NOWY") && wybrany_magazyn.Contains("CENTR"))
                {
                    //jest ok
                }
                else
                {
                    zaDuzoMagazynow = true;
                    break;
                }
            }

            if (!zaDuzoMagazynow)
            {
                StringBuilder builder = new StringBuilder();
                if (CzyDodacNaglowek.Checked)
                {
                    for (int i = 0; i < dataGridView1.ColumnCount; i++)
                        if (checkBoxKwalifik.Checked)
                        {
                            builder.AppendFormat(i == (dataGridView1.Columns.Count - 1) ? " \"{0}\"" : "\"{0}\";", dataGridView1.Columns[i].HeaderText);
                        }
                        else
                        {
                            builder.AppendFormat(i == (dataGridView1.Columns.Count - 1) ? " {0}" : "{0};", dataGridView1.Columns[i].HeaderText);
                        }
                    builder.AppendLine();
                }

                for (int i = 0; i < dataGridView1.RowCount - 1; i++)
                {

                    foreach (DataGridViewCell cell in dataGridView1.Rows[i].Cells)
                    {
                        if (cell.ValueType.ToString().Equals("System.Decimal"))
                        {
                            if (checkBoxKwalifik.Checked)
                            {
                                builder.AppendFormat(cell.ColumnIndex == (dataGridView1.Columns.Count - 1) ? "\"{0}\"" : "\"{0}\";", cell.Value.ToString().Replace(",", "."));
                            }
                            else
                            {
                                builder.AppendFormat(cell.ColumnIndex == (dataGridView1.Columns.Count - 1) ? "{0}" : "{0};", cell.Value.ToString().Replace(",", "."));
                            }
                        }
                        else
                        {
                            if (checkBoxKwalifik.Checked)
                            {
                                builder.AppendFormat(cell.ColumnIndex == (dataGridView1.Columns.Count - 1) ? "\"{0}\"" : "\"{0}\";", cell.Value);
                            }
                            else
                            {
                                builder.AppendFormat(cell.ColumnIndex == (dataGridView1.Columns.Count - 1) ? "{0}" : "{0};", cell.Value);
                            }
                        }
                    }
                    builder.AppendLine();
                }

                string file ="";
                try
                {
                    if (magazyny == "'KRAK'")
                        file = "N00780.csv"; //Kraków
                    else if (magazyny == "'WARS'")
                        file = "N04964.csv"; //Warszawa (Puławska)
                    else if (magazyny == "'PRZE'")
                        file = "N03885.csv"; //Przemyśl
                    else if (magazyny.Contains( "'NOWY'") && magazyny.Contains( "'CENTR'"))
                        file = "N00779.csv"; //Nowy Sącz magazyn główny i pomocniczy
                    else if (magazyny == "'NOWY'")
                        file = "N00779.csv"; //Nowy Sącz
                    else if (magazyny == "'WESO'")
                        file = "N05484.csv"; //N05484 Warszawa (Trakt Brzeski)
                    else if (magazyny == "'CENTR'")
                        file = "N05533.csv"; //N05533 Nowy Sącz (magazyn centrala)
                    else
                    {
                        file = "N00000.csv";
                        MessageBox.Show("Brak nazwy pliku dla tego magazynu, na serwer ftp zostanie zapisany plik " + file);
                    }
                    #region pobranie ustawienien do połączenia z serwerem FTP
                    RaksForPoverbike.SettingFile sf = new RaksForPoverbike.SettingFile();
                    string tAdresFTP = sf.AdresFTP;
                    string tUserFTP = sf.UserFTP;
                    string tPassFTP = sf.PassFTP;
                    #endregion

                    string mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\" + file ;
                    File.WriteAllText(mydocpath, builder.ToString());

                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + tAdresFTP + "//" + file);
                    request.Method = WebRequestMethods.Ftp.UploadFile;

                    request.Credentials = new NetworkCredential(tUserFTP, tPassFTP);

                    StreamReader sourceStream = new StreamReader(mydocpath);
                    byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
                    sourceStream.Close();
                    request.ContentLength = fileContents.Length;

                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(fileContents, 0, fileContents.Length);
                    requestStream.Close();

                    FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                    MessageBox.Show("Zapisano plik z raportem: " + file + " Status odpowiedzi serwera:" + response.StatusDescription);

                    response.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Bład zapisu pliku:" + file + " z raportem:" + ex.Message);
                    throw;
                }
            }
            else
            {
                MessageBox.Show("Wybrano za dużo magazynów, zapis raportu na serwer ftp przerwano!");
            }
        }

        private void bCheckFtpPowerBike_Click(object sender, EventArgs e)
        {
            OknoFTP of = new OknoFTP();
            of.Show();
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (tabControlParametry.SelectedTab.Name.Equals("tabAdmin"))
            {
                currUserId = 0;
                try
                {
                    currUserId = Convert.ToInt32(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value);
                    if (Convert.ToInt32(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["ISLOCK"].Value) == 0)
                    {
                        bUsrLock.Text = "Zablokuj użytkownika " + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["NAZWA"].Value.ToString();
                    }
                    else
                    {
                        bUsrLock.Text = "Odblokuj użytkownika " + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["NAZWA"].Value.ToString();
                    }
                    bSetPass.Text = "Nadaj hasło użytkownika " + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["NAZWA"].Value.ToString();
                    bResetPassUser.Text = "Resetuj hasło użytkownika " + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["NAZWA"].Value.ToString();
                }
                catch (Exception a)
                {
                    bUsrLock.Text = "Nie ustawiono...";
                    bSetPass.Text = "Nie ustawiono...";
                    bResetPassUser.Text = "Nie ustawiono...";
                }
            }
            else
            {

                if (radioButton1.Checked)
                {
                    labelCol.Text = dataGridView1.Columns[dataGridView1.CurrentCell.ColumnIndex].HeaderText;
                    toSearch.ReadOnly = false;
                    if ((labelCol.Text.Contains("STAN")) ||
                                labelCol.Text.Contains("DO_ZAM") ||
                                labelCol.Text.Contains("ILOSC"))
                    {
                        toSearch.Text = dataGridView1.CurrentCell.Value.ToString();
                    }
                    else
                    {
                        toSearch.Text = "%" + dataGridView1.CurrentCell.Value.ToString() + "%";
                    }
                }
                else
                {
                    labelCol2.Text = dataGridView1.Columns[dataGridView1.CurrentCell.ColumnIndex].HeaderText;
                    toSearch2.ReadOnly = false;
                    if ((labelCol2.Text.Contains("STAN")) ||
                                labelCol2.Text.Contains("DO_ZAM") ||
                                labelCol2.Text.Contains("ILOSC"))
                    {
                        toSearch2.Text = dataGridView1.CurrentCell.Value.ToString();
                    }
                    else
                    {
                        toSearch2.Text = "%" + dataGridView1.CurrentCell.Value.ToString() + "%";
                    }
                }
            }

        }

        private void toSearch_TextChanged(object sender, EventArgs e)
        {
            if (toSearch.Text.Length > 0)
            {
                try
                {
                    if ((labelCol.Text.Contains("STAN")) ||
                        labelCol.Text.Contains("DO_ZAM") ||
                        labelCol.Text.Contains("ILOSC"))
                    {
                        search1 = " " + labelCol.Text + " = " + toSearch.Text;
                        if (search2.Length > 0)
                            fDataView.RowFilter = search1 + " AND " + search2;
                        else
                            fDataView.RowFilter = search1;
                        dataGridView1.Refresh();
                    }
                    else
                    {
                        search1 = " " + labelCol.Text + " Like '" + toSearch.Text + "'";
                        if (search2.Length > 0)
                            fDataView.RowFilter = search1 + " AND " + search2;
                        else
                            fDataView.RowFilter = search1;
                        dataGridView1.Refresh();
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Pierwsze filtrowanie nie działa poprawnie na kolumnie " + labelCol.Text + " - proszę filtrować po innej kolumnie.");
                }
            }
        }

        private void toSearchClear_Click(object sender, EventArgs e)
        {
            fDataView.RowFilter = search2;
            search1 = "";
            dataGridView1.Refresh();
            toSearch.Text = "";
            toSearch.ReadOnly = true;
            labelCol.Text = "kliknij w kolumnę";
        }

        private void bSaveToRaksSQLClipboard_Click(object sender, EventArgs e)
        {
            OknoZapisDoSchowkaRaks okno = new OknoZapisDoSchowkaRaks(fbconn, ref dataGridView1, checkBoxIlosc1.Checked, tabControlParametry.SelectedTab.Name.ToString());
            okno.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Lokalizacja pliku konf.ini to: " + Directory.GetCurrentDirectory());
        }

        private void toSearchClear2_Click(object sender, EventArgs e)
        {
            fDataView.RowFilter = search1;
            search2 = "";
            dataGridView1.Refresh();
            toSearch2.Text = "";
            toSearch2.ReadOnly = true;
            labelCol2.Text = "kliknij w kolumnę";
        }

        private void toSearch2_TextChanged(object sender, EventArgs e)
        {
            if (toSearch2.Text.Length > 0)
            {
                try
                {
                    if ((labelCol2.Text.Contains("STAN")) ||
                        labelCol2.Text.Contains("DO_ZAM") ||
                        labelCol2.Text.Contains("ILOSC"))
                    {
                        search2 = " " + labelCol2.Text + " = " + toSearch2.Text;
                        if (search1.Length > 0)
                            fDataView.RowFilter = search1 + " AND " + search2;
                        else
                            fDataView.RowFilter = search2;
                        dataGridView1.Refresh();
                    }
                    else
                    {
                        search2 = " " + labelCol2.Text + " Like '" + toSearch2.Text + "'";
                        if (search1.Length > 0)
                            fDataView.RowFilter = search1 + " AND " + search2;
                        else
                            fDataView.RowFilter = search2;
                        dataGridView1.Refresh();
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Drugie filtrowanie nie działa poprawnie na kolumnie " + labelCol.Text + " - proszę filtrować po innej kolumnie.");
                }
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            //if (radioButton1.Checked)
            //    MessageBox.Show("Zaznaczony");
        }

        private void tabControlParametry_Selected(object sender, TabControlEventArgs e)
        {
            if (tabControlParametry.SelectedTab.Name.ToString() == "tabPageImportCSV")
            {
                stanSaveClip = bSaveToRaksSQLClipboard.Enabled;
                bSaveToRaksSQLClipboard.Enabled = true;
            }
            else
            {
                bSaveToRaksSQLClipboard.Enabled = stanSaveClip;
            }
        }

        private void bReadFileCSV_Click(object sender, EventArgs e)
        {
            OpenFileDialog dial = new OpenFileDialog();
            dial.Filter = "CSV files (*.csv)|*.csv";
            if (dial.ShowDialog()==DialogResult.OK || dial.ShowDialog()==DialogResult.Yes)
            {
                try
                {
                    DataTable dt = new DataTable("RESULT");
                    dt.Columns.Add(new DataColumn("SKROT",typeof(String)));
                    dt.Columns.Add(new DataColumn("CENA",typeof(Double)));

                    
                    string[] lines = System.IO.File.ReadAllLines(dial.FileName.ToString(), Encoding.Default);
                    foreach (string line in lines)
                    {
                        string[] tablica = line.Split(';');
                        // Use a tab to indent each line of the file.
                        Console.WriteLine("\t" + tablica[0].ToString() + ";;" + tablica[1].ToString());
                        DataRow workRow = dt.NewRow();
                        workRow["SKROT"] = tablica[0].ToString();
                        workRow["CENA"] = Convert.ToDouble(tablica[1].ToString());
                        dt.Rows.Add(workRow); 
                    }

                    fDataView = new DataView();
                    fDataView.Table = dt;
                    dataGridView1.DataSource = fDataView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Błąd importu pliku: " + ex.Message);
                    throw;
                }
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            bSaveToRaksSQLClipboard.Enabled = false;
            checkBoxIlosc1.Enabled = false;

            string sql = "select * ";
            sql += " from MM_USERS;";

            FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
            try
            {
                FbDataAdapter adapter = new FbDataAdapter(cdk);
                DataTable dt = new DataTable("RESULT");
                adapter.Fill(dt);
                fDataView = new DataView();
                fDataView.Table = dt;
                dataGridView1.DataSource = fDataView;
                dataGridView1.Columns[0].Visible = false;
            }
            catch (FbException ex)
            {
                statusLable.Text = "Błąd wczytania listy użytkowników!";
                MessageBox.Show("Błąd wczytywania danych o uzytkownikach do okna Raportu: " + ex.Message);
            }
        }

        private void bAddUser_Click(object sender, EventArgs e)
        {
            bool wynik = false;
            Int32 gen_id = 0;

            string magazyny = "";
            foreach (var item in chMagazynyAdmin.CheckedItems)
            {
                if (magazyny.Length == 0)
                    magazyny = item.ToString() ;
                else
                    magazyny += "," + item.ToString();
            }
            StringBuilder myStringBuilder = new StringBuilder();

            if (czyUserWczytany)
            {
                myStringBuilder.Append("UPDATE MM_USERS SET ");
                myStringBuilder.Append("KOD='" + tbUsrLogin.Text + "', ");
                myStringBuilder.Append("NAZWA='" + tbUsrName.Text + "', ");
                myStringBuilder.Append("ISADMIN=" + (cUsrAdmin.Checked ? 1 : 0) + ", ");
                myStringBuilder.Append("MAGAZYNY='" + magazyny + "' ");
                myStringBuilder.Append(" WHERE ID=" + currUserId + "; ");
            }
            else
            {
                #region pobranie ID z generatora
                FbCommand gen_id_statement = new FbCommand("SELECT GEN_ID(MM_USERS_GEN,1) from rdb$database", fbconn.getCurentConnection());
                try
                {
                    gen_id = Convert.ToInt32(gen_id_statement.ExecuteScalar());
                }
                catch (FbException exgen)
                {
                    MessageBox.Show("Błąd pobierania nowego ID dla MM_USERS z bazy. Operacje przerwano! " + exgen.Message);
                    throw;
                }
                #endregion

                myStringBuilder.Append("INSERT INTO MM_USERS (");
                myStringBuilder.Append("ID, ");
                myStringBuilder.Append("KOD, ");
                myStringBuilder.Append("NAZWA, ");
                myStringBuilder.Append("ISADMIN, ");
                myStringBuilder.Append("MAGAZYNY ");

                myStringBuilder.Append(") VALUES ( ");

                myStringBuilder.Append(gen_id.ToString() + ",");  // ID
                myStringBuilder.Append("'" + tbUsrLogin.Text.ToString() + "',");  // KOD
                myStringBuilder.Append("'" + tbUsrName.Text.ToString() + "', ");  // NAZWA
                myStringBuilder.Append((cUsrAdmin.Checked ? "1," : "0,"));  // ISADMIN
                myStringBuilder.Append(" '" + magazyny + "' ");  // MAGAZYNY
                myStringBuilder.Append(");");
            }

            FbCommand cdk = new FbCommand(myStringBuilder.ToString(), fbconn.getCurentConnection());
            try
            {
                cdk.ExecuteScalar();
                wynik = true;
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd zapisu nowego użytkownika do bazy RaksSQL: " + ex.Message);
            }

            if (wynik)
            {
                bUsrClear.PerformClick();
                button2.PerformClick();
            }
        }

        private void bUsrLock_Click(object sender, EventArgs e)
        {
            string sql = "";

            if (bUsrLock.Text.StartsWith("Zablo"))
            {
                sql = "update MM_USERS set ISLOCK=1, MODYFIKOWANY='NOW' where ID=" + currUserId + ";";
            }
            else
            {
                sql = "update MM_USERS set ISLOCK=0, MODYFIKOWANY='NOW' where ID=" + currUserId + ";";
            }

            FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
            try
            {
                cdk.ExecuteScalar();
                MessageBox.Show("Zmieniono status!");
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd zapisu statusu blokady użytkownika do bazy RaksSQL: " + ex.Message);
            }

            button2.PerformClick();
        }

        private void bSetPass_Click(object sender, EventArgs e)
        {
            SetChangePassForCurrentUser();
        }

        private void SetChangePassForCurrentUser()
        {
            Autentykacja at = new Autentykacja(fbconn, currUserId);
            if (at.SetNewPassByUser() == AutoryzationType.PassChanged)
            {
                MessageBox.Show("Zmiana przeprowadzona prawidłowo", "Zmiana hasła");
            }
            else
            {
                MessageBox.Show("Zmianę hasła anulowano!", "Zmiana hasła");
            }
        }

        private void bResetPassUser_Click(object sender, EventArgs e)
        {
            Autentykacja at = new Autentykacja(fbconn, currUserId);
            at.SetResetPass();
            if (at.GetAutoryzationResult().Equals(AutoryzationType.PassChanged))
            {
                MessageBox.Show("Zresetowano hasło do pustego poprawnie.","Reset hasła");
            }
            else
            {
                MessageBox.Show("Resetowanie hasła przerwano. Operacja anulowana.", "Reset hasła");
            }
        }

        private void bChangeMyPass_Click(object sender, EventArgs e)
        {
            SetChangePassForCurrentUser();
        }

        private void bReadUser_Click(object sender, EventArgs e)
        {
            Autentykacja at = new Autentykacja(fbconn, currUserId);
            string[] tab = at.GetUserPropertiesByID(currUserId);
            tbUsrLogin.Text = tab[0].ToString();
            cUsrAdmin.Checked = (tab[1].ToString().Equals("true") ? true : false);
            tbUsrName.Text = tab[2].ToString();

            string rightMagazyny = tab[3].ToString();

            for (int i = 0; i < chMagazynyAdmin.Items.Count; i++)
            {

                if (rightMagazyny.Contains(chMagazynyAdmin.Items[i].ToString()))
                {

                    chMagazynyAdmin.SetItemChecked(i, true);
                }
                else
                {
                    chMagazynyAdmin.SetItemChecked(i, false);
                }

            }
            czyUserWczytany = true;
        }

        private void bUsrClear_Click(object sender, EventArgs e)
        {
            czyUserWczytany = false;
            tbUsrLogin.Text = "";
            tbUsrName.Text = "";
            cUsrAdmin.Checked = false;
            for (int i = 0; i < chMagazynyAdmin.Items.Count; i++)
            {
                chMagazynyAdmin.SetItemChecked(i, false);
            }
        }
    }
}
