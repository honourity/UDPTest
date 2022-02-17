using System.Net;
using System.Net.Sockets;

namespace UDPTest
{
    public class Server
    {
        readonly UdpClient _udp = new(Config.SERVER_PORT);
        readonly List<int> _clients = new();

        public Server()
        {
            _ = Receiver.Receive(_udp);
        }

        public async Task Run()
        {
            Console.WriteLine("Server started");

            Console.WriteLine("Running on port: " + (_udp.Client.LocalEndPoint as IPEndPoint)?.Port.ToString());

            while (true)
            {
                await Iterate();
            }
        }

        private async Task Iterate()
        {
            await Task.Delay(Config.SLEEP_TIME_MS);

            //check for buffer and print until there isnt any
            while (Receiver.Buffer.Any())
            {
                var dequeue = Receiver.Buffer.TryDequeue(out var payload);
                if (dequeue)
                {
                    var messageRaw = Utility.FromByteArray(payload.Buffer);
                    var messageParts = messageRaw.Split('|');
                    var messageId = Guid.Parse(messageParts[0]);
                    var message = messageParts[1];

                    var port = payload.RemoteEndPoint.Port;

                    Console.WriteLine("Received client: " + port + " id: " + messageId);

                    //if this is the first time a message has been received by this client, add their port to the bag
                    if (!_clients.Contains(port) && port != Config.SERVER_PORT)
                    {
                        _clients.Add(port);
                        Console.WriteLine("Registered new client: " + port);
                    }

                    //broadcast message to all clients (including original sender, as an acknowledgement)
                    // however original sender gets original messageId, other clients get new id's for future acknowledgements
                    foreach (var client in _clients)
                    {
                        var id = Guid.NewGuid();
                        if (client == port)
                        {
                            id = messageId;
                        }

                        await Sender.Send(_udp, Config.SERVER_HOSTNAME, client, message, id);

                        Console.WriteLine("Sent client: " + client + " id: " + id);
                    }
                }
            }
        }
    }
}
