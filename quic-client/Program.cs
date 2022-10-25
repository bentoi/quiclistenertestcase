using System.Diagnostics;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

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
        ;
        for (; i < int.Parse(args[0]); ++i)
        {
            int client = i;
            tasks.Add(Task.Run(() => ConnectAsync(client, delay: true)));
        }

        for (; i < int.Parse(args[1]); ++i)
        {
            int client = i;
            tasks.Add(Task.Run(() => ConnectAsync(client, delay: false)));
        }

        await Task.WhenAll(tasks);

        async Task ConnectAsync(int i, bool delay)
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
                                    // Malicious sleep to trigger the server AcceptConnectAsync hang.
                                    //Console.WriteLine($"{stopWatch.Elapsed}: client remote certificate validation delayed for 15s");
                                    Thread.Sleep(15000); // 15s sleep
                                }
                                else
                                {
                                    //Console.WriteLine($"{stopWatch.Elapsed}: client remote certificate validation returns immediately");
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
