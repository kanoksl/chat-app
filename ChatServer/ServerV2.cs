using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using ChatClassLibrary;

namespace ChatServer
{
    class ServerV2
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing server V2...");

            //IPAddress localAddress = ServerProgram.SelectLocalIPAddress();
            IPAddress localAddress = IPAddress.Any;

            MessageServer server = new MessageServer(localAddress);
            server.StartListening();
            
        }
    }
}
