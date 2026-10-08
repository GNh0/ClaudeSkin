using System.Diagnostics;
namespace ClaudeSkin;
public sealed class ManagerForm : Form
{
    readonly ThemeService service=new();
    readonly ComboBox themes=new() {DropDownStyle=ComboBoxStyle.DropDownList};
    readonly TrackBar dim=new() {Minimum=10,Maximum=85,TickFrequency=15};
    readonly CheckBox motion=new() {Text="움직이는 배경",AutoSize=true};
    readonly CheckBox web=new() {Text="웹에 적용 · 관리 프로그램 실행 중",AutoSize=true};
    readonly PictureBox preview=new() {SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(5,10,17)};
    readonly Label status=new() {AutoSize=false,ForeColor=Color.FromArgb(115,235,219),Height=55};
    readonly TextBox executable=new() {PlaceholderText="Claude.exe 경로"};
    readonly NumericUpDown port=new() {Minimum=1024,Maximum=65535,Value=9226};
    LocalServer? server;
    readonly DesktopAdapter desktop;
    bool busy;
    public ManagerForm(bool previewOnly=false)
    {
        desktop=new(service);
        Text="ClaudeSkin · GNho Labs"; MinimumSize=new Size(920,680); Size=new Size(1140,760);
        BackColor=Color.FromArgb(12,19,29); ForeColor=Color.FromArgb(230,239,245); Font=new Font("맑은 고딕",10);
        var layout=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Padding=new Padding(28)};
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,64)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,75));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));Controls.Add(layout);
        var header=new Label {Text="ClaudeSkin\n하나의 라이브러리, 두 가지 Claude 화면",AutoSize=true,Font=new Font(Font.FontFamily,18,FontStyle.Bold)};
        layout.Controls.Add(header,0,0);layout.SetColumnSpan(header,2);
        var previewPanel=new TableLayoutPanel {Dock=DockStyle.Fill,RowCount=2,ColumnCount=1,Padding=new Padding(0,12,25,0)};
        previewPanel.RowStyles.Add(new RowStyle(SizeType.Percent,100));previewPanel.RowStyles.Add(new RowStyle(SizeType.Absolute,45));
        preview.Dock=DockStyle.Fill;previewPanel.Controls.Add(preview);
        previewPanel.Controls.Add(new Label {Text="미디어 미리보기 · 실제 Claude 화면 캡처가 아닙니다",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.FromArgb(157,177,192)});
        layout.Controls.Add(previewPanel,0,1);
        var options=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(0,12,0,0)};
        layout.Controls.Add(options,1,1);
        void Title(string text)=>options.Controls.Add(new Label {Text=text,AutoSize=true,Margin=new Padding(0,10,0,8),Font=new Font(Font,FontStyle.Bold)});
        void Add(Control control){control.Width=310;control.Margin=new Padding(0,0,0,10);options.Controls.Add(control);}
        themes.DataSource=service.Themes.ToList();themes.DisplayMember="Name";themes.SelectedItem=service.Themes.First(t=>t.Id==service.Settings.ThemeId);
        dim.Value=(int)(service.Settings.Dim*100);motion.Checked=service.Settings.Motion;web.Checked=service.Settings.WebEnabled;
        Title("테마 라이브러리");Add(themes);Title("배경 어둡게 · 10–85%");Add(dim);Add(motion);Add(web);
        Add(Button("선택 저장 / 웹 적용",()=> {Save();status.Text="저장했습니다. 웹 확장은 약 5초 후 반영합니다.";}));
        Add(Button("웹 확장 폴더 열기",()=>Process.Start(new ProcessStartInfo(Path.Combine(service.Root,"extension")){UseShellExecute=true})));
        Title("Claude Desktop · 실험 기능");Add(executable);
        Add(Button("Claude.exe 선택",()=> {using var dialog=new OpenFileDialog {Filter="Claude Desktop|Claude.exe",Title="공식 Claude.exe 선택"};if(dialog.ShowDialog()==DialogResult.OK)executable.Text=dialog.FileName;}));
        Add(port);
        Add(Button("디버그 모드로 열기",()=> {desktop.Launch(executable.Text,(int)port.Value);status.Text="Desktop을 열었습니다. 화면 로딩 후 연결 및 적용을 누르세요.";}));
        Add(Button("Desktop 연결 및 적용",async()=> {Save();var count=await desktop.Apply(executable.Text,(int)port.Value,true);status.Text=$"Desktop {count}개 화면에 적용했습니다.";}));
        Add(Button("기본 화면 복원",async()=> {web.Checked=false;Save();if(!string.IsNullOrWhiteSpace(executable.Text))await desktop.Apply(executable.Text,(int)port.Value,false);status.Text="기본 화면으로 복원했습니다.";}));
        Add(new Label {Text="웹: Chrome/Edge 확장 설치 필요\nDesktop: 디버그 연결을 지원하는 버전만 가능\n디버그 포트는 이 PC의 다른 프로그램에서도 접근 가능합니다. 사용 후 Desktop을 정상 종료하고 평소 방식으로 다시 여세요.",AutoSize=true,MaximumSize=new Size(310,0),ForeColor=Color.FromArgb(157,177,192)});
        Add(status);
        themes.SelectedValueChanged+=(_,_)=>ShowPoster();ShowPoster();
        if (!previewOnly) {try {server=new LocalServer(service);status.Text="웹 관리 서비스가 준비되었습니다.";}catch(Exception e){status.Text="웹 연결 오류: "+e.Message;}}
        FormClosed+=(_,_)=> {server?.Dispose();preview.Image?.Dispose();};
    }
    Button Button(string text,Action action)=>Button(text,()=>{action();return Task.CompletedTask;});
    Button Button(string text,Func<Task> action)
    {
        var button=new Button {Text=text,Height=37,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(24,50,58),ForeColor=Color.FromArgb(180,250,237)};
        button.Click+=async(_,_)=> {if(busy)return;busy=true;button.Enabled=false;try{await action();}catch(Exception e){status.Text=e.Message;}finally{busy=false;button.Enabled=true;}};
        return button;
    }
    public void ExportPreview(string file) { ShowInTaskbar=false;Opacity=0;Show();PerformLayout();Application.DoEvents(); using var bitmap=new Bitmap(ClientSize.Width,ClientSize.Height);DrawToBitmap(bitmap,new Rectangle(Point.Empty,ClientSize));bitmap.Save(file,System.Drawing.Imaging.ImageFormat.Png); }
    void Save() { if(themes.SelectedItem is Theme theme)service.Update(new(theme.Id,dim.Value/100d,motion.Checked,web.Checked)); }
    void ShowPoster()
    {
        if(themes.SelectedItem is not Theme theme)return;
        using var loaded=Image.FromFile(service.PosterPath(theme));var copy=new Bitmap(loaded);
        var old=preview.Image;preview.Image=copy;old?.Dispose();
    }
}
