from pathlib import Path
import json,hashlib,shutil
r=Path.cwd();a=r/'artifacts';root=a/'process-info-logical-prototype';p=root/'src/Erlang.Runtime/ProcessSuspensions.cs';shutil.copyfile(r/'src/Erlang.Runtime/ProcessSuspensions.cs',p);s=p.read_text();s=s.replace('    private sealed class Owner(ProcessContext context)', '''    internal Term SnapshotOwned(Pid owner)
    {
        lock (gate)
        {
            var entries = new List<Term>();
            foreach (var pair in targets)
            {
                if (!pair.Value.Owners.TryGetValue(owner, out var suspension))
                    continue;
                entries.Add(Term.Tuple(
                    pair.Key,
                    Term.I(pair.Value.Active ? suspension.Count : 0),
                    Term.I(pair.Value.Active ? 0 : suspension.Count)
                ));
            }

            return Cons.From(entries);
        }
    }

    private sealed class Owner(ProcessContext context)''',1);p.write_bytes(s.encode());p=root/'src/Erlang.Runtime/ProcessInfoLogicalNames.cs';s=p.read_text().replace('    public const string Reductions', '    public const string Suspending = "suspending";\n    public const string Reductions',1);p.write_bytes(s.encode());p=root/'src/Erlang.Runtime/ProcessInfoAtoms.cs';s=p.read_text().replace('ProcessInfoLogicalNames.Priority or ProcessInfoLogicalNames.Reductions or ', 'ProcessInfoLogicalNames.Priority or ProcessInfoLogicalNames.Reductions or ProcessInfoLogicalNames.Suspending or ',1);p.write_bytes(s.encode());p=root/'src/Erlang.Runtime/ProcessRuntime.cs';s=p.read_text().replace('                    ProcessInfoLogicalNames.InitialCall =>','                    ProcessInfoLogicalNames.Suspending => Suspensions.SnapshotOwned(process.Self),\n                    ProcessInfoLogicalNames.InitialCall =>',1);p.write_bytes(s.encode());p=root/'src/Erlang.Runtime/ProcessInfoItem.cs';shutil.copyfile(r/'src/Erlang.Runtime/ProcessInfoItem.cs',p);s=p.read_text().replace('Name is ProcessInfoNames.Links', 'Name is ProcessInfoLogicalNames.Suspending or ProcessInfoNames.Links',1);p.write_bytes(s.encode())
new=[('suspending-empty','-module(process_info_logical). -export([run/0]). run()->{suspending,[]}=process_info(self(),suspending),done.'),('suspending-active-counts','-module(process_info_logical). -export([run/0]). run()->{P,R}=spawn_monitor(fun()->receive stop->ok end end),true=suspend_process(P),{suspending,[{P,1,0}]}=process_info(self(),suspending),true=suspend_process(P),{suspending,[{P,2,0}]}=process_info(self(),suspending),true=resume_process(P),{suspending,[{P,1,0}]}=process_info(self(),suspending),true=resume_process(P),{suspending,[]}=process_info(self(),suspending),P!stop,receive {\'DOWN\',R,process,P,normal}->done end.')];p=root/'tools/Erlang.Differential/ProcessInfoLogicalCases.cs';s=p.read_text();at=s.rfind('    ];');add=',\n'+',\n'.join('        new(\n            '+json.dumps('process-info-logical-'+n)+',\n            '+json.dumps(source)+',\n            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))\n        )' for n,source in new)+'\n';s=s[:at].rstrip()+add+s[at:];p.write_bytes(s.encode());p=root/'tests/Erlang.Tests/ProcessInfoLogicalHostChecks.cs';s=p.read_text().replace('ProcessInfoLogicalNames.Priority, ProcessInfoLogicalNames.ErrorHandler }','ProcessInfoLogicalNames.Priority, ProcessInfoLogicalNames.ErrorHandler, ProcessInfoLogicalNames.Suspending }');p.write_bytes(s.encode())
prior=a/'process-info-logical-source-freeze-revision3.json';x=json.loads(prior.read_text());sha=lambda b:hashlib.sha256(b).hexdigest().upper()
for path in ['src/Erlang.Runtime/ProcessSuspensions.cs','src/Erlang.Runtime/ProcessInfoItem.cs']:
 p=r/path;x['Files'].append({'Path':path,'BeforeSha256':sha(p.read_bytes()),'BeforeSource':p.read_text()})
for f in x['Files']:
 p=root/f['Path'];f['SourceContent']=p.read_text();f['Sha256']=sha(p.read_bytes())
x['PreviousFreezeSha256']=sha(prior.read_bytes());x['CompiledCases']=7;x['MissingDefaultFields']=['total_heap_size','heap_size','stack_size','garbage_collection'];x['SuspendingSourceModel']='Existing requester-specific long owner counts and active target state; gate-protected snapshot with original {Pid,Active,Pending} shape';p=a/'process-info-logical-source-freeze-revision4.json';p.write_bytes((json.dumps(x,indent=2)+'\n').encode());print(len(x['Files']),sha(p.read_bytes()))
