using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PupuTool
{
    /// <summary>
    /// Json序列化和反序列化方法
    /// </summary>
    public static class JsonExtensions
    {
        /// <summary>
        /// 对象转换为Json字符串(使用Newtonsoft.Json4.5 Release 5)
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static string ToJson(this object obj, JsonSerializerSettings settings = null)
        {
            settings = settings ?? new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,//忽略循环引用 即不序列化循环引用
            };
            Formatting format = Formatting.None; //Formatting.Indented表示生成的json是格式化好的
            return JsonConvert.SerializeObject(obj, format, settings);
        }

        /// <summary>
        /// Json字符串转换为对象(使用Newtonsoft.Json4.5 Release 5)
        /// </summary>
        /// <param name="json"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static T DeserializeJson<T>(this string json, JsonSerializerSettings settings = null) where T : class
        {
            settings = settings ?? new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,//忽略循环引用 即不序列化循环引用
            };
            return JsonConvert.DeserializeObject(json, typeof(T), settings) as T;
        }
    }
}
