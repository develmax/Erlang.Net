"""Explicit actual pins, artifact outputs only; --prepare cannot authorize mutation."""
import sys,json,importlib.util
sys.dont_write_bytecode=True
from pathlib import Path
spec=importlib.util.spec_from_file_location('common312',Path(__file__).with_name('common312.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
b=Path(__file__).resolve().parent;a=m.read(sys.argv[1]);prepare='--prepare' in sys.argv
assert a['Part']==312 and a['Status'] in ['ROOT_REVIEWED_ACTUAL_FINAL312_AUTHORITY','ROOT_ACTUAL_FINAL312_AUTHORITY_PENDING_REVIEW']
assert (a.get('RootExecutionApproved') is True and a['Status']=='ROOT_REVIEWED_ACTUAL_FINAL312_AUTHORITY') or prepare,'Root review not approved'
def pin(name):
 data=m.safe(name).read_bytes();return {'Path':name,'Bytes':len(data),'Sha256':m.sha(data)}
assert pin(a['SourceFreeze']['Path'])==a['SourceFreeze'];f=m.read(m.pinned(a['SourceFreeze']));rows=[]
def add(source,dest,category):
 actual=pin(source);target=m.safe(dest);assert target.is_relative_to(m.OUT)
 previous=next((x for x in rows if x['Destination']==dest),None)
 if previous:assert (previous['Bytes'],previous['Sha256'])==(actual['Bytes'],actual['Sha256']);return
 rows.append({'SourcePath':source,'Destination':dest,'Bytes':actual['Bytes'],'Sha256':actual['Sha256'],'Category':category})
t=m.read(m.pinned(a['PreviousTerminalProofIndex']))
for declaration in t['Manifests']:
 assert pin(declaration['Path'])['Sha256']==declaration['Sha256']
 for row in m.read(m.safe(declaration['Path'])):
  assert pin(row['SourcePath'])['Sha256']==row['Sha256'];add(row['SourcePath'],row['Destination'],'previous-terminal')
 add(declaration['Path'],'docs/validation/part-312/staging-manifests/'+Path(declaration['Path']).name,'previous-terminal')
add(a['PreviousTerminalProofIndex']['Path'],'docs/validation/part-312/staging-manifests/'+Path(a['PreviousTerminalProofIndex']['Path']).name,'previous-terminal')
for key,category,subdir in [('PortableFiles','portable','composite-portable'),('RootProofPaths','root-explicit','root-artifacts'),('IndependentReviewPaths','independent-review','independent-reviews'),('PackagingControlPaths','packaging-control','packaging-proposal1')]:
 for source in a[key]:add(source,'docs/validation/part-312/'+subdir+'/'+Path(source).name,category)
c={'Part':312,'BoundToActualComposite':True,'RootExecutionApproved':a.get('RootExecutionApproved') is True,'ReusedFullValidation':False,'ParentHead':f['ParentHead'],'SourceFreeze':a['SourceFreeze'],'CompositeAuthority':pin(Path(sys.argv[1]).resolve().relative_to(m.ROOT).as_posix()),'PreviousTerminalProofIndex':a['PreviousTerminalProofIndex'],'GeneralNifAbiComplete':False,'DefaultNativeHandlerInstalled':False,'WholeCompatibilityComplete':False,'FreshOriginal312Verified':False,'PublicLoadNifRegistered':True,'ComparisonTestReport':'validation/part-311/tests.json','RotationTarget':'docs/changelog/0047.md'}
for key,field in [('SourcePaths','SourcePaths'),('CSharpPaths','CSharpPaths'),('ExpectedPassed','ExpectedFullPassed'),('ExpectedRegistry','ExpectedRegistry'),('ExpectedErlang','ExpectedErlangPresence'),('ExpectedNativeInputs','ReviewedSourceInputs'),('ExpectedOptionalIo','ExpectedOptionalIoRegistry')]:c[key]=f[field]
for key in ['AddedHostNames']:c[key]=f[key]
for key in ['GuardAssertions','FormatGroups','MetadataPaths','StageMetadataPaths','AllowedTextLfSources','ActualValidationSentence','ChangelogIndexOldLabel','ChangelogIndexNewLabel','ChangelogIndexAppendLabel']:c[key]=a[key]
c.update({key:pin(value) for key,value in a['EvidenceBindings'].items()})
full=m.read(m.pinned(c['Tests']));old=m.read(m.pinned(c['PreviousTests']));assert full['Passed']==c['ExpectedPassed'] and full['Failed']==0
prior={x['Name'] for x in old['Tests']};c['AddedNames']=[x['Name'] for x in full['Tests'] if x['Name'] not in prior]
for key,dest in [('Tests','tests.json'),('Integration','integration-results.json'),('DerivedRegistry','supported-mfas.json'),('ReadinessResult','module-readiness.json')]:add(c[key]['Path'],'docs/validation/part-312/'+dest,'actual-outputs')
add('docs/MODULE_READINESS.md','docs/validation/part-312/MODULE_READINESS.md','actual-outputs')
index=b/'evidence-index.json';index.write_text(json.dumps(rows,indent=2)+'\n');c['EvidenceIndex']=pin(index.relative_to(m.ROOT).as_posix())
(b/'config-actual.json').write_text(json.dumps(c,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print(json.dumps({'IndexedFiles':len(rows),'SourcePaths':c['SourcePaths'],'CSharpPaths':c['CSharpPaths'],'RootExecutionApproved':c['RootExecutionApproved'],'PreparationOnly':prepare}))
