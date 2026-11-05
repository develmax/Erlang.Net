import argparse,hashlib,json,platform,subprocess
from pathlib import Path
parser=argparse.ArgumentParser()
parser.add_argument('--repository',type=Path,required=True)
parser.add_argument('--otp-root',type=Path,required=True)
parser.add_argument('--output',type=Path,required=True)
parser.add_argument('--bits',type=int,choices=[32,64],required=True)
args=parser.parse_args()
root=args.repository.resolve();otp=args.otp_root.resolve();out=args.output.resolve()
assert out.is_relative_to(root/'artifacts') and not out.exists()
out.mkdir(parents=True)
source=root/'tools/Native/Erlang.NifPlatformValidation'
subprocess.run([str(otp/'bin/erlc'),'+debug_info','-o',str(out),str(source/'nif_resource_fixture.erl')],check=True)
subprocess.run([str(otp/'bin/erlc'),'+debug_info','-o',str(out),str(source/'nif_resource_oracle.erl')],check=True)
command=['cc','-m'+str(args.bits),'-std=c11','-shared','-fPIC','-O2','-Wall','-Wextra','-Werror','-I'+str(otp/'usr/include'),str(source/'nif_resource_fixture.c'),'-o',str(out/'nif_resource_fixture.so')]
subprocess.run(command,check=True)
expression='true = erlang:system_info(wordsize) =:= '+str(args.bits//8)+', nif_resource_oracle:run('+json.dumps(str(out/'nif_resource_fixture'))+','+json.dumps(str(out/'original.etf'))+'),halt().'
subprocess.run([str(otp/'bin/erl'),'+S','2:2','-noshell','-pa',str(out),'-eval',expression],check=True,timeout=90)
sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
files=[source/'nif_resource_oracle.erl',source/'nif_resource_fixture.erl',source/'nif_resource_fixture.c',out/'nif_resource_fixture.beam',out/'original.etf']
(out/'provenance.json').write_text(json.dumps({'OtpPin':json.loads((root/'reference-baseline.json').read_text())['commit'],'Host':platform.platform(),'OriginalResourceNifExecuted':True,'WordBytes':args.bits//8,'Files':{f.name:sha(f) for f in files},'Command':command},indent=2)+'\n')
