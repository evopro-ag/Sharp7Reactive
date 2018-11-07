Write-Host "Setting .nuspec version tag to $env:appveyor_build_version"

$content = (Get-Content _nuspec\Sharp7.Rx.nuspec) 
$content = $content -replace '\$version\$', $env:appveyor_build_version

$content | Out-File _nuspec\Sharp7.Rx.compiled.nuspec

& _nuspec\NuGet.exe pack _nuspec\Sharp7.Rx.compiled.nuspec