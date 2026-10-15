"""Artifact-only explicit authority binder; no source/tree scans or docs mutations."""
from pathlib import Path
import hashlib,json,sys,ast
r=Path.cwd();a=r/'artifacts';b=Path(__file__).resolve().parent;h=lambda data:hashlib.sha256(data).hexdigest().upper();j=lambda p:json.loads(p.read_bytes())
authority_path=Path(sys.argv[1]) if len(sys.argv)>1 else b/'composite-authority.json';authority=j(authority_path)
assert authority['Part']==308 and authority['SourceFreeze']['Sha256']=='C1A2377E970ECDE5E2AF5C298D4EBC32BD0E45E59FD6F9EFDC2EB57BE6F5492D'
def pin(name):
 p=r/name;return {'Path':name,'Bytes':p.stat().st_size if p.is_file() else -1,'Sha256':h(p.read_bytes()) if p.is_file() else 'PENDING_ACTUAL_TERMINAL_EVIDENCE'}
assert pin(authority['SourceFreeze']['Path'])==authority['SourceFreeze'];f=j(r/authority['SourceFreeze']['Path']);rows=[]
def add(source,dest,category):
 actual=pin(source);assert actual['Bytes']>=0
 if any(x['Destination']==dest for x in rows):assert next(x for x in rows if x['Destination']==dest)['Sha256']==actual['Sha256'];return
 assert 'dispatch' not in Path(source).name.lower() and 'stage-index' not in Path(source).name.lower() or category=='previous307'
 rows.append({'SourcePath':source,'Destination':dest,'Bytes':actual['Bytes'],'Sha256':actual['Sha256'],'Category':category})
previous={}
for kind,name in [('ci','pack307-ci-detached-staging-manifest.json'),('native','pack307-native-detached-staging-manifest.json'),('original','pack307-original-detached-staging-manifest-review2.json')]:
 mp=a/name;previous[kind]=pin(mp.relative_to(r).as_posix())
 for row in j(mp):assert pin(row['SourcePath'])['Sha256']==row['Sha256'];rows.append(dict(row,Category='previous307'))
 add(mp.relative_to(r).as_posix(),'docs/validation/part-308/staging-manifests/'+name,'previous307')
portable=a/'part308-composite-portable1'
for relative in authority['PortableFiles']:
 assert not relative.startswith('/') and '..' not in Path(relative).parts
 add((portable/relative).relative_to(r).as_posix(),'docs/validation/part-308/composite-portable/'+relative,'composite-portable')
for source in authority['RootProofPaths']:
 if (r/source).is_file():add(source,'docs/validation/part-308/root-artifacts/'+Path(source).name,'root-explicit-allowlist')
for source in authority['IndependentReviewPaths']:
 add(source,'docs/validation/part-308/independent-reviews/'+Path(source).name,'independent-review')
for source in authority['PackagingControlPaths']:
 add(source,'docs/validation/part-308/packaging-proposal2/'+Path(source).name,'packaging-control')
c={'Part':308,'BoundToActualComposite':True,'ParentHead':f['ParentHead'],'SourceFreeze':authority['SourceFreeze'],'CompositeAuthority':pin(authority_path.resolve().relative_to(r.resolve()).as_posix()),'Previous307Manifests':previous,'GeneralNifAbiComplete':False,'DefaultNativeHandlerInstalled':False,'WholeCompatibilityComplete':False,'FreshOriginal308Verified':False,'ComparisonTestReport':'validation/part-307/tests.json'}
for key,field in [('SourcePaths','SourcePaths'),('CSharpPaths','CSharpPaths'),('ExpectedPassed','ExpectedFullPassed'),('ExpectedRegistry','ExpectedRegistry'),('ExpectedErlang','ExpectedErlangPresence'),('ExpectedNativeInputs','ReviewedSourceInputs')]:c[key]=f[field]
c['ExpectedOptionalIo']=f['ExpectedOptionalIoRegistry'];c['AddedHostNames']=f['AddedHostNames'];c['AllowedTextLfSources']=authority['AllowedTextLfSources'];c['GuardAssertions']=authority['GuardAssertions']
c.update({key:pin(value) for key,value in authority['EvidenceBindings'].items()})
full=j(r/c['Tests']['Path']);old=j(r/c['PreviousTests']['Path']);oldnames={x['Name'] for x in old['Tests']};c['AddedNames']=[x['Name'] for x in full['Tests'] if x['Name'] not in oldnames] if full['Passed']==c['ExpectedPassed'] and full['Failed']==0 else []
c['MetadataPaths']=authority['MetadataPaths'];c['StageMetadataPaths']=authority['StageMetadataPaths'];c['ChangelogIndexOldLabel']='parts302–307 (active)';c['ChangelogIndexNewLabel']='parts302–308 (active)'
for key,dest in [('Tests','tests.json'),('Integration','integration-results.json'),('DerivedRegistry','supported-mfas.json'),('ReadinessResult','module-readiness.json')]:
 if (r/c[key]['Path']).is_file():add(c[key]['Path'],'docs/validation/part-308/'+dest,'actual-outputs')
add('docs/MODULE_READINESS.md','docs/validation/part-308/MODULE_READINESS.md','actual-outputs')
(b/'evidence-index.json').write_text(json.dumps(rows,indent=2)+'\n');c['EvidenceIndex']=pin((b/'evidence-index.json').relative_to(r).as_posix());(b/'config-actual.json').write_text(json.dumps(c,indent=2)+'\n')
for script in b.glob('*.py'):ast.parse(script.read_text(encoding='utf-8-sig'))
print(json.dumps({'IndexedFiles':len(rows),'SourcePaths':c['SourcePaths'],'CSharpPaths':c['CSharpPaths'],'ExpectedFromFrozenAuthority':c['ExpectedPassed'],'ActualFullComplete':(full['Passed'],full['Failed'])==(c['ExpectedPassed'],0),'NoRecursiveSourceScans':True,'DispatchAndCurrentStageGuardExcluded':True}))
