using System;using System.Runtime.InteropServices;using System.Drawing;using System.Diagnostics;using System.Windows.Forms;using QRCoder;
sealed class PhoneSetupForm:Form {
 LocalSetupServer server;readonly PictureBox qr=new PictureBox();readonly Label state=new Label(),address=new Label();readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
 public PhoneSetupForm(){Text="PhoneDeck · Conectar celular";BackColor=DeckTheme.Bg;ForeColor=DeckTheme.Text;Font=new Font("Segoe UI",10);ClientSize=new Size(720,515);FormBorderStyle=FormBorderStyle.None;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
  var title=DeckTheme.Label("Conecte seu celular",23,DeckTheme.Text,true);title.SetBounds(26,24,660,45);Controls.Add(title);var subtitle=DeckTheme.Label("Escaneie o QR na mesma rede Wi-Fi. Sem instalar app no celular.",10,DeckTheme.Dim);subtitle.SetBounds(28,80,660,30);Controls.Add(subtitle);
  qr.SetBounds(28,128,260,260);qr.SizeMode=PictureBoxSizeMode.Zoom;qr.BackColor=Color.White;Controls.Add(qr);var instructions=DeckTheme.Label("1   Abra a câmera do celular\n\n2   Acesse o guia local\n\n3   Confirme o Bluetooth nos dois aparelhos",12,DeckTheme.Text,true);instructions.SetBounds(321,132,365,164);Controls.Add(instructions);
  state.ForeColor=DeckTheme.Dim;state.SetBounds(321,309,365,70);state.Text="Preparando página local…";Controls.Add(state);address.ForeColor=DeckTheme.Dim;address.SetBounds(28,403,660,27);address.AutoEllipsis=true;Controls.Add(address);
  var bluetooth=new DeckButton{Text="Bluetooth do Windows",Fill=DeckTheme.Blue};bluetooth.SetBounds(28,448,230,40);bluetooth.Click+=(s,e)=>Process.Start(new ProcessStartInfo("ms-settings:bluetooth"){UseShellExecute=true});Controls.Add(bluetooth);var local=new DeckButton{Text="Abrir guia neste PC"};local.SetBounds(272,448,210,40);local.Click+=(s,e)=>{if(server!=null)Process.Start(new ProcessStartInfo(server.Url){UseShellExecute=true});};Controls.Add(local);var close=new DeckButton{Text="Fechar"};close.SetBounds(496,448,194,40);close.Click+=(s,e)=>Close();Controls.Add(close);
  foreach(Control control in Controls)control.Top+=50;
  ClientSize=new Size(720,565);
  var bar=new Panel{BackColor=DeckTheme.Surface};bar.SetBounds(0,0,720,50);Controls.Add(bar);
  var brand=DeckTheme.Label("\uE7F6",17,DeckTheme.Blue);brand.Font=new Font("Segoe MDL2 Assets",17);brand.SetBounds(18,10,30,30);bar.Controls.Add(brand);
  var caption=DeckTheme.Label("PhoneDeck  /  Conectar celular",11,DeckTheme.Text,true);caption.SetBounds(54,14,520,26);bar.Controls.Add(caption);
  var topClose=new DeckButton{Text="×",Fill=DeckTheme.Surface,Font=new Font("Segoe UI",15),AccessibleName="Fechar assistente"};topClose.SetBounds(667,7,42,36);topClose.Click+=(s,e)=>Close();bar.Controls.Add(topClose);
  MouseEventHandler drag=(s,e)=>{if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);}};bar.MouseDown+=drag;brand.MouseDown+=drag;caption.MouseDown+=drag;
  timer.Interval=700;timer.Tick+=(s,e)=>{if(server==null)return;if(server.Expired){state.Text="QR expirado. Feche e abra o assistente para gerar outro.";state.ForeColor=DeckTheme.Dim;server.Dispose();server=null;}else if(server.GuideOpened){state.Text="Guia aberto. Confirme o pareamento Bluetooth nos aparelhos.";state.ForeColor=DeckTheme.Green;}};Shown+=(s,e)=>Start();FormClosed+=(s,e)=>{timer.Dispose();if(server!=null)server.Dispose();if(qr.Image!=null)qr.Image.Dispose();};
 }
 [DllImport("user32.dll")]static extern bool ReleaseCapture();
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr wp,IntPtr lp);
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.Style &= ~0x00C00000;return p;}}
 protected override void WndProc(ref Message m){if(m.Msg==0x83||m.Msg==0x85){m.Result=IntPtr.Zero;return;}if(m.Msg==0x86){m.Result=(IntPtr)1;return;}base.WndProc(ref m);}
 void Start(){try{server=new LocalSetupServer(LocalSetupServer.LanAddress(),40521);using(var generator=new QRCodeGenerator())using(var data=generator.CreateQrCode(server.Url,QRCodeGenerator.ECCLevel.Q))using(var code=new QRCode(data)){qr.Image=code.GetGraphic(8);}address.Text=server.Url;state.Text="Aguardando abertura do guia.\nO QR expira em 10 minutos.";timer.Start();}catch(Exception e){state.Text=e.Message+"\nSe outra janela de configuração estiver aberta, feche-a.";}}
}
