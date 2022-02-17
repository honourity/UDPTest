using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace UDPTest
{
    public class Client
    {
        readonly UdpClient _udp;
        readonly int _port;
        readonly ConcurrentDictionary<Guid, PendingMessage> _pendingMessages = new();

        public Client()
        {
            _udp = new(0);

            _udp.EnableBroadcast = false;
            _udp.DontFragment = true;

            _port = (_udp.Client.LocalEndPoint as IPEndPoint)?.Port ?? 0;
            _ = Receiver.Receive(_udp);
        }

        public async Task Run()
        {
            Console.WriteLine("Client started");

            //hit server to register this client
            await SendMessage(".", Guid.NewGuid());

            Console.WriteLine("Starting input listener");
            _ = UserInput();

            Console.WriteLine("Running on port: " + _port);

            while (true)
            {
                await Iterate();
            }
        }

        private async Task UserInput()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    var input = Console.ReadLine();
                    
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        SendMessage(input, Guid.NewGuid()).Wait();
                    }
                }
            });
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

                    //only accept messages from the server
                    if (payload.RemoteEndPoint.Port == Config.SERVER_PORT)
                    {
                        if (_pendingMessages.ContainsKey(messageId))
                        {
                            var removed = _pendingMessages.Remove(messageId, out var removedMessage);

                            if (removed)
                            {
                                Console.WriteLine("Acknowledged " + messageId);
                            }
                            else
                            {
                                Console.WriteLine("Failed to remove acknowledged message from pending message dictionary");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Received " + messageId);
                        }
                    }
                }
            }

            //resend messages which have not been acknowledged beyond a certain timeframe
            var messagesToRetry = _pendingMessages.Where(m => (DateTime.Now - m.Value.DateSent) > TimeSpan.FromMilliseconds(Config.TIME_BEFORE_RETRY_MS));
            foreach (var message in messagesToRetry)
            {
                Console.WriteLine("Resending: " + message.Key);

                //resetting the timer on each message which is being re-sent
                message.Value.DateSent = DateTime.Now;

                await SendMessage(message.Value.Message, message.Key);
            }
        }

        private async Task SendMessage(string message, Guid messageId)
        {
            _pendingMessages.TryAdd(messageId, new PendingMessage() { DateSent = DateTime.Now, Message = message });
            await Sender.Send(_udp, Config.SERVER_HOSTNAME, Config.SERVER_PORT, message, messageId);
        }
    }

    public class PendingMessage
    {
        public DateTime DateSent { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
