using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ThirdParty.Json.LitJson;
using System.IO;
using System.Net;

namespace FeishuRobot
{
    class Robot
    {
        private const string docUrl = "https://open.feishu.cn/open-apis/doc/v2/meta/";
        private const string docToken = "doccn9aB6u2oMVhVDQBfV67P51c";
        private const string randomSentenceUrl = "https://v1.hitokoto.cn/?c=a&c=b&c=c&c=e&c=f&c=h&c=i&c=k";
        private const string url = "https://open.feishu.cn/open-apis/bot/v2/hook/259d8d53-167b-471c-b860-2d2de3228d98";
        private const string weatherUrl = "https://devapi.qweather.com/v7/weather/3d?";
        private const string cityInfoUrl = "https://geoapi.qweather.com/v2/city/lookup?";
        private const string freeDayCheckUrl = "https://timor.tech/api/holiday/info/";
        private const string weatherKey = "a4d4f41646a5454e891e70c8ff54c37e";
        private const string secret = "nmhkJtm32yeibZPLkfdiUh";
        private const string msgType = "text";
        private const string text = "100! =";
        
        private HttpClient client;
        private HttpClient weatherClient;

        private bool newDay;
        private bool isWorkDay;
        public Robot()
        {
            client = new HttpClient();
            // 和风天气v7版本API默认采用gzip压缩，可大幅降低流量，提高响应速度，需对数据进行解压。
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip
            };
            weatherClient = new HttpClient(handler);
            //DailyRefresh();
        }

        public void Run()
        {
            while (true)
            {
                //if (CheckDailyRefresh())
                //{
                //    DailyRefresh();
                //}

                //if (!isWorkDay)
                //{
                //    continue;
                //}

                //if (CheckWeatherTime())
                //{
                //    SendWeatherMessage();
                //    newDay = false;
                //}
                
                if (Check())
                {
                    var t = DrinkingText();
                    string message = t.Result;
                    Console.WriteLine(message);
                    SendMessage(message);
                }
                Thread.Sleep(1000 * 60);
            }
        }

        private void SendWeatherMessage()
        {
            var t = WeatherText();
            string message = t.Result;
            Console.WriteLine(message);
            SendMessage(message);
        }

        private bool CheckWeatherTime()
        {
            if (!newDay)
            {
                return false;
            }

            if (CheckTime(DateTime.Now,9,30,10,30)) // 9:30-10:30
            {
                return true;
            }

            return false;
        }

        private bool CheckDailyRefresh()
        {
            if (newDay)
            {
                return false;
            }

            if (CheckTime(DateTime.Now,0,0,0,2))    // 当前时间在00:00-00:02之间认为新的一天到了
            {
                return true;
            }

            return false;
        }

        public void DailyRefresh()
        {
            newDay = true;
            try
            {
                isWorkDay = !(CheckFreeDay().Result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                isWorkDay = true;
            }
            Console.WriteLine(isWorkDay);
        }

        public async Task<string> DrinkingText()
        {
            string txt = "提醒喝水小助手:\n现在是" + DateTime.Now.Hour + "点\n" +
                            $"距离下班还有{GetRemainTime(DateTime.Now,new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,18,30,0))}分钟,坚持住,打工人!\n"+
                          "工作再忙也要记得挺直腰杆多喝热水哦!\n<at user_id=\"all\">小伙子们！</at>\n\n";
            var data = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await client.PostAsync(randomSentenceUrl, data);
            var responseString = await response.Content.ReadAsStringAsync();
             JsonConvert.DeserializeObject(responseString);
            var jsonData = JsonMapper.ToObject(responseString);
            var sentence = jsonData["hitokoto"];
            var from = jsonData["from"];
            txt += sentence + "----"+ from + "\n";
            return txt;
        }

        public async Task<string> WeatherText()
        {
            string ret = "";
            string targetCityUrl = cityInfoUrl + "location=binjiang&adm=zhejiang&key=" + weatherKey;
            var response = await weatherClient.GetAsync(targetCityUrl);
            var cityContent = response.Content.ReadAsStringAsync().Result;
            var cityData = JsonMapper.ToObject(cityContent);
            var id = cityData["location"][0]["id"];
            string targetWeatherUrl = weatherUrl + "location=" +id+"&key=" + weatherKey;
            response = await weatherClient.GetAsync(targetWeatherUrl);
            var weatherContent = response.Content.ReadAsStringAsync().Result;

            ret = WeatherParser(weatherContent);
            return ret;
        }

        private string WeatherParser(string weatherContent)
        {
            var weatherData = JsonMapper.ToObject(weatherContent);
            var todayWeatherData = weatherData["daily"][0];
            var date = todayWeatherData["fxDate"].ToString();   // 今天的日期
            var tempMax = todayWeatherData["tempMax"].ToString();   // 今天的最高温度
            var tempMin = todayWeatherData["tempMin"].ToString();   // 今天的最低温度
            var weatherDay = todayWeatherData["textDay"].ToString();    // 今日天气
            var ret = "天气小助手:\n今日天气: " + weatherDay + "\n" +
                          $"最高温度: {tempMax}    最低温度: {tempMin} \n<at user_id=\"all\">所有人</at>\n\n";
            return ret;
        }

        public async void SendMessage(string message)
        {
            var obj = new MessageData(msgType, new MessageContent(message));
            var json = JsonConvert.SerializeObject(obj);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, data);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
        }

        public bool Check()
        {
            // 是否在一定时间范围内
            if (CheckTime(DateTime.Now,9,30,18,30))
            {
                // 是否是整点
                return DateTime.Now.Minute == 0;
            }
            return false;
        }

        /// <summary>
        /// {
        //  "code": 0,              // 0服务正常。-1服务出错
        //  "type": {
        //    "type": enum(0, 1, 2, 3), // 节假日类型，分别表示 工作日、周末、节日、调休。
        //    "name": "周六",         // 节假日类型中文名，可能值为 周一 至 周日、假期的名字、某某调休。
        //    "week": enum(1 - 7)    // 一周中的第几天。值为 1 - 7，分别表示 周一 至 周日。
        //  },
        //  "holiday": {
        //    "holiday": false,     // true表示是节假日，false表示是调休
        //    "name": "国庆前调休",  // 节假日的中文名。如果是调休，则是调休的中文名，例如'国庆前调休'
        //    "wage": 1,            // 薪资倍数，1表示是1倍工资
        //    "after": false,       // 只在调休下有该字段。true表示放完假后调休，false表示先调休再放假
        //    "target": '国庆节'     // 只在调休下有该字段。表示调休的节假日
        //  }
        //  }
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckFreeDay()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var targetUrl = freeDayCheckUrl + today;
            var response = await client.GetAsync(targetUrl);
            var content = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine(content);
            var dayInfo =  JsonMapper.ToObject(content);
            var code = dayInfo["code"];
            if (((int)code) != 0)
            {
                throw new Exception("code error");
            }
            var dayType = dayInfo["type"]["type"];
            if (((int)dayType) != 0)
            {
                return true;
            }
            return false;
        }

        public bool CheckTime(DateTime time,int startHour, int startMinute, int endHour,int endMinute)
        {
            if (time.Hour == startHour)
            {
                return time.Minute >= startMinute;
            }
            if (time.Hour == endHour)
            {
                return time.Minute <= endMinute;
            }
            return time.Hour > startHour && time.Hour < endHour;
        }

        public string GetRemainTime(DateTime time1, DateTime time2)
        {
            return (time2 - time1).TotalMinutes.ToString();
        }
    }
}
