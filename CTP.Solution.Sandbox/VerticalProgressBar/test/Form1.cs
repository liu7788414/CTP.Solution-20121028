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
		private VerticalProgressBar.VerticalProgressBar verticalProgressBar1;
		private VerticalProgressBar.VerticalProgressBar verticalProgressBar2;
		private VerticalProgressBar.VerticalProgressBar verticalProgressBar3;
		private VerticalProgressBar.VerticalProgressBar verticalProgressBar4;
		private VerticalProgressBar.VerticalProgressBar verticalProgressBar5;
		private VerticalProgressBar.VerticalProgressBar verticalProgressBar6;
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
            this.verticalProgressBar1 = new VerticalProgressBar.VerticalProgressBar();
            this.verticalProgressBar2 = new VerticalProgressBar.VerticalProgressBar();
            this.verticalProgressBar3 = new VerticalProgressBar.VerticalProgressBar();
            this.verticalProgressBar4 = new VerticalProgressBar.VerticalProgressBar();
            this.verticalProgressBar5 = new VerticalProgressBar.VerticalProgressBar();
            this.verticalProgressBar6 = new VerticalProgressBar.VerticalProgressBar();
            this.SuspendLayout();
            // 
            // verticalProgressBar1
            // 
            this.verticalProgressBar1.BorderStyle = VerticalProgressBar.BorderStyles.Classic;
            this.verticalProgressBar1.Color = System.Drawing.Color.Blue;
            this.verticalProgressBar1.Location = new System.Drawing.Point(32, 28);
            this.verticalProgressBar1.Maximum = 100;
            this.verticalProgressBar1.Minimum = 50;
            this.verticalProgressBar1.Name = "verticalProgressBar1";
            this.verticalProgressBar1.Size = new System.Drawing.Size(19, 129);
            this.verticalProgressBar1.Step = 10;
            this.verticalProgressBar1.Style = VerticalProgressBar.Styles.Classic;
            this.verticalProgressBar1.TabIndex = 0;
            this.verticalProgressBar1.Value = 80;
            // 
            // verticalProgressBar2
            // 
            this.verticalProgressBar2.BorderStyle = VerticalProgressBar.BorderStyles.None;
            this.verticalProgressBar2.Color = System.Drawing.Color.Blue;
            this.verticalProgressBar2.Location = new System.Drawing.Point(74, 28);
            this.verticalProgressBar2.Maximum = 100;
            this.verticalProgressBar2.Minimum = 0;
            this.verticalProgressBar2.Name = "verticalProgressBar2";
            this.verticalProgressBar2.Size = new System.Drawing.Size(12, 129);
            this.verticalProgressBar2.Step = 10;
            this.verticalProgressBar2.Style = VerticalProgressBar.Styles.Solid;
            this.verticalProgressBar2.TabIndex = 1;
            this.verticalProgressBar2.Value = 30;
            // 
            // verticalProgressBar3
            // 
            this.verticalProgressBar3.BorderStyle = VerticalProgressBar.BorderStyles.Classic;
            this.verticalProgressBar3.Color = System.Drawing.Color.Brown;
            this.verticalProgressBar3.Location = new System.Drawing.Point(108, 28);
            this.verticalProgressBar3.Maximum = 100;
            this.verticalProgressBar3.Minimum = -30;
            this.verticalProgressBar3.Name = "verticalProgressBar3";
            this.verticalProgressBar3.Size = new System.Drawing.Size(28, 129);
            this.verticalProgressBar3.Step = 10;
            this.verticalProgressBar3.Style = VerticalProgressBar.Styles.Classic;
            this.verticalProgressBar3.TabIndex = 2;
            this.verticalProgressBar3.Value = -20;
            // 
            // verticalProgressBar4
            // 
            this.verticalProgressBar4.BorderStyle = VerticalProgressBar.BorderStyles.Classic;
            this.verticalProgressBar4.Color = System.Drawing.Color.CornflowerBlue;
            this.verticalProgressBar4.Location = new System.Drawing.Point(158, 28);
            this.verticalProgressBar4.Maximum = 100;
            this.verticalProgressBar4.Minimum = 0;
            this.verticalProgressBar4.Name = "verticalProgressBar4";
            this.verticalProgressBar4.Size = new System.Drawing.Size(12, 129);
            this.verticalProgressBar4.Step = 10;
            this.verticalProgressBar4.Style = VerticalProgressBar.Styles.Classic;
            this.verticalProgressBar4.TabIndex = 3;
            this.verticalProgressBar4.Value = 50;
            // 
            // verticalProgressBar5
            // 
            this.verticalProgressBar5.BorderStyle = VerticalProgressBar.BorderStyles.Classic;
            this.verticalProgressBar5.Color = System.Drawing.Color.Blue;
            this.verticalProgressBar5.Location = new System.Drawing.Point(268, 28);
            this.verticalProgressBar5.Maximum = 100;
            this.verticalProgressBar5.Minimum = 0;
            this.verticalProgressBar5.Name = "verticalProgressBar5";
            this.verticalProgressBar5.Size = new System.Drawing.Size(12, 129);
            this.verticalProgressBar5.Step = 10;
            this.verticalProgressBar5.Style = VerticalProgressBar.Styles.Classic;
            this.verticalProgressBar5.TabIndex = 1;
            this.verticalProgressBar5.Value = 50;
            // 
            // verticalProgressBar6
            // 
            this.verticalProgressBar6.BorderStyle = VerticalProgressBar.BorderStyles.Classic;
            this.verticalProgressBar6.Color = System.Drawing.Color.BurlyWood;
            this.verticalProgressBar6.Location = new System.Drawing.Point(202, 28);
            this.verticalProgressBar6.Maximum = 100;
            this.verticalProgressBar6.Minimum = 0;
            this.verticalProgressBar6.Name = "verticalProgressBar6";
            this.verticalProgressBar6.Size = new System.Drawing.Size(12, 129);
            this.verticalProgressBar6.Step = 10;
            this.verticalProgressBar6.Style = VerticalProgressBar.Styles.Solid;
            this.verticalProgressBar6.TabIndex = 0;
            this.verticalProgressBar6.Value = 70;
            // 
            // Form1
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.ClientSize = new System.Drawing.Size(338, 186);
            this.Controls.Add(this.verticalProgressBar6);
            this.Controls.Add(this.verticalProgressBar5);
            this.Controls.Add(this.verticalProgressBar4);
            this.Controls.Add(this.verticalProgressBar3);
            this.Controls.Add(this.verticalProgressBar2);
            this.Controls.Add(this.verticalProgressBar1);
            this.Name = "Form1";
            this.Text = "Form1";
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
	}
}
