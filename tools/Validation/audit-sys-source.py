"""Inventory the sys port against the pinned source; this is not semantic acceptance."""
import hashlib
import json
from pathlib import Path
import re
import subprocess

BASELINE_TAG = 'OTP-29.1.1'
BASELINE_COMMIT = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
REFERENCE_SOURCE = 'lib/stdlib/src/sys.erl'
MANAGED_SOURCE = 'src/Otp/Managed/Erlang.Otp.Stdlib/Processes/Sys'
REPOSITORY = Path(__file__).resolve().parents[2]
REFERENCE_CHECKOUT = REPOSITORY.parent / 'otp'

reference_commit = subprocess.check_output(['git', '-C', str(REFERENCE_CHECKOUT), 'rev-list', '-n', '1', BASELINE_TAG], text=True).strip()
if reference_commit != BASELINE_COMMIT:
    raise SystemExit('sys audit: reference tag no longer identifies the pinned commit')
reference = subprocess.check_output(['git', '-C', str(REFERENCE_CHECKOUT), 'show', f'{BASELINE_TAG}:{REFERENCE_SOURCE}'])
source = reference.decode('utf-8')
exports = {(name, int(arity)) for block in re.findall(r'-export\(\[(.*?)\]\)\.', source, re.S)
           for name, arity in re.findall(r'(\w+)\s*/\s*(\d+)', block)}
exports.add(('behaviour_info', 1))
root = REPOSITORY / MANAGED_SOURCE
names = dict(re.findall(r'const string (\w+) = "([^"]+)";', (root / 'SysApiNames.cs').read_text(encoding='utf-8-sig')))
dispatch = (root / 'SysModule.cs').read_text(encoding='utf-8-sig')
declarations = dispatch[:dispatch.index('public static void Register')]
managed = {(names[name], int(arity)) for name, arity in re.findall(r'\(SysApiNames\.(\w+), (\d+)\)', declarations)}
if managed != exports:
    raise SystemExit(f'sys audit: missing={sorted(exports-managed)}, extra={sorted(managed-exports)}')
implementation = [*root.glob('SysModule*.cs'), root / 'SysSource.cs']
for path in implementation:
    code = re.sub(r'//[^\n]*|/\*.*?\*/', '', path.read_text(encoding='utf-8-sig'), flags=re.S)
    if '"' in code:
        raise SystemExit(f'sys audit: unnamed string in {path.name}')
    if re.search(r'PrepareBeam|ExternalTermFormat|BeamRegisterInterpreter|Call\(context,\s*(?:Name|SysModule\.Name|SysLibraryNames\.Module)', code):
        raise SystemExit(f'sys audit: original sys execution/fallback in {path.name}')
print(json.dumps({
    'Baseline': BASELINE_TAG, 'Commit': BASELINE_COMMIT,
    'ReferenceSourceSha256': hashlib.sha256(reference).hexdigest(),
    'DeclaredNonMetadataExports': len(exports), 'ManagedExports': len(managed),
    'ExactExportInventory': True, 'NoOriginalSysFallback': True,
    'Scope': 'Source inventory and named strings only; API coverage does not prove compatibility',
    'Sources': [{'Path': str(path.relative_to(REPOSITORY)).replace('\\', '/'),
                 'Sha256': hashlib.sha256(path.read_bytes()).hexdigest()} for path in sorted(root.glob('Sys*.cs'))]
}, indent=2))
