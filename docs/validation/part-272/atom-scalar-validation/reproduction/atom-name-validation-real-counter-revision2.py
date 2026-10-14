from pathlib import Path
import json,hashlib,shutil
r=Path.cwd();p=r/'artifacts/atom-name-validation-prototype';e=r/'artifacts/atom-name-validation-evidence'
shutil.copyfile(e/'new-focus.json',e/'initial-unsupported-atom-count-focus.json');shutil.copyfile(e/'new-build.log',e/'initial-validation-only-build.log')
f=p/'src/Erlang.Runtime/SequenceTraceRegistrations.cs';s=f.read_text();needle='''            ? ValueTask.FromResult<Term>(Term.Tuple(Term.A(SequenceTraceNames.SystemTracer), context.Runtime.SequenceTracing.Tracer()))
            : CpuTopologyStateRegistrations.TrySystemInfo''';assert s.count(needle)==1;s=s.replace(needle,'''            ? ValueTask.FromResult<Term>(Term.Tuple(Term.A(SequenceTraceNames.SystemTracer), context.Runtime.SequenceTracing.Tracer()))
            : arguments[0] is Atom { Name: AtomRegistryInfoNames.AtomCount }
            ? ValueTask.FromResult<Term>(Term.I(AtomNameRegistry.Count))
            : CpuTopologyStateRegistrations.TrySystemInfo''');f.write_text(s)
f=p/'tests/Erlang.Tests/AtomNameValidationChecks.cs';s=f.read_text();needle='''        var actor = runtime.Spawn(async context =>
        {
            var list''';assert s.count(needle)==1;s=s.replace(needle,'''        var actor = runtime.Spawn(async context =>
        {
            Term reportedCount = await runtime.Modules.Call(context, ErlangModuleNames.Module, SequenceTraceNames.SystemInfo, Term.A(AtomRegistryInfoNames.AtomCount));
            Check(reportedCount.Equals(Term.I(AtomNameRegistry.Count)));
            var list''');f.write_text(s)
initial=json.loads((r/'artifacts/atom-name-validation-source-freeze-initial.json').read_text());owned=[x['Path'] for x in initial['Files']]+['src/Erlang.Runtime/AtomRegistryInfoNames.cs','src/Erlang.Runtime/SequenceTraceRegistrations.cs'];rows=[]
for name in owned:
 raw=(p/name).read_bytes();old=r/name;rows.append({'Path':name,'BeforeSha256':hashlib.sha256(old.read_bytes()).hexdigest().upper() if old.exists() else None,'Sha256':hashlib.sha256(raw).hexdigest().upper(),'BeforeSource':old.read_text() if old.exists() else None,'SourceContent':raw.decode()})
freeze={'Stage':'Revision2RealAtomCountPreBuild','Files':rows,'InitialSourceExpectedCases8Unchanged':True,'InitialGeneratedPass':6,'InitialGeneratedFail':2,'FailureCause':'Existing system_info(atom_count) selector was unsupported; sourceexpected native nopollution contracts unchanged. Add actual registry counter, no numeric baseline or synthetic heap statistics.','NativeExecuted':False,'CoreRegistrationsAdded':0,'ProductionFiles':5,'InitialFreezeSha256':hashlib.sha256((r/'artifacts/atom-name-validation-source-freeze-initial.json').read_bytes()).hexdigest().upper()};f=r/'artifacts/atom-name-validation-source-freeze-revision2.json';f.write_text(json.dumps(freeze,indent=2));print('Revision2prebuild SHA',hashlib.sha256(f.read_bytes()).hexdigest().upper())
style=e/'owned-style-root'
for name in owned:
 target=style/name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p/name,target)
