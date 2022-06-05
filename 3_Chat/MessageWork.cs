using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat
{
    internal class MessageWork
    {
        public static byte[] MakeMessage(string message, byte message_type)
        {
            byte[] data = new byte[message.Length * 2 + 2];
            
            Encoding.Unicode.GetBytes(message, 0, message.Length, data, 2);
            data[0] = message_type;
            data[1] = (byte)message.Length;
            return data;
        }

        public static string GetMessage(byte[] data, out byte length, out byte message_type)
        {
            string message = Encoding.Unicode.GetString(data, 2, data[1] * 2);
            message_type = data[0];
            length = data[1];            
            return message;
        }
    }
}
