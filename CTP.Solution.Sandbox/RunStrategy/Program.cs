using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RunStrategy
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                System.Timers.Timer timer = new System.Timers.Timer(10000);
                timer.Elapsed += timer_Elapsed;
                timer.Start();
                
                Thread.Sleep(10000000);
            }
            catch (Exception ex)
            {
                return;
            }
        }

        public static void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var now = DateTime.Now;
            var processes = Process.GetProcesses();
            var strategyProcesses = (from p in processes where p.ProcessName.StartsWith("WrapperTest") select p.ProcessName).ToList();

            //if ((now.Hour == 8 && (now.Minute >= 50 && now.Minute < 59)) || (now.Hour == 20 && (now.Minute >= 50 && now.Minute < 59)) || (now.Hour == 13 && (now.Minute >= 20 && now.Minute < 29)))
            if ((now.Hour == 6 && (now.Minute >= 50 && now.Minute < 59)) || (now.Hour == 18 && (now.Minute >= 50 && now.Minute < 59)))
            {

                //if (!strategyProcesses.Contains("WrapperTestRealTrading"))
                //{
                //    Console.WriteLine("启动WrapperTestRealTrading");
                //    StartProcess(@"C:\CTP.Solution-20121028\CTP.Solution.Sandbox\WrapperTest\bin\DebugRealTrading\WrapperTestRealTrading.exe", "true 5");
                //}

                //if (!strategyProcesses.Contains("WrapperTestSandBox"))
                //{
                //    Console.WriteLine("启动WrapperTestSandBox");
                //    StartProcess(@"C:\CTP.Solution-20121028\CTP.Solution.Sandbox\WrapperTest\bin\DebugSandBox\WrapperTestSandBox.exe", "true 2");
                //}

                //if (!strategyProcesses.Contains("WrapperTestSandBox2"))
                //{
                //    Console.WriteLine("启动WrapperTestSandBox2");
                //    StartProcess(@"C:\CTP.Solution-20121028\CTP.Solution.Sandbox\WrapperTest\bin\DebugSandBox2\WrapperTestSandBox2.exe", "true 2");
                //}

                //if (!strategyProcesses.Contains("WrapperTestQuote"))
                //{
                //    Console.WriteLine("启动WrapperTestQuote");
                //    StartProcess(@"C:\CTP.Solution-20121028\CTP.Solution.Sandbox\WrapperTest\bin\DebugQuote\WrapperTestQuote.exe", "true 3");
                //}

                //if (!strategyProcesses.Contains("WrapperTestPrompt"))
                //{
                //    Console.WriteLine("启动WrapperTestPrompt");
                //    StartProcess(@"C:\CTP.Solution-20121028\CTP.Solution.Sandbox\WrapperTest\bin\DebugPrompt\WrapperTestPrompt.exe", "true 5");
                //}

                if (!strategyProcesses.Contains("WrapperTestPromptTest"))
                {
                    Console.WriteLine("启动WrapperTestPromptTest");
                    StartProcess(@"C:\CTP.Solution-20121028\CTP.Solution.Sandbox\WrapperTest\bin\DebugPromptTest\WrapperTestPromptTest.exe", "true 2");
                }

                if (!strategyProcesses.Contains("WrapperTestPromptTestReal"))
                {
                    Console.WriteLine("启动WrapperTestPromptTestReal");
                    StartProcess(@"C:\CTP.Solution-20121028\CTP.Solution.Sandbox\WrapperTest\bin\DebugPromptTestReal\WrapperTestPromptTestReal.exe", "true 5");
                }

                if (!strategyProcesses.Contains("WrapperTestPromptTest2"))
                {
                    Console.WriteLine("启动WrapperTestPromptTest2");
                    StartProcess(@"C:\CTP.Solution-20121028\CTP.Solution.Sandbox\WrapperTest\bin\DebugPromptTest2\WrapperTestPromptTest2.exe", "true 2");
                }
            }

            if(now.Hour == 15 && (now.Minute >= 5))
            {
                Environment.Exit(0);
            }
        }

        public static void StartProcess(string fileName, string arguments)
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(fileName, arguments);
            p.Start();
        }
    }
}
