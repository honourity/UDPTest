using System.Net.Sockets;

namespace UDPTest
{
    public static class Sender
    {
        public static async Task Send(UdpClient udp, string hostname, int destinationPort, Message message)
        {
            var data = Utility.ToPayload(message);

            var result = await udp.SendAsync(data, hostname, destinationPort);

            if (result != data.Length)
            {
                throw new Exception("Message was only partially sent. Message: " + message);
            }
        }
    }
}
