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
        private string nazwa;
        private bool isadmin;
        private string magazyny;
        private string pass;

        //wersja logowanie do systemu 
        public Autentykacja(FBConn fbc)
        {
            InitializeComponent();
            fbconn = fbc;
            GetMMUSERSexists();
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

        public Int32 GetCurrentUserID()
        {
            return locIdUser;
        }

        private string GetUserNameById(Int32 lUserID =  0)
        {
            string sql = "SELECT KOD,ISADMIN,MAGAZYNY,PASS,NAZWA from MM_USERS ";
            if (lUserID > 0)
            {
                sql += "  where ID=" + lUserID;
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
                nazwa = row[4].ToString();
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
            string sql = "SELECT KOD,ISADMIN,MAGAZYNY,PASS,ID,NAZWA from MM_USERS ";
            sql += " where ISLOCK=0 and KOD='" + login +"';";

            FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
            try
            {
                FbDataReader row = cdk.ExecuteReader();
                if (row.Read())
                {
                    kod = row[0].ToString();
                    isadmin = (Convert.ToInt32(row[1]) == 1) ? true : false;
                    magazyny = row[2].ToString();
                    pass = row[3].ToString();
                    locIdUser = Convert.ToInt32(row[4]);
                    nazwa = row[5].ToString();
                }
                else
                {
                    MessageBox.Show("Brak autoryzacji użytkownika.","Nieprawidłowe logowanie");
                    loginResult = AutoryzationType.Odrzucony;
                }
                row.Close();
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd odczytu użytkownika z bazy RaksSQL: " + ex.Message,"Bład odczytu z bazy Firebird");
            }
            return kod;
        }

        public string[] GetUserPropertiesByID(Int32 usrID)
        {
            string[] tab = new string[4];
            GetUserNameById(usrID);
            tab[0] = kod;
            tab[1] = isadmin.ToString();
            tab[2] = nazwa;
            tab[3] = magazyny;

            return tab;
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
                lPassWrong.Text = "Hasło niezgodne!";
                bLogin.Enabled = false;
            }
        }

        private void bLogin_Click(object sender, EventArgs e)
        {
            if (locIdUser == 0)
                {
                    //logowanie do systemu
                    GetUserNameByLogin(tLogin.Text);
                    if (pass != null && pass.Length == 0 && kod.Length > 0)
                    {
                        //hasło puste i trzeba ustawić
                        MessageBox.Show("Hasło jest zresetowane, należy ustawić nowe hasło.", "Ustawienie hasła");
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
                        SetTimeTryToLogin();
                    }
                }
                else
                {
                    //ustawianie hasła
                    string sql = "UPDATE MM_USERS SET PASS='";
                    sql += "" + (Encoding.GetEncoding(1250).GetString(GenerateHash(tLogin.Text, tPass.Text))).Replace("'","");
                    sql += "', MODYFIKOWANY='NOW' where ID=" + locIdUser + " ;";

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

        private void SetTimeTryToLogin()
        {
            string sql = "UPDATE MM_USERS SET LOGOWANIE_PROBA='NOW' where ID = " + locIdUser + "; ";
            
            FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
            try
            {
                cdk.ExecuteScalar();
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd zapisu czasu próby logowania do bazy RaksSQL: " + ex.Message);
            }
        }

        private bool GetComparePass(string passOkno, string passDB, string userKod)
        {
            string curPass = (Encoding.GetEncoding(1250).GetString(GenerateHash(userKod, passOkno))).Replace("'", "");
            
            if (curPass!=null && passDB!=null && passDB.Equals(curPass))
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
            if (pass.Length > 0)
            {
                return (pass + userKod + "12345678901234567890").Substring(0, 20);
            } else
            {
                return "";
            }
        }

        private string SetStdInputUser(string userKod)
        {
            return (userKod + "12345678").Substring(0, 8);
        }

        public AutoryzationType SetResetPass()
        {
            //czyszczenie hasła
            string sql = "UPDATE MM_USERS SET PASS='', MODYFIKOWANY='NOW' where ID=" + locIdUser + " ;";

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

        public void SetTimestampLastLogin()
        {
            string sql = "UPDATE MM_USERS SET LOGOWANIE='NOW' where ID=" + locIdUser + " ;";

            FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
            try
            {
                cdk.ExecuteScalar();
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd zapisu ostatniej daty logowania do bazy RaksSQL: " + ex.Message);
            }
        }

        private void tPass_TextChanged(object sender, EventArgs e)
        {
            if (tPassToConfirmation.Visible)
            {
                if (tPass.Text.Length < 4)
                {
                    bLogin.Enabled = false;
                    lPassWrong.Text = "Za krótkie hasło!";
                    lPassWrong.Visible = true;
                }
                else
                {
                    lPassWrong.Visible = false;
                }
            }
        }

        private void tPass_KeyUp(object sender, KeyEventArgs e)
        {
            if (tPass.Text.Length==0 && tPassToConfirmation.Visible)
            {
                //nic nie robie
            }
            else if (e.KeyCode == Keys.Enter)
            {
                bLogin.PerformClick();
            }
        }

        public string GetMagazyny()
        {
            return magazyny;
        }

        private void GetMMUSERSexists()
        {
            string sql = "select 1 from rdb$relations where rdb$relation_name = 'MM_USERS'";
            
            FbCommand cdk = new FbCommand(sql, fbconn.getCurentConnection());
            try
            {
                if (cdk.ExecuteScalar()==null)
                {
                    sql = "CREATE TABLE MM_USERS";
                    sql += "(";
                    sql += "  ID ID_D NOT NULL,";
                    sql += "  KOD STRING8_D NOT NULL,";
                    sql += "  ISADMIN BOOLEAN_D,";
                    sql += "  ISLOCK BOOLEAN_D,";
                    sql += "  NAZWA STRING40_D NOT NULL,";
                    sql += "  MAGAZYNY STRING200_D,";
                    sql += "  UTWORZONY timestamp DEFAULT current_timestamp,";
                    sql += "  MODYFIKOWANY timestamp DEFAULT current_timestamp,";
                    sql += "  LOGOWANIE timestamp,";
                    sql += "  PASS STRING100_D,";
                    sql += "  LOGOWANIE_PROBA timestamp,";
                    sql += "  RIGHT_READ STRING120_D,";
                    sql += "  RIGHT_ADD STRING120_D,";
                    sql += "  RIGHT_UPDATE STRING120_D,";
                    sql += "  RIGHT_DEL STRING120_D,";
                    sql += "  RIGHT_ADM STRING120_D,";
                    sql += "  CONSTRAINT MM_USERS_PK_ PRIMARY KEY (ID)";
                    sql += ");";
                    cdk.CommandText = sql;
                    cdk.ExecuteScalar();

                    cdk.CommandText = "CREATE UNIQUE INDEX MM_USERS_ID_IDX ON MM_USERS (KOD); ";
                    cdk.ExecuteNonQuery();
                    cdk.CommandText = "CREATE INDEX MM_USERS_ISLOCK_ISADM_IND ON MM_USERS (ISLOCK,ISADMIN,KOD); ";
                    cdk.ExecuteNonQuery();
                    cdk.CommandText = "CREATE INDEX MM_USERS_ISLOCK_KOD_IND ON MM_USERS (ISLOCK,KOD); ";
                    cdk.ExecuteNonQuery();

                    cdk.CommandText = "CREATE GENERATOR MM_USERS_GEN;";
                    cdk.ExecuteNonQuery();

                    cdk.CommandText = "SELECT GEN_ID(MM_USERS_GEN,1) from rdb$database;";
                    cdk.ExecuteScalar();

                    cdk.CommandText = "INSERT INTO MM_USERS (ID,KOD,NAZWA,ISADMIN) values (1,'ADMIN','Administrator systemu',1);";
                    cdk.ExecuteScalar();
                }
            }
            catch (FbException ex)
            {
                MessageBox.Show("Błąd sprawdzenia istnienia tabeli MM_USERS bazy RaksSQL: " + ex.Message);
            }
        }

        public string GetCurrentUserLogin()
        {
            return kod;
        }
    }
}
