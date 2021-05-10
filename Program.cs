using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace PupuTool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("朴朴签到小助手");
            Console.WriteLine(DateTime.Now.ToString());

            if (await GetConfig("Authorization") == null || await GetConfig("Authorization") == "")
            {
                Console.WriteLine("登录身份未配置");
                return;
            }

            main:
            Console.WriteLine("1.分享任务;2.签到任务");
            string command = Console.ReadLine();

            if (command == "1")
            {
                //分享
                Console.WriteLine($"{DateTime.Now} -----开始执行分享任务-----");
                string shareResultStr = await SignShare();
                var shareResult = shareResultStr.DeserializeJson<dynamic>();
                if (shareResult.errcode != 0)
                {
                    Console.WriteLine($"{DateTime.Now}  {shareResult.errmsg}");
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now}  分享成功，获得朴分:{shareResult.data}");
                }
            }
            else if (command == "2")
            {
                //签到
                Console.WriteLine($"{DateTime.Now} -----开始执行签到任务-----");
                string signResultStr = await Sign();
                var signResult = signResultStr.DeserializeJson<dynamic>();
                if (signResult.errcode != 0)
                {
                    Console.WriteLine($"{DateTime.Now}  {signResult.errmsg}");
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now}  签到成功，获得朴分:{signResult.data.increased_score}");
                    //有获得优惠券的情况，待处理
                    if (signResult.data.reward_coupon_list != "")
                    {
                        /*
                         {
                          "errcode": 0,
                          "errmsg": "",
                          "data": {
                            "increased_score": 8,
                            "reward_coupon_list": [
                              {
                                "condition_amount": 4900,
                                "discount_amount": 500
                              }
                            ],
                            "title": "“来朴朴，一起眼见为食”",
                            "sub_title": " ",
                            "current_time": 1620665176566
                          }
                        }
                         */
                    }
                }
            }
            goto main;
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
        static async Task<string> SignShare()
        {
            HttpClient client = new HttpClient();
            HttpContent httpContent = new StringContent("");
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpContent.Headers.ContentType.CharSet = "utf-8";
            client.DefaultRequestHeaders.Add("Authorization", await GetConfig("Authorization"));
            var result = await client.PostAsync("https://j1.pupuapi.com/client/game/sign/share", httpContent);
            string resultStr = await result.Content.ReadAsStringAsync();
            //Console.WriteLine($"{resultStr}");
            return resultStr;
        }

        /// <summary>
        /// 签到获得朴分
        /// </summary>
        /// <returns></returns>
        static async Task<string> Sign()
        {
            HttpClient client = new HttpClient();
            HttpContent httpContent = new StringContent("");
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpContent.Headers.ContentType.CharSet = "utf-8";
            client.DefaultRequestHeaders.Add("Authorization", await GetConfig("Authorization"));
            var result = await client.PostAsync("https://j1.pupuapi.com/client/game/sign?city_zip=350100&challenge=", httpContent);
            string resultStr = await result.Content.ReadAsStringAsync();
            //Console.WriteLine($"{resultStr}");
            return resultStr;
        }
    }
}
