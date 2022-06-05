using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Chat
{
    class Program
    {
        const int RemotePort = 8001; 
        const int LocalPort = 8001;
        const int TcpPort = 8004;
        const int TcpSendPort = 8005;
        static IPEndPoint? IPEndPointLocal;
        static IPAddress? IPAddressLocal;
        static readonly IPAddress BroadcastIP = IPAddress.Broadcast;
        static string? UserName;
        static StringBuilder History = new();
        const string HistoryMessage = "History";
        const string LeavingMessage = "Leave";
        const int BufferSize = 64;
        static bool InChat = false;
        static Socket? ListenConnectionTcpSocket;
        static UdpClient? ListenConnectionUdpClient;

        enum MessageType : byte
        {
            message = 1,
            name = 2,
            newUser = 3,
            offUser = 4
        }
        public struct ClientInfo
        {
            public string Address;
            public string Name;
            public IPAddress? IpAddress;
            public Socket Handler;
            public ClientInfo(string address, string name, Socket handler)
            {
                Address = address;
                Name = name;
                Handler = handler;
                IpAddress = IPAddress.Parse(address);
            }
        }

        public static List<ClientInfo> AccessibleAddresses = new();
        
        public static string GetName(string ipaddress)
        {
            foreach (ClientInfo addressInfo in AccessibleAddresses)
            {
                if (addressInfo.Address == ipaddress)
                    return addressInfo.Name;
            }
            return null;
        }

        public static void DeleteAddress(string address)
        {
            var addressInfo = AccessibleAddresses.Find(x => x.Address == address);
            addressInfo.Handler.Close();
            AccessibleAddresses.Remove(addressInfo);
        }

        private static void SendUdp(string username)
        {
            IPEndPoint iep = new(IPAddressLocal, 8007);
            UdpClient sendClient = new(iep)
            {
                EnableBroadcast = true
            };
            IPEndPoint endPoint = new(BroadcastIP, RemotePort);

            try
            {
                string message = username;
                byte[] data = new byte[message.Length + 2];
                data = MessageWork.MakeMessage(message, (byte)MessageType.newUser);
                sendClient.Send(data, data.Length, endPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                sendClient.Close();
            }
        }
        public static void ListenConnectionTcp()
        {
            IPEndPoint ipPoint = new(IPAddressLocal, TcpPort);
            Socket listenSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            ListenConnectionTcpSocket = listenSocket; 
            try
            {
                listenSocket.Bind(ipPoint);
                listenSocket.Listen(10);

                while (true)
                {
                    if (!InChat)
                        break;
                    Socket handler = listenSocket.Accept();

                    byte[] data = new byte[BufferSize];
                    handler.Receive(data);
                    
                    byte messageType, messageLength;
                    string message = MessageWork.GetMessage(data, out messageLength, out messageType);
                    AccessibleAddresses.Add(new ClientInfo(((IPEndPoint)handler.RemoteEndPoint).Address.ToString(), message, handler) );
                    
                    Thread ListenTcpConnectionThread = new Thread(() => ListenTcpMessage(handler));
                    ListenTcpConnectionThread.Start();
                }
            }
            catch 
            {
                
            }
            finally 
            {
                listenSocket.Close(); 
            }
        }
        public static void ListenTcpMessage(Socket tcpListener)
        {
            bool cycle = true;
            try
            {
                while (cycle)
                {
                    if (!InChat)
                        break;
                    byte[] data = new byte[BufferSize];
                    StringBuilder sb = new StringBuilder();
                    string remoteIpAddress = ((IPEndPoint)tcpListener.RemoteEndPoint).Address.ToString();

                    try 
                    { 
                        tcpListener.Receive(data); 
                    }
                    catch {
                        sb.Append("Пользователь " + GetName(remoteIpAddress) + " покинул чат\n");
                        History.Append(DateTime.Now.ToShortTimeString() + ":: " + sb.ToString() + "\n");
                        Console.WriteLine(sb.ToString());

                        DeleteAddress(remoteIpAddress);
                        break;
                    }

                    string message = MessageWork.GetMessage(data, out byte length, out byte message_type);
                    
                    switch (message_type)
                    {
                        case (byte)MessageType.message:
                            sb.Append(GetName(remoteIpAddress) + "(" + remoteIpAddress + ") написал: " + message + "\n"); 
                            break;
                        case (byte)MessageType.name:
                            sb.Append("Пользователь " + GetName(remoteIpAddress) + " присоединился к чату\n");
                            break;
                        case (byte)MessageType.offUser:
                            sb.Append("Пользователь "+GetName(remoteIpAddress) + " покинул чат\n");
                            DeleteAddress(remoteIpAddress);
                            cycle = false;
                            break;
                        default:
                            Console.WriteLine(" ");
                            break;
                    }
                    Console.WriteLine(sb.ToString());
                    History.Append(DateTime.Now.ToShortTimeString() + ":: " + sb.ToString() + "\n");
                }
            }
            catch (Exception ex)
            {
                if(InChat)
                  Console.WriteLine(ex.ToString());                
            }
            finally
            {
                tcpListener.Close();
            }
        }
        private static void ListenUdp()
        {
            
            IPEndPointLocal = new IPEndPoint(IPAddressLocal, LocalPort);
            UdpClient receiveСlient = new UdpClient(IPEndPointLocal);
            ListenConnectionUdpClient = receiveСlient;

            IPEndPoint? remoteIp = null;
            try
            {
                while (true)
                {
                    if (!InChat)
                        break;
                    byte[] data = receiveСlient.Receive(ref remoteIp);
                    string message = MessageWork.GetMessage(data, out byte length, out byte message_type);

                    StringBuilder sb = new StringBuilder("");
                    sb.Append("Пользователь " + message + " теперь в чате!\n");
                    Console.WriteLine(sb.ToString());
                    History.Append(DateTime.Now.ToShortTimeString() + ":: " + sb.ToString() + "\n");

                    Socket TCPSender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    byte[] dataSend;
                    
                    dataSend = MessageWork.MakeMessage(UserName, 2);
                   
                    IPEndPoint remoteiep = new IPEndPoint(remoteIp.Address, TcpPort);

                    TCPSender.Connect(remoteiep);
                    TCPSender.Send(dataSend);

                    AccessibleAddresses.Add(new ClientInfo(remoteIp.Address.ToString(), message, TCPSender));
                    Thread ListenTcpThread = new Thread(() => ListenTcpMessage(TCPSender));
                    ListenTcpThread.Start();
                }
            }
            catch
            {

            }
            finally
            {
                receiveСlient.Close();
            }
        }

        static void Main() 
        {
            Console.WriteLine("Добро пожаловать в чат! \nВведите свой IP:");
            bool good = false;
            while (good == false)
            {
                string? s = Console.ReadLine();
                if (s != null)
                {
                    good = IPAddress.TryParse(s, out IPAddressLocal);
                    if(!good)
                        Console.WriteLine("Uncorrect IP\n\nВведите свой IP:");   
                }
            }

            Console.Write("Введите свое имя:");
            UserName = Console.ReadLine();
            InChat = true;
            Console.WriteLine("Вы в чате.");
            History.Append(DateTime.Now.ToShortTimeString() + ":: " + "Я в чате" + "\n");
            Console.WriteLine("Write \"" + HistoryMessage + "\" to see history, \"" + LeavingMessage + "\" to leave");
            if (UserName != null)
                SendUdp(UserName);
            try
            {
                Thread ListenUdpThread = new(new ThreadStart(ListenUdp));
                ListenUdpThread.Start();

                Thread ListenConnectionTcpThread = new Thread(ListenConnectionTcp);
                ListenConnectionTcpThread.Start();

                string message;
                MessageType message_type = MessageType.message;

                while (message_type != MessageType.offUser)
                {
                    message = Console.ReadLine();
                    while (message.Length > BufferSize / 2 - 1)
                    {
                        Console.WriteLine("Слишком большое сообщение. Сократите");
                        message = Console.ReadLine();
                    }

                    if (message == HistoryMessage)
                    {
                        Console.WriteLine(History.ToString());
                    }
                    else
                    {
                        if (message == LeavingMessage)
                            message_type = MessageType.offUser;
                        else
                            message_type = MessageType.message;

                        foreach (var item in AccessibleAddresses)
                        {
                            item.Handler.Send(MessageWork.MakeMessage(message, (byte)message_type));
                        }

                        if (message_type == MessageType.offUser)
                        {
                            foreach (var item in AccessibleAddresses)
                            {
                                item.Handler.Shutdown(SocketShutdown.Both);
                                item.Handler.Close();
                            }
                            
                            InChat = false;
                            ListenConnectionTcpSocket.Close();
                            ListenConnectionUdpClient.Close();
                            Console.WriteLine("Вы покинули чат");
                            string s = Console.ReadLine();
                            if (s == HistoryMessage)
                                Console.WriteLine(History.ToString());
                        }
                        
                        History.Append(DateTime.Now.ToShortTimeString() + ":: " + message_type.ToString() + ": " + message + "\n");
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
    }
}
