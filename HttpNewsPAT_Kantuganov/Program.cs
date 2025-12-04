

using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;

namespace HttpNewsPAT_Kantuganov
{
    public class Program
    {
        static Cookie Token;
        static string logFilePath = "debug_trace.log";
        static void Main(string[] args)
        {
            SingIn("tomsmith", "SuperSecretPassword!");
            if (Token != null)
            {
                string pageContent = GetContent("https://the-internet.herokuapp.com/secure");
                Console.WriteLine("Содержимое страницы:");
                ParsingHtml(pageContent);
            }
            if (File.Exists(logFilePath))
            {
                string logContent = File.ReadAllText(logFilePath);
                Console.WriteLine(logContent);
            }
            Console.Read();

        }
        public static void SingIn(string username, string password)
        {
            CookieContainer cookieContainer = new CookieContainer();
            string url = "https://the-internet.herokuapp.com/authenticate";

            WriteToLog($"Выполняем запрос: {url}");
            WriteToLog($"Логин: {username}, Пароль: {new string('*', password.Length)}");

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
                WriteToLog($"Отправлено данных: {data.Length} байт");
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                WriteToLog($"Статус ответа: {response.StatusCode}");

                Uri uri = new Uri(url);
                CookieCollection cookies = cookieContainer.GetCookies(uri);

                if (cookies.Count > 0)
                {
                    Token = cookies[0];
                    WriteToLog($"Получен токен: {Token.Name}={Token.Value}");
                    WriteToLog($"Домен токена: {Token.Domain}, Путь: {Token.Path}");
                }
                else
                {
                    WriteToLog("Токен не получен");
                }
            }
        }

        public static string GetContent(string url)
        {
            WriteToLog($"Запрос защищенной страницы: {url}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(Token);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                WriteToLog($"Статус защищенной страницы: {response.StatusCode}");

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string content = reader.ReadToEnd();
                    WriteToLog($"Получено символов: {content.Length}");
                    return content;
                }
            }
        }

        public static void ParsingHtml(string htmlCode)
        {
            WriteToLog("Начало парсинга HTML");

            var html = new HtmlDocument();
            html.LoadHtml(htmlCode);
            var document = html.DocumentNode;

            var exampleDivs = document.Descendants("div").Where(n => n.HasClass("example"));

            WriteToLog($"Найдено элементов с классом 'example': {exampleDivs.Count()}");

            foreach (HtmlNode div in exampleDivs)
            {
                var h2 = div.Descendants("h2").FirstOrDefault();
                if (h2 != null)
                {
                    string header = h2.InnerText.Trim();
                    Console.WriteLine("Заголовок: " + header);
                    WriteToLog($"Парсинг: Заголовок = {header}");
                }

                var h4 = div.Descendants("h4").FirstOrDefault();
                if (h4 != null)
                {
                    string text = h4.InnerText.Trim();
                    Console.WriteLine("Текст: " + text);
                    WriteToLog($"Парсинг: Текст = {text}");
                }

                var links = div.Descendants("a");
                foreach (var link in links)
                {
                    var href = link.GetAttributeValue("href", "");
                    var text = link.InnerText.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        Console.WriteLine("Ссылка: " + text + " -> " + href);
                        WriteToLog($"Парсинг: Ссылка {text} -> {href}");
                    }
                }

                Console.WriteLine();
            }

            WriteToLog("Парсинг HTML завершен");
        }
        public static void WriteToLog(string message)
        {
            try
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);

                Debug.WriteLine(logMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи в лог: {ex.Message}");
            }
        }
    }
}
