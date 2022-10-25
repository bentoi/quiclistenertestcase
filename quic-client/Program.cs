using System.Diagnostics;
using System.Net;
using System.Net.Quic;
using System.Net.Security;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
internal class Program
{
    private static async Task Main(string[] args)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        TimeSpan start = stopWatch.Elapsed;
        var tasks = new List<Task>();

        // Start clients that delay the remote certificate validation.
        if (args.Length < 1 || !int.TryParse(args[0], out int bogusClientCount))
        {
            bogusClientCount = 2;
        }
        for (int i = 0; i < bogusClientCount; ++i)
        {
            int client = i;
            tasks.Add(Task.Run(() => RunQuicClientAsync(client, delay: true)));
        }

        // Start normal clients.
        if (args.Length < 2 || !int.TryParse(args[1], out int normalClientCount))
        {
            normalClientCount = 2;
        }
        for (int i = 0; i < normalClientCount; ++i)
        {
            int client = bogusClientCount + i;
            tasks.Add(Task.Run(() => RunQuicClientAsync(client, delay: false)));
        }

        await Task.WhenAll(tasks);

        async Task RunQuicClientAsync(int i, bool delay)
        {
            try
            {
                // Start the first connection establishment.
                Console.WriteLine($"{stopWatch.Elapsed}: starting connection {i} connection establishment {(delay ? "(delayed)" : "")}");

                await using var connection = await QuicConnection.ConnectAsync(new QuicClientConnectionOptions
                {
                    DefaultCloseErrorCode = 0,
                    DefaultStreamErrorCode = 0,
                    RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 5001),
                    ClientAuthenticationOptions = new SslClientAuthenticationOptions
                    {
                        ApplicationProtocols = new List<SslApplicationProtocol> { new SslApplicationProtocol("h3") },
                        RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                            {
                                if (delay)
                                {
                                    Thread.Sleep(15000); // 15s sleep
                                }
                                return true;
                            }
                    }
                });

                Console.WriteLine($"{stopWatch.Elapsed}: connection {i} connection establishment succeeds after {(stopWatch.Elapsed - start).TotalMilliseconds} (ms) {(delay ? "(delayed)" : "")}");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{stopWatch.Elapsed}: connection {i} connection establishment failed:\n{exception}");
                throw;
            }
        }
    }
}
