using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CTP;
using log4net;
using SendMail;
using System.Timers;
using System.Windows.Forms;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "WrapperTestSandBox2.exe.config", Watch = true)]

namespace WrapperTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var processes = Process.GetProcessesByName("WrapperTestSandBox2");
                var currrentProcess = Process.GetCurrentProcess();

                foreach (var process in processes)
                {
                    Console.WriteLine(process.MainModule.FileName + " " + process.Id);
                    var fileName = process.MainModule.FileName;
                    var id = process.Id;

                    if (fileName.Equals(currrentProcess.MainModule.FileName) && !id.Equals(currrentProcess.Id))
                    {
                        Console.WriteLine("已经运行");
                        Thread.Sleep(1000);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                return;
            }

            try
            {
                Utils.IsMailingEnabled = false;

                if (args.Length > 1)
                {
                    Utils.IsMailingEnabled = Convert.ToBoolean(args[0]);
                }


                Utils.ReadConfig();

                var timerExit = new System.Timers.Timer(60000);
                timerExit.Elapsed += timerExit_Elapsed;
                timerExit.Start();

                var timerCloseAllPositions = new System.Timers.Timer(10000);
                timerCloseAllPositions.Elapsed += timerCloseAllPosition_Elapsed;
                timerCloseAllPositions.Start();

                string line;

                if (Utils.IsMailingEnabled) //命令行方式
                {
                    line = args[1];
                }
                else //手动方式
                {
                    Console.WriteLine("选择登录类型，1-模拟24*7，2-模拟交易所，3-华泰，4-宏源");
                    line = Console.ReadLine();
                }

                switch (Convert.ToInt32(line))
                {
                    case 1:
                        {
                            //模拟
                            Utils.Trader = new TraderAdapter
                            {
                                BrokerId = "9999",
                                InvestorId = Utils.SimNowAccount,
                                Password = Utils.SimNowPassword,
                                Front = new[] { "tcp://180.168.146.187:10030" } //模拟24*7
                            };

                            Utils.QuoteMain = new QuoteAdapter((TraderAdapter)Utils.Trader)
                            {
                                BrokerId = "9999",
                                InvestorId = Utils.SimNowAccount,
                                Password = Utils.SimNowPassword,
                                Front = new[] { "tcp://180.168.146.187:10031" } //模拟24*7
                            };

                            Utils.CurrentChannel = ChannelType.模拟24X7;

                            break;
                        }
                    case 2:
                        {
                            //模拟
                            Utils.Trader = new TraderAdapter
                            {
                                BrokerId = "9999",
                                InvestorId = Utils.SimNowAccount,
                                Password = Utils.SimNowPassword,
                                Front =
                                    new[]
                                {
                                    "tcp://180.168.146.187:10003"
                                }
                                //模拟交易所时间
                            };

                            //华泰期货的行情
                            Utils.QuoteMain = new QuoteAdapter((TraderAdapter)Utils.Trader)
                            {
                                BrokerId = "9999",
                                InvestorId = "031116",
                                Password = "7788414liu",
                                Front =
                                    new[]
                                {
                                    "tcp://180.168.146.187:10013"
                                } //模拟交易所时间
                            };

                            Utils.CurrentChannel = ChannelType.模拟交易所;
                            break;
                        }
                    case 3:
                        {
                            //华泰
                            Utils.Trader = new TraderAdapter
                            {
                                BrokerId = "8080",
                                InvestorId = "20051875",
                                Password = "414887",
                                Front =
                                    new[]
                                {
                                    "tcp://180.168.212.228:41205", "tcp://180.168.212.229:41205",
                                    "tcp://180.168.212.230:41205",
                                    "tcp://180.168.212.231:41205", "tcp://180.168.212.232:41205",
                                    "tcp://180.168.212.233:41205",
                                    "tcp://180.168.212.234:41205"
                                }
                            };

                            //华泰期货的行情
                            Utils.QuoteMain = new QuoteAdapter((TraderAdapter)Utils.Trader)
                            {
                                BrokerId = "9999",
                                InvestorId = "20051875",
                                Password = "91418",
                                Front =
                                    new[]
                                {
                                    "tcp://180.168.212.228:41213", "tcp://180.168.212.229:41213",
                                    "tcp://180.168.212.230:41213",
                                    "tcp://180.168.212.231:41213", "tcp://180.168.212.232:41213",
                                    "tcp://180.168.212.233:41213", "tcp://180.168.212.234:41213"
                                }
                            };

                            Utils.CurrentChannel = ChannelType.华泰期货;

                            break;
                        }
                    case 4:
                        {
                            //宏源
                            Utils.Trader = new TraderAdapter
                            {
                                BrokerId = "1080",
                                InvestorId = "901200953",
                                Password = "414887",
                                Front =
                                    new[]
                                {
                                    "tcp://180.169.112.52:41205", "tcp://180.169.112.53:41205",
                                    "tcp://180.169.112.54:41205",
                                    "tcp://180.169.112.55:41205",
                                    "tcp://106.37.231.6:41205",
                                    "tcp://106.37.231.7:41205",
                                    "tcp://140.206.101.109:41213",
                                    "tcp://140.206.101.110:41213",
                                    "tcp://140.207.168.9:41213",
                                    "tcp://140.207.168.10:41213",
                                }
                            };

                            ////宏源期货的行情
                            //Utils.QuoteMain = new QuoteAdapter(Utils.Trader)
                            //{
                            //    BrokerId = "1080",
                            //    InvestorId = "901200953",
                            //    Password = "091418",
                            //    Front =
                            //        new[]
                            //        {
                            //            "tcp://180.169.112.52:41213", "tcp://180.169.112.53:41213",
                            //            "tcp://180.169.112.54:41213",
                            //            "tcp://180.169.112.55:41213"
                            //        }
                            //};

                            //华泰期货的行情
                            Utils.QuoteMain = new QuoteAdapter((TraderAdapter)Utils.Trader)
                            {
                                BrokerId = "9999",
                                InvestorId = "20051875",
                                Password = "91418",
                                Front =
                                    new[]
                                {
                                    "tcp://180.168.212.228:41213", "tcp://180.168.212.229:41213",
                                    "tcp://180.168.212.230:41213",
                                    "tcp://180.168.212.231:41213", "tcp://180.168.212.232:41213",
                                    "tcp://180.168.212.233:41213", "tcp://180.168.212.234:41213"
                                }
                            };

                            Utils.CurrentChannel = ChannelType.宏源期货;
                            break;
                        }
                    default:
                        {
                            //模拟
                            Utils.Trader = new TraderAdapter
                            {
                                BrokerId = "9999",
                                InvestorId = Utils.SimNowAccount,
                                Password = Utils.SimNowPassword,
                                Front = new[] { "tcp://180.168.146.187:10030" } //模拟24*7
                            };

                            Utils.QuoteMain = new QuoteAdapter((TraderAdapter)Utils.Trader)
                            {
                                BrokerId = "9999",
                                InvestorId = Utils.SimNowAccount,
                                Password = Utils.SimNowPassword,
                                Front = new[] { "tcp://180.168.146.187:10031" } //模拟24*7
                            };

                            Utils.CurrentChannel = ChannelType.模拟24X7;
                            break;
                        }
                }

                Utils.GetDebugAndInfoLoggers();
                Utils.ReadStopLossPrices();
                Utils.GetQuoteLoggers();
                Utils.WriteLine("我是1");

                Task.Run(() => { ((QuoteAdapter)Utils.QuoteMain).Connect(); });

                while (!((QuoteAdapter)Utils.QuoteMain).IsReady)
                {
                    Utils.WriteLine("等待行情连接");
                    Thread.Sleep(100);
                }

                Utils.WriteLine("行情连接成功！！！");

                Task.Run(() => { ((TraderAdapter)Utils.Trader).Connect(); });

                while (!Utils.IsTraderReady)
                {
                    Utils.WriteLine("等待交易连接");
                    Thread.Sleep(1000);
                }

                Utils.WriteLine("交易连接成功！！！");

                var mainInstrumentsFile = string.Format("{0}主力合约{1}.txt", Utils.AssemblyPath,
                    ((TraderAdapter)Utils.Trader).TradingDay);

                if (File.Exists(mainInstrumentsFile)) //本交易日已经查询过主力合约
                {
                    Utils.WriteLine("读取本交易日主力合约列表");

                    var sr = new StreamReader(mainInstrumentsFile);
                    string instrument = null;
                    while ((instrument = sr.ReadLine()) != null)
                    {
                        var s = instrument.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        Utils.CategoryToMainInstrument[s[0]] = s[1];
                    }
                    sr.Close();
                }
                else //本交易日没有查询过主力合约
                {
                    while (true)
                    {
                        QryInstrumentDepthMarketData((TraderAdapter)Utils.Trader);

                        //主力合约排序
                        Utils.WriteLine("主力合约排序");
                        foreach (var kv in Utils.InstrumentToInstrumentsDepthMarketData)
                        {
                            kv.Value.Sort(new InstrumentComparer());
                        }

                        break;
                    }


                    //保存当前交易日的主力合约
                    Utils.WriteLine("保存当前交易日的主力合约");
                    if (Utils.InstrumentToInstrumentsDepthMarketData.Count <= 0)
                    {
                        var temp = "查询主力合约失败，重新启动";
                        Utils.WriteLine(temp, true);
                        Email.SendMail(temp, DateTime.Now.ToString(CultureInfo.InvariantCulture));
                        Utils.Exit();
                    }

                    var sw = new StreamWriter(mainInstrumentsFile, false, Encoding.UTF8);

                    foreach (var kv in Utils.InstrumentToInstrumentsDepthMarketData)
                    {
                        var ins = kv.Value[kv.Value.Count - 1].InstrumentID;
                        sw.WriteLine("{0}:{1}", Utils.GetInstrumentCategory(ins), ins);
                        Utils.CategoryToMainInstrument[Utils.GetInstrumentCategory(ins)] = ins;
                    }

                    sw.Close();
                }



                Email.SendMail(((TraderAdapter)Utils.Trader).InvestorId + "今日主力合约列表",
                    DateTime.Now.ToString(CultureInfo.InvariantCulture), Utils.IsMailingEnabled,
                    mainInstrumentsFile);

                //订阅全部主力合约行情
                Utils.WriteLine("订阅全部主力合约行情", true);
                ((QuoteAdapter)Utils.QuoteMain).SubscribeMarketData(Utils.CategoryToMainInstrument.Values.ToArray());
                ((QuoteAdapter)Utils.QuoteMain).SubscribedQuotes.AddRange(Utils.CategoryToMainInstrument.Values);

                //初始化开仓手数
                foreach (var kv in Utils.CategoryToMainInstrument)
                {
                    Utils.InstrumentToOpenCount[kv.Value] = 0;
                }

                Utils.IsInitialized = true;

                Thread.Sleep(1000);

                #region 平掉非主力合约仓位

                var positionsToClose = new List<ThostFtdcInvestorPositionField>();

                foreach (var kv in ((TraderAdapter)Utils.Trader).PositionFields)
                {
                    if (!Utils.CategoryToMainInstrument.Values.Contains(kv.Value.InstrumentID))
                    {
                        positionsToClose.Add(kv.Value);
                    }
                }

                //首先需要获取要平掉的非主力合约的行情
                if (positionsToClose.Count > 0)
                {
                    ((QuoteAdapter)Utils.QuoteMain).SubscribeMarketData(
                        positionsToClose.Select(s => s.InstrumentID).ToArray());
                    ((QuoteAdapter)Utils.QuoteMain).SubscribedQuotes.AddRange(
                        positionsToClose.Select(s => s.InstrumentID));

                    Thread.Sleep(1000);

                    foreach (var position in positionsToClose)
                    {
                        if (position.PosiDirection == EnumPosiDirectionType.Long)
                        {
                            ((TraderAdapter)Utils.Trader).CloseLongPositionByInstrument(position.InstrumentID,
                                "平掉非主力多仓", false, 0);
                        }

                        if (position.PosiDirection == EnumPosiDirectionType.Short)
                        {
                            ((TraderAdapter)Utils.Trader).CloseShortPositionByInstrument(position.InstrumentID,
                                "平掉非主力空仓", false, 99999);
                        }
                    }
                }

                #endregion

                //准备完毕后才进入开平仓检查，防止在查询过程中进入
                ((QuoteAdapter)Utils.QuoteMain).StartTimer();

                if (Utils.CurrentChannel == ChannelType.模拟24X7)
                {
                    //_trader.CloseAllPositions();

                    //Thread.Sleep(2000);

                    //Utils.IsOpenLocked = false;

                    //foreach (var kv in Utils.CategoryToMainInstrument)
                    //{
                    //    _trader.OpenLongPositionByInstrument(kv.Value, "测试开多仓",
                    //        Utils.InstrumentToQuotes[kv.Value][0].LastPrice, false);
                    //    Thread.Sleep(2000);
                    //    _trader.OpenShortPositionByInstrument(kv.Value, "测试开空仓",
                    //        Utils.InstrumentToQuotes[kv.Value][0].LastPrice, false);
                    //}

                    //Thread.Sleep(2000);
                    //foreach (var kv in Utils.CategoryToMainInstrument)
                    //{
                    //    _trader.CloseLongPositionByInstrument(kv.Value, "测试平多仓");
                    //    Thread.Sleep(2000);
                    //    _trader.CloseShortPositionByInstrument(kv.Value, "测试平空仓");
                    //}

                    //Thread.Sleep(10000);
                    //foreach (var kv in Utils.CategoryToMainInstrument)
                    //{
                    //    _trader.OpenLongPositionByInstrument(kv.Value, "测试开多仓");
                    //    Thread.Sleep(2000);
                    //    _trader.OpenShortPositionByInstrument(kv.Value, "测试开空仓");
                    //}
                }
                Thread.Sleep(100000000);
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private static void timerCloseAllPosition_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var dateTime = DateTime.Now;
                Utils.WriteLine(string.Format("检查是否全部平仓{0}", dateTime));

                //收盘前10分钟就禁止再开仓
                if ((dateTime.Hour == 14 && dateTime.Minute == 58) ||
                    (dateTime.Hour == 22 && dateTime.Minute == 58))
                {
                    Utils.WriteLine(string.Format("到达禁止开仓时间{0}", dateTime));
                    Utils.IsOpenLocked = true;
                }

                if ((dateTime.Hour == 14 && dateTime.Minute == 59 && dateTime.Second >= 30) ||
                    (dateTime.Hour == 22 && dateTime.Minute == 59 && dateTime.Second >= 30))
                {
                    Utils.WriteLine(string.Format("临近收盘，平掉所有持仓{0}", dateTime), true);
                    ((TraderAdapter)Utils.Trader).CloseAllPositions();
                    ((System.Timers.Timer)sender).Stop();
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private static void timerExit_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                foreach (var key in Utils.InstrumentToMatchPrice.Keys)
                {
                    var dict = Utils.InstrumentToMatchPrice[key];
                    dict = dict.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => v.Value);

                    foreach (var m in dict)
                    {
                        Utils.WriteLine(string.Format("成交价:{0},成交量:{1}", m.Key, m.Value.Volume));
                    }
                }

                var dateTime = DateTime.Now;
                Utils.WriteLine(string.Format("检查是否退出{0}", dateTime));

                if ((dateTime.Hour == 15 && dateTime.Minute == 5) ||
                    (dateTime.Hour == 23 && dateTime.Minute == 35))
                {
                    Utils.WriteLine(string.Format("收盘，程序关闭{0}", dateTime));
                    Email.SendMail("收盘，程序关闭", DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        Utils.IsMailingEnabled);
                    Utils.Exit(Utils.Trader);
                }

                if (dateTime.Hour == 8 &&
                    (dateTime.Minute == 45 || dateTime.Minute == 46 || dateTime.Minute == 47 || dateTime.Minute == 48 ||
                     dateTime.Minute == 49) && !Utils.IsTraderReady)
                //上午开盘时通道没有准备好，每隔一分钟尝试重新连接
                {
                    Utils.WriteLine(string.Format("通道没有准备好，重新连接，{0}", dateTime));
                    Email.SendMail("通道没有准备好，重新连接", DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        Utils.IsMailingEnabled);

                    ((TraderAdapter)Utils.Trader).CreateNewTrader();
                }

                if (dateTime.Hour == 8 && dateTime.Minute == 44) //早盘开盘前，主动重新登录一次
                {
                    var t = new ThostFtdcUserLogoutField
                    {
                        BrokerID = ((TraderAdapter)Utils.Trader).BrokerId,
                        UserID = ((TraderAdapter)Utils.Trader).InvestorId
                    };
                    ((TraderAdapter)Utils.Trader).ReqUserLogout(t, TraderAdapter.RequestId++);
                    Utils.WriteLine(string.Format("登出{0}", ((TraderAdapter)Utils.Trader).InvestorId), true);
                    Email.SendMail(string.Format("登出{0}", ((TraderAdapter)Utils.Trader).InvestorId),
                        DateTime.Now.ToString(CultureInfo.InvariantCulture), Utils.IsMailingEnabled);
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }


        private static void QryInstrumentDepthMarketData(TraderAdapter trader)
        {
            try
            {
                foreach (var kv in Utils.InstrumentToInstrumentInfo)
                {
                    Thread.Sleep(1000);

                    Utils.WriteLine(string.Format("查询{0}...", kv.Key));

                    var ins = new ThostFtdcQryDepthMarketDataField
                    {
                        InstrumentID = kv.Key
                    };

                    trader.ReqQryDepthMarketData(ins, TraderAdapter.RequestId++);
                }

                Utils.WriteLine("查询合约详情完毕！！！");

                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }
    }
}
