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
        }

        public async Task Run()
        {
            Console.WriteLine("Client started");

            _ = Receiver.Receive(_udp);

            //hit server to register this client
            var message = new Message() { TestValue = string.Empty };
            await SendMessage(message);

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
                        var message = new Message() { TestValue = input };
                        SendMessage(message).Wait();
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
                    var message = Utility.FromPayload(payload);

                    if (!string.IsNullOrEmpty(message.Error))
                    {
                        Console.WriteLine(message.Error);
                    }

                    //only accept messages from the server
                    if (payload.RemoteEndPoint.Port == Config.SERVER_PORT)
                    {
                        if (_pendingMessages.ContainsKey(message.Id))
                        {
                            var removed = _pendingMessages.Remove(message.Id, out var removedMessage);

                            if (removed)
                            {
                                Console.WriteLine("Acknowledged " + message.Id.ToString().Split('-').Last());
                            }
                            else
                            {
                                Console.WriteLine("Failed to remove acknowledged message from pending message dictionary");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Received " + message.Id.ToString().Split('-').Last());
                        }
                    }
                    else
                    {
                        Console.WriteLine("Received a message but remote endpoint port isnt server");
                    }
                }
            }

            //resend messages which have not been acknowledged beyond a certain timeframe
            var pendingRetryMessages = _pendingMessages.Where(m => (DateTime.Now - m.Value.DateSent) > TimeSpan.FromMilliseconds(Config.TIME_BEFORE_RETRY_MS));
            foreach (var pendingRetryMessage in pendingRetryMessages)
            {
                if (pendingRetryMessage.Value.Message != null)
                {
                    Console.WriteLine("Resending: " + pendingRetryMessage.Value.Message.Id.ToString().Split('-').Last());

                    //resetting the timer on each message which is being re-sent
                    pendingRetryMessage.Value.DateSent = DateTime.Now;

                    await SendMessage(pendingRetryMessage.Value.Message);
                }
            }
        }

        private async Task SendMessage(Message message)
        {
            if (message != null)
            {
                _pendingMessages.TryAdd(message.Id, new PendingMessage() { DateSent = DateTime.Now, Message = message });
                await Sender.Send(_udp, Config.SERVER_HOSTNAME, Config.SERVER_PORT, message);
            }
        }
    }

    public class PendingMessage
    {
        public DateTime DateSent { get; set; }
        public Message? Message { get; set; }
    }
}
