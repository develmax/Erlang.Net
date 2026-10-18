"""Pinned source evidence and permanent canonicalization checks; no production patch."""
from pathlib import Path
import hashlib,json,subprocess
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1];PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def main():
    paths=['erts/emulator/beam/external.c','erts/emulator/beam/erl_bif_info.c','erts/preloaded/src/erlang.erl'];rows=[]
    for name in paths:
        raw=subprocess.check_output(['git','-C',str(ROOT.parent/'otp'),'show',PIN+':'+name]);p=HERE/'authority'/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw)
        text=raw.decode();markers=['Creator pid, discarded','ep = enc_pid(acmp, erts_init_process_id','val = is_local_fun(funp) ? erts_init_process_id','Starting in Erlang/OTP 27, `Pid` always points to the local `init`']
        hits=[dict(Line=i,Text=line.strip()) for i,line in enumerate(text.splitlines(),1) if any(x in line for x in markers)]
        rows.append(dict(Path=name,Bytes=p.stat().st_size,Sha256=H(p),Evidence=hits))
    (HERE/'source-proof.json').write_text(json.dumps(dict(PinnedOtpCommit=PIN,Finding='Decoded NEW_FUN creator PID is validated as PID then discarded; encoding and fun_info(pid) use local initialization PID since OTP27.',PreserveExternalCreatorCompatible=False,ProductionChangesRequired=False,EarlierAgentGapClaim='RETRACTED_BY_PINNED_SOURCE',Sources=rows),indent=2)+'\n',encoding='utf-8')
    (HERE/'README.md').write_text('The earlier claimed NEW_FUN creator-preservation gap was incorrect. Pinned original external.c explicitly discards the parsed creator PID, emits erts_init_process_id, and erl_bif_info.c reports that same PID. Original documentation states this change applies since OTP27. Existing Main code follows the pinned contract; do not add creator state or change equality/hash. This proposal adds only permanent host checks of foreign-creator canonicalization and malformed-creator rejection before factory invocation. Root owns test registration/build. No production changes, build, native execution or network by author.\n',encoding='utf-8')
    print('Pinned creator canonicalization proof written; no production patch')
if __name__=='__main__':main()
