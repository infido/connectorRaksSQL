using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KonfiguratorConnectorRaksSQL;
using FirebirdSql.Data.FirebirdClient;

namespace RaportyRaksSQL
{
    public partial class Autentykacja : Form
    {
        private AutoryzationType loginResult = AutoryzationType.Anulowane;
        private FBConn fbconn;
        private Int32 locIdUser = 0;
        private string kod;
        private bool isadmin;
        private string magazyny;
        private string pass;

        //wersja logowanie do systemu 
        public Autentykacja(FBConn fbc)
        {
            InitializeComponent();
            fbconn = fbc;
            ShowDialog();
        }

        //wersja ustawienie lub zmiana hasła
        public Autentykacja(FBConn fbc, Int32 idUser)
        {
            InitializeComponent();
            fbconn = fbc;
            locIdUser = idUser;
            tPassToConfirmation.Visible = true;
            lPassToConfirmation.Visible = true;
            tLogin.Text = GetUserNameById(idUser);
            tLogin.ReadOnly = true;
            bLogin.Enabled = false;
            ShowDialog();
        }

        public AutoryzationType GetAutoryzationResult()
        {
            return loginResult;
        }

        private string GetUserNameById(Int32 lUserID =  0)
        {
            string sql = "SELECT KOD,ISADMIN,MAGAZYNY,PASS from MM_USERS ";
            sql += " where ISLOCK=0 ";
            if (lUserID > 0)
            {
                sql += " and ID=" + lUserID;
            }
            sql += ";";

            FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
            try
            {
                FbDataReader row = cdk.ExecuteReader();
                row.Read();
                kod = row[0].ToString();
                isadmin = (Convert.ToInt32(row[1]) == 1) ? true : false;
                magazyny = row[2].ToString();
                pass = row[3].ToString();
                row.Close();
                MessageBox.Show("Odczytano dane dla " + kod);
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd odczytu użytkownika z bazy RaksSQL: " + ex.Message);
            }
            return kod;
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            loginResult = AutoryzationType.Anulowane;
            Visible = false;
        }

        private void tPassToConfirmation_TextChanged(object sender, EventArgs e)
        {
            if (tPass.Text.Equals(tPassToConfirmation.Text))
            {
                lPassWrong.Visible = false;
                bLogin.Enabled = true;
            }
            else
            {
                lPassWrong.Visible = true;
                bLogin.Enabled = false;
            }
        }

        private void bLogin_Click(object sender, EventArgs e)
        {
            if (locIdUser==0)
            {
                //logowanie do systemu
            }
            else
            {
                //ustawianie hasła
                string sql = "UPDATE MM_USERS SET PASS='" + tPass.Text +"' where ID=" + locIdUser + " ;";
               
                FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
                try
                {
                    cdk.ExecuteScalar();
                    loginResult = AutoryzationType.PassChanged;
                }
                catch (FbException ex)
                {
                    MessageBox.Show("Błąd zapisu hasła do bazy RaksSQL: " + ex.Message);
                    loginResult = AutoryzationType.Odrzucony;
                }
                Visible = false;
            }
        }
    }
}
