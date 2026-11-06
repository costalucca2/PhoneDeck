param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$a=[Reflection.Assembly]::LoadFrom((Resolve-Path $Executable).Path)
$form=[Windows.Forms.Form]::new();$form.ClientSize=[Drawing.Size]::new(760,520)
$grid=[Windows.Forms.TableLayoutPanel]::new();$grid.Dock='Fill';$grid.ColumnCount=2;$grid.RowCount=2;$grid.ColumnStyles.Add([Windows.Forms.ColumnStyle]::new([Windows.Forms.SizeType]::Percent,50))|Out-Null;$grid.ColumnStyles.Add([Windows.Forms.ColumnStyle]::new([Windows.Forms.SizeType]::Percent,50))|Out-Null;$grid.RowStyles.Add([Windows.Forms.RowStyle]::new([Windows.Forms.SizeType]::Percent,50))|Out-Null;$grid.RowStyles.Add([Windows.Forms.RowStyle]::new([Windows.Forms.SizeType]::Percent,50))|Out-Null;$form.Controls.Add($grid)
$text=[Windows.Forms.TextBox]::new();$text.Multiline=$true;$text.ReadOnly=$true;$text.WordWrap=$false;$text.ScrollBars='Both';$text.BorderStyle='None';$text.Dock='Fill';$text.Text=(@(1..80|ForEach-Object {('Synthetic log '+$_+' '+('long segment '*25))})-join "`r`n");$grid.Controls.Add($text,0,0)
$list=[Windows.Forms.ListBox]::new();$list.BorderStyle='None';$list.Dock='Fill';1..100|ForEach-Object {[void]$list.Items.Add('Synthetic item '+$_)};$grid.Controls.Add($list,1,0)
$panel=[Windows.Forms.Panel]::new();$panel.Dock='Fill';$panel.AutoScroll=$true;$panel.AutoScrollMinSize=[Drawing.Size]::new(900,1000);$grid.Controls.Add($panel,0,1)
$combo=[Activator]::CreateInstance($a.GetType('DeckComboBox'));$combo.Dock='Top';$combo.MaxDropDownItems=4;1..30|ForEach-Object {[void]$combo.Items.Add('Synthetic device '+$_)};$combo.SelectedIndex=0;$grid.Controls.Add($combo,1,1)
$surface=$a.GetType('DeckTheme').GetProperty('Surface').GetValue($null,$null);$ink=$a.GetType('DeckTheme').GetProperty('Text').GetValue($null,$null)
foreach($control in @($form,$text,$list,$panel,$grid)){$control.BackColor=$surface;$control.ForeColor=$ink}
Add-Type -ReferencedAssemblies System.Windows.Forms @"
using System;using System.Runtime.InteropServices;
public static class ChromeScrollCheck {
 [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]struct R{public int L,T,Rt,B;}
 [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]struct C{public int Size;public R Item,Button;public int State;public IntPtr Combo,Text,List;}
 delegate bool Callback(IntPtr h,IntPtr p);
 [DllImport("user32.dll")]static extern bool GetComboBoxInfo(IntPtr h,ref C info);
 [DllImport("user32.dll")]static extern bool EnumChildWindows(IntPtr h,Callback callback,IntPtr p);
 [DllImport("user32.dll")]public static extern IntPtr GetParent(IntPtr h);
 public static System.Windows.Forms.Control PopupBar(System.Windows.Forms.ComboBox combo){var info=new C{Size=System.Runtime.InteropServices.Marshal.SizeOf(typeof(C))};if(!GetComboBoxInfo(combo.Handle,ref info))return null;System.Windows.Forms.Control found=null;EnumChildWindows(info.List,(h,p)=>{var c=System.Windows.Forms.Control.FromHandle(h);if(c!=null&&c.Name=="SlimVerticalScroll")found=c;return true;},IntPtr.Zero);return found;}
 [DllImport("user32.dll")]public static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
}
"@
function Assert($ok,$message){if(-not $ok){throw $message}}
try{
 $a.GetType('UiPreferences').GetMethod('Apply').Invoke($null,[object[]]@($form))|Out-Null;$form.Show();[Windows.Forms.Application]::DoEvents()
 [void][ChromeScrollCheck]::SendMessage($text.Handle,0xB6,[IntPtr]40,[IntPtr]20)
 Assert ($text.Parent -ne $grid -and $text.Width -gt $text.Parent.ClientSize.Width -and $text.Height -gt $text.Parent.ClientSize.Height) 'Native scrollbars are not outside the clipping viewport'
 Assert ([ChromeScrollCheck]::SendMessage($text.Handle,0xCE,[IntPtr]::Zero,[IntPtr]::Zero).ToInt32() -gt 0) 'Log vertical scroll broke'
 $list.TopIndex=80;Assert ($list.TopIndex -gt 0) 'List scroll broke'
 $panel.AutoScrollPosition=[Drawing.Point]::new(100,600);Assert ($panel.AutoScrollPosition.Y -lt 0) 'Settings panel scroll broke'
 [void]$combo.Focus();$combo.DroppedDown=$true;[Windows.Forms.Application]::DoEvents();$popupBar=[ChromeScrollCheck]::PopupBar($combo);Assert ($null -ne $popupBar) 'Custom dropdown bar missing';$flags=[Reflection.BindingFlags]'NonPublic,Instance';$grip=$popupBar.GetType().GetProperty('Grip',$flags).GetValue($popupBar,$null);$selected=$combo.SelectedIndex;$popupBar.GetType().GetMethod('OnMouseDown',$flags).Invoke($popupBar,[object[]]@([Windows.Forms.MouseEventArgs]::new('Left',1,4,($grip.Top+3),0)))|Out-Null;$popupBar.GetType().GetMethod('OnMouseMove',$flags).Invoke($popupBar,[object[]]@([Windows.Forms.MouseEventArgs]::new('Left',0,4,($popupBar.Height-2),0)))|Out-Null;$popupBar.GetType().GetMethod('OnMouseUp',$flags).Invoke($popupBar,[object[]]@([Windows.Forms.MouseEventArgs]::new('Left',0,4,($popupBar.Height-2),0)))|Out-Null;[Windows.Forms.Application]::DoEvents();Assert ($combo.DroppedDown -and $combo.SelectedIndex -eq $selected) 'Dropdown scroll closed the popup or changed selection';Assert ([ChromeScrollCheck]::SendMessage([ChromeScrollCheck]::GetParent($popupBar.Handle),0x18E,[IntPtr]::Zero,[IntPtr]::Zero).ToInt32() -gt 0) 'Dropdown drag did not scroll';$combo.DroppedDown=$false
 $text.Select(0,0);[void]$list.Focus();foreach($light in $false,$true){$a.GetType('UiPreferences').GetMethod('Set').Invoke($null,[object[]]@($light,$false,$false))|Out-Null;[Windows.Forms.Application]::DoEvents();$scrollHost=$text.Parent.Parent;$bitmap=[Drawing.Bitmap]::new($scrollHost.Width,$scrollHost.Height);try{$scrollHost.DrawToBitmap($bitmap,[Drawing.Rectangle]::new(0,0,$scrollHost.Width,$scrollHost.Height));if(-not $light){Assert ($bitmap.GetPixel($bitmap.Width-5,5).R -lt 150) 'White vertical arrow box remains';Assert ($bitmap.GetPixel(5,$bitmap.Height-5).R -lt 150) 'White horizontal arrow box remains';Assert ($bitmap.GetPixel($bitmap.Width-3,$bitmap.Height-3).R -lt 150) 'White scrollbar corner remains';Assert ($bitmap.GetPixel($bitmap.Width-1,[int]($bitmap.Height/2)).R -lt 150) 'White outer scrollbar frame remains'};$name=if($light){'light'}else{'dark'};$bitmap.Save((Join-Path (Split-Path (Resolve-Path $Executable).Path) ('all-scrollbars-'+$name+'.local.png')))}finally{$bitmap.Dispose()}}
 'PASS: native bars stay outside clipped viewports; log/list/panel/combo scrolling and dark/light custom controls. No phone or audio actions.'
}finally{$form.Dispose()}