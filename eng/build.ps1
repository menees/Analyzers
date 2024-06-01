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
		# Note: We can't use "dotnet build" because we have a Vsix project that transitively uses
		# CodeTaskFactory (for SetVsSDKEnvironmentVariables) via Microsoft.VSSDK.BuildTools.targets.
		# The .NET Core version of MSBuild doesn't support CodeTaskFactory, so we're stuck with MSBuild.
		# We need to keep the Vsix project for easy debugging and testing of the analyzers in VS.
		#
		# Restore NuGet packages first
		Write-Host "`nRestoring $configuration packages"
		msbuild $slnPath /p:Configuration=$configuration /v:$msBuildVerbosity /nologo /t:Restore
		Write-Host "`nBuilding $configuration projects"
		msbuild $slnPath /p:Configuration=$configuration /v:$msBuildVerbosity /nologo
	}
}

if ($test)
{
	foreach ($configuration in $configurations)
	{
		dotnet test $slnPath /p:Configuration=$configuration /v:$msBuildVerbosity /nologo
	}
}

if ($publish)
{
	$version = GetXmlPropertyValue "$repoPath\src\Directory.Build.props" 'Version'
	$published = $false
	if ($version)
	{
		$vsixPath = "$repoPath\src\Menees.Analyzers.Vsix\source.extension.vsixmanifest"
		if (Test-Path $vsixPath)
		{
			$pattern = [Text.RegularExpressions.Regex]::Escape("Id=`"Menees.Analyzers.Vsix`" Version=`"$version`"")
			if (!(Get-Content $vsixPath | Select-String $pattern))
			{
				throw "Unable to find Version=`"$version`" in the .vsix manifest. The manifest and Directory.Build.props are out of sync."
			}
		}

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

				$vsixSource = "$repoPath\src\Menees.Analyzers.Vsix\bin\Release\netstandard2.0\Menees.Analyzers.Vsix.vsix"
				$vsixTarget = "$artifactsPath\Menees.Analyzers.vsix"
				Copy-Item -Path $vsixSource -Destination $vsixTarget
				Write-Host "Published" ([IO.Path]::GetFileName($vsixTarget))

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
