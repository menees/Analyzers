param(
	[bool] $build = $true,
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
		# Restore NuGet packages first
		# For information on the following error: NuGet.targets(124,5): error : Ambiguous project name 'Menees.Analyzers'
		# See https://github.com/NuGet/Home/issues/6143#issuecomment-343647462
		# This is also mentioned in src\Menees.Analyzers.CodeFixes\Menees.Analyzers.nuspec.
		msbuild $slnPath /p:Configuration=$configuration /t:Restore /v:$msBuildVerbosity /nologo
		msbuild $slnPath /p:Configuration=$configuration /v:$msBuildVerbosity /nologo
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
