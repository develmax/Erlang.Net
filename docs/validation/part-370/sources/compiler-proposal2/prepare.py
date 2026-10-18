from pathlib import Path
import json,hashlib,shutil,difflib

BASE=Path(__file__).resolve().parent;ROOT=BASE.parents[1]
OLD=ROOT/'artifacts/erlang370-element-continuation-proposal1'
SRC=BASE/'source';SRC.mkdir(exist_ok=True)
plan=json.loads((OLD/'copy-plan.json').read_text())
for row in plan['Rows']:
 source=OLD/row['Source'];dest=BASE/row['Source'];dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,dest)
def read(name):return (SRC/name).read_text(encoding='utf-8')
def write(name,value):
 p=SRC/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(value,encoding='utf-8',newline='\n')
def replace(name,old,new):
 s=read(name);assert s.count(old)==1,(name,old[:90],s.count(old));write(name,s.replace(old,new))
def h(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()

write('src/Erlang.Compiler/SourceBifForwardingContracts.cs','''namespace Erlang.Compiler;

internal static class SourceBifForwardingContracts
{
    internal static Expr.Call? Resolve(ModuleDefinition module, Expr.Call call, out int indexSlot, out int tupleSlot)
    {
        indexSlot = tupleSlot = -1;
        if (call.Module is not null)
            return null;
        var function = module.Functions.FirstOrDefault(value => value.Name == call.Function && value.Arity == call.Arguments.Count);
        if (function is null || function.Clauses.Count != 1 || function.Clauses[0].Guard is not null)
            return null;
        var clause = function.Clauses[0];
        if (clause.Patterns.Any(pattern => pattern is not Pattern.Variable { Name: not SourceBifReturnParameters.DiscardedVariable }))
            return null;
        var names = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
        if (names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            return null;
        Expr body = clause.Body;
        while (body is Expr.Block || body is Expr.Sequence { Items.Count: 1 })
            body = body is Expr.Block current ? current.Body : ((Expr.Sequence)body).Items[0];
        if (body is not Expr.Call target || target.Function != ErlangModuleNames.Element
            || target.Arguments is not [Expr.Variable index, Expr.Variable tuple]
            || !(target.Module == ErlangModuleNames.Module
                || target.Module is null && SourceCallResolution.IsGuardAutoImport(module, target.Function, target.Arguments.Count)))
            return null;
        indexSlot = Array.IndexOf(names, index.Name);
        tupleSlot = Array.IndexOf(names, tuple.Name);
        if (indexSlot < 0 || tupleSlot < 0
            || call.Arguments[indexSlot] is not Expr.Literal { Value: Integer value }
            || value.Value <= SourceBifReturnParameters.MaximumTupleArity)
            return null;

        return target;
    }
}
''')
write('src/Erlang.Compiler/SourceBifReturnObservation.cs','''namespace Erlang.Compiler;

internal sealed class SourceBifReturnObservation : IDisposable
{
    private static readonly AsyncLocal<SourceBifReturnObservation?> Active = new();
    private readonly object gate = new();
    private SourceBifReturnObservation? previous;
    private ModuleDefinition? module;
    private ProcessContext? context;
    private Expr.Call? target;
    private Term? index;
    private Term? tuple;
    private Term? result;
    private int hits;
    private bool closed;

    private SourceBifReturnObservation(ModuleDefinition owner, ProcessContext caller, Expr.Call body, Term position, Term value)
    {
        module = owner;
        context = caller;
        target = body;
        index = position;
        tuple = value;
        previous = Active.Value;
        Active.Value = this;
    }

    internal static SourceBifReturnObservation? Enter(ModuleDefinition? module, ProcessContext context, Expr.Call call, Term[] arguments)
    {
        if (module is null || !call.CanDiscardBifContinuation || SourceReturnSafety.HasNativeOverrides(module, context)
            || SourceBifForwardingContracts.Resolve(module, call, out int indexSlot, out int tupleSlot) is not { } target)
            return null;

        return new(module, context, target, arguments[indexSlot], arguments[tupleSlot]);
    }

    internal static void Record(ModuleDefinition? module, ProcessContext context, Expr.Call call, Term[] arguments, Term result, bool canonical)
    {
        if (!canonical || arguments.Length != SourceBifReturnParameters.ElementArity || Active.Value is not { } active)
            return;
        lock (active.gate)
        {
            if (active.closed || !ReferenceEquals(active.module, module) || !ReferenceEquals(active.context, context)
                || !ReferenceEquals(active.target, call) || !active.index!.Equals(arguments[0])
                || !ReferenceEquals(active.tuple, arguments[1]) || SourceReturnSafety.HasNativeOverrides(module!, context))
                return;
            active.result = result;
            active.hits++;
        }
    }

    internal bool Matches(Term value)
    {
        lock (gate)
            return !closed && hits == 1 && ReferenceEquals(result, value);
    }

    public void Dispose()
    {
        SourceBifReturnObservation? restore;
        lock (gate)
        {
            if (closed)
                return;
            closed = true;
            restore = previous;
            previous = null;
            module = null;
            context = null;
            target = null;
            index = tuple = result = null;
        }
        if (ReferenceEquals(Active.Value, this))
            Active.Value = restore;
    }
}
''')
replace('src/Erlang.Compiler/SourceBifContinuationBindings.cs',
 'permitted && SourceBifReturnContracts.IsCandidate(module, call)',
 'permitted && (SourceBifReturnContracts.IsCandidate(module, call) || SourceBifForwardingContracts.Resolve(module, call, out _, out _) is not null)')
replace('src/Erlang.Compiler/SourceBifReturnParameters.cs',
 '    internal const int ElementArity = 2;',
 '    internal const string DiscardedVariable = "_";\n    internal const int ElementArity = 2;')
replace('src/Erlang.Compiler/SourceBifContinuationBindings.cs',
 '''                case Expr.Case conditional:''',
 '''                case Expr.Match matched:
                    foreach (var child in LocalFunctionEntries.Children(matched))
                        Visit(child, ReferenceEquals(child, matched.Value) && permitted);
                    return;
                case Expr.MaybeMatch matched:
                    foreach (var child in LocalFunctionEntries.Children(matched))
                        Visit(child, ReferenceEquals(child, matched.Value) && permitted);
                    return;
                case Expr.Maybe maybe:
                    foreach (var item in maybe.Items)
                        Visit(item, permitted);
                    VisitClauses(maybe.Clauses, permitted);
                    return;
                case Expr.Case conditional:''')
replace('src/Erlang.Compiler/SourceBifContinuationBindings.cs',
 '''                case Expr.MaybeMatch matched:
                    foreach (var child in LocalFunctionEntries.Children(matched))
                        Visit(child, ReferenceEquals(child, matched.Value) && permitted);''',
 '''                case Expr.MaybeMatch maybeMatched:
                    foreach (var child in LocalFunctionEntries.Children(maybeMatched))
                        Visit(child, ReferenceEquals(child, maybeMatched.Value) && permitted);''')
replace('src/Erlang.Compiler/SourceBifContinuationBindings.cs',
 '''                case Expr.If conditional:
                    VisitClauses(conditional.Clauses, permitted);''',
 '''                case Expr.If conditionalIf:
                    VisitClauses(conditionalIf.Clauses, permitted);''')
replace('src/Erlang.Compiler/Execution.cs',
 '''                    if (module is not null && x.Module is null && module.Functions.Any(f => f.Name == x.Function && f.Arity == args.Length))
                    {
                        if (tail)
                            throw new TailCall(x.Function, args, ctx.ExecutingSourceImage(module));

                        return await InvokeAsync(
                            module,
                            x.Function,
                            ctx,
                            args
                        );
                    }''',
 '''                    if (module is not null && x.Module is null && module.Functions.Any(f => f.Name == x.Function && f.Arity == args.Length))
                    {
                        if (tail)
                            throw new TailCall(x.Function, args, ctx.ExecutingSourceImage(module));

                        using var observation = SourceBifReturnObservation.Enter(module, ctx, x, args);
                        var result = await InvokeAsync(
                            module,
                            x.Function,
                            ctx,
                            args
                        );
                        if (observation?.Matches(result) == true)
                            throw new SourceContinuationReturn(result);

                        return result;
                    }''')
replace('src/Erlang.Compiler/Execution.cs',
 '''                        if (!tail && x.CanDiscardBifContinuation && module is not null''',
 '''                        SourceBifReturnObservation.Record(module, ctx, x, args, result, selectedElementSourceContract);
                        if (!tail && x.CanDiscardBifContinuation && module is not null''')
write('src/Erlang.Runtime/ModuleRegistry.ElementSourceContract.cs','''namespace Erlang;

public sealed partial class ModuleRegistry
{
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<ErlangFunction, ModuleCodeImage> elementSourceContracts = new();

    internal void SealElementSourceContract()
    {
        lock (gate)
        {
            var body = functions[(ErlangModuleNames.Module, ErlangModuleNames.Element, 2)];
            elementSourceContracts.Add(body, codeImages[ErlangModuleNames.Module]);
        }
    }

    private bool IsElementSourceContract(
        (string Module, string Function, int Arity) signature,
        ErlangFunction? body,
        ModuleCodeImage? image
    ) => signature == (ErlangModuleNames.Module, ErlangModuleNames.Element, 2)
        && body is not null && elementSourceContracts.TryGetValue(body, out var canonicalImage)
        && ReferenceEquals(canonicalImage, image);
}
''')
rows=[]
for p in sorted(SRC.rglob('*.cs')):
 n=p.relative_to(SRC).as_posix();base=ROOT/n
 row={'Source':'source/'+n,'Target':n,'Sha256':h(p),'Existing':base.exists()}
 if base.exists():
  row['BaseSha256']=h(base)
  (BASE/(base.name+'.diff')).write_text(''.join(difflib.unified_diff(base.read_text(encoding='utf-8-sig').replace('\r\n','\n').splitlines(True),p.read_text().splitlines(True),fromfile=n,tofile=n)),encoding='utf-8')
 rows.append(row)
(BASE/'copy-plan.json').write_text(json.dumps({'SourceOnly':True,'Built':False,'Rows':rows,'Scope':'Direct literal out-of-domain element plus one-hop pure variable-only local forwarder; no fixture names or captured result atoms.'},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Files':len(rows),'Existing':sum(r['Existing'] for r in rows),'New':sum(not r['Existing'] for r in rows)}))
