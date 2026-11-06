using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class ScrollChrome {
 static readonly ConditionalWeakTable<Control,NativeScrollSkin> attached=new ConditionalWeakTable<Control,NativeScrollSkin>();
 public static void Apply(Control control){if(control is SlimHistoryList)return;var scroll=control as ScrollableControl;var text=control as TextBoxBase;if(!(text!=null&&text.Multiline)&&!(control is ListBox)&&!(control is ComboBox)&&!(scroll!=null&&scroll.AutoScroll))return;NativeScrollSkin existing;if(attached.TryGetValue(control,out existing))return;var skin=new NativeScrollSkin(control);attached.Add(control,skin);skin.Attach();}
 sealed class NativeScrollSkin:NativeWindow {
  readonly Control owner;bool drawing;NativeScrollSkin popup;
  public NativeScrollSkin(Control control){owner=control;}
  public void Attach(){owner.HandleCreated+=(s,e)=>Assign();owner.HandleDestroyed+=(s,e)=>{if(popup!=null)popup.ReleaseHandle();ReleaseHandle();};owner.BackColorChanged+=(s,e)=>Paint(IntPtr.Zero);if(owner.IsHandleCreated)Assign();var combo=owner as ComboBox;if(combo!=null)combo.DropDown+=(s,e)=>{var info=new ComboInfo{Size=Marshal.SizeOf(typeof(ComboInfo))};if(GetComboBoxInfo(combo.Handle,ref info)&&info.List!=IntPtr.Zero){if(popup!=null)popup.ReleaseHandle();popup=new NativeScrollSkin(owner);popup.AssignHandle(info.List);popup.Paint(IntPtr.Zero);}};}
  void Assign(){if(Handle!=IntPtr.Zero)ReleaseHandle();AssignHandle(owner.Handle);Paint(IntPtr.Zero);}
  protected override void WndProc(ref Message message){base.WndProc(ref message);switch(message.Msg){case 0x85:case 0xF:case 5:case 0x115:case 0x114:case 0x20A:case 0x100:case 0xA0:case 0x7D:case 0xC:Paint(IntPtr.Zero);break;case 0x317:case 0x318:Paint(message.WParam);break;}}
  void Paint(IntPtr target){if(drawing||Handle==IntPtr.Zero||owner.IsDisposed)return;drawing=true;bool release=target==IntPtr.Zero;IntPtr dc=release?GetWindowDC(Handle):target;try{if(dc==IntPtr.Zero)return;Rect window;if(!GetWindowRect(Handle,out window))return;using(var g=Graphics.FromHdc(dc)){g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;var background=owner.BackColor.A==255?owner.BackColor:DeckTheme.Surface;var vertical=Axis(g,window,-5,true,background);var horizontal=Axis(g,window,-6,false,background);if(vertical.HasValue&&horizontal.HasValue)using(var brush=new SolidBrush(background))g.FillRectangle(brush,new Rectangle(vertical.Value.Left,horizontal.Value.Top,vertical.Value.Width,horizontal.Value.Height));if(vertical.HasValue||horizontal.HasValue)using(var pen=new Pen(background))g.DrawRectangle(pen,0,0,Math.Max(0,window.Right-window.Left-1),Math.Max(0,window.Bottom-window.Top-1));}}catch{}finally{if(release&&dc!=IntPtr.Zero)ReleaseDC(Handle,dc);drawing=false;}}
  Rectangle? Axis(Graphics g,Rect window,int id,bool vertical,Color background){var info=new ScrollInfo{Size=Marshal.SizeOf(typeof(ScrollInfo)),States=new uint[6]};if(!GetScrollBarInfo(Handle,id,ref info)||(info.States[0]&0x18000)!=0)return null;var r=new Rectangle(info.Bar.Left-window.Left,info.Bar.Top-window.Top,info.Bar.Right-info.Bar.Left,info.Bar.Bottom-info.Bar.Top);if(r.Width<=0||r.Height<=0)return null;using(var brush=new SolidBrush(background))g.FillRectangle(brush,r);int length=info.ThumbBottom-info.ThumbTop;if(length>0){int thickness=Math.Max(4,(int)Math.Round(4*owner.DeviceDpi/96.0));var thumb=vertical?new Rectangle(r.Left+(r.Width-thickness)/2,r.Top+info.ThumbTop,thickness,length):new Rectangle(r.Left+info.ThumbTop,r.Top+(r.Height-thickness)/2,length,thickness);if(thumb.Width>=4&&thumb.Height>=4)using(var path=DeckTheme.Round(thumb,2))using(var brush=new SolidBrush(UiPreferences.Light?Color.FromArgb(155,169,187):Color.FromArgb(78,92,111)))g.FillPath(brush,path);}return r;}
 }
 [StructLayout(LayoutKind.Sequential)]struct Rect {public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)]struct ScrollInfo {public int Size;public Rect Bar;public int Line,ThumbTop,ThumbBottom,Reserved;[MarshalAs(UnmanagedType.ByValArray,SizeConst=6)]public uint[] States;}
 [StructLayout(LayoutKind.Sequential)]struct ComboInfo {public int Size;public Rect Item,Button;public int State;public IntPtr Combo,Text,List;}
 [DllImport("user32.dll")]static extern bool GetScrollBarInfo(IntPtr window,int id,ref ScrollInfo info);
 [DllImport("user32.dll")]static extern bool GetComboBoxInfo(IntPtr window,ref ComboInfo info);
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr window,out Rect rect);
 [DllImport("user32.dll")]static extern IntPtr GetWindowDC(IntPtr window);
 [DllImport("user32.dll")]static extern int ReleaseDC(IntPtr window,IntPtr dc);
}