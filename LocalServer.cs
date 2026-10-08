using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace ClaudeSkin;
public sealed class LocalServer : IDisposable
{
    readonly TcpListener listener = new(IPAddress.Loopback,9164);
    readonly CancellationTokenSource stop = new();
    readonly SemaphoreSlim clients = new(4);
    readonly ThemeService service;
    public LocalServer(ThemeService service) { this.service=service;listener.Start(8);_=Run(); }
    async Task Run()
    {
        try {
            while(!stop.IsCancellationRequested) {
                var client=await listener.AcceptTcpClientAsync(stop.Token);
                if(!clients.Wait(0)) {client.Dispose();continue;}
                _=Handle(client);
            }
        } catch(Exception e) when(e is OperationCanceledException or SocketException or ObjectDisposedException) { }
    }
    async Task Handle(TcpClient client)
    {
        using(client)
        using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop.Token)) {
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            try {
                var stream=client.GetStream();var header=new List<byte>();var buffer=new byte[1];
                while(header.Count<8192) {
                    if(await stream.ReadAsync(buffer,timeout.Token)==0)return;
                    header.Add(buffer[0]);
                    var n=header.Count;
                    if(n>=4&&header[n-4]==13&&header[n-3]==10&&header[n-2]==13&&header[n-1]==10)break;
                }
                var lines=Encoding.ASCII.GetString(header.ToArray()).Split("\r\n");
                var first=lines[0].Split(' ');var headers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);bool valid=true;
                foreach(var line in lines.Skip(1).Where(l=>l.Length>0)) {
                    var split=line.IndexOf(':');
                    if(split<=0||!headers.TryAdd(line[..split].Trim(),line[(split+1)..].Trim())) {valid=false;break;}
                }
                if(headers.TryGetValue("Content-Length",out var length)) {
                    if(!int.TryParse(length,out var count)||count<0||count>8192)valid=false;
                    else for(int i=0;i<count;i++)if(await stream.ReadAsync(buffer,timeout.Token)==0)return;
                }
                if(headers.ContainsKey("Transfer-Encoding"))valid=false;
                headers.TryGetValue("Origin",out var origin);
                var allowed=valid&&header.Count<8192&&first.Length==3&&first[0]=="GET"&&headers.GetValueOrDefault("Host")=="127.0.0.1:9164"&&(origin is null||Regex.IsMatch(origin,"^chrome-extension://[a-p]{32}$"));
                var status=allowed ? 200 : 403;string body="{}";
                if(allowed) {
                    switch(first[1]) {
                        case "/state": body=JsonSerializer.Serialize(new {app="ClaudeSkin",revision=service.Revision},ThemeService.Json);break;
                        case "/skin": body=service.Payload();break;
                        default:status=404;break;
                    }
                }
                var bytes=Encoding.UTF8.GetBytes(body);
                var response=Encoding.ASCII.GetBytes($"HTTP/1.1 {status} {(status==200?"OK":"Rejected")}\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\n\r\n");
                await stream.WriteAsync(response,timeout.Token);await stream.WriteAsync(bytes,timeout.Token);
            } catch(Exception e) when(e is IOException or SocketException or OperationCanceledException or ObjectDisposedException) { }
            finally {clients.Release();}
        }
    }
    public void Dispose(){stop.Cancel();listener.Stop();}
}