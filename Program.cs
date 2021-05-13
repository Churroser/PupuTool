using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace PupuTool
{
    public class Program
    {
        //最后签到日期
        static DateTime? lastSignDate;
        static TimeSpan time;

        //定时器,一分钟执行一次
        static System.Timers.Timer daySkipTime = new System.Timers.Timer(1000 * 60 * 1);

        static async Task Main(string[] args)
        {
            LogHelper.WriteCustom(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + "程序已开启.", "Application\\");
            Console.WriteLine("朴朴签到小助手");
            Console.WriteLine(DateTime.Now.ToString());

            if (await GetConfig("Authorization") == null || await GetConfig("Authorization") == "")
            {
                Console.WriteLine("登录身份未配置");
                return;
            }

            main:
            Console.WriteLine("1.分享任务;2.签到任务;3.每天定时自动签到分享;");
            string command = Console.ReadLine();

            if (command == "1")
            {
                await SignShare();
                goto main;
            }
            else if (command == "2")
            {
                await Sign();
                goto main;
            }
            else if (command == "3")
            {

                if (await GetConfig("PlanTime") != null && await GetConfig("PlanTime") != "")
                {

                    if (!TimeSpan.TryParse(GetConfig("PlanTime").GetAwaiter().GetResult(), out time))
                    {
                        Console.WriteLine("定时任务时间配置错误,时间不合法");
                        return;
                    }
                    time = TimeSpan.Parse(GetConfig("PlanTime").GetAwaiter().GetResult());

                    daySkipTime.Elapsed += new System.Timers.ElapsedEventHandler(TimingTask); //到达时间的时候执行事件；   
                    daySkipTime.AutoReset = true;   //设置是执行一次（false）还是一直执行(true)；   
                    daySkipTime.Enabled = true;     //是否执行System.Timers.Timer.Elapsed事件；  
                    Console.Read();
                }
            }

        }

        /// <summary>
        /// 定时任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void TimingTask(object sender, ElapsedEventArgs e)
        {
            Console.Clear();
            Console.WriteLine(DateTime.Now);

            if (DateTime.Now.TimeOfDay > time && (!lastSignDate.HasValue || DateTime.Now.Date > lastSignDate))
            {
                Sign().GetAwaiter().GetResult();
                SignShare().GetAwaiter().GetResult();              

                lastSignDate = DateTime.Now.Date;
            }
        }

        /// <summary>
        /// 获取配置文件
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        static async Task<string> GetConfig(string node)
        {
            string jsonfile = $"{AppDomain.CurrentDomain.BaseDirectory}config.json";//JSON文件路径
            using (System.IO.StreamReader file = System.IO.File.OpenText(jsonfile))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject o = (JObject)JToken.ReadFrom(reader);
                    var value = o[node].ToString();
                    return value;
                }
            }
        }

        /// <summary>
        /// 获取朴分记录
        /// </summary>
        /// <returns></returns>
        static async Task<string> GetCoinRecord()
        {
            string t = ((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000).ToString();
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", await GetConfig("Authorization"));
            var result = await client.GetAsync($"https://j1.pupuapi.com/client/coin/record?time_to={t}&page=1&size=20");
            string resultStr = await result.Content.ReadAsStringAsync();
            //Console.WriteLine($"{resultStr}");
            return resultStr;
        }


        /// <summary>
        /// 分享获得朴分
        /// </summary>
        /// <returns></returns>
        static async Task SignShare()
        {
            //分享
            Console.WriteLine($"{DateTime.Now} -----开始执行分享任务-----");

            HttpClient client = new HttpClient();
            HttpContent httpContent = new StringContent("");
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpContent.Headers.ContentType.CharSet = "utf-8";
            client.DefaultRequestHeaders.Add("Authorization", await GetConfig("Authorization"));
            var result = await client.PostAsync("https://j1.pupuapi.com/client/game/sign/share", httpContent);
            string resultStr = await result.Content.ReadAsStringAsync();

            var shareResult = resultStr.DeserializeJson<dynamic>();
            if (shareResult.errcode != 0)
            {
                LogHelper.WriteCustom($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") }{shareResult.errmsg}", "Business\\");
                Console.WriteLine($"{DateTime.Now}  {shareResult.errmsg}");
            }
            else
            {
                LogHelper.WriteCustom($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") }分享成功，获得朴分:{shareResult.data}", "Business\\");
                Console.WriteLine($"{DateTime.Now}  分享成功，获得朴分:{shareResult.data}");
            }

            return;
        }

        /// <summary>
        /// 签到获得朴分
        /// </summary>
        /// <returns></returns>
        static async Task Sign()
        {
            //签到
            Console.WriteLine($"{DateTime.Now} -----开始执行签到任务-----");

            HttpClient client = new HttpClient();
            HttpContent httpContent = new StringContent("");
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpContent.Headers.ContentType.CharSet = "utf-8";
            client.DefaultRequestHeaders.Add("Authorization", await GetConfig("Authorization"));
            var result = await client.PostAsync("https://j1.pupuapi.com/client/game/sign?city_zip=350100&challenge=", httpContent);
            string resultStr = await result.Content.ReadAsStringAsync();

            var signResult = resultStr.DeserializeJson<Rootobject>();
            if (signResult.errcode != 0)
            {
                LogHelper.WriteCustom($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") } {signResult.errmsg}", "Business\\");
                Console.WriteLine($"{DateTime.Now}  {signResult.errmsg}");
            }
            else
            {
                LogHelper.WriteCustom($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") }  签到成功，获得朴分:{signResult.data.increased_score}", "Business\\");
                Console.WriteLine($"{DateTime.Now}  签到成功，获得朴分:{signResult.data.increased_score}");
                //有获得优惠券的情况，待处理
                if (signResult.data.reward_coupon_list != null && signResult.data.reward_coupon_list.Count > 0)
                {
                    signResult.data.reward_coupon_list.ForEach(x =>
                    {
                        LogHelper.WriteCustom($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") }  获得优惠券:满{x.condition_amount}减{x.discount_amount}", "Business\\");
                        Console.WriteLine($"获得优惠券:满{x.condition_amount}减{x.discount_amount}");
                    });
                }
            }
            return;
        }
    }


    public class Rootobject
    {
        public int errcode { get; set; }
        public string errmsg { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public int increased_score { get; set; }
        public List<Reward_Coupon_List> reward_coupon_list { get; set; }
        public string title { get; set; }
        public string sub_title { get; set; }
        public long current_time { get; set; }
    }

    public class Reward_Coupon_List
    {
        public int condition_amount { get; set; }
        public int discount_amount { get; set; }
    }

}
