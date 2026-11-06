param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$a=[Reflection.Assembly]::LoadFrom((Resolve-Path $Executable).Path)
$type=$a.GetType('SlimHistoryList');$view=[Activator]::CreateInstance($type)
$form=[Windows.Forms.Form]::new();$form.ClientSize=[Drawing.Size]::new(440,260);$view.Dock='Fill';$view.View='Details';$view.BorderStyle='None';$view.BackColor=$a.GetType('DeckTheme').GetProperty('Surface').GetValue($null,$null);$view.ForeColor=$a.GetType('DeckTheme').GetProperty('Text').GetValue($null,$null);$view.OwnerDraw=$true;$view.add_DrawColumnHeader({param($sender,$e) $e.Graphics.Clear($sender.BackColor);[Windows.Forms.TextRenderer]::DrawText($e.Graphics,$e.Header.Text,$sender.Font,$e.Bounds,$sender.ForeColor)});$view.add_DrawItem({param($sender,$e) $e.DrawDefault=$true});$view.add_DrawSubItem({param($sender,$e) $e.DrawDefault=$true});$view.Columns.Add('CONTACT',220)|Out-Null;$view.Columns.Add('TIME',150)|Out-Null;$form.Controls.Add($view)
$flags=[Reflection.BindingFlags]'Instance,NonPublic'
Add-Type @"
using System;using System.Runtime.InteropServices;
public static class HistoryScrollCheck {
 [DllImport("user32.dll",EntryPoint="GetWindowLongW")]public static extern int Style(IntPtr h,int index);
 [DllImport("user32.dll")]public static extern IntPtr SendMessage(IntPtr h,int message,IntPtr w,IntPtr l);
}
"@
function Assert($ok,$text){if(-not $ok){throw $text}}
try{
 foreach($i in 1..100){$item=[Windows.Forms.ListViewItem]::new('Synthetic contact '+$i);$item.SubItems.Add('12:34:56')|Out-Null;$view.Items.Add($item)|Out-Null}
 $form.Show();[Windows.Forms.Application]::DoEvents();$bar=$view.Controls.Find('HistoryScroll',$true)[0]
 Assert ($bar.Visible -and $bar.Width -le 16) 'Slim scrollbar missing or too wide'
 Assert (([HistoryScrollCheck]::Style($view.Handle,-16) -band 0x00300000) -eq 0) 'Native white scrollbar still present'
 [void][HistoryScrollCheck]::SendMessage($view.Handle,0x20A,[IntPtr](-120 -shl 16),[IntPtr]::Zero);[Windows.Forms.Application]::DoEvents()
 Assert ($view.TopItem.Index -gt 0) 'Mouse wheel stopped working'
 $type.GetMethod('ScrollToRow').Invoke($view,[object[]]@(0))|Out-Null
 $grip=$bar.GetType().GetProperty('Grip',$flags).GetValue($bar,$null)
 $bar.GetType().GetMethod('OnMouseDown',$flags).Invoke($bar,[object[]]@([Windows.Forms.MouseEventArgs]::new('Left',1,4,($grip.Top+3),0)))|Out-Null
 $bar.GetType().GetMethod('OnMouseMove',$flags).Invoke($bar,[object[]]@([Windows.Forms.MouseEventArgs]::new('Left',0,4,$bar.Height,0)))|Out-Null
 $bar.GetType().GetMethod('OnMouseUp',$flags).Invoke($bar,[object[]]@([Windows.Forms.MouseEventArgs]::new('Left',0,4,$bar.Height,0)))|Out-Null
 Assert ($view.TopItem.Index -gt 80 -and $view.Items[99].Bounds.Bottom -le $view.ClientSize.Height) 'Dragging cannot reach the last record'
 $bar.GetType().GetMethod('OnKeyDown',$flags).Invoke($bar,[object[]]@([Windows.Forms.KeyEventArgs]::new('Home')))|Out-Null
 Assert ($view.TopItem.Index -eq 0) 'Keyboard Home failed'
 foreach($light in $false,$true){$a.GetType('UiPreferences').GetMethod('Set').Invoke($null,[object[]]@($light,$false,$false))|Out-Null;[Windows.Forms.Application]::DoEvents();$bitmap=[Drawing.Bitmap]::new($view.Width,$view.Height);try{$view.DrawToBitmap($bitmap,$view.ClientRectangle);if(-not $light){Assert ($bitmap.GetPixel($bitmap.Width-3,5).R -lt 150) "White header filler remains"};$name=if($light){'light'}else{'dark'};$bitmap.Save((Join-Path (Split-Path (Resolve-Path $Executable).Path) ('history-scroll-'+$name+'.local.png')))}finally{$bitmap.Dispose()}}
 $view.Items.Clear();[Windows.Forms.Application]::DoEvents();Assert (-not $bar.Visible) 'Empty history retained a scrollbar'
 'PASS: slim themed scrollbar; native arrows removed; wheel, drag to final row, keyboard and empty state; dark/light images. Synthetic rows only.'
}finally{$form.Dispose()}