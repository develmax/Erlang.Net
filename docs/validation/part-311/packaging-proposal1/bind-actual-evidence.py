"""Explicit pinned authority; no wildcard artifact scans, docs mutation or source copies."""
from pathlib import Path
import hashlib,json,sys,ast
r=Path.cwd();b=Path(__file__).resolve().parent;h=lambda data:hashlib.sha256(data).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());ap=Path(sys.argv[1]) if len(sys.argv)>1 else b/'composite-authority.json';authority=j(ap)
assert authority['Part']==311 and authority['Status']=='ROOT_REVIEWED_ACTUAL_FINAL311_AUTHORITY';rows=[]
def pin(name):
 p=r/name;data=p.read_bytes();return {'Path':name,'Bytes':len(data),'Sha256':h(data)}
assert pin(authority['SourceFreeze']['Path'])==authority['SourceFreeze'];f=j(r/authority['SourceFreeze']['Path']);assert f['ParentHead']=='79bfaecaa7d89a03e2248149ae55c3b5bf03e6a8'
def add(source,dest,category):
 actual=pin(source)
 if any(x['Destination']==dest for x in rows):assert next(x for x in rows if x['Destination']==dest)['Sha256']==actual['Sha256'];return
 assert ('dispatch' not in Path(source).name.lower() and 'stage-index' not in Path(source).name.lower()) or category=='previous-terminal' or (category=='root-explicit-allowlist' and source=='artifacts/root-dispatch311-native.ps1')
 rows.append({'SourcePath':source,'Destination':dest,'Bytes':actual['Bytes'],'Sha256':actual['Sha256'],'Category':category})
terminal='artifacts/pack311-previous310-terminal-proof-index.json';t=j(r/terminal)
for mp in t['ManifestPins']:
 assert pin(mp['Path'])==mp
 for row in j(r/mp['Path']):assert pin(row['SourcePath'])['Sha256']==row['Sha256'];add(row['SourcePath'],row['Destination'],'previous-terminal')
 add(mp['Path'],'docs/validation/part-311/staging-manifests/'+Path(mp['Path']).name,'previous-terminal')
add(terminal,'docs/validation/part-311/staging-manifests/'+Path(terminal).name,'previous-terminal')
for key,category,subdir in [('PortableFiles','portable','composite-portable'),('RootProofPaths','root-explicit-allowlist','root-artifacts'),('IndependentReviewPaths','independent-review','independent-reviews'),('PackagingControlPaths','packaging-control','packaging-proposal1')]:
 for source in authority[key]:add(source,'docs/validation/part-311/'+subdir+'/'+Path(source).name,category)
c={'Part':311,'BoundToActualComposite':True,'ReusedFullValidation':False,'ParentHead':f['ParentHead'],'SourceFreeze':authority['SourceFreeze'],'CompositeAuthority':pin(ap.resolve().relative_to(r.resolve()).as_posix()),'PreviousTerminalProofIndex':pin(terminal),'GeneralNifAbiComplete':False,'DefaultNativeHandlerInstalled':False,'WholeCompatibilityComplete':False,'FreshOriginal311Verified':False,'PublicLoadNifRegistered':True,'ComparisonTestReport':'validation/part-310/tests.json'}
for key,field in [('SourcePaths','SourcePaths'),('CSharpPaths','CSharpPaths'),('ExpectedPassed','ExpectedFullPassed'),('ExpectedRegistry','ExpectedRegistry'),('ExpectedErlang','ExpectedErlangPresence'),('ExpectedNativeInputs','ReviewedSourceInputs'),('ExpectedOptionalIo','ExpectedOptionalIoRegistry')]:c[key]=f[field]
c['AddedHostNames']=f['AddedHostNames'];c['AllowedTextLfSources']=authority['AllowedTextLfSources'];c['GuardAssertions']=authority['GuardAssertions'];c.update({key:pin(value) for key,value in authority['EvidenceBindings'].items()})
full=j(r/c['Tests']['Path']);old=j(r/c['PreviousTests']['Path']);assert full['Passed']==c['ExpectedPassed'] and full['Failed']==0;oldnames={x['Name'] for x in old['Tests']};c['AddedNames']=[x['Name'] for x in full['Tests'] if x['Name'] not in oldnames]
c['MetadataPaths']=authority['MetadataPaths'];c['StageMetadataPaths']=authority['StageMetadataPaths'];c['ChangelogIndexOldLabel']='parts302–310 (active)';c['ChangelogIndexNewLabel']='parts302–311 (active)'
for key,dest in [('Tests','tests.json'),('Integration','integration-results.json'),('DerivedRegistry','supported-mfas.json'),('ReadinessResult','module-readiness.json')]:add(c[key]['Path'],'docs/validation/part-311/'+dest,'actual-outputs')
add('docs/MODULE_READINESS.md','docs/validation/part-311/MODULE_READINESS.md','actual-outputs')
(b/'evidence-index.json').write_text(json.dumps(rows,indent=2)+'\n');c['EvidenceIndex']=pin((b/'evidence-index.json').relative_to(r).as_posix());(b/'config-actual.json').write_text(json.dumps(c,indent=2)+'\n')
for p in b.glob('*.py'):ast.parse(p.read_text(encoding='utf-8-sig'))
print(json.dumps({'IndexedFiles':len(rows),'PassedFromActualFreeze':c['ExpectedPassed'],'SourcePaths':c['SourcePaths'],'CSharpPaths':c['CSharpPaths'],'NativeInputs':c['ExpectedNativeInputs'],'FreshMainFullRequired':True,'NoRecursiveSourceScans':True}))
