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
        static string? Key;
        const string KeyConst = "5";

        static StringBuilder History = new();
        const string HistoryMessage = "History";
        const string LeavingMessage = "Leave";
        const int BufferSize = 32;
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
            public string Key;
            public ClientInfo(string address, string name, Socket handler, string key)
            {
                Address = address;
                Name = name;
                Handler = handler;
                IpAddress = IPAddress.Parse(address);
                Key = key;
                //ВВОДИТЬ Число (ключ) для шифровки и расшифровки сообщений если оба ввели правильный ключ, то показываем норм сообщения
            }
        }

        //info for matching names and addresses
        public static List<ClientInfo> AccessibleAddresses = new();
        
        //find name by address
        public static string GetName(string ipaddress)
        {
            foreach (ClientInfo addressInfo in AccessibleAddresses)
            {
                if (addressInfo.Address == ipaddress)
                    return addressInfo.Name;
            }

            return null;
        }

        public static string GetKey(string ipaddress)
        {
            foreach (ClientInfo Info in AccessibleAddresses)
            {
                if (Info.Address == ipaddress)
                    return Info.Key;
            }
            return null;
        }

        //delete member by address
        public static void DeleteAddress(string address)
        {
            var addressInfo = AccessibleAddresses.Find(x => x.Address == address);
            addressInfo.Handler.Close();
            AccessibleAddresses.Remove(addressInfo);
        }

        //onlu for launch, saying others we're here
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

        //only for launch, we wait someone to connect to us after UDP broadcasting
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
                    AccessibleAddresses.Add(new ClientInfo(((IPEndPoint)handler.RemoteEndPoint).Address.ToString(), message, handler, Key) );
                    
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

        //listening to messages
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

                    try { tcpListener.Receive(data); }////////////////////////////////////////////////////////////////////////////
                    catch {
                        sb.Append(GetName(remoteIpAddress) + " вышел из чата!!\n");
                        History.Append(DateTime.Now.ToShortTimeString() + ":: " + sb.ToString() + "\n");
                        Console.WriteLine(sb.ToString());

                        DeleteAddress(remoteIpAddress);
                        break;
                    }

                    string message = MessageWork.GetMessage(data, out byte length, out byte message_type);

                    string keymet = GetKey(remoteIpAddress);
                    message = RailEncrypt(message, keymet);

                    switch (message_type)
                    {
                        case (byte)MessageType.message:
                            if (keymet != KeyConst)
                            {
                                sb.Append(GetName(remoteIpAddress) + "(" + remoteIpAddress + ") пишет: " + message + "\n");
                            }
                            else
                            {
                                message = RailDecrypt(message, keymet);
                                sb.Append(GetName(remoteIpAddress) + "(" + remoteIpAddress + ") пишет: " + message + "\n");
                            }
                            break;
                        case (byte)MessageType.name:
                            sb.Append(GetName(remoteIpAddress) + " теперь в чате!\n");
                            break;
                        case (byte)MessageType.offUser:
                            sb.Append(GetName(remoteIpAddress) + " вышел из чата!!\n");
                            DeleteAddress(remoteIpAddress);
                            cycle = false;
                            break;
                        default:
                            Console.WriteLine("unknown message type!");
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

        //looking for new users
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
                    sb.Append(message + " теперь в чате!\n");
                    Console.WriteLine(sb.ToString());
                    History.Append(DateTime.Now.ToShortTimeString() + ":: " + sb.ToString() + "\n");

                    Socket TCPSender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    byte[] dataSend;
                    dataSend = MessageWork.MakeMessage(UserName, 2);
                   
                    IPEndPoint remoteiep = new IPEndPoint(remoteIp.Address, TcpPort);
                    TCPSender.Connect(remoteiep);
                    TCPSender.Send(dataSend);
                    AccessibleAddresses.Add(new ClientInfo(remoteIp.Address.ToString(), message, TCPSender, Key));
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

        private static string RailEncrypt(string plaintext, string inpkey)
        {
            StringBuilder result = new StringBuilder();
            var lines = new List<StringBuilder>();

            var parsres = Int16.TryParse(inpkey, out var key);
            //if (!parsres) throw new ArgumentNullException(nameof(lines));

            //if (lines == null) throw new ArgumentNullException(nameof(lines));
            for (int i = 0; i < key; i++)
                lines.Add(new StringBuilder());
            int currentLine = 0;
            int direction = 1;

            foreach (var t in plaintext)
            {
                lines[currentLine].Append(t);

                if (currentLine == 0)
                    direction = 1;
                else if (currentLine == key - 1)
                    direction = -1;

                currentLine += direction;
            }


            for (int i = 0; i < key; i++)
                result.Append(lines[i]);

            return result.ToString();//.ToUpper();
        }

        private static string RailDecrypt(string ciphertext, string inpkey)
        {
            StringBuilder result = new StringBuilder();
            var parsres = Int32.TryParse(inpkey, out var key);
            var lines = new List<StringBuilder>();
            if (!parsres) throw new ArgumentNullException(nameof(lines));

            for (int i = 0; i < key; i++)
                lines.Add(new StringBuilder());

            int[] linesLenght = Enumerable.Repeat(0, key).ToArray();

            int currentLine = 0;
            int direction = 1;

            for (int i = 0; i < ciphertext.Length; i++)
            {
                linesLenght[currentLine]++;

                if (currentLine == 0)
                    direction = 1;
                else if (currentLine == key - 1)
                    direction = -1;

                currentLine += direction;
            }

            int currentChar = 0;

            for (int line = 0; line < key; line++)
            {
                for (int c = 0; c < linesLenght[line]; c++)
                {
                    lines[line].Append(ciphertext[currentChar]);
                    currentChar++;
                }
            }

            currentLine = 0;
            direction = 1;

            int[] currentReadLine = Enumerable.Repeat(0, key).ToArray();

            for (int i = 0; i < ciphertext.Length; i++)
            {
                result.Append(lines[currentLine][currentReadLine[currentLine]]);
                currentReadLine[currentLine]++;

                if (currentLine == 0)
                    direction = 1;
                else if (currentLine == key - 1)
                    direction = -1;

                currentLine += direction;
            }

            return result.ToString();//.ToUpper();
        }

        static void Main() 
        {
            Console.WriteLine("Добро пожаловать в чат! Введите свой ip:");
            bool good = false;
            while (good == false)
            {
                string? s = Console.ReadLine();
                if (s != null)
                {
                    good = IPAddress.TryParse(s, out IPAddressLocal);
                    if(!good)
                        Console.WriteLine("!НОРМАЛЬНЫЙ IP!");   
                }
            }

            Console.Write("Введите свое имя:");
            UserName = Console.ReadLine();
            InChat = true;

            Console.Write("Введите КЛЮЧ:");
            Key = Console.ReadLine();
            int intKey = Convert.ToInt32(Key);

            good = false;
            while (good == false)
            {
                if (intKey < 2)
                {
                    Console.WriteLine("Некорректный Ключ. Заново\n");
                    Key = Console.ReadLine();
                    intKey = Convert.ToInt32(Key);
                }
                else good = true;

            }



            Console.WriteLine("Вы в чате.");
            History.Append(DateTime.Now.ToShortTimeString() + ":: " + "Я в чате." + "\n");
            Console.WriteLine("write \"" + HistoryMessage + "\" to see history, \"" + LeavingMessage + "\" to leave");
            if (intKey > 1) GetKey(Key);
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
                        Console.WriteLine("too long, less words, please");
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
                            message = RailEncrypt(message, item.Key);
                            if (item.Key != KeyConst)
                            {
                                item.Handler.Send(MessageWork.MakeMessage(message, (byte)message_type));
                            }
                            else
                            {
                                message = RailDecrypt(message, item.Key);
                                item.Handler.Send(MessageWork.MakeMessage(message, (byte)message_type));
                            }
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
                            Console.WriteLine("Вы больше не в чате!");
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
