from pathlib import Path
import hashlib,importlib.util,json
r=Path.cwd();p=r/'artifacts/part307-packaging-proposal1';out=r/'docs/validation/part-307';sha=lambda b:hashlib.sha256(b).hexdigest().upper()
s=importlib.util.spec_from_file_location('review_common',p/'part307-common.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
c,f,index=m.gates(p/'config-unbound.json')
rp=out/'evidence-receipt.json';receipt=json.loads(rp.read_bytes())
assert receipt['Passed']==6627 and 'PostFinalizationReadOnlyReview' not in receipt
for row in receipt['Files']:
 b=(out/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256']
old=json.loads((out/'packaging-proposal1/evidence-index.json').read_bytes());oldpaths={x['SourcePath'] for x in old}
extra=[x for x in index if x['SourcePath'] not in oldpaths]
assert len(extra)==1 and extra[0]['SourcePath']=='artifacts/root-part307-stage-pathspec'
copies=[('packaging-final-review2/'+name,(p/name).read_bytes()) for name in ['part307-common.py','config-unbound.json','evidence-index.json']]
copies.append(('packaging-final-review2/pre-final-stage-pathspec',(r/extra[0]['SourcePath']).read_bytes()))
copies.append(('packaging-final-review2/root-post-finalization307-review.py',Path(__file__).read_bytes()))
review={'Status':'PASS_POST_FINALIZATION_READ_ONLY_SUCCESS_GUARDS','SourceFreezeSha256':c['SourceFreeze']['Sha256'],'ProductionSourceChanged':False,'OriginalFinalizerConfigRetained':True,'Reason':'Additional explicit prior306 successful CI/native/ordinary and strict-map-only refusal assertions were reviewed after initial packaging; initial frozen packaging retained.','Scope':'Read-only gate replay, no build/native/original execution or second changelog append.','Inputs':[{'Path':name,'Bytes':len(b),'Sha256':sha(b)} for name,b in copies]}
reviewbytes=(json.dumps(review,indent=2)+'\n').encode();copies.append(('packaging-final-review2/review.json',reviewbytes))
for name,b in copies:assert not (out/name).exists()
for name,b in copies:
 q=out/name;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(b);receipt['Files'].append({'Path':name,'Bytes':len(b),'Sha256':sha(b)})
receipt['PostFinalizationReadOnlyReview']={'Path':'packaging-final-review2/review.json','Sha256':sha(reviewbytes)}
rp.write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Status':review['Status'],'AdditionalProofFiles':len(copies),'ProofFiles':len(receipt['Files'])}))
