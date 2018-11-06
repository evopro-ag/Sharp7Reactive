$version = [System.Reflection.Assembly]::LoadFile("C:\projects\Sharp7Reactive\Sharp7.Rx\bin\Release\netstandard2.0\Sharp7.Rx.dll").GetName().Version
$versionStr = "{0}.{1}.{2}" -f ($version.Major, $version.Minor, $version.Build)

Write-Host "Setting .nuspec version tag to $versionStr"

$content = (Get-Content _nuspec\Sharp7.Rx.nuspec) 
$content = $content -replace '\$version\$',$versionStr

$content | Out-File _nuspec\Sharp7.Rx.compiled.nuspec

& _nuspec\NuGet.exe pack _nuspec\Sharp7.Rx.compiled.nuspec