

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace HttpNewsPAT_Kantuganov
{
    public class Program
    {
        static void Main(string[] args)
        {
            SingIn("emilys", "emilyspass");
            Console.Read();

        }
        public static void SingIn(string username, string password)
        {
            CookieContainer cookieContainer = new CookieContainer();
            string url = "https://dummyjson.com/auth/login";
            Debug.WriteLine($"Выполняем запрос: {url}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = cookieContainer;

            string postData = $"username={username}&password={password}";
            byte[] data = Encoding.ASCII.GetBytes(postData);
            request.ContentLength = data.Length;

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            using (HttpWebResponse responseCookieGeter = (HttpWebResponse)request.GetResponse()) {
                Debug.WriteLine($"Статус выполнения: {responseCookieGeter.StatusCode}");
                string cookieHeader = responseCookieGeter.Headers["Set-Cookie"]; ;

                Uri uri = new Uri(url);
                CookieCollection cookies = cookieContainer.GetCookies(uri);

                Console.WriteLine("COOKIES: ");
                foreach (Cookie cookie in cookies)
                {
                    Console.WriteLine($"{cookie.Name}: \n{cookie.Value}");
                }
            }

        }
    }
}
