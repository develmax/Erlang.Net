param($target)
$ErrorActionPreference='Stop'
$src=$PSScriptRoot
Set-Location -LiteralPath $target
$exclude=@('.gitattributes','**/ManagedStdlibRegistration456.cs','**/BeamRegisterInterpreter.cs','**/LibrarySelectionNames.cs','**/RuntimeManagedLibraryStage456.cs','**/ErlangFunctionCallErrors.cs','**/ProcessRuntime.LibrarySelection.cs') | ForEach-Object {"--exclude=$_"}
git apply @exclude "$src/docs/validation/gen-server-port-20261105/source.patch"
if($LASTEXITCODE){throw 'apply failed'}
function Edit($p,$old,$new){$s=[IO.File]::ReadAllText((Resolve-Path $p)); if(!$s.Contains($old)){throw $p}; [IO.File]::WriteAllText((Resolve-Path $p),$s.Replace($old,$new))}
Edit 'src/Otp/Managed/Erlang.Otp.Stdlib/Registration/ManagedStdlibRegistration456.cs' '            default: throw' "            case RuntimeManagedLibraryStage456.GenServer: GenServerModule.Register(r); break;`n            default: throw"
Edit 'src/Runtime/Erlang.Runtime.Core/Modules/LibrarySelectionNames.cs' '        SysLibraryNames.Module' "        GenServerLibraryNames.Module,`n        SysLibraryNames.Module"
Edit 'src/Runtime/Erlang.Runtime.Core/Modules/RuntimeManagedLibraryStage456.cs' "    Io`n" "    Io,`n    GenServer`n"
Edit 'src/Runtime/Erlang.Runtime.Core/Processes/Context/ProcessRuntime.LibrarySelection.cs' '                if (options.BootstrapFileSystem)' "                if (libraries.Modules.Any(choice => choice.Module == GenServerLibraryNames.Module && choice.Backend == RuntimeModuleBackend.Managed))`n                    RuntimeManagedLibraries456.Register(registry, RuntimeManagedLibraryStage456.GenServer);`n                if (options.BootstrapFileSystem)"
Copy-Item "$src/docs/gen-server-backends.md" docs
Copy-Item "$src/tools/Validation/Erlang.GenServerCompatibility/README.md" tools/Validation/Erlang.GenServerCompatibility
New-Item -ItemType Directory -Force docs/validation/gen-server-port-20261105 | Out-Null
Copy-Item "$src/docs/validation/gen-server-port-20261105/README.md" docs/validation/gen-server-port-20261105
Copy-Item "$src/docs/validation/gen-server-port-20261105/final-107" docs/validation/gen-server-port-20261105 -Recurse
[IO.File]::AppendAllText((Resolve-Path '.gitattributes'),"`n/docs/validation/gen-server-port-20261105/** -text`n/tools/Validation/Erlang.GenServerCompatibility/** -text`n")
