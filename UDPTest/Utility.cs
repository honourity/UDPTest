using System.Text;

namespace UDPTest
{
    public static class Utility
    {
        public static string FromByteArray(byte[] data)
        {
            return Encoding.ASCII.GetString(data);
        }

        public static byte[] ToByteArray(string data)
        {
            return Encoding.ASCII.GetBytes(data);
        }
    }
}
