using System;
using System.Net;
using System.Net.Sockets;

namespace tracert
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Write("tracert ");
            String hostStr = Console.ReadLine();
            tracert tracert = new tracert(hostStr);
            tracert.trace();
        }
    }
}