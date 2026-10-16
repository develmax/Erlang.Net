from pathlib import Path
import json,hashlib,subprocess
r=Path.cwd();h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());s=r/'artifacts/non-tail-stack329-root-focus2';d=r/'artifacts/non-tail-stack329-root-candidate5';assert not d.exists();assert h(s/'formatted-source-freeze.json')=='C3887D93B55DCA16DED9E707BBAF1B6C7ED52071B92C887BE4B351BE544A881C';f=read(s/'formatted-source-freeze.json');rows=read(s/'formatted-source-resource-closure.json')['Files'];assert len(rows)==2397;assert subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD']).decode().strip()=='bb71b9ce04b21e4ec58ba1d62028f3a2c33c3612';d.mkdir();c=d/'candidate';c.mkdir()
for x in rows:
 q=s/'candidate'/x['Path'];assert h(q)==x['Sha256'];dest=c/x['Path'];dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(q.read_bytes())
p=c/'tests/Erlang.Tests/NonTailStackLifecycleHostChecks.cs';text=p.read_text();old='            replacement.Definition.Register(runtime.Modules);';assert text.count(old)==1;text=text.replace(old,'            Require(runtime.Modules.DeleteModule(source.Definition.Name).Equals(Term.A(ErlangBooleanAtoms.True)));\n'+old);p.write_text(text,encoding='utf-8')
p=c/'src/Erlang.Compiler/Execution.cs';text=p.read_text();old='''            context.Cancellation.ThrowIfCancellationRequested();
            await Task.Yield();
            context.Runtime.ThrowIfHalted();
            context.Cancellation.ThrowIfCancellationRequested();
        }
        using var frame''';new='''            context.Cancellation.ThrowIfCancellationRequested();

            await Task.Yield();

            context.Runtime.ThrowIfHalted();
            context.Cancellation.ThrowIfCancellationRequested();
        }

        using var frame''';assert text.count(old)==2;text=text.replace(old,new);p.write_text(text,encoding='utf-8')
owned={x['Path'] for x in f['Files']};newrows=[dict(Path=x['Path'],Bytes=(c/x['Path']).stat().st_size,Sha256=h(c/x['Path'])) for x in rows];cl=d/'source-resource-closure.json';cl.write_text(json.dumps({'Files':newrows},indent=2)+'\n');f.update(Status='SOURCE_ONLY_RECURSION4096_AND_CORRECTED_DELETE_REGISTER_LIFECYCLE',ParentHead='bb71b9ce04b21e4ec58ba1d62028f3a2c33c3612',SourceClosureSha256=h(cl),Files=[x for x in newrows if x['Path'] in owned],ReplacedFailedFocusFreezeSha256=h(s/'formatted-source-freeze.json'),FailedFocusRetained=True,ExpectedFutureFullPassed=6747,NativePlansNotResealed=True,NoMainWrites=True);(d/'source-freeze.json').write_text(json.dumps(f,indent=2)+'\n');print(json.dumps({'FreezeSha256':h(d/'source-freeze.json'),'ClosureSha256':h(cl)}))
