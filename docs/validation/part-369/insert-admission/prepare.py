"""Prepare exact insert7 passive capture/admission/comparison, never executes them."""
from pathlib import Path
import ast,hashlib,json,subprocess
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).resolve().parent
BASE=ROOT/'artifacts/binary369-closure-admission-proposal1'
HEAD='d5bc60c53a966e1eaf1c64f7e40bb15f9244fb14';OLD_HEAD='53823b168e672fc946e65f6dd53c0ea2f9c94611'
MANIFEST='DA790B2A72A0F1D821B51873FDC52F47FC5523E02A70CE4A77E5AA8DD4C0BD20';OLD_MANIFEST='11E72CB41047BA9442B442A747D281138697D58E10AFA08CF8555160280893EC'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
intent=json.loads((ROOT/'artifacts/binary-original369-insert1/ci/dispatch-intent.json').read_bytes())
assert intent['Head']==HEAD and intent['SourceInputsSha256']==MANIFEST
for row in intent['Bindings']:
    data=subprocess.check_output(['git','-C',str(ROOT),'show',HEAD+':'+row['Path']])
    assert len(data)==row['Bytes'] and hashlib.sha256(data).hexdigest().upper()==row['Sha256']
bindings=OUT/'source-bindings.json'
bindings.write_text(json.dumps(dict(Head=HEAD,Branch='validation/binary-369-insert',Runs=dict(original=39085952486),Workflows=intent['Bindings']),indent=2)+'\n',encoding='utf-8')
changes=[]
for name in ['capture.py','audit.py','audit-source.py','audit-provenance.py','compare.py','controls.py']:
    source=BASE/name;text=source.read_text()
    text=text.replace(OLD_HEAD,HEAD).replace(OLD_MANIFEST,MANIFEST).replace('39084998283','39085952486')
    text=text.replace('binary369-closure-admission-proposal1','binary369-insert-admission-proposal1').replace('binary369-closure-execution-proposal1','binary369-insert-execution-proposal1').replace('binary-original369-closure1','binary-original369-insert1').replace('validation/binary-369-closure','validation/binary-369-insert')
    text=text.replace('F77A3C91130C06F371BF38E64EA3E93DD364FA06608DDD614529E9FA6F7FB26D',sha(bindings))
    text=text.replace("'Values':14","'Values':7").replace("'FullStackProfiles':70","'FullStackProfiles':35").replace('value_count==14','value_count==7').replace('profile_count==70','profile_count==35')
    text=text.replace('14_70','7_35').replace('PASS_PASSIVE_BINARY369_CLOSURE_ORIGINAL_ADMISSION','PASS_PASSIVE_BINARY369_INSERT_ORIGINAL_ADMISSION')
    text=text.replace("len(set(profile['Mfas']))==6","len(set(profile['Mfas']))==2")
    text=text.replace("profile['ExpectedUniqueSourceCount']==4","profile['ExpectedUniqueSourceCount']==3").replace("len(parse['Fixtures'])==4","len(parse['Fixtures'])==3").replace("len({x['Path'] for x in parse['Fixtures']})==4","len({x['Path'] for x in parse['Fixtures']})==3")
    text=text.replace('binary369_closure_contract','binary369_insert_contract')
    if name=='compare.py':
        text=text.replace("len(profile['FixtureSources'])==4","len(profile['FixtureSources'])==3").replace("fixture['Id']=='closure'","fixture['Id']=='insert'")
        text=text.replace("len(fixture['Names'])==14","len(fixture['Names'])==7").replace("len(set(fixture['Names']))==14","len(set(fixture['Names']))==7")
        text=text.replace("group['Id']=='closure'","group['Id']=='insert'").replace('len(sources)==4','len(sources)==3')
        text=text.replace("a.native/'closure'","a.native/'insert'").replace("a.sdk/'closure'","a.sdk/'insert'")
        text=text.replace('len(rows)==84','len(rows)==42').replace('ExpectedValues=14','ExpectedValues=7').replace('ExpectedProfiles=70','ExpectedProfiles=35').replace('ExpectedObservations=84','ExpectedObservations=42')
        text=text.replace('PASS_INDEPENDENT_STRICT_MATCHED_WORKER_CLOSURE','PASS_INDEPENDENT_STRICT_MATCHED_WORKER_INSERT').replace('OldTwoLiveValuesRetrospectivelyAccepted=False','PriorBatchesRetrospectivelyAltered=False')
    ast.parse(text);target=OUT/name;target.write_text(text,encoding='utf-8')
    changes.append(dict(Path=name,BaseSha256=sha(source),Sha256=sha(target)))
(OUT/'preparation.json').write_text(json.dumps(dict(Status='SOURCE_RECIPES_NOT_EXECUTED',Head=HEAD,Run=39085952486,SourceInputsSha256=MANIFEST,SourceBindingsSha256=sha(bindings),Values=7,Profiles=35,Comparisons=42,UniqueSources=3,NormalOriginalProcesses=5,PreflightProcesses=3,Files=changes,NetworkExecuted=False),indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(Status='PREPARED_NOT_EXECUTED',SourceBindingsSha256=sha(bindings))))
