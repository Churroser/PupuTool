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
            Console.WriteLine("朴朴主动签到小助手");
            Console.WriteLine(DateTime.Now.ToString());

            if (await GetConfig("Authorization") == null || await GetConfig("Authorization") == "")
            {
                Console.WriteLine("登录身份未配置");
                return;
            }

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
            Console.ReadKey();
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
    }
}
