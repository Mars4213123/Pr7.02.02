using HtmlAgilityPack;
using System;
using System.Diagnostics;
using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;

namespace HttpNewsPAT_Kantuganov
{
    public class Program
    {
        static Cookie Token;
        static string logFilePath = "debug_trace.log";
        static HttpClient httpClient = new HttpClient();

        static async Task Main(string[] args)
        {

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

                await SingInAsync(loginUrl, login, password, loginField, passwordField);
            }

            string pageContent = await GetContentAsync(siteUrl);

            if (!string.IsNullOrEmpty(pageContent))
            {
                ParsingHtml(pageContent, siteUrl);
            }

            Console.Write("\nДобавить эту страницу в базу новостей? (да/нет): ");
            string addToNews = Console.ReadLine().ToLower();

            if (addToNews == "да" || addToNews == "yes" || addToNews == "y" || addToNews == "д")
            {
                await AddNewsAsync(siteUrl);
            }

            WriteToLog("Завершение");

            Console.WriteLine($"\nЛог сохранен: {logFilePath}");
            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }

        public static async Task SingInAsync(string url, string login, string password, string loginField = "username", string passwordField = "password")
        {
            WriteToLog($"Запрос: {url}");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(loginField, login),
                new KeyValuePair<string, string>(passwordField, password)
            });

            try
            {
                var response = await httpClient.PostAsync(url, content);

                WriteToLog($"Статус: {response.StatusCode}");

                if (response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
                {
                    foreach (var cookieHeader in cookieHeaders)
                    {
                        WriteToLog($"Cookie: {cookieHeader}");
                    }
                }

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Авторизация успешна");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка авторизации: {ex.Message}");
                WriteToLog($"Ошибка авторизации: {ex.Message}");
            }
        }

        public static async Task<string> GetContentAsync(string url)
        {
            WriteToLog($"Запрос страницы: {url}");

            try
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var response = await httpClient.GetAsync(url);

                WriteToLog($"Статус: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    WriteToLog($"Символов: {content.Length}");
                    return content;
                }
                else
                {
                    Console.WriteLine($"Ошибка: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                WriteToLog($"Ошибка: {ex.Message}");
                return null;
            }
        }

        public static async Task AddNewsAsync(string url)
        {
            WriteToLog($"Добавление новости: {url}");

            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("url", url)
                });

                var response = await httpClient.PostAsync("http://news.permaviat.ru/add", content);

                Console.WriteLine($"Статус добавления: {response.StatusCode}");
                WriteToLog($"Статус добавления: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Результат: {result}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления: {ex.Message}");
                WriteToLog($"Ошибка добавления: {ex.Message}");
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