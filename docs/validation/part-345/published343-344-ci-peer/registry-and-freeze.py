from pathlib import Path
import hashlib,json,subprocess
R=Path.cwd();O=R/'artifacts/successor343-344-ci-independent';sha=lambda b:hashlib.sha256(b).hexdigest().upper()
heads=['0114bc4ba2c7b8152612e930e3cd36db1caf4180','f26d913cf9bb9e27576f01f55eb9b78ec19fa57c','9c7949f87348223a61c65d37d859b02841036374']
raw=[subprocess.check_output(['git','show',h+':docs/supported-mfas.json'],cwd=R) for h in heads]
assert raw[0]==raw[1]==raw[2] and len(json.loads(raw[0]))==587
proof={'Status':'PASS_ALL587_REGISTRY_ROWS_RAW_UNCHANGED_342_THROUGH344','Heads':heads,'RegistrySha256':sha(raw[0]),'Rows':587,'AllRowOrderEvidenceAndCustomLimitsRawUnchanged':True,'NoSdkOrNetwork':True}
p=O/'registry-prior-preservation.json';assert not p.exists();p.write_text(json.dumps(proof,indent=2)+'\n',encoding='utf-8')
files=[]
for name in ['review.py','registry-and-freeze.py','part343-ci-independent-guard.json','part344-ci-independent-guard.json','registry-prior-preservation.json']:
 b=(O/name).read_bytes();files.append({'Path':name,'Bytes':len(b),'Sha256':sha(b)})
f=O/'source-and-actual-freeze.json';assert not f.exists();f.write_text(json.dumps({'Status':'FROZEN_PASSIVE_SUCCESSOR_CI_READER_AND_ACTUAL_PROOFS','Files':files,'SdkExecuted':False,'NetworkUsed':False,'MainModified':False},indent=2)+'\n',encoding='utf-8');print(json.dumps({'FreezeSha256':sha(f.read_bytes()),'RegistryGuardSha256':sha(p.read_bytes())}))
