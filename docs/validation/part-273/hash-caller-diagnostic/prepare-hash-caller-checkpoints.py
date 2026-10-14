from pathlib import Path
import json,hashlib,shutil
r=Path(__file__).resolve().parents[1];a=r/'artifacts';prior=a/'hash-checkpoint-diagnostic-source';d=a/'hash-caller-checkpoint-source';d.mkdir(exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
for name in ['ModuleRegistry.cs','ErlangHashRegistrations.cs','HashDiagnosticCheckpoints.cs','Program.cs']:shutil.copyfile(prior/name,d/name)
s=(prior/'ErlangHashHostChecks.cs').read_text()
warm='''                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangHashCheckParameters.ImprovedFunction,
                    binary,
                    Term.I(ErlangHashCheckParameters.FullRange)
                );'''
measured=warm.replace('await runtime.Modules.Call','ValueTask<Term> pending = runtime.Modules.Call')
def split_call(name,invoke):
 return f'''                ModuleRegistry {name}Registry = runtime.Modules;
                HashDiagnosticCheckpoints.Mark(40);
                Term[] {name}Arguments = new Term[2];
                HashDiagnosticCheckpoints.Mark(41);
                {name}Arguments[0] = binary;
                HashDiagnosticCheckpoints.Mark(42);
                {name}Arguments[1] = Term.I(ErlangHashCheckParameters.FullRange);
                HashDiagnosticCheckpoints.Mark(43);
                {invoke} {name}Registry.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangHashCheckParameters.ImprovedFunction,
                    {name}Arguments
                );'''
assert s.count(warm)==s.count(measured)==1
s=s.replace(warm,split_call('warmup','await')).replace(measured,split_call('measured','ValueTask<Term> pending ='))
(d/'ErlangHashHostChecks.cs').write_bytes(s.encode())
raw=(r/'src/Erlang.Terms/Term.cs').read_bytes();term=raw.decode('utf-8-sig').replace('\r\n','\n')
old='    public static Integer I(long value) => new(value);'
assert term.count(old)==1
term=term.replace(old,'''    public static Integer I(long value)
    {
        HashDiagnosticCheckpoints.Mark(50);
        BigInteger diagnosticValue = value;
        HashDiagnosticCheckpoints.Mark(52);
        Integer diagnosticInteger = new(diagnosticValue);
        HashDiagnosticCheckpoints.Mark(53);
        return diagnosticInteger;
    }''')
(d/'Term.cs').write_bytes(term.encode())
caller=json.loads((a/'hash-caller-actual-main-il.json').read_bytes());termil=json.loads((a/'hash-term-actual-main-il.json').read_bytes())
assert [x['TokenName'] for x in termil['Instructions'] if x['Opcode'] in ['call','newobj']]==['op_Implicit','.ctor']
ledger={'Status':'SOURCE_ONLY_PROPOSED_ACTUAL_IL_ORDER_AWAITING_ROOT_REVIEW','NativeExecuted':False,'MainModified':False,'HeavyExecuted':False,'OriginalBudget':4096,'OriginalWarmupCalls':1,'Registered':6526,'ExecutedPrefix':60,'ActualMainCallerIlSha256':sha((a/'hash-caller-actual-main-il.json').read_bytes()),'ActualMainTermIlSha256':sha((a/'hash-term-actual-main-il.json').read_bytes()),'ActualMainTestDllSha256':caller['AssemblySha256'],'ActualMainTermsDllSha256':termil['AssemblySha256'],'OriginalTermSourceSha256':sha(raw),'OrderPreserved':['runtime.Modules getter','new Term[2]','store binary at index0','Term.I(full range) implicit BigInteger conversion then Integer allocation','store integer at index1','ModuleRegistry.Call'],'NewStageNames':{'40':'after Modules getter/before params-array allocation','41':'after params-array allocation','42':'after binary slot store/before Term.I','43':'after Term.I and integer slot store/before Call','50':'Term.I entry/before BigInteger implicit conversion','52':'after BigInteger implicit conversion/before Integer newobj','53':'after Integer newobj'},'CollectorMovesFromIsolatedRuntimeToIsolatedTerms':True,'NoCircularDependencyIntroduced':True,'IsolatedTermsRuntimeTestAssembliesRequired':True,'NoAllocatingReflectionOrSerializationInsideInterval':True,'SourceAndJitShapePerturbationPossible':True,'HistoricalCauseUnproved':True,'CandidateFiles':[{'Path':p.name,'Sha256':sha(p.read_bytes())} for p in sorted(d.iterdir()) if p.is_file()]}
(a/'hash-caller-checkpoint-proposed-ledger.json').write_bytes((json.dumps(ledger,indent=2)+'\n').encode())
print('Caller/Terms source-only proposal bound to actual IL evaluation order.')
