using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using  WrapperTest;

namespace TestTrend
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("选择文件：");
            var filePath = Console.ReadLine();
            Console.WriteLine("窗口大小：");
            var windowLength = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("拟合的直线斜角要求，单位是角度：");
            var slope = Convert.ToDouble(Console.ReadLine());
            Console.WriteLine("是否使用移动均值：");
            var ma = Convert.ToBoolean(Console.ReadLine());

            MathUtils.Slope = slope;

            var reg = new Regex("最新价:[0-9]+");
            var regTime = new Regex("\\d{2}:\\d{2}:\\d{2}[.]\\d{3}");

            var sr = new StreamReader(filePath, Encoding.UTF8);
            string line;
            var quotes = new List<Tuple<string, double>>();

            while ((line = sr.ReadLine()) != null)
            {
                var matches = reg.Matches(line);
                var matchesTime = regTime.Matches(line);

                string time = null;
                double price = 0;

                foreach (var VARIABLE in matchesTime)
                {
                    time = VARIABLE.ToString();
                }

                foreach (var VARIABLE in matches)
                {
                    var s = VARIABLE.ToString().Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);
                    price = Convert.ToDouble(s[1]);
                }

                quotes.Add(new Tuple<string, double>(time, price));
            }

            sr.Close();

            var xdata = new List<double>();
            for (var i = 0; i < windowLength; i++)
            {
                xdata.Add(i);
            }

            var sw = new StreamWriter(string.Format("Trend_{0}_{1}_{2}.txt", windowLength, slope, ma), false, Encoding.UTF8);

            for (var i = 0; i < quotes.Count - windowLength; i++)
            {
                var currentWindow = new List<double>();

                for (var j = 0; j < windowLength; j++)
                {
                    currentWindow.Add(quotes[i + j].Item2);
                }

                var isPointingUp = MathUtils.IsPointingUp(xdata, currentWindow, slope);
                var isPointingDown = MathUtils.IsPointingDown(xdata, currentWindow, slope);

                sw.WriteLine("向上{0}，正切{1}，向下{2}，正切{3}，行情区间{4}-{5}，时间区间{6}-{7}", isPointingUp.Item1, isPointingUp.Item2,
                    isPointingDown.Item1, isPointingDown.Item2, i,
                    i + windowLength - 1, quotes[i].Item1, quotes[i + windowLength - 1].Item1);
            }

            sw.Close();
        }
    }
}
