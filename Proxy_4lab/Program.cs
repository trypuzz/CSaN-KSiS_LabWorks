using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace laba4ProxyServer
{
    static class Program
    {
        static void Main()
        {
            try
            {
                TcpListener Expectant = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080);
                Expectant.Start();
                
                while (true)
                {
                    TcpClient received = Expectant.AcceptTcpClient();
                    Task listening = new Task(() => Listen(received));
                    listening.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
               
            }
        }

        private static void Listen(TcpClient client)
        {
            NetworkStream browser = client.GetStream();
            byte[] buf = new byte[65536];
            while (browser.CanRead)
            {
                if (browser.DataAvailable)
                {
                    try
                    {
                        int message_length = browser.Read(buf, 0, buf.Length);
                        RequestFunc(buf, message_length, browser);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error occured: " + ex.Message);
                        return;
                    }
                }
            }

            client.Close();
        }

        private static byte[] GetPath(byte[] data)
        {
            string buffer = Encoding.UTF8.GetString(data);
            Regex regex_header = new Regex(@"http:\/\/[a-z0-9а-яё\:\.]*");
            MatchCollection headers = regex_header.Matches(buffer);
            buffer = buffer.Replace(headers[0].Value, "");
            data = Encoding.UTF8.GetBytes(buffer);
            return data;
        }

        private static void RequestFunc(byte[] buf, int bufLength, NetworkStream browser)
        {
            try
            {
                char[] IFS = {'\r', '\n'};
                string[] buffer = Encoding.UTF8.GetString(buf).Trim().Split(IFS);
                string host = buffer.FirstOrDefault(x => x.Contains("Host"));
                if (host != null)
                {
                    host = host.Substring(host.IndexOf(":", StringComparison.Ordinal) + 2);
                    string[] request_information = host.Trim().Split(new char[] {':'});

                    string hostname = request_information[0];
                    var sender = request_information.Length == 2
                        ? new TcpClient(hostname, int.Parse(request_information[1]))
                        : new TcpClient(hostname, 80);

                    NetworkStream server = sender.GetStream();
                    server.Write(GetPath(buf), 0, bufLength);

                    byte[] answer = new byte[65536];
                    int length = server.Read(answer, 0, answer.Length);

                    string[] head = Encoding.UTF8.GetString(answer).Split(IFS);
                    string codeOfAnswer = head[0].Substring(head[0].IndexOf(" ") + 1);
                    Console.WriteLine(host + "  " + codeOfAnswer);

                    browser.Write(answer, 0, length);
                    server.CopyTo(browser);

                    server.Close();
                }
            }
            catch
            {
                return;
            }
            finally
            {
                browser.Close();
            }
        }

       
    }
}