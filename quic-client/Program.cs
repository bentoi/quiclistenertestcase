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

        int i = 0;

        // Start clients that delay the remote certificate validation.
        if (!int.TryParse(args[0], out int bogusClientCount))
        {
            bogusClientCount = 1;
        }
        for (; i < bogusClientCount; ++i)
        {
            int client = i;
            tasks.Add(Task.Run(() => RunQuicClientAsync(client, delay: true)));
        }

        // Start normal clients.
        if (!int.TryParse(args[1], out int normalClientCount))
        {
            normalClientCount = 1;
        }
        for (; i < normalClientCount; ++i)
        {
            int client = i;
            tasks.Add(Task.Run(() => RunHttpClientAsync(client, delay: false)));
        }

        await Task.WhenAll(tasks);

        async Task RunHttpClientAsync(int i, bool delay)
        {
            Console.WriteLine($"{stopWatch.Elapsed}: starting Http client {i}{(delay ? "-delayed" : "")}");
            var socketsHandler = new SocketsHttpHandler()
                {
                    SslOptions = new SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                            {
                                if (delay)
                                {
                                    Thread.Sleep(15000); // 15s sleep
                                }
                                return true;
                            }
                    }
                };

            using var httpClient = new HttpClient(socketsHandler)
                {
                    BaseAddress = new Uri("https://localhost:5001"),
                    DefaultRequestVersion = HttpVersion.Version30,
                    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
                };

            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync("/");
                Console.WriteLine($"{stopWatch.Elapsed}: Http client {i}{(delay ? "-delayed" : "")} GET returned after {(stopWatch.Elapsed - start).TotalMilliseconds} (ms)");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{stopWatch.Elapsed}: Http client {i}{(delay ? "-delayed" : "")} GET failed after {(stopWatch.Elapsed - start).TotalMilliseconds} (ms): {exception.GetType()}");
            }
        }

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
