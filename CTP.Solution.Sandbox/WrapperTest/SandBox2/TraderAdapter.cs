using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CTP;
using log4net.Layout;
using SendMail;
using System.Timers;

namespace WrapperTest
{
    public class TraderAdapter : CTPTraderAdapter, ITraderAdapter
    {
        public static int RequestId = 1;
        public static int CurrentOrderRef;

        private string _tradingDay;

        public string TradingDay
        {
            get { return _tradingDay; }
            set { _tradingDay = value; }
        }

        private int _frontId;

        public int FrontId
        {
            get { return _frontId; }
            set { _frontId = value; }
        }

        private int _sessionId;

        public int SessionId
        {
            get { return _sessionId; }
            set { _sessionId = value; }
        }

        private string _brokerId;

        public string BrokerId
        {
            get { return _brokerId; }
            set { _brokerId = value; }
        }

        private string _investorId;

        public string InvestorId
        {
            get { return _investorId; }
            set { _investorId = value; }
        }

        private string _password;

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        private string[] _front;

        public string[] Front
        {
            get { return _front; }
            set { _front = value; }
        }

        //private bool _isReady;

        //public bool IsReady
        //{
        //    get { return _isReady; }
        //    set { _isReady = value; }
        //}

        private ConcurrentDictionary<string, ThostFtdcInvestorPositionField> _positionFields =
            new ConcurrentDictionary<string, ThostFtdcInvestorPositionField>();

        public ConcurrentDictionary<string, ThostFtdcInvestorPositionField> PositionFields
        {
            get { return _positionFields; }
            set { _positionFields = value; }
        }

        private ConcurrentDictionary<string, ThostFtdcOrderField> _unfinishedOrderFields =
            new ConcurrentDictionary<string, ThostFtdcOrderField>();

        public ConcurrentDictionary<string, ThostFtdcOrderField> UnFinishedOrderFields
        {
            get { return _unfinishedOrderFields; }
            set { _unfinishedOrderFields = value; }
        }

        private System.Timers.Timer _timerReportPosition = new System.Timers.Timer(1000 * 60 * 60); //每小时报告一次
        private System.Timers.Timer _timerCancelOrder = new System.Timers.Timer(1000 * 10);

        public TraderAdapter()
        {
            _timerReportPosition.Elapsed += _timerReportPosition_Elapsed;
            _timerReportPosition.Start();

            _timerCancelOrder.Elapsed += _timerCancelOrder_Elapsed;
            _timerCancelOrder.Start();

            OnFrontConnected += TraderAdapter_OnFrontConnected;
            OnRspUserLogin += TraderAdapter_OnRspUserLogin;
            OnRspSettlementInfoConfirm += TraderAdapter_OnRspSettlementInfoConfirm;
            OnRspQryInstrument += TraderAdapter_OnRspQryInstrument;
            OnRspQryTradingAccount += TraderAdapter_OnRspQryTradingAccount;
            OnRspQryInvestorPositionDetail += TraderAdapter_OnRspQryInvestorPositionDetail;
            OnRspQryInvestorPosition += TraderAdapter_OnRspQryInvestorPosition;
            OnRtnOrder += TraderAdapter_OnRtnOrder;
            OnErrRtnOrderInsert += TraderAdapter_OnErrRtnOrderInsert;
            OnRspOrderAction += TraderAdapter_OnRspOrderAction;
            OnRspQryDepthMarketData += TraderAdapter_OnRspQryDepthMarketData;
            OnFrontDisconnected += TraderAdapter_OnFrontDisconnected;
            OnRspOrderInsert += TraderAdapter_OnRspOrderInsert;
            OnErrRtnOrderAction += TraderAdapter_OnErrRtnOrderAction;
            OnHeartBeatWarning += TraderAdapter_OnHeartBeatWarning;
            OnRspError += TraderAdapter_OnRspError;
            OnRtnTrade += TraderAdapter_OnRtnTrade;
            OnRspUserLogout += TraderAdapter_OnRspUserLogout;
            OnRspQryOrder += TraderAdapter_OnRspQryOrder;
            OnRspQryTrade += TraderAdapter_OnRspQryTrade;
        }

        private void _timerCancelOrder_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var ordersToCancel = new List<ThostFtdcOrderField>();

                foreach (var order in UnFinishedOrderFields.Values)
                {
                    //只撤开仓单
                    if ((order.CombOffsetFlag_0 == EnumOffsetFlagType.Open) && (order.OrderStatus == EnumOrderStatusType.NoTradeQueueing || order.OrderStatus == EnumOrderStatusType.PartTradedQueueing))
                    {
                        var updateTime = Convert.ToDateTime(order.InsertTime);

                        if (DateTime.Now + new TimeSpan(0, 0, Utils.ExchangeTimeOffset) - updateTime > new TimeSpan(0, 0, 30))
                        {
                            ordersToCancel.Add(order);
                        }
                    }
                }

                foreach (var order in ordersToCancel)
                {
                    if (Utils.IsInInstrumentTradingTime(order.InstrumentID))
                    {
                        ReqOrderAction(order.FrontID, order.SessionID, order.OrderRef, order.InstrumentID);
                    }
                }

                ordersToCancel.Clear();

                //撤平仓单
                foreach (var order in UnFinishedOrderFields.Values)
                {
                    if (order.CombOffsetFlag_0 == EnumOffsetFlagType.Close || order.CombOffsetFlag_0 == EnumOffsetFlagType.CloseToday || order.CombOffsetFlag_0 == EnumOffsetFlagType.CloseYesterday)
                    {
                        var updateTime = Convert.ToDateTime(order.InsertTime);

                        if (DateTime.Now + new TimeSpan(0, 0, Utils.ExchangeTimeOffset) - updateTime > new TimeSpan(0, 0, 30))
                        {
                            ordersToCancel.Add(order);
                        }
                    }
                }

