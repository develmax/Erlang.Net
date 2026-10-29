from pathlib import Path
import os,sys,json,hashlib,importlib.util,argparse
sys.dont_write_bytecode=True
p=argparse.ArgumentParser();p.add_argument('--label',required=True);p.add_argument('--names',type=Path,required=True);p.add_argument('--red',action='store_true');p.add_argument('--configuration',default='Release');a=p.parse_args()
r=Path.cwd();out=r/'artifacts/resource536'/a.label;out.mkdir(exist_ok=False)
names=json.loads(a.names.read_bytes());assert names and len(names)==len(set(names))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
host=r/f'tests/Erlang.Tests/bin/{a.configuration}/net10.0/Erlang.Tests.dll'
runtime=r/f'src/Runtime/Erlang.Runtime.Core/bin/{a.configuration}/net10.0/Erlang.Runtime.dll'
before=dict(Host=sha(host),Runtime=sha(runtime))
spec=importlib.util.spec_from_file_location('owner536',r/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
cmd=['dotnet','exec','--fx-version','10.0.11',str(host)]
for name in names:cmd+=['--case',name]
cmd+=['--report',str(out/'report.json')]
try:m.owned(cmd,out,'tests',180,dict(os.environ,DOTNET_PROCESSOR_COUNT='2'),str(r))
except RuntimeError:pass
receipt=json.loads((out/'tests.exit.json').read_bytes());report=json.loads((out/'report.json').read_bytes())
assert receipt['Settled'] and not receipt['TimedOut']
assert sha(host)==before['Host'] and sha(runtime)==before['Runtime']
assert report['Partial'] and {x['Name'] for x in report['Tests']}==set(names)
(out/'result.json').write_text(json.dumps(dict(Names=names,Assemblies=before,Receipt=receipt,Report=report,ExpectedRed=a.red),indent=2))
print(json.dumps(dict(Passed=report['Passed'],Failed=report['Failed'],Selected=len(names),Settled=receipt['Settled'])))
for row in report['Tests']:
    if row['Status']!='Passed':print(row['Name']+'\n'+row.get('Error',''))
assert (report['Failed']>0 and receipt['ExitCode']!=0) if a.red else (report['Passed']==len(names) and report['Failed']==0 and receipt['ExitCode']==0)
