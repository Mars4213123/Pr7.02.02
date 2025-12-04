using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace HttpNewsPAT_Kantuganov
{
    public class Program
    {
        static Cookie Token;
        static string logFilePath = "debug_trace.log";
        static void Main(string[] args)
        {
            WriteToLog("Начало работы");

            string siteUrl = "https://the-internet.herokuapp.com/secure";

            Console.Write("Требуется авторизация? (да/нет): ");
            string needAuth = Console.ReadLine().ToLower();

            if (needAuth == "да")
            {
                Console.Write("Введите логин/email: ");
                string login = Console.ReadLine();

                Console.Write("Введите пароль: ");
                string password = Console.ReadLine();

                Console.Write("Введите поле для логина (username/email или Enter для 'username'): ");
                string loginField = Console.ReadLine();
                if (string.IsNullOrEmpty(loginField))
                {
                    loginField = "username";
                }

                Console.Write("Введите поле для пароля (Enter для 'password'): ");
                string passwordField = Console.ReadLine();
                if (string.IsNullOrEmpty(passwordField))
                {
                    passwordField = "password";
                }

                Console.Write("URL для логина (Enter для основного URL): ");
                string loginUrl = Console.ReadLine();

                if (string.IsNullOrEmpty(loginUrl))
                {
                    loginUrl = siteUrl;
                }

                SingIn(loginUrl, login, password, loginField, passwordField);
            }
            Console.Write("\nДобавить эту страницу в базу новостей? (да/нет): ");
            string addToNews = Console.ReadLine().ToLower();

            if (addToNews == "да" || addToNews == "yes" || addToNews == "y" || addToNews == "д")
            {
                AddNews(siteUrl);
            }
            string pageContent = GetContent(siteUrl);

            if (!string.IsNullOrEmpty(pageContent))
            {
                ParsingHtml(pageContent, siteUrl);
            }

            WriteToLog("Завершение");

            Console.WriteLine($"\nЛог сохранен: {logFilePath}");
            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }



        public static void SingIn(string url, string login, string password, string loginField = "username", string passwordField = "password")
        {
            CookieContainer cookieContainer = new CookieContainer();

            WriteToLog($"Запрос: {url}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = cookieContainer;

            string postData = $"{loginField}={Uri.EscapeDataString(login)}&{passwordField}={Uri.EscapeDataString(password)}";
            byte[] data = Encoding.UTF8.GetBytes(postData);
            request.ContentLength = data.Length;

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                WriteToLog($"Статус: {response.StatusCode}");

                Uri uri = new Uri(url);
                CookieCollection cookies = cookieContainer.GetCookies(uri);

                if (cookies.Count > 0)
                {
                    Token = cookies[0];
                }
            }
        }

        public static void AddNews(string url)
        {
            try
            {
                WriteToLog($"Добавление новости: {url}");

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://...");
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                string postData = $"url={Uri.EscapeDataString(url)}";
                byte[] data = Encoding.UTF8.GetBytes(postData);
                request.ContentLength = data.Length;

                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Console.WriteLine($"Статус добавления: {response.StatusCode}");
                    WriteToLog($"Статус добавления: {response.StatusCode}");

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string result = reader.ReadToEnd();
                        Console.WriteLine($"Результат: {result}");
                    }
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine($"Ошибка добавления: {ex.Message}");
                WriteToLog($"Ошибка добавления: {ex.Message}");
            }
        }

        public static string GetContent(string url)
        {
            WriteToLog($"Запрос страницы: {url}");

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                if (Token != null)
                {
                    request.CookieContainer = new CookieContainer();
                    request.CookieContainer.Add(Token);
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    WriteToLog($"Статус: {response.StatusCode}");

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string content = reader.ReadToEnd();
                        WriteToLog($"Символов: {content.Length}");
                        return content;
                    }
                }
            }
            catch (WebException ex)
            {
                WriteToLog($"Ошибка: {ex.Message}");
                Console.WriteLine($"Ошибка: {ex.Message}");
                return null;
            }
        }

        public static void ParsingHtml(string htmlCode, string url)
        {
            WriteToLog($"Парсинг: {url}");

            if (string.IsNullOrEmpty(htmlCode))
            {
                Console.WriteLine("HTML код пуст");
                return;
            }

            var html = new HtmlDocument();
            html.LoadHtml(htmlCode);
            var document = html.DocumentNode;

            var title = document.Descendants("title").FirstOrDefault();
            if (title != null)
            {
                Console.WriteLine($"Заголовок: {title.InnerText.Trim()}");
            }

            var headers = document.Descendants("h1")
                .Concat(document.Descendants("h2"))
                .Take(5);

            foreach (var header in headers)
            {
                if (!string.IsNullOrWhiteSpace(header.InnerText))
                {
                    Console.WriteLine($"Заголовок: {header.InnerText.Trim()}");
                }
            }

            var links = document.Descendants("a").Take(10);
            foreach (var link in links)
            {
                var href = link.GetAttributeValue("href", "");
                var text = link.InnerText.Trim();

                if (!string.IsNullOrEmpty(text) && text.Length < 50)
                {
                    if (!string.IsNullOrEmpty(href) && !href.StartsWith("http"))
                    {
                        if (href.StartsWith("/"))
                        {
                            Uri baseUri = new Uri(url);
                            href = new Uri(baseUri, href).ToString();
                        }
                    }

                    Console.WriteLine($"Ссылка: {text} -> {href}");
                }
            }

            var images = document.Descendants("img").Take(5);
            foreach (var img in images)
            {
                var src = img.GetAttributeValue("src", "");
                var alt = img.GetAttributeValue("alt", "");

                if (!string.IsNullOrEmpty(src))
                {
                    Console.WriteLine($"Изображение: {alt} -> {src}");
                }
            }
        }

        public static void WriteToLog(string message)
        {
            try
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи в лог: {ex.Message}");
            }
        }
    }
}