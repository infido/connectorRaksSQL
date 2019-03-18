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
        DateTime mark;
        DataView fDataView;

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
                onLoadWindow();
            }
            else
            {
                MessageBox.Show("Brak połączenia do bazy danych RaksSQL");
            }
        }

        private void onLoadWindow()
        {
            FbCommand cdk = new FbCommand("SELECT NUMER from GM_MAGAZYNY order by NUMER;", fbconn.getCurentConnection());
            try
            {
                FbDataReader fdk = cdk.ExecuteReader();
                while (fdk.Read())
                {
                    chMagazyny1.Items.Add((string)fdk["NUMER"]);
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
            SetStatusStartuRaportu(DateTime.Now);
            SetWartosciParametrowDlaWhere();
            bSaveToRaksSQLClipboard.Enabled = false;

            string sql = "select * from (";
            sql += " select ";
            sql += " GM_FSPOZ.SKROT_ORYGINALNY INDEKS,";
            sql += " GM_FSPOZ.NAZWA_ORYGINALNA NAZWA,";
            sql += " GM_FSPOZ.ILOSC,";
            sql += " GM_WZPOZ.CENA_ZAKUPU_PO CENA_ZAKUPU_NETTO," ;
            sql += " GM_FSPOZ.CENA_SP_PLN_NETTO CENA_SPRZEDAZY_NETTO,";
            sql += " IIF (GM_FSPOZ.CENA_SP_PLN_NETTO = 0, -1,((GM_FSPOZ.CENA_SP_PLN_NETTO-GM_WZPOZ.CENA_ZAKUPU_PO)/GM_FSPOZ.CENA_SP_PLN_NETTO)) MARZA,";
            sql += " ((GM_FSPOZ.ILOSC*GM_FSPOZ.CENA_SP_PLN_NETTO)-(GM_FSPOZ.ILOSC*GM_WZPOZ.CENA_ZAKUPU_PO)) ZYSK_NETTO,";
            sql += " gm_fs.OPERATOR,";
            sql += " GM_MAGAZYNY.NUMER MAGAZYN ";
            sql += " from GM_FSPOZ";
            sql += " left join GM_WZPOZ on GM_FSPOZ.ID = GM_WZPOZ.ID_FSPOZ";
            sql += " join gm_fs on gm_fspoz.id_glowki=gm_fs.id ";
            sql += " join GM_TOWARY on GM_TOWARY.ID_TOWARU=GM_FSPOZ.ID_TOWARU ";
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
                sql += " join R3_CONTACTS R3DOST on R3DOST.ID=GM_TOWARY.DOSTAWCA ";
            }
            if (producenci.Length != 0)
            {
                sql += " join R3_CONTACTS R3PRODU on R3PRODU.ID=GM_TOWARY.PRODUCENT ";
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
            sql += " GM_KSPOZ.ILOSC_PO - GM_KSPOZ.ILOSC_PRZED as ILOSC,";
            //sql += " GM_WZPOZ.CENA_ZAKUPU_PO CENA_ZAKUPU_NETTO,";
            sql += " 0 CENA_ZAKUPU_NETTO,";
            //sql += " GM_KSPOZ.CENA_SP_PLN_NETTO_PO - GM_KSPOZ.CENA_SP_PLN_NETTO_PRZED CENA_SPRZEDAZY_NETTO,";
            sql += " IIF ((GM_KSPOZ.CENA_SP_PLN_NETTO_PO - GM_KSPOZ.CENA_SP_PLN_NETTO_PRZED)=0,GM_KSPOZ.CENA_SP_PLN_NETTO_PO,GM_KSPOZ.CENA_SP_PLN_NETTO_PO - GM_KSPOZ.CENA_SP_PLN_NETTO_PRZED) as CENA_SPRZEDAZY_NETTO, ";

            sql += " IIF (GM_KSPOZ.CENA_SP_PLN_NETTO_PO = 0, -1,((GM_KSPOZ.CENA_SP_PLN_NETTO_PO-";
            //sql += "( select first 1 CENA_ZAKUPU_PO from GM_WZPOZ where ID_TOWARU=000  order by DATA_ZAKUPU asc )";
            sql += "0";
            sql += ")/GM_KSPOZ.CENA_SP_PLN_NETTO_PO)) MARZA,";
            //sql += " ((GM_FSPOZ.ILOSC*GM_FSPOZ.CENA_SP_PLN_NETTO)-(GM_FSPOZ.ILOSC*GM_WZPOZ.CENA_ZAKUPU_PO)) ZYSK_NETTO,";
            sql += " 0 ZYSK_NETTO,";

            sql += " gm_ks.OPERATOR,";
            sql += " GM_MAGAZYNY.NUMER MAGAZYN ";
            sql += " from GM_KSPOZ";
            //sql += " left join GM_WZPOZ on GM_KSPOZ.ID = GM_WZPOZ.ID_KSPOZ";
            sql += " join gm_ks on gm_kspoz.id_glowki=gm_ks.id ";
            sql += " join GM_TOWARY on GM_TOWARY.ID_TOWARU=GM_KSPOZ.ID_TOWARU ";
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
                        builder.AppendFormat(i == (dataGridView1.Columns.Count - 1) ? " {0}" : "{0};", dataGridView1.Columns[i].HeaderText);
                    builder.AppendLine();
                }

                for (int i = 0; i < dataGridView1.RowCount-1; i++)
                {

                    foreach (DataGridViewCell cell in dataGridView1.Rows[i].Cells)
                    {
                        if (cell.ValueType.ToString().Equals("System.Decimal"))
                        {
                            builder.AppendFormat(cell.ColumnIndex == (dataGridView1.Columns.Count - 1) ? "{0}" : "{0};", cell.Value.ToString().Replace(",","."));
                        }
                        else
                        {
                            builder.AppendFormat(cell.ColumnIndex == (dataGridView1.Columns.Count - 1) ? "{0}" : "{0};", cell.Value);
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
            SetStatusStartuRaportu(DateTime.Now);
            SetWartosciParametrowDlaWhere();
            bSaveToRaksSQLClipboard.Enabled = false;

            string sql = " select MAGAZYN, INDEKS, NAZWA, sum(ILOSC) as ILOSC, sum(STANMIN) as STANMIN, sum(STANMAX) as STANMAX, DOSTAWCA, PRODUCENT from ( ";

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
            sql += " R3PRODU.SHORT_NAME PRODUCENT";
            sql += " from GM_TOWARY";
            sql += " left join GM_FSPOZ on GM_TOWARY.ID_TOWARU=GM_FSPOZ.ID_TOWARU ";
            sql += " left join GM_WZPOZ on GM_FSPOZ.ID = GM_WZPOZ.ID_FSPOZ";
            sql += " left join gm_fs on gm_fspoz.id_glowki=gm_fs.id ";
            sql += " left join GM_MAGAZYNY on GM_FS.MAGNUM=GM_MAGAZYNY.ID ";
            sql += " left join R3_CONTACTS R3DOST on R3DOST.ID=GM_TOWARY.DOSTAWCA ";
            sql += " left join R3_CONTACTS R3PRODU on R3PRODU.ID=GM_TOWARY.PRODUCENT ";

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
            sql += " R3PRODU.SHORT_NAME PRODUCENT";
            sql += " from GM_TOWARY";
            sql += " left join GM_KSPOZ on GM_TOWARY.ID_TOWARU=GM_KSPOZ.ID_TOWARU ";
            //sql += " left join GM_WZPOZ on GM_KSPOZ.ID = GM_WZPOZ.ID_KSPOZ";
            sql += " left join gm_ks on gm_kspoz.id_glowki=gm_ks.id ";
            sql += " left join GM_MAGAZYNY on GM_KS.MAGNUM=GM_MAGAZYNY.ID ";
            sql += " left join R3_CONTACTS R3DOST on R3DOST.ID=GM_TOWARY.DOSTAWCA ";
            sql += " left join R3_CONTACTS R3PRODU on R3PRODU.ID=GM_TOWARY.PRODUCENT ";

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

            sql += " ) group by MAGAZYN, INDEKS, NAZWA, DOSTAWCA, PRODUCENT; ";

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
            SetStatusStartuRaportu(DateTime.Now);
            SetWartosciParametrowDlaWhere();
            bSaveToRaksSQLClipboard.Enabled = true;

            string sql = " ";
            sql += " select SKROT, NAZWA, sum(STAN_MAG) STAN_MAG, sum(STANMIN) STAN_MIN, sum(STANMAX) STAN_MAX,  ";
            sql += " IIF((sum(STANMIN)-sum(STAN_MAG))<0, ";
            sql += " IIF((sum(STANMAX)-sum(STAN_MAG))<0,(sum(STANMAX)-sum(STAN_MAG)),0),(sum(STANMIN)-sum(STAN_MAG)) ) DO_ZAMOWIENIA,DOST DOSTAWCA, PRODU PRODUCENT, LOKALIZACJA ";
            sql += " FROM ( ";
            sql += " SELECT GM_TOWARY.SKROT, GM_TOWARY.NAZWA, 0 STAN_MAG, GM_TOWARY.STANMIN, GM_TOWARY.STANMAX, R3DOST.SHORT_NAME DOST, R3PRODU.SHORT_NAME PRODU, GM_LOKALIZACJE.NAZWA LOKALIZACJA ";
            sql += " FROM GM_TOWARY   ";
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
            sql += " SELECT GM_TOWARY.SKROT , GM_TOWARY.NAZWA, sum(GM_MAGAZYN.ILOSC) STAN_MAG, 0 STANMIN, 0 STANMAX, R3DOST.SHORT_NAME DOST, R3PRODU.SHORT_NAME PRODU, GM_LOKALIZACJE.NAZWA LOKALIZACJA  ";
            sql += " FROM GM_MAGAZYN ";
            sql += " join GM_TOWARY on GM_TOWARY.ID_TOWARU=GM_MAGAZYN.ID_TOWAR ";
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

            sql += " group by  GM_MAGAZYN.MAGNUM, GM_TOWARY.SKROT,GM_TOWARY.NAZWA,R3DOST.SHORT_NAME, R3PRODU.SHORT_NAME, GM_LOKALIZACJE.NAZWA) a ";
            sql += " group by SKROT, NAZWA, DOSTAWCA, PRODUCENT, LOKALIZACJA ";
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
            SetStatusStartuRaportu(DateTime.Now);
            SetWartosciParametrowDlaWhere();
            bSaveToRaksSQLClipboard.Enabled = false;

            string sql = " ";
            sql += " select SKROT, sum(AKTUALNY_STAN) STAN_MAGAZYNU, sum(ILOSC_SPRZEDANA) SPRZEDANE_DNIA ";
            sql += " from ( ";

            sql += " select GM_TOWARY.SKROT, 0 AKTUALNY_STAN, 0 ILOSC_SPRZEDANA ";
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

            sql += " group by  GM_TOWARY.SKROT ";


            sql += " union ";


            sql += " select GM_TOWARY.SKROT, sum(GM_MAGAZYN.ILOSC) AKTUALNY_STAN, 0 ILOSC_SPRZEDANA ";
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

            sql += " group by  GM_TOWARY.SKROT ";

            sql += " union ";

            sql += " select GM_TOWARY.SKROT, 0 AKTUALNY_STAN, sum(GM_FSPOZ.ILOSC) ILOSC_SPRZEDANA ";
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
            sql += " group by  GM_TOWARY.SKROT ";
            
            sql += " ) a ";
            sql += " group by SKROT ";

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
                        builder.AppendFormat(i == (dataGridView1.Columns.Count - 1) ? " {0}" : "{0};", dataGridView1.Columns[i].HeaderText);
                    builder.AppendLine();
                }

                for (int i = 0; i < dataGridView1.RowCount - 1; i++)
                {

                    foreach (DataGridViewCell cell in dataGridView1.Rows[i].Cells)
                    {
                        if (cell.ValueType.ToString().Equals("System.Decimal"))
                        {
                            builder.AppendFormat(cell.ColumnIndex == (dataGridView1.Columns.Count - 1) ? "{0}" : "{0};", cell.Value.ToString().Replace(",", "."));
                        }
                        else
                        {
                            builder.AppendFormat(cell.ColumnIndex == (dataGridView1.Columns.Count - 1) ? "{0}" : "{0};", cell.Value);
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
                        fDataView.RowFilter = " " + labelCol.Text + " = " + toSearch.Text;
                        dataGridView1.Refresh();
                    }
                    else
                    {
                        fDataView.RowFilter = " " + labelCol.Text + " Like '" + toSearch.Text + "'";
                        dataGridView1.Refresh();
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Filtrowanie nie działa poprawnie na kolumnie " + labelCol.Text + " - proszę filtrować po innej kolumnie.");
                }
            }
        }

        private void toSearchClear_Click(object sender, EventArgs e)
        {
            fDataView.RowFilter = " " ;
            dataGridView1.Refresh();
            toSearch.Text = "";
            toSearch.ReadOnly = true;
            labelCol.Text = "kliknij w kolumnę";
        }

        private void bSaveToRaksSQLClipboard_Click(object sender, EventArgs e)
        {
            OknoZapisDoSchowkaRaks okno = new OknoZapisDoSchowkaRaks(fbconn,ref dataGridView1);
            okno.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Lokalizacja pliku konf.ini to: " + Directory.GetCurrentDirectory());
        }
    }
}
