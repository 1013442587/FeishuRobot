using System;
using System.Security.Cryptography;
using System.Text;
namespace FeishuRobot
{
    class Util
    {
        public static string Base64WithSha256(string data, string secret)
        {
            string signRet = string.Empty;
            using (HMACSHA256 mac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = mac.ComputeHash(Encoding.UTF8.GetBytes(data));
                signRet = Convert.ToBase64String(hash);
            }
            return signRet;
        }

        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }
    }
}
