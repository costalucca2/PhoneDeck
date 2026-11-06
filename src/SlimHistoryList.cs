using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

sealed class SlimHistoryList:ListView {
 HeaderSkin headerSkin;readonly SlimScroll thumb;bool updating,updateQueued;int wheelRemainder;
 [DllImport("user32.dll")]static extern bool ShowScrollBar(IntPtr window,int bar,bool show);
 [DllImport("user32.dll",EntryPoint="GetWindowLongW")]static extern int GetWindowLong(IntPtr window,int index);
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr w,IntPtr l);
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr window,out NativeBounds rect);
 [StructLayout(LayoutKind.Sequential)]struct NativeBounds {public int Left,Top,Right,Bottom;}
 public SlimHistoryList(){DoubleBuffered=true;thumb=new SlimScroll(this){Name="HistoryScroll",AccessibleName="Rolar histórico",AccessibleRole=AccessibleRole.ScrollBar};Controls.Add(thumb);}
 internal int Page {get{return IsHandleCreated?Math.Max(1,SendMessage(Handle,0x1028,IntPtr.Zero,IntPtr.Zero).ToInt32()):1;}}
 internal int First {get{try{return TopItem==null?0:TopItem.Index;}catch{return 0;}}}
 internal int Last {get{return Math.Max(0,Items.Count-Page);}}
 public void ScrollToRow(int row){if(!IsHandleCreated||Items.Count==0)return;TopItem=Items[Math.Max(0,Math.Min(Last,row))];UpdateScroll();}
 protected override CreateParams CreateParams {get{var p=base.CreateParams;p.Style &= ~0x00300000;return p;}}
 protected override void WndProc(ref Message message){if(message.Msg==0x7C&&unchecked((int)message.WParam.ToInt64())==-16&&message.LParam!=IntPtr.Zero){base.WndProc(ref message);Marshal.WriteInt32(message.LParam,4,Marshal.ReadInt32(message.LParam,4)&~0x00300000);return;}if(message.Msg==0x20A){wheelRemainder+=(short)(message.WParam.ToInt64()>>16);int steps=wheelRemainder/120;wheelRemainder%=120;int lines=SystemInformation.MouseWheelScrollLines;if(steps!=0&&lines!=0)ScrollToRow(First-steps*(lines<0?Page:lines));message.Result=IntPtr.Zero;return;}base.WndProc(ref message);if((message.Msg==0x317||message.Msg==0x318)&&message.WParam!=IntPtr.Zero)PaintHeaderGap(message.WParam);if(message.Msg==5||message.Msg==0x115||message.Msg==0x20A||message.Msg==0x100||message.Msg==0x1014||message.Msg==0x1007||message.Msg==0x104D||message.Msg==0x1008||message.Msg==0x1009||message.Msg==0x102F||message.Msg==0x101E||message.Msg==0xB){UpdateScroll();if((message.Msg==0x1009||message.Msg==0x1008||message.Msg==0x1007||message.Msg==0x104D||message.Msg==0xB)&&!updateQueued&&IsHandleCreated&&!IsDisposed){updateQueued=true;BeginInvoke((Action)(()=>{updateQueued=false;UpdateScroll();}));}}}
 void UpdateScroll(){if(updating||thumb==null||!IsHandleCreated||IsDisposed)return;updating=true;try{if((GetWindowLong(Handle,-16)&0x00300000)!=0)ShowScrollBar(Handle,3,false);int header=Font.Height+12;NativeBounds r;IntPtr h=SendMessage(Handle,0x101F,IntPtr.Zero,IntPtr.Zero);if(h!=IntPtr.Zero&&GetWindowRect(h,out r))header=r.Bottom-r.Top;int width=Math.Max(8,(int)Math.Round(8*DeviceDpi/96.0));thumb.SetBounds(ClientSize.Width-width-1,header+3,width,Math.Max(0,ClientSize.Height-header-6));thumb.Visible=Items.Count>Page&&thumb.Height>0;thumb.Invalidate();}finally{updating=false;}}
 void PaintHeaderGap(IntPtr dc){int right=0;foreach(ColumnHeader column in Columns)right+=column.Width;IntPtr header=SendMessage(Handle,0x101F,IntPtr.Zero,IntPtr.Zero);NativeBounds r;if(dc!=IntPtr.Zero&&header!=IntPtr.Zero&&GetWindowRect(header,out r)&&right<r.Right-r.Left)using(var g=Graphics.FromHdc(dc))using(var brush=new SolidBrush(BackColor))g.FillRectangle(brush,new Rectangle(right,0,r.Right-r.Left-right,r.Bottom-r.Top));}
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);IntPtr h=SendMessage(Handle,0x101F,IntPtr.Zero,IntPtr.Zero);if(h!=IntPtr.Zero){headerSkin=new HeaderSkin(this);headerSkin.AssignHandle(h);}}
 protected override void OnHandleDestroyed(EventArgs e){if(headerSkin!=null){headerSkin.ReleaseHandle();headerSkin=null;}base.OnHandleDestroyed(e);}
 sealed class HeaderSkin:NativeWindow {
  readonly SlimHistoryList list;bool painting;
  [DllImport("user32.dll")]static extern IntPtr GetDC(IntPtr window);
  [DllImport("user32.dll")]static extern int ReleaseDC(IntPtr window,IntPtr dc);
  public HeaderSkin(SlimHistoryList control){list=control;}
 
 protected override void WndProc(ref Message message){if(message.Msg==0x7C&&unchecked((int)message.WParam.ToInt64())==-16&&message.LParam!=IntPtr.Zero){base.WndProc(ref message);Marshal.WriteInt32(message.LParam,4,Marshal.ReadInt32(message.LParam,4)&~0x00300000);return;}base.WndProc(ref message);if(painting||list.IsDisposed)return;if(message.Msg==0xF||message.Msg==0x14||message.Msg==0x317||message.Msg==0x318){painting=true;bool own=message.Msg!=0x317&&message.Msg!=0x318;IntPtr dc=own?GetDC(Handle):message.WParam;try{int right=0;foreach(ColumnHeader column in list.Columns)right+=column.Width;NativeBounds r;if(dc!=IntPtr.Zero&&GetWindowRect(Handle,out r)&&right<r.Right-r.Left)using(var g=Graphics.FromHdc(dc))using(var brush=new SolidBrush(list.BackColor))g.FillRectangle(brush,new Rectangle(right,0,r.Right-r.Left-right,r.Bottom-r.Top));}finally{if(own&&dc!=IntPtr.Zero)ReleaseDC(Handle,dc);painting=false;}}}
 }
 sealed class SlimScroll:Control {
  readonly SlimHistoryList list;bool hover,drag;int offset;
  public SlimScroll(SlimHistoryList owner){list=owner;TabStop=true;Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable,true);}
  Rectangle Grip {get{int height=Math.Min(Height,Math.Max(24,(int)((long)Height*list.Page/Math.Max(1,list.Items.Count))));int y=list.Last==0?0:(int)((long)(Height-height)*list.First/list.Last);return new Rectangle(hover||drag?1:2,y,Math.Max(2,Width-(hover||drag?2:4)),height);}}
  protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(list.BackColor);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;var r=Grip;if(r.Height<4)return;using(var shape=DeckTheme.Round(r,Math.Min(2,r.Width/2)))using(var brush=new SolidBrush(hover||drag?DeckTheme.Dim:UiPreferences.Light?Color.FromArgb(169,181,196):Color.FromArgb(78,92,111)))e.Graphics.FillPath(brush,shape);}
  protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;Focus();var r=Grip;if(r.Contains(e.Location)){drag=true;offset=e.Y-r.Top;Capture=true;}else list.ScrollToRow(list.First+(e.Y<r.Top?-list.Page:list.Page));Invalidate();}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(drag){int span=Height-Grip.Height;if(span>0)list.ScrollToRow((int)Math.Round((double)(e.Y-offset)*list.Last/span));}}
  protected override void OnMouseUp(MouseEventArgs e){drag=false;Capture=false;Invalidate();base.OnMouseUp(e);}
  protected override void OnMouseCaptureChanged(EventArgs e){if(!Capture)drag=false;base.OnMouseCaptureChanged(e);}
  protected override void OnMouseWheel(MouseEventArgs e){int lines=SystemInformation.MouseWheelScrollLines;list.ScrollToRow(list.First-Math.Sign(e.Delta)*(lines<0?list.Page:Math.Max(1,lines)));base.OnMouseWheel(e);}
  protected override bool IsInputKey(Keys key){return key==Keys.Up||key==Keys.Down||key==Keys.PageUp||key==Keys.PageDown||key==Keys.Home||key==Keys.End||base.IsInputKey(key);}
  protected override void OnKeyDown(KeyEventArgs e){int target=list.First;switch(e.KeyCode){case Keys.Up:target--;break;case Keys.Down:target++;break;case Keys.PageUp:target-=list.Page;break;case Keys.PageDown:target+=list.Page;break;case Keys.Home:target=0;break;case Keys.End:target=list.Last;break;default:base.OnKeyDown(e);return;}list.ScrollToRow(target);e.Handled=true;}
 }
}