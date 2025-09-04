using System.Net;
using System.Net.Sockets;

public static class NetworkUtils
{
    public static string GetLocalIPv4()
    {
        string localIP = "Unavailable";
        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4 only
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }
}
