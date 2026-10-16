from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();h=lambda q:hashlib.sha256(q.read_bytes()).hexdigest().upper();read=lambda q:json.loads(q.read_bytes());s=r/'artifacts/non-tail-stack329-root-focus1';d=r/'artifacts/non-tail-stack329-root-candidate4';assert not d.exists();assert h(s/'formatted-source-freeze.json')=='EB86DFD9644A59E45CE33677ABEDEF7ABD3C4C0C5E23FF32597C8E5828F7C032';assert subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD']).decode().strip()=='bb71b9ce04b21e4ec58ba1d62028f3a2c33c3612';assert not subprocess.check_output(['git','--no-optional-locks','diff','HEAD','--name-only']).strip();rows=read(s/'formatted-source-resource-closure.json')['Files'];assert len(rows)==2395;d.mkdir();c=d/'candidate';c.mkdir()
for x in rows:
 q=s/'candidate'/x['Path'];assert h(q)==x['Sha256'];dest=c/x['Path'];dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(q.read_bytes())
name='tools/Erlang.Differential/NonTailRecursionCaseNames.cs';q=c/name;text=q.read_text();old='public const int FiniteDepth = 256;';assert text.count(old)==1;q.write_text(text.replace(old,'public const int FiniteDepth = 4096;'),encoding='utf-8')
extra=['tests/Erlang.Tests/NonTailStackLifecycleHostNames.cs','tests/Erlang.Tests/NonTailStackLifecycleHostChecks.cs']
for n in extra:
 q=c/n;raw=(r/'artifacts/non-tail-stack329-root-lifecycle1/overlay'/n).read_bytes();
 if n.endswith('Names.cs'):raw=raw.replace(b'using System.Reflection;\n\n',b'').replace(b'using System.Reflection;\r\n\r\n',b'')
 q.write_bytes(raw)
p=c/'tests/Erlang.Tests/Program.cs';text=p.read_text();old='Test(Erlang.Tests.UndefinedLambdaPublicationHostNames.Test, Erlang.Tests.UndefinedLambdaPublicationHostChecks.Run);';assert text.count(old)==1;text=text.replace(old,old+'\nTest(Erlang.Tests.NonTailStackLifecycleHostNames.Test, Erlang.Tests.NonTailStackLifecycleHostChecks.Run);');p.write_text(text,encoding='utf-8')
owned={x['Path'] for x in read(s/'formatted-source-freeze.json')['Files']}|set(extra)|{'tests/Erlang.Tests/Program.cs'};names=sorted({x['Path'] for x in rows}|set(extra));new=[dict(Path=n,Bytes=(c/n).stat().st_size,Sha256=h(c/n)) for n in names];assert len(owned)==7 and len(new)==2397;parent={x['Path']:x for x in read(r/'artifacts/root-part328-main2/source-resource-closure.json')['Files']}
for x in new:
 if x['Path'] not in owned:assert x==parent[x['Path']]
cl=d/'source-resource-closure.json';cl.write_text(json.dumps({'Files':new},indent=2)+'\n');selectors=read(s/'formatted-source-freeze.json')['Selectors']+[f'Erlang.Tests.NonTailStackLifecycleHostChecks:{method}' for method in ['RunNormal','RunError','RunCancellation']]
f=dict(Status='SOURCE_ONLY_GUARD_PLUS_4096_RECURSION_AND_ASYNC_IMAGE_LIFECYCLE_DIAGNOSTIC',ParentHead='bb71b9ce04b21e4ec58ba1d62028f3a2c33c3612',ParentMainFreezeSha256='BA9F82F848896139AC2D4BC81FCA724C7E0A9F145A1622B838A126B3D2228542',ParentSourceClosureSha256='B86A971D4922D1F20C97BACDF08A7A99534932CC8EA29FB2DACFEDC444084302',SourcePaths=7,CSharpPaths=7,SourceClosurePaths=2397,SourceClosureSha256=h(cl),Files=[x for x in new if x['Path'] in owned],Selectors=selectors,PreviousActualFormattedFreezeSha256=h(s/'formatted-source-freeze.json'),ExpectedFutureFullPassed=6747,NativePlansNotResealed=True,OrdinaryCases=8,OneNewHostRow=True,RawOutsideOwnedExactPublished328=2390,NoMainWrites=True)
(d/'source-freeze.json').write_text(json.dumps(f,indent=2)+'\n');print(json.dumps({'FreezeSha256':h(d/'source-freeze.json'),'ClosureSha256':h(cl),'Owned':len(owned),'Source':len(new)}))
