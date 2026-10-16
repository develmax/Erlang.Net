from pathlib import Path
import json,hashlib,shutil
root=Path(__file__).resolve().parents[1]
old=root/'artifacts/process-error-handler327-candidate2'
out=root/'artifacts/process-error-handler327-candidate3'
assert not out.exists();out.mkdir()
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
def dump(p,v):p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
assert sha((old/'source-freeze.json').read_bytes())=='66DBA0592260D83E129F481FC331EF9E0C21944CF878ED872E21CD2461550151'
shutil.copytree(old/'candidate',out/'candidate')
checks='tests/Erlang.Tests/ProcessErrorHandlerHostChecks.cs'
names='tests/Erlang.Tests/ProcessErrorHandlerHostNames.cs'
p=out/'candidate'/checks;s=p.read_text()
before='''        runtime.Modules.Register(ProcessErrorHandlerHostNames.Bridge, ProcessErrorHandlerHostNames.Held, 3,
            async (context, arguments) =>'''
assert s.count(before)==1;s=s.replace(before,'        ErlangFunction bridge = async (context, arguments) =>')
before='''                CheckActive(context, source.Definition, image, arguments);
                unwound.TrySetResult();
            }
        });
        ProcessHandle process'''
after='''                try
                {
                    CheckActive(context, source.Definition, image, arguments);
                }
                finally
                {
                    unwound.TrySetResult();
                }
            }
        };
        NifAdmissionHostBridge.Call(runtime.Modules, ProcessErrorHandlerHostNames.RegisterIntrinsic,
            ProcessErrorHandlerHostNames.Bridge, ProcessErrorHandlerHostNames.Held, 3, bridge);
        ProcessHandle process'''
assert s.count(before)==1;s=s.replace(before,after)
before='''        Require((await a).Equals(Term.Tuple(Term.A(ProcessErrorHandlerHostNames.Handler), first.Process.Pid)));
        Require((await b).Equals(Term.Tuple(Term.A(ProcessErrorHandlerHostNames.OtherHandler), second.Process.Pid)));'''
after='''        await Task.WhenAll(a, b).WaitAsync(ProcessErrorHandlerHostNames.Deadline);
        Require(a.Result.Equals(Term.Tuple(Term.A(ProcessErrorHandlerHostNames.Handler), first.Process.Pid)));
        Require(b.Result.Equals(Term.Tuple(Term.A(ProcessErrorHandlerHostNames.OtherHandler), second.Process.Pid)));'''
assert s.count(before)==1;s=s.replace(before,after);p.write_text(s,encoding='utf-8',newline='\n')
p=out/'candidate'/names;s=p.read_text();needle='    internal const string ActiveCode = "activeCode";';assert s.count(needle)==1
s=s.replace(needle,needle+'\n    internal const string RegisterIntrinsic = "RegisterIntrinsic";');p.write_text(s,encoding='utf-8',newline='\n')
changed={checks,names}
closure=json.loads((old/'source-resource-closure.json').read_bytes())
for row in closure['Files']:
 data=(out/'candidate'/row['Path']).read_bytes()
 if row['Path'] in changed:row.update(Bytes=len(data),Sha256=sha(data))
 else:assert len(data)==row['Bytes'] and sha(data)==row['Sha256']
dump(out/'source-resource-closure.json',closure)
freeze=json.loads((old/'source-freeze.json').read_bytes())
freeze['CandidateDirectory']=str(out/'candidate')
freeze['FinalSourceClosureSha256']=sha((out/'source-resource-closure.json').read_bytes())
for row in freeze['Files']:
 if row['Path'] in changed:
  data=(out/'candidate'/row['Path']).read_bytes();row.update(Bytes=len(data),Sha256=sha(data))
freeze['ParentCandidateFreezeSha256']=sha((old/'source-freeze.json').read_bytes())
freeze['Revision']='Only HOST Checks/Names: existing reflected RegisterIntrinsic bridge preserves real source topflow; guarantee unwind signal on assertion; bound both isolation joins. Other13 owned files exact including all9 production.'
freeze['PublishedParentPreflightPath']='artifacts/process-error-handler327-candidate1/published-parent-source-preflight.json'
freeze['PublishedParentPreflightSha256']=sha((root/freeze['PublishedParentPreflightPath']).read_bytes())
note=(old/'SOURCE_NOTE.txt').read_text()+'\nRevision3 HOST-only prerequisites: existing RegisterIntrinsic reflected through existing bridge, no new hook/friend; actual declared method (string,string,int,ErlangFunction) and activeCode field inspected. Guaranteed unwind signal and bounded both-operation join prevent masking assertion with timeout. Candidate1 unsupported Capture fifth arg and candidate2 bridge topflow issue never built/executed. Known nested frame-order risk remains unresolved.\n'
(out/'SOURCE_NOTE.txt').write_text(note,encoding='utf-8',newline='\n');freeze['NoteSha256']=sha((out/'SOURCE_NOTE.txt').read_bytes())
dump(out/'source-freeze.json',freeze)
dump(out/'host-precondition-revision.json',{'Status':'SOURCE_ONLY_HOST_PRECONDITION_REPAIR','PriorFreezeSha256':sha((old/'source-freeze.json').read_bytes()),'ChangedPaths':sorted(changed),'Production9RawExact':True,'NewProductionIntrospection':False,'SdkVmBuildExecuted':False})
print(json.dumps({'FreezeSha256':sha((out/'source-freeze.json').read_bytes()),'ClosureSha256':freeze['FinalSourceClosureSha256']}))
