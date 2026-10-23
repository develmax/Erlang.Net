from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();pin='ad05823719d77c8faee87348ea39513d4e2f99c5';otp=r.parent/'otp';rows=[]
for name in ['lib/kernel/src/dist_util.erl','lib/kernel/src/erl_epmd.erl','lib/kernel/src/net_kernel.erl','lib/kernel/src/net_adm.erl','erts/emulator/beam/erl_bif_ddll.c','erts/emulator/beam/external.c','erts/emulator/beam/erl_node_tables.c']:
 raw=subprocess.check_output(['git','-C',str(otp),'show',pin+':'+name]);rows.append({'Path':name,'PinnedSha256':hashlib.sha256(raw).hexdigest().upper(),'Bytes':len(raw),'Authority':'exact pinned Git blob, not checkout HEAD'})
receipt={'Passed':True,'PinnedCommit':pin,'CheckoutHead':subprocess.check_output(['git','-C',str(otp),'rev-parse','HEAD']).decode().strip(),'ActualOracle':'unchanged admitted OTP29.1.1 release provider, not source-built checkout HEAD','SourceFiles':rows,'ProductionSourceChanges':subprocess.check_output(['git','diff','--name-only','HEAD','--','src'],cwd=r).decode().splitlines()};assert not receipt['ProductionSourceChanges']
(r/'artifacts/library450-source-authority.json').write_text(json.dumps(receipt,indent=2)+'\n')
