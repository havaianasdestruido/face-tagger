namespace FaceTagger {

public partial class Form1 : Form
{
    private readonly FaceTagger.Engine.FaceRecognitionEngine _engine = new();
    private readonly string _imageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
    private readonly string _statusFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "status.txt");
    private readonly ListBox _listImages = new();
    private readonly PictureBox _pic = new();
    private readonly ListBox _listTags = new();
    private readonly Button _btnDetect = new();
    private readonly Button _btnAddTag = new();
    private readonly Button _btnSave = new();
    private readonly TextBox _txtStatus = new();
    private Image _currentImage; 
    private Point _dragStart; 
    private Rectangle _selection;
    private bool _dragging;
    private readonly Dictionary<string, List<Tag>> _tags = new();
    private class Tag { public string Name; public Rectangle Rect; }
    public Form1()
    {
        // UI setup
        this.Text = "FaceTagger";
        this.Width = 1000; this.Height = 600;
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 200 };
        this.Controls.Add(split);
        // left pane list images
        var leftPanel = split.Panel1;
        _listImages.Dock = DockStyle.Fill;
        leftPanel.Controls.Add(_listImages);
        // center picture
        var centerPanel = split.Panel2;
        var innerSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 400 };
        centerPanel.Controls.Add(innerSplit);
        _pic.Dock = DockStyle.Fill; _pic.SizeMode = PictureBoxSizeMode.Zoom; innerSplit.Panel1.Controls.Add(_pic);
        // right pane tags and buttons
        var rightPanel = innerSplit.Panel2;
        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30 };
        _btnDetect.Text = "Detect"; _btnAddTag.Text = "Add Tag"; _btnSave.Text = "Save";
        btnPanel.Controls.Add(_btnDetect); btnPanel.Controls.Add(_btnAddTag); btnPanel.Controls.Add(_btnSave);
        rightPanel.Controls.Add(btnPanel);
        _listTags.Dock = DockStyle.Fill; rightPanel.Controls.Add(_listTags);
        _txtStatus.Dock = DockStyle.Bottom; _txtStatus.Height = 40; rightPanel.Controls.Add(_txtStatus);
        // events
        _listImages.SelectedIndexChanged += (s,e)=>LoadImage();
        _btnDetect.Click += (s,e)=>Detect();
        _btnAddTag.Click += (s,e)=>StartManualTag();
        _btnSave.Click += (s,e)=>SaveTags();
        _pic.MouseDown += Pic_MouseDown; _pic.MouseMove += Pic_MouseMove; _pic.MouseUp += Pic_MouseUp; _pic.Paint += Pic_Paint;
        // init folder
        Directory.CreateDirectory(_imageDir);
        // generate test image if none
        if (!Directory.GetFiles(_imageDir,"*.jpg").Any())
        {
            var testPath = Path.Combine(_imageDir,"test.jpg");
            using var bmp = new System.Drawing.Bitmap(200,200);
            using var g = System.Drawing.Graphics.FromImage(bmp);
            g.Clear(System.Drawing.Color.LightGray);
            g.DrawString("Test", new System.Drawing.Font("Arial",20), System.Drawing.Brushes.Black,10,80);
            bmp.Save(testPath, System.Drawing.Imaging.ImageFormat.Jpeg);
        }
        // populate list
        foreach (var f in Directory.GetFiles(_imageDir,"*.jpg")) _listImages.Items.Add(Path.GetFileName(f));
        // engine status
        _txtStatus.Text = _engine.ProbeMessage;
        _btnDetect.Enabled = _engine.ProbeResult == 0; // enable only if S_OK
        // load first image
        if (_listImages.Items.Count>0) _listImages.SelectedIndex=0;
    }
    private void LoadImage()
    {
        var file = _listImages.SelectedItem as string; if (file==null) return;
        var path = Path.Combine(_imageDir,file);
        _currentImage = System.Drawing.Image.FromFile(path);
        _pic.Image = _currentImage;
        // load tags
        var jsonPath = Path.ChangeExtension(path,".json");
        if (File.Exists(jsonPath))
        {
            var txt = File.ReadAllText(jsonPath);
            var data = System.Text.Json.JsonSerializer.Deserialize<ImageData>(txt);
            _tags[file]=data.Tags.Select(t=>new Tag{ Name=t.Name, Rect= new Rectangle(t.Rect.X,t.Rect.Y,t.Rect.W,t.Rect.H)}).ToList();
        }
        else _tags[file]=new();
        RefreshTagList();
    }
    private void RefreshTagList()
    {
        var file = _listImages.SelectedItem as string; if (file==null) return;
        _listTags.Items.Clear();
        foreach (var t in _tags[file]) _listTags.Items.Add($"{t.Name} [{t.Rect.X},{t.Rect.Y},{t.Rect.Width},{t.Rect.Height}]");
    }
    private void Detect()
    {
        // stub: report stub HRESULT
        _txtStatus.Text = $"Detect stub: {_engine.ProbeMessage}";
    }
    private void StartManualTag()
    {
        _dragging=true; _selection=Rectangle.Empty;
    }
    private void Pic_MouseDown(object s,MouseEventArgs e){ if(e.Button!=MouseButtons.Left)return; _dragStart=e.Location; _dragging=true; }
    private void Pic_MouseMove(object s,MouseEventArgs e){ if(!_dragging)return; var cur=e.Location; _selection=Rectangle.FromLTRB(Math.Min(_dragStart.X,cur.X),Math.Min(_dragStart.Y,cur.Y),Math.Max(_dragStart.X,cur.X),Math.Max(_dragStart.Y,cur.Y)); _pic.Invalidate(); }
    private void Pic_MouseUp(object s,MouseEventArgs e){ if(e.Button!=MouseButtons.Left||!_dragging)return; _dragging=false; if(_selection.Width<5||_selection.Height<5)return; var name=Microsoft.VisualBasic.Interaction.InputBox("Enter name","Tag"); if(string.IsNullOrWhiteSpace(name))return; var file=_listImages.SelectedItem as string; if(file==null)return; var rect=_selection; // convert to image coordinates
        var imgW=_pic.Image.Width; var imgH=_pic.Image.Height; var ctrlW=_pic.Width; var ctrlH=_pic.Height; var scaleX=(float)imgW/ctrlW; var scaleY=(float)imgH/ctrlH; var imgRect=new Rectangle((int)(rect.X*scaleX),(int)(rect.Y*scaleY),(int)(rect.Width*scaleX),(int)(rect.Height*scaleY));
        _tags[file].Add(new Tag{name=name,Rect=imgRect});
        RefreshTagList(); _pic.Invalidate();
    }
    private void Pic_Paint(object s,PaintEventArgs e){ if(_selection!=Rectangle.Empty) e.Graphics.DrawRectangle(Pens.Red,_selection); foreach(var t in _tags[_listImages.SelectedItem as string]??new()) e.Graphics.DrawRectangle(Pens.Green,t.Rect); }
    private void SaveTags(){ var file=_listImages.SelectedItem as string; if(file==null)return; var path=Path.Combine(_imageDir,file); var jsonPath=Path.ChangeExtension(path,".json"); var data=new ImageData{ File=file, Tags=_tags[file].Select(t=>new TagDto{ Name=t.Name, Rect=new RectDto{ X=t.Rect.X,Y=t.Rect.Y,W=t.Rect.Width,H=t.Rect.Height}}).ToList()}; var txt=System.Text.Json.JsonSerializer.Serialize(data); File.WriteAllText(jsonPath,txt); _txtStatus.Text="Saved"; }
    private class ImageData{ public string File{get;set;}public List<TagDto> Tags{get;set;} }
    private class TagDto{ public string Name{get;set;}public RectDto Rect{get;set;} }
    private class RectDto{ public int X{get;set;}public int Y{get;set;}public int W{get;set;}public int H{get;set;} }
}
}
