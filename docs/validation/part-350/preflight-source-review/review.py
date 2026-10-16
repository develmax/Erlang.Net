from pathlib import Path
from collections import Counter
import json,hashlib,re,subprocess
R=Path.cwd();C=R/'artifacts/additional-type-predicate-candidate';F=R/'artifacts/additional-type-predicate350-full-source3';O=R/'artifacts/additional-type-predicate350-source-independent'
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());head='712933b97a654a3b6ed42250902017bde0740f17'
assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()==head
assert sha((F/'source-freeze.json').read_bytes())=='B2CA082349C03497A4992D9148D122848EE59FFF517940B30BAF49C4EB0D1AC5'
assert sha((F/'source-resource-closure.json').read_bytes())=='2D4C13B3BE68D947965AB9D6D168B9643373E4856EF1F1786E57A7A7D22B7037'
freeze=read(F/'source-freeze.json');closure=read(F/'source-resource-closure.json');owned={x['Path'] for x in freeze['Files']};assert len(owned)==11 and sum(x.endswith('.cs') for x in owned)==7
assert not any(x.startswith('src/') for x in owned)
for row in freeze['Files']:
 b=(C/row['Path']).read_bytes();snapshot=(F/'source'/row['Path']).read_bytes();assert b==snapshot and len(b)==row['Bytes'] and sha(b)==row['Sha256']
assert len(closure['Files'])==2768 and len({x['Path'] for x in closure['Files']})==2768
for row in closure['Files']:
 b=(C/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256']
 if row['Path'] not in owned:assert b==(R/row['Path']).read_bytes(),row['Path']
pkgs=read(F/'package-resources.json')['Files'];assert len(pkgs)==6
for row in pkgs:
 b=(C/row['Path']).read_bytes();assert b==(R/row['Path']).read_bytes() and len(b)==row['Bytes'] and sha(b)==row['Sha256']
inputs=read(F/'readiness-inputs.json')['Files'];assert len(inputs)==29
for row in inputs:
 b=(C/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256']
scope=read(C/'docs/module-readiness-scope.json');oldscope=read(R/'docs/module-readiness-scope.json');assert scope['ComparisonTestReport']=='validation/part-348/full/tests.json'
assert {k:v for k,v in scope.items() if k!='ComparisonTestReport'}=={k:v for k,v in oldscope.items() if k!='ComparisonTestReport'}
ledger=read(C/'docs/mfa-compatibility.json');assert (C/'docs/mfa-compatibility.json').read_bytes()==(R/'docs/mfa-compatibility.json').read_bytes()
assert Counter(e['Status'] for e in ledger['Entries'])=={'Verified compatible':10,'Known differences':19,'Needs verification':558} and len(ledger['Entries'])==587 and len(ledger['VerifiedSourceFiles'])==59
for row in ledger['VerifiedSourceFiles']:assert row['Path'].startswith('src/') and sha((C/row['Path']).read_bytes())==row['Sha256']
for e in ledger['Entries']:
 for item in e['Evidence']:assert sha((C/'docs'/item['Path']).read_bytes())==item['Sha256']
registry=(F/'registry-before.json').read_bytes();assert registry==(C/'docs/supported-mfas.json').read_bytes()==(R/'docs/supported-mfas.json').read_bytes() and len(json.loads(registry))==587
previous=read(F/'previous-tests.json');assert previous['Passed']==len(previous['Tests'])==7268 and previous['Failed']==0 and all(x['Status']=='Passed' for x in previous['Tests'])
assert (F/'previous-tests.json').read_bytes()==(R/'docs/validation/part-348/full/tests.json').read_bytes()
planchecks=[]
for path,workflow,var,count in [('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1250),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1275)]:
 raw=(C/path).read_bytes();oldraw=(R/path).read_bytes();new=json.loads(raw);old=json.loads(oldraw);assert b'\r' not in raw and raw.endswith(b'\n') and len(new['Files'])==count
 assert {k:v for k,v in new.items() if k!='Files'}=={k:v for k,v in old.items() if k!='Files'}
 changed=[]
 for x,y in zip(new['Files'],old['Files'],strict=True):
  assert x['Path']==y['Path'] and x['HashMode']==y['HashMode'] in ['TextLF','BinaryExact']
  b=(C/x['Path']).read_bytes();hashed=b.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else b
  assert len(hashed)==x['Bytes'] and sha(hashed)==x['Sha256']
  if x!=y:changed.append(x['Path'])
 assert changed==([] if count==1250 else ['.gitattributes'])
 wf=(C/workflow).read_bytes();matches=re.findall(var.encode()+rb': ([0-9A-F]{64})',wf);assert matches==[sha(raw).encode()]
 if count==1250:assert raw==oldraw and wf==(R/workflow).read_bytes()
 else:assert wf==(R/workflow).read_bytes().replace(sha(oldraw).encode(),sha(raw).encode())
 planchecks.append({'Path':path,'Rows':count,'ChangedRows':changed,'Sha256':sha(raw),'WorkflowSealExact':True})
proofs=[];allnames=[]
for prefix,proposal,expected in [('AdditionalTypePredicate',R/'artifacts/additional-type-predicate-proposal',6),('NumericBoundary',R/'artifacts/numeric-bif350-fixture-proposal',12)]:
 files=[]
 for suffix in ['CaseNames.cs','CaseSources.cs','Cases.cs']:
  path='tools/Erlang.Differential/'+prefix+suffix;b=(C/path).read_bytes();row=next(x for x in read(proposal/'source-freeze.json')['Files'] if x['Path'].endswith('/'+prefix+suffix))
  assert sha(b)==row['Sha256'] and len(b)==row['Bytes'];files.append(path)
 names=dict(re.findall(r'public const string (\w+) = ("(?:\\.|[^"\\])*");',(C/files[0]).read_text(encoding='utf-8')));names={k:json.loads(v) for k,v in names.items()};sources=dict(re.findall(r'public const string (\w+) = ("(?:\\.|[^"\\])*");',(C/files[1]).read_text(encoding='utf-8')));sources={k:json.loads(v) for k,v in sources.items()};assert len(sources)==expected
 for name,s in sources.items():assert name in names and '-module(' in s and 'done.' in s;allnames.append('oracle/compiled-module/'+names[name])
 proofs.append({'Class':prefix,'Fixtures':expected,'FilesRawExactFocusedFreeze':files,'LogicalSourcesSha256':{k:sha(s.encode()) for k,s in sources.items()}})
added=read(F/'added-tests.json');assert added['SourceFreezeSha256']==sha((F/'source-freeze.json').read_bytes()) and added['Names']==allnames and len(set(allnames))==18
assert not set(allnames)&{x['Name'] for x in previous['Tests']}
catalogpath='tools/Erlang.Differential/CompiledModuleCases.cs';catalog=(C/catalogpath).read_bytes();oldcatalog=(R/catalogpath).read_bytes()
oldtail=b'        .. TermOrderCases.Modules\n'
newtail=b'        .. TermOrderCases.Modules,\n        .. AdditionalTypePredicateCases.Modules,\n        .. NumericBoundaryCases.Modules\n'
assert oldcatalog.count(oldtail)==1 and catalog==oldcatalog.replace(oldtail,newtail)
style=read(F/'style/exit.json');assert style['ExitCode']==0 and style['Settled'] and not style['TimedOut']
report={'Status':'PASS_INDEPENDENT_SOURCE_ONLY350_PREFLIGHT_BEFORE_FULL','ParentHead':head,'SourceFreezeSha256':sha((F/'source-freeze.json').read_bytes()),'SourceClosureSha256':sha((F/'source-resource-closure.json').read_bytes()),'PreflightSha256':sha((F/'preflight.json').read_bytes()),'OwnedPaths':11,'OwnedCSharp':7,'ProductionCSharpChanges':0,'ClosureInputsRehashed':2768,'AllUnownedClosureInputsRawExactCurrentMain':True,'PackageResourcesRawExactMain':6,'ReadinessInputsRehashed':29,'RegistryRawExactIncludingLimits':587,'CurrentRuntimeBindings':59,'CompatibilityCounts':[10,19,558],'Plans':planchecks,'SourceCatalogs':proofs,'PreviousOrderedTests':7268,'NewDeclaredTests':18,'ExpectedFullArithmeticOnly':7286,'CatalogDeltaExactlyTwoSpreads':True,'StyleExit0ReceiptBound':True,'Preparation1And2NewlineRefusalsPreservedNotSdkFailures':True,'OriginalComparisonClaimed':False,'FullExecutedByReview':False,'SdkExecutedByReview':False,'MainModified':False,'NetworkUsed':False}
p=O/'report.json';assert not p.exists();p.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(sha(p.read_bytes()))
