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
Add-Type @"
using System;using System.Runtime.InteropServices;
public static class ChromeScrollCheck {
 [DllImport("user32.dll")]public static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
}
"@
function Assert($ok,$message){if(-not $ok){throw $message}}
try{
 $a.GetType('UiPreferences').GetMethod('Apply').Invoke($null,[object[]]@($form))|Out-Null;$form.Show();[Windows.Forms.Application]::DoEvents()
 [void][ChromeScrollCheck]::SendMessage($text.Handle,0xB6,[IntPtr]40,[IntPtr]20)
 Assert ([ChromeScrollCheck]::SendMessage($text.Handle,0xCE,[IntPtr]::Zero,[IntPtr]::Zero).ToInt32() -gt 0) 'Log vertical scroll broke'
 $list.TopIndex=80;Assert ($list.TopIndex -gt 0) 'List scroll broke'
 $panel.AutoScrollPosition=[Drawing.Point]::new(100,600);Assert ($panel.AutoScrollPosition.Y -lt 0) 'Settings panel scroll broke'
 $combo.DroppedDown=$true;[Windows.Forms.Application]::DoEvents();$combo.DroppedDown=$false
 $text.Select(0,0);[void]$list.Focus();foreach($light in $false,$true){$a.GetType('UiPreferences').GetMethod('Set').Invoke($null,[object[]]@($light,$false,$false))|Out-Null;[Windows.Forms.Application]::DoEvents();$bitmap=[Drawing.Bitmap]::new($text.Width,$text.Height);try{$text.DrawToBitmap($bitmap,[Drawing.Rectangle]::new(0,0,$text.Width,$text.Height));if(-not $light){Assert ($bitmap.GetPixel($bitmap.Width-5,5).R -lt 150) 'White vertical arrow box remains';Assert ($bitmap.GetPixel(5,$bitmap.Height-5).R -lt 150) 'White horizontal arrow box remains';Assert ($bitmap.GetPixel($bitmap.Width-3,$bitmap.Height-3).R -lt 150) 'White scrollbar corner remains';Assert ($bitmap.GetPixel($bitmap.Width-1,[int]($bitmap.Height/2)).R -lt 150) 'White outer scrollbar frame remains'};$name=if($light){'light'}else{'dark'};$bitmap.Save((Join-Path (Split-Path (Resolve-Path $Executable).Path) ('all-scrollbars-'+$name+'.local.png')))}finally{$bitmap.Dispose()}}
 'PASS: themed log/list/panel/combo scrollbar chrome, vertical/horizontal scrolling and corners; dark/light captures; no phone or audio actions.'
}finally{$form.Dispose()}