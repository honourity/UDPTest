using System.Collections.Concurrent;
using System.Net.Sockets;

namespace UDPTest
{
    public static class Receiver
    {
        public static ConcurrentQueue<UdpReceiveResult> Buffer = new();

        public static async Task Receive(UdpClient udp)
        {
            while (true)
            {
                var received = await udp.ReceiveAsync();
                Buffer.Enqueue(received);
            }
        }
    }
}
