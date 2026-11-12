"""Inventory/identity audit for the complete selectable OTP-29.1.1 gen_server port."""
import argparse
import hashlib
import json
import pathlib
import re
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True)
args = parser.parse_args()
root = pathlib.Path(__file__).resolve().parents[2]
baseline = json.loads((root / 'reference-baseline.json').read_text(encoding='utf-8-sig'))
assert baseline['tag'] == 'OTP-29.1.1'
assert baseline['commit'] == 'ad05823719d77c8faee87348ea39513d4e2f99c5'
source = subprocess.check_output(['git', '-C', str(root.parent / 'otp'), 'show', baseline['tag'] + ':lib/stdlib/src/gen_server.erl'])
suite = subprocess.check_output(['git', '-C', str(root.parent / 'otp'), 'show', baseline['tag'] + ':lib/stdlib/test/gen_server_SUITE.erl'])
original = root / 'src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp'
assert (original / 'gen_server.erl').read_bytes() == source, 'Original source differs from the pinned upstream bytes'
assert (root / 'tools/Validation/Erlang.GenServerCompatibility/Reference/gen_server_SUITE.erl').read_bytes() == suite
exports = set()
for block in re.findall(rb'-export\(\[(.*?)\]\)', source, re.S):
    exports.update((name.decode(), int(arity)) for name, arity in re.findall(rb'([a-z][a-z_0-9]*)\s*/\s*(\d+)', block))
exports.add(('behaviour_info', 1))
managed = root / 'src/Otp/Managed/Erlang.Otp.Stdlib/Processes/GenServer'
names = dict(re.findall(r'const string (\w+) = "([^"]+)";', (managed / 'GenServerFunctionNames.cs').read_text()))
body = (managed / 'GenServerModule.cs').read_text()
export_block = body[:body.index('    public static void Register')]
registered = {(names[name], int(arity)) for name, arity in re.findall(r'\(GenServerFunctionNames\.(\w+),\s*(\d+)\)', export_block)}
assert registered == exports, {'missing': sorted(exports - registered), 'extra': sorted(registered - exports)}
assert 'EnterSourceTailCall' in body
assert 'GenServerMachine.Initialize' in body
assert 'GenServerMachine.EnterLoop' in body
files = sorted(managed.glob('GenServer*.cs'))
for file in files:
    if file.name in ['GenServer.cs', 'GenServerMessageTags.cs']:
        continue  # The unchanged host facade is a separate contract.
    text = file.read_text(encoding='utf-8-sig')
    if not file.name.endswith(('Names.cs', 'Atoms.cs', 'Formats.cs')):
        assert not re.search(r'(?m)^[^/\n]*"(?:\\.|[^"\\])*"', text), f'Inline semantic string in {file.name}'

sha = lambda data: hashlib.sha256(data).hexdigest().upper()
receipt = {
    'Baseline': baseline,
    'OriginalSourceSha256': sha(source),
    'OriginalBeamSha256': sha((original / 'gen_server.beam').read_bytes()),
    'UpstreamSuiteSha256': sha(suite),
    'ExplicitExportsIncludingBehaviourInfo': len(exports),
    'Exports': [f'{name}/{arity}' for name, arity in sorted(exports)],
    'ManagedSources': {str(p.relative_to(root)).replace('\\', '/'): sha(p.read_bytes()) for p in files},
    'FullRegressionRun': False,
    'UniversalOtpCompatibilityCertified': False,
}
output = pathlib.Path(args.output)
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
print(f'Pinned source, unchanged suite, {len(exports)} exports and semantic string ownership verified.')
