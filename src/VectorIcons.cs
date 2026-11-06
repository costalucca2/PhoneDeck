using System;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Collections.Generic;

static class VectorIcons {
 static readonly Dictionary<string,Bitmap> cache=new Dictionary<string,Bitmap>();
 public static Color Background(System.Windows.Forms.Control parent){while(parent!=null){if(parent.BackColor.A==255)return parent.BackColor;parent=parent.Parent;}return DeckTheme.Surface;}
 public static void Draw(Graphics target,string name,Rectangle rect,Color color){string key=name+":"+rect.Width+":"+color.ToArgb();Bitmap bitmap;if(!cache.TryGetValue(key,out bitmap)){
  bitmap=new Bitmap(rect.Width,rect.Height);using(var stream=typeof(VectorIcons).Assembly.GetManifestResourceStream("PhoneDeck.Icons."+name+".svg")){if(stream==null)throw new InvalidDataException("Missing SVG icon: "+name);var document=new XmlDocument{XmlResolver=null};document.Load(stream);var root=document.DocumentElement;string[] view=root.GetAttribute("viewBox").Split(' ');float width=Single.Parse(view[2],CultureInfo.InvariantCulture);using(var graphics=Graphics.FromImage(bitmap)){
   graphics.SmoothingMode=SmoothingMode.AntiAlias;graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;graphics.Clear(Color.Transparent);graphics.ScaleTransform(rect.Width/width,rect.Height/width);
   foreach(XmlNode node in root.ChildNodes){var element=node as XmlElement;if(element==null)continue;using(var path=Path(element)){
    string fill=element.HasAttribute("fill")?element.GetAttribute("fill"):root.GetAttribute("fill");string stroke=element.HasAttribute("stroke")?element.GetAttribute("stroke"):root.GetAttribute("stroke");if(fill!="none"&&fill.Length>0)using(var brush=new SolidBrush(color))graphics.FillPath(brush,path);
    if(stroke!="none"&&stroke.Length>0){float size=1.8f;Single.TryParse(root.GetAttribute("stroke-width"),NumberStyles.Float,CultureInfo.InvariantCulture,out size);using(var pen=new Pen(color,size)){pen.StartCap=pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;graphics.DrawPath(pen,path);}}
   }}
  }}cache[key]=bitmap;
 }target.DrawImage(bitmap,rect);}
 static float Number(XmlElement node,string attribute){return Single.Parse(node.GetAttribute(attribute),CultureInfo.InvariantCulture);}
 static GraphicsPath Path(XmlElement node){var path=new GraphicsPath();switch(node.LocalName){case "circle":float radius=Number(node,"r");path.AddEllipse(Number(node,"cx")-radius,Number(node,"cy")-radius,radius*2,radius*2);break;case "rect":path.AddRectangle(new RectangleF(Number(node,"x"),Number(node,"y"),Number(node,"width"),Number(node,"height")));break;case "path":
  var geometry=System.Windows.Media.Geometry.Parse(node.GetAttribute("d")).GetFlattenedPathGeometry(.02,System.Windows.Media.ToleranceType.Absolute);
  foreach(var figure in geometry.Figures){var points=new List<PointF>{new PointF((float)figure.StartPoint.X,(float)figure.StartPoint.Y)};foreach(var segment in figure.Segments){var poly=segment as System.Windows.Media.PolyLineSegment;if(poly!=null)foreach(var point in poly.Points)points.Add(new PointF((float)point.X,(float)point.Y));var line=segment as System.Windows.Media.LineSegment;if(line!=null)points.Add(new PointF((float)line.Point.X,(float)line.Point.Y));}if(points.Count>1){path.StartFigure();path.AddLines(points.ToArray());if(figure.IsClosed)path.CloseFigure();}}break;
  default:throw new InvalidDataException("Unsupported SVG element: "+node.LocalName);
 }return path;}
}

class DeckIconButton:System.Windows.Forms.Button {
 public string IconName="settings";public Color? Accent;bool hover;
 public DeckIconButton(){Size=new Size(44,44);FlatStyle=System.Windows.Forms.FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=System.Windows.Forms.Cursors.Hand;SetStyle(System.Windows.Forms.ControlStyles.UserPaint|System.Windows.Forms.ControlStyles.AllPaintingInWmPaint|System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer,true);}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnPaint(System.Windows.Forms.PaintEventArgs e){e.Graphics.Clear(VectorIcons.Background(Parent));e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;int box=Math.Min(Width,Height);if(hover)using(var brush=new SolidBrush(DeckTheme.Raised))using(var path=DeckTheme.Round(new Rectangle((Width-box)/2+3,(Height-box)/2+3,box-6,box-6),7))e.Graphics.FillPath(brush,path);int size=(int)Math.Round(19*e.Graphics.DpiX/96);VectorIcons.Draw(e.Graphics,IconName,new Rectangle((Width-size)/2,(Height-size)/2,size,size),Accent??DeckTheme.Text);if(Focused&&ShowFocusCues)System.Windows.Forms.ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle((Width-box)/2+4,(Height-box)/2+4,box-8,box-8),DeckTheme.Blue,DeckTheme.Surface);}
}
