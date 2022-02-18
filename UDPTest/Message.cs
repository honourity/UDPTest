namespace UDPTest
{
    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Error { get; set; }
        public string? TestValue { get; set; }
    }
}
