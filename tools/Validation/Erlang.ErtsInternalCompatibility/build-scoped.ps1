param(
    [string]$BaseCommit = '20e02eb941ef3b99cb675827ef0f20663da7c04b',
    [string]$Output = 'artifacts/erts-internal-scoped'
)
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$destination = [IO.Path]::GetFullPath($Output, $repository)
if (-not $destination.StartsWith($repository + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The scoped output must remain inside this repository'
}
if (Test-Path -LiteralPath $destination) { throw 'Use a new scoped output directory' }
New-Item -ItemType Directory -Path $destination | Out-Null
$archive = Join-Path $destination 'base.tar'
$snapshot = Join-Path $destination 'snapshot'
New-Item -ItemType Directory -Path $snapshot | Out-Null
git -C $repository archive --format=tar --output=$archive $BaseCommit src/Runtime src/Otp/Managed src/Otp/Original/Foundation/Erlang.Otp.Erts.Beam Directory.Build.props global.json
if ($LASTEXITCODE -ne 0) { throw 'The pinned base archive failed' }
tar -xf $archive -C $snapshot
if ($LASTEXITCODE -ne 0) { throw 'The scoped extraction failed' }
$owned = @(
    'src/Runtime/Erlang.Runtime.Core/Primitives/ErtsInternal',
    'src/Runtime/Erlang.Runtime.Core/Modules/ModuleRegistry.ErtsInternal.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Context/ProcessRuntime.MultiScheduling.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Context/ProcessRuntime.DirtyNative419.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Context/ProcessRuntime.DirtySchedulerInfo419.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Context/ProcessRuntime.SchedulerWallTimeSampling.cs',
    'src/Runtime/Erlang.Runtime.Core/Scheduling/RuntimeMultiSchedulingNames.cs',
    'src/Runtime/Erlang.Runtime.Core/Scheduling/RuntimeMultiSchedulingLimits.cs',
    'src/Runtime/Erlang.Runtime.Core/Scheduling/RuntimeMultiSchedulingBlocker.cs',
    'src/Runtime/Erlang.Runtime.Core/Scheduling/RuntimeActorDispatcher.MultiScheduling.cs',
    'src/Runtime/Erlang.Runtime.Core/Scheduling/RuntimeActorDispatcher.cs',
    'src/Runtime/Erlang.Runtime.Core/Scheduling/Dirty/DirtyNativeOwner419.MultiScheduling.cs',
    'src/Runtime/Erlang.Runtime.Core/Scheduling/Dirty/DirtyNativeOwner419.cs',
    'src/Runtime/Erlang.Runtime.Core/Scheduling/Dirty/DirtyNativeWorker419.cs',
    'src/Runtime/Erlang.Runtime.Core/Diagnostics/Tracing/SequenceTraceRegistrations.cs',
    'tools/Validation/Erlang.ErtsInternalCompatibility',
    'tools/Validation/Erlang.MultiSchedulingCompatibility'
)
foreach ($relative in $owned) {
    $target = Join-Path $snapshot $relative
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
    $source = Join-Path $repository $relative
    if (Test-Path -LiteralPath $source -PathType Container) {
        New-Item -ItemType Directory -Path $target -Force | Out-Null
        foreach ($file in Get-ChildItem -LiteralPath $source -File -Recurse) {
            $child = [IO.Path]::GetRelativePath($source, $file.FullName)
            if ($child.Split([IO.Path]::DirectorySeparatorChar) | Where-Object { $_ -in @('bin', 'obj') }) { continue }
            $childTarget = Join-Path $target $child
            New-Item -ItemType Directory -Path (Split-Path $childTarget -Parent) -Force | Out-Null
            Copy-Item -LiteralPath $file.FullName -Destination $childTarget
        }
    }
    else {
        Copy-Item -LiteralPath $source -Destination $target
    }
}
function Replace-Once([string]$Relative, [string]$Before, [string]$After) {
    $path = Join-Path $snapshot $Relative
    $text = [IO.File]::ReadAllText($path).Replace("`r`n", "`n")
    if ($text.Split(@($Before), [StringSplitOptions]::None).Count -ne 2) {
        throw "The pinned integration anchor changed: $Relative"
    }
    [IO.File]::WriteAllText($path, $text.Replace($Before, $After), [Text.UTF8Encoding]::new($false))
}
Replace-Once 'src/Runtime/Erlang.Runtime.Core/Modules/LibrarySelectionNames.cs' `
    '        ErlangModuleNames.Module,' "        ErlangModuleNames.Module,`n        ErtsInternalContract.Module,"
$anchor = '                RuntimeNativePrimitiveProviders.Register(runtime, registry, options.NativePrimitives);'
$installation = @'
                RuntimeNativePrimitiveProviders.Register(runtime, registry, options.NativePrimitives);
                RuntimeModuleSelection? internalChoice = libraries.Modules.SingleOrDefault(choice => choice.Module == ErtsInternalContract.Module);
                if (internalChoice is not null)
                {
                    registry.InstallErtsInternalLibrary(internalChoice.Backend == RuntimeModuleBackend.Managed);
                }
'@
Replace-Once 'src/Runtime/Erlang.Runtime.Core/Processes/Context/ProcessRuntime.LibrarySelection.cs' $anchor $installation.Replace("`r`n", "`n")
$anchor = '        BeamLinkedFunction target = image.Code.Functions.Single(value => value.Name == function && value.Arity == arguments.Length);'
$dispatch = @'
        if (managedErtsInternalLibrary)
        {
            return Call(caller, StatisticsTrapNames.Module, function, arguments);
        }
        BeamLinkedFunction target = image.Code.Functions.Single(value => value.Name == function && value.Arity == arguments.Length);
'@
Replace-Once 'src/Runtime/Erlang.Runtime.Core/Modules/ModuleRegistry.StatisticsTrap.cs' $anchor $dispatch.Replace("`r`n", "`n")
[IO.File]::WriteAllText((Join-Path $destination 'base-commit.txt'), $BaseCommit)
dotnet build (Join-Path $snapshot 'tools/Validation/Erlang.ErtsInternalCompatibility/Erlang.ErtsInternalCompatibility.csproj') -m:1 --nologo
if ($LASTEXITCODE -ne 0) { throw 'The scoped build failed' }
dotnet build (Join-Path $snapshot 'tools/Validation/Erlang.MultiSchedulingCompatibility/Erlang.MultiSchedulingCompatibility.csproj') -m:1 --nologo
if ($LASTEXITCODE -ne 0) { throw 'The physical scheduling validator build failed' }
