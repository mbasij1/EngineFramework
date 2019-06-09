$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath
$temp = dir -Path "$dir\bin\Release" -Filter "*.nupkg" -Recurse | %{$_.FullName}

foreach($item in $temp)
{
    C:\Users\m.basij\Downloads\nuget.exe push -Source "tadbir package" -ApiKey VSTS $item
}