from pathlib import Path
import hashlib,json,collections
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent
def read(p):return json.loads((R/p).read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256((R/p).read_bytes()).hexdigest().upper()
def out(n,x):(O/n).write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')
def durable(p):return 'validation/part-371/evidence/'+p.removeprefix('artifacts/')
def evidence(p,kind,scope):return {'Kind':kind,'Path':durable(p),'Sha256':sha(p),'Scope':scope}
ledger=read('docs/mfa-compatibility.json')
proof=read('artifacts/erlang371-contract-audit1/proposed-classifications.json')['Changes']
ordproof=read('artifacts/erlang371-ordsets-independent-review1/review.json')['Rows'][:3]
scopes={x['Mfa']:x['Scope'] for x in proof}
scopes.update({x['Mfa']:x['Proof']+' Complete observable logical contract over supported valid Erlang terms; runtime physical resource capacity and performance are not claimed identical.' for x in ordproof})
selected=[]
for prefix,profile,comparison in [('erlang','artifacts/erlang371-next-original-proposal1/tool/profile.json','artifacts/erlang371-final-comparison1'),('ordsets','artifacts/ordsets371-remaining-original-proposal3/tool/profile.json','artifacts/ordsets371-fixed-comparison2')]:
 p=read(profile); rows=read(comparison+'/rows.json')
 for mfa in scopes:
  if not mfa.startswith(prefix+':'):continue
  cases=[(f['Id'],c['Name']) for f in p['Fixtures'] for c in f['Cases'] if mfa in c['Mfas']]
  indices=[i for i,x in enumerate(rows) if (x['Group'],x['Name']) in cases]
  assert len(indices)==6*len(cases) and all(rows[i]['Strict'] for i in indices)
  selected.append({'Mfa':mfa,'ComparisonRowsPath':durable(comparison+'/rows.json'),'ComparisonRowsSha256':sha(comparison+'/rows.json'),'RowIndexBase':0,'Indices':indices,'ValueCases':len(cases),'StrictObservations':len(indices),'ProfilePath':durable(profile),'ProfileSha256':sha(profile),'CaseNames':[{'Group':g,'Name':n} for g,n in cases]})
out('selected-original-witnesses.json',selected)
sourceev=evidence('artifacts/erlang371-final-guard-review1/source-review.json','SourceReview','Independent final Main source delta review; preserves previous verified evidence, binds 496 production sources, and records exact additive dependencies. Subject to final canonical acceptance.')
witness=evidence('artifacts/erlang371-final-guard-review1/selected-original-witnesses.json','OriginalOtpComparison','Actual pinned original CI observations, full outcomes/stacks depths default/0/1/2/8; strict all fields, no projection or normalization. Exact per-MFA row indices and profile/source bindings.')
for entry in ledger['Entries']:
 if entry['Status']=='Verified compatible':entry['Evidence'].append(sourceev)
 mfa=f"{entry['Module']}:{entry['Function']}/{entry['Arity']}"
 if mfa in scopes:
  assert entry['Status']=='Needs verification'
  entry.update(Status='Verified compatible',Scope=scopes[mfa],Limits=[])
  entry['Evidence'] += [sourceev,witness,evidence('artifacts/erlang371-contract-audit1/contract-proof.json','CompatibilityAudit','Exhaustive source branch, term partition, guard/error-frame and depth proof for the four bounded erlang APIs. Existing source bindings are superseded only by the reviewed registration addition and final guarded source hashes.')]
profile=read('artifacts/ordsets371-remaining-original-proposal3/tool/profile.json')
for mfa in profile['Mfas']:
 module,rest=mfa.split(':');name,arity=rest.rsplit('/',1)
 entry={'Module':module,'Function':name,'Arity':int(arity),'Status':'Needs verification','Scope':'Implemented source port; all 21 APIs have actual pinned OTP finite value/full-stack comparisons. This is not a whole-domain compatibility claim.','Evidence':[sourceev,evidence('artifacts/ordsets371-fixed-comparison2/acceptance.json','OriginalOtpComparison','436 actual original outcomes and 2180 full stacks across all 21 APIs; 2616 strict field comparisons, zero differences, no normalization.')],'Limits':['Whole-domain source dependency closure remains pending: complete term ordering/equality including HAMT map keys and all function terms, dependent lists private helper/error paths, or length trapping/resource lifetime as applicable. Finite samples do not establish these general contracts.']}
 if mfa in scopes:
  entry.update(Status='Verified compatible',Scope=scopes[mfa],Limits=[])
  entry['Evidence'] += [witness,evidence('artifacts/erlang371-ordsets-independent-review1/review.json','CompatibilityAudit','Pinned source clause and exhaustive valid-term partition proof for new/0, is_empty/1 and to_list/1, independently confirmed against final Main registration bodies. These three do not traverse payloads or invoke term ordering/equality helpers.')]
 ledger['Entries'].append(entry)
ledger['VerifiedSourceFiles']=read('artifacts/erlang371-final-guard-review1/verified-source-files.proposed.json')
assert len(ledger['Entries'])==699
counts=collections.Counter(x['Status'] for x in ledger['Entries'])
assert counts=={'Verified compatible':114,'Needs verification':555,'Known differences':30}
out('mfa-compatibility.proposed.json',ledger)
out('ledger-proposal-summary.json',{'Status':'PROPOSAL_ONLY_CONDITIONAL_ON_PARENT_FINAL_CANONICAL_ACCEPTANCE','BaseEntryCount':678,'FinalEntries':699,'AddedImplementedApis':21,'NewFullContracts':7,'Counts':dict(counts),'PromotionMfas':list(scopes),'FinalGuardCount':496,'NoExistingEvidenceRemoved':True,'NoOtherStatusChanged':True,'RequiredDurableDirectory':'docs/validation/part-371/evidence/erlang371-final-guard-review1','ProposedLedgerSha256':sha('artifacts/erlang371-final-guard-review1/mfa-compatibility.proposed.json')})
print(json.dumps({'Counts':dict(counts),'Promotions':list(scopes),'Witnesses':sum(x['StrictObservations'] for x in selected)}))
