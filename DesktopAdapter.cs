using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
namespace ClaudeSkin;
public sealed class DesktopAdapter
{
    readonly ThemeService service;
    static readonly HttpClient http=new(new HttpClientHandler {UseProxy=false, AllowAutoRedirect=false}) {Timeout=TimeSpan.FromSeconds(3), MaxResponseContentBufferSize=1_000_000};
    public DesktopAdapter(ThemeService service) => this.service=service;
    public static bool IsClaudePage(string? value) => Uri.TryCreate(value,UriKind.Absolute,out var uri) && uri.Scheme=="https" && uri.Host=="claude.ai";
    void VerifyOwner(string executable,int port)
    {
        var pid=PortOwner.Find(port) ?? throw new InvalidOperationException("해당 포트에 Claude Desktop이 없습니다. 앱을 디버그 모드로 열어 주세요.");
        using var process=Process.GetProcessById(pid);
        if (!string.Equals(Path.GetFullPath(executable),process.MainModule?.FileName,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("연결 포트가 선택한 Claude 실행 파일의 소유가 아닙니다.");
    }
    public void Launch(string executable,int port)
    {
        if (!File.Exists(executable) || !Path.GetFileName(executable).Equals("Claude.exe",StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("공식 Claude.exe를 선택해 주세요.");
        if (PortOwner.Find(port) is not null) throw new InvalidOperationException("포트가 사용 중입니다. 번호를 바꾸거나 기존 앱을 직접 닫아 주세요.");
        var info=new ProcessStartInfo(executable) {UseShellExecute=false};
        info.ArgumentList.Add("--remote-debugging-address=127.0.0.1");
        info.ArgumentList.Add($"--remote-debugging-port={port}");
        Process.Start(info);
    }
    public async Task<int> Apply(string executable,int port,bool enabled)
    {
        VerifyOwner(executable,port);
        var list=await http.GetStringAsync($"http://127.0.0.1:{port}/json/list");
        using var document=JsonDocument.Parse(list);
        var targets=document.RootElement.EnumerateArray().Where(t=>t.TryGetProperty("type",out var type)&&type.GetString()=="page"&&t.TryGetProperty("url",out var url)&&IsClaudePage(url.GetString())).ToArray();
        if(targets.Length==0) throw new InvalidOperationException("지원하는 claude.ai Desktop 화면을 찾지 못했습니다. 이 앱 버전은 실험용 연결 대상과 다를 수 있습니다.");
        var engine=File.ReadAllText(Path.Combine(service.Root,"shared","skin-engine.js"));
        var expression=enabled ? engine+"\n globalThis.ClaudeSkin.apply("+service.Payload(true)+"); true;" : "globalThis.ClaudeSkin?.remove(); true;";
        foreach(var target in targets) {
            VerifyOwner(executable,port);
            var websocket=target.GetProperty("webSocketDebuggerUrl").GetString()!;
            if(!Uri.TryCreate(websocket,UriKind.Absolute,out var uri) || uri.Scheme!="ws" || uri.Host is not ("127.0.0.1" or "localhost") || uri.Port!=port) throw new InvalidDataException("잘못된 Desktop 연결 주소입니다.");
            using var socket=new ClientWebSocket(); socket.Options.Proxy=null;
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await socket.ConnectAsync(uri,timeout.Token);
            var message=JsonSerializer.Serialize(new {id=1,method="Runtime.evaluate",@params=new {expression,returnByValue=true}},ThemeService.Json);
            await socket.SendAsync(Encoding.UTF8.GetBytes(message),WebSocketMessageType.Text,true,timeout.Token);
            using var received=new MemoryStream(); var buffer=new byte[16_384];
            while(true) {
                var result=await socket.ReceiveAsync(buffer,timeout.Token);
                if(result.MessageType==WebSocketMessageType.Close) throw new IOException("Desktop 연결이 닫혔습니다.");
                received.Write(buffer,0,result.Count);
                if(received.Length>2_000_000) throw new IOException("Desktop 응답이 너무 큽니다.");
                if(!result.EndOfMessage) continue;
                using var response=JsonDocument.Parse(received.ToArray()); received.SetLength(0);
                if(!response.RootElement.TryGetProperty("id",out var id)||id.GetInt32()!=1) continue;
                if(response.RootElement.TryGetProperty("error",out _) || (response.RootElement.TryGetProperty("result",out var evaluation)&&evaluation.TryGetProperty("exceptionDetails",out _))) throw new InvalidOperationException("Desktop에서 테마 적용을 거부했습니다.");
                break;
            }
        }
        return targets.Length;
    }
}
