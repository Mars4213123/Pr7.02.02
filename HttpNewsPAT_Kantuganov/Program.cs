

using System;
using System.IO;
using System.Net;

namespace HttpNewsPAT_Kantuganov
{
    internal class Program
    {
        static void Main(string[] args)
        {
            WebRequest request = WebRequest.Create("https://59.ru/");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Console.WriteLine(response.StatusDescription);
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            Console.WriteLine(responseFromServer);
            reader.Close();
            dataStream.Close();
            response.Close();
            Console.Read();

        }
    }
}
