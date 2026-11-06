using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class ScrollChrome {
 const int VerticalStyle=0x00200000,HorizontalStyle=0x00100000;
 static readonly ConditionalWeakTable<Control,Binding> attached=new ConditionalWeakTable<Control,Binding>();
 public static void Apply(Control control){if(control is SlimHistoryList)return;var scroll=control as ScrollableControl;var text=control as TextBoxBase;if(!(text!=null&&text.Multiline)&&!(control is ListBox)&&!(control is ComboBox)&&!(scroll!=null&&scroll.AutoScroll))return;Binding existing;if(attached.TryGetValue(control,out existing))return;var binding=new Binding(control);attached.Add(control,binding);binding.Attach();}
 internal struct Range {
  public int Min,Max,Value;public uint Page;
  public int End {get{return Math.Max(Min,Max-(int)Math.Max(1,Page)+1);}}
  public bool Scrollable {get{return End>Min;}}
 }
 interface Target {Range Read(bool vertical);void Move(bool vertical,int value);void Wheel(int delta);Color Background{get;}}
 static Range ReadRange(IntPtr window,bool vertical){if(window==IntPtr.Zero)return new Range{Page=1};var info=new Info{Size=Marshal.SizeOf(typeof(Info)),Mask=0x17};if(!GetScrollInfo(window,vertical?1:0,ref info))return new Range{Page=1};return new Range{Min=info.Min,Max=info.Max,Value=info.Pos,Page=info.Page};}
 sealed class Binding:Target {
  readonly Control owner;Host host;Hook hook;ComboMouseHook comboHook;Popup popup;bool wrapping,queued,clipping;int clipWidth=-1,clipHeight=-1;
  public Binding(Control control){owner=control;}
  public Color Background {get{return owner.BackColor.A==255?owner.BackColor:DeckTheme.Surface;}}
  public void Attach(){var combo=owner as ComboBox;if(combo!=null){owner.HandleCreated+=(s,e)=>{if(comboHook!=null)comboHook.ReleaseHandle();comboHook=new ComboMouseHook(this);comboHook.AssignHandle(owner.Handle);};owner.HandleDestroyed+=(s,e)=>{if(comboHook!=null)comboHook.ReleaseHandle();};if(owner.IsHandleCreated){comboHook=new ComboMouseHook(this);comboHook.AssignHandle(owner.Handle);}combo.DropDown+=(s,e)=>{var info=new ComboInfo{Size=Marshal.SizeOf(typeof(ComboInfo))};if(GetComboBoxInfo(combo.Handle,ref info)&&info.List!=IntPtr.Zero){if(popup!=null)popup.Dispose();popup=new Popup(combo,info.List);}};owner.Disposed+=(s,e)=>{if(popup!=null)popup.Dispose();};return;}owner.ParentChanged+=(s,e)=>Wrap();owner.HandleCreated+=(s,e)=>{clipWidth=clipHeight=-1;if(hook!=null)hook.ReleaseHandle();hook=new Hook(this);hook.AssignHandle(owner.Handle);RefreshLater();};owner.HandleDestroyed+=(s,e)=>{if(hook!=null)hook.ReleaseHandle();};owner.SizeChanged+=(s,e)=>RefreshLater();owner.FontChanged+=(s,e)=>RefreshLater();owner.TextChanged+=(s,e)=>RefreshLater();owner.BackColorChanged+=(s,e)=>{if(host!=null)host.RefreshColors();};Wrap();if(owner.IsHandleCreated){hook=new Hook(this);hook.AssignHandle(owner.Handle);RefreshLater();}}
  void Wrap(){if(wrapping||host!=null||owner.Parent==null)return;wrapping=true;var parent=owner.Parent;var table=parent as TableLayoutPanel;var flow=parent as FlowLayoutPanel;var cell=table==null?new TableLayoutPanelCellPosition():table.GetCellPosition(owner);int rowSpan=table==null?1:table.GetRowSpan(owner),columnSpan=table==null?1:table.GetColumnSpan(owner);bool flowBreak=flow!=null&&flow.GetFlowBreak(owner);int index=parent.Controls.GetChildIndex(owner);var replacement=new Host(this,owner){Name=owner.Name+"ScrollHost",Bounds=owner.Bounds,Dock=owner.Dock,Anchor=owner.Anchor,Margin=owner.Margin,TabIndex=owner.TabIndex};host=replacement;parent.SuspendLayout();try{parent.Controls.Remove(owner);var list=owner as ListBox;if(list!=null)list.IntegralHeight=false;owner.Dock=DockStyle.None;owner.Anchor=AnchorStyles.Top|AnchorStyles.Left;owner.Margin=Padding.Empty;host.View.Controls.Add(owner);if(table!=null){table.Controls.Add(host,cell.Column,cell.Row);table.SetRowSpan(host,rowSpan);table.SetColumnSpan(host,columnSpan);}else parent.Controls.Add(host);if(flow!=null)flow.SetFlowBreak(host,flowBreak);parent.Controls.SetChildIndex(host,index);}finally{parent.ResumeLayout(true);wrapping=false;}host.Arrange();}
  public Range Read(bool vertical){return ReadRange(owner.IsHandleCreated?owner.Handle:IntPtr.Zero,vertical);}
  public void Move(bool vertical,int value){var range=Read(vertical);value=Math.Max(range.Min,Math.Min(range.End,value));var list=owner as ListBox;var panel=owner as ScrollableControl;if(list!=null&&vertical)list.TopIndex=value;else if(panel!=null)panel.AutoScrollPosition=vertical?new Point(Read(false).Value,value):new Point(value,Read(true).Value);else if(owner is TextBox&&vertical)SendMessage(owner.Handle,0xB6,IntPtr.Zero,(IntPtr)(value-range.Value));else SendMessage(owner.Handle,vertical?0x115:0x114,(IntPtr)(4|((value&65535)<<16)),IntPtr.Zero);if(host!=null)host.Arrange();}
  public void Wheel(int delta){if(owner.IsHandleCreated)SendMessage(owner.Handle,0x20A,(IntPtr)(delta<<16),IntPtr.Zero);}
  void RefreshLater(){if(host==null||host.Arranging||queued||host.IsDisposed||!host.IsHandleCreated)return;queued=true;host.BeginInvoke((Action)(()=>{queued=false;if(!host.IsDisposed)host.Arrange();}));}
  public void ClipChild(int width,int height){if(clipping||!owner.IsHandleCreated)return;width=Math.Max(0,width);height=Math.Max(0,height);if(width==clipWidth&&height==clipHeight)return;clipping=true;try{clipWidth=width;clipHeight=height;IntPtr region=CreateRectRgn(0,0,width,height);if(SetWindowRgn(owner.Handle,region,true)==0)DeleteObject(region);}finally{clipping=false;}}
  sealed class ComboMouseHook:NativeWindow {readonly Binding binding;public ComboMouseHook(Binding value){binding=value;}protected override void WndProc(ref Message m){if(binding.popup!=null&&binding.popup.Route(ref m))return;base.WndProc(ref m);}}
  sealed class Hook:NativeWindow {readonly Binding binding;public Hook(Binding value){binding=value;}protected override void WndProc(ref Message m){if(m.Msg==0x7C&&unchecked((int)m.WParam.ToInt64())==-16&&m.LParam!=IntPtr.Zero){int style=Marshal.ReadInt32(m.LParam,4);binding.ClipChild(binding.owner.Width-((style&VerticalStyle)!=0?SystemInformation.VerticalScrollBarWidth:0),binding.owner.Height-((style&HorizontalStyle)!=0?SystemInformation.HorizontalScrollBarHeight:0));}base.WndProc(ref m);if(m.Msg==5||m.Msg==0x47||m.Msg==0xF||m.Msg==0x115||m.Msg==0x114||m.Msg==0x20A||m.Msg==0x100||m.Msg==0x7D||m.Msg==0xC)binding.RefreshLater();}}
 }
 sealed class ClipView:Panel {public ClipView(){DoubleBuffered=true;}protected override void WndProc(ref Message m){if((m.Msg==0x317||m.Msg==0x318)&&m.WParam!=IntPtr.Zero){int saved=SaveDC(m.WParam);try{IntersectClipRect(m.WParam,0,0,ClientSize.Width,ClientSize.Height);SetMetaRgn(m.WParam);base.WndProc(ref m);}finally{RestoreDC(m.WParam,saved);}return;}base.WndProc(ref m);}}
 sealed class Host:Panel {
  readonly Binding target;readonly Control content;readonly AxisBar vertical,horizontal;Rectangle requestedBounds=Rectangle.Empty;
  public readonly Panel View=new ClipView();public bool Arranging;
  public Host(Binding binding,Control control){target=binding;content=control;DoubleBuffered=true;vertical=new AxisBar(binding,true);horizontal=new AxisBar(binding,false);Controls.Add(View);Controls.Add(vertical);Controls.Add(horizontal);SizeChanged+=(s,e)=>Arrange();HandleCreated+=(s,e)=>Arrange();RefreshColors();}
  public void RefreshColors(){BackColor=View.BackColor=target.Background;vertical.Invalidate();horizontal.Invalidate();}
  public void Arrange(){if(Arranging||IsDisposed)return;Arranging=true;try{int bar=Math.Max(8,(int)Math.Round(8*DeviceDpi/96.0));for(int pass=0;pass<3;pass++){var v=target.Read(true);var h=target.Read(false);int width=Math.Max(0,ClientSize.Width-(v.Scrollable?bar:0)),height=Math.Max(0,ClientSize.Height-(h.Scrollable?bar:0));View.SetBounds(0,0,width,height);int style=content.IsHandleCreated?GetWindowLong(content.Handle,-16):0;var desired=new Rectangle(0,0,width+((style&VerticalStyle)!=0?SystemInformation.VerticalScrollBarWidth:0),height+((style&HorizontalStyle)!=0?SystemInformation.HorizontalScrollBarHeight:0));if(desired!=requestedBounds){requestedBounds=desired;content.Bounds=desired;}}target.ClipChild(View.ClientSize.Width,View.ClientSize.Height);var vr=target.Read(true);var hr=target.Read(false);vertical.SetBounds(View.Right,0,bar,View.Height);horizontal.SetBounds(0,View.Bottom,View.Width,bar);vertical.Visible=vr.Scrollable;horizontal.Visible=hr.Scrollable;vertical.UpdateRange();horizontal.UpdateRange();}finally{Arranging=false;}}
 }
 sealed class AxisBar:Control {
  readonly Target target;readonly bool vertical;readonly IntPtr foreignParent;bool dragging,hover;int offset;string stamp="";
  public AxisBar(Target value,bool axis,IntPtr parent=default(IntPtr)){target=value;vertical=axis;foreignParent=parent;Name=axis?"SlimVerticalScroll":"SlimHorizontalScroll";AccessibleRole=AccessibleRole.ScrollBar;AccessibleName=axis?"Rolar verticalmente":"Rolar horizontalmente";TabStop=parent==IntPtr.Zero;Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable,true);if(parent!=IntPtr.Zero)SetStyle(ControlStyles.Selectable,false);}
  protected override CreateParams CreateParams {get{var p=base.CreateParams;if(foreignParent!=IntPtr.Zero){p.Parent=foreignParent;p.ExStyle|=0x08000000;p.Style=(p.Style|0x40000000)&unchecked((int)~0x80000000);}return p;}}
  Rectangle Grip {get{var r=target.Read(vertical);int length=vertical?Height:Width;int size=Math.Min(length,Math.Max(22,(int)((long)length*Math.Max(1,r.Page)/Math.Max(1,r.Max-r.Min+1))));int position=r.End<=r.Min?0:(int)((long)(length-size)*(r.Value-r.Min)/(r.End-r.Min));return vertical?new Rectangle(2,position,Math.Max(2,Width-4),size):new Rectangle(position,2,size,Math.Max(2,Height-4));}}
  public void UpdateRange(){var r=target.Read(vertical);string next=r.Min+"|"+r.Max+"|"+r.Page+"|"+r.Value+"|"+Size+"|"+UiPreferences.Light;if(next!=stamp){stamp=next;Invalidate();}}
  protected override void WndProc(ref Message m){if(foreignParent!=IntPtr.Zero&&m.Msg==0x21){m.Result=(IntPtr)3;return;}base.WndProc(ref m);}
  protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(target.Background);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;var grip=Grip;if(grip.Width<4||grip.Height<4)return;using(var path=DeckTheme.Round(grip,2))using(var brush=new SolidBrush(hover||dragging?DeckTheme.Dim:UiPreferences.Light?Color.FromArgb(155,169,187):Color.FromArgb(78,92,111)))e.Graphics.FillPath(brush,path);}
  public void Routed(int message,Point screen){var p=PointToClient(screen);var e=new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0);if(message==0x201)OnMouseDown(e);else if(message==0x200)OnMouseMove(e);else if(message==0x202)OnMouseUp(e);}
  protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;if(foreignParent!=IntPtr.Zero){var popup=target as Popup;if(popup!=null)popup.Gesture=true;}if(foreignParent==IntPtr.Zero)Focus();var g=Grip;int p=vertical?e.Y:e.X;if(g.Contains(e.Location)){dragging=true;if(foreignParent==IntPtr.Zero)Capture=true;offset=p-(vertical?g.Top:g.Left);}else{var r=target.Read(vertical);target.Move(vertical,r.Value+(p<(vertical?g.Top:g.Left)?-1:1)*(int)Math.Max(1,r.Page));}Invalidate();}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!dragging)return;var r=target.Read(vertical);int span=(vertical?Height:Width)-(vertical?Grip.Height:Grip.Width);if(span>0)target.Move(vertical,r.Min+(int)Math.Round((double)((vertical?e.Y:e.X)-offset)*(r.End-r.Min)/span));UpdateRange();}
  protected override void OnMouseUp(MouseEventArgs e){dragging=false;if(foreignParent==IntPtr.Zero)Capture=false;else{var popup=target as Popup;if(popup!=null)popup.Gesture=false;}Invalidate();base.OnMouseUp(e);}protected override void OnMouseCaptureChanged(EventArgs e){if(!Capture)dragging=false;base.OnMouseCaptureChanged(e);}
  protected override void OnMouseWheel(MouseEventArgs e){target.Wheel(e.Delta);base.OnMouseWheel(e);}
  protected override bool IsInputKey(Keys k){return k==Keys.Up||k==Keys.Down||k==Keys.Left||k==Keys.Right||k==Keys.PageUp||k==Keys.PageDown||k==Keys.Home||k==Keys.End||base.IsInputKey(k);}
  protected override void OnKeyDown(KeyEventArgs e){var r=target.Read(vertical);int value=r.Value;switch(e.KeyCode){case Keys.Up:case Keys.Left:value--;break;case Keys.Down:case Keys.Right:value++;break;case Keys.PageUp:value-=(int)Math.Max(1,r.Page);break;case Keys.PageDown:value+=(int)Math.Max(1,r.Page);break;case Keys.Home:value=r.Min;break;case Keys.End:value=r.End;break;default:base.OnKeyDown(e);return;}target.Move(vertical,value);e.Handled=true;UpdateRange();}
 }
 sealed class Popup:NativeWindow,Target,IDisposable {
  readonly ComboBox owner;readonly AxisBar bar;bool arranging,routing;public bool Gesture;int clippedWidth,clippedHeight;
  public Popup(ComboBox control,IntPtr window){owner=control;AssignHandle(window);bar=new AxisBar(this,true,window);SetWindowLong(window,-16,GetWindowLong(window,-16)|0x02000000);bar.CreateControl();Arrange();}
  public Color Background {get{return owner.BackColor;}}
  public Range Read(bool vertical){return ReadRange(Handle,vertical);}
  public void Move(bool vertical,int value){var r=Read(true);SendMessage(Handle,0x197,(IntPtr)Math.Max(r.Min,Math.Min(r.End,value)),IntPtr.Zero);Arrange();}
  public void Wheel(int delta){routing=true;try{SendMessage(Handle,0x20A,(IntPtr)(delta<<16),IntPtr.Zero);}finally{routing=false;}Arrange();}
  public bool Route(ref Message m){if(routing||bar==null||m.Msg!=0x201&&m.Msg!=0x200&&m.Msg!=0x202)return false;if(!owner.DroppedDown)return false;var point=new PointData{X=(short)(m.LParam.ToInt64()&65535),Y=(short)((m.LParam.ToInt64()>>16)&65535)};ClientToScreen(m.HWnd,ref point);Rect rect;if(!GetWindowRect(bar.Handle,out rect))return false;bool hit=point.X>=rect.Left&&point.X<rect.Right&&point.Y>=rect.Top&&point.Y<rect.Bottom;if(!Gesture&&(m.Msg!=0x201||!hit))return false;routing=true;try{bar.Routed(m.Msg,new Point(point.X,point.Y));if(m.Msg==0x202)Gesture=false;m.Result=IntPtr.Zero;return true;}finally{routing=false;}}
  void Clip(int width,int height){int hidden=((GetWindowLong(Handle,-16)&VerticalStyle)!=0||owner.Items.Count>owner.MaxDropDownItems)?SystemInformation.VerticalScrollBarWidth:0;width-=hidden;if(width<=2||height<=2||width==clippedWidth&&height==clippedHeight)return;clippedWidth=width;clippedHeight=height;IntPtr region=CreateRectRgn(1,1,width-1,height-1);if(SetWindowRgn(Handle,region,true)==0)DeleteObject(region);}
  void Arrange(){if(arranging||Handle==IntPtr.Zero||bar==null)return;arranging=true;try{Rect r;if(GetWindowRect(Handle,out r))Clip(r.Right-r.Left,r.Bottom-r.Top);Rect client;if(GetClientRect(Handle,out client)){int width=Math.Max(8,(int)Math.Round(8*owner.DeviceDpi/96.0));bar.SetBounds(Math.Max(0,client.Right-width),0,width,Math.Max(0,client.Bottom));bar.Visible=Read(true).Scrollable;bar.UpdateRange();}}finally{arranging=false;}}
  protected override void WndProc(ref Message m){if(Route(ref m))return;if(m.Msg==0x46&&!arranging&&m.LParam!=IntPtr.Zero){var pos=(WindowPos)Marshal.PtrToStructure(m.LParam,typeof(WindowPos));if((pos.Flags&1)==0){arranging=true;try{Clip(pos.Width,pos.Height);}finally{arranging=false;}}}base.WndProc(ref m);if(m.Msg==5||m.Msg==0x47||m.Msg==0x115||m.Msg==0x20A||m.Msg==0x100||m.Msg==0xF)Arrange();}
  public void Dispose(){bar.Dispose();ReleaseHandle();}
 }
 [StructLayout(LayoutKind.Sequential)]struct PointData {public int X,Y;}
 [StructLayout(LayoutKind.Sequential)]struct Info {public int Size,Mask,Min,Max;public uint Page;public int Pos,Track;}
 [StructLayout(LayoutKind.Sequential)]struct Rect {public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)]struct ComboInfo {public int Size;public Rect Item,Button;public int State;public IntPtr Combo,Text,List;}
 [StructLayout(LayoutKind.Sequential)]struct WindowPos {public IntPtr Window,After;public int X,Y,Width,Height;public uint Flags;}
 [DllImport("user32.dll")]static extern bool ClientToScreen(IntPtr h,ref PointData point);
 [DllImport("user32.dll")]static extern bool GetScrollInfo(IntPtr h,int bar,ref Info info);
 [DllImport("user32.dll",EntryPoint="GetWindowLongW")]static extern int GetWindowLong(IntPtr h,int index);
 [DllImport("user32.dll",EntryPoint="SetWindowLongW")]static extern int SetWindowLong(IntPtr h,int index,int value);
 [DllImport("user32.dll")]static extern bool GetComboBoxInfo(IntPtr h,ref ComboInfo info);
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out Rect rect);
 [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr h,out Rect rect);
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int message,IntPtr w,IntPtr l);
 [DllImport("gdi32.dll")]static extern int SetMetaRgn(IntPtr dc);
 [DllImport("gdi32.dll")]static extern int SaveDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern bool RestoreDC(IntPtr dc,int saved);
 [DllImport("gdi32.dll")]static extern int IntersectClipRect(IntPtr dc,int left,int top,int right,int bottom);
 [DllImport("gdi32.dll")]static extern IntPtr CreateRectRgn(int left,int top,int right,int bottom);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr value);
 [DllImport("user32.dll")]static extern int SetWindowRgn(IntPtr h,IntPtr region,bool redraw);
}