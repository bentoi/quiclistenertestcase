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


        Task task1 = Task.Run(() => ConnectAsync(1, malicious: true));

        await Task.Delay(1000);

        Task task2 = Task.Run(() => ConnectAsync(2, malicious: false));
        Task task3 = Task.Run(() => ConnectAsync(3, malicious: false));

        await Task.WhenAll(task1, task2, task3);

        async Task ConnectAsync(int i, bool malicious)
        {
            try
            {
                // Start the first connection establishment.
                Console.WriteLine($"{stopWatch.Elapsed}: starting connection {i} connection establishment");

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
                                if (malicious)
                                {
                                    // Malicious sleep to trigger the server AcceptConnectAsync hang.
                                    Console.WriteLine($"{stopWatch.Elapsed}: 15s client certificate validation");
                                    Thread.Sleep(15000); // 15s sleep
                                }
                                else
                                {
                                    Console.WriteLine($"{stopWatch.Elapsed}: normal client certificate validation");
                                }
                                return true;
                            }
                    }
                });
                Console.WriteLine($"{stopWatch.Elapsed}: connection {i} connection establishment success");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{stopWatch.Elapsed}: connection {i} connection establishment failed:\n{exception}");
                throw;
            }
        }
    }
}
