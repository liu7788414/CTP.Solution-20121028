#include <iostream>
#include <fstream>
#include <string>
#include <windows.h>
#include <time.h>
using namespace std;

#include "TraderSpi.h"
#include "MdSpi.h"
#pragma warning(disable : 4996)

// USER_API参数
extern CThostFtdcMdApi* pMdApi;

// 配置参数
extern char FRONT_ADDR[];		
extern TThostFtdcBrokerIDType	BROKER_ID;
extern TThostFtdcInvestorIDType INVESTOR_ID;
extern TThostFtdcPasswordType	PASSWORD;
extern char* ppInstrumentID[];	
extern int iInstrumentID;
extern TThostFtdcInstrumentIDType INSTRUMENT_ID;

// 请求编号
extern int iRequestID;

// User行情数据
extern	TThostFtdcDateExprType	TradingDay;

extern	bool	ReceiveTick;

bool	CloseAll= false;										//收盘标志

char	*InstrumentID_name;	//
double	Q_Dayopen=0;		//
double	Q_UpperLimit = 0;	//
double	Q_LowerLimit = 0;	//
string	Q_BarTime_s;		//
int		Q_BarTime_1;		//
double	Q_BarTime_2;		//
int		FirstVolume;		//前一次成交量数据
int		BNum = 0;			//开仓次数
int		SNum = 0;			//开仓次数

bool	MnKlinesig=false;		//
double  Mn_open[3]	= {0,0,0};			// 
double  Mn_high[3]	= {0,0,0};			// 
double  Mn_low[3]	= {0,0,0};			// 
double  Mn_close[3] = {0,0,0};			// 
double  NewPrice = 0;		//

double  Mn_bbreak = 0;		// 
double  Mn_sbreak = 0;		// 
double  Mn_UpperBand = 100000;	// 
double  Mn_LowerBand = 0;	// 

double  M4 = 1.0;			// 
double  M5 = 1.0;			// 

double  BuyPrice = 0;		//开仓价
double  SellPrice = 0;		//开仓价

double  BuyPrice1 = 0;		//买一价
double  SellPrice1 = 0;		//卖一价
double  BuyVol1 = 0;		//买一量
double  SellVol1 = 0;		//卖一量
double  BuyVol10 = 0;		//上一次买一量
double  SellVol10 = 0;		//上一次卖一量

double	TodayVolume = 0;	//成交量
double	OpenInt = 0;		//持仓量

double  BuyStopline = 0;	//开仓止损价
double  SellStopline = 0;	//开仓止损价 
double	BSVolume = 1;		//开仓量

bool	BuySignal = false;	//
bool	SellSignal = false;	//

int		TickABS = 0;
double  TickAPrice[4];		//
int		TickBNum = 0;
double  TickBPrice[4];		//
char    TickFileWritepaths[80]="";	//
char    LogFilePaths[80]="";	//

void CMdSpi::OnRspError(CThostFtdcRspInfoField *pRspInfo,
		int nRequestID, bool bIsLast)
{
	cerr << "--->>> "<< __FUNCTION__ << endl;
	IsErrorRspInfo(pRspInfo);
}

void CMdSpi::OnFrontDisconnected(int nReason)
{
	cerr << "--->>> " << __FUNCTION__ << endl;
	cerr << "--->>> Reason = " << nReason << endl;
}
		
void CMdSpi::OnHeartBeatWarning(int nTimeLapse)
{
	cerr << "--->>> " << __FUNCTION__ << endl;
	cerr << "--->>> nTimerLapse = " << nTimeLapse << endl;
}

void CMdSpi::OnFrontConnected()
{
	cerr << "--->>> " << __FUNCTION__ << endl;
	//cerr << "--->>> " << "CMdSpi::OnFrontConnected" << endl;
	///用户登录请求
	ReqUserLogin();

}


void CMdSpi::ReqUserLogin()
{
	CThostFtdcReqUserLoginField req;
	memset(&req, 0, sizeof(req));
	strcpy(req.BrokerID, BROKER_ID);
	strcpy(req.UserID, INVESTOR_ID);
	strcpy(req.Password, PASSWORD);
	int iResult = pMdApi->ReqUserLogin(&req, ++iRequestID);
	cerr << "--->>> 发送行情用户登录请求: " << ((iResult == 0) ? "成功" : "失败") << endl;
}

