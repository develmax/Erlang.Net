import argparse,hashlib,json
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--repository',type=Path,required=True);parser.add_argument('--observed',type=Path,required=True);args=parser.parse_args()
root=args.repository.resolve();source=root/'tools/Native/Erlang.NifPlatformValidation';actual=args.observed.read_bytes()
for bits in [32,64]:
 oracle=root/('artifacts/resource-oracle'+str(bits));metadata=json.loads((oracle/'provenance.json').read_text());assert metadata['WordBytes']==bits//8 and metadata['OriginalResourceNifExecuted'] and metadata['OtpPin']==json.loads((root/'reference-baseline.json').read_text())['commit']
 assert actual==(oracle/'original.etf').read_bytes()
 for name in ['nif_resource_fixture.c','nif_resource_fixture.erl','nif_resource_oracle.erl']:
  assert hashlib.sha256((source/name).read_bytes()).hexdigest()==metadata['Files'][name]
 assert hashlib.sha256((oracle/'nif_resource_fixture.beam').read_bytes()).hexdigest()==metadata['Files']['nif_resource_fixture.beam']
print(json.dumps({'Passed':True,'OriginalErtsWordBytes':[4,8],'EtfSha256':hashlib.sha256(actual).hexdigest(),'OracleSourceIdentityVerified':True}))
