# https://tech.guitarrapc.com/entry/2013/03/12/030306
function Measure-Stopwatch{

    [CmdletBinding()]
    param(
    [parameter(Mandatory=$true)]
    [ScriptBlock]$Command,
    [switch]$Days,
    [switch]$Hours,
    [switch]$Minutes,
    [switch]$Seconds,
    [switch]$Milliseconds
    )

    function Start-InputScriptBlock {
        $sw = New-Object System.Diagnostics.StopWatch

        # Start Stopwatch
        $sw.Start()

        #TargetCommand to measure
        $command.Invoke()

        # Stop Stopwatch
        $sw.Stop()

        #Show Result
        switch ($true){
        $Days {$sw.Elapsed.TotalDays}
        $Hours {$sw.Elapsed.TotalHours}
        $Minutes {$sw.Elapsed.TotalMinutes}
        $Seconds {$sw.Elapsed.TotalSeconds}
        $Milliseconds {$sw.Elapsed.TotalMilliseconds}
        default {$sw.Elapsed}
        }

        #Reset Result
        $sw.Reset()
    }

    Start-InputScriptBlock

}

$Count = 10

$Sum = 0
for ($i = 0; $i -lt $Count; $i++) {
    $Sum += (Measure-Command { Start-Process ".\Bin\App\x86\Release\netcoreapp3.1\WA.Viewer.exe" -ArgumentList "-k" -Wait }).TotalMilliseconds
}
$Avg = $Sum / $Count
Write-Output ("WA.Viewer Loaded Time: " + $Avg.ToString() + "ms")

$Sum = 0
for ($i = 0; $i -lt $Count; $i++) {
    $Sum += (Measure-Command { Start-Process ".\WA.Blank\bin\Release\netcoreapp3.1\WA.Blank.exe" -ArgumentList "-k" -Wait }).TotalMilliseconds
}
$Avg = $Sum / $Count
Write-Output ("WA.Blank Loaded Time: " + $Avg.ToString() + "ms")

$Sum = 0
for ($i = 0; $i -lt $Count; $i++) {
    $Sum += (Measure-Command { Start-Process ".\Bin\App\x86\Release\netcoreapp3.1\WA.Viewer.exe" -ArgumentList "-e" -Wait }).TotalMilliseconds
}
$Avg = $Sum / $Count
Write-Output ("WA.Viewer Loaded & Exit Time: " + $Avg.ToString() + "ms")

$Sum = 0
for ($i = 0; $i -lt $Count; $i++) {
    $Sum += (Measure-Command { Start-Process ".\WA.Blank\bin\Release\netcoreapp3.1\WA.Blank.exe" -ArgumentList "-e" -Wait }).TotalMilliseconds
}
$Avg = $Sum / $Count
Write-Output ("WA.Blank Loaded & Exit Time: " + $Avg.ToString() + "ms")
