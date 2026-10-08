using System.Text.Json;
namespace ClaudeSkin;
public sealed record Theme(string Id, string Name, string Kind, string Media, string Poster, string Note);
public sealed record SkinSettings(string ThemeId = "shorekeeper", double Dim = .45, bool Motion = true, bool WebEnabled = false);
public sealed class ThemeService
{
    public static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, WriteIndented = true };
    public string Root { get; }
    public IReadOnlyList<Theme> Themes { get; }
    public SkinSettings Settings { get; private set; }
    public string Revision { get; private set; } = Guid.NewGuid().ToString("N");
    private readonly string settingsPath;
    private readonly object gate = new();
    public ThemeService(string? root = null, string? configPath = null)
    {
        Root = root ?? AppContext.BaseDirectory;
        settingsPath = configPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClaudeSkin", "settings.json");
        Themes = JsonSerializer.Deserialize<List<Theme>>(File.ReadAllText(Path.Combine(Root,"themes","catalog.json")), Json) ?? throw new InvalidDataException("테마 목록이 없습니다.");
        if (Themes.Count == 0 || Themes.Any(t => t.Kind is not ("gif" or "video") || !ValidAsset(t.Media) || !ValidAsset(t.Poster)) || Themes.Select(t=>t.Id).Distinct().Count()!=Themes.Count) throw new InvalidDataException("잘못된 테마 목록입니다.");
        SkinSettings loaded = new();
        try { if (File.Exists(settingsPath)) loaded = JsonSerializer.Deserialize<SkinSettings>(File.ReadAllText(settingsPath), Json) ?? loaded; }
        catch (Exception e) when (e is JsonException or IOException) { }
        Settings = Normalize(loaded);
    }
    static bool ValidAsset(string name) => Path.GetFileName(name) == name && !name.Contains(':') && !name.Contains('/') && !name.Contains('\\') && name.Length > 0;
    SkinSettings Normalize(SkinSettings value) => value with { ThemeId = Themes.Any(t=>t.Id==value.ThemeId) ? value.ThemeId : Themes[0].Id, Dim = double.IsFinite(value.Dim) ? Math.Clamp(value.Dim,.1,.85) : .45 };
    public void Update(SkinSettings value)
    {
        lock (gate) {
            var normalized = Normalize(value);
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            var temporary=settingsPath+".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(normalized,Json));
            File.Move(temporary,settingsPath,true);
            Settings=normalized; Revision=Guid.NewGuid().ToString("N");
        }
    }
    public string PosterPath(Theme theme) => Path.Combine(Root,"themes","media",theme.Poster);
    public string Payload(bool? enabled = null)
    {
        lock (gate) {
            var theme = Themes.Single(t=>t.Id==Settings.ThemeId);
            string Data(string file) {
                var mime=Path.GetExtension(file).ToLowerInvariant() switch { ".mp4"=>"video/mp4", ".webm"=>"video/webm", ".gif"=>"image/gif", ".png"=>"image/png", _=>throw new InvalidDataException("지원하지 않는 미디어") };
                var data=File.ReadAllBytes(Path.Combine(Root,"themes","media",file));
                if (data.Length>20_000_000) throw new InvalidDataException("미디어가 너무 큽니다.");
                return $"data:{mime};base64,{Convert.ToBase64String(data)}";
            }
            return JsonSerializer.Serialize(new { enabled=enabled??Settings.WebEnabled, theme.Id, theme.Name, theme.Kind, dim=Settings.Dim, motion=Settings.Motion, media=Data(theme.Media), poster=Data(theme.Poster), css=File.ReadAllText(Path.Combine(Root,"shared","skin.css")) }, Json);
        }
    }
}
