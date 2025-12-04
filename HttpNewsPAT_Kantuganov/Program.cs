

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Policy;
using System.Text;

namespace HttpNewsPAT_Kantuganov
{
    public class Program
    {
        static Cookie Token;
        static void Main(string[] args)
        {
            SingIn("tomsmith", "SuperSecretPassword!");
            if (Token != null)
            {
                string pageContent = GetContent("https://the-internet.herokuapp.com/secure");
                Console.WriteLine("Содержимое страницы:");
                Console.WriteLine(pageContent);
            }
            Console.Read();

        }
        public static void SingIn(string username, string password)
        {
            CookieContainer cookieContainer = new CookieContainer();
            string url = "https://the-internet.herokuapp.com/authenticate";
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
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {

                Uri uri = new Uri(url);
                CookieCollection cookies = cookieContainer.GetCookies(uri);
                Token = cookies[0];
            }
        }
        public static string GetContent(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(Token);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
