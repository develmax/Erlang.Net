"""Seal final existing runtime/source inputs; does not build or run a VM."""
import argparse
import json
from pathlib import Path
import re
import shutil
import subprocess
from common import PACKAGE, PIN, HELPER, PASSIVE, sha, save, require, snapshot


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--managed-host', type=Path, required=True)
    parser.add_argument('--runtime-version', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--adapter-build-receipt', type=Path, required=True)
    parser.add_argument('--adapter-build-receipt-sha256', required=True)
    args = parser.parse_args()
    repo, host, output = args.repository.resolve(), args.managed_host.resolve(), args.output.resolve()
    require(host.is_file() and host.is_relative_to(repo), 'Existing actual native host DLL required')
    require(output.is_relative_to(repo / 'artifacts') and not output.exists(), 'Fresh plan inside repository artifacts required')
    proof_path = args.adapter_build_receipt.resolve()
    require(sha(proof_path) == args.adapter_build_receipt_sha256.upper(), 'Adapter build proof differs')
    proof = json.loads(proof_path.read_bytes())
    source = repo / 'src/Runtime/Erlang.Runtime.Core/Native/Nif/native/windows_varargs526.c'
    header = repo / 'tools/Native/Erlang.NifNativeBackend/native/nif_snprintf791.h'
    build_script = repo / 'tools/Native/Erlang.NifNativeBackend/build-windows-varargs526.py'
    require(proof['Status'] == 'WINDOWS_VARARGS526_BUILD_COMPLETE'
            and proof['SourceSha256'] == proof['CompiledSourceSha256'] == sha(source)
            and proof['FormatHeaderSha256'] == sha(header)
            and proof['BuildScriptSha256'] == sha(build_script)
            and proof['AdapterSha256'] == sha(host.parent / 'Erlang.Nif.WindowsVariadic.dll')
            and all(row['ExitCode'] == 0 for row in proof['Commands']), 'Adapter/source/header/build closure mismatch')
    subjects = json.loads((PACKAGE / 'subjects.json').read_bytes())
    inputs = []
    package_files = [*PACKAGE.glob('*.py'), PACKAGE / 'subjects.json', PACKAGE / 'README.md',
                     *sorted((PACKAGE / 'fixtures').glob('*'))]
    repo_files = [repo / HELPER, repo / PASSIVE,
                 repo / 'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py',
                 repo / 'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/bounded_inventory.py',
                 repo / 'tools/Native/Erlang.NifVariadic526Observations/source-plan.json',
                 repo / 'tools/Native/Erlang.NifNativeBackend/NifDirty521Driver.cs',
                 repo / 'tools/Native/Erlang.NifNativeBackend/NifDirty521Names.cs',
                 repo / 'tools/Native/Erlang.NifNativeBackend/NativeOwned516Profile.cs',
                 repo / 'src/Runtime/Erlang.Runtime.Core/Primitives/TermDisplayWriter.cs']
    repo_files += sorted((repo / 'tools/Native/Erlang.NifNativeBackend').glob('*.cs'))
    repo_files += [p for p in (repo / 'src/Runtime/Erlang.Runtime.Core/Native/Nif').rglob('*')
                   if p.is_file() and p.suffix in ('.cs', '.c', '.h')]
    repo_files += list((repo / 'src/Runtime/Erlang.Runtime.Core/Primitives').glob('TermDisplayWriter*.cs'))
    repo_files += list((repo / 'src/Runtime/Erlang.Runtime.Core/Primitives').glob('PrintfTerm*.cs'))
    repo_files += [proof_path, header, build_script, repo / 'Directory.Build.props']
    for path in sorted(set([*package_files, *repo_files])):
        require(path.is_file(), 'Missing final source input: ' + str(path))
        inputs.append({'Path': str(path), 'Sha256': sha(path)})
    header_recipe = json.loads((repo / 'tools/Native/Erlang.NifVariadic526Observations/source-plan.json').read_bytes())
    require(header_recipe['PinnedCommit'] == PIN, 'Header recipe pin differs')
    io_source = (repo / 'tools/Native/Erlang.NifNativeBackend/NifDirty521Names.cs').read_text()
    io_workers = int(re.search(r'IoWorkers\s*=\s*(\d+)', io_source).group(1))
    dotnet = shutil.which('dotnet')
    require(dotnet is not None, 'Existing dotnet host required')
    value = {'DotnetPath': str(Path(dotnet).resolve()), 'DotnetSha256': sha(dotnet), 'Status': 'SEALED_SOURCE_ONLY_MUTEX795_LIFECYCLE_PLAN', 'Pin': PIN, 'Platform': 'windows-x64',
             'Repository': str(repo), 'HeadAtSeal': subprocess.check_output(['git', '-C', str(repo), 'rev-parse', 'HEAD'], text=True).strip(),
             'ManagedHost': str(host), 'ManagedInputs': snapshot(host.parent), 'RuntimeVersion': args.runtime_version,
             'OriginalIoWorkers': io_workers, 'Headers': header_recipe['Headers'], 'Inputs': inputs, 'Subjects': subjects,
             'OriginalExecuted': False, 'ManagedExecuted': False, 'FullFormatterCompatibilityClaimed': False,
             'AdapterBuildReceipt': str(proof_path), 'AdapterBuildReceiptSha256': sha(proof_path),
             'WorkingTreeDirty': bool(subprocess.check_output(['git','-C',str(repo),'status','--porcelain','--untracked-files=no'],text=True).strip()), 'WholeCiHeadClaimed': False}
    output.parent.mkdir(parents=True, exist_ok=True)
    save(output, value)
    print(json.dumps({'Plan': str(output), 'Sha256': sha(output), 'ExecutionPerformed': False}))


if __name__ == '__main__':
    main()
