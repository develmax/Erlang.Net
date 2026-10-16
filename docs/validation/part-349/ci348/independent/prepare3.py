from pathlib import Path
import json,ast
p=Path('artifacts/successor348-ci-independent');s=(p/'review2.py').read_text();env={'__file__':str(p/'review2.py')};exec(s.split('import argparse',1)[0],env)
sha=env['sha'];failure=json.loads((p/'reader2-failure.json').read_bytes());paths=[x['Path'] for x in failure['Differences']]
pub=env['blobs']('d1bd571fc1f655abf81bdc0336b7133f3d023fac',paths);old=env['blobs']('2ae381b4f0258d9846bb5b0fc7b9f00ed46b905b',paths);rows=[]
for x in failure['Differences']:
 path=x['Path'];assert path.endswith(('.csproj','.slnx'));raw=(Path('artifacts/term-order348-candidate')/path).read_bytes()
 assert sha(raw)==x['Expected']['Sha256'] and len(raw)==x['Expected']['Bytes']
 assert pub[path]==old[path] and pub[path].replace(b'\r\n',b'\n')==raw.replace(b'\r\n',b'\n')
 rows.append({'Path':path,'FrozenBytes':len(raw),'FrozenSha256':sha(raw),'PublishedBytes':len(pub[path]),'Published348Sha256':sha(pub[path]),'Published347Sha256':sha(old[path]),'OnlyCrlfLfDifference':True})
assert len(rows)==22
proof={'Status':'PASS_TWENTYTWO_INHERITED_NONCS_BUILD_INPUT_EOL_ONLY_DIFFERENCES','Rows':rows,'InitialTwentyDescriptionCountCorrection':22,'NoCSharpDifferences':True,'FrozenCandidateRawHashesExact':True,'NoCaptureSdkNetworkMain':True}
(p/'build-eol-proof.json').write_text(json.dumps(proof,indent=2)+'\n',encoding='utf-8')
line="for row in buildrows:assert len(buildblobs[row['Path']])==row['Bytes'] and sha(buildblobs[row['Path']])==row['Sha256']"
replacement="""build_eol=[]
eolproof=read(O/'build-eol-proof.json');expectedbuild={r['Path']:r for r in eolproof['Rows']};assert len(expectedbuild)==22
oldbuild=blobs(priorhead,expectedbuild)
for row in buildrows:
 actual=buildblobs[row['Path']]
 if len(actual)!=row['Bytes'] or sha(actual)!=row['Sha256']:
  path=row['Path'];assert path in expectedbuild and path.endswith(('.csproj','.slnx'))
  frozen=(A/'term-order348-candidate'/path).read_bytes();assert len(frozen)==row['Bytes'] and sha(frozen)==row['Sha256']==expectedbuild[path]['FrozenSha256']
  assert actual==oldbuild[path] and sha(actual)==expectedbuild[path]['Published348Sha256']==expectedbuild[path]['Published347Sha256'] and actual.replace(b'\\r\\n',b'\\n')==frozen.replace(b'\\r\\n',b'\\n');build_eol.append(path)
assert set(build_eol)==set(expectedbuild)"""
assert s.count(line)==1;s=s.replace(line,replacement).replace("'FrozenRuntimeBuildInputsRehashedAtPublishedHead':len(buildrows)","'FrozenRuntimeBuildInputsRehashedAtPublishedHead':len(buildrows),'ExplicitInheritedNonCsBuildInputEolOnlyDifferences':build_eol,'BuildEolProofSha256':sha((O/'build-eol-proof.json').read_bytes()),'Reader2OverstrictRawClosureRefusalPreserved':True")
ast.parse(s);(p/'review3.py').write_text(s,encoding='utf-8',newline='\n')
freeze={'Status':'SOURCE_ONLY_PASSIVE348_CI_READER_REVISION3','PreviousSourceFreezeSha256':sha((p/'source-freeze2.json').read_bytes()),'Files':[{'Path':n,'Sha256':sha((p/n).read_bytes())} for n in ['review3.py','build-eol-proof.json','reader2-failure.json','prepare3.py']],'Change':'Exactly22 inherited csproj/slnx publishedLF/frozenCRLF rows, raw frozen hashes+both347/348published bytes checked; no C# exception or normalization of runtime results','CaptureExecuted':False,'Sdk':False,'Network':False,'MainChanged':False}
(p/'source-freeze3.json').write_text(json.dumps(freeze,indent=2)+'\n',encoding='utf-8');print(sha((p/'source-freeze3.json').read_bytes()))
