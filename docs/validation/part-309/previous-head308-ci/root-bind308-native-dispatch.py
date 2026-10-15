from pathlib import Path
import re,hashlib
r=Path.cwd();p=r/'artifacts/root-dispatch308-native.ps1';s=p.read_text()
assert not (r/'artifacts/root-part308-native-dispatch-attempt.json').exists()
line=next(x for x in s.splitlines() if 'Get-FileHash tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json' in x)
old=re.search(r"-ne '([0-9A-F]+)'",line).group(1)
new=hashlib.sha256((r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json').read_bytes()).hexdigest().upper()
assert new=='29FC1D27A9A11D03C9928C50DDB6A900D5F235ADE731FE3E3934F66181E7CFA0'
before=r/'artifacts/root-dispatch308-native-initial-hash-precondition.ps1';assert not before.exists();before.write_bytes(p.read_bytes())
assert s.count(old)==1;p.write_text(s.replace(old,new))
print({'PriorPlanHashLiteral':old,'CurrentPlanHashLiteral':new,'PostAttemptExists':False})
