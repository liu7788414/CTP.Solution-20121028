using CTP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WrapperTest;

namespace PromptForm
{
    public partial class PromptForm : Form
    {
        public TraderAdapter _trader;
        public System.Timers.Timer _timer;
        public PromptForm()
        {
            InitializeComponent();
            _timer = new System.Timers.Timer(500);
            _timer.Elapsed += _timer_Tick;
            _timer.Start();
        }

        private object _locker = new object();

        void _timer_Tick(object sender, EventArgs e)
        {
            lock (_locker)
            {
                if (IsHandleCreated)
                {
                    Invoke(new Action(() =>
                    {
                        TopMost = false;
                        BringToFront();
                        TopMost = true;

                        if (_trader != null && _trader.PositionFields != null)
                        {
                            listView2.Items.Clear();

                            var totalProfit = 0.0;

                            foreach (var kv in _trader.PositionFields)
                            {
                                var color = Color.Black;
                                var dir = kv.Value.PosiDirection;
                                if (dir == EnumPosiDirectionType.Long)
                                {
                                    color = Color.Red;
                                }
                                else
                                {
                                    color = Color.Green;
                                }
                                var item = new ListViewItem();
                                item.UseItemStyleForSubItems = false;

                                var ins = kv.Value.InstrumentID;
                                var sub = item.SubItems.Add(ins);
                                sub.ForeColor = color;
                                
                                sub = item.SubItems.Add(dir.ToString());
                                sub.ForeColor = color;

                                var volume = kv.Value.Position;
                                sub = item.SubItems.Add(volume.ToString());
                                sub.ForeColor = color;


                                if (Utils.InstrumentToLastTick.ContainsKey(ins) && Utils.InstrumentToInstrumentInfo.ContainsKey(ins))
                                {                                
                                    var lastTick = Utils.InstrumentToLastTick[ins];
                                    var info = Utils.InstrumentToInstrumentInfo[ins];
                                    var cost = kv.Value.OpenCost / info.VolumeMultiple / volume;
                                    item.SubItems.Add(cost.ToString("f1"));

                                    var profit = (dir == EnumPosiDirectionType.Long ? 1 : (-1)) * (lastTick.LastPrice - cost) * info.VolumeMultiple * volume;
                                    sub = item.SubItems.Add(profit.ToString("f0"));
                                    if (profit >= 0)
                                    {
                                        sub.ForeColor = Color.Red;
                                    }
                                    else
                                    {
                                        sub.ForeColor = Color.Green;
                                    }

                                    totalProfit += profit;
                                }

                                listView2.Items.Add(item);
                            }

                            textBox1.Text = totalProfit.ToString("f2");
                            if(totalProfit > 0)
                            {
                                textBox1.ForeColor = Color.Red;
                            }
                            else
                            {
                                if(totalProfit < 0)
                                {
                                    textBox1.ForeColor = Color.Green;
                                }
                                else
                                {
                                    textBox1.ForeColor = Color.Black;
                                }
                            }
                        }
                    }));
                }
            }
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

        private Point p;
        private Point p2;
        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            p.X = e.X;
            p.Y = e.Y;
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var li = listView1.GetItemAt(p.X, p.Y);
            if (li != null)
            {
                var ins = li.SubItems[1].Text;
                var price = Convert.ToDouble(li.SubItems[6].Text);

                if (Utils.InstrumentToLastTick.ContainsKey(ins))
                {
                    var lastTick = Utils.InstrumentToLastTick[ins];

                    if (li.SubItems[2].Text.Equals("涨"))
                    {
                        _trader.ReqOrderInsert(ins, EnumDirectionType.Buy, lastTick.UpperLimitPrice, 1, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "快捷开多仓");
                    }
                    else
                    {
                        _trader.ReqOrderInsert(ins, EnumDirectionType.Sell, lastTick.LowerLimitPrice, 1, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "快捷开空仓");
                    }
                }               
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _trader.CloseAllPositions();
        }

        private void listView2_MouseDown(object sender, MouseEventArgs e)
        {
            p2.X = e.X;
            p2.Y = e.Y;
        }

        private void listView2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var li = listView2.GetItemAt(p2.X, p2.Y);
            if (li != null)
            {
                var ins = li.SubItems[1].Text;
                var longOrShort = li.SubItems[2].Text;

                if (Utils.InstrumentToLastTick.ContainsKey(ins))
                {
                    var lastTick = Utils.InstrumentToLastTick[ins];
                    if (longOrShort.Equals("Long"))
                    {
                        _trader.CloseLongPositionByInstrument(ins, "手工平多仓", true, lastTick.LowerLimitPrice);
                    }
                    else
                    {
                        _trader.CloseShortPositionByInstrument(ins, "手工平空仓", true, lastTick.UpperLimitPrice);
                    }
                }
            }
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
