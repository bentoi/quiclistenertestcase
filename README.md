This test demonstrates the issue with the QuicListener delaying connection establishment when some clients are slow to validate the remote certificate or if they delay it on purpose (malicious client).

The test case runs a Kestrel server with only HTTP/3 enabled.

Two clients are provided: a Quic client and an HTTP/3 client.

Both exhibit the Kestrel issue: Kestrel can't promptly accept new connections if it's stuck waiting on one of the bogus Quic/Http connection handshake to complete.

For example:
```csharp
~/workspace/quiclistenertestcase/http-client$ dotnet run 5 5
00:00:00.0188153: starting Http client 0-delayed
00:00:00.0188626: starting Http client 1-delayed
00:00:00.0188767: starting Http client 3-delayed
00:00:00.0188621: starting Http client 2-delayed
00:00:00.1523461: starting Http client 5
00:00:00.1528555: starting Http client 6
00:00:00.1531022: starting Http client 7
00:00:00.1533254: starting Http client 8
00:00:00.1535033: starting Http client 4-delayed
00:00:00.1538022: starting Http client 9
00:00:15.3543138: Http client 0-delayed GET failed after 15354.3135 (ms): System.Net.Http.HttpRequestException
00:00:15.3624149: Http client 1-delayed GET failed after 15362.4133 (ms): System.Net.Http.HttpRequestException
00:00:15.4120303: Http client 5 GET returned after 15412.0296 (ms)
00:00:15.4156046: Http client 6 GET returned after 15415.6033 (ms)
00:00:15.4176752: Http client 7 GET returned after 15417.6743 (ms)
00:00:15.4383169: Http client 3-delayed GET failed after 15438.3165 (ms): System.Net.Http.HttpRequestException
00:00:15.4389524: Http client 2-delayed GET failed after 15438.9528 (ms): System.Net.Http.HttpRequestException
00:00:30.6200279: Http client 4-delayed GET failed after 30620.0269 (ms): System.Net.Http.HttpRequestException
00:00:30.6402066: Http client 9 GET returned after 30640.2059 (ms)
00:00:30.6427695: Http client 8 GET returned after 30642.7676 (ms)
```

The HTTP requests from the legit non-delayed clients take up to 30s here.