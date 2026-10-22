"""Adapt trusted passive reviewers for the exact new-only36 run; no network execution."""
from pathlib import Path
import ast, hashlib, json, subprocess

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
BASE = ROOT / 'artifacts/binary369-original-admission-proposal2'
HEAD = 'f643e3a4a1c2911c7afe58f48703d1af0cf7a9b9'
OLD_HEAD = '672e4424ec9f0238be80ae14e6e077018adcb933'
MANIFEST = '399F2F229896B4D7AA10B2E36C63103C4F2233A20974300591A5DF27D6734156'
OLD_MANIFEST = 'A54B3C119311C183D635FD8112507E979FE574031584240D78639BF637A4CBC0'

def sha(data):
    return hashlib.sha256(data).hexdigest().upper()

intent = json.loads((ROOT / 'artifacts/binary-original369-supplement1/ci/dispatch-intent.json').read_bytes())
assert intent['Head'] == HEAD and intent['SourceInputsSha256'] == MANIFEST
for row in intent['Bindings']:
    raw = subprocess.check_output(['git', '-C', str(ROOT), 'show', HEAD + ':' + row['Path']])
    assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256']
bindings = OUT / 'source-bindings.json'
bindings.write_text(json.dumps(dict(Head=HEAD, Branch='validation/binary-369-supplement', Runs=dict(original=39083564610), Workflows=intent['Bindings']), indent=2) + '\n', encoding='utf-8')
bindings_sha = sha(bindings.read_bytes())
changes = []
for name in ['capture.py', 'audit.py', 'audit-source.py', 'audit-provenance.py']:
    path = BASE / name
    text = path.read_text(encoding='utf-8')
    text = text.replace(OLD_HEAD, HEAD).replace('39083021286', '39083564610').replace(OLD_MANIFEST, MANIFEST)
    text = text.replace('binary369-original-admission-proposal2', 'binary369-supplement-admission-proposal1')
    text = text.replace('binary-original369-original1', 'binary-original369-supplement1').replace('validation/binary-369-all', 'validation/binary-369-supplement')
    text = text.replace('A1866265ECE18235C9561963AD60089850E85CBA6CA2ABCE18BA41E6E635C3F6', bindings_sha)
    text = text.replace("'Values':585", "'Values':36").replace("'FullStackProfiles':2925", "'FullStackProfiles':180")
    text = text.replace('value_count==585', 'value_count==36').replace('profile_count==2925', 'profile_count==180')
    text = text.replace("'OriginalNormalProcesses':19", "'OriginalNormalProcesses':7").replace('len(stages)==19', 'len(stages)==7').replace("len({x['Pid'] for x in stages})==19", "len({x['Pid'] for x in stages})==7")
    text = text.replace("len(set(profile['Mfas']))==31", "len(set(profile['Mfas']))==6").replace("len(profile['Fixtures'])==8", "len(profile['Fixtures'])==2")
    text = text.replace("profile['ExpectedUniqueSourceCount']==10", "profile['ExpectedUniqueSourceCount']==4").replace("len(parse['Fixtures'])==10", "len(parse['Fixtures'])==4").replace("len({x['Path'] for x in parse['Fixtures']})==10", "len({x['Path'] for x in parse['Fixtures']})==4")
    text = text.replace('585_2925', '36_180').replace('PASS_PASSIVE_BINARY369_ORIGINAL_ADMISSION', 'PASS_PASSIVE_BINARY369_SUPPLEMENT_ORIGINAL_ADMISSION')
    ast.parse(text)
    target = OUT / name
    target.write_text(text, encoding='utf-8')
    changes.append(dict(Path=name, BaseSha256=sha(path.read_bytes()), Sha256=sha(target.read_bytes())))
(OUT / 'preparation.json').write_text(json.dumps(dict(Status='PREPARED_NOT_EXECUTED', Head=HEAD, Run=39083564610, SourceInputsSha256=MANIFEST, SourceBindingsSha256=bindings_sha, Values=36, Profiles=180, OriginalProcesses=7, PreflightProcesses=3, Files=changes, NetworkExecuted=False), indent=2) + '\n', encoding='utf-8')
print(json.dumps(dict(Status='PREPARED_NOT_EXECUTED', SourceBindingsSha256=bindings_sha)))
