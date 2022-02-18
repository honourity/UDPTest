using Newtonsoft.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace UDPTest
{
    public static class Utility
    {
        static readonly MD5 _md5 = MD5.Create();

        static readonly JsonSerializerSettings jsonSettings = new() {
            NullValueHandling = NullValueHandling.Ignore,
        };

        public static Message FromPayload(UdpReceiveResult data)
        {
            var checksumBytes = data.Buffer.Take(16).ToArray();
            var messageBytes = data.Buffer.Skip(16).ToArray();

            var calculatedChecksumBytes = _md5.ComputeHash(messageBytes);

            //checksum the message string
            if (checksumBytes.SequenceEqual(calculatedChecksumBytes))
            {
                var messageString = Encoding.UTF8.GetString(messageBytes);

                var message = JsonConvert.DeserializeObject<Message>(messageString, jsonSettings);

                if (message == null)
                {
                    message = new Message() { Id = Guid.Empty, Error = "Failed to deserialise message" };
                }

                return message;
            }
            else
            {
                var message = new Message() { Id = Guid.Empty, Error = "Message checksum failed" };
                return message;
            }
        }

        public static byte[] ToPayload(Message message)
        {
            var messageString = JsonConvert.SerializeObject(message, jsonSettings);

            var messageBytes = Encoding.UTF8.GetBytes(messageString);
            var checksumBytes = _md5.ComputeHash(messageBytes);

            var payload = checksumBytes.Concat(messageBytes).ToArray();

            return payload;
        }
    }

    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Error { get; set; }
        public string? TestValue { get; set; }
    }
}
