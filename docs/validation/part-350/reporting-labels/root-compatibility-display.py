from pathlib import Path
import hashlib, json, subprocess
import xml.etree.ElementTree as ET

r = Path.cwd()
o = r / 'artifacts/compatibility-display350'
o.mkdir(exist_ok=False)
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest().upper()
names = ['Readiness', 'ReadinessText', 'MfaCompatibility', 'MfaCompatibilityNames',
         'MfaCompatibilityMarkdown', 'MfaCompatibilityLedger', 'MfaCompatibilityEntry',
         'MfaCompatibilityEvidence', 'MfaCompatibilitySourceFile', 'ToolDiagnostics']
project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
props = ET.SubElement(project, 'PropertyGroup')
for key, value in {'OutputType':'Exe', 'TargetFramework':'net10.0',
                   'EnableDefaultCompileItems':'false', 'UseSharedCompilation':'false'}.items():
    ET.SubElement(props, key).text = value
items = ET.SubElement(project, 'ItemGroup')
sources = []
for name in names:
    p = r / 'tools/Erlang.Tool' / (name + '.cs')
    ET.SubElement(items, 'Compile', Include=str(p))
    sources.append({'Path': str(p.relative_to(r)), 'Sha256': sha(p)})
ET.SubElement(items, 'Compile', Include='Program.cs')
libraries = []
for name in ['Erlang.Terms', 'Erlang.Runtime', 'Erlang.Serialization', 'Erlang.Otp.Behaviours']:
    p = r / 'tools/Erlang.Tool/bin/Debug/net10.0' / (name + '.dll')
    ref = ET.SubElement(items, 'Reference', Include=name)
    ET.SubElement(ref, 'HintPath').text = str(p)
    libraries.append({'Path': str(p.relative_to(r)), 'Sha256': sha(p)})
ET.ElementTree(project).write(o / 'Display.csproj', encoding='utf-8', xml_declaration=False)
(o / 'Program.cs').write_text('await Readiness.Generate(args[0], args[1], args[2]);\n', encoding='utf-8')
report = r / 'docs/validation/part-350/full/tests.json'
ledger = r / 'docs/mfa-compatibility.json'
before = sha(ledger)
commands = [ ['dotnet', 'build', str(o/'Display.csproj'), '--nologo', '-v:q'],
             ['dotnet', str(o/'bin/Debug/net10.0/Display.dll'), str(r/'docs'), str(report), str(o/'generated')] ]
receipts = []
for i, command in enumerate(commands):
    with (o/f'{i}.stdout.txt').open('xb') as out, (o/f'{i}.stderr.txt').open('xb') as err:
        result = subprocess.run(command, cwd=r, stdout=out, stderr=err, timeout=120)
    receipts.append({'Command': command, 'ExitCode': result.returncode})
    assert result.returncode == 0, receipts[-1]
assert sha(ledger) == before
data = json.loads((o/'generated/module-readiness.json').read_bytes())
assert (data['VerifiedCompatibleMfas'], data['NeedsVerificationMfas'], data['KnownDifferenceMfas']) == (10,554,23)
old = json.loads((r/'docs/module-readiness.json').read_bytes())
assert data == old, 'Machine classifications or evidence changed'
md = (o/'generated/API_COMPATIBILITY.md').read_text(encoding='utf-8')
entries = json.loads(ledger.read_bytes())['Entries']
labels = {'Verified compatible': '100% compatible', 'Needs verification': 'Needs verification for 100% compatibility', 'Known differences': 'Known differences'}
for e in entries:
    key = f"{e['Module']}:{e['Function']}/{e['Arity']}"
    assert f"| `{key}` | {labels[e['Status']]} |" in md, key
assert md.count('| 100% compatible |') == 10
assert md.count('| Needs verification for 100% compatibility |') == 554
assert md.count('| Known differences |') == 23
for name in ['MODULE_READINESS.md', 'API_COMPATIBILITY.md']:
    b = (o/'generated'/name).read_bytes().rstrip(b'\r\n')+b'\n'
    (r/'docs'/name).write_bytes(b)
receipt = {'Status':'PASS', 'Commands':receipts, 'Sources':sources, 'Libraries':libraries,
           'LedgerSha256':before, 'Counts':{'Verified':10,'Pending':554,'KnownDifferences':23},
           'MachineReportUnchanged':True, 'RuntimeTestsRerun':False,
           'Generated':[{ 'Path':name, 'Sha256':sha(r/'docs'/name)} for name in ['MODULE_READINESS.md','API_COMPATIBILITY.md']]}
(o/'receipt.json').write_text(json.dumps(receipt, indent=2)+'\n', encoding='utf-8')
print(json.dumps(receipt['Counts']))
