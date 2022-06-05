using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;

namespace tracert
{
    public class tracert
    {
        Stopwatch stopWatch;
        private byte SequenceNumber = 0;
        private int ttl = 0;
        private static int maxHops = 30;
        private static int maxWaitingTime = 1000;
        private IPHostEntry hostDNS;
        private Boolean isNodeReached = false;

        public tracert(string hostStr)
        {
            int err=0;
            try
            {
                hostDNS = Dns.GetHostEntry(hostStr);
                Console.WriteLine($"Трассировка маршрута к {hostDNS.HostName} [{hostDNS.AddressList[0]}] \nс максимальным числом прыжков {maxHops}\n");
            }
            catch (Exception e)
            {
                ++err;
                Console.WriteLine("Неверное системное имя узла ");
                return;
            }
        }

        public void trace()
        {
           
            IPEndPoint endPoint = new IPEndPoint(hostDNS.AddressList[0],0);
            EndPoint remoteEndPoint = endPoint;
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, maxWaitingTime);
            
            byte[] package = new byte[72];
            ICMP icmp = new ICMP(package);
                      
            byte[] receivedPackage = new byte[256];
            
            while (ttl <= maxHops)
            {
                icmp.SequenceNumber(package, SequenceNumber);
                icmp.CheckSum(package);

                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, ttl++);

                Console.Write($"{ttl} \t");
                int error = 0;
                for (int i = 0; i < 3; i++)
                {
                    
                    isNodeReached = false;
                    
                    try
                    {
                        stopWatch = Stopwatch.StartNew();
                        
                        socket.SendTo(package, endPoint);
                        socket.ReceiveFrom(receivedPackage, ref remoteEndPoint);

                        stopWatch.Stop();

                        string str = remoteEndPoint.ToString();
                        String word = str.Substring(0, str.IndexOf(':'));

                        if (ttl != 1)
                        {
                            Console.Write($"{(Int32)stopWatch.ElapsedMilliseconds}ms\t ");
                            isNodeReached = true;
                        }
                        else
                        {
                            if (i == 0)
                            {
                                Console.Write($"First Point\t         {word}");
                                isNodeReached = true;
                            }
                        }
                    }
                    catch (SocketException e)
                    {
                        error++;
                        Console.Write($" * \t");  
                        
                        if (error == 3)
                        {
                            Console.Write(" Превышен интервал ожидания для запроса");
                            break;
                        }
                    }

                    icmp.SequenceNumber(package, ++SequenceNumber);
                    icmp.CheckSum(package);                    
                }

                if (receivedPackage[20] == 0)
                {
                    string str = remoteEndPoint.ToString();
                    String word = str.Substring(0, str.IndexOf(':'));

                    Console.Write($"{word}");

                    Console.WriteLine("\n\nТрассировка завершена\n");
                    Console.ReadLine();
                    return;
                }

                if ((ttl != 1) && (error != 3))
                {
                    string str = remoteEndPoint.ToString();
                    String word = str.Substring(0, str.IndexOf(':'));

                    Console.Write($"{word}");
                }
                Console.WriteLine();                 
            }
            
        }
    }
}