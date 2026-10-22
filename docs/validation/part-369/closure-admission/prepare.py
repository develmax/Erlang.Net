"""Prepare exact closure GET/admission sources only; no network/VM/SDK execution."""
from pathlib import Path
import ast,hashlib,json,subprocess

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
BASE=ROOT/'artifacts/binary369-supplement-admission-proposal1'
HEAD='53823b168e672fc946e65f6dd53c0ea2f9c94611'
OLD_HEAD='f643e3a4a1c2911c7afe58f48703d1af0cf7a9b9'
MANIFEST='11E72CB41047BA9442B442A747D281138697D58E10AFA08CF8555160280893EC'
OLD_MANIFEST='399F2F229896B4D7AA10B2E36C63103C4F2233A20974300591A5DF27D6734156'
POLICY='compiled-monitored-worker-values-and-stacks-v1'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
intent=json.loads((ROOT/'artifacts/binary-original369-closure1/ci/dispatch-intent.json').read_bytes())
assert intent['Head']==HEAD and intent['SourceInputsSha256']==MANIFEST
for row in intent['Bindings']:
    data=subprocess.check_output(['git','-C',str(ROOT),'show',HEAD+':'+row['Path']])
    assert len(data)==row['Bytes'] and hashlib.sha256(data).hexdigest().upper()==row['Sha256']
config=OUT/'source-bindings.json'
config.write_text(json.dumps(dict(Head=HEAD,Branch='validation/binary-369-closure',Runs=dict(original=39084998283),Workflows=intent['Bindings']),indent=2)+'\n',encoding='utf-8')
changes=[]
for name in ['capture.py','audit.py','audit-source.py','audit-provenance.py']:
    source=BASE/name;text=source.read_text()
    text=text.replace(OLD_HEAD,HEAD).replace(OLD_MANIFEST,MANIFEST).replace('39083564610','39084998283')
    text=text.replace('binary369-supplement-admission-proposal1','binary369-closure-admission-proposal1').replace('binary-original369-supplement1','binary-original369-closure1').replace('validation/binary-369-supplement','validation/binary-369-closure')
    text=text.replace('3DB41115638695F2E89BE50EB02A1682C7C7C0D209837C2DC6702BAACAAECA49',sha(config))
    text=text.replace("'Values':36","'Values':14").replace("'FullStackProfiles':180","'FullStackProfiles':70")
    text=text.replace('value_count==36','value_count==14').replace('profile_count==180','profile_count==70')
    text=text.replace("'OriginalNormalProcesses':7","'OriginalNormalProcesses':5").replace('len(stages)==7','len(stages)==5').replace("len({x['Pid'] for x in stages})==7","len({x['Pid'] for x in stages})==5")
    text=text.replace("len(profile['Fixtures'])==2","len(profile['Fixtures'])==1").replace('36_180','14_70').replace('PASS_PASSIVE_BINARY369_SUPPLEMENT_ORIGINAL_ADMISSION','PASS_PASSIVE_BINARY369_CLOSURE_ORIGINAL_ADMISSION')
    if name=='audit-provenance.py':
        anchor="assert len(profile['Fixtures'])==1 and profile['ExpectedUniqueSourceCount']==4"
        assert anchor in text
        text=text.replace(anchor,anchor+"\n    assert profile['ValueAcquisition']=='"+POLICY+"' and all(f['ValueAcquisition']=='"+POLICY+"' for f in profile['Fixtures'])")
        anchor="directory=capture/fixture['Id'];assert group['Acquired'] and not group['VmSkipped']"
        assert anchor in text
        text=text.replace(anchor,anchor+"\n        assert group['ValueAcquisition']=='"+POLICY+"'")
        anchor="assert '-Werror' in compileargs and '+deterministic' in compileargs"
        assert anchor in text
        text=text.replace(anchor,anchor+"\n        expression=observeargs[observeargs.index('-eval')+1]\n        assert 'Rows=proplists_observation_worker:run(binary369_closure_contract,run),' in expression\n        assert 'Stacks=proplists_observation_worker:run(binary369_closure_contract,run_stacks),' in expression")
    ast.parse(text)
    target=OUT/name;target.write_text(text,encoding='utf-8')
    changes.append(dict(Path=name,BaseSha256=sha(source),Sha256=sha(target)))
(OUT/'preparation.json').write_text(json.dumps(dict(Status='PASS_SOURCE_RECIPE_PREPARATION_ONLY',Head=HEAD,Run=39084998283,SourceInputsSha256=MANIFEST,SourceBindingsSha256=sha(config),Values=14,Profiles=70,NormalOriginalProcesses=5,PreflightProcesses=3,ValueAcquisition=POLICY,Changes=changes,NetworkExecuted=False),indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(Status='PREPARED_NOT_EXECUTED',BindingsSha256=sha(config))))
