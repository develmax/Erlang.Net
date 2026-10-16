from pathlib import Path
import ast,json,hashlib
R=Path.cwd();A=R/'artifacts/proc-lib-real-peer-reader-draft2';O=R/'artifacts/proc-lib-real-peer-reader-draft3';O.mkdir(exist_ok=True)
old='78D4C5B9268520DED2C79C846894DEA8CAFC7DCB9BD07A68A6564E26BF595032';new='61E52EC3EEE23FD4084772C0E383470C7FD09DAC6A6F16D7BD50AD3A6CB85229';sha=lambda b:hashlib.sha256(b).hexdigest().upper()
previous=(A/'source-freeze.json').read_bytes();prior=json.loads(previous)
for row in prior['Files']:assert sha((A/row['Path']).read_bytes())==row['Sha256']
reader=(A/'read.py').read_bytes();assert reader.count(old.encode())==1
changed=reader.replace(old.encode(),new.encode());(O/'read.py').write_bytes(changed)
cross=(A/'source-crosswalk.json').read_bytes();assert cross.count(old.encode())==1
(O/'source-crosswalk.json').write_bytes(cross.replace(old.encode(),new.encode()))
history='''
Revision3 binds the corrected collector manifest61E52EC3EEE23FD4084772C0E383470C7FD09DAC6A6F16D7BD50AD3A6CB85229. The reader executable changes only that manifest constant; its eight-case contracts, transport, digest/extraction, loaded-image provenance and normal cleanup checks are unchanged from revision2. The source-only crosswalk changes only its manifest binding, with no promotion of observational statuses.

Preserve the failed published342 acquisition and its raw refusal: the first case records ProcessLookupError and only peer-options, with no peer identity/spawn result; seven other acquisitions are partial evidence rather than a complete eight-case comparison. The exact historical syscall and ordering of the peer crash relative to cleanup are unproven. Deterministic source controls confirm the process-discovery ESRCH hole and corrected behavior; they do not establish a fresh native run. Revision2 and its original manifest remain immutable historical readers.

No future published head, run, job or artifact is guessed here. All future values remain explicit required CLI inputs. This revision has only been parsed and source-compared; the reader has not been run against a native capture. No SDK, network or Main source mutation occurred.
'''
(O/'README.md').write_text((A/'README.md').read_text(encoding='utf-8').rstrip()+'\n\n'+history.strip()+'\n',encoding='utf-8')
before=ast.parse(reader);after=ast.parse(changed)
for tree in (before,after):
 assign=next(n for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='MANIFEST' for t in n.targets));assert assign.value.value in [old,new];assign.value.value='REVIEWED_MANIFEST_PLACEHOLDER'
assert ast.dump(before,include_attributes=False)==ast.dump(after,include_attributes=False)
control={'Status':'PASS_SOURCE_ONLY_MANIFEST_LITERAL_DELTA','ExecutableBytesChangeOnlyManifest':True,'AllOtherReaderAstExactRevision2':True,'CrosswalkBytesChangeOnlyManifest':True,'RequiredFutureCliIdentityArgumentsPreserved':True,'ActualCaptureExecuted':False,'MainModified':False,'NetworkUsed':False,'SdkExecuted':False}
(O/'source-controls.json').write_text(json.dumps(control,indent=2)+'\n',encoding='utf-8')
rows=[]
for name in ['read.py','README.md','source-crosswalk.json','source-controls.json']:
 b=(O/name).read_bytes();rows.append({'Path':name,'Bytes':len(b),'Sha256':sha(b)})
freeze={'Status':'PASSIVE_REAL_PEER_READER_REVISION3_SOURCE_ONLY','PreviousSourceFreezeSha256':sha(previous),'OriginalSourceManifestSha256':new,'PreviousOriginalSourceManifestSha256':old,'Files':rows,'ExecutableChanges':['One MANIFEST constant only'],'HistoricalPublished342FailureRetained':True,'PythonAstParsed':True,'ReaderExecuted':False,'NativeExecuted':False,'SdkExecuted':False,'NetworkUsed':False,'MainEdited':False}
(O/'source-freeze.json').write_text(json.dumps(freeze,indent=2)+'\n',encoding='utf-8');print(json.dumps({'FreezeSha256':sha((O/'source-freeze.json').read_bytes()),'ReaderSha256':sha(changed)}))
