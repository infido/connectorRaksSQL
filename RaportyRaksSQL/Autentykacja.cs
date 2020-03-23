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
using System.Security.Cryptography;

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

        //wersja reset, ustawienie lub zmiana hasła
        public Autentykacja(FBConn fbc, Int32 idUser)
        {
            InitializeComponent();
            fbconn = fbc;
            locIdUser = idUser;
            
        }
        // sprawdzenie czy prawidłowo podpięte pod button
        public AutoryzationType SetNewPassByUser()
        {
            tPassToConfirmation.Visible = true;
            lPassToConfirmation.Visible = true;
            tLogin.Text = GetUserNameById(locIdUser);
            tLogin.ReadOnly = true;
            bLogin.Enabled = false;
            ShowDialog();
            return loginResult;
        }

        public AutoryzationType GetPassIsEnable()
        {
            tLogin.Text = GetUserNameById(locIdUser);
            if (pass.Length > 0)
                return AutoryzationType.HasloUstawione;
            else
                return AutoryzationType.HasloPuste;
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
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd odczytu użytkownika z bazy RaksSQL: " + ex.Message);
            }
            return kod;
        }

        private string GetUserNameByLogin(string login)
        {
            string sql = "SELECT KOD,ISADMIN,MAGAZYNY,PASS,ID from MM_USERS ";
            sql += " where ISLOCK=0 and KOD='" + login +"';";

            FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
            try
            {
                FbDataReader row = cdk.ExecuteReader();
                row.Read();
                kod = row[0].ToString();
                isadmin = (Convert.ToInt32(row[1]) == 1) ? true : false;
                magazyny = row[2].ToString();
                pass = row[3].ToString();
                locIdUser = Convert.ToInt32(row[4]);
                row.Close();
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd odczytu użytkownika z bazy RaksSQL: " + ex.Message,"Bład odczytu z bazy Firebird");
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
            if (locIdUser == 0)
            {
                //logowanie do systemu
                GetUserNameByLogin(tLogin.Text);
                if (pass.Length == 0 && kod.Length > 0)
                {
                    //hasło puste i trzeba ustawić
                    MessageBox.Show("Hasło jest zresetowane, należy ustawić nowe hasło.","Ustawienie hasła");
                    lPassWrong.Visible = true;
                    tPassToConfirmation.Visible = true;
                    lPassToConfirmation.Visible = true;
                    tLogin.ReadOnly = true;
                }
                else
                {
                    if (GetComparePass(tPass.Text, pass, tLogin.Text))
                    {
                        if (isadmin)
                        {
                            loginResult = AutoryzationType.Administartor;
                        }
                        else
                        {
                            loginResult = AutoryzationType.Uzytkownik;
                        }
                    }
                    else
                    {
                        loginResult = AutoryzationType.Odrzucony;
                    }
                    Visible = false;
                }
            }
            else
            {
                //ustawianie hasła
                string sql = "UPDATE MM_USERS SET PASS='";
                sql += "" + Encoding.GetEncoding(1250).GetString(GenerateHash(tLogin.Text, tPass.Text));  
                sql += "' where ID=" + locIdUser + " ;";
               
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

        private bool GetComparePass(string passOkno, string passDB, string userKod)
        {
            string curPass = Encoding.GetEncoding(1250).GetString(GenerateHash(userKod, passOkno));
            
            if (passDB.Equals(curPass))
                return true;
            else
                return false;
        }

        private byte[] GenerateHash(string userKod, string password)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash( Encoding.Default.GetBytes(SetStdInputPass(password, userKod)));
            return data;
        }

        private string SetStdInputPass(string pass, string userKod)
        {
            return (pass + userKod + "12345678901234567890").Substring(0, 20);
        }

        private string SetStdInputUser(string userKod)
        {
            return (userKod + "12345678").Substring(0, 8);
        }

        public AutoryzationType SetResetPass()
        {
            //czyszczenie hasła
            string sql = "UPDATE MM_USERS SET PASS='' where ID=" + locIdUser + " ;";

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

            return loginResult;
        }
    }
}
