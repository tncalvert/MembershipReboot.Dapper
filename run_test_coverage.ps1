$nuget = Join-Path -Path $env:USERPROFILE -ChildPath '.nuget'

$ocPath = Join-Path -Path $nuget -ChildPath 'packages\OpenCover\4.6.519\tools\OpenCover.Console.exe'
$rgPath = Join-Path -Path $nuget -ChildPath 'packages\ReportGenerator\3.0.2\tools\ReportGenerator.exe'

$outputDir = 'test_coverage'
$outputFile = Join-Path -Path $outputDir -ChildPath 'coverage.xml'
$reportsDir = Join-Path -Path $outputDir -ChildPath 'reports'
$searchDirs = "${pwd}\MembershipReboot.Dapper.Tests\bin\Debug\net452"

if(!(Test-Path -PathType Container $outputDir)) {
	New-Item -ItemType Directory -Path $outputDir | Out-Null
}
if(!(Test-Path -PathType Container $reportsDir)) {
	New-Item -ItemType Directory -Path $reportsDir | Out-Null
}

& $ocPath -target:"C:\Program Files\dotnet\dotnet.exe" -targetargs:"test .\MembershipReboot.Dapper.Tests" -output:$outputFile -filter:"+[MembershipReboot.Dapper]*" -register:user "-searchdirs:$searchDirs"
& $rgPath -reports:$outputFile -targetdir:$reportsDir

Invoke-Item -Path (Join-Path -Path $reportsDir -ChildPath 'index.htm')
