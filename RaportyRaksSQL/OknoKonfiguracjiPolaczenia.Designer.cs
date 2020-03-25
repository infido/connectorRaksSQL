namespace RaportyRaksSQL
{
    partial class OknoKonfiguracjiPolaczenia
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.tIP = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tPath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tLogin = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tPass = new System.Windows.Forms.TextBox();
            this.tOutput = new System.Windows.Forms.TextBox();
            this.bSave = new System.Windows.Forms.Button();
            this.lPathInfo = new System.Windows.Forms.Label();
            this.bClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(273, 10);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(132, 23);
            this.button1.TabIndex = 26;
            this.button1.Text = "Wczytaj z ustawień";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tIP
            // 
            this.tIP.Location = new System.Drawing.Point(125, 12);
            this.tIP.Name = "tIP";
            this.tIP.Size = new System.Drawing.Size(100, 20);
            this.tIP.TabIndex = 18;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(94, 13);
            this.label1.TabIndex = 19;
            this.label1.Text = "IP serwera Firebird";
            // 
            // tPath
            // 
            this.tPath.Location = new System.Drawing.Point(125, 38);
            this.tPath.Name = "tPath";
            this.tPath.Size = new System.Drawing.Size(280, 20);
            this.tPath.TabIndex = 20;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 13);
            this.label2.TabIndex = 21;
            this.label2.Text = "Ścieżka do bazy";
            // 
            // tLogin
            // 
            this.tLogin.Location = new System.Drawing.Point(125, 64);
            this.tLogin.Name = "tLogin";
            this.tLogin.Size = new System.Drawing.Size(100, 20);
            this.tLogin.TabIndex = 22;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 67);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 13);
            this.label3.TabIndex = 23;
            this.label3.Text = "Login";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(11, 93);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(36, 13);
            this.label4.TabIndex = 25;
            this.label4.Text = "Hasło";
            // 
            // tPass
            // 
            this.tPass.Location = new System.Drawing.Point(125, 90);
            this.tPass.Name = "tPass";
            this.tPass.PasswordChar = '*';
            this.tPass.Size = new System.Drawing.Size(100, 20);
            this.tPass.TabIndex = 24;
            // 
            // tOutput
            // 
            this.tOutput.Location = new System.Drawing.Point(14, 140);
            this.tOutput.Multiline = true;
            this.tOutput.Name = "tOutput";
            this.tOutput.ReadOnly = true;
            this.tOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tOutput.Size = new System.Drawing.Size(391, 199);
            this.tOutput.TabIndex = 28;
            // 
            // bSave
            // 
            this.bSave.Location = new System.Drawing.Point(170, 343);
            this.bSave.Name = "bSave";
            this.bSave.Size = new System.Drawing.Size(116, 23);
            this.bSave.TabIndex = 27;
            this.bSave.Text = "&Zapisz i Testuj";
            this.bSave.UseVisualStyleBackColor = true;
            this.bSave.Click += new System.EventHandler(this.bSave_Click);
            // 
            // lPathInfo
            // 
            this.lPathInfo.AutoSize = true;
            this.lPathInfo.Location = new System.Drawing.Point(12, 124);
            this.lPathInfo.Name = "lPathInfo";
            this.lPathInfo.Size = new System.Drawing.Size(16, 13);
            this.lPathInfo.TabIndex = 29;
            this.lPathInfo.Text = "...";
            // 
            // bClose
            // 
            this.bClose.Location = new System.Drawing.Point(292, 343);
            this.bClose.Name = "bClose";
            this.bClose.Size = new System.Drawing.Size(116, 23);
            this.bClose.TabIndex = 30;
            this.bClose.Text = "Zamknij";
            this.bClose.UseVisualStyleBackColor = true;
            this.bClose.Click += new System.EventHandler(this.bClose_Click);
            // 
            // OknoKonfiguracjiPolaczenia
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(420, 378);
            this.Controls.Add(this.bClose);
            this.Controls.Add(this.lPathInfo);
            this.Controls.Add(this.tOutput);
            this.Controls.Add(this.bSave);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.tIP);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tPath);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tLogin);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.tPass);
            this.Name = "OknoKonfiguracjiPolaczenia";
            this.ShowIcon = false;
            this.Text = "Konfiguracja połączenia";
            this.Load += new System.EventHandler(this.OknoKonfiguracjiPolaczenia_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox tIP;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tPath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tLogin;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tPass;
        private System.Windows.Forms.TextBox tOutput;
        private System.Windows.Forms.Button bSave;
        private System.Windows.Forms.Label lPathInfo;
        private System.Windows.Forms.Button bClose;
    }
}