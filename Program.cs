using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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

        //access_token过期时间
        static DateTime? accessTokenExpires;

        #region 身份信息
        //登录凭证，2小时过期
        static string authorization;
        //昵称
        static string nickName;
        //是否绑定手机号
        static bool isBindPhone;
        //是否新用户
        static bool isNewUser;
        #endregion

        //定时器,一分钟执行一次
        static System.Timers.Timer daySkipTime = new System.Timers.Timer(1000 * 60 * 1);

        static async Task Main(string[] args)
        {
            LogHelper.WriteCustom(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + "程序已开启.", "Application\\");
            Console.Title = "朴朴超市助手——未登录";

            if (await GetConfig("refresh_token") == null || await GetConfig("refresh_token") == "")
            {
                Console.WriteLine("refresh_token未配置");
                return;
            }
            if (await Refresh_token())
            {
                Console.Title = $"朴朴超市助手——{nickName}";
                Console.WriteLine($"昵称:{nickName}\n" +
                    $"{(isBindPhone ? "已绑定" : "未绑定")}手机号\n" +
                    $"用户类型:{(isNewUser ? "新用户" : "老用户")}");
            }

            main:
            Console.WriteLine("\n请输入你要执行的操作");
            Console.WriteLine("1.分享任务;2.签到任务;3.每天定时自动签到分享;4.查询朴分");
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

                    Sign().GetAwaiter().GetResult();
                    SignShare().GetAwaiter().GetResult();

                    time = TimeSpan.Parse(GetConfig("PlanTime").GetAwaiter().GetResult());

                    daySkipTime.Elapsed += new System.Timers.ElapsedEventHandler(TimingTask); //到达时间的时候执行事件；   
                    daySkipTime.AutoReset = true;   //设置是执行一次（false）还是一直执行(true)；   
                    daySkipTime.Enabled = true;     //是否执行System.Timers.Timer.Elapsed事件；  

                    Console.WriteLine("定时任务执行中,可以执行其他操作");
                    goto main;
                }
            }
            else if (command == "4")
            {
                await GetCoin();
                goto main;
            }

        }

        /// <summary>
        /// 定时任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void TimingTask(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.TimeOfDay > time && (!lastSignDate.HasValue || DateTime.Now.Date > lastSignDate))
            {
                Console.WriteLine($"{DateTime.Now} 开始执行定时任务");
                if (!accessTokenExpires.HasValue || accessTokenExpires < DateTime.Now)
                {
                    Refresh_token().GetAwaiter().GetResult();
                }
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
            string url = $"https://j1.pupuapi.com/client/coin/record?time_to={t}&page=1&size=20";
            HttpWebRequest request = null;
            HttpWebResponse httpResponse = null;
            request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("Authorization", authorization);
            try
            {
                httpResponse = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                httpResponse = (HttpWebResponse)ex.Response;
            }
            Stream st = httpResponse.GetResponseStream();
            StreamReader reader = new StreamReader(st, Encoding.GetEncoding("utf-8"));
            string result = reader.ReadToEnd();

            return result;
        }

        /// <summary>
        /// 查询朴分余额
        /// </summary>
        /// <returns></returns>
        static async Task GetCoin()
        {
            string t = ((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000).ToString();
            string url = $"https://j1.pupuapi.com/client/coin";
            HttpWebRequest request = null;
            HttpWebResponse httpResponse = null;
            request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("Authorization", authorization);
            try
            {
                httpResponse = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                httpResponse = (HttpWebResponse)ex.Response;
            }
            Stream st = httpResponse.GetResponseStream();
            StreamReader reader = new StreamReader(st, Encoding.GetEncoding("utf-8"));
            string resultStr = reader.ReadToEnd();
            var result = resultStr.DeserializeJson<dynamic>();
            if (result.errcode != 0)
            {
                Console.WriteLine($"{DateTime.Now}  {result.errmsg}");
            }
            else
            {
                DateTime UnixTimeStampStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime expireDate = UnixTimeStampStart.AddSeconds(Convert.ToInt64(result.data.expire_time) / 1000).ToLocalTime();
                Console.WriteLine($"\n{DateTime.Now} 查询积分成功，朴分:{result.data.balance}," +
                            $"其中 {result.data.expiring_coin} 分将在 {expireDate.ToString("yyyy-MM-dd HH:mm:ss")} 过期\n\n");
            }
            return;
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
            client.DefaultRequestHeaders.Add("Authorization", authorization);
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
            client.DefaultRequestHeaders.Add("Authorization", authorization);
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
                        Console.WriteLine($"获得优惠券:满{x.condition_amount / 100m}元减{x.discount_amount / 100m}元");
                    });
                }
            }
            return;
        }

        /// <summary>
        /// Refresh_token换access_token,access_token目前的有效期仅2小时
        /// </summary>
        /// <returns></returns>
        static async Task<bool> Refresh_token()
        {
            SortedDictionary<string, string> postData = new SortedDictionary<string, string>();
            postData.Add("refresh_token", await GetConfig("refresh_token"));

            HttpClient client = new HttpClient();
            HttpContent httpContent = new StringContent(postData.ToJson());
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpContent.Headers.ContentType.CharSet = "utf-8";
            //client.DefaultRequestHeaders.Add("pp_storeid", "09c4ddb4-d87b-493f-9144-08190bb4f5c1");
            //client.DefaultRequestHeaders.Add("pp-version", "2021042003");
            //client.DefaultRequestHeaders.Add("pp-os", "20");
            //client.DefaultRequestHeaders.Add("mark-num", "12");

            var result = await client.PutAsync("https://cauth.pupuapi.com/clientauth/user/refresh_token", httpContent);
            string resultStr = await result.Content.ReadAsStringAsync();

            var resultJson = resultStr.DeserializeJson<dynamic>();
            if (resultJson.errcode != 0)
            {
                LogHelper.WriteCustom($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") } {resultJson.errmsg}", "Business\\");
                Console.WriteLine($"{DateTime.Now}  {resultJson.errmsg}");
            }
            else
            {

                authorization = "Bearer " + resultJson.data.access_token;
                DateTime UnixTimeStampStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                accessTokenExpires = UnixTimeStampStart.AddSeconds(Convert.ToInt64(resultJson.data.expires_in) / 1000).ToLocalTime();
                nickName = resultJson.data.nick_name;
                isBindPhone = Convert.ToBoolean(resultJson.data.is_bind_phone);
                isNewUser = Convert.ToBoolean(resultJson.data.is_new_user);
                return true;
            }
            return false;
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
