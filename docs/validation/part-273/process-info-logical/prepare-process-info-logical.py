from pathlib import Path
import shutil,json,hashlib,subprocess
r=Path.cwd();a=r/'artifacts';root=a/'process-info-logical-prototype';root.mkdir(exist_ok=True)
for n in ['Directory.Build.props','Directory.Packages.props','global.json','NuGet.Config']:
 if (r/n).exists():shutil.copyfile(r/n,root/n)
for top in ['src','tools/Erlang.Differential']:
 for p in (r/top).rglob('*'):
  if p.is_file() and not any(v in ['bin','obj'] for v in p.parts):
   d=root/p.relative_to(r);d.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,d)
for n in ['tests','benchmarks','examples']:(root/n).mkdir(exist_ok=True)
owned=[]
def edit(path,old,new):
 p=root/path;s=p.read_text();assert old in s,path;s=s.replace(old,new,1);p.write_bytes(s.encode());owned.append(path)
edit('src/Erlang.Runtime/ProcessContext.cs','    private int reductions;','''    private int reductions;
    private long cumulativeReductions;
    internal Term InitialCall { get; set; } = ProcessInfoAtoms.Undefined;
    internal Atom ErrorHandler { get; } = Term.A(ProcessInfoLogicalNames.DefaultErrorHandler);
    internal Atom SchedulingPriority { get; } = Term.A(ProcessInfoLogicalNames.NormalPriority);
    internal long CumulativeReductions => Interlocked.Read(ref cumulativeReductions);''')
p=root/'src/Erlang.Runtime/ProcessContext.cs';s=p.read_text().replace('        reductions += count;','        Interlocked.Add(ref cumulativeReductions, count);\n        reductions += count;',1);p.write_bytes(s.encode())
edit('src/Erlang.Runtime/ProcessRuntime.cs','            ctx.GroupLeader = parent?.GroupLeader ?? processes[InitializationPid].GroupLeader;','''            ctx.GroupLeader = parent?.GroupLeader ?? processes[InitializationPid].GroupLeader;
            ctx.InitialCall = description?.Mfa ?? ProcessInfoAtoms.Undefined;''')
p=root/'src/Erlang.Runtime/ProcessRuntime.cs';s=p.read_text().replace('                    ProcessInfoNames.Links =>','''                    ProcessInfoLogicalNames.InitialCall => process.InitialCall,
                    ProcessInfoLogicalNames.ErrorHandler => process.ErrorHandler,
                    ProcessInfoLogicalNames.Priority => process.SchedulingPriority,
                    ProcessInfoLogicalNames.Reductions => Term.I(process.CumulativeReductions),
                    ProcessInfoNames.Links =>''',1);p.write_bytes(s.encode())
edit('src/Erlang.Runtime/ProcessInfoAtoms.cs','    public static bool IsSupported(Atom key) => key.Name is ','''    public static bool IsSupported(Atom key) => key.Name is ProcessInfoLogicalNames.InitialCall or ProcessInfoLogicalNames.ErrorHandler or
        ProcessInfoLogicalNames.Priority or ProcessInfoLogicalNames.Reductions or ''')
p=root/'src/Erlang.Runtime/ProcessInfoLogicalNames.cs';p.write_bytes(b'''namespace Erlang;

public static class ProcessInfoLogicalNames
{
    public const string InitialCall = "initial_call";
    public const string ErrorHandler = "error_handler";
    public const string DefaultErrorHandler = "error_handler";
    public const string Priority = "priority";
    public const string NormalPriority = "normal";
    public const string Reductions = "reductions";
}
''');owned.append(str(p.relative_to(root)).replace('\\','/'))
cases=[('defaults','-module(process_info_logical). -export([run/0]). run()->{error_handler,error_handler}=process_info(self(),error_handler),{priority,normal}=process_info(self(),priority),done.'),('charged-reductions-monotonic','-module(process_info_logical). -export([run/0]). run()->{reductions,A}=process_info(self(),reductions),true=is_integer(A),true=(A>=0),true=erlang:bump_reductions(4000),{reductions,B}=process_info(self(),reductions),true=(B>=A+4000),done.'),('spawn-module-initial-mfa','-module(process_info_logical). -export([run/0,child/1,nested/1]). run()->{P,R}=spawn_monitor(?MODULE,child,[self()]),receive {P,{initial_call,{process_info_logical,child,1}}}->ok end,receive {\'DOWN\',R,process,P,normal}->done end. child(Owner)->nested(Owner). nested(Owner)->Owner!{self(),process_info(self(),initial_call)},ok.'),('spawn-callback-initial-mfa','-module(process_info_logical). -export([run/0]). run()->Owner=self(),{P,R}=spawn_monitor(fun()->Owner!{self(),process_info(self(),initial_call)} end),receive {P,{initial_call,{erlang,apply,2}}}->ok end,receive {\'DOWN\',R,process,P,normal}->done end.'),('logical-list-order-and-duplicates','-module(process_info_logical). -export([run/0]). run()->[{priority,normal},{error_handler,error_handler},{priority,normal},{reductions,R}]=process_info(self(),[priority,error_handler,priority,reductions]),true=is_integer(R),done.')]
s='namespace Erlang.Differential;\n\npublic static class ProcessInfoLogicalCases\n{\n    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =\n    [\n'+',\n'.join('        new(\n            '+json.dumps('process-info-logical-'+n)+',\n            '+json.dumps(source)+',\n            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))\n        )' for n,source in cases)+'\n    ];\n}\n';p=root/'tools/Erlang.Differential/ProcessInfoLogicalCases.cs';p.write_bytes(s.encode());owned.append(str(p.relative_to(root)).replace('\\','/'))
p=root/'tools/Erlang.Differential/CompiledModuleCases.cs';s=p.read_text();at=s.rfind('];');assert at>0;s=s[:at].rstrip()+', .. ProcessInfoLogicalCases.Modules\n    '+s[at:];p.write_bytes(s.encode());owned.append(str(p.relative_to(root)).replace('\\','/'))
sha=lambda b:hashlib.sha256(b).hexdigest().upper();files=[]
for path in owned:
 old=r/path;new=root/path;files.append({'Path':path,'BeforeSha256':sha(old.read_bytes()) if old.exists() else None,'BeforeSource':old.read_text() if old.exists() else None,'Sha256':sha(new.read_bytes()),'SourceContent':new.read_text()})
x={'Stage':'BeforeBuildSourceOnly','BaseHead':subprocess.check_output(['git','rev-parse','HEAD']).decode().strip(),'Files':files,'ProcessInfo1Registered':False,'PhysicalMetricsInvented':False,'MissingDefaultFields':['total_heap_size','heap_size','stack_size','garbage_collection','suspending'],'RemainingLogicalGaps':['custom error_handler dispatch and flag','scheduler priority options and scheduling','native initial MFA for host-only callbacks'],'NativeExecuted':False,'CompiledCases':5};p=a/'process-info-logical-source-freeze-initial.json';p.write_bytes((json.dumps(x,indent=2)+'\n').encode());print(len(files),sha(p.read_bytes()))
