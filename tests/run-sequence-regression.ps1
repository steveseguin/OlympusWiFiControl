$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$production = Get-Content -Raw -LiteralPath (Join-Path $root 'Application/class/UDPInputStream.cs')
$regression = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'SequenceRegression.cs')
Add-Type -TypeDefinition ($production + [Environment]::NewLine + $regression) -Language CSharp
[DebugShellTest.SequenceRegression]::Run()
