from pathlib import Path
import hashlib,json,collections
r=Path.cwd();h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();j=lambda p:json.loads(p.read_bytes())
def save(p,x):p.write_bytes((json.dumps(x,indent=2,ensure_ascii=False)+'\n').encode('utf-8'))
tests=j(r/'artifacts/tests.json');prior=j(r/'docs/validation/part-370/local-completion/canonical-tests.json');assert tests['Passed']==7673 and tests['Failed']==0
oldnames=[x['Name'] for x in prior['Tests']];oldset=set(oldnames);assert [x['Name'] for x in tests['Tests'] if x['Name'] in oldset]==oldnames
receipt=j(r/'artifacts/binary-integration369/erlang371-canonical-tests2/execution.exit.json');assert receipt['ExitCode']==0 and receipt['Settled'] and not receipt['TimedOut']
for packet in ['ordsets371-full-main-fixed2','erlang371-main-final1']:
 for row in j(r/'artifacts'/packet/'capture1/main-dll-closure.json')['Files']:assert h(r/row['Path'])==row['Sha256']
proposal=r/'artifacts/erlang371-final-guard-review1/mfa-compatibility.proposed.json';assert h(proposal)=='AD00104EE9B4AA782DE57850E152D795520F649ED7E1CC739B6E61128740CB5D';ledger=j(proposal)
assert len(ledger['Entries'])==699 and len(ledger['VerifiedSourceFiles'])==496
for row in ledger['VerifiedSourceFiles']:assert h(r/row['Path'])==row['Sha256']
for row in ledger['Entries']:
 for evidence in row['Evidence']:
  q=r/'docs'/evidence['Path'];assert q.is_file() and h(q)==evidence['Sha256'],str(q)
registry=j(r/'docs/supported-mfas.json');key=lambda x:(x['Module'],x['Function'],x['Arity']);assert len(registry)==699 and {key(x) for x in registry}=={key(x) for x in ledger['Entries']}
raw=r/'artifacts/erlang371-root/supported-mfas-raw-final.json';assert not raw.exists();raw.write_bytes((r/'docs/supported-mfas.json').read_bytes())
baseline={key(x):x for x in j(r/'docs/validation/part-370/local-completion/supported-mfas.json')};restored=[]
for row in registry:
 old=baseline.get(key(row))
 if old and row.get('Limits')!=old.get('Limits'):restored.append(dict(Mfa=key(row),Before=row.get('Limits'),After=old.get('Limits')));row['Limits']=old.get('Limits')
statuses={key(x):x['Status'] for x in ledger['Entries']}
promoted={('erlang','hd',1),('erlang','tl',1),('erlang','tuple_size',1),('erlang','is_integer',3),('ordsets','new',0),('ordsets','is_empty',1),('ordsets','to_list',1)}
for row in registry:
 if key(row) in promoted:row['Limits']='Complete observable source contract verified in part371; original values/full stacks and final-source regression passed. Physical layout and performance equivalence are not claimed.'
 elif row['Module']=='ordsets':row['Limits']='All21 exports implemented and2616 original value/full-stack observations match strictly. Complete dependency source closure remains pending; see mfa-compatibility.json.'
save(r/'docs/mfa-compatibility.json',ledger);save(r/'docs/supported-mfas.json',registry)
scope=j(r/'docs/module-readiness-scope.json');scope['ComparisonTestReport']='validation/part-370/local-completion/canonical-tests.json'
if 'ordsets' not in scope['FocusModules']:scope['FocusModules'].append('ordsets')
scope['Notes'].insert(0,'Part371: all21 ordsets exports implemented; native2616 and new erlang4506 strict comparisons, retained9846 strict erlang observations, final7673 tests. Delta21 APIs/7 complete contracts; global699/114,496 source guards. Full21 compatibility is not inferred from a finite packet.')
save(r/'docs/module-readiness-scope.json',scope)
save(r/'artifacts/erlang371-root/metadata-restoration.json',dict(RestoredExistingLimits=len(restored),Rows=restored,PromotedMfas=sorted(promoted),Statuses=dict(collections.Counter(x['Status'] for x in ledger['Entries'])),PriorOrderedNamesRetained=len(oldnames),NewTests=len(tests['Tests'])-len(oldnames),FinalTests=tests['Passed']))
print(json.dumps(dict(Apis=len(registry),Contracts=sum(x['Status']=='Verified compatible' for x in ledger['Entries']),Guards=496,RestoredLimits=len(restored),Tests=7673)))
