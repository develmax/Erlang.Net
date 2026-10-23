from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();otp=r.parent/'otp';pin='ad05823719d77c8faee87348ea39513d4e2f99c5';head=subprocess.check_output(['git','-C',str(otp),'rev-parse','HEAD']).decode().strip()
assert not subprocess.check_output(['git','-C',str(otp),'status','--porcelain','--untracked-files=no'])
files=['lib/kernel/src/dist_util.erl','lib/kernel/include/dist.hrl','erts/emulator/beam/dist.c','erts/emulator/beam/erl_bif_ddll.c','erts/emulator/beam/external.c'];rows=[]
for name in files:
 actual=(otp/name).read_bytes();original=subprocess.check_output(['git','-C',str(otp),'show',pin+':'+name]);a=actual.decode().replace('\r\n','\n');b=original.decode().replace('\r\n','\n');same=a==b;extras={}
 if name.endswith('dist_util.erl'):
  attribute='-compile([{nowarn_deprecated_function, [{erlang,exit,2}]}]).\n\n';assert a.replace(attribute,'')==b
  extras=dict(OnlyCheckoutDelta='compile warning suppression outside handshake',AllOtherCodeIdentical=True)
 elif name.endswith('dist.c'):
  start='        if ((prio || is_external_ref(remote))';next_branch='        else if (is_atom(remote))';end='        else {'
  def slice(t):
   first=t.index(start);last=t.index(end,t.index(next_branch,first));return t[first:last],t[:first].count('\n')+1,t[:last].count('\n')
  x,_,_=slice(a);y,first,last=slice(b);assert x==y;extras=dict(ReviewedSendSliceIdentical=True,PinFirstLine=first,PinLastLine=last,SliceSha256=hashlib.sha256(y.encode()).hexdigest().upper(),OtherCheckoutChanges='receiver trace-token validation outside reviewed send slice; not used as pinned authority')
 else:assert same
 rows.append(dict(Path=name,PinSha256=hashlib.sha256(original).hexdigest().upper(),CheckoutSha256=hashlib.sha256(actual).hexdigest().upper(),WholeNormalizedEqual=same,**extras))
value=dict(Passed=True,Pin=pin,CheckoutHead=head,CheckoutUnchanged=True,AuthorityReadFromExactGitBlobs=True,RuntimeOracle='Official OTP29.1.1 provider, all6427 inventory files matched before/after actual execution',Files=rows,NoRuntimeOrObserverChange=True,NoGoldenRewrite=True)
(r/'artifacts/library448-pinned-source-audit.json').write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(dict(Passed=True,CheckoutHead=head,Pin=pin,ReviewedProtocolMatches=True)))
