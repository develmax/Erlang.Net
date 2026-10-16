"""Bind the actually validated post-full327 Style binary; retain rejected stale pin."""
from pathlib import Path
import json, hashlib, subprocess
r=Path.cwd(); a=r/'artifacts'; src=a/'undefined-lambda328-root-focus-proposal1'; d=a/'undefined-lambda328-root-focus-proposal2'
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
j=lambda p:json.loads(p.read_bytes())
assert not d.exists() and not (a/'undefined-lambda328-root-focus1').exists()
style_inputs=[]
for row in j(a/'root-part327-main1/source-resource-closure.json')['Files']:
    if row['Path'].startswith('tools/Erlang.Style/'):
        q=r/row['Path']; assert q.stat().st_size==row['Bytes'] and h(q)==row['Sha256']
        blob=subprocess.run(['git','show','4bc4c00b713c1cc0bbf32a1c74add45174bbd10a:'+row['Path']],capture_output=True,check=True).stdout
        assert blob.replace(b'\r\n',b'\n')==q.read_bytes().replace(b'\r\n',b'\n')
        style_inputs.append(row)
assert style_inputs
refs=j(src/'references.json'); changed=[]
for row in refs:
    q=r/row['Path']
    if h(q)!=row['Sha256']:
        assert row['Path']=='tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll'
        assert h(q)=='29DE6ED68EBB76270216B5DCA1288C6EEC7C5250AB16A4C97A29E4D7E4242D35'
        changed.append({'Path':row['Path'],'PriorSha256':row['Sha256'],'ActualSha256':h(q)})
        row['Sha256']=h(q);row['Bytes']=q.stat().st_size
assert len(changed)==1
d.mkdir()
for name in ('focus.py','README.md'):(d/name).write_bytes((src/name).read_bytes())
(d/'references.json').write_text(json.dumps(refs,indent=2)+'\n')
receipt={'Status':'SOURCE_ONLY_CURRENT_STYLE_BINARY_BINDING_PRIOR_RECIPE_REFUSED_BEFORE_OUTPUT_OR_SDK','PriorProposalSha256':h(src/'proposal-freeze.json'),'InitialAttemptExitCode':1,'InitialRefusal':'Style binary reference hash differs after validated Main327 full rebuild','FocusOutputCreatedByInitialAttempt':False,'SourceUnchangedFromPublished327':style_inputs,'ChangedReferences':changed,'SdkOrVmExecuted':False,'Files':[{'Path':p.name,'Bytes':p.stat().st_size,'Sha256':h(p)} for p in sorted(d.iterdir())]}
(d/'proposal-freeze.json').write_text(json.dumps(receipt,indent=2)+'\n')
print(json.dumps({'FreezeSha256':h(d/'proposal-freeze.json'),'SourceInputsVerified':len(style_inputs),'PriorRefusalIsNotRuntimeFailure':True}))
