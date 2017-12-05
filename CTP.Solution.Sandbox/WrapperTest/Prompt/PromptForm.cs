using CTP;
using System;
using System.Collections.Concurrent;
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
        private double stopProfit = 1000;
        private double stopLoss = -1000;
       
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
                            var listIns = new List<string>();
                            foreach (var kv in Utils.PositionKeyToHighLowProfit)
                            {
                                if(!_trader.PositionFields.ContainsKey(kv.Key))
                                {
                                    listIns.Add(kv.Key);
                                }
                            }

                            listIns.ForEach(l => 
                            { 
                                HighLowProfit highLowProfit; 
                                Utils.PositionKeyToHighLowProfit.TryRemove(l, out highLowProfit);
                                Utils.WriteLine(string.Format("去掉{0}的最高最低盈利...", l));
                            });

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

                                if(!Utils.InstrumentToLastTick.ContainsKey(ins))
                                {
                                    var list = new List<string>();
                                    list.Add(ins);
                                    ((QuoteAdapter)Utils.QuoteMain).SubscribeMarketData(list.ToArray());
                                }

                                if (Utils.InstrumentToLastTick.ContainsKey(ins) && Utils.InstrumentToInstrumentInfo.ContainsKey(ins))
                                {
                                    var lastTick = Utils.InstrumentToLastTick[ins];
                                    var info = Utils.InstrumentToInstrumentInfo[ins];
                                    var cost = kv.Value.OpenCost / info.VolumeMultiple / volume;
                                    item.SubItems.Add(cost.ToString("f1"));

                                    var profit = (dir == EnumPosiDirectionType.Long ? 1 : (-1)) * (lastTick.LastPrice - cost) * info.VolumeMultiple * volume;
                                    sub = item.SubItems.Add(profit.ToString("f0"));

                                    if (cbEnable.Checked)
                                    {
                                        if (profit > stopProfit)
                                        {
                                            ClosePositionByItem(item, "多仓止盈", "空仓止盈");
                                        }

                                        if (profit < stopLoss)
                                        {
                                            ClosePositionByItem(item, "多仓止损", "空仓止损");
                                        }
                                    }
                                    if (profit >= 0)
                                    {
                                        sub.ForeColor = Color.Red;
                                    }
                                    else
                                    {
                                        sub.ForeColor = Color.Green;
                                    }

                                    HighLowProfit highLowProfit;
                                    if (Utils.PositionKeyToHighLowProfit.ContainsKey(kv.Key))
                                    {
                                        highLowProfit = Utils.PositionKeyToHighLowProfit[kv.Key];

                                        if (profit > highLowProfit.High)
                                        {
                                            highLowProfit.High = profit;
                                            Utils.WriteLine(string.Format("设置{0}最高为{1}", kv.Key, profit));
                                        }
                                        else
                                        {
                                            if (profit < highLowProfit.Low)
                                            {
                                                highLowProfit.Low = profit;
                                                Utils.WriteLine(string.Format("设置{0}最低为{1}", kv.Key, profit));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        highLowProfit = new HighLowProfit();
                                        if(profit >= 0)
                                        {
                                            highLowProfit.High = profit;
                                            highLowProfit.Low = 0;
                                        }
                                        else
                                        {
                                            highLowProfit.High = 0;
                                            highLowProfit.Low = profit;
                                        }

                                        Utils.PositionKeyToHighLowProfit[kv.Key] = highLowProfit;
                                        Utils.WriteLine(string.Format("创建{0}最高最低盈利...", kv.Key));
                                    }

                                    sub = item.SubItems.Add(Math.Round(highLowProfit.High).ToString());
                                    sub.ForeColor = Color.Red;

                                    sub = item.SubItems.Add(Math.Round(highLowProfit.Low).ToString());
                                    sub.ForeColor = Color.Green;


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
                        if (!_trader.ContainsPositionByInstrument(ins, EnumPosiDirectionType.Short))  //持有空仓不开多仓
                        {
                            _trader.ReqOrderInsert(ins, EnumDirectionType.Buy, lastTick.UpperLimitPrice, 1, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "快捷开多仓");
                        }
                        else
                        {
                            MessageBox.Show("持有空仓不开多仓");
                        }
                    }
                    else
                    {
                        if (!_trader.ContainsPositionByInstrument(ins, EnumPosiDirectionType.Long)) //持有多仓不开空仓
                        {
                            _trader.ReqOrderInsert(ins, EnumDirectionType.Sell, lastTick.LowerLimitPrice, 1, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "快捷开空仓");
                        }
                        else
                        {
                            MessageBox.Show("持有多仓不开空仓");
                        }
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

        public void SetUpStatus(string message)
        {
            toolStripStatusLabel1.Text = message;
        }

        public void SetDownStatus(string message)
        {
            toolStripStatusLabel2.Text = message;
        }

        private void ClosePositionByItem(ListViewItem li, string longReason, string shortReason)
        {
            if (li != null)
            {
                var ins = li.SubItems[1].Text;
                var longOrShort = li.SubItems[2].Text;

                if (Utils.InstrumentToLastTick.ContainsKey(ins))
                {
                    var lastTick = Utils.InstrumentToLastTick[ins];
                    if (longOrShort.Equals("Long"))
                    {
                        _trader.CloseLongPositionByInstrument(ins, longReason, true, lastTick.LowerLimitPrice);
                    }
                    else
                    {
                        _trader.CloseShortPositionByInstrument(ins, shortReason, true, lastTick.UpperLimitPrice);
                    }
                }
            }
        }

        private void listView2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var li = listView2.GetItemAt(p2.X, p2.Y);
            ClosePositionByItem(li, "手工平多仓", "手工平空仓");
        }

        private void btOK_Click(object sender, EventArgs e)
        {
            stopProfit = Convert.ToDouble(tbStopProfit.Text);
            stopLoss = Convert.ToDouble(tbStopLoss.Text);
        }

        private void cbEnable_CheckedChanged(object sender, EventArgs e)
        {
            tbStopLoss.Enabled = tbStopProfit.Enabled = btOK.Enabled = ((CheckBox)sender).Checked;
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
