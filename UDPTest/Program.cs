namespace UDPTest
{
    public class MainAsync
    {
        public static async Task Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "server")
            {
                var server = new Server();
                await server.Run();
            }
            else
            {
                var client = new Client();
                await client.Run();
            }
        }
    }
}
