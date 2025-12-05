using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using MySql.Data.MySqlClient;

namespace HttpNewsPAT_Kantuganov
{
    public class Program
    {
        static Cookie Token;
        static string logFilePath = "debug_trace.log";
        static HttpClient httpClient = new HttpClient();


        static string connectionPath = "Server=127.0.0.1;port=3306;Database=news;Uid=root;Pwd=;";
        static async Task Main(string[] args)
        {

            string siteUrl = "https://www.rbc.ru/?ysclid=miso5rvl29857274833";

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
                string htmlCode = await GetContentAsync(url);
                if (string.IsNullOrEmpty(htmlCode))
                {
                    Console.WriteLine("Не удалось получить контент страницы");
                    return;
                }
                
                var newsData = ParseNewsFromHtml(htmlCode, url);

                bool success = await InsertNewsToDatabase(newsData);
                
                if (success)
                {
                    Console.WriteLine("Новость успешно добавлена в базу данных");
                    WriteToLog("Новость успешно добавлена в базу данных");
                }
                else
                {
                    Console.WriteLine("Не удалось добавить новость в базу данных");
                    WriteToLog("Не удалось добавить новость в базу данных");
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

        public static NewsData ParseNewsFromHtml(string htmlCode, string url)
        {
            var newsData = new NewsData();
            var html = new HtmlDocument();
            html.LoadHtml(htmlCode);
            var document = html.DocumentNode;
        
            var title = document.Descendants("title").FirstOrDefault();
            if (title != null)
            {
                newsData.Name = title.InnerText.Trim();
            }
            else
            {
                var h1 = document.Descendants("h1").FirstOrDefault();
                if (h1 != null)
                {
                    newsData.Name = h1.InnerText.Trim();
                }
                else
                {
                    newsData.Name = "Без названия";
                }
            }
        
            var contentNodes = document.Descendants("article")
                .Concat(document.Descendants("main"))
                .Concat(document.Descendants("div")
                    .Where(d => d.HasClass("content") || d.HasClass("post-content") || d.HasClass("article-content")))
                .FirstOrDefault();
        
            if (contentNodes != null)
            {
                var paragraphs = contentNodes.Descendants("p").Select(p => p.InnerText.Trim());
                newsData.Description = string.Join("\r\n\r\n", paragraphs.Take(5));
        
                if (string.IsNullOrEmpty(newsData.Description))
                {
                    newsData.Description = contentNodes.InnerText.Trim();
                }
            }
            else
            {
                var paragraphs = document.Descendants("p").Select(p => p.InnerText.Trim());
                newsData.Description = string.Join("\r\n\r\n", paragraphs.Take(3));
            }
        
            if (newsData.Description.Length > 10000)
            {
                newsData.Description = newsData.Description.Substring(0, 9997) + "...";
            }
        
            var images = document.Descendants("img")
                .Where(img => !string.IsNullOrEmpty(img.GetAttributeValue("src", "")))
                .ToList();
        
            if (images.Count > 0)
            {
                var mainImage = images.FirstOrDefault(img =>
                    img.HasClass("featured") || img.HasClass("main") || img.HasClass("hero") ||
                    img.HasClass("article-image") || img.HasClass("post-thumbnail"));
        
                if (mainImage != null)
                {
                    newsData.ImageUrl = mainImage.GetAttributeValue("src", "");
                }
                else
                {
                    newsData.ImageUrl = images[0].GetAttributeValue("src", "");
                }
        
                if (!string.IsNullOrEmpty(newsData.ImageUrl) && !newsData.ImageUrl.StartsWith("http"))
                {
                    if (newsData.ImageUrl.StartsWith("/"))
                    {
                        Uri baseUri = new Uri(url);
                        newsData.ImageUrl = new Uri(baseUri, newsData.ImageUrl).ToString();
                    }
                }
            }
        
            if (string.IsNullOrEmpty(newsData.ImageUrl))
            {
                newsData.ImageUrl = "https://via.placeholder.com/300x200?text=No+Image";
            }
        
            WriteToLog($"Парсинг новости: {newsData.Name}, Изображение: {newsData.ImageUrl}, Длина описания: {newsData.Description.Length}");
        
            return newsData;
        }
        
        public static async Task<bool> InsertNewsToDatabase(NewsData news)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionPath))
                {
                    await connection.OpenAsync();
        
                    int maxId = 0;
                    string maxIdQuery = "SELECT MAX(id) FROM news";
                    using (var maxIdCommand = new MySqlCommand(maxIdQuery, connection))
                    {
                        var result = await maxIdCommand.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            maxId = Convert.ToInt32(result);
                        }
                    }
        
                    string insertQuery = @"
                                INSERT INTO news (id, img, name, description) 
                                VALUES (@id, @img, @name, @description)"
                    ;
        
                    using (var command = new MySqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@id", maxId + 1);
                        command.Parameters.AddWithValue("@img", news.ImageUrl);
                        command.Parameters.AddWithValue("@name", news.Name);
                        command.Parameters.AddWithValue("@description", news.Description);
        
                        int rowsAffected = await command.ExecuteNonQueryAsync();
        
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи в БД: {ex.Message}");
                WriteToLog($"Ошибка при записи в БД: {ex.Message}");
                return false;
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
        public class NewsData
        {
            public string ImageUrl { get; set; } = "";
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
        }
    }
}