This test case demonstrates two issues with Quic connection establishment and the Quic listener implementation. This test case uses Kestrel for the server and the client is a simple Quic client.

- first issue: `QuicConnection.ConnectAsync` can block synchronously the thread
- second issue: `QuicListener.AcceptAsync` doesn't accept any new connection establishment as long as the client doesn't validate the server certificate

The output showing the first issue:
```csharp
00:00:00.0008648: starting connection1 connection establishment
00:00:00.1361398: starting connection2 connection establishment
00:00:00.1999513: malicious 15s client certificate validation
<-- second ConnectAsync blocks the thread here, it would otherwise print "waiting for connection1 connection establishment" -->
00:00:15.3641743: waiting for connection1 connection establishment
00:00:15.3822147: normal client certificate validation
00:00:15.4157622: waiting for connection2 connection establishment
```

The output showing the second issue:
```csharp
00:00:00.0007263: starting connection1 connection establishment
00:00:00.1333318: starting connection2 connection establishment
00:00:00.1349389: waiting for connection1 connection establishment
00:00:00.2029323: malicious 15s client certificate validation
00:00:00.2483211: normal client certificate validation
<-- the second ConnectAsync doesn't block here, the 15s wait is from the waiting of the fist connection establishment -->
00:00:15.2076603: waiting for connection2 connection establishment
```
