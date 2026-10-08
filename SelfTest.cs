using System.Net;
using System.Net.Sockets;
using System.Text.Json;
namespace ClaudeSkin;
internal static class SelfTest
{
    public static int Run()
    {
        var config=Path.Combine(Path.GetTempPath(),"claudeskin-test-"+Guid.NewGuid().ToString("N")+".json");
        void Require(bool value,string message){if(!value)throw new Exception(message);}
        try {
            var service=new ThemeService(configPath:config);
            Require(service.Themes.Count==3,"Missing migrated themes");
            foreach(var theme in service.Themes) {
                service.Update(new(theme.Id,.42,true,true));
                using var payload=JsonDocument.Parse(service.Payload());
                var media=payload.RootElement.GetProperty("media").GetString()!;
                var bytes=Convert.FromBase64String(media[(media.IndexOf(',')+1)..]);
                Require(bytes.Length>100_000,"Media missing or a hash was copied instead");
                Require(theme.Kind!="video"||System.Text.Encoding.ASCII.GetString(bytes,4,4)=="ftyp","Not a real MP4");
                Require(File.Exists(service.PosterPath(theme)),"Missing poster");
            }
            service.Update(new("invalid",5,true,false));Require(service.Settings.Dim==.85&&!service.Settings.WebEnabled,"Settings normalization failed");
            var reload=new ThemeService(configPath:config);Require(reload.Settings==service.Settings,"Settings persistence failed");
            Require(DesktopAdapter.IsClaudePage("https://claude.ai/new")&&!DesktopAdapter.IsClaudePage("https://claude.ai.evil.test/new")&&!DesktopAdapter.IsClaudePage("http://claude.ai/new"),"Desktop target restriction failed");
            using var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();var p=((IPEndPoint)listener.LocalEndpoint).Port;
            Require(PortOwner.Find(p)==Environment.ProcessId,"Loopback owner validation failed");listener.Stop();
            using var server=new LocalServer(service);
            using var client=new HttpClient(new HttpClientHandler {UseProxy=false});
            var state=client.GetStringAsync("http://127.0.0.1:9164/state").GetAwaiter().GetResult();Require(state.Contains("ClaudeSkin"),"Manager API unavailable");
            var request=new HttpRequestMessage(HttpMethod.Get,"http://127.0.0.1:9164/skin");request.Headers.Add("Origin","https://example.com");
            Require(client.Send(request).StatusCode==HttpStatusCode.Forbidden,"Ordinary webpage must not access settings");
            Require(client.PostAsync("http://127.0.0.1:9164/skin",new StringContent("{}" )).GetAwaiter().GetResult().StatusCode==HttpStatusCode.Forbidden,"Settings must be read-only");
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"self-test-result.txt"),"PASS: theme media, settings, desktop allowlist, loopback ownership, read-only API, origin restrictions.\n");
            return 0;
        } catch(Exception e) {File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"self-test-result.txt"),"FAIL: "+e);return 1;}
        finally {if(File.Exists(config))File.Delete(config);if(File.Exists(config+".tmp"))File.Delete(config+".tmp");}
    }
}
