import pathlib,json,hashlib,subprocess,sys
main=pathlib.Path(__file__).resolve().parents[1]
freeze_path=main/'artifacts/distribution-packet-promotion-source-freeze-final.json'
expected_freeze='10A1454BB0E02498DF0E5792F3182BB068266106C071D64AA3145D677D8D6CF3'
expected_head='f26e1a15320553eb9d725764ec2260f3c262e1da'
def sha(raw):return hashlib.sha256(raw).hexdigest().upper()
assert sha(freeze_path.read_bytes())==expected_freeze
freeze=json.loads(freeze_path.read_text())
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=main).decode().strip()==expected_head
assert freeze['Receipt']['Status']=='PASS_SCOPED_PROMOTION' and not freeze['Receipt']['NativeExecuted']
assert len(freeze['Files'])==25 and len({row['Path'] for row in freeze['Files']})==25
checks=[];writes=[]
for row in freeze['Files']:
 relative=pathlib.PurePosixPath(row['Path']);assert not relative.is_absolute() and '..' not in relative.parts
 path=main.joinpath(*relative.parts);assert path.resolve().is_relative_to(main.resolve())
 before=path.read_bytes() if path.exists() else None
 assert (sha(before) if before is not None else None)==row['BeforeSha256'],row['Path']
 git=subprocess.run(['git','show',expected_head+':'+row['Path']],cwd=main,capture_output=True)
 if before is None:
  assert git.returncode!=0,row['Path']
  before_binding='AbsentInWorkingAndPublishedGit'
 else:
  assert row['Path']=='tests/Erlang.Tests/Program.cs' and git.returncode==0
  assert git.stdout.decode().replace('\r\n','\n')==before.decode().replace('\r\n','\n')
  before_binding='RawWorkingSHAExactAndPublishedGitUtf8TextExactWithOnlyCRLFcanonicalization'
 raw=row['SourceContent'].encode('utf8');assert sha(raw)==row['Sha256'],row['Path']
 writes.append((path,raw));checks.append({'Path':row['Path'],'BeforeBinding':before_binding,'BeforeSha256':row['BeforeSha256'],'Sha256':row['Sha256']})
# All before/after bindings validate before the first write. No Git/index/publication operation is performed.
for path,raw in writes:path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(raw)
assert all(sha(path.read_bytes())==sha(raw) for path,raw in writes)
receipt={'Status':'PASS','Head':expected_head,'FreezeSha256':expected_freeze,'Imported':25,'Checks':checks,'CoreRegistrationsAdded':0,'NativeExecuted':False,'FullMainValidationExecuted':False}
output=main/'artifacts/distribution-packet-main-import-receipt.json';output.write_text(json.dumps(receipt,indent=2));print(json.dumps({'Status':'PASS','Imported':25,'Receipt':str(output)},indent=2))
