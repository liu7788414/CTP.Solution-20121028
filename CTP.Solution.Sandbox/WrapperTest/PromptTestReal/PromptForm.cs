using CTP;
using SendMail;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WrapperTest;
using VerticalProgressBar;

namespace PromptForm
{
    public partial class PromptForm : Form
    {
        public TraderAdapter _trader;
        public System.Timers.Timer _timer;
        public System.Timers.Timer _timerMoney;
        public System.Timers.Timer _timerOpen;
        private double stopProfitPoint = 10;
        private double stopLossPoint = -10;
        private double stopProfitTotal = 2000;
        private double stopLossTotal = -2000;
        private double warningTick = 10;
        private double closeRatio = 0.5;
        private double overtimePoint = -10;
        public ConcurrentDictionary<string, bool> InsTobBuyOpen = new ConcurrentDictionary<string, bool>();
        public ConcurrentDictionary<string, bool> InsTobSellOpen = new ConcurrentDictionary<string, bool>();
        public ConcurrentDictionary<string, DateTime> InsTodtBuyOpen = new ConcurrentDictionary<string, DateTime>();
        public ConcurrentDictionary<string, DateTime> InsTodtSellOpen = new ConcurrentDictionary<string, DateTime>();
        public static bool isPromptSent = false;

        public PromptForm()
        {
            InitializeComponent();
            _timer = new System.Timers.Timer(250);
            _timer.Elapsed += _timer_Tick;
            _timer.Start();

            _timerMoney = new System.Timers.Timer(1000 * 10);
            _timerMoney.Elapsed += _timerMoney_Elapsed; ;
            _timerMoney.Start();

            _timerOpen = new System.Timers.Timer(1000 * 10);
            _timerOpen.Elapsed += _timerOpen_Elapsed;
            _timerOpen.Start();
        }

        public void SetInstrument(string ins)
        {
            tbIns.Text = ins;
        }

        void _timerOpen_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var dtNow = DateTime.Now;

            var bBuyOpenToChange = new List<string>();

            foreach (var kv in InsTobBuyOpen)
            {
                if (!kv.Value)
                {
                    if (dtNow - InsTodtBuyOpen[kv.Key] > ts3)
                    {
                        bBuyOpenToChange.Add(kv.Key);
                    }
                }
            }

            foreach (var ins in bBuyOpenToChange)
            {
                InsTobBuyOpen[ins] = true;
            }

            var bSellOpenToChange = new List<string>();

            foreach (var kv in InsTobSellOpen)
            {
                if (!kv.Value)
                {
                    if (dtNow - InsTodtSellOpen[kv.Key] > ts3)
                    {
                        bSellOpenToChange.Add(kv.Key);
                    }
                }
            }

            foreach (var ins in bSellOpenToChange)
            {
                InsTobSellOpen[ins] = true;
            }
        }

