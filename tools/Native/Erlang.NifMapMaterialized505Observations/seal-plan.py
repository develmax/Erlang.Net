"""Seal source files only. This command starts no compiler or native fixture."""
import argparse
import hashlib
import json
from pathlib import Path

TOOL = 'tools/Native/Erlang.NifMapMaterialized505Observations'
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', type=Path, required=True)
    args = parser.parse_args()
    root, tool = args.repository.resolve(), Path(__file__).resolve().parent
    if tool != root / TOOL:
        raise RuntimeError('source root mismatch')
    helpers = 'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/'
    decoder = 'tools/Native/Erlang.NifMapFamily504Observations/diagnostic_etf.py'
    previous = json.loads((root / 'tools/Native/Erlang.NifMapFamily504Observations/source-plan.json').read_bytes())
    paths = ['README.md','collect.py','build-native.py','seal-plan.py','map_postreturn505.c','map_postreturn505.erl']
    dependency_paths = [helpers + 'owned.py', helpers + 'bounded_inventory.py', decoder]
    dependency_paths += [path.relative_to(root).as_posix()
        for path in sorted((root / 'tools/Native/Erlang.NifNativeBackend').glob('MapMaterialized505*.cs'))]
    dependency_paths += ['tools/Native/Erlang.NifNativeBackend/NativeOwned516Profile.cs',
        'tools/Native/Erlang.NifNativeBackend/NativeOwned516ProfileNames.cs']
    dependency_paths += ['src/Runtime/Erlang.Runtime.Core/Native/Nif/NifNativeCallEnvironment.cs', 'src/Runtime/Erlang.Runtime.Core/Native/Nif/NifNativeCallEnvironment.OwnedRoots516.cs', 'src/Runtime/Erlang.Runtime.Core/Native/Nif/NifNativeEnvironmentCallbacks.cs', 'src/Runtime/Erlang.Runtime.Core/Native/Nif/NifNativeLibraryOwner.cs', 'src/Runtime/Erlang.Runtime.Core/Native/Nif/NifNativeTermCopy504.cs']
    dependency_paths.append('tools/Native/Erlang.NifNativeBackend/Program.cs')
    dependency_rows = []
    for name in dependency_paths:
        raw = (root / name).read_bytes().replace(b'\r\n', b'\n')
        dependency_rows.append(dict(Path=name,HashMode='TextLF',Bytes=len(raw),Sha256=hashlib.sha256(raw).hexdigest().upper()))
    plan = dict(PinnedCommit=PIN,Platform='windows-x64',Status='SOURCE_SEALED_EXECUTION_TRACKED_BY_EXTERNAL_RECEIPTS',
        ProtocolCaseCount=32,Cases=[dict(Count=n,Bytes=size,Target=target)
            for n in [2,32,33,65] for size in [64,65] for target in [1,128,129,143]],
        Headers=previous['Headers'],NativeSourcePath=TOOL+'/map_postreturn505.c',
        NativeSourceSha256=sha(tool/'map_postreturn505.c'),ErlangSourcePath=TOOL+'/map_postreturn505.erl',
        ErlangSourceSha256=sha(tool/'map_postreturn505.erl'),BuildScriptSha256=sha(tool/'build-native.py'),
        CollectorSha256=sha(tool/'collect.py'),
        Files=[dict(Path=TOOL+'/'+name,HashMode='Raw',Bytes=(tool/name).stat().st_size,Sha256=sha(tool/name)) for name in paths],
        Dependencies=dependency_rows,OwnedHelper=helpers+'owned.py',InventoryHelper=helpers+'bounded_inventory.py',Decoder=decoder,
        SerializationProfiles=dict(CanonicalOptions=['deterministic',dict(minor_version=2)],DefaultOptions=[dict(minor_version=2)]),
        FreshVmSafetyRequired=32,OriginalProviderFiles=6427,WholeApiCompatibilityClaimed=False)
    # A golden is optional and can only be supplied after actual original batch
    # acquisition. Resealing preserves its independent captured-file hashes.
    for field, name in [('OriginalGoldenSha256','original-values.etf'),('OriginalDefaultGoldenSha256','original-values-default.etf')]:
        if (tool / name).is_file():
            plan[field] = sha(tool / name)
    raw = (json.dumps(plan,indent=2)+'\n').encode('utf-8')
    (tool/'source-plan.json').write_bytes(raw)
    print(hashlib.sha256(raw).hexdigest().upper())


if __name__ == '__main__':
    main()
