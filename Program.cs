namespace ClaudeSkin;
internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Contains("--self-test")) return SelfTest.Run();
        if (args.Length==2 && args[0]=="--export-preview") { ApplicationConfiguration.Initialize();using var form=new ManagerForm(previewOnly:true); form.ExportPreview(Path.GetFullPath(args[1]));return 0; }
        using var mutex = new Mutex(true, "Local\\GNhoLabs.ClaudeSkin", out var first);
        if (!first) { MessageBox.Show("ClaudeSkin이 이미 실행 중입니다."); return 0; }
        ApplicationConfiguration.Initialize();
        Application.Run(new ManagerForm());
        return 0;
    }
}
