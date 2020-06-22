namespace RaportyRaksSQL
{
    partial class OknoZapisDoSchowkaRaks
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OknoZapisDoSchowkaRaks));
            this.bCancel = new System.Windows.Forms.Button();
            this.bSave = new System.Windows.Forms.Button();
            this.tnameClipboard = new System.Windows.Forms.TextBox();
            this.tnameUser = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tBrakiTowarow = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // bCancel
            // 
            this.bCancel.Location = new System.Drawing.Point(192, 241);
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size(75, 23);
            this.bCancel.TabIndex = 0;
            this.bCancel.Text = "&Anuluj";
            this.bCancel.UseVisualStyleBackColor = true;
            this.bCancel.Click += new System.EventHandler(this.bCancel_Click);
            // 
            // bSave
            // 
            this.bSave.Location = new System.Drawing.Point(273, 241);
            this.bSave.Name = "bSave";
            this.bSave.Size = new System.Drawing.Size(75, 23);
            this.bSave.TabIndex = 1;
            this.bSave.Text = "&Zapisz";
            this.bSave.UseVisualStyleBackColor = true;
            this.bSave.Click += new System.EventHandler(this.bSave_Click);
            // 
            // tnameClipboard
            // 
            this.tnameClipboard.Location = new System.Drawing.Point(117, 23);
            this.tnameClipboard.MaxLength = 25;
            this.tnameClipboard.Name = "tnameClipboard";
            this.tnameClipboard.Size = new System.Drawing.Size(231, 20);
            this.tnameClipboard.TabIndex = 2;
            // 
            // tnameUser
            // 
            this.tnameUser.Location = new System.Drawing.Point(117, 49);
            this.tnameUser.MaxLength = 15;
            this.tnameUser.Name = "tnameUser";
            this.tnameUser.Size = new System.Drawing.Size(231, 20);
            this.tnameUser.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Nazwa schowka";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 52);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Użytkownik";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(22, 84);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "label3";
            // 
            // tBrakiTowarow
            // 
            this.tBrakiTowarow.Location = new System.Drawing.Point(25, 120);
            this.tBrakiTowarow.Multiline = true;
            this.tBrakiTowarow.Name = "tBrakiTowarow";
            this.tBrakiTowarow.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tBrakiTowarow.Size = new System.Drawing.Size(323, 115);
            this.tBrakiTowarow.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(22, 104);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(169, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Towary, których brak w kartotece:";
            // 
            // OknoZapisDoSchowkaRaks
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(360, 271);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.tBrakiTowarow);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tnameUser);
            this.Controls.Add(this.tnameClipboard);
            this.Controls.Add(this.bSave);
            this.Controls.Add(this.bCancel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "OknoZapisDoSchowkaRaks";
            this.Text = "OknoZapisDoSchowkaRaks";
            this.Load += new System.EventHandler(this.OknoZapisDoSchowkaRaks_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.Button bSave;
        private System.Windows.Forms.TextBox tnameClipboard;
        private System.Windows.Forms.TextBox tnameUser;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tBrakiTowarow;
        private System.Windows.Forms.Label label4;
    }
}