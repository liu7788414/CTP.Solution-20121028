namespace PromptForm
{
    partial class PromptForm
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
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.InstrumentId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.UpOrDown = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.From = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.To = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Ratio = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Last = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Time = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btChase = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listView1
            // 
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.InstrumentId,
            this.UpOrDown,
            this.From,
            this.To,
            this.Ratio,
            this.Last,
            this.Time});
            this.listView1.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = true;
            this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listView1.Location = new System.Drawing.Point(3, 2);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(431, 343);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Width = 2;
            // 
            // InstrumentId
            // 
            this.InstrumentId.Text = "合约";
            this.InstrumentId.Width = 73;
            // 
            // UpOrDown
            // 
            this.UpOrDown.Text = "?";
            this.UpOrDown.Width = 31;
            // 
            // From
            // 
            this.From.Text = "从";
            this.From.Width = 67;
            // 
            // To
            // 
            this.To.Text = "到";
            this.To.Width = 63;
            // 
            // Ratio
            // 
            this.Ratio.Text = "幅度";
            // 
            // Last
            // 
            this.Last.Text = "最新";
            this.Last.Width = 61;
            // 
            // Time
            // 
            this.Time.Text = "时间";
            this.Time.Width = 61;
            // 
            // btChase
            // 
            this.btChase.Location = new System.Drawing.Point(213, 312);
            this.btChase.Name = "btChase";
            this.btChase.Size = new System.Drawing.Size(75, 23);
            this.btChase.TabIndex = 1;
            this.btChase.Text = "追";
            this.btChase.UseVisualStyleBackColor = true;
            // 
            // PromptForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(437, 348);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.btChase);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "PromptForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "信号监测";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.PromptForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.Button btChase;
        private System.Windows.Forms.ColumnHeader InstrumentId;
        private System.Windows.Forms.ColumnHeader UpOrDown;
        private System.Windows.Forms.ColumnHeader From;
        private System.Windows.Forms.ColumnHeader To;
        private System.Windows.Forms.ColumnHeader Last;
        private System.Windows.Forms.ColumnHeader Time;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader Ratio;
    }
}

