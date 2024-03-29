﻿using System;
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
using System.Diagnostics;
using CsvHelper;
using System.Globalization;
using LinqToDB.Common;

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
            WaitStarting ws = new WaitStarting();
            InitializeComponent();
            Text += " " + Application.ProductVersion;
            fbconn = new FBConn();
            ws.Close();
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
                        if (logToSys.GetCurrentUserLogin().Equals("SABINA") || logToSys.GetCurrentUserLogin().Equals("HONORATA"))
                            Text += " KSIĘGOWOŚĆ"; //workaround dla roli księgowość
                        else
                            tabControlParametry.TabPages.Remove((TabPage)tabControlParametry.TabPages["tabKasaBank"]);
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
                OknoKonfiguracjiPolaczenia ok = new OknoKonfiguracjiPolaczenia();
                ok.ShowDialog();
                Application.Exit();
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

            Dictionary<int, string> listCeny = new Dictionary<int, string>();
            cdk.CommandText = ("SELECT ID,NAZWA FROM GM_CENY;");
            try
            {
                FbDataReader fdk = cdk.ExecuteReader();
                while (fdk.Read())
                {
                    listCeny.Add((int)fdk["ID"], (string)fdk["NAZWA"]);
                }
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd wczytywania listy magazynów: " + ex.Message);
            }

            cCena.DataSource = new BindingSource(listCeny, null);
            cCena.DisplayMember = "Value";
            cCena.ValueMember = "Key";
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
            sql += "IIF (GM_KSPOZ.CENA_SP_PLN_NETTO_PRZED=0, -1, ((GM_KSPOZ.CENA_SP_PLN_NETTO_PRZED-GM_WZPOZ.CENA_ZAKUPU_PO) /GM_KSPOZ.CENA_SP_PLN_NETTO_PRZED))";
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
            //FbCommand update_towar = new FbCommand("ALTER TABLE GM_TOWARY ALTER SKROT TYPE STRING25_D;", fbconn.getCurentConnection());
            //try
            //{
            //    update_towar.ExecuteNonQuery();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Test" + ex.Message);
            //    throw;
            //}
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
                catch (Exception)
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
                    else if(labelCol.Text.Contains("DO_AKTUALIZACJI"))
                    {
                        if ((bool)dataGridView1.CurrentCell.Value)
                        {
                            search1 = " " + labelCol.Text + " = " + 1;
                        }
                        else
                        {
                            search1 = " " + labelCol.Text + " = " + 0;
                        }
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
            OknoZapisDoSchowkaRaks okno = new OknoZapisDoSchowkaRaks(fbconn, ref dataGridView1, checkBoxIlosc1.Checked, tabControlParametry.SelectedTab.Name.ToString(), rbSkrot.Checked);
            okno.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Lokalizacja pliku konf.ini to: " + Directory.GetCurrentDirectory() + "  Czy edytować plik ini?","Konfiguracja ini", MessageBoxButtons.YesNo)==DialogResult.Yes)
            {
                //edycja pliku ini
                Process.Start(Directory.GetCurrentDirectory() + "\\konf.ini");
            }
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
                    if (rbSkrot.Checked)
                        dt.Columns.Add(new DataColumn("SKROT",typeof(String)));
                    else
                        dt.Columns.Add(new DataColumn("KONTOFK", typeof(String)));

                    dt.Columns.Add(new DataColumn("CENA",typeof(Double)));

                    
                    string[] lines = System.IO.File.ReadAllLines(dial.FileName.ToString(), Encoding.Default);
                    foreach (string line in lines)
                    {
                        string[] tablica = line.Split(';');
                        // Use a tab to indent each line of the file.
                        Console.WriteLine("\t" + tablica[0].ToString() + ";;" + tablica[1].ToString());
                        DataRow workRow = dt.NewRow();

                        if (rbSkrot.Checked)
                            workRow["SKROT"] = tablica[0].ToString();
                        else
                            workRow["KONTOFK"] = tablica[0].ToString();

                        workRow["CENA"] = Convert.ToDouble(tablica[1].ToString());

                        if (rbSkrot.Checked)
                        {
                            if (workRow["SKROT"].ToString() != "")
                            {
                                dt.Rows.Add(workRow);
                            }
                        }
                        else
                        {
                            if (workRow["KONTOFK"].ToString() != "")
                            {
                                dt.Rows.Add(workRow);
                            }
                        }
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
                button2.PerformClick();
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
                button2.PerformClick();
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
            if (tab[1].ToString().Equals("True") || tab[1].ToString().Equals("1"))
            {
                cUsrAdmin.Checked = true;
            }else
            {
                cUsrAdmin.Checked = false;
            }
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

        private void bConnectionSet_Click(object sender, EventArgs e)
        {
            OknoKonfiguracjiPolaczenia conDef = new OknoKonfiguracjiPolaczenia();
            conDef.ShowDialog();
        }

        private void bReadFileForHeaders_Click(object sender, EventArgs e)
        {
            if (openFileDialogCSV.ShowDialog()==DialogResult.OK)
            {
                string pathToFile = openFileDialogCSV.FileName;
                lFilePath.Text = pathToFile;

                using (var reader = new StreamReader(pathToFile))
                using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
                {
                    // Do any configuration to `CsvReader` before creating CsvDataReader.
                    csv.Configuration.BadDataFound = null;
                    csv.Configuration.Delimiter = ",";
                    csv.Configuration.HasHeaderRecord = true;


                    using (var dr = new CsvDataReader(csv))
                    {
                        var dt = new DataTable();
                        dt.Load(dr);
                        dataGridView1.DataSource = dt;

                        Dictionary<int, string> listaKolumn = new Dictionary<int, string>();
                        try
                        {
                            int ndx = 1;
                            foreach (DataColumn kolumna in dt.Columns)
                            {
                                listaKolumn.Add(ndx, kolumna.ColumnName);
                                ndx++;
                            }

                        }
                        catch (FbException ex)
                        {
                            MessageBox.Show("Błąd wczytywania listy kolumn do combobox-ów: " + ex.Message);
                        }

                        cMapKodTowaru.DataSource = new BindingSource(listaKolumn, null);
                        cMapKodTowaru.DisplayMember = "Value";
                        cMapKodTowaru.ValueMember = "Key";

                        cMapGTIN.DataSource = new BindingSource(listaKolumn, null);
                        cMapGTIN.DisplayMember = "Value";
                        cMapGTIN.ValueMember = "Key";

                        cMapNetto.DataSource = new BindingSource(listaKolumn, null);
                        cMapNetto.DisplayMember = "Value";
                        cMapNetto.ValueMember = "Key";

                        cMapBrutto.DataSource = new BindingSource(listaKolumn, null);
                        cMapBrutto.DisplayMember = "Value";
                        cMapBrutto.ValueMember = "Key";
                    }
                }
                bGTINcheck.Visible = true;
                bGTINupdate.Visible = false;
            }
            else
            {
                lFilePath.Text = "Zrezygnowano ze wskazania pliku cennika do imporu...";
            }
        }

        private void bSaveGenCen_Click(object sender, EventArgs e)
        {
            //int idNag;

            #region pobranie nowego id nagłówka z bazy
            //FbCommand gen_id_nag = new FbCommand("SELECT GEN_ID(GM_GENCEN_GEN,1) from rdb$database", fbconn.getCurentConnection());
            //try
            //{
            //    idNag = Convert.ToInt32(gen_id_nag.ExecuteScalar());
            //}
            //catch (FbException exgen)
            //{
            //    MessageBox.Show("Błąd pobierania nowego ID z bazy. Operacje przerwano! " + exgen.Message);
            //    throw;
            //}
            #endregion

            //StringBuilder myStringBuilder = new StringBuilder("INSERT INTO GM_GENCEN (");
            //myStringBuilder.Append("ID, ");
            //myStringBuilder.Append("ROK, ");
            //myStringBuilder.Append("MIESIAC, ");
            //myStringBuilder.Append("NAZWA_CENNIKA, ");
            //myStringBuilder.Append("OPERATOR, ");
            //myStringBuilder.Append("ZMIENIL, "); 
            //myStringBuilder.Append("GUID ");

            //myStringBuilder.Append(") VALUES ( ");

            //myStringBuilder.Append(idNag.ToString() + ",");  // ID
            //myStringBuilder.Append( DateTime.Now.Year.ToString() + ", ");  //ROK
            //myStringBuilder.Append(DateTime.Now.Month.ToString() + ", ");  //MIESIAC
            //myStringBuilder.Append("'" + tNazwaCennika.Text + "', ");  //NAZWA_CENNIKA

            //myStringBuilder.Append("'ADMIN', ");  // OPERATOR
            //myStringBuilder.Append("'ADMIN', ");  // ZMIENIL

            //System.Guid gd = new System.Guid();
            //myStringBuilder.Append("'" + gd.ToString("D") +  "'); ");  //GUID
            

            //FbCommand cdk = new FbCommand(myStringBuilder.ToString(), fbconn.getCurentConnection());
            //try
            //{
            //    cdk.ExecuteScalar();
            //}
            //catch (FbException ex)
            //{
            //    MessageBox.Show("Błąd zapisu nagłówka cennika: " + ex.Message);
            //}




            #region obliczanie ID towaru z Indeksu
            //string idtow = "0";
            //FbCommand gen_id_towaru = new FbCommand("SELECT ID_TOWARU from GM_TOWARY where SKROT='" + row.Cells["SKROT"].Value.ToString() + "';", fbconn.getCurentConnection());
            //try
            //{
            //    idtow = (gen_id_towaru.ExecuteScalar()).ToString();
            //}
            //catch (FbException exgen)
            //{
            //    MessageBox.Show("Błąd pobierania nowego ID_TOWARU na podstawie skrótu" + row.Cells["SKROT"].Value.ToString() + " . Operacje przerwano! " + exgen.Message);
            //    throw;
            //}

            #endregion
        }

        private void bGTINcheck_Click(object sender, EventArgs e)
        {
            DataSet fdsr = new DataSet();
            fdsr.Tables.Add("TAB");
            fdsr.Tables["TAB"].Columns.Add("SKROT", typeof(String));
            fdsr.Tables["TAB"].Columns.Add("ARCHIWALNY", typeof(int));
            fdsr.Tables["TAB"].Columns.Add("STAN_W_RAKS", typeof(Double));
            fdsr.Tables["TAB"].Columns.Add("GTIN_W_RAKS", typeof(String));
            fdsr.Tables["TAB"].Columns.Add("NOWY_GTIN", typeof(String));
            fdsr.Tables["TAB"].Columns.Add("DO_AKTUALIZACJI", typeof(bool));

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells[cMapKodTowaru.Text].Value != null)
                {
                    #region obliczanie ID towaru z Indeksu
                    string stary = "BRAK INDEKSU";
                    Double stan = 0.0;
                    int arch;
                    FbCommand gen_id_towaru = new FbCommand("SELECT GTIN from GM_TOWARY where SKROT='" + row.Cells[cMapKodTowaru.Text].Value.ToString() + "';", fbconn.getCurentConnection());
                    FbCommand get_archiv_towaru = new FbCommand("SELECT ARCHIWALNY from GM_TOWARY where SKROT='" + row.Cells[cMapKodTowaru.Text].Value.ToString() + "';", fbconn.getCurentConnection());
                    FbCommand get_stan_towaru = new FbCommand("SELECT sum(ILOSC) from GM_MAGAZYN join GM_TOWARY on GM_TOWARY.ID_TOWARU=GM_MAGAZYN.ID_TOWAR where GM_TOWARY.SKROT='" + row.Cells[cMapKodTowaru.Text].Value.ToString() + "';", fbconn.getCurentConnection());
                    try
                    {
                        if (gen_id_towaru.ExecuteScalar() != null)
                        {
                            stary = (gen_id_towaru.ExecuteScalar()).ToString();
                            arch = Convert.ToInt16(get_archiv_towaru.ExecuteScalar());
                            if (get_stan_towaru.ExecuteScalar() != DBNull.Value)
                            {
                                stan = Convert.ToDouble(get_stan_towaru.ExecuteScalar());
                            }
                        }
                        else
                        {
                            arch = -2;
                        }
                    }
                    catch (FbException exgen)
                    {
                        MessageBox.Show("Błąd pobierania nowego ID_TOWARU na podstawie skrótu" + row.Cells["SKROT"].Value.ToString() + " . Operacje przerwano! " + exgen.Message);
                        throw;
                    }

                    #endregion
                    fdsr.Tables["TAB"].Rows.Add(row.Cells[cMapKodTowaru.Text].Value.ToString(), arch, stan, stary, row.Cells[cMapGTIN.Text].Value.ToString(), (!stary.Equals(row.Cells[cMapGTIN.Text].Value.ToString()) && !stary.Equals("BRAK INDEKSU")) );
                }
            }

            fDataView = new DataView();
            fDataView.Table = fdsr.Tables["TAB"];
            dataGridView1.DataSource = fDataView;

            bGTINupdate.Visible = true;
            bGTINcheck.Visible = false;
        }

        private void bGTINupdate_Click(object sender, EventArgs e)
        {
            int przetworzono = 0; 
            int zaktualizowano = 0;

            foreach (DataGridViewRow item in dataGridView1.Rows)
            {
                przetworzono++;
                if (item.Cells["DO_AKTUALIZACJI"].Value!=null && (bool)item.Cells["DO_AKTUALIZACJI"].Value && !item.Cells["SKROT"].Value.ToString().IsNullOrEmpty())
                {
                    FbCommand update_towar = new FbCommand("UPDATE GM_TOWARY set GTIN='" + item.Cells["NOWY_GTIN"].Value.ToString() + "' where SKROT='" + item.Cells["SKROT"].Value.ToString() + "';", fbconn.getCurentConnection());
                    try
                    {
                        update_towar.ExecuteScalar();
                        zaktualizowano++;
                    }
                    catch (Exception exp)
                    {
                        MessageBox.Show("Błąd aktualizacji pola GTIN na towarze o indeksie: " + item.Cells["SKROT"].Value.ToString() + System.Environment.NewLine + exp.Message);
                        throw;
                    }
                }
            }
            MessageBox.Show("Przetworzono: " + przetworzono + " indeksów, a zaktualizowano: " + zaktualizowano);
            bGTINupdate.Visible = false;
        }

        private void bImportFakturEPP_Click(object sender, EventArgs e)
        {
            OpenFileDialog dial = new OpenFileDialog();
            dial.Filter = "EPP files (*.epp)|*.epp";
            if (cbWyczyscOknoLoga.Checked)
                tPodsumowanieZeSchowka.Text = "";

            if (dial.ShowDialog() == DialogResult.OK || dial.ShowDialog() == DialogResult.Yes)
            {
                tPodsumowanieZeSchowka.Text += "Krok 1 " + DateTime.Now.ToString() + " Wczytanie pliku: " + dial.FileName + System.Environment.NewLine;
                try
                {
                    DataTable dt = new DataTable("PLIK");
                    dt.Columns.Add(new DataColumn("SEKCJA", typeof(String)));
                    dt.Columns.Add(new DataColumn("TYP", typeof(String)));
                    dt.Columns.Add(new DataColumn("LINIA", typeof(String)));
                    dt.Columns.Add(new DataColumn("LP", typeof(int)));
                    dt.Columns.Add(new DataColumn("SKROT", typeof(String)));
                    dt.Columns.Add(new DataColumn("KOD_KRESKOWY", typeof(String)));
                    //dt.Columns.Add(new DataColumn("KOD_KRESKOWY_W_RAKS", typeof(String)));
                    dt.Columns.Add(new DataColumn("NAZWA", typeof(String)));
                    dt.Columns.Add(new DataColumn("JM", typeof(String)));
                    dt.Columns.Add(new DataColumn("ILOSC", typeof(Decimal)));
                    dt.Columns.Add(new DataColumn("PROC_VAT", typeof(Decimal)));
                    dt.Columns.Add(new DataColumn("CENA_KAT_NETTO", typeof(Decimal)));
                    dt.Columns.Add(new DataColumn("CENA_KAT_BRUTTO", typeof(Decimal)));
                    dt.Columns.Add(new DataColumn("CENA_NETTO", typeof(Decimal)));
                    dt.Columns.Add(new DataColumn("VAT", typeof(Decimal)));
                    dt.Columns.Add(new DataColumn("CENA_BRUTTO", typeof(Decimal)));


                    string[] lines = System.IO.File.ReadAllLines(dial.FileName.ToString(), Encoding.Default);
                    string sekcja = "";
                    string typ = "";
                    foreach (string line in lines)
                    {
                        string[] tablica = line.Split(',');
                        if (line.StartsWith("[") && !line.Equals("[ZAWARTOSC]"))
                        {
                            sekcja = line;
                            typ = "";
                        }else if (sekcja.Equals("[ZAWARTOSC]"))
                        {
                            //nic typ pozostaje
                        }
                        else if (typ.Equals(""))
                        {
                            typ = tablica[0].ToString();
                        }

                        if (line.StartsWith("\"FS") || line.StartsWith("\"FZ"))
                            lSymbolFakturyZakupu.Text = tablica[4].ToString().Replace("\"","");

                        if (typ != "" && typ!=line && line!="" && line!="[ZAWARTOSC]")
                        {
                            if ( (  (typ.Equals("\"FS\"") && !line.StartsWith("\"FS")) ||
                                   (typ.Equals("\"FZ\"") && !line.StartsWith("\"FZ"))  
                                 ) || cTrybTestuPliku.Checked
                                )
                            {
                                DataRow workRow = dt.NewRow();
                                workRow["SEKCJA"] = sekcja;
                                workRow["TYP"] = typ;
                                workRow["LINIA"] = line;
                                if (cTrybTestuPliku.Checked == false)
                                {
                                    workRow["LP"] = tablica[0].ToString();
                                    workRow["ILOSC"] = Convert.ToDecimal(tablica[10].ToString().Replace(".", ","));
                                    workRow["SKROT"] = tablica[2].ToString().Replace("\"", "");

                                    workRow["JM"] = tablica[9].ToString().Replace("\"", "");
                                    workRow["CENA_NETTO"] = Convert.ToDecimal(tablica[12].ToString().Replace(".", ","));
                                    workRow["VAT"] = Math.Round(Convert.ToDecimal(tablica[12].ToString().Replace(".", ",")) * (Convert.ToDecimal(tablica[15].ToString().Replace(".", ","))/100),2);
                                    //workRow["CENA_BRUTTO"] = Math.Round(Convert.ToDecimal(tablica[16].ToString().Replace(".", ",")) + Convert.ToDecimal(tablica[17].ToString().Replace(".", ",")), 2);
                                    workRow["CENA_BRUTTO"] = Convert.ToDecimal(workRow["CENA_NETTO"]) + Convert.ToDecimal(workRow["VAT"]);

                                    workRow["PROC_VAT"] = Convert.ToDecimal(tablica[15].ToString().Replace(".", ","));
                                    workRow["CENA_KAT_BRUTTO"] = Convert.ToDecimal(tablica[14].ToString().Replace(".", ","));
                                    workRow["CENA_KAT_NETTO"] = Math.Round(Convert.ToDecimal(tablica[14].ToString().Replace(".", ",")) / (1 + (Convert.ToDecimal(tablica[15].ToString().Replace(".", ",")) / 100)), 2);
                                }
                                dt.Rows.Add(workRow);
                            }

                            if (typ.Equals("\"TOWARY\"") && !line.Equals("\"TOWARY\"") && cTrybTestuPliku.Checked == false)
                            {
                                string[] tabTowary = line.Split(',');
                                //tabTowary[1] nr zapasu
                                //tabTowary[3] kod kreskowy (KOD_KRESKOWY)
                                //tabTowary[4] nazwa skrocona
                                //tabTowary[5] index i nazwa (NAZWA)
                                Console.WriteLine(line);
                                foreach (DataGridViewRow row in dataGridView1.Rows)
                                {
                                    if (row.Cells["SKROT"].Value!=null && row.Cells["SKROT"].Value.Equals(tabTowary[1].Replace("\"","")))
                                    {
                                        if (tabTowary[3]!=null)
                                            row.Cells["KOD_KRESKOWY"].Value = tabTowary[3].Replace("\"", "");
                                        row.Cells["NAZWA"].Value = tabTowary[5].Replace("\"", "");
                                    }
                                }

                                foreach (DataRow row in dt.Rows)
                                {
                                    if (row["SKROT"].Equals(tabTowary[1].Replace("\"", "")))
                                    {
                                        row["KOD_KRESKOWY"] = tabTowary[3].Replace("\"", "");
                                        row["NAZWA"] = tabTowary[5].Replace("\"", "");
                                    }
                                }
                            }
                        }
                    }

                    fDataView = new DataView();
                    fDataView.Table = dt;
                    dataGridView1.DataSource = fDataView;
                    if (cUkryjKolumnyTechniczne.Checked)
                    {
                        dataGridView1.Columns[0].Visible = false;
                        dataGridView1.Columns[1].Visible = false;
                        dataGridView1.Columns[2].Visible = false;
                    }
                    else
                    {
                        dataGridView1.Columns[0].Visible = true;
                        dataGridView1.Columns[1].Visible = true;
                        dataGridView1.Columns[2].Visible = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Błąd importu pliku: " + ex.Message);
                    throw;
                }
            }
            else
            {
                MessageBox.Show("Operację importu faktur anulowano","Anulowano import:");
            }
        }

        private void bZapiszFakZakdoSchowka_Click(object sender, EventArgs e)
        {
            int idscho = 0;
            int iloscSkip = 0;
            int iloscSave = 0;
            tPodsumowanieZeSchowka.Text += "Krok 3 " + DateTime.Now.ToString() + " Zapis faktury zakupowej do schowka pozycji RaksSQL" + System.Environment.NewLine;
            FbCommand gen_id_schowek = new FbCommand("SELECT GEN_ID(GM_SCHOWEK_POZYCJI_GEN,1) from rdb$database", fbconn.getCurentConnection());
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["SKROT"].Value!=null)
                {
                    try
                    {
                        idscho = Convert.ToInt32(gen_id_schowek.ExecuteScalar());
                    }
                    catch (FbException exgen)
                    {
                        MessageBox.Show("Błąd pobierania nowego ID z bazy. Operacje przerwano! " + exgen.Message);
                        throw;
                    }


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
                    myStringBuilder.Append("CENA_ZAKUPU_PLN_NETTO, "); 
                    myStringBuilder.Append("CENA_ZAKUPU_PLN_BRUTTO, "); 
                    myStringBuilder.Append("ZNACZNIKI, ");
                    myStringBuilder.Append("UWAGI");

                    myStringBuilder.Append(") VALUES ( ");

                    myStringBuilder.Append(idscho.ToString() + ",");  // ID
                    myStringBuilder.Append("'EPP', ");  // OPERATOR
                    myStringBuilder.Append("'EPP " + lSymbolFakturyZakupu.Text + "', ");  //IDENTYFIKATOR
                    myStringBuilder.Append("1, ");  //PUBLICZNA

                    string skrot = "";
                    if (row.Cells["SKROT"].Value.ToString().Equals("SCHENKERD") || row.Cells["SKROT"].Value.ToString().Equals("SCHENKERM") || row.Cells["SKROT"].Value.ToString().Equals("ZX0001"))
                        skrot = "TR";
                    else
                        skrot = row.Cells["SKROT"].Value.ToString();

                    string sql = "SELECT ID_TOWARU from GM_TOWARY where SKROT='" + skrot + "'";
                    sql += " OR SKROT2='" + skrot + "'";
                    sql += " OR KONTOFK='" + skrot + "'";
                    sql += ";";
                    FbCommand gen_id_towaru = new FbCommand(sql, fbconn.getCurentConnection());
                    string idtow = "0";
                    try
                    {
                        if (gen_id_towaru.ExecuteScalar() != null)
                        {
                            idtow = (gen_id_towaru.ExecuteScalar()).ToString();
                        }
                        else
                        {
                            tPodsumowanieZeSchowka.Text += "Brak towaru: " + row.Cells["SKROT"].Value.ToString() + " ; " + row.Cells["NAZWA"].Value.ToString() + " ;\t " + row.Cells["KOD_KRESKOWY"].Value.ToString() + "; " + row.Cells["JM"].Value.ToString() + System.Environment.NewLine;

                        }
                    }
                    catch (FbException fex)
                    {
                        MessageBox.Show("Bład pobierania ID towaru: " + row.Cells["SKROT"].Value.ToString() + System.Environment.NewLine + fex.Message);
                        throw;
                    }


                    myStringBuilder.Append(idtow + ", ");  //ID_TOWARU

                    myStringBuilder.Append(row.Cells["ILOSC"].Value.ToString().Replace(",", ".") + ", ");  //ILOSC
                    myStringBuilder.Append(row.Cells["CENA_KAT_NETTO"].Value.ToString().ToString().Replace(",", ".") + ", ");  //CENA_SP_PLN_NETTO_PO_RAB
                    myStringBuilder.Append(row.Cells["CENA_KAT_BRUTTO"].Value.ToString().ToString().Replace(",", ".") + ", ");  //CENA_SP_PLN_BRUTTO_PO_RAB
                    myStringBuilder.Append(row.Cells["CENA_KAT_NETTO"].Value.ToString().Replace(",", ".") + ", ");  //CENA_SP_PLN_NETTO_PRZED_RAB
                    myStringBuilder.Append(row.Cells["CENA_KAT_BRUTTO"].Value.ToString().Replace(",", ".") + ", ");  //CENA_SP_PLN_BRUTTO_PRZED_RAB
                    myStringBuilder.Append(row.Cells["CENA_KAT_NETTO"].Value.ToString().Replace(",", ".") + ", ");  //CENA_SP_WAL_NETTO_PRZED_RAB
                    myStringBuilder.Append(row.Cells["CENA_KAT_BRUTTO"].Value.ToString().Replace(",", ".") + ", ");  //CCENA_SP_WAL_BRUTTO_PRZED_RAB
                    myStringBuilder.Append(row.Cells["CENA_KAT_NETTO"].Value.ToString().Replace(",", ".") + ", ");  //CENA_SP_WAL_NETTO_PO_RAB
                    myStringBuilder.Append(row.Cells["CENA_KAT_BRUTTO"].Value.ToString().Replace(",", ".") + ", ");  //CCENA_SP_WAL_BRUTTO_PO_RAB

                    myStringBuilder.Append(row.Cells["CENA_KAT_NETTO"].Value.ToString().Replace(",", ".") + ", ");  //CENA_KATALOGOWA_NETTO
                    myStringBuilder.Append(row.Cells["CENA_KAT_BRUTTO"].Value.ToString().Replace(",", ".") + ", ");  //CENA_KATALOGOWA_BRUTTO

                    myStringBuilder.Append(row.Cells["CENA_NETTO"].Value.ToString().Replace(",", ".") + ", ");  //CENA_ZAKUPU_PLN_NETTO
                    myStringBuilder.Append(row.Cells["CENA_BRUTTO"].Value.ToString().Replace(",", ".") + ", ");  //CENA_ZAKUPU_PLN_BRUTTO

                    myStringBuilder.Append("NULL, ");  //ZNACZNIKI
                    myStringBuilder.Append("NULL");  //UWAGI
                    myStringBuilder.Append(");");

                    FbCommand cdk = new FbCommand(myStringBuilder.ToString(), fbconn.getCurentConnection());
                    try
                    {
                        if (idtow.Equals("0"))
                        {
                            iloscSkip++;
                        }
                        else
                        {
                            cdk.ExecuteScalar();
                            iloscSave++;
                        }
                    }
                    catch (FbException ex)
                    {
                        MessageBox.Show("Błąd zapisu danych do schowka z okna z import faktur EPP: " + ex.Message);
                    }
                }
            }
            tPodsumowanieZeSchowka.Text += System.Environment.NewLine + "Zapisano " + iloscSave + " pozycji...";
            tPodsumowanieZeSchowka.Text += System.Environment.NewLine + "Pominieto " + iloscSkip + " pozycji..." + System.Environment.NewLine;
        }

        private void lSymbolFakturyZakupu_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Clipboard.SetText(lSymbolFakturyZakupu.Text);
        }

        private void cTrybTestuPliku_CheckedChanged(object sender, EventArgs e)
        {
            if (cTrybTestuPliku.Checked)
                cUkryjKolumnyTechniczne.Checked = false;
        }

        private void bRaportOperacjiKasaBank_Click(object sender, EventArgs e)
        {
            toSearch.ReadOnly = true;
            toSearch2.ReadOnly = true;
            SetStatusStartuRaportu(DateTime.Now);
            SetWartosciParametrowDlaWhere();
            bSaveToRaksSQLClipboard.Enabled = false;
            checkBoxIlosc1.Enabled = true;
            checkBoxIlosc1.Checked = false;

            StringBuilder myStringBuilder = new StringBuilder(" select * from ( ");
            myStringBuilder.Append(" Select KB_CASH_DESKS.NAME as MAGAZYN, 'KASA' as TYP, ");
            myStringBuilder.Append(" CASE when GM_FS.KOD is null then GM_FZ.KOD else GM_FS.KOD end as KOD, ");
            myStringBuilder.Append(" KB_CASH_DOCUMENTS.NUMBER as NUMER, KB_CASH_DOCUMENTS.CREATION_DATE as DATA_WYSTAWIENIA, KB_CASH_DOCUMENTS.CREATION_DATE as DATA_PLATNOSCI, ");
            myStringBuilder.Append(" CASE when KB_CASH_DOCUMENTS.DIRECTION_CODE='I' then KB_CASH_DOCUMENTS.AMOUNT else -KB_CASH_DOCUMENTS.AMOUNT end as KWOTA,  ");
            myStringBuilder.Append(" KB_CASH_DOCUMENTS.CURRENCY_SYMBOL as WALUTA, ");
            myStringBuilder.Append(" KB_CASH_DOCUMENTS.DESCRIPTION as OPIS, KB_CASH_DOCUMENTS.CONTACT_FULL_NAME  as KONTRAHENT, KB_CASH_DOCUMENTS.CONTACT_TAXID as NIP ");
            myStringBuilder.Append(" FROM KB_CASH_DOCUMENTS ");
            myStringBuilder.Append(" join KB_CASH_DESKS on KB_CASH_DESKS.ID=KB_CASH_DOCUMENTS.CASH_DESK_ID ");
            myStringBuilder.Append(" left join GM_FS on KB_CASH_DOCUMENTS.NUMBER=GM_FS.NUMER ");
            myStringBuilder.Append(" left join GM_FZ on KB_CASH_DOCUMENTS.NUMBER=GM_FZ.NUMER ");
            myStringBuilder.Append(" where KB_CASH_DOCUMENTS.CREATION_DATE BETWEEN '" + dtKasaBankOD.Value.ToShortDateString() + "' AND '" + dtKasaBankDO.Value.ToShortDateString() + "' ");
            myStringBuilder.Append(" and KB_CASH_DOCUMENTS.DOC_TYPE='G' ");

            myStringBuilder.Append(" union  ");

            myStringBuilder.Append(" SELECT GM_MAGAZYNY.NUMER AS MAGAZYN , 'BANK' as TYP, GM_FS.KOD, GM_FS.NUMER, DATA_WYSTAWIENIA, DATA_PLATNOSCI, WAL_WARTOSC_BRUTTO as KWOTA,  ");
            myStringBuilder.Append(" CASE when ID_WALUTY=0 then 'PLN' ");
            myStringBuilder.Append(" when ID_WALUTY=1 then 'CHF' ");
            myStringBuilder.Append(" when ID_WALUTY=2 then 'EUR' ");
            myStringBuilder.Append(" when ID_WALUTY=3 then 'USD' ");
            myStringBuilder.Append(" when ID_WALUTY=201 then 'GBP' ");
            myStringBuilder.Append(" else '0' ");
            myStringBuilder.Append(" end WALUTA, ");
            myStringBuilder.Append(" NAZWA_SPOSOBU_PLATNOSCI as OPIS,  ");
            myStringBuilder.Append(" NAZWA_PELNA_PLATNIKA as KONTRAHENT, NIP_PLATNIKA as NIP ");
            myStringBuilder.Append(" FROM GM_FS  ");
            myStringBuilder.Append(" join GM_MAGAZYNY on GM_MAGAZYNY.ID=GM_FS.MAGNUM ");
            myStringBuilder.Append(" where NAZWA_SPOSOBU_PLATNOSCI not like 'Gotówka' ");
            myStringBuilder.Append(" and DATA_WYSTAWIENIA BETWEEN '" + dtKasaBankOD.Value.ToShortDateString() + "' AND '" + dtKasaBankDO.Value.ToShortDateString() + "' ) ");
            myStringBuilder.Append(" order by DATA_WYSTAWIENIA, DATA_PLATNOSCI, NUMER ");

            FbCommand cdk = new FbCommand(myStringBuilder.ToString(), fbconn.getCurentConnection());
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

        private void bRaportSprzedazyKasaBank_Click(object sender, EventArgs e)
        {
            toSearch.ReadOnly = true;
            toSearch2.ReadOnly = true;
            SetStatusStartuRaportu(DateTime.Now);
            SetWartosciParametrowDlaWhere();
            bSaveToRaksSQLClipboard.Enabled = false;
            checkBoxIlosc1.Enabled = true;
            checkBoxIlosc1.Checked = false;

            StringBuilder myStringBuilder = new StringBuilder("select MAGAZYN, TYP, DATA_WYSTAWIENIA, WALUTA, SUM(UZNANIE) as SUMA_UZNANIE, SUM(OBCIAZENIE) as SUMA_OBCIAZENIE  from ( ");
            myStringBuilder.Append(" Select KB_CASH_DESKS.NAME as MAGAZYN, 'KASA' as TYP, KB_CASH_DOCUMENTS.CREATION_DATE as DATA_WYSTAWIENIA, ");
            myStringBuilder.Append(" KB_CASH_DOCUMENTS.CURRENCY_SYMBOL as WALUTA, ");
            myStringBuilder.Append(" CASE when KB_CASH_DOCUMENTS.DIRECTION_CODE='I' then KB_CASH_DOCUMENTS.AMOUNT end as UZNANIE,  ");
            myStringBuilder.Append(" CASE when KB_CASH_DOCUMENTS.DIRECTION_CODE='O' then -KB_CASH_DOCUMENTS.AMOUNT end as OBCIAZENIE ");

            myStringBuilder.Append(" FROM KB_CASH_DOCUMENTS ");
            myStringBuilder.Append(" join KB_CASH_DESKS on KB_CASH_DESKS.ID=KB_CASH_DOCUMENTS.CASH_DESK_ID ");
            myStringBuilder.Append(" where KB_CASH_DOCUMENTS.CREATION_DATE BETWEEN '" + dtKasaBankOD.Value.ToShortDateString() + "' AND '" + dtKasaBankDO.Value.ToShortDateString() + "' ");
            myStringBuilder.Append(" and KB_CASH_DOCUMENTS.DOC_TYPE='G' ");

            myStringBuilder.Append(" union ");

            myStringBuilder.Append(" SELECT GM_MAGAZYNY.NUMER AS MAGAZYN , 'BANK' as TYP, DATA_WYSTAWIENIA,   ");
            myStringBuilder.Append(" CASE when ID_WALUTY=0 then 'PLN' ");
            myStringBuilder.Append(" when ID_WALUTY=1 then 'CHF' ");
            myStringBuilder.Append(" when ID_WALUTY=2 then 'EUR' ");
            myStringBuilder.Append(" when ID_WALUTY=3 then 'USD' ");
            myStringBuilder.Append(" when ID_WALUTY=201 then 'GBP' ");
            myStringBuilder.Append(" else '0' ");
            myStringBuilder.Append(" end WALUTA, ");
            myStringBuilder.Append(" WAL_WARTOSC_BRUTTO as UZNANIE, ");
            myStringBuilder.Append(" 0 as OBCIAZENIE ");
            myStringBuilder.Append(" FROM GM_FS  ");
            myStringBuilder.Append(" join GM_MAGAZYNY on GM_MAGAZYNY.ID=GM_FS.MAGNUM ");
            myStringBuilder.Append(" where NAZWA_SPOSOBU_PLATNOSCI not like 'Gotówka' ");
            myStringBuilder.Append(" and DATA_WYSTAWIENIA BETWEEN '" + dtKasaBankOD.Value.ToShortDateString() + "' AND '" + dtKasaBankDO.Value.ToShortDateString() + "' ) ");
            myStringBuilder.Append(" group by MAGAZYN, TYP, DATA_WYSTAWIENIA, WALUTA ");
            myStringBuilder.Append(" order by DATA_WYSTAWIENIA ");

            FbCommand cdk = new FbCommand(myStringBuilder.ToString(), fbconn.getCurentConnection());
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
    }
}
