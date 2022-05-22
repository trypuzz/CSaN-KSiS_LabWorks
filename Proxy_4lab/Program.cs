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
                    TcpClient Receiving = Expectant.AcceptTcpClient();
                    Task ListenTask = new Task(() => Listen(Receiving));
                    ListenTask.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
               
            }
        }

        private static void Listen(TcpClient client)
        {
            NetworkStream browserStream = client.GetStream();
            byte[] buf = new byte[65536];
            while (browserStream.CanRead)
            {
                if (browserStream.DataAvailable)
                {
                    try
                    {
                        int msgLength = browserStream.Read(buf, 0, buf.Length);
                        ProcessRequest(buf, msgLength, browserStream);
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
            Regex headerRegex = new Regex(@"http:\/\/[a-z0-9а-яё\:\.]*");
            MatchCollection headers = headerRegex.Matches(buffer);
            buffer = buffer.Replace(headers[0].Value, "");
            data = Encoding.UTF8.GetBytes(buffer);
            return data;
        }

        private static void ProcessRequest(byte[] buf, int bufLength, NetworkStream browserStream)
        {
            try
            {
                char[] splitCharsArray = {'\r', '\n'};
                string[] buffer = Encoding.UTF8.GetString(buf).Trim().Split(splitCharsArray);
                string host = buffer.FirstOrDefault(x => x.Contains("Host"));
                if (host != null)
                {
                    host = host.Substring(host.IndexOf(":", StringComparison.Ordinal) + 2);
                    string[] requestInfo = host.Trim().Split(new char[] {':'});

                    string hostname = requestInfo[0];
                    var sender = requestInfo.Length == 2
                        ? new TcpClient(hostname, int.Parse(requestInfo[1]))
                        : new TcpClient(hostname, 80);

                    NetworkStream serverStream = sender.GetStream();
                    serverStream.Write(GetPath(buf), 0, bufLength);

                    byte[] answer = new byte[65536];
                    int length = serverStream.Read(answer, 0, answer.Length);

                    string[] head = Encoding.UTF8.GetString(answer).Split(splitCharsArray);
                    string ResponseCode = head[0].Substring(head[0].IndexOf(" ") + 1);
                    Console.WriteLine(host + "  " + ResponseCode);

                    browserStream.Write(answer, 0, length);
                    serverStream.CopyTo(browserStream);

                    serverStream.Close();
                }
            }
            catch
            {
                return;
            }
            finally
            {
                browserStream.Close();
            }
        }

       
    }
}