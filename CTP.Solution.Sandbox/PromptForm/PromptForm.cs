using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PromptForm
{
    public partial class PromptForm : Form
    {
        public PromptForm()
        {
            InitializeComponent();
        }

        public void AddMessage(PromptItem promptItem)
        {
            var item = new ListViewItem();

            item.SubItems.Add(promptItem.MessageItems[0]);
            item.SubItems.Add(promptItem.MessageItems[1]);
            item.SubItems.Add(promptItem.MessageItems[2]);
            item.SubItems.Add(promptItem.MessageItems[3]);
            item.SubItems.Add(promptItem.MessageItems[4]);
            item.SubItems.Add(promptItem.MessageItems[5]);
            item.SubItems.Add(promptItem.MessageItems[6]);

            if (promptItem.MessageItems[1].Equals("涨"))
            {
                item.ForeColor = Color.Red;
            }
            else
            {
                item.ForeColor = Color.Green;
            }

            listView1.Items.Add(item);
            Refresh();
        }

        public ListView ListViewObj
        {
            get { return listView1; }
        }

        private void PromptForm_Load(object sender, EventArgs e)
        {
            //var list = new List<string>();
            //list.Add("1");
            //list.Add("2");
            //list.Add("3");
            //list.Add("4");
            //list.Add("5");
            //list.Add("6");

            //var promptItem = new PromptItem
            //{
            //    MessageItems = list,
            //    InstrumentId = "aaa",
            //    OpenOrClose = "Close",
            //    Direction = "Buy",
            //    Price = 0.9,
            //    Volume = 10,
            //    Offset = 0.1
            //};

            //AddMessage(promptItem);
        }
    }

    public class PromptItem
    {
        public List<string> MessageItems;
        public string InstrumentId;
        public string OpenOrClose;
        public string Direction;
        public double Price;
        public int Volume;
        public double Offset;
    }
}
