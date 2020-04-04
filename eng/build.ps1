param(
	[bool] $build = $true,
	[bool] $test = $false,
	[string[]] $configurations = @('Debug', 'Release'),
	[bool] $publish = $false,
	[string] $msBuildVerbosity = 'minimal',
	[string] $nugetApiKey = $null
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptPath = [IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Definition)
$repoPath = Resolve-Path (Join-Path $scriptPath '..')
$slnPath = Get-ChildItem -Path $repoPath -Filter *.sln

function GetXmlPropertyValue($fileName, $propertyName)
{
	$result = Get-Content $fileName |`
		Where-Object {$_ -like "*<$propertyName>*</$propertyName>*"} |`
		ForEach-Object {$_.Replace("<$propertyName>", '').Replace("</$propertyName>", '').Trim()}
	return $result
}

if ($build)
{
	foreach ($configuration in $configurations)
	{
		Write-Host "`nRestoring $configuration packages"
		# Restore NuGet packages first
		# We have to restore the Test project (which pulls in the main and CodeFixes projects) and skip the Vsix project.
		# Vsix is a legacy .csproj format that references Menees.Analyzers as a project and an analyzer,
		# which leads to the following error during a package restore: NuGet.targets(124,5): error : Ambiguous project name 'Menees.Analyzers'
		# This is also mentioned in src\Menees.Analyzers.CodeFixes\Menees.Analyzers.nuspec.
		# See https://github.com/NuGet/Home/issues/6143#issuecomment-343647462
		dotnet restore /p:Configuration=$configuration /v:$msBuildVerbosity "$repoPath\tests\Menees.Analyzers.Test\Menees.Analyzers.Test.csproj"

		Write-Host "`nBuilding $configuration projects"
		msbuild $slnPath /p:Configuration=$configuration /v:$msBuildVerbosity /nologo
	}
}

if ($test)
{
	# Using "dotnet test Analyzers.sln" gets the NuGet restore error mentioned above.
	# So we have to directly run the vstest app against the DLLs we built above.
	foreach ($configuration in $configurations)
	{
		$testDlls = @(Get-ChildItem -r "$repoPath\tests\**\*.Test.dll" | Where-Object {$_.Directory -like "*\bin\$configuration\*"})
		foreach ($testDll in $testDlls)
		{
			write-host "`n`n***** $testDll *****"
			vstest.console.exe $testDll /Platform:X64
		}
	}
}

if ($publish)
{
	$version = GetXmlPropertyValue "$repoPath\src\Directory.Build.props" 'Version'
	$published = $false
	if ($version)
	{
		$artifactsPath = "$repoPath\artifacts"
		if (Test-Path $artifactsPath)
		{
			Remove-Item -Recurse -Force $artifactsPath
		}

		$ignore = mkdir $artifactsPath
		if ($ignore) { } # For PSUseDeclaredVarsMoreThanAssignments

		foreach ($configuration in $configurations)
		{
			if ($configuration -like '*Release*')
			{
				Write-Host "Publishing version $version $configuration files to $artifactsPath"

				$package = "$repoPath\src\Menees.Analyzers.CodeFixes\bin\Release\Menees.Analyzers.$version.nupkg"
				Copy-Item -Path $package -Destination $artifactsPath
				Write-Host "Published" ([IO.Path]::GetFileName($package))

				$vsix = "$repoPath\src\Menees.Analyzers.Vsix\bin\Release\Menees.Analyzers.vsix"
				Copy-Item -Path $vsix -Destination $artifactsPath
				Write-Host "Published" ([IO.Path]::GetFileName($vsix))

				if ($nugetApiKey)
				{
					$artifactPackage = Join-Path $artifactsPath (Split-Path -Leaf $package)
					dotnet nuget push $artifactPackage -k $nugetApiKey -s https://api.nuget.org/v3/index.json --skip-duplicate
					$published = $true
				}
			}
		}
	}

	if ($published)
	{
		Write-Host "`n`n****** REMEMBER TO ADD A GITHUB RELEASE! ******"
	}
}