void CMdSpi::OnRspUserLogin(CThostFtdcRspUserLoginField *pRspUserLogin,
		CThostFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	cerr << "--->>> " << __FUNCTION__ << endl;

	if (bIsLast && !IsErrorRspInfo(pRspInfo))
	{
		///获取当前交易日
		cerr << "--->>> 获取当前交易日 = " << pMdApi->GetTradingDay() << endl;
		// 请求订阅行情
		SubscribeMarketData();	
	}
}

void CMdSpi::SubscribeMarketData()
{
	int iResult0 = pMdApi->UnSubscribeMarketData(ppInstrumentID, iInstrumentID);
	cerr << "--->>> 取消行情订阅请求: " << ((iResult0 == 0) ? "成功" : "失败") << endl;
	Sleep(1000);
	int iResult = pMdApi->SubscribeMarketData(ppInstrumentID, iInstrumentID);
	cerr << "--->>> 发送行情订阅请求: " << ((iResult == 0) ? "成功" : "失败") << endl;

	//文件命名，“OrderInfo_date.txt”
	char perf[100];
	char tmp[20];
	int tmp1 = 20140424;
	strcpy(perf,"./AutoTrade/");
	strcpy(perf,"TraderInfo_");
	sprintf(tmp,"%d",tmp1);
	strcat(perf,tmp);
	strcat(perf,".txt");
	cerr << "--->>> " << perf << endl;
	
	//检查文件是否存在，是否需要新建文本文件
	ifstream inf;
	ofstream ouf;
	inf.open(perf,ios::out);

}

void CMdSpi::OnRspSubMarketData(CThostFtdcSpecificInstrumentField *pSpecificInstrument, CThostFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	cerr << "--->>> 成功订阅合约:" <<"_"<<pSpecificInstrument->InstrumentID<< endl;

}

void CMdSpi::OnRspUnSubMarketData(CThostFtdcSpecificInstrumentField *pSpecificInstrument, CThostFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	cerr << "--->>> " << __FUNCTION__ << endl;
}