                foreach (var order in ordersToCancel)
                {
                    if (Utils.IsInInstrumentTradingTime(order.InstrumentID))
                    {
                        ReqOrderAction(order.FrontID, order.SessionID, order.OrderRef, order.InstrumentID);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        /// <summary>
        /// 根据委托撤单信息构成键,ExchangeID + OrderSysID
        /// </summary>
        /// <param name="pOrder"></param>
        /// <returns></returns>
        private string GetOrderKey(ThostFtdcOrderField pOrder)
        {
            return string.Format("{0}:{1}:{2}", pOrder.FrontID, pOrder.SessionID, pOrder.OrderRef);
        }

        //private string GetOrderKey(ThostFtdcTradeField pTrade)
        //{
        //    return string.Format("{0}:{1}:{2}", pTrade.ExchangeID, pTrade.OrderSysID);
        //}

        private string GetOrderKey(ThostFtdcInputOrderActionField pInputOrderAction)
        {
            return string.Format("{0}:{1}:{2}", pInputOrderAction.FrontID, pInputOrderAction.SessionID, pInputOrderAction.OrderRef);
        }

        private void TraderAdapter_OnRspQryTrade(ThostFtdcTradeField pTrade, ThostFtdcRspInfoField pRspInfo, int nRequestID, bool bIsLast)
        {
            try
            {
                Utils.ReportError(pRspInfo, "查询成交回报错误");
                if (pTrade != null)
                {
                    var temp =
                        string.Format(
                            "查询成交回报:合约:{0},买卖:{1},交易所:{2},开平:{3},成交量:{4},报单引用:{5},成交时间:{6},成交日期:{7},系统号:{8},成交价:{9}",
                            pTrade.InstrumentID, pTrade.Direction, pTrade.ExchangeID, pTrade.OffsetFlag, pTrade.Volume,
                            pTrade.OrderRef, pTrade.TradeTime, pTrade.TradeDate, pTrade.OrderSysID, pTrade.Price);

                    Utils.WriteLine(temp, true);
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        public int ReqOrderAction(int frontId, int sessionId, string orderRef, string instrumentId, EnumActionFlagType actionFlag = EnumActionFlagType.Delete)
        {
            Utils.WriteLine(string.Format("撤单:前置编号:{0},会话编号:{1},报单引用:{2},合约:{3}", frontId, sessionId, orderRef, instrumentId), true);

            var req = new ThostFtdcInputOrderActionField
            {
                ActionFlag = actionFlag,
                BrokerID = _brokerId,
                InvestorID = _investorId,
                FrontID = frontId,
                SessionID = sessionId,
                OrderRef = orderRef,
                InstrumentID = instrumentId
            };

            int ret = ReqOrderAction(req, ++RequestId);

            return ret;
        }

        public int ReqOrderAction(string exchangeId, string orderSysId, string instrumentId, EnumActionFlagType actionFlag = EnumActionFlagType.Delete)
        {
            Utils.WriteLine(string.Format("撤单:交易所:{0},系统编号:{1},合约:{2}", exchangeId, orderSysId, instrumentId), true);

            var req = new ThostFtdcInputOrderActionField
            {
                ActionFlag = actionFlag,
                ExchangeID = exchangeId,
                OrderSysID = orderSysId,
                InstrumentID = instrumentId
            };

            int ret = ReqOrderAction(req, ++RequestId);

            return ret;
        }

        void TraderAdapter_OnRspQryOrder(ThostFtdcOrderField pOrder, ThostFtdcRspInfoField pRspInfo, int nRequestID, bool bIsLast)
        {
            try
            {
                Utils.ReportError(pRspInfo, "查询委托回报错误");

                if (pOrder != null)
                {
                    var temp = string.Format(
                        "查询委托回报:合约:{0},买卖:{1},交易所:{2},开平:{3},委托状态:{4},委托价格类型:{5},委托价格:{6},委托数量:{7},已成交数量:{8},报单引用:{9},前台编号:{10},对话编号:{11},系统号:{12}",
                        pOrder.InstrumentID, pOrder.Direction, pOrder.ExchangeID, pOrder.CombOffsetFlag_0, pOrder.OrderStatus, pOrder.OrderPriceType, pOrder.LimitPrice, pOrder.VolumeTotalOriginal, pOrder.VolumeTraded,
                        pOrder.OrderRef, pOrder.FrontID, pOrder.SessionID, pOrder.OrderSysID);

                    Utils.WriteLine(temp);


                    if (pOrder.OrderStatus == EnumOrderStatusType.PartTradedQueueing || pOrder.OrderStatus == EnumOrderStatusType.NoTradeQueueing)
                    {
                        UnFinishedOrderFields[GetOrderKey(pOrder)] = pOrder;
                    }
                }

                if (bIsLast)
                {
                    Utils.IsTraderReady = true;
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        void TraderAdapter_OnRspUserLogout(ThostFtdcUserLogoutField pUserLogout, ThostFtdcRspInfoField pRspInfo,
            int nRequestID, bool bIsLast)
        {
            Utils.IsTraderReady = false;
            Utils.WriteLine(string.Format("登出回报,设置准备状态为{0}", Utils.IsTraderReady), true);
        }

        public bool IsLessThanCategoryUpperLimit()
        {
            try
            {
                if (PositionFields.Count < Utils.CategoryUpperLimit - Utils.LockedOpenInstruments.Count)
                {
                    return true;
                }

                if (Utils.LockedOpenInstruments.Count > 0)
                {
                    Utils.WriteLine(string.Format("开仓后会超过 品种上限:{0} 和 开仓在途:{1}之差:{2}，不能开仓...", Utils.CategoryUpperLimit,
                        Utils.LockedOpenInstruments.Count, Utils.CategoryUpperLimit - Utils.LockedOpenInstruments.Count));
                }


            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
            return false;
        }

        /// <summary>
        /// 开合约的仓位
        /// </summary>
        /// <param name="instrumentId"></param>
        /// <param name="reason"></param>
        /// <param name="longOrShort"></param>
        public void OpenPositionByInstrument(string instrumentId, string reason, EnumPosiDirectionType longOrShort, double openTrendStartPoint, bool bConsiderOppositePosition, bool bForceOpen, double openAngle, double price)
        {
            try
            {
                if (Utils.IsOpenLocked)
                {
                    Utils.WriteLine("目前禁止开仓!", true);
                    return;
                }

                var buyOrSell = longOrShort == EnumPosiDirectionType.Long
                    ? EnumDirectionType.Buy
                    : EnumDirectionType.Sell;

                var keyToday = Utils.GetPositionKey(instrumentId, longOrShort,
                    EnumPositionDateType.Today);
                var keyHistory = Utils.GetPositionKey(instrumentId, longOrShort,
                    EnumPositionDateType.History);

                //只有主力合约才考虑开仓，否则只能平仓；只有在合约的交易时间才开仓；只有没有该合约的持仓时，才开仓；持有品种的数量不能超过上限
                if (Utils.IsTradableInstrument(instrumentId) && Utils.IsInInstrumentTradingTime(instrumentId) &&
                    !PositionFields.ContainsKey(keyToday) && !PositionFields.ContainsKey(keyHistory) &&
                    IsLessThanCategoryUpperLimit()) //没有空仓，卖出开仓
                {
                    if (IsUnFinishedOrderExisting(instrumentId, buyOrSell, EnumOffsetFlagType.Open))
                    //该合约还有未完成的开仓报单，不报单
                    {
                        return;
                    }

                    if (bConsiderOppositePosition)
                    {
                        var opposite = longOrShort == EnumPosiDirectionType.Long
                            ? EnumPosiDirectionType.Short
                            : EnumPosiDirectionType.Long;

                        var keyOppositeToday = Utils.GetPositionKey(instrumentId, opposite, EnumPositionDateType.Today);
                        var keyOppositeHistory = Utils.GetPositionKey(instrumentId, opposite,
                            EnumPositionDateType.History);

                        if (PositionFields.ContainsKey(keyOppositeToday) ||
                            PositionFields.ContainsKey(keyOppositeHistory))
                        {
                            //Utils.WriteLine(
                            //    string.Format("持有{0}的{1}仓,趋势还未破坏,不能开{2}仓,当前启动点{3}", instrumentId, opposite, longOrShort,
                            //        openTrendStartPoint), true);
                            return;
                        }
                    }

                    if (Utils.InstrumentToMaxAndMinPrice.ContainsKey(instrumentId))
                    {
                        var mamp = Utils.InstrumentToMaxAndMinPrice[instrumentId];

                        if (Utils.InstrumentToQuotes.ContainsKey(instrumentId))
                        {
                            OrderInsertOffsetPrice(instrumentId, buyOrSell, Utils.OpenVolumePerTime,
                                EnumOffsetFlagType.Open, Utils.开仓偏移量,
                                reason, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, price);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        /// <summary>
        /// 开合约的空仓
        /// </summary>
        /// <param name="instrumentId"></param>
        /// <param name="reason"></param>
        public void OpenShortPositionByInstrument(string instrumentId, string reason, double openTrendStartPoint, bool bConsiderOppositePosition, bool bForceOpen, double openAngle, double price)
        {
            OpenPositionByInstrument(instrumentId, reason, EnumPosiDirectionType.Short, openTrendStartPoint, bConsiderOppositePosition, bForceOpen, openAngle, price);
        }

        /// <summary>
        /// 开合约的多仓
        /// </summary>
        /// <param name="instrumentId"></param>
        /// <param name="reason"></param>
        public void OpenLongPositionByInstrument(string instrumentId, string reason, double openTrendStartPoint, bool bConsiderOppositePosition, bool bForceOpen, double openAngle, double price)
        {
            OpenPositionByInstrument(instrumentId, reason, EnumPosiDirectionType.Long, openTrendStartPoint, bConsiderOppositePosition, bForceOpen, openAngle, price);
        }

        /// <summary>
        /// 平掉合约的仓位
        /// </summary>
        /// <param name="instrumentId"></param>
        /// <param name="reason"></param>
        /// <param name="longOrShort"></param>
        /// <param name="isForceClose">是否强制平仓操作</param>
        public void ClosePositionByInstrument(string instrumentId, string reason, EnumPosiDirectionType longOrShort, double offset,
            bool isForceClose = false, double price = 0)
        {
            try
            {
                var keyToday = Utils.GetPositionKey(instrumentId, longOrShort, EnumPositionDateType.Today);
                var keyHistory = Utils.GetPositionKey(instrumentId, longOrShort, EnumPositionDateType.History);

                if ((isForceClose || Utils.IsInInstrumentTradingTime(instrumentId)) &&
                    (PositionFields.ContainsKey(keyToday) || PositionFields.ContainsKey(keyHistory)))
                {
                    //平今仓
                    if (PositionFields.ContainsKey(keyToday))
                    {
                        if (!IsUnFinishedOrderExisting(instrumentId,
                            longOrShort == EnumPosiDirectionType.Long ? EnumDirectionType.Sell : EnumDirectionType.Buy,
                            EnumOffsetFlagType.CloseToday)) //是否存在未完成的平今仓报单，如果存在，不报单
                        {
                            var pos = PositionFields[keyToday];

                            var offsetFlag = EnumOffsetFlagType.CloseToday;

                            //平今仓
                            OrderInsertOffsetPrice(instrumentId,
                                longOrShort == EnumPosiDirectionType.Long
                                    ? EnumDirectionType.Sell
                                    : EnumDirectionType.Buy,
                                pos.Position,
                                offsetFlag, offset, reason, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, price);
                        }
                    }

                    //平昨仓
                    if (PositionFields.ContainsKey(keyHistory))
                    {
                        if (!IsUnFinishedOrderExisting(instrumentId,
                            longOrShort == EnumPosiDirectionType.Long ? EnumDirectionType.Sell : EnumDirectionType.Buy,
                            EnumOffsetFlagType.CloseYesterday)) //是否存在未完成的平昨仓报单，如果存在，不报单
                        {
                            var pos = PositionFields[keyHistory];

                            var offsetFlag = EnumOffsetFlagType.CloseYesterday;

                            //平昨仓
                            OrderInsertOffsetPrice(instrumentId,
                                longOrShort == EnumPosiDirectionType.Long
                                    ? EnumDirectionType.Sell
                                    : EnumDirectionType.Buy,
                                pos.Position,
                                offsetFlag, offset, reason, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, price);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        /// <summary>
        /// 平掉合约的多仓
        /// </summary>
        /// <param name="instrumentId"></param>
        /// <param name="reason"></param>
        /// <param name="isForceClose"></param>
        public void CloseLongPositionByInstrument(string instrumentId, string reason, bool isForceClose, double price)
        {
            ClosePositionByInstrument(instrumentId, reason, EnumPosiDirectionType.Long, -10, isForceClose, price);
        }

        /// <summary>
        /// 平掉合约的空仓
        /// </summary>
        /// <param name="instrumentId"></param>
        /// <param name="reason"></param>
        /// <param name="isForceClose"></param>
        public void CloseShortPositionByInstrument(string instrumentId, string reason, bool isForceClose, double price)
        {
            ClosePositionByInstrument(instrumentId, reason, EnumPosiDirectionType.Short, -10, isForceClose, price);
        }

        private void SetStopLossPrice(ThostFtdcTradeField pTrade, StopLossPrices stopLossPrices)
        {
            try
            {
                if (pTrade.Direction == EnumDirectionType.Buy) //买开，调整多仓的止损参考价
                {
                    stopLossPrices.CostLong = pTrade.Price;
                    stopLossPrices.ForLong = pTrade.Price;
                    Utils.WriteLine(
                        string.Format("设置多仓的成本价为{0},调整{1}的多仓止损参考价为{2}", stopLossPrices.CostLong, pTrade.InstrumentID,
                            stopLossPrices.ForLong), true);
                }
                else //卖开，调整空仓的止损参考价
                {
                    stopLossPrices.CostShort = pTrade.Price;
                    stopLossPrices.ForShort = pTrade.Price;
                    Utils.WriteLine(
                        string.Format("设置空仓的成本价为{0},调整{1}的空仓止损参考价为{2}", stopLossPrices.CostShort, pTrade.InstrumentID,
                            stopLossPrices.ForShort), true);
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private void TraderAdapter_OnRtnTrade(ThostFtdcTradeField pTrade)
        {
            try
            {
                if (pTrade != null)
                {
                    var temp =
                        string.Format(
                            "成交回报:合约:{0},买卖:{1},交易所:{2},开平:{3},成交量:{4},报单引用:{5},成交时间:{6},成交日期:{7},系统号:{8},成交价:{9}",
                            pTrade.InstrumentID, pTrade.Direction, pTrade.ExchangeID, pTrade.OffsetFlag, pTrade.Volume,
                            pTrade.OrderRef, pTrade.TradeTime, pTrade.TradeDate, pTrade.OrderSysID, pTrade.Price);

                    Utils.WriteLine(temp, true);

                    //三种情况，买开找多仓；卖开找空仓；其它的都是找反向仓：卖平找多仓，买平找空仓
                    EnumPosiDirectionType longOrShort;

                    if (pTrade.Direction == EnumDirectionType.Buy && pTrade.OffsetFlag == EnumOffsetFlagType.Open)
                    //买开找多仓
                    {
                        longOrShort = EnumPosiDirectionType.Long;
                        Utils.InstrumentToLastPosiDirectionType[pTrade.InstrumentID] = EnumPosiDirectionType.Long;
                        //ReqOrderInsert(pTrade.InstrumentID, EnumDirectionType.Sell,pTrade.Price+2,pTrade.Volume, EnumOffsetFlagType.CloseToday, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV,"立即平多仓");
                    }
                    else
                    {
                        if (pTrade.Direction == EnumDirectionType.Sell && pTrade.OffsetFlag == EnumOffsetFlagType.Open)
                        //卖开找空仓
                        {
                            longOrShort = EnumPosiDirectionType.Short;
                            Utils.InstrumentToLastPosiDirectionType[pTrade.InstrumentID] = EnumPosiDirectionType.Short;
                            //ReqOrderInsert(pTrade.InstrumentID, EnumDirectionType.Buy, pTrade.Price - 2, pTrade.Volume, EnumOffsetFlagType.CloseToday, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "立即平空仓");
                        }
                        else //其它的都是找反向仓：卖平找多仓，买平找空仓
                        {
                            longOrShort = pTrade.Direction == EnumDirectionType.Buy
                                ? EnumPosiDirectionType.Short
                                : EnumPosiDirectionType.Long;

                            //买平、卖平，增加开仓的冷却时间
                            var dtNow = DateTime.Now;
                            EnumDirectionType buyOrSell;

                            if (pTrade.Direction == EnumDirectionType.Buy) //买入平仓对应卖出开仓
                            {
                                buyOrSell = EnumDirectionType.Sell;
                            }
                            else //卖出平仓对应买入开仓
                            {
                                buyOrSell = EnumDirectionType.Buy;
                            }

                            Utils.InstrumentToOpenAngle[Utils.GetOpenPositionKey(pTrade.InstrumentID, buyOrSell)] = 0;
                            Utils.WriteLine(string.Format("{0}仓已平，设置{1}的{2}仓开仓角度为0", buyOrSell, pTrade.InstrumentID, buyOrSell));

                            var key = Utils.GetOpenPositionKey(pTrade.InstrumentID, buyOrSell);
                            Utils.InstrumentToLastCloseTime[key] = dtNow;
                            Utils.WriteLine(
                                string.Format("设置{0}的{1}开仓冷却时间起点为{2}", pTrade.InstrumentID, buyOrSell, dtNow),
                                true);
                        }
                    }


                    switch (pTrade.OffsetFlag)
                    {
                        //开仓成交回报，不会影响昨仓的持仓；只有开仓才需要调整止损价参考值
                        case EnumOffsetFlagType.Open:
                            {
                                if (Utils.InstrumentToStopLossPrices.ContainsKey(pTrade.InstrumentID))
                                {
                                    var stopLossPrices = Utils.InstrumentToStopLossPrices[pTrade.InstrumentID];

                                    SetStopLossPrice(pTrade, stopLossPrices);
                                }
                                else
                                {
                                    if (Utils.InstrumentToQuotes.ContainsKey(pTrade.InstrumentID))
                                    {
                                        var quotes = Utils.InstrumentToQuotes[pTrade.InstrumentID];

                                        if (quotes.Count > 0)
                                        {
                                            var quote = quotes[quotes.Count - 1];
                                            var stopLossPrices = Utils.CreateStopLossPrices(quote);
                                            SetStopLossPrice(pTrade, stopLossPrices);
                                        }
                                    }
                                }

                                var keyToday = Utils.GetPositionKey(pTrade.InstrumentID, longOrShort,
                                    EnumPositionDateType.Today);

                                Utils.WriteLine(string.Format("加今仓{0}", keyToday), true);

                                if (PositionFields.ContainsKey(keyToday))
                                {
                                    var positionToday = PositionFields[keyToday];

                                    positionToday.Position += pTrade.Volume;
                                    positionToday.TodayPosition += pTrade.Volume;
                                }
                                else
                                {
                                    PositionFields[keyToday] = new ThostFtdcInvestorPositionField
                                    {
                                        InstrumentID = pTrade.InstrumentID,
                                        PosiDirection = longOrShort,
                                        Position = pTrade.Volume,
                                        TodayPosition = pTrade.Volume,
                                        YdPosition = 0,
                                        PositionDate = EnumPositionDateType.Today
                                    };
                                }

                                Utils.RemoveLockedOpenInstrument(pTrade.InstrumentID);
                                break;
                            }
                        case EnumOffsetFlagType.Close:
                        case EnumOffsetFlagType.CloseToday: //减今仓，减到0则移除
                            {
                                ReducePosition(pTrade, longOrShort, EnumPositionDateType.Today);
                                break;
                            }
                        case EnumOffsetFlagType.CloseYesterday: //减昨仓，减到0则移除
                            {
                                ReducePosition(pTrade, longOrShort, EnumPositionDateType.History);
                                break;
                            }
                        default:
                            {
                                temp = "未处理的成交回报:" + temp;
                                break;
                            }
                    }

                    if (Utils.IsInitialized) //初始化完成后才发送成交回报，也就是交易过程中
                    {
                        //Email.SendMail("成交回报", temp, Utils.IsMailingEnabled);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private void ReducePosition(ThostFtdcTradeField pTrade, EnumPosiDirectionType longOrShort,
            EnumPositionDateType todayOrHistory)
        {
            try
            {
                var key = Utils.GetPositionKey(pTrade.InstrumentID, longOrShort, todayOrHistory);

                Utils.WriteLine(string.Format("减仓{0}", key), true);

                if (PositionFields.ContainsKey(key))
                {
                    var positionToReduce = PositionFields[key];

                    positionToReduce.Position -= pTrade.Volume;

                    if (todayOrHistory == EnumPositionDateType.Today)
                    {
                        positionToReduce.TodayPosition -= pTrade.Volume;
                    }
                    else
                    {
                        positionToReduce.YdPosition -= pTrade.Volume;
                    }

                    if (positionToReduce.Position <= 0)
                    {
                        ThostFtdcInvestorPositionField position;
                        PositionFields.TryRemove(key, out position);
                    }
                }
                else
                {
                    var temp = string.Format("错误:{0}并不含有{1}的{2}仓", pTrade.InstrumentID, longOrShort, todayOrHistory);
                    Utils.WriteLine(temp, true);
                    Email.SendMail(temp, temp, Utils.IsMailingEnabled);
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private void TraderAdapter_OnRspError(ThostFtdcRspInfoField pRspInfo, int nRequestId, bool bIsLast)
        {
            try
            {
                if (pRspInfo != null)
                {
                    Utils.OutputField(pRspInfo);
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private void TraderAdapter_OnHeartBeatWarning(int nTimeLapse)
        {
            Utils.WriteLine(string.Format("心跳警告:{0}", nTimeLapse));
        }

        private void TraderAdapter_OnErrRtnOrderAction(ThostFtdcOrderActionField pOrderAction,
            ThostFtdcRspInfoField pRspInfo)
        {
            try
            {
                Utils.ReportError(pRspInfo, "报单操作回报错误");

                if (pOrderAction != null)
                {
                    Utils.OutputField(pOrderAction);
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private void TraderAdapter_OnRspOrderInsert(ThostFtdcInputOrderField pInputOrder, ThostFtdcRspInfoField pRspInfo,
            int nRequestId, bool bIsLast)
        {
            try
            {
                Utils.ReportError(pRspInfo, "报单插入回报错误");

                if (pInputOrder != null)
                {
                    var temp =
                        string.Format(
                            "报单插入回报:组合投机套保标志:{0},组合开平标志:{1},触发条件:{2},买卖方向:{3},强平原因:{4},GTD日期:{5},合约代码:{6},自动挂起标志:{7},互换单标志:{8},价格:{9},最小成交量:{10},报单价格条件:{11},报单引用:{12},请求编号:{13},有效期类型:{14},成交量类型:{15},数量:{16}",
                            pInputOrder.CombHedgeFlag_0, pInputOrder.CombOffsetFlag_0, pInputOrder.ContingentCondition,
                            pInputOrder.Direction, pInputOrder.ForceCloseReason, pInputOrder.GTDDate,
                            pInputOrder.InstrumentID, pInputOrder.IsAutoSuspend, pInputOrder.IsSwapOrder,
                            pInputOrder.LimitPrice, pInputOrder.MinVolume, pInputOrder.OrderPriceType,
                            pInputOrder.OrderRef,
                            pInputOrder.RequestID, pInputOrder.TimeCondition, pInputOrder.VolumeCondition,
                            pInputOrder.VolumeTotalOriginal);

                    Utils.WriteLine(temp, true);

                    ForceQuit(pInputOrder.InstrumentID);
                    Utils.UnlockInstrument(pInputOrder.InstrumentID, pInputOrder.CombOffsetFlag_0);
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private void TraderAdapter_OnFrontDisconnected(int nReason)
        {
            try
            {
                Utils.WriteLine(string.Format("错误：{0}交易断线,原因{1}", InvestorId, nReason), true);
                Email.SendMail(string.Format("错误：{0}交易断线", InvestorId),
                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    Utils.IsMailingEnabled);
                Utils.IsTraderReady = false;
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        /// <summary>
        /// 报告当前持仓
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _timerReportPosition_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var sb = new StringBuilder();

                foreach (var kv in PositionFields)
                {
                    sb.AppendLine(string.Format("{0}:{1}", kv.Key, PositionInfo(kv.Value)));
                    sb.AppendLine();
                }

                var dtNow = DateTime.Now;

                if (dtNow.Hour >= 3 && dtNow.Hour <= 8) //凌晨休市时段不报告
                {
                    return;
                }

                Email.SendMail("当前持仓:", InvestorId + sb, Utils.IsMailingEnabled);
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private void TraderAdapter_OnRspQryDepthMarketData(ThostFtdcDepthMarketDataField pDepthMarketData,
            ThostFtdcRspInfoField pRspInfo, int nRequestId, bool bIsLast)
        {
            try
            {
                if (pDepthMarketData != null)
                {
                    var category = Utils.GetInstrumentCategory(pDepthMarketData.InstrumentID);

                    if (Utils.InstrumentToInstrumentsDepthMarketData.ContainsKey(category))
                    {
                        Utils.InstrumentToInstrumentsDepthMarketData[category].Add(pDepthMarketData);
                    }
                    else
                    {
                        Utils.InstrumentToInstrumentsDepthMarketData[category] = new List<ThostFtdcDepthMarketDataField>
                        {
                            pDepthMarketData
                        };
                    }


                    var temp = string.Format("查询合约市场信息回报:合约代码:{0},持仓量:{1},昨持仓量:{2},交易日:{3},成交金额:{4},数量:{5}",
                        pDepthMarketData.InstrumentID,
                        pDepthMarketData.OpenInterest, pDepthMarketData.PreOpenInterest, pDepthMarketData.TradingDay,
                        pDepthMarketData.Turnover, pDepthMarketData.Volume);

                    Utils.WriteLine(temp);
                }

                if (bIsLast)
                {

                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }

        }

        public void Connect()
        {
            //SubscribePublicTopic(EnumTeResumeType.THOST_TERT_RESTART); // 注册公有流
            //SubscribePrivateTopic(EnumTeResumeType.THOST_TERT_RESTART); // 注册私有流
            SubscribePublicTopic(EnumTeResumeType.THOST_TERT_QUICK); // 注册公有流
            SubscribePrivateTopic(EnumTeResumeType.THOST_TERT_QUICK); // 注册私有流
            foreach (var server in _front)
            {
                RegisterFront(server);
            }
            Init();
            Join();
        }

        private void TraderAdapter_OnRspOrderAction(ThostFtdcInputOrderActionField pInputOrderAction,
            ThostFtdcRspInfoField pRspInfo, int nRequestId, bool bIsLast)
        {
            try
            {
                Utils.ReportError(pRspInfo, "撤单回报错误");
                if (pInputOrderAction != null)
                {
                    Utils.OutputField(pInputOrderAction);
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        /// <summary>
        /// 交易所发回的报单错误
        /// </summary>
        /// <param name="pInputOrder"></param>
        /// <param name="pRspInfo"></param>
        private void TraderAdapter_OnErrRtnOrderInsert(ThostFtdcInputOrderField pInputOrder,
            ThostFtdcRspInfoField pRspInfo)
        {
            try
            {
                if (pInputOrder != null)
                {
                    var temp = Utils.OutputField(pInputOrder);
                    Email.SendMail("交易所报单错误", temp, Utils.IsMailingEnabled);

                    Utils.UnlockInstrument(pInputOrder.InstrumentID, pInputOrder.CombOffsetFlag_0);

                    ForceQuit(pInputOrder.InstrumentID);
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        /// <summary>
        /// 判断合约是否有未完成的报单
        /// </summary>
        /// <param name="instrumentId"></param>
        /// <param name="direction"></param>
        /// <param name="openOrClose"></param>
        /// <returns></returns>
        public bool IsUnFinishedOrderExisting(string instrumentId, EnumDirectionType direction,
            EnumOffsetFlagType openOrClose)
        {
            if (
                UnFinishedOrderFields.Values.Any(
                    s =>
                        s.InstrumentID.Equals(instrumentId) && s.Direction == direction &&
                        s.CombOffsetFlag_0 == openOrClose)) //该合约还有未完成的同方向报单，不报单
            {
                return true;
            }

            return false;
        }

        private void TraderAdapter_OnRtnOrder(ThostFtdcOrderField pOrder)
        {
            try
            {
                if (pOrder != null)
                {
                    var temp =
                        string.Format(
                            "报单回报:合约:{0},买卖:{1},交易所:{2},开平:{3},价格:{4},报单状态:{5},报单提交状态:{6},报单数量:{7},成交数量:{8},剩余数量:{9},报单引用:{10},插入时间:{11},插入日期:{12},系统号:{13}",
                            pOrder.InstrumentID, pOrder.Direction, pOrder.ExchangeID, pOrder.CombOffsetFlag_0,
                            pOrder.LimitPrice, pOrder.OrderStatus, pOrder.OrderSubmitStatus, pOrder.VolumeTotalOriginal,
                            pOrder.VolumeTraded, pOrder.VolumeTotal, pOrder.OrderRef, pOrder.InsertTime,
                            pOrder.InsertDate,
                            pOrder.OrderSysID);

                    Utils.WriteLine(temp, true);

                    UnFinishedOrderFields[GetOrderKey(pOrder)] = pOrder;

                    if (pOrder.OrderStatus == EnumOrderStatusType.Canceled) //报单被撤单
                    {
                        Utils.WriteLine(string.Format("OrderRef为{0}的报单被撤单，从未完成报单列表中移除", pOrder.OrderRef), true);
                        ThostFtdcOrderField pOrderTemp;
                        UnFinishedOrderFields.TryRemove(GetOrderKey(pOrder), out pOrderTemp);
                        Utils.UnlockInstrument(pOrder.InstrumentID, pOrderTemp.CombOffsetFlag_0);
                    }

                    if (pOrder.OrderStatus == EnumOrderStatusType.AllTraded) //报单到达终结状态后移除
                    {
                        if (UnFinishedOrderFields.ContainsKey(GetOrderKey(pOrder)))
                        {
                            var order = UnFinishedOrderFields[GetOrderKey(pOrder)];
                            Utils.WriteLine(string.Format("OrderRef为{0}的报单完全成交，从未完成报单列表中移除", pOrder.OrderRef), true);
                            ThostFtdcOrderField pOrderTemp;
                            UnFinishedOrderFields.TryRemove(GetOrderKey(order), out pOrderTemp);

                            //Thread.Sleep(2000); //报单回报和成交回报之间有延时,目前发现1.5秒,故此处延迟2秒
                            Utils.UnlockInstrument(order.InstrumentID, order.CombOffsetFlag_0);
                        }
                        else
                        {
                            Utils.WriteLine(string.Format("错误:未找到OrderRef为{0}的成交回报的报单信息!", pOrder.OrderRef), true);
                        }
                    }


                    if (pOrder.InstrumentID.StartsWith("au"))
                    {
                        //立即撤掉代表命令的单
                        var field = new ThostFtdcInputOrderActionField
                        {
                            ActionFlag = EnumActionFlagType.Delete,
                            BrokerID = pOrder.BrokerID,
                            ExchangeID = pOrder.ExchangeID,
                            FrontID = pOrder.FrontID,
                            InstrumentID = pOrder.InstrumentID,
                            InvestorID = pOrder.InvestorID,
                            OrderRef = pOrder.OrderRef,
                            OrderSysID = pOrder.OrderSysID,
                            SessionID = pOrder.SessionID
                        };
                        ReqOrderAction(field, RequestId++);

                        var temp1 = string.Format("收到{0}合约报单回报,FrontID:{1},SessionID:{2},解释为强制退出...",
                            pOrder.InstrumentID,
                            pOrder.FrontID, pOrder.SessionID);
                        Utils.WriteLine(temp1, true);
                        Email.SendMail(temp1, DateTime.Now.ToString(CultureInfo.InvariantCulture),
                            Utils.IsMailingEnabled);
                        Utils.Exit(this);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private void ForceQuit(string instrumentId)
        {
            try
            {
                if (instrumentId.StartsWith("au"))
                {
                    var temp1 = string.Format("收到{0}合约报单回报,解释为强制退出...", instrumentId);
                    Utils.WriteLine(temp1, true);
                    Email.SendMail(temp1, DateTime.Now.ToString(CultureInfo.InvariantCulture), Utils.IsMailingEnabled);
                    Utils.Exit(this);
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private string PositionInfo(ThostFtdcInvestorPositionField pInvestorPosition)
        {
            var temp =
                string.Format(
                    "持仓回报:平仓金额:{0},平仓盈亏:{1},逐日盯市平仓盈亏:{2},逐笔对冲平仓盈亏:{3},平仓量:{4},手续费:{5},交易所保证金:{6},冻结的资金:{7},冻结的手续费:{8},冻结的保证金:{9},合约代码:{10},多头冻结:{11},开仓冻结金额:{12},保证金率:{13},开仓金额:{14},开仓成本:{15},开仓量:{16},持仓多空方向:{17},今日持仓:{18},持仓成本:{19},持仓日期:{20},持仓盈亏:{21},上次占用的保证金:{22},上次结算价:{23},结算编号:{24},本次结算价:{25},空头冻结:{26},开仓冻结金额:{27},今日持仓:{28},交易日:{29},占用的保证金:{30},上日持仓:{31}",
                    pInvestorPosition.CloseAmount,
                    pInvestorPosition.CloseProfit, pInvestorPosition.CloseProfitByDate,
                    pInvestorPosition.CloseProfitByTrade, pInvestorPosition.CloseVolume,
                    pInvestorPosition.Commission, pInvestorPosition.ExchangeMargin,
                    pInvestorPosition.FrozenCash, pInvestorPosition.FrozenCommission, pInvestorPosition.FrozenMargin,
                    pInvestorPosition.InstrumentID,
                    pInvestorPosition.LongFrozen, pInvestorPosition.LongFrozenAmount,
                    pInvestorPosition.MarginRateByMoney,
                    pInvestorPosition.OpenAmount, pInvestorPosition.OpenCost, pInvestorPosition.OpenVolume,
                    pInvestorPosition.PosiDirection, pInvestorPosition.Position, pInvestorPosition.PositionCost,
                    pInvestorPosition.PositionDate, pInvestorPosition.PositionProfit, pInvestorPosition.PreMargin,
                    pInvestorPosition.PreSettlementPrice, pInvestorPosition.SettlementID,
                    pInvestorPosition.SettlementPrice, pInvestorPosition.ShortFrozen,
                    pInvestorPosition.ShortFrozenAmount, pInvestorPosition.TodayPosition,
                    pInvestorPosition.TradingDay, pInvestorPosition.UseMargin, pInvestorPosition.YdPosition);

            return temp;
        }

        //查询返回时，昨仓和今仓会分开返回，所以要分别判断时间，取YDPosition或TodayPosition，Position是总持仓
        private void TraderAdapter_OnRspQryInvestorPosition(ThostFtdcInvestorPositionField pInvestorPosition,
            ThostFtdcRspInfoField pRspInfo, int nRequestId, bool bIsLast)
        {
            try
            {
                if (pInvestorPosition != null)
                {
                    Utils.WriteLine(PositionInfo(pInvestorPosition), true);

                    if (pInvestorPosition.PosiDirection != EnumPosiDirectionType.Net && pInvestorPosition.Position > 0)
                    {
                        var keyToday = Utils.GetPositionKey(pInvestorPosition.InstrumentID,
                            pInvestorPosition.PosiDirection,
                            EnumPositionDateType.Today);

                        var keyHistory = Utils.GetPositionKey(pInvestorPosition.InstrumentID,
                            pInvestorPosition.PosiDirection,
                            EnumPositionDateType.History);

                        if (Utils.IsShfeInstrument(pInvestorPosition.InstrumentID)) //上期所合约今仓昨仓分开
                        {
                            //今仓
                            if (pInvestorPosition.PositionDate == EnumPositionDateType.Today)
                            {
                                PositionFields[keyToday] = new ThostFtdcInvestorPositionField
                                {
                                    InstrumentID = pInvestorPosition.InstrumentID,
                                    PosiDirection = pInvestorPosition.PosiDirection,
                                    Position = pInvestorPosition.Position,
                                    TodayPosition = pInvestorPosition.TodayPosition,
                                    YdPosition = 0,
                                    PositionDate = EnumPositionDateType.Today
                                };
                            }

                            //昨仓
                            if (pInvestorPosition.PositionDate == EnumPositionDateType.History)
                            {
                                PositionFields[keyHistory] = new ThostFtdcInvestorPositionField
                                {
                                    InstrumentID = pInvestorPosition.InstrumentID,
                                    PosiDirection = pInvestorPosition.PosiDirection,
                                    Position = pInvestorPosition.Position,
                                    TodayPosition = 0,
                                    YdPosition = pInvestorPosition.Position - pInvestorPosition.TodayPosition,
                                    PositionDate = EnumPositionDateType.History
                                };
                            }
                        }
                        else //其它交易所今仓昨仓在一条结果里
                        {
                            if (pInvestorPosition.TodayPosition > 0)
                            {
                                PositionFields[keyToday] = new ThostFtdcInvestorPositionField
                                {
                                    InstrumentID = pInvestorPosition.InstrumentID,
                                    PosiDirection = pInvestorPosition.PosiDirection,
                                    Position = pInvestorPosition.Position,
                                    TodayPosition = pInvestorPosition.TodayPosition,
                                    YdPosition = 0,
                                    PositionDate = EnumPositionDateType.Today
                                };
                            }

                            if (pInvestorPosition.Position - pInvestorPosition.TodayPosition > 0)
                            {
                                PositionFields[keyHistory] = new ThostFtdcInvestorPositionField
                                {
                                    InstrumentID = pInvestorPosition.InstrumentID,
                                    PosiDirection = pInvestorPosition.PosiDirection,
                                    Position = pInvestorPosition.Position,
                                    TodayPosition = pInvestorPosition.TodayPosition,
                                    YdPosition = pInvestorPosition.Position - pInvestorPosition.TodayPosition,
                                    PositionDate = EnumPositionDateType.History
                                };
                            }
                        }

                    }
                }

                if (bIsLast)
                {
                    Thread.Sleep(1000);
                    ReqQryOrder();
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private void TraderAdapter_OnRspQryInvestorPositionDetail(
            ThostFtdcInvestorPositionDetailField pInvestorPositionDetail, ThostFtdcRspInfoField pRspInfo,
            int nRequestId, bool bIsLast)
        {
            try
            {
                if (pInvestorPositionDetail != null)
                {
                    var temp =
                        string.Format(
                            "持仓明细回报:平仓金额:{0},逐日盯市平仓盈亏:{1},逐笔对冲平仓盈亏:{2},平仓量:{3},买卖:{4},交易所代码:{5},交易所保证金:{6},合约代码:{7},昨结算价:{8},投资者保证金:{9},保证金率:{10},开仓日期:{11},开仓价:{12},逐日盯市持仓盈亏:{13},逐笔对冲持仓盈亏:{14},结算编号:{15},结算价:{16},成交编号:{17},成交类型:{18},交易日:{19},数量:{20}",
                            pInvestorPositionDetail.CloseAmount,
                            pInvestorPositionDetail.CloseProfitByDate, pInvestorPositionDetail.CloseProfitByTrade,
                            pInvestorPositionDetail.CloseVolume,
                            pInvestorPositionDetail.Direction, pInvestorPositionDetail.ExchangeID,
                            pInvestorPositionDetail.ExchMargin,
                            pInvestorPositionDetail.InstrumentID,
                            pInvestorPositionDetail.LastSettlementPrice, pInvestorPositionDetail.Margin,
                            pInvestorPositionDetail.MarginRateByMoney,
                            pInvestorPositionDetail.OpenDate, pInvestorPositionDetail.OpenPrice,
                            pInvestorPositionDetail.PositionProfitByDate, pInvestorPositionDetail.PositionProfitByTrade,
                            pInvestorPositionDetail.SettlementID, pInvestorPositionDetail.SettlementPrice,
                            pInvestorPositionDetail.TradeID, pInvestorPositionDetail.TradeType,
                            pInvestorPositionDetail.TradingDay, pInvestorPositionDetail.Volume);


                    Utils.WriteLine(temp, true);
                }

                if (bIsLast)
                {
                    Thread.Sleep(1000);
                    ReqQryInvestorPosition();
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private void ReqQryInvestorPosition()
        {
            var req = new ThostFtdcQryInvestorPositionField { BrokerID = BrokerId, InvestorID = InvestorId };
            int iResult = ReqQryInvestorPosition(req, RequestId++);
        }

        private void ReqQryOrder()
        {
            var req = new ThostFtdcQryOrderField { BrokerID = BrokerId, InvestorID = InvestorId };
            int iResult = ReqQryOrder(req, RequestId++);
        }

        private void TraderAdapter_OnRspQryTradingAccount(ThostFtdcTradingAccountField pTradingAccount,
            ThostFtdcRspInfoField pRspInfo, int nRequestId, bool bIsLast)
        {
            try
            {
                if (pTradingAccount != null)
                {
                    //查询到的资金信息保存到文件中
                    var temp =
                        string.Format(
                            "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29}",
                            pTradingAccount.TradingDay, pTradingAccount.AccountID, pTradingAccount.Available,
                            pTradingAccount.Balance,
                            pTradingAccount.BrokerID, pTradingAccount.CashIn, pTradingAccount.CloseProfit,
                            pTradingAccount.Commission, pTradingAccount.Credit, pTradingAccount.CurrMargin,
                            pTradingAccount.DeliveryMargin, pTradingAccount.Deposit,
                            pTradingAccount.ExchangeDeliveryMargin, pTradingAccount.ExchangeMargin,
                            pTradingAccount.FrozenCash, pTradingAccount.FrozenCommission, pTradingAccount.FrozenMargin,
                            pTradingAccount.Interest, pTradingAccount.InterestBase, pTradingAccount.Mortgage,
                            pTradingAccount.PositionProfit, pTradingAccount.PreBalance, pTradingAccount.PreCredit,
                            pTradingAccount.PreDeposit, pTradingAccount.PreMargin, pTradingAccount.PreMortgage,
                            pTradingAccount.Reserve, pTradingAccount.SettlementID, pTradingAccount.Withdraw,
                            pTradingAccount.WithdrawQuota);

                    var moneyFile = Utils.AssemblyPath + "money.csv";
                    try
                    {
                        var dicMoney = new Dictionary<string, string>();

                        if (File.Exists(moneyFile))
                        {
                            var sr = new StreamReader(moneyFile, Encoding.UTF8);
                            string line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                dicMoney[line] = line;
                            }
                            sr.Close();

                            dicMoney[temp] = temp;

                            File.Delete(moneyFile);

                            var sw = new StreamWriter(moneyFile, true, Encoding.UTF8);
                            foreach (var s in dicMoney)
                            {
                                sw.WriteLine(s.Value);
                            }

                            sw.Close();
                        }
                        else
                        {
                            const string title =
                                "交易日,投资者帐号,可用资金,期货结算准备金,经纪公司代码,资金差额,平仓盈亏,手续费,信用额度,当前保证金总额,投资者交割保证金,入金金额,交易所交割保证金,交易所保证金,冻结的资金,冻结的手续费,冻结的保证金,利息收入,利息基数,质押金额,持仓盈亏,上次结算准备金,上次信用额度,上次存款额,上次占用的保证金,上次质押金额,基本准备金,结算编号,出金金额,可取资金";

                            var sw = new StreamWriter(moneyFile, true, Encoding.UTF8);
                            sw.WriteLine(title);
                            sw.WriteLine(temp);
                            sw.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = ex.Message + ex.Source + ex.StackTrace;
                        Utils.WriteLine(errorMsg, true);
                        Email.SendMail(string.Format("错误:处理{0}出现异常", moneyFile), errorMsg, Utils.IsMailingEnabled);
                    }

                    Utils.OutputField(pTradingAccount);
                }

                if (bIsLast)
                {
                    Thread.Sleep(1000);
                    ReqQryInvestorPositionDetail();
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private void ReqQryInvestorPositionDetail()
        {
            var req = new ThostFtdcQryInvestorPositionDetailField { BrokerID = BrokerId, InvestorID = InvestorId };
            var iResult = ReqQryInvestorPositionDetail(req, RequestId++);
        }

        private void TraderAdapter_OnRspSettlementInfoConfirm(
            ThostFtdcSettlementInfoConfirmField pSettlementInfoConfirm,
            ThostFtdcRspInfoField pRspInfo, int nRequestId, bool bIsLast)
        {
            try
            {
                Utils.ReportError(pRspInfo, "结算信息确认错误");

                if (pSettlementInfoConfirm != null)
                {
                    var temp = string.Format("结算信息确认回报:经纪公司代码:{0},确认日期:{1},确认时间:{2},投资者代码:{3}",
                        pSettlementInfoConfirm.BrokerID, pSettlementInfoConfirm.ConfirmDate,
                        pSettlementInfoConfirm.ConfirmTime, pSettlementInfoConfirm.InvestorID);
                    Utils.WriteLine(temp, true);
                }

                if (bIsLast)
                {
                    Thread.Sleep(1000);
                    ReqQryAllInstruments(); //查询所有合约
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private void TraderAdapter_OnRspQryInstrument(ThostFtdcInstrumentField pInstrument,
            ThostFtdcRspInfoField pRspInfo, int nRequestId, bool bIsLast)
        {
            try
            {
                Utils.ReportError(pRspInfo, "查询合约错误");

                if (pInstrument != null) //排除套利合约
                {
                    if (!pInstrument.InstrumentID.Contains("&") && !pInstrument.InstrumentID.Contains("efp") && !pInstrument.InstrumentID.Contains("eof") &&
                        Utils.AllowedCategories.Contains(Utils.GetInstrumentCategory(pInstrument.InstrumentID)))
                    {
                        Utils.InstrumentToInstrumentInfo[pInstrument.InstrumentID] = pInstrument;

                        Utils.CategoryToExchangeId[Utils.GetInstrumentCategory(pInstrument.InstrumentID)] =
                            pInstrument.ExchangeID;
                    }

                    var temp =
                        string.Format(
                            "查询合约回报: 创建日:{0},交割月:{1},交割年份:{2},结束交割日:{3},交易所代码:{4},合约在交易所的代码:{5},到期日:{6},合约生命周期状态:{7},合约代码:{8},合约名称:{9},当前是否交易:{10},多头保证金率:{11},限价单最大下单量:{12},市价单最大下单量:{13},限价单最小下单量:{14},市价单最小下单量:{15},上市日:{16},持仓日期类型:{17},持仓类型:{18},最小变动价位:{19},产品类型:{20},产品代码:{21},空头保证金率:{22},开始交割日:{23},合约数量乘数:{24}",
                            pInstrument.CreateDate, pInstrument.DeliveryMonth, pInstrument.DeliveryYear,
                            pInstrument.EndDelivDate, pInstrument.ExchangeID, pInstrument.ExchangeInstID,
                            pInstrument.ExpireDate, pInstrument.InstLifePhase, pInstrument.InstrumentID,
                            pInstrument.InstrumentName, pInstrument.IsTrading, pInstrument.LongMarginRatio,
                            pInstrument.MaxLimitOrderVolume, pInstrument.MaxMarketOrderVolume,
                            pInstrument.MinLimitOrderVolume, pInstrument.MinMarketOrderVolume, pInstrument.OpenDate,
                            pInstrument.PositionDateType, pInstrument.PositionType, pInstrument.PriceTick,
                            pInstrument.ProductClass, pInstrument.ProductID, pInstrument.ShortMarginRatio,
                            pInstrument.StartDelivDate, pInstrument.VolumeMultiple);

                    Utils.WriteLine(temp, true);
                }

                if (bIsLast)
                {
                    Thread.Sleep(1000);
                    ReqQryTradingAccount();
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private void ReqQryTradingAccount()
        {
            var req = new ThostFtdcQryTradingAccountField
            {
                BrokerID = BrokerId,
                InvestorID = InvestorId
            };

            var iResult = ReqQryTradingAccount(req, RequestId++);
        }

        private void ReqQryAllInstruments()
        {
            var req = new ThostFtdcQryInstrumentField();
            var iResult = ReqQryInstrument(req, RequestId++);
        }

        public void Login()
        {
            var loginField = new ThostFtdcReqUserLoginField
            {
                BrokerID = _brokerId,
                UserID = _investorId,
                Password = _password,
                UserProductInfo = "MyClient"
            };


            ReqUserLogin(loginField, RequestId++);
        }
        private void TraderAdapter_OnFrontConnected()
        {
            Login();
        }

        public void CreateNewTrader()
        {
            Task.Run(() =>
            {
                Thread.Sleep(5000);
                var newTrader = new TraderAdapter
                {
                    BrokerId = BrokerId,
                    InvestorId = InvestorId,
                    Password = Password,
                    Front = Front
                };

                ((QuoteAdapter)Utils.QuoteMain).Trader = newTrader;

                newTrader.Connect();
            });
        }
        private void TraderAdapter_OnRspUserLogin(ThostFtdcRspUserLoginField pRspUserLogin,
            ThostFtdcRspInfoField pRspInfo, int nRequestId, bool bIsLast)
        {
            try
            {
                Utils.ReportError(pRspInfo, "登录回报错误");

                if (bIsLast && Utils.IsCorrectRspInfo(pRspInfo))
                {
                    var temp =
                        string.Format(
                            "登录回报:经纪公司代码:{0},郑商所时间:{1},大商所时间:{2},中金所时间:{3},前置编号:{4},登录成功时间:{5},最大报单引用:{6},会话编号:{7},上期所时间:{8},交易系统名称:{9},交易日:{10},用户代码:{11}",
                            pRspUserLogin.BrokerID, pRspUserLogin.CZCETime, pRspUserLogin.DCETime,
                            pRspUserLogin.FFEXTime,
                            pRspUserLogin.FrontID, pRspUserLogin.LoginTime, pRspUserLogin.MaxOrderRef,
                            pRspUserLogin.SessionID, pRspUserLogin.SHFETime, pRspUserLogin.SystemName,
                            pRspUserLogin.TradingDay, pRspUserLogin.UserID);


                    Utils.WriteLine(temp, true);

                    _frontId = pRspUserLogin.FrontID;
                    _sessionId = pRspUserLogin.SessionID;
                    _tradingDay = pRspUserLogin.TradingDay;

                    if (string.IsNullOrEmpty(pRspUserLogin.MaxOrderRef))
                    {
                        CurrentOrderRef = 0;
                    }
                    else
                    {
                        CurrentOrderRef = Convert.ToInt32(pRspUserLogin.MaxOrderRef);
                    }

                    //比较交易所时间和本地时间的偏移值
                    DateTime shfeTime;
                    try
                    {
                        shfeTime = Convert.ToDateTime(pRspUserLogin.SHFETime);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            shfeTime = Convert.ToDateTime(pRspUserLogin.CZCETime);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                shfeTime = Convert.ToDateTime(pRspUserLogin.DCETime);
                            }
                            catch (Exception)
                            {
                                var dtNow = DateTime.Now;
                                Utils.WriteLine("交易所时间格式不正确，重新连接...", true);
                                Email.SendMail("交易所时间格式不正确，重新连接...", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                                shfeTime = dtNow;
                                Utils.Exit();
                            }

                        }
                    }

                    Utils.ExchangeTimeOffset = ExchangeTime.Instance.GetSecFromDateTime(shfeTime) -
                                               ExchangeTime.Instance.GetSecFromDateTime(DateTime.Now);
                    Utils.WriteLine(string.Format("交易所时间与本地时间的偏移值为{0}秒", Utils.ExchangeTimeOffset), true);

                    Thread.Sleep(1000);
                    ReqSettlementInfoConfirm();
                }
                else
                {
                    Utils.WriteLine("登录失败，重新连接...", true);
                    Email.SendMail("登录失败，重新连接...", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                    Utils.Exit();
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }

        }

        public void ReqSettlementInfoConfirm()
        {
            var req = new ThostFtdcSettlementInfoConfirmField
            {
                BrokerID = _brokerId,
                InvestorID = _investorId
            };

            var iResult = ReqSettlementInfoConfirm(req, RequestId++);
        }

        //public int OrderInsertLimitPrice(string instrumentId, EnumDirectionType direction, int nVolume,
        //    EnumOffsetFlagType openOrClose, string reason,
        //    EnumTimeConditionType timeCondition = EnumTimeConditionType.GFD,
        //    EnumVolumeConditionType volumeCondition = EnumVolumeConditionType.AV)
        //{
        //    try
        //    {
        //        if (Utils.InstrumentToQuotes.ContainsKey(instrumentId))
        //        {
        //            double price;
        //            var quotes = Utils.InstrumentToQuotes[instrumentId];

        //            if (quotes.Count > 0)
        //            {
        //                var quote = quotes[quotes.Count - 1];
        //                switch (direction)
        //                {
        //                    case EnumDirectionType.Buy: //买
        //                    {
        //                        price = quote.UpperLimitPrice;
        //                        break;
        //                    }
        //                    case EnumDirectionType.Sell: //卖
        //                    {
        //                        price = quote.LowerLimitPrice;
        //                        break;
        //                    }
        //                    default:
        //                    {
        //                        price = 0;
        //                        break;
        //                    }
        //                }

        //                ReqOrderInsert(instrumentId, direction, price, nVolume, openOrClose, timeCondition,
        //                    volumeCondition,
        //                    reason);
        //            }
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        Utils.WriteException(ex);
        //    }
        //    return -1;
        //}

        /// <summary>
        /// give a offset price
        /// </summary>
        /// <param name="instrumentId"></param>
        /// <param name="direction"></param>
        /// <param name="nVolume"></param>
        /// <param name="openOrClose"></param>
        /// <param name="reason"></param>
        /// <param name="timeCondition"></param>
        /// <param name="volumeCondition"></param>
        /// <returns></returns>
        public int OrderInsertOffsetPrice(string instrumentId, EnumDirectionType direction, int nVolume,
            EnumOffsetFlagType openOrClose, double offset, string reason,
            EnumTimeConditionType timeCondition = EnumTimeConditionType.GFD,
            EnumVolumeConditionType volumeCondition = EnumVolumeConditionType.AV, double priceIn = 0)
        {
            try
            {
                var price = 0.0;

                switch (direction)
                {
                    case EnumDirectionType.Buy: //买
                        {
                            price = priceIn - offset;
                            break;
                        }
                    case EnumDirectionType.Sell: //卖
                        {
                            price = priceIn + offset;
                            break;
                        }
                    default:
                        {
                            price = 0;
                            break;
                        }
                }

                if (Utils.InstrumentToQuotes.ContainsKey(instrumentId))
                {
                    var quotes = Utils.InstrumentToQuotes[instrumentId];

                    if (quotes.Count > 0)
                    {
                        var quote = quotes[0];


                        if (price > quote.UpperLimitPrice)
                        {
                            price = quote.UpperLimitPrice;
                        }

                        if (price < quote.LowerLimitPrice)
                        {
                            price = quote.LowerLimitPrice;
                        }

                        ReqOrderInsert(instrumentId, direction, price, nVolume, openOrClose, timeCondition,
                            volumeCondition,
                            reason);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
            return -1;
        }

        public void CloseAllPositions()
        {
            try
            {
                Utils.IsOpenLocked = true;
                var positionsToClose = new List<ThostFtdcInvestorPositionField>();

                foreach (var kv in PositionFields)
                {
                    positionsToClose.Add(kv.Value);
                }

                //首先需要获取要平掉的非主力合约的行情
                if (positionsToClose.Count > 0)
                {
                    foreach (var position in positionsToClose)
                    {
                        if (position.PosiDirection == EnumPosiDirectionType.Long)
                        {
                            CloseLongPositionByInstrument(position.InstrumentID, "收盘平多仓", true, 0);
                            Thread.Sleep(2000); //防止有相同的合约平仓互相影响，保证前一个已经完成，再报第二笔
                        }

                        if (position.PosiDirection == EnumPosiDirectionType.Short)
                        {
                            CloseShortPositionByInstrument(position.InstrumentID, "收盘平空仓", true, 99999);
                            Thread.Sleep(2000);
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        public int ReqOrderInsert(string instrumentId, EnumDirectionType direction, double price, int nVolume,
            EnumOffsetFlagType openOrClose, EnumTimeConditionType timeCondition,
            EnumVolumeConditionType volumeCondition, string reason)
        {
            try
            {
                //合约还有报单在途中，不报单
                if (Utils.IsInstrumentLocked(instrumentId))
                {
                    Utils.WriteLine(string.Format("合约{0}还有在途单，不能报单", instrumentId), true);
                    return -1;
                }

                var req = new ThostFtdcInputOrderField
                {
                    BrokerID = _brokerId,
                    InvestorID = _investorId,
                    InstrumentID = instrumentId,
                    OrderRef = (++CurrentOrderRef).ToString(),
                    OrderPriceType = EnumOrderPriceTypeType.LimitPrice,
                    Direction = direction,
                    CombOffsetFlag_0 = openOrClose,
                    CombHedgeFlag_0 = EnumHedgeFlagType.Speculation,
                    LimitPrice = price,
                    VolumeTotalOriginal = nVolume,
                    TimeCondition = timeCondition,
                    VolumeCondition = volumeCondition,
                    MinVolume = 1,
                    ContingentCondition = EnumContingentConditionType.Immediately,
                    ForceCloseReason = EnumForceCloseReasonType.NotForceClose,
                    IsAutoSuspend = 0,
                    UserForceClose = 0
                };

                var temp =
                    string.Format(
                        "交易账号:{0},合约代码:{1},报单引用:{2},报单价格条件:{3},买卖方向:{4},开平:{5},价格:{6},数量:{7},有效期类型:{8},成交量类型:{9}",
                        InvestorId, req.InstrumentID, req.OrderRef, req.OrderPriceType, req.Direction,
                        req.CombOffsetFlag_0,
                        req.LimitPrice, req.VolumeTotalOriginal, req.TimeCondition, req.VolumeCondition);

                var iResult = -1;
                bool isOrderInserted = false;

                if (req.CombOffsetFlag_0 == EnumOffsetFlagType.Open &&
                    Utils.InstrumentToOpenCount.ContainsKey(req.InstrumentID)) //开仓的时候要判断开仓次数是否超过上限
                {
                    if (Utils.LockedOpenInstruments.ContainsKey(req.InstrumentID))
                    {
                        Utils.WriteLine(string.Format("合约{0}还有在途开仓单，不能开仓", req.InstrumentID), true);
                    }
                    else
                    {
                        var count = Utils.InstrumentToOpenCount[req.InstrumentID];

                        if (count < 100)
                        {
                            iResult = ReqOrderInsert(req, RequestId++);
                            Utils.LockOpenInstrument(req.InstrumentID);
                            Utils.InstrumentToOpenCount[req.InstrumentID]++;
                            isOrderInserted = true;
                        }
                        else
                        {
                            Utils.WriteLine(string.Format("合约{0}的开仓次数{1}超过了上限，不能开仓", req.InstrumentID, count), true);
                        }
                    }

                }
                else
                {
                    iResult = ReqOrderInsert(req, RequestId++);
                    Utils.LockInstrument(req.InstrumentID);
                    isOrderInserted = true;
                }

                if (isOrderInserted)
                {
                    Utils.WriteLine(string.Format("新委托：{0},原因:{1}", temp, reason), true);
                    Email.SendMail(string.Format("新委托:{0},原因:{1}", temp, reason), reason + temp, Utils.IsMailingEnabled);
                }


                return iResult == 0 ? Convert.ToInt32(req.OrderRef) : -1;
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }

            return -1;
        }
    }
}
