using System.Text;

namespace Hdq.PersonDataManager.Api.Dal
{
    public static class StringExtensions
    {
        public static string Enclose(this string s, string e = "\"")
        {
            return $"{e}{s}{e}";
        }


        public static string AsUtf8String(this byte[] b)
        {
            return Encoding.UTF8.GetString(b);
        }

    }
}