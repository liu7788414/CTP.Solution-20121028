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
            this.listView2 = new System.Windows.Forms.ListView();
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader10 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.btCloseAll = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tbStopProfit = new System.Windows.Forms.TextBox();
            this.tbStopLoss = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btOK = new System.Windows.Forms.Button();
            this.cbEnable = new System.Windows.Forms.CheckBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel4 = new System.Windows.Forms.ToolStripStatusLabel();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.richTextBox2 = new System.Windows.Forms.RichTextBox();
            this.cbEnableTotal = new System.Windows.Forms.CheckBox();
            this.btOKTotal = new System.Windows.Forms.Button();
            this.tbStopLossTotal = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbStopProfitTotal = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.lbHighTotal = new System.Windows.Forms.Label();
            this.lbLowTotal = new System.Windows.Forms.Label();
            this.statusStrip1.SuspendLayout();
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
            this.listView1.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = true;
            this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listView1.Location = new System.Drawing.Point(3, 2);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(441, 175);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listView1_MouseDoubleClick);
            this.listView1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView1_MouseDown);
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
            this.To.Width = 67;
            // 
            // Ratio
            // 
            this.Ratio.Text = "幅度";
            // 
            // Last
            // 
            this.Last.Text = "最新";
            this.Last.Width = 67;
            // 
            // Time
            // 
            this.Time.Text = "时间";
            this.Time.Width = 61;
            // 
            // listView2
            // 
            this.listView2.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader7,
            this.columnHeader8,
            this.columnHeader9,
            this.columnHeader10});
            this.listView2.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.listView2.FullRowSelect = true;
            this.listView2.GridLines = true;
            this.listView2.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listView2.Location = new System.Drawing.Point(3, 183);
            this.listView2.Name = "listView2";
            this.listView2.Size = new System.Drawing.Size(441, 147);
            this.listView2.TabIndex = 1;
            this.listView2.UseCompatibleStateImageBehavior = false;
            this.listView2.View = System.Windows.Forms.View.Details;
            this.listView2.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listView2_MouseDoubleClick);
            this.listView2.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView2_MouseDown);
            // 
            // columnHeader2
            // 
            this.columnHeader2.Width = 2;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "合约";
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "方向";
            this.columnHeader4.Width = 55;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "数";
            this.columnHeader5.Width = 30;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "成本";
            this.columnHeader6.Width = 65;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "盈亏";
            this.columnHeader7.Width = 50;
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "最高";
            this.columnHeader8.Width = 50;
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "最低";
            this.columnHeader9.Width = 50;
            // 
            // columnHeader10
            // 
            this.columnHeader10.Text = "时间";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("SimSun", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(241, 346);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 24);
            this.label1.TabIndex = 2;
            this.label1.Text = "总：";
            // 
            // textBox1
            // 
            this.textBox1.Font = new System.Drawing.Font("SimSun", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textBox1.Location = new System.Drawing.Point(278, 346);
            this.textBox1.Margin = new System.Windows.Forms.Padding(2);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(71, 30);
            this.textBox1.TabIndex = 3;
            // 
            // btCloseAll
            // 
            this.btCloseAll.Font = new System.Drawing.Font("SimSun", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btCloseAll.Location = new System.Drawing.Point(353, 346);
            this.btCloseAll.Margin = new System.Windows.Forms.Padding(2);
            this.btCloseAll.Name = "btCloseAll";
            this.btCloseAll.Size = new System.Drawing.Size(31, 31);
            this.btCloseAll.TabIndex = 4;
            this.btCloseAll.Text = "平";
            this.btCloseAll.UseVisualStyleBackColor = true;
            this.btCloseAll.Click += new System.EventHandler(this.btCloseAll_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(25, 341);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 12);
            this.label2.TabIndex = 5;
            this.label2.Text = "单盈：";
            // 
            // tbStopProfit
            // 
            this.tbStopProfit.Location = new System.Drawing.Point(59, 336);
            this.tbStopProfit.Margin = new System.Windows.Forms.Padding(2);
            this.tbStopProfit.Name = "tbStopProfit";
            this.tbStopProfit.Size = new System.Drawing.Size(46, 21);
            this.tbStopProfit.TabIndex = 6;
            this.tbStopProfit.Text = "1000";
            // 
            // tbStopLoss
            // 
            this.tbStopLoss.BackColor = System.Drawing.SystemColors.Window;
            this.tbStopLoss.Location = new System.Drawing.Point(142, 336);
            this.tbStopLoss.Margin = new System.Windows.Forms.Padding(2);
            this.tbStopLoss.Name = "tbStopLoss";
            this.tbStopLoss.Size = new System.Drawing.Size(51, 21);
            this.tbStopLoss.TabIndex = 8;
            this.tbStopLoss.Text = "-1000";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(109, 341);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(41, 12);
            this.label3.TabIndex = 7;
            this.label3.Text = "单损：";
            // 
            // btOK
            // 
            this.btOK.Location = new System.Drawing.Point(197, 335);
            this.btOK.Margin = new System.Windows.Forms.Padding(2);
            this.btOK.Name = "btOK";
            this.btOK.Size = new System.Drawing.Size(40, 20);
            this.btOK.TabIndex = 9;
            this.btOK.Text = "确定";
            this.btOK.UseVisualStyleBackColor = true;
            this.btOK.Click += new System.EventHandler(this.btOK_Click);
            // 
            // cbEnable
            // 
            this.cbEnable.AutoSize = true;
            this.cbEnable.Checked = true;
            this.cbEnable.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbEnable.Location = new System.Drawing.Point(6, 341);
            this.cbEnable.Margin = new System.Windows.Forms.Padding(2);
            this.cbEnable.Name = "cbEnable";
            this.cbEnable.Size = new System.Drawing.Size(15, 14);
            this.cbEnable.TabIndex = 10;
            this.cbEnable.UseVisualStyleBackColor = true;
            this.cbEnable.CheckedChanged += new System.EventHandler(this.cbEnable_CheckedChanged);
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2,
            this.toolStripStatusLabel4});
            this.statusStrip1.Location = new System.Drawing.Point(0, 386);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(447, 22);
            this.statusStrip1.TabIndex = 11;
            this.statusStrip1.Text = "statusStrip1";
            this.statusStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.statusStrip1_ItemClicked);
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripStatusLabel1.ForeColor = System.Drawing.Color.Red;
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(62, 17);
            this.toolStripStatusLabel1.Text = "Label1";
            this.toolStripStatusLabel1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.toolStripStatusLabel1_MouseDown);
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripStatusLabel2.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripStatusLabel2.ForeColor = System.Drawing.Color.Green;
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(62, 17);
            this.toolStripStatusLabel2.Text = "Label2";
            this.toolStripStatusLabel2.MouseDown += new System.Windows.Forms.MouseEventHandler(this.toolStripStatusLabel2_MouseDown);
            // 
            // toolStripStatusLabel4
            // 
            this.toolStripStatusLabel4.Font = new System.Drawing.Font("SimHei", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripStatusLabel4.Name = "toolStripStatusLabel4";
            this.toolStripStatusLabel4.Size = new System.Drawing.Size(62, 17);
            this.toolStripStatusLabel4.Text = "Label4";
            // 
            // richTextBox1
            // 
            this.richTextBox1.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.richTextBox1.ForeColor = System.Drawing.Color.Red;
            this.richTextBox1.Location = new System.Drawing.Point(249, 42);
            this.richTextBox1.Margin = new System.Windows.Forms.Padding(2);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(188, 107);
            this.richTextBox1.TabIndex = 12;
            this.richTextBox1.Text = "";
            this.richTextBox1.Visible = false;
            // 
            // richTextBox2
            // 
            this.richTextBox2.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.richTextBox2.ForeColor = System.Drawing.Color.Green;
            this.richTextBox2.Location = new System.Drawing.Point(249, 202);
            this.richTextBox2.Margin = new System.Windows.Forms.Padding(2);
            this.richTextBox2.Name = "richTextBox2";
            this.richTextBox2.Size = new System.Drawing.Size(191, 119);
            this.richTextBox2.TabIndex = 13;
            this.richTextBox2.Text = "";
            this.richTextBox2.Visible = false;
            // 
            // cbEnableTotal
            // 
            this.cbEnableTotal.AutoSize = true;
            this.cbEnableTotal.Checked = true;
            this.cbEnableTotal.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbEnableTotal.Location = new System.Drawing.Point(6, 368);
            this.cbEnableTotal.Margin = new System.Windows.Forms.Padding(2);
            this.cbEnableTotal.Name = "cbEnableTotal";
            this.cbEnableTotal.Size = new System.Drawing.Size(15, 14);
            this.cbEnableTotal.TabIndex = 19;
            this.cbEnableTotal.UseVisualStyleBackColor = true;
            this.cbEnableTotal.CheckedChanged += new System.EventHandler(this.cbEnableTotal_CheckedChanged);
            // 
            // btOKTotal
            // 
            this.btOKTotal.Location = new System.Drawing.Point(197, 362);
            this.btOKTotal.Margin = new System.Windows.Forms.Padding(2);
            this.btOKTotal.Name = "btOKTotal";
            this.btOKTotal.Size = new System.Drawing.Size(40, 20);
            this.btOKTotal.TabIndex = 18;
            this.btOKTotal.Text = "确定";
            this.btOKTotal.UseVisualStyleBackColor = true;
            this.btOKTotal.Click += new System.EventHandler(this.btOKTotal_Click);
            // 
            // tbStopLossTotal
            // 
            this.tbStopLossTotal.Location = new System.Drawing.Point(142, 363);
            this.tbStopLossTotal.Margin = new System.Windows.Forms.Padding(2);
            this.tbStopLossTotal.Name = "tbStopLossTotal";
            this.tbStopLossTotal.Size = new System.Drawing.Size(51, 21);
            this.tbStopLossTotal.TabIndex = 17;
            this.tbStopLossTotal.Text = "-2000";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(109, 368);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 12);
            this.label4.TabIndex = 16;
            this.label4.Text = "总损：";
            // 
            // tbStopProfitTotal
            // 
            this.tbStopProfitTotal.Location = new System.Drawing.Point(59, 363);
            this.tbStopProfitTotal.Margin = new System.Windows.Forms.Padding(2);
            this.tbStopProfitTotal.Name = "tbStopProfitTotal";
            this.tbStopProfitTotal.Size = new System.Drawing.Size(46, 21);
            this.tbStopProfitTotal.TabIndex = 15;
            this.tbStopProfitTotal.Text = "2000";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(25, 368);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 12);
            this.label5.TabIndex = 14;
            this.label5.Text = "总盈：";
            // 
            // lbHighTotal
            // 
            this.lbHighTotal.AutoSize = true;
            this.lbHighTotal.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbHighTotal.ForeColor = System.Drawing.Color.Red;
            this.lbHighTotal.Location = new System.Drawing.Point(390, 346);
            this.lbHighTotal.Name = "lbHighTotal";
            this.lbHighTotal.Size = new System.Drawing.Size(12, 12);
            this.lbHighTotal.TabIndex = 20;
            this.lbHighTotal.Text = "0";
            this.lbHighTotal.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lbHighTotal_MouseDoubleClick);
            // 
            // lbLowTotal
            // 
            this.lbLowTotal.AutoSize = true;
            this.lbLowTotal.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbLowTotal.ForeColor = System.Drawing.Color.Green;
            this.lbLowTotal.Location = new System.Drawing.Point(390, 368);
            this.lbLowTotal.Name = "lbLowTotal";
            this.lbLowTotal.Size = new System.Drawing.Size(12, 12);
            this.lbLowTotal.TabIndex = 21;
            this.lbLowTotal.Text = "0";
            this.lbLowTotal.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lbLowTotal_MouseDoubleClick);
            // 
            // PromptForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(447, 408);
            this.Controls.Add(this.lbLowTotal);
            this.Controls.Add(this.lbHighTotal);
            this.Controls.Add(this.cbEnableTotal);
            this.Controls.Add(this.btOKTotal);
            this.Controls.Add(this.tbStopLossTotal);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.tbStopProfitTotal);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.richTextBox2);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.cbEnable);
            this.Controls.Add(this.btOK);
            this.Controls.Add(this.tbStopLoss);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tbStopProfit);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btCloseAll);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listView2);
            this.Controls.Add(this.listView1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "PromptForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "信号监测";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.PromptForm_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader InstrumentId;
        private System.Windows.Forms.ColumnHeader UpOrDown;
        private System.Windows.Forms.ColumnHeader From;
        private System.Windows.Forms.ColumnHeader To;
        private System.Windows.Forms.ColumnHeader Last;
        private System.Windows.Forms.ColumnHeader Time;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader Ratio;
        private System.Windows.Forms.ListView listView2;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button btCloseAll;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbStopProfit;
        private System.Windows.Forms.TextBox tbStopLoss;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btOK;
        private System.Windows.Forms.CheckBox cbEnable;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.ColumnHeader columnHeader10;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel4;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.RichTextBox richTextBox2;
        private System.Windows.Forms.CheckBox cbEnableTotal;
        private System.Windows.Forms.Button btOKTotal;
        private System.Windows.Forms.TextBox tbStopLossTotal;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbStopProfitTotal;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lbHighTotal;
        private System.Windows.Forms.Label lbLowTotal;
    }
}

