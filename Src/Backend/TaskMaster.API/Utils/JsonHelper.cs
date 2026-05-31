using System.Text.Json;

namespace TaskMaster.API.Utils
{
    public static class JsonHelper
    {
        public static string ToJson<T>(this T obj)
        {
            return JsonSerializer.Serialize(obj);
        }

        public static T? ToObject<T>(this string json)
        {
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}
