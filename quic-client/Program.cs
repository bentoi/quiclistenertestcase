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

        // Start the first connection establishment.
        Console.WriteLine($"{stopWatch.Elapsed}: starting connection1 connection establishment");
        Task clientConnection1Task = ConnectAsync(malicious: true);

        await Task.Delay(50);

        // Start the second connection establishment. Note that depending on timeout and the delay above, this second
        // ConnectAsync can synchronously block which is obviously wrong. QuicConnection.ConnectAsync should never
        // block.
        Console.WriteLine($"{stopWatch.Elapsed}: starting connection2 connection establishment");
        Task clientConnection2Task = ConnectAsync(malicious: false);


        Console.WriteLine($"{stopWatch.Elapsed}: waiting for connection1 connection establishment");
        try
        {
            await clientConnection1Task;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"{stopWatch.Elapsed}: connection1 connection establishment failed:\n{exception}");
        }

        Console.WriteLine($"{stopWatch.Elapsed}: waiting for connection2 connection establishment");
        try
        {
            await clientConnection2Task;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"{stopWatch.Elapsed}: connection1 connection establishment failed:\n{exception}");
        }

        async Task ConnectAsync(bool malicious)
        {
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
                                Console.WriteLine($"{stopWatch.Elapsed}: malicious 15s client certificate validation");
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

            QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
            await stream.WriteAsync(new byte[1024]);
            await stream.DisposeAsync();
        }
    }
}