        private void _timerMoney_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_trader != null)
            {
                _trader.ReqQryTradingAccount();
            }
        }

        private DateTime dt9 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 9, 0, 0);
        private DateTime dt10 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 10, 0, 0);
        private DateTime dt1015 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 10, 15, 0);
        private DateTime dt1030 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 10, 30, 0);
        private DateTime dt1130 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 11, 30, 0);
        private DateTime dt1330 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 13, 30, 0);
        private DateTime dt1500 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 15, 0, 0);
        private DateTime dt2100 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 21, 0, 0);
        private DateTime dt2200 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 22, 0, 0);
        private DateTime dt2300 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 0, 0);
        private DateTime dt0100 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 1, 0, 0);
        private TimeSpan ts1 = new TimeSpan(0, 1, 0);
        private TimeSpan ts0 = new TimeSpan(0, 0, 0);
        private TimeSpan ts2 = new TimeSpan(0, 2, 0);
        private TimeSpan ts5 = new TimeSpan(0, 5, 0);
        private TimeSpan ts3 = new TimeSpan(0, 3, 0);

        public void SetTime(DateTime dt)
        {
            timeLabel.Text = dt.ToString("HH:mm:ss");

            //if ((dt > dt9 && dt < dt10 && dt - dt9 < new TimeSpan(0, Utils.分钟数, 0)) || //9:00
            //    (dt > dt2100 && dt < dt2200 && dt - dt2100 < new TimeSpan(0, Utils.分钟数, 0)) || //21:00
            //    (dt1500 - dt > ts0 && dt1500 - dt < ts5) || //15:00
            //    (dt0100 - dt > ts0 && dt0100 - dt < ts5))   //01:00
            //{
            //    cbAutoOpen.Checked = false;
            //    timeLabel.ForeColor = Color.Red;
            //}
            //else
            //{
            //    cbAutoOpen.Checked = true;
            //    timeLabel.ForeColor = Color.Black;
            //}
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

                        if (_savedItem != null)
                        {
                            if (DateTime.Now - _savedItem.Time > new TimeSpan(0, 0, 0, 0, 0))
                            {
                                AddMessage(_savedItem);
                                _savedItem = null;
                            }
                        }

                        if (cbAutoOpen.Checked)
                        {
                            if (cbAutoOpen.ForeColor == Color.Black)
                            {
                                cbAutoOpen.ForeColor = Color.Red;
                            }
                            else
                            {
                                cbAutoOpen.ForeColor = Color.Black;
                            }
                        }
                        else
                        {
                            cbAutoOpen.ForeColor = Color.Black;
                        }

                        if (Utils.IsTraderReady)
                        {
                            toolStripStatusLabel4.Text = "已连接";
                            toolStripStatusLabel4.ForeColor = Color.Blue;
                        }
                        else
                        {
                            toolStripStatusLabel4.Text = "已断开";
                            toolStripStatusLabel4.ForeColor = Color.Red;
                        }

                        toolStripStatusLabel3.Text = Utils.availableMoney.ToString("f2");

                        if (_trader != null && _trader.UnFinishedOrderFields != null)
                        {
                            lvOrder.Items.Clear();

                            foreach (var kv in _trader.UnFinishedOrderFields)
                            {
                                //cbAutoOpen.Checked = false;

                                var item = new ListViewItem();
                                item.UseItemStyleForSubItems = false;

                                var color = Color.Red;

                                if (kv.Value.Direction == EnumDirectionType.Sell)
                                {
                                    color = Color.Green;
                                }

                                var ins = kv.Value.InstrumentID;
                                var subIns = item.SubItems.Add(ins);
                                subIns.ForeColor = color;

                                var price = kv.Value.LimitPrice;
                                var subPrice = item.SubItems.Add(price.ToString("f1"));
                                subPrice.ForeColor = color;

                                if (Utils.InstrumentToLastTick.ContainsKey(ins))
                                {
                                    var lastTick = Utils.InstrumentToLastTick[ins];

                                    var dis = lastTick.LastPrice - price;
                                    var subDis = item.SubItems.Add(dis.ToString("f1"));
                                    subDis.ForeColor = color;
                                }

                                var subVol = item.SubItems.Add(kv.Value.VolumeTotal.ToString());
                                subVol.ForeColor = color;

                                lvOrder.Items.Add(item);
                            }
                        }

                        if (_trader != null && _trader.PositionFields != null)
                        {
                            listView2.Items.Clear();
                            var listIns = new List<string>();
                            foreach (var kv in Utils.PositionKeyToHighLowProfit)
                            {
                                if (!_trader.PositionFields.ContainsKey(kv.Key))
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

                            if (_trader.PositionFields.Count <= 0)
                            {
                                lbHighTotal.Text = "-99999";
                                lbLowTotal.Text = "99999";
                            }

                            //if (_trader.UnFinishedOrderFields.Count == 0)
                            //{
                            //    cbEnable.Checked = true;
                            //}
                            //else
                            //{
                            //    cbEnable.Checked = false;
                            //}

                            var insAlreadyProcessed = new List<string>();

                            foreach (var kv in _trader.PositionFields)
                            {
                                //cbAutoOpen.Checked = false;
                                if (!Utils.IsShfeInstrument(kv.Value.InstrumentID) && insAlreadyProcessed.Contains(kv.Value.InstrumentID))
                                {
                                    continue;
                                }

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
                                insAlreadyProcessed.Add(ins);
                                var subIns = item.SubItems.Add(ins);
                                subIns.ForeColor = color;

                                var subLongShort = item.SubItems.Add(dir == EnumPosiDirectionType.Long ? "多" : "空");
                                subLongShort.ForeColor = color;

                                var volume = kv.Value.Position;

                                var subVolume = item.SubItems.Add(volume.ToString());
                                subVolume.ForeColor = color;

                                if (!Utils.InstrumentToLastTick.ContainsKey(ins))
                                {
                                    var list = new List<string>();
                                    list.Add(ins);
                                    ((QuoteAdapter)Utils.QuoteMain).SubscribeMarketData(list.ToArray());
                                }

                                if (Utils.InstrumentToLastTick.ContainsKey(ins) && Utils.InstrumentToInstrumentInfo.ContainsKey(ins))
                                {
                                    var info = Utils.InstrumentToInstrumentInfo[ins];
                                    var lastTick = Utils.InstrumentToLastTick[ins];
                                    var rangepoint = (int)(lastTick.LastPrice * Utils.范围 * info.VolumeMultiple);

                                    //nudWarningPoint.Value = rangepoint;
                                    //nudLossPoint.Value = rangepoint * 2;
                                    var cost = kv.Value.OpenCost / info.VolumeMultiple / volume;
                                    item.SubItems.Add(cost.ToString("f1"));

                                    double profitPoint = 0;

                                    if (dir == EnumPosiDirectionType.Long)
                                    {
                                        if (lastTick.BidPrice1.Equals(0) || Math.Abs(lastTick.BidPrice1) > 999999)
                                        {
                                            profitPoint = (lastTick.LastPrice - cost) / info.PriceTick;
                                        }
                                        else
                                        {
                                            profitPoint = (lastTick.BidPrice1 - cost) / info.PriceTick;
                                        }
                                    }
                                    else
                                    {
                                        if (lastTick.AskPrice1.Equals(0) || Math.Abs(lastTick.AskPrice1) > 999999)
                                        {
                                            profitPoint = (-1) * (lastTick.LastPrice - cost) / info.PriceTick;
                                        }
                                        else
                                        {
                                            profitPoint = (-1) * (lastTick.AskPrice1 - cost) / info.PriceTick;
                                        }
                                    }

                                    //Utils.WriteLine(string.Format("{0}:当前持仓倍数:{1},当前点数:{2}", ins, volume / Utils.OpenVolumePerTime, profitPoint), true);

                                    //if (kv.Value.Position > Utils.OpenVolumePerTime)  //已经触发多个委托，争取保本平仓
                                    //{
                                    //    if (profitPoint >= 1)
                                    //    {
                                    //        ClosePositionByItem(item, "保本离场", "保本离场");
                                    //    }
                                    //}

                                    var profit = profitPoint * info.PriceTick * info.VolumeMultiple * volume;
                                    var subProfit = item.SubItems.Add(profit.ToString("f0"));

                                    var subProfitPoint = item.SubItems.Add(profitPoint.ToString("f1"));

                                    if (cbEnable.Checked)
                                    {
                                        if (profitPoint > stopProfitPoint)
                                        {
                                            ClosePositionByItem(item, "多仓止盈", "空仓止盈");
                                        }

                                        if (profitPoint < stopLossPoint)
                                        {
                                            ClosePositionByItem(item, "多仓止损", "空仓止损");
                                        }

                                        if (profitPoint > 0.9 * stopProfitPoint)
                                        {
                                            nudProfitPoint.BackColor = Color.Red;
                                        }
                                        else
                                        {
                                            nudProfitPoint.BackColor = Color.White;
                                        }

                                        if (profitPoint < 0.9 * stopLossPoint)
                                        {
                                            nudLossPoint.BackColor = Color.Green;
                                        }
                                        else
                                        {
                                            nudLossPoint.BackColor = Color.White;
                                        }
                                    }

                                    if (cbAutoCloseProfit.Checked)
                                    {
                                        if ((dir == EnumPosiDirectionType.Long && cost + profitPoint * info.PriceTick > (cost * (1 + Utils.止盈比例) + Utils.偏移Tick数量 * info.PriceTick)))
                                        {
                                            ClosePositionByItem(item, string.Format("{0}多仓止盈！", ins), "");
                                            //Email.SendMessage(true, "liu7788414", "15800377605", string.Format("{0}多仓止盈！", ins));
                                        }

                                        if ((dir == EnumPosiDirectionType.Short && cost - profitPoint * info.PriceTick < (cost * (1 - Utils.止盈比例) - Utils.偏移Tick数量 * info.PriceTick)))
                                        {
                                            ClosePositionByItem(item, "", string.Format("{0}空仓止盈！", ins));
                                            //Email.SendMessage(true, "liu7788414", "15800377605", string.Format("{0}空仓止盈！", ins));
                                        }
                                    }

                                    if (cbAutoCloseLoss.Checked)
                                    {
                                        if ((dir == EnumPosiDirectionType.Long && cost + profitPoint * info.PriceTick < (cost * (1 - Utils.止损比例) + Utils.偏移Tick数量 * info.PriceTick)))
                                        {
                                            ClosePositionByItem(item, string.Format("{0}多仓止损！", ins), "");
                                            //Email.SendMessage(true, "liu7788414", "15800377605", string.Format("{0}多仓止损！", ins));
                                        }

                                        if ((dir == EnumPosiDirectionType.Short && cost - profitPoint * info.PriceTick > (cost * (1 + Utils.止损比例) - Utils.偏移Tick数量 * info.PriceTick)))
                                        {
                                            ClosePositionByItem(item, "", string.Format("{0}空仓止损！", ins));
                                            //Email.SendMessage(true, "liu7788414", "15800377605", string.Format("{0}空仓止损！", ins));
                                        }

                                        HighLowProfit highLowProfitTemp;
                                        if (Utils.PositionKeyToHighLowProfit.ContainsKey(kv.Key))
                                        {
                                            highLowProfitTemp = Utils.PositionKeyToHighLowProfit[kv.Key];

                                            if ((dir == EnumPosiDirectionType.Long && cost + profitPoint * info.PriceTick < (cost + highLowProfitTemp.HighTick * info.PriceTick) * (1 - Utils.止损比例)))
                                            {
                                                ClosePositionByItem(item, string.Format("{0}多仓移动止损！", ins), "");
                                                //Email.SendMessage(true, "liu7788414", "15800377605", string.Format("{0}多仓移动止损！", ins));
                                            }

                                            if ((dir == EnumPosiDirectionType.Short && cost - profitPoint * info.PriceTick > (cost + highLowProfitTemp.LowTick * info.PriceTick) * (1 + Utils.止损比例)))
                                            {
                                                ClosePositionByItem(item, "", string.Format("{0}空仓移动止损！", ins));
                                                //Email.SendMessage(true, "liu7788414", "15800377605", string.Format("{0}空仓移动止损！", ins));
                                            }
                                        }
                                    }

                                    if (profit > 0)
                                    {
                                        subProfit.ForeColor = Color.Red;
                                        subProfitPoint.ForeColor = Color.Red;
                                    }
                                    else
                                    {
                                        if (profit < 0)
                                        {
                                            subProfit.ForeColor = Color.Green;
                                            subProfitPoint.ForeColor = Color.Green;
                                        }
                                        else
                                        {
                                            subProfit.ForeColor = Color.Black;
                                            subProfitPoint.ForeColor = Color.Black;
                                        }
                                    }

                                    if (profitPoint > (double)nudWarningPoint.Value)
                                    {
                                        try
                                        {
                                            nudWarningPoint.Value = Convert.ToDecimal(profitPoint);
                                        }
                                        catch
                                        {

                                        }
                                    }

                                    HighLowProfit highLowProfit;
                                    if (Utils.PositionKeyToHighLowProfit.ContainsKey(kv.Key))
                                    {
                                        highLowProfit = Utils.PositionKeyToHighLowProfit[kv.Key];

                                        if (profit > highLowProfit.High)
                                        {
                                            highLowProfit.High = profit;
                                            highLowProfit.HighTick = profitPoint;
                                            Utils.WriteLine(string.Format("设置{0}最高为{1}", kv.Key, profit));
                                        }
                                        else
                                        {
                                            if (profit < highLowProfit.Low)
                                            {
                                                highLowProfit.Low = profit;
                                                highLowProfit.LowTick = profitPoint;
                                                Utils.WriteLine(string.Format("设置{0}最低为{1}", kv.Key, profit));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        highLowProfit = new HighLowProfit();
                                        if (profit >= 0)
                                        {
                                            highLowProfit.High = profit;
                                            highLowProfit.HighTick = profitPoint;
                                            highLowProfit.Low = 0;
                                            highLowProfit.LowTick = 0;
                                        }
                                        else
                                        {
                                            highLowProfit.High = 0;
                                            highLowProfit.HighTick = 0;
                                            highLowProfit.Low = profit;
                                            highLowProfit.LowTick = profitPoint;
                                        }

                                        Utils.PositionKeyToHighLowProfit[kv.Key] = highLowProfit;
                                        Utils.WriteLine(string.Format("创建{0}最高最低盈利...", kv.Key));
                                    }

                                    var subHigh = item.SubItems.Add(Math.Round(highLowProfit.High).ToString("f0"));
                                    subHigh.ForeColor = Color.Red;

                                    var subHighTick = item.SubItems.Add(Math.Round(highLowProfit.HighTick).ToString("f1"));
                                    subHighTick.ForeColor = Color.Red;

                                    var subLow = item.SubItems.Add(Math.Round(highLowProfit.Low).ToString("f0"));
                                    subLow.ForeColor = Color.Green;

                                    var subLowTick = item.SubItems.Add(Math.Round(highLowProfit.LowTick).ToString("f1"));
                                    subLowTick.ForeColor = Color.Green;

                                    if (highLowProfit.Low <= -4000)
                                    {
                                        subLow.BackColor = Color.Yellow;
                                    }

                                    //if (highLowProfit.HighTick >= 10 && profitPoint <= 2)
                                    //{
                                    //    ClosePositionByItem(item, "大回撤保本离场", "大回撤保本离场");
                                    //}

                                    if (cbWarning.Checked && highLowProfit.HighTick >= warningTick)
                                    {
                                        subHigh.BackColor = Color.Yellow;

                                        if (profitPoint >= Convert.ToDouble(nudWarningPoint.Value))
                                        {
                                            Utils.WriteLine(string.Format("当前价格{0},当前点数{1},当前最高点数{2}", lastTick.LastPrice, profitPoint, highLowProfit.HighTick), true);
                                            ClosePositionByItem(item, "警戒线止盈", "警戒线止盈");
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(kv.Value.TradingDay))
                                    {
                                        try
                                        {
                                            var subInsValue = item.SubItems.Add((lastTick.LastPrice * volume * info.VolumeMultiple).ToString());
                                        }
                                        catch
                                        { }
                                    }
                                    else
                                    {
                                        var subInsValue = item.SubItems.Add("0");
                                    }

                                    var upRatio = (lastTick.LastPrice - lastTick.OpenPrice) / lastTick.OpenPrice;
                                    var subupRatio = item.SubItems.Add(upRatio.ToString("P"));

                                    if (upRatio > 0.01)
                                    {
                                        subupRatio.ForeColor = Color.Red;
                                    }
                                    else
                                    {
                                        if (upRatio < -0.01)
                                        {
                                            subupRatio.ForeColor = Color.Green;
                                        }
                                    }

                                    totalProfit += profit;
                                }

                                listView2.Items.Add(item);
                            }

                            textBox1.Text = totalProfit.ToString("f2");
                            if (totalProfit > 0)
                            {
                                textBox1.ForeColor = Color.Red;
                            }
                            else
                            {
                                if (totalProfit < 0)
                                {
                                    textBox1.ForeColor = Color.Green;
                                }
                                else
                                {
                                    textBox1.ForeColor = Color.Black;
                                }
                            }

                            if (!totalProfit.Equals(0) && (totalProfit > Convert.ToDouble(lbHighTotal.Text)))
                            {
                                lbHighTotal.Text = totalProfit.ToString("f2");
                            }

                            if (!totalProfit.Equals(0) && (totalProfit < Convert.ToDouble(lbLowTotal.Text)))
                            {
                                lbLowTotal.Text = totalProfit.ToString("f2");
                            }

                            if (cbEnableTotal.Checked)
                            {
                                if (totalProfit > stopProfitTotal || totalProfit < stopLossTotal)
                                {
                                    CloseAll("总盈利平仓", "总盈利平仓");
                                }
                            }
                        }
                        else
                        {
                            lbHighTotal.Text = "-99999";
                            lbLowTotal.Text = "99999";
                        }

                        if(listView1.Items.Count > 0)
                        {
                            BackColor = Color.Pink;
                        }
                        else
                        {
                            BackColor = SystemColors.Control;
                        }
                    }));
                }
            }
        }

        public void SetTitle(string title)
        {
            Text = title;
        }

        private PromptItem _savedItem = null;

        public void SaveMessage(PromptItem savedItem)
        {
            _savedItem = savedItem;
        }

        public void ShowWave(double max, double min, double wave)
        {
            var s = string.Format("高:{0},低:{1},幅:{2}", (int)max, (int)min, (int)wave);
            toolStripStatusLabel5.Text = s;

            if (wave > 250)
            {
                toolStripStatusLabel5.ForeColor = Color.Red;
            }
            else
            {
                toolStripStatusLabel5.ForeColor = Color.Black;
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
            if (promptItem.MessageItems.Count > 7)
            { item.SubItems.Add(promptItem.MessageItems[7]); }


            var ins = promptItem.MessageItems[0];


            //double largeRatioOffset = 0;

            //if (promptItem.Ratio > 0.004)
            //{
            //    largeRatioOffset = 10;
            //    Utils.WriteLine(string.Format("{0}遇见巨大涨跌幅度，增大偏移量", ins), true);
            //}

            if(_trader.IsInsTradedToday(ins))
            {
                Utils.WriteLine(string.Format("今日已经交易过{0}，不再报单...", ins), true);
                return;
            }

            _trader.AddOrderCount(ins);

            if (promptItem.MessageItems[1].Equals("涨"))
            {
                item.ForeColor = Color.Red;
                if (cbAutoOpen.Checked)
                {
                    if (Utils.AllowedShortTradeCategories.Contains(Utils.GetInstrumentCategory(promptItem.MessageItems[0])))
                    {
                        if (!InsTobBuyOpen.ContainsKey(ins))
                        {
                            InsTobBuyOpen[ins] = true;
                        }

                        if (InsTobBuyOpen[ins])
                        {
                            OpenByItem(item, Utils.开仓偏移比例[Utils.GetInstrumentCategory(ins)] * Utils.InstrumentToLastTick[ins].LastPrice);
                        }
                        else
                        {
                            Utils.WriteLine(string.Format("禁止{0}开多仓", ins), true);
                        }
                    }
                }
            }
            else
            {
                if (promptItem.MessageItems[1].Equals("跌"))
                {
                    item.ForeColor = Color.Green;
                    if (cbAutoOpen.Checked)
                    {
                        if (Utils.AllowedShortTradeCategories.Contains(Utils.GetInstrumentCategory(promptItem.MessageItems[0])))
                        {
                            if (!InsTobSellOpen.ContainsKey(ins))
                            {
                                InsTobSellOpen[ins] = true;
                            }

                            if (InsTobSellOpen[ins])
                            {
                                OpenByItem(item, Utils.开仓偏移比例[Utils.GetInstrumentCategory(ins)] * Utils.InstrumentToLastTick[ins].LastPrice);
                            }
                            else
                            {
                                Utils.WriteLine(string.Format("禁止{0}开空仓", ins), true);
                            }
                        }
                    }
                }
                else
                {
                    //if (promptItem.MessageItems[1].Equals("兴"))
                    //{
                    //    item.ForeColor = Color.Brown;

                    //    if (cbAutoOpen.Checked)
                    //    {
                    //        OpenByItem(item, Utils.开仓偏移量 + largeRatioOffset);
                    //    }
                    //}
                    //else
                    //{
                    //    if (promptItem.MessageItems[1].Equals("衰"))
                    //    {
                    //        item.ForeColor = Color.Blue;

                    //        if (cbAutoOpen.Checked)
                    //        {
                    //            OpenByItem(item, Utils.开仓偏移量 + largeRatioOffset);
                    //        }
                    //    }
                    //    else
                    //    { }
                    //}
                }
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
            stopProfitPoint = Convert.ToDouble(nudProfitPoint.Value);
            stopLossPoint = -Convert.ToDouble(nudLossPoint.Value);
            stopProfitTotal = Convert.ToDouble(tbStopProfitTotal.Text);
            stopLossTotal = Convert.ToDouble(tbStopLossTotal.Text);
            warningTick = Convert.ToDouble(nudWarningPoint.Value);
            closeRatio = Convert.ToDouble(nudCloseRatio.Value);
            overtimePoint = -Convert.ToDouble(nudOverTimePoint.Value);
            tbUpDownRatio.Text = Utils.涨跌幅提示.ToString();
            tbTargetMoney.Text = Utils.单手总金额.ToString();
            nudOffsetTickCount.Value = Utils.偏移Tick数量;

            var listIns = Utils.CategoryToMainInstrument.Values.ToList();
            listIns.Sort();

            foreach(var ins in listIns)
            {
                var item = new ListViewItem();
                var sub = item.SubItems.Add(ins);
                sub = item.SubItems.Add(ins);
                lvMainIns.Items.Add(item);
            }

            var distance = Width / listIns.Count;

            for (var i = 0; i < listIns.Count;i++ )
            {
                var vpb = new VerticalProgressBar.VerticalProgressBar();

                vpb.BorderStyle = VerticalProgressBar.BorderStyles.Classic;
                vpb.Location = new Point(12 + distance * i, 334);
                vpb.Maximum = ((int)Utils.成交量阈值[Utils.GetInstrumentCategory(listIns[i])]) * 10000;
                vpb.Minimum = 0;
                vpb.Name = listIns[i];
                vpb.Size = new Size(15, 60);
                vpb.Step = 1;
                vpb.Style = VerticalProgressBar.Styles.Solid;
                vpb.Value = 0;
                vpb.Click += vpb_Click;
                var tp = new ToolTip();
                tp.SetToolTip(vpb, listIns[i]);

                Controls.Add(vpb);

                var l = new Label();
                l.AutoSize = true;
                if (i % 2 == 0)
                {
                    l.Location = new Point(0 + distance * i, 410);
                }
                else
                {
                    l.Location = new Point(0 + distance * i, 400);
                }

                l.Name = string.Format("l{0}", i);
                l.Size = new Size(15, 9);
                l.Text = listIns[i];
                l.Font = new Font("SimSun", 8F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(134)));
                l.Click += label_Click;

                Controls.Add(l);

                var limit = new Label();
                limit.AutoSize = true;

                if (i % 2 == 0)
                {
                    limit.Location = new Point(l.Location.X + 7, l.Location.Y - 13);
                }
                else
                {
                    limit.Location = new Point(l.Location.X + 7, l.Location.Y + 13);
                }

                limit.Size = new Size(15, 9);
                limit.Text = Utils.成交量阈值[Utils.GetInstrumentCategory(listIns[i])].ToString();
                limit.Font = new Font("SimSun", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
                limit.ForeColor = Color.Blue;

                Controls.Add(limit);


                var vpbWaveRatio = new VerticalProgressBar.VerticalProgressBar();

                vpbWaveRatio.BorderStyle = VerticalProgressBar.BorderStyles.Classic;
                vpbWaveRatio.Location = new Point(12 + distance * i, 430);
                vpbWaveRatio.Maximum = (int)(Utils.涨跌幅提示 * 10000);
                vpbWaveRatio.Minimum = 0;
                vpbWaveRatio.Size = new Size(15, 40);
                vpbWaveRatio.Step = 1;
                vpbWaveRatio.Style = VerticalProgressBar.Styles.Solid;
                vpbWaveRatio.Value = 0;

                Controls.Add(vpbWaveRatio);

                var vpbLongCount = new VerticalProgressBar.VerticalProgressBar();

                vpbLongCount.BorderStyle = VerticalProgressBar.BorderStyles.Classic;
                vpbLongCount.Location = new Point(12 + distance * i, 475);
                vpbLongCount.Maximum = (int)Utils.成交量阈值[Utils.GetInstrumentCategory(listIns[i])] * 10000 / Utils.分钟数;
                vpbLongCount.Minimum = 0;
                vpbLongCount.Size = new Size(7, 55);
                vpbLongCount.Step = 1;
                vpbLongCount.Style = VerticalProgressBar.Styles.Solid;
                vpbLongCount.Color = Color.Red;
                vpbLongCount.Value = 0;

                Controls.Add(vpbLongCount);

                var vpbShortCount = new VerticalProgressBar.VerticalProgressBar();

                vpbShortCount.BorderStyle = VerticalProgressBar.BorderStyles.Classic;
                vpbShortCount.Location = new Point(20 + distance * i, 475);
                vpbShortCount.Maximum = (int)Utils.成交量阈值[Utils.GetInstrumentCategory(listIns[i])] * 10000 / Utils.分钟数;
                vpbShortCount.Minimum = 0;
                vpbShortCount.Size = new Size(7, 55);
                vpbShortCount.Step = 1;
                vpbShortCount.Style = VerticalProgressBar.Styles.Solid;
                vpbShortCount.Color = Color.Green;
                vpbShortCount.Value = 0;

                Controls.Add(vpbShortCount);

                InsToVpb[listIns[i]] = vpb;

                InsToLimitLabel[listIns[i]] = limit;

                InsToVpbWaveRatio[listIns[i]] = vpbWaveRatio;

                InsToVpbLongCount[listIns[i]] = vpbLongCount;

                InsToVpbShortCount[listIns[i]] = vpbShortCount;
            }
        }

        void vpb_Click(object sender, EventArgs e)
        {
            tbIns.Text = ((VerticalProgressBar.VerticalProgressBar)sender).Name;
        }

        public void PerformStep(string ins, int value, double waveRatio, MarketData marketData)
        {
            if (InsToVpb.ContainsKey(ins))
            {
                var limit = (int)(Utils.成交量阈值[Utils.GetInstrumentCategory(ins)] * 10000);
                var vpb = InsToVpb[ins];
                var limitLabel = InsToLimitLabel[ins];
                var vpbWaveRatio = InsToVpbWaveRatio[ins];
                var vpbLongCount = InsToVpbLongCount[ins];
                var vpbShortCount = InsToVpbShortCount[ins];

                vpb.Value = value;

                if (value >= limit)
                {
                    vpb.Color = Color.Red;
                }
                else
                {
                    if (value > limit * 0.8)
                    {
                        vpb.Color = Color.Orange;
                    }
                    else
                    {
                        vpb.Color = Color.Green;
                    }
                }

                if (value >= vpb.Maximum)
                {
                    vpb.Maximum *= 2;
                    limitLabel.Text = (vpb.Maximum / 10000.0).ToString("N1");
                }

                if (value < limit)
                {
                    vpb.Maximum = limit;
                    limitLabel.Text = (limit / 10000.0).ToString("N1");
                }

                if (((TraderAdapter)Utils.Trader).InsToOrderCount.ContainsKey(ins))
                {
                    limitLabel.Enabled = vpb.Enabled = false;
                    vpb.Color = Color.Gray;
                }

                vpbWaveRatio.Value = (int)(waveRatio * 10000);

                if (waveRatio >= Utils.涨跌幅提示)
                {
                    vpbWaveRatio.Color = Color.Red;
                }
                else
                {
                    if (waveRatio > 0.8)
                    {
                        vpbWaveRatio.Color = Color.Orange;
                    }
                    else
                    {
                        vpbWaveRatio.Color = Color.Green;
                    }
                }

                vpbLongCount.Value = (int)marketData.近期多头势力;

                vpbShortCount.Value = (int)marketData.近期空头势力;
            }
        }

        private ConcurrentDictionary<string, VerticalProgressBar.VerticalProgressBar> InsToVpb = new ConcurrentDictionary<string, VerticalProgressBar.VerticalProgressBar>();
        private ConcurrentDictionary<string, VerticalProgressBar.VerticalProgressBar> InsToVpbWaveRatio = new ConcurrentDictionary<string, VerticalProgressBar.VerticalProgressBar>();
        private ConcurrentDictionary<string, VerticalProgressBar.VerticalProgressBar> InsToVpbLongCount = new ConcurrentDictionary<string, VerticalProgressBar.VerticalProgressBar>();
        private ConcurrentDictionary<string, VerticalProgressBar.VerticalProgressBar> InsToVpbShortCount = new ConcurrentDictionary<string, VerticalProgressBar.VerticalProgressBar>();

        private ConcurrentDictionary<string, Label> InsToLimitLabel = new ConcurrentDictionary<string, Label>();

        private Point p;
        private Point p2;
        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            p.X = e.X;
            p.Y = e.Y;
        }

        private void OpenByItem(ListViewItem item, double offset)
        {


            if (item != null)
            {
                var ins = item.SubItems[1].Text;

                if (_trader.UnFinishedOrderFields.Count > 0 && _trader.UnFinishedOrderFields.ContainsKey(ins))
                {
                    Utils.WriteLine(string.Format("{0}有未成交单，不报新单...", ins), true);
                    return;
                }

                var price = Convert.ToDouble(item.SubItems[6].Text);
                int vol = 1;
                if (item.SubItems.Count > 8)
                {
                    vol = Convert.ToInt32(item.SubItems[8].Text);
                }

                //if (_trader.PositionFields.Count >= 4)
                //{
                //    var pos = _trader.PositionFields.Values.Where(ppp => ppp.InstrumentID.Equals(ins));
                //    if (!(pos.Count() > 0))
                //    {
                //        Utils.WriteLine("不超品种数量持仓...", true);
                //        return;
                //    }
                //}

                //if (_trader.PositionFields.Count > 0)
                //{
                //    Utils.WriteLine("持仓不报单...", true);
                //    return;
                //}

                if (Utils.InstrumentToLastTick.ContainsKey(ins))
                {
                    var lastTick = Utils.InstrumentToLastTick[ins];
                    var info = Utils.InstrumentToInstrumentInfo[ins];

                    var adjustedOffset = ((int)(offset / info.PriceTick)) * info.PriceTick;
                    var adjustedOffset2 = ((int)((offset / 2) / info.PriceTick)) * info.PriceTick;
                    var targetVolume = 1;

                    if (cbTargetMoney.Checked)
                    {
                        var targetMoney = Utils.单手总金额;
                        targetVolume = (int)Math.Round(targetMoney / (info.VolumeMultiple * lastTick.LastPrice), MidpointRounding.AwayFromZero);
                    }

                    if (item.SubItems[2].Text.Equals("涨") || item.SubItems[2].Text.Equals("兴"))
                    {
                        if (!_trader.ContainsPositionByInstrument(ins, EnumPosiDirectionType.Short))  //持有空仓不开多仓
                        {                      
                            _trader.ReqOrderInsert(ins, EnumDirectionType.Buy, lastTick.LastPrice - adjustedOffset, targetVolume, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "自动开多仓");
                            _trader.ReqOrderInsert(ins, EnumDirectionType.Buy, lastTick.LastPrice - adjustedOffset2, targetVolume, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "自动开多仓");

                            InsTobBuyOpen[ins] = false;
                            InsTodtBuyOpen[ins] = DateTime.Now;

                            //Email.SendMessage(true, "liu7788414", "15800377605", string.Format("{0}触发买单信号!", ins));
                        }
                        else
                        {
                            Utils.WriteLine("持有空仓不开多仓", true);
                        }
                    }
                    else
                    {
                        if (!_trader.ContainsPositionByInstrument(ins, EnumPosiDirectionType.Long)) //持有多仓不开空仓
                        {
                            _trader.ReqOrderInsert(ins, EnumDirectionType.Sell, lastTick.LastPrice + adjustedOffset, targetVolume, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "自动开空仓");
                            _trader.ReqOrderInsert(ins, EnumDirectionType.Sell, lastTick.LastPrice + adjustedOffset2, targetVolume, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "自动开空仓");

                            InsTobSellOpen[ins] = false;
                            InsTodtSellOpen[ins] = DateTime.Now;

                            //Email.SendMessage(true, "liu7788414", "15800377605", string.Format("{0}触发卖单信号!", ins));
                        }
                        else
                        {
                            Utils.WriteLine("持有多仓不开空仓", true);
                        }
                    }
                }
            }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var item = listView1.GetItemAt(p.X, p.Y);
            OpenByItem(item, Utils.开仓偏移量);
        }

        private void CloseAll(string longReason, string shortReason)
        {
            for (var i = listView2.Items.Count - 1; i >= 0; i--)
            {
                ClosePositionByItem(listView2.Items[i], longReason, shortReason);
            }
        }

        private void btCloseAll_Click(object sender, EventArgs e)
        {
            CloseAll("手工全平", "手工全平");
        }

        private void listView2_MouseDown(object sender, MouseEventArgs e)
        {
            p2.X = e.X;
            p2.Y = e.Y;
        }

        public void SetUpStatus(string message)
        {
            toolStripStatusLabel1.Text = message;

            var insert = message + "\n";
            richTextBox1.AppendText(insert);
            richTextBox1.Focus();
            Application.DoEvents();
        }

        public void SetDownStatus(string message)
        {
            toolStripStatusLabel2.Text = message;

            var insert = message + "\n";
            richTextBox2.AppendText(insert);
            richTextBox2.Focus();
            Application.DoEvents();
        }

        private void ClosePositionByItem(ListViewItem li, string longReason, string shortReason)
        {
            if (li != null)
            {
                var ins = li.SubItems[1].Text;
                var longOrShort = li.SubItems[2].Text;
                var vol = Convert.ToInt32(li.SubItems[3].Text);

                if (Utils.InstrumentToLastTick.ContainsKey(ins))
                {
                    var lastTick = Utils.InstrumentToLastTick[ins];
                    if (longOrShort.Equals("多"))
                    {

                        _trader.ReqOrderInsert(ins, EnumDirectionType.Sell, lastTick.LowerLimitPrice, vol, EnumOffsetFlagType.CloseToday, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, longReason);
                        _trader.ReqOrderInsert(ins, EnumDirectionType.Sell, lastTick.LowerLimitPrice, vol, EnumOffsetFlagType.CloseYesterday, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, longReason);

                        InsTobBuyOpen[ins] = false;
                        InsTodtBuyOpen[ins] = DateTime.Now;
                        //cbAutoOpen.Checked = true;
                    }
                    else
                    {
                        _trader.ReqOrderInsert(ins, EnumDirectionType.Buy, lastTick.UpperLimitPrice, vol, EnumOffsetFlagType.CloseToday, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, shortReason);
                        _trader.ReqOrderInsert(ins, EnumDirectionType.Buy, lastTick.UpperLimitPrice, vol, EnumOffsetFlagType.CloseYesterday, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, shortReason);

                        InsTobSellOpen[ins] = false;
                        InsTodtSellOpen[ins] = DateTime.Now;
                        //cbAutoOpen.Checked = true;
                    }

                    CancelAllOrders();
                }
            }
        }

        private void listView2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var li = listView2.GetItemAt(p2.X, p2.Y);
            ClosePositionByItem(li, "手工平多仓", "手工平空仓");
        }

        private void cbEnable_CheckedChanged(object sender, EventArgs e)
        {
            nudLossPoint.Enabled = nudProfitPoint.Enabled = ((CheckBox)sender).Checked;
        }

        private void toolStripStatusLabel1_MouseDown(object sender, MouseEventArgs e)
        {
            richTextBox1.Show();
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            richTextBox1.Hide();
            richTextBox2.Hide();
        }

        private void toolStripStatusLabel2_MouseDown(object sender, MouseEventArgs e)
        {
            richTextBox2.Show();
        }

        private void btOKTotal_Click(object sender, EventArgs e)
        {
            stopProfitTotal = Convert.ToDouble(tbStopProfitTotal.Text);
            stopLossTotal = Convert.ToDouble(tbStopLossTotal.Text);
        }

        private void cbEnableTotal_CheckedChanged(object sender, EventArgs e)
        {
            tbStopLossTotal.Enabled = tbStopProfitTotal.Enabled = btOKTotal.Enabled = ((CheckBox)sender).Checked;
        }

        private void lbHighTotal_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            lbHighTotal.Text = "-99999";
            lbLowTotal.Text = "99999";
        }

        private void lbLowTotal_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            lbHighTotal.Text = "-99999";
            lbLowTotal.Text = "99999";
        }

        private void nudCloseRatio_ValueChanged(object sender, EventArgs e)
        {
            closeRatio = Convert.ToDouble(nudCloseRatio.Value);
            Utils.WriteLine(string.Format("平仓比例设为{0}", closeRatio), true);
        }

        private void cbWarning_CheckedChanged(object sender, EventArgs e)
        {
            nudWarningPoint.Enabled = nudCloseRatio.Enabled = ((CheckBox)sender).Checked;
        }

        private void richTextBox3_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (richTextBox1.ForeColor == Color.Red)
            {
                richTextBox1.ForeColor = Color.White;
            }
            else
            {
                richTextBox1.ForeColor = Color.Red;
            }
        }

        private void nudProfitPoint_ValueChanged(object sender, EventArgs e)
        {
            stopProfitPoint = Convert.ToDouble(nudProfitPoint.Value);
        }

        private void nudLossPoint_ValueChanged(object sender, EventArgs e)
        {
            stopLossPoint = -Convert.ToDouble(nudLossPoint.Value);
        }

        private void nudWarningPoint_ValueChanged(object sender, EventArgs e)
        {
            warningTick = Convert.ToDouble(nudWarningPoint.Value);
            //Utils.WriteLine(string.Format("警戒线设为{0}", warningTick), true);
        }

        private void cbOverTime_CheckedChanged(object sender, EventArgs e)
        {
            nudOverTimePoint.Enabled = ((CheckBox)sender).Checked;
        }

        private void nudOverTimePoint_ValueChanged(object sender, EventArgs e)
        {
            overtimePoint = -Convert.ToDouble(nudOverTimePoint.Value);
            Utils.WriteLine(string.Format("超时止损点设为{0}", overtimePoint), true);
        }

        private void btUpDownRatio_Click(object sender, EventArgs e)
        {
            Utils.涨跌幅提示 = Convert.ToDouble(tbUpDownRatio.Text);
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            CancelAllOrders();
        }

        private void OpenByButtonSell(Button button)
        {
            if (!string.IsNullOrEmpty(tbIns.Text))
            {
                var ins = tbIns.Text;
                if (Utils.InstrumentToLastTick.ContainsKey(ins))
                {
                    var lastTick = Utils.InstrumentToLastTick[ins];
                    var info = Utils.InstrumentToInstrumentInfo[ins];

                    if (cbTargetMoney.Checked)
                    {
                        var targetMoney = Utils.单手总金额;
                        var targetVolume = (int)Math.Round(targetMoney / (info.VolumeMultiple * lastTick.LastPrice), MidpointRounding.AwayFromZero);
                        _trader.ReqOrderInsert(ins, EnumDirectionType.Sell, lastTick.LastPrice + Convert.ToDouble(button.Text) * info.PriceTick, targetVolume, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "手工开空仓");
                    }
                    else
                    {
                        _trader.ReqOrderInsert(ins, EnumDirectionType.Sell, lastTick.LastPrice + Convert.ToDouble(button.Text) * info.PriceTick, 1, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "手工开空仓");
                    }
                }
            }
        }

        private void btBuy1_Click(object sender, EventArgs e)
        {
            OpenByButtonBuy((Button)sender);
        }

        private void OpenByButtonBuy(Button button)
        {
            if (!string.IsNullOrEmpty(tbIns.Text))
            {
                var ins = tbIns.Text;
                if (Utils.InstrumentToLastTick.ContainsKey(ins))
                {
                    var lastTick = Utils.InstrumentToLastTick[ins];
                    var info = Utils.InstrumentToInstrumentInfo[ins];

                    if (cbTargetMoney.Checked)
                    {
                        var targetMoney = Utils.单手总金额;
                        var targetVolume = (int)Math.Round(targetMoney / (info.VolumeMultiple * lastTick.LastPrice), MidpointRounding.AwayFromZero);
                        _trader.ReqOrderInsert(ins, EnumDirectionType.Buy, lastTick.LastPrice - Convert.ToDouble(button.Text) * info.PriceTick, targetVolume, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "手工开多仓");
                    }
                    else
                    {
                        _trader.ReqOrderInsert(ins, EnumDirectionType.Buy, lastTick.LastPrice - Convert.ToDouble(button.Text) * info.PriceTick, 1, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "手工开多仓");
                    }
                }
            }
        }

        private void btSell1_Click(object sender, EventArgs e)
        {
            OpenByButtonSell((Button)sender);
        }

        private void btBuy3_Click(object sender, EventArgs e)
        {
            OpenByButtonBuy((Button)sender);
        }

        private void btBuy5_Click(object sender, EventArgs e)
        {
            OpenByButtonBuy((Button)sender);
        }

        private void btBuy7_Click(object sender, EventArgs e)
        {
            OpenByButtonBuy((Button)sender);
        }

        private void btBuy9_Click(object sender, EventArgs e)
        {
            OpenByButtonBuy((Button)sender);
        }

        private void btSell3_Click(object sender, EventArgs e)
        {
            OpenByButtonSell((Button)sender);
        }

        private void btSell5_Click(object sender, EventArgs e)
        {
            OpenByButtonSell((Button)sender);
        }

        private void btSell7_Click(object sender, EventArgs e)
        {
            OpenByButtonSell((Button)sender);
        }

        private void btSell9_Click(object sender, EventArgs e)
        {
            OpenByButtonSell((Button)sender);
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems != null && listView1.SelectedItems.Count > 0)
            {
                tbIns.Text = listView1.SelectedItems[0].SubItems[1].Text;
            }
        }

        private void btBuy11_Click(object sender, EventArgs e)
        {
            OpenByButtonBuy((Button)sender);
        }

        private void btBuy13_Click(object sender, EventArgs e)
        {
            OpenByButtonBuy((Button)sender);
        }

        private void btBuy15_Click(object sender, EventArgs e)
        {
            OpenByButtonBuy((Button)sender);
        }

        private void btBuy17_Click(object sender, EventArgs e)
        {
            OpenByButtonBuy((Button)sender);
        }

        private void btBuy19_Click(object sender, EventArgs e)
        {
            OpenByButtonBuy((Button)sender);
        }

        private void btSell11_Click(object sender, EventArgs e)
        {
            OpenByButtonSell((Button)sender);
        }

        private void btSell13_Click(object sender, EventArgs e)
        {
            OpenByButtonSell((Button)sender);
        }

        private void btSell15_Click(object sender, EventArgs e)
        {
            OpenByButtonSell((Button)sender);
        }

        private void btSell17_Click(object sender, EventArgs e)
        {
            OpenByButtonSell((Button)sender);
        }

        private void btSell19_Click(object sender, EventArgs e)
        {
            OpenByButtonSell((Button)sender);
        }

        private void CancelAllOrders()
        {
            var ordersToCancel = new List<ThostFtdcOrderField>();

            foreach (var order in _trader.UnFinishedOrderFields.Values)
            {
                ordersToCancel.Add(order);
            }

            foreach (var order in ordersToCancel)
            {
                _trader.ReqOrderAction(order.FrontID, order.SessionID, order.OrderRef, order.InstrumentID);
            }
        }

        private void lvOrder_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            CancelAllOrders();
        }

        private void lvOrder_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            CancelAllOrders();
        }

        private void btPosition_Click(object sender, EventArgs e)
        {
            lbHighTotal.Text = "-99999";
            lbLowTotal.Text = "99999";
            _trader.ReqQryInvestorPosition();
        }

        private void cbTargetMoney_CheckedChanged(object sender, EventArgs e)
        {
            labelTargetMoney.Enabled = tbTargetMoney.Enabled = ((CheckBox)sender).Checked;
        }

        private void btBuy9_Click_1(object sender, EventArgs e)
        {
            OpenByButtonBuy((Button)sender);
        }

        private void btSell9_Click_1(object sender, EventArgs e)
        {
            OpenByButtonSell((Button)sender);
        }

        private void lvMainIns_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvMainIns.SelectedItems != null && lvMainIns.SelectedItems.Count > 0)
            {
                tbIns.Text = lvMainIns.SelectedItems[0].SubItems[1].Text;
            }
        }

        private void label_Click(object sender, EventArgs e)
        {
            tbIns.Text = ((Label)sender).Text;
        }

        private void cbAutoClose_CheckedChanged(object sender, EventArgs e)
        {
            if(cbAutoCloseProfit.Checked)
            {
                cbAutoCloseProfit.BackColor = Color.Gray;
            }
            else
            {
                cbAutoCloseProfit.BackColor = Color.Red;
            }
        }

        private void tbTargetMoney_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Utils.单手总金额 = Convert.ToDouble(tbTargetMoney.Text);
            }
            catch
            {
                Utils.单手总金额 = 200000;
            }
        }

        private void nudOffsetTickCount_ValueChanged(object sender, EventArgs e)
        {
            Utils.偏移Tick数量 = (int)nudOffsetTickCount.Value;
        }
    }

    public class PromptItem
    {
        public DateTime Time;
        public List<string> MessageItems;
        public string InstrumentId;
        public string OpenOrClose;
        public string Direction;
        public double Price;
        public int Volume;
        public double Offset;
        public double Ratio;
    }
}
