using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WrapperTest.Prompt
{
    public partial class PopForm : Form
    {
        public System.Timers.Timer _timerClear;

        public PopForm()
        {
            InitializeComponent();

            _timerClear = new System.Timers.Timer(1000 * 60 * 5);
            _timerClear.Elapsed += _timerClear_Elapsed;
            _timerClear.Start();
        }

        void _timerClear_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (IsHandleCreated)
            {
                Invoke(new Action(() =>
                    {
                        Clear();
                    }));
            }
        }

        public void AddItem(string instrument, double ratio)
        {
            var item = new ListViewItem();

            if (ratio > 0)
            {
                var sub = item.SubItems.Add(instrument);
                sub.ForeColor = Color.Red;

                sub = item.SubItems.Add(ratio.ToString("P"));
                sub.ForeColor = Color.Red;
            }
            else
            {
                var sub = item.SubItems.Add(instrument);
                sub.ForeColor = Color.Green;

                sub = item.SubItems.Add(ratio.ToString("P"));
                sub.ForeColor = Color.Green;
            }
        }

        public void Clear()
        {
            listView1.Items.Clear();
        }

        private void PopForm_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void PopForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
        }
    }
}
