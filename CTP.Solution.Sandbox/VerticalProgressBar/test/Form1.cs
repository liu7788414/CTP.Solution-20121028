using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using VerticalProgressBar;

namespace test
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
    {
        public VerticalProgressBar.VerticalProgressBar vpbTest;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//VerticalProgressBar.VerticalProgressBar vpb = new VerticalProgressBar.VerticalProgressBar();
			//vpb.Value = 80;
			//vpb.Width = 20;
			//vpb.Style= VerticalProgressBar.Styles.Solid;
			//this.Controls.Add(vpb);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.vpbTest = new VerticalProgressBar.VerticalProgressBar();
            this.SuspendLayout();
            // 
            // vpbTest
            // 
            this.vpbTest.BorderStyle = VerticalProgressBar.BorderStyles.Classic;
            this.vpbTest.Color = System.Drawing.Color.BurlyWood;
            this.vpbTest.Location = new System.Drawing.Point(192, 12);
            this.vpbTest.Maximum = 100;
            this.vpbTest.Minimum = 0;
            this.vpbTest.Name = "vpbTest";
            this.vpbTest.Size = new System.Drawing.Size(20, 162);
            this.vpbTest.Step = 10;
            this.vpbTest.Style = VerticalProgressBar.Styles.Solid;
            this.vpbTest.TabIndex = 0;
            this.vpbTest.Value = 70;
            // 
            // Form1
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.ClientSize = new System.Drawing.Size(338, 186);
            this.Controls.Add(this.vpbTest);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.verticalProgressBar1_Load);
            this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new Form1());
		}

        private Timer m_timer = new Timer();
        private void verticalProgressBar1_Load(object sender, EventArgs e)
        {
            m_timer.Interval = 1000;

            m_timer.Tick += m_timer_Tick;

            m_timer.Start();
        }

        void m_timer_Tick(object sender, EventArgs e)
        {
            if (IsHandleCreated)
            {
                Invoke(new Action(() =>
                {
                    vpbTest.Value = DateTime.Now.Millisecond % vpbTest.Maximum;
                    Refresh();
                }));
            }


        }
	}
}
