Add-Type -AssemblyName System.Windows.Forms
$f=New-Object Windows.Forms.Form
$f.Text='Teste local de botao - sem chamada'
$b=New-Object Windows.Forms.Button
$b.Text='12345678'; $b.Width=180
$script:n=0
$b.Add_Click({$script:n++; [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'teste-cliques.local.txt'),[string]$script:n)})
$f.Controls.Add($b)
$f.Add_Shown({[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'teste-hwnd.local.txt'),[string]$f.Handle.ToInt64())})
$timer=New-Object Windows.Forms.Timer
$timer.Interval=20000
$timer.Add_Tick({$f.Close()})
$timer.Start()
[void]$f.ShowDialog()