void CMdSpi::OnRtnDepthMarketData(CThostFtdcDepthMarketDataField *pDepthMarketData)
{
	//cerr << "--->>> " << __FUNCTION__ << endl;
	///数据缓存。
	ReceiveTick = true;
	void WriteConfiguration(char *filepaths);	
	//SaveData();

	int  Volume_t=pDepthMarketData->Volume;
	//cerr << "--->>> " <<"NewTick!" << endl;

	//抛弃报单变动但无成交的tick数据,也可保留
	if (1)
	//if (Volume_t>FirstVolume)
	{

	FirstVolume=Volume_t;

	char names[10];
	char times[10];

	strcpy(names, pDepthMarketData->InstrumentID);
	strcpy(times, pDepthMarketData->UpdateTime);
	
	string str0=names;
	string str1=times;
	string str2=times;
	string str3=times;
	
	str1=str1.substr(0,2);		//
	str2=str2.substr(3,2);		//
	str3=str3.substr(6,2);		//
	int hours = atoi(str1.c_str());
	int minutes = atoi(str2.c_str());
	int seconds = atoi(str3.c_str());
	int Millisecs = pDepthMarketData->UpdateMillisec;

	Q_BarTime_s = times;												//时间字符串
	Q_BarTime_1 = hours*60*60+minutes*60+seconds;						//时间采用秒计
	Q_BarTime_2	= (1/10e1)*hours + (1/10e3)*minutes + (1/10e5)*seconds;	//时间格式0.145100 = 14：51：00

	Q_UpperLimit=pDepthMarketData->UpperLimitPrice;
	Q_LowerLimit=pDepthMarketData->LowerLimitPrice;

	InstrumentID_name=names;

	NewPrice	= pDepthMarketData->LastPrice;
	SellPrice1	= pDepthMarketData->AskPrice1;
	SellVol1	= pDepthMarketData->AskVolume1;
	BuyPrice1	= pDepthMarketData->BidPrice1;
	BuyVol1		= pDepthMarketData->BidVolume1;

	TodayVolume	= pDepthMarketData->Volume;
	OpenInt		= pDepthMarketData->OpenInterest;

	TickAPrice[3]=TickAPrice[2];
	TickAPrice[2]=TickAPrice[1];
	TickAPrice[1]=TickAPrice[0];
	TickAPrice[0]=pDepthMarketData->LastPrice;
	
	//TICK数据
	//cerr << "--->>> " <<InstrumentID_name<<"_"<<Q_BarTime_2<<"_"<<SellPrice1<<"_"<<SellVol1<<"_"<<BuyPrice1<<"_"<<BuyVol1<<"_"<<NewPrice<<"_"<<TodayVolume<<"_"<<OpenInt<< endl;

			//***************************************************	
			//生成1分钟K线

			bool Timemore0=Q_BarTime_2!=0.0859 && Q_BarTime_2!=0.0900 && Q_BarTime_2!=0.1015 && Q_BarTime_2!=0.1130 && Q_BarTime_2!=0.1500;
			bool Timemore1=Q_BarTime_2!=0.2059 && Q_BarTime_2!=0.2100 && Q_BarTime_2!=0.0230;
			bool Timemore2=(Q_BarTime_2>0.0900 && Q_BarTime_2<0.1500) ||  (Q_BarTime_2>0.2100 || Q_BarTime_2<0.0230);

			if (Timemore0 && Timemore1  && Timemore2 && seconds>=0 && seconds<40 && MnKlinesig==false)
			{
				MnKlinesig=true;

				Mn_open[1]	=Mn_open[0]; 
				Mn_high[1]	=Mn_high[0];
				Mn_low[1]	=Mn_low[0];
				Mn_close[1]	=Mn_close[0];

				Mn_open[0]	=NewPrice; 
				Mn_high[0]	=NewPrice;
				Mn_low[0]	=NewPrice;
				Mn_close[0]	=NewPrice;
				
				//打印1分钟K线数据
				cerr << "--->>> " <<InstrumentID_name<< "_" <<Q_BarTime_s<< "_" << Mn_open[1]<< "_" << Mn_high[1]<< "_" << Mn_low[1]<< "_" << Mn_close[1] << endl;
			}
			else
			{
				Mn_high[0]	=max(Mn_high[0],NewPrice);
				Mn_low[0]	=min(Mn_low[0],NewPrice);
				Mn_close[0]	=NewPrice;
			}

			if (seconds>45 && seconds<55 && MnKlinesig==true)
			{
				MnKlinesig=false;
			}
			//***************************************************


	if (LogFilePaths[0]== '\0')
	{
		strcpy_s(LogFilePaths,"./AutoTrade/");
		strcat_s(LogFilePaths,pDepthMarketData->TradingDay);
		strcat_s(LogFilePaths,".txt");

		//检查文件是否存在，是否需要新建文本文件
		ifstream inf;
		ofstream ouf;
		inf.open(LogFilePaths,ios::out);
	}

	if (TickFileWritepaths[0]== '\0')
	{
		strcpy_s(TickFileWritepaths,"./TickData/");
		strcat_s(TickFileWritepaths,pDepthMarketData->InstrumentID);
		strcat_s(TickFileWritepaths,"_");
		strcat_s(TickFileWritepaths,pDepthMarketData->TradingDay);	
		strcat_s(TickFileWritepaths,".txt");

		//检查文件是否存在，是否需要新建文本文件
		ifstream inf;
		ofstream ouf;
		inf.open(TickFileWritepaths,ios::out);

	}
	
	//仿真模拟无法获取交易日数据，实盘行情可使用 TradingDay
	ofstream o_file(TickFileWritepaths,ios::app);
	o_file << "20140508" << " " <<Q_BarTime_2<< "\t" << Millisecs <<"\t"<<SellPrice1 <<"\t"<< SellVol1 << "\t" << BuyPrice1 << "\t" << BuyVol1 << "\t" << NewPrice <<"\t"<<TodayVolume<<"\t"<<OpenInt<< endl; //将内容写入到文本文件中
	o_file.close();						//关闭文件

	}
	
	ReceiveTick = false;
}

bool CMdSpi::IsErrorRspInfo(CThostFtdcRspInfoField *pRspInfo)
{
	// 如果ErrorID != 0, 说明收到了错误的响应
	bool bResult = ((pRspInfo) && (pRspInfo->ErrorID != 0));
	if (bResult)
		cerr << "--->>> ErrorID=" << pRspInfo->ErrorID << ", ErrorMsg=" << pRspInfo->ErrorMsg << endl;
	return bResult;
}


