$nuget = Join-Path -Path $env:USERPROFILE -ChildPath '.nuget'

$ocPath = Join-Path -Path $nuget -ChildPath 'packages\OpenCover\4.6.519\tools\OpenCover.Console.exe'
$rgPath = Join-Path -Path $nuget -ChildPath 'packages\ReportGenerator\2.4.5\tools\ReportGenerator.exe'

$outputDir = 'test_coverage'
$outputFile = Join-Path -Path $outputDir -ChildPath 'coverage.xml'
$reportsDir = Join-Path -Path $outputDir -ChildPath 'reports'

if(!(Test-Path -PathType Container $outputDir)) {
	New-Item -ItemType Directory -Path $outputDir
}
if(!(Test-Path -PathType Container $reportsDir)) {
	New-Item -ItemType Directory -Path $reportsDir
}

& $ocPath -target:"C:\Program Files\dotnet\dotnet.exe" -targetargs:"test .\MembershipReboot.Dapper.Tests" -output:$outputFile -filter:"+[MembershipReboot.Dapper]*" -register:user
& $rgPath -reports:$outputFile -targetdir:$reportsDir

Invoke-Item -Path (Join-Path -Path $reportsDir -ChildPath 'index.htm')
