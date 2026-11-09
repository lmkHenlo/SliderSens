<#
  Builds SliderSens.dll without the .NET SDK, using the C# compiler that ships with PowerShell 7.

  Requirements: PowerShell 7 (pwsh), the game, and BepInEx 5's core folder (BepInEx.dll + 0Harmony.dll).

  Examples:
    pwsh ./build.ps1
    pwsh ./build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Super Battle Golf" -BepInExCore "C:\path\to\BepInEx\core"
#>
param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Super Battle Golf",
    [string]$ManagedDir = "",
    [string]$BepInExCore = "",
    [string]$OutDir = (Join-Path $PSScriptRoot "bin")
)
$ErrorActionPreference = 'Stop'

if (-not $ManagedDir) { $ManagedDir = Join-Path $GameDir "Super Battle Golf_Data/Managed" }
if (-not $BepInExCore) { $BepInExCore = Join-Path $GameDir "BepInEx/core" }
if (-not (Test-Path (Join-Path $ManagedDir "GameAssembly.dll"))) { throw "GameAssembly.dll not found in '$ManagedDir'. Pass -GameDir or -ManagedDir." }
if (-not (Test-Path (Join-Path $BepInExCore "BepInEx.dll"))) { throw "BepInEx.dll not found in '$BepInExCore'. Pass -BepInExCore (e.g. your r2modman profile's BepInEx/core folder)." }

$pwshDir = Split-Path ([System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName)
$roslyn = Join-Path $pwshDir 'Microsoft.CodeAnalysis.CSharp.dll'
if (-not (Test-Path $roslyn)) { throw "Run this with PowerShell 7 (pwsh), which includes the C# compiler." }
Add-Type -Path (Join-Path $pwshDir 'Microsoft.CodeAnalysis.dll')
Add-Type -Path $roslyn

# The game's own assemblies (its Mono class library included) + BepInEx/Harmony.
$refFiles = @(Get-ChildItem $ManagedDir -Filter *.dll | ForEach-Object FullName) +
            @((Join-Path $BepInExCore 'BepInEx.dll'), (Join-Path $BepInExCore '0Harmony.dll'))
$refs = [Microsoft.CodeAnalysis.MetadataReference[]]@($refFiles | ForEach-Object { [Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($_) })

$parse = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::new([Microsoft.CodeAnalysis.CSharp.LanguageVersion]::CSharp9)
$files = @(Get-ChildItem -Recurse -Filter *.cs (Join-Path $PSScriptRoot 'src') | ForEach-Object FullName)
$trees = [Microsoft.CodeAnalysis.SyntaxTree[]]@($files | ForEach-Object {
    [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($_), $parse, $_) })

$opts = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
$opts = $opts.WithOptimizationLevel([Microsoft.CodeAnalysis.OptimizationLevel]::Release).
              WithNullableContextOptions([Microsoft.CodeAnalysis.NullableContextOptions]::Disable).
              WithDeterministic($true)
$comp = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('SliderSens', $trees, $refs, $opts)

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$out = Join-Path $OutDir 'SliderSens.dll'
$fs = [IO.File]::Create($out)
try { $res = $comp.Emit($fs) } finally { $fs.Dispose() }

# CS1701: BepInEx/Harmony reference mscorlib 2.0, the game ships 4.0. Normal for BepInEx 5 mods.
$diags = @($res.Diagnostics | Where-Object { $_.Severity -ge [Microsoft.CodeAnalysis.DiagnosticSeverity]::Warning -and $_.Id -ne 'CS1701' })
$diags | ForEach-Object { Write-Host $_.ToString() }
if (-not $res.Success) { Remove-Item $out -ErrorAction SilentlyContinue; throw "Build failed." }
Write-Host "Built $out"
