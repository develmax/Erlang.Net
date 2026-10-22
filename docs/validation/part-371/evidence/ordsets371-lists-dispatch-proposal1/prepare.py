"""Artifact-only extraction of source kernels behind real lists dispatch."""
from pathlib import Path
import hashlib,json,subprocess
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def emit(name,text):
    path=HERE/'source'/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_text(text,encoding='utf-8')
def main():
    base=ROOT/'src/Erlang.Runtime/OrdsetFullCallbacks.cs';old=base.read_text()
    fold=old[old.index('    internal static async ValueTask<Term> Fold('):old.index('    internal static async ValueTask<Term> Transform(')]
    fold=fold.replace('    internal static async ValueTask<Term> Fold(','    private static async ValueTask<Term> Fold(').replace('        using var tail = context.EnterSourceTailCall(OrdsetCoreNames.Module, OrdsetFullNames.Fold);\n','')
    body=old[old.index('    private static async ValueTask<Term> Filter('):old.rfind('\n}')]
    body=body.replace('bool map = operation == OrdsetFullNames.Map;','bool map = operation == ListModuleNames.Map;')
    run='''namespace Erlang;

internal static class ListCallbackSourceExecution
{
    internal static async ValueTask<Term> Run(
        ProcessContext context,
        string operation,
        Term function,
        Term list,
        Term? accumulator
    )
    {
        using var tail = context.EnterSourceTailCall(ListModuleNames.Module, operation);
        if (operation == ListModuleNames.FoldLeft)
            return await Fold(context, function, accumulator!, list);
        if (operation == ListModuleNames.Filter)
            return await Filter(context, function, list);

        return await TransformList(context, operation, function, list);
    }

    internal static async ValueTask<Term> LegacyFilterMap(ProcessContext context, Term function, Term list)
    {
        using var tail = context.EnterSourceTailCall(ListModuleNames.Module, ListModuleNames.FilterMapLegacy);

        return await context.Runtime.Modules.Call(
            context,
            ListModuleNames.Module,
            ListModuleNames.FilterMap,
            [function, list]
        );
    }

'''
    emit('src/Erlang.Runtime/ListCallbackSourceExecution.cs',run+fold+body+'\n}\n')
    emit('src/Erlang.Runtime/OrdsetFullCallbacks.cs','''namespace Erlang;

internal static class OrdsetFullCallbacks
{
    internal static async ValueTask<Term> Fold(
        ProcessContext context,
        Term function,
        Term accumulator,
        Term set
    )
    {
        using var tail = context.EnterSourceTailCall(OrdsetCoreNames.Module, OrdsetFullNames.Fold);

        return await context.Runtime.Modules.Call(
            context,
            ListModuleNames.Module,
            ListModuleNames.FoldLeft,
            [function, accumulator, set]
        );
    }

    internal static async ValueTask<Term> Transform(
        ProcessContext context,
        string operation,
        Term function,
        Term set
    )
    {
        using var tail = context.EnterSourceTailCall(OrdsetCoreNames.Module, operation);
        if (operation == OrdsetFullNames.Filter)
            return await context.Runtime.Modules.Call(
                context,
                ListModuleNames.Module,
                ListModuleNames.Filter,
                [function, set]
            );
        string listOperation = operation == OrdsetFullNames.Map ? ListModuleNames.Map : ListModuleNames.FilterMap;
        Term transformed;
        using (OrdsetFullTrace.Enter(
            context,
            new(
                OrdsetCoreNames.Module,
                operation,
                2,
                operation == OrdsetFullNames.Map ? OrdsetFullNames.MapCall : OrdsetFullNames.FilterMapCall
            )
        ))
            transformed = await context.Runtime.Modules.Call(
                context,
                ListModuleNames.Module,
                listOperation,
                [function, set]
            );

        return await OrdsetFullExecution.Delegate(context, OrdsetFullNames.FromList, transformed);
    }
}
''')
    path=ROOT/'src/Erlang.Runtime/ListFunctions.cs';text=path.read_text();anchor='        if (operation == ListModuleNames.FoldRight)';assert text.count(anchor)==1
    text=text.replace(anchor,'''        if (operation is ListModuleNames.FoldLeft or ListModuleNames.Filter or ListModuleNames.Map or ListModuleNames.FilterMap)
            return await ListCallbackSourceExecution.Run(
                context,
                operation,
                function,
                list,
                accumulator
            );
'''+anchor)
    emit('src/Erlang.Runtime/ListFunctions.cs',text)
    path=ROOT/'src/Erlang.Runtime/ListModuleRegistrations.cs';text=path.read_text();oldblock='''            (c, a) => ListFunctions.Run(
                c,
                ListModuleNames.FilterMap,
                a[0],
                a[1]
            )''';newblock='''            (c, a) => ListCallbackSourceExecution.LegacyFilterMap(c, a[0], a[1])''';assert text.count(oldblock)==1;text=text.replace(oldblock,newblock);emit('src/Erlang.Runtime/ListModuleRegistrations.cs',text)
    # Raw pinned source is passive attribution/evidence; never compiled or executed by this preparation.
    pin='ad05823719d77c8faee87348ea39513d4e2f99c5';authority=[]
    for name in ['lib/stdlib/src/ordsets.erl','lib/stdlib/src/lists.erl']:
        raw=subprocess.check_output(['git','-C',str(ROOT.parent/'otp'),'show',pin+':'+name]);p=HERE/'authority'/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw);authority.append(dict(Path=name,Bytes=len(raw),Sha256=H(p)))
    (HERE/'source-review.json').write_text(json.dumps(dict(PinnedOtpCommit=pin,Source=authority,ActualDynamicModuleDispatch=True,RegistryStampsAdded=False,BeforeAfterModuleQueries=False,ProductionApiCountDelta=0,BranchReview=[dict(Mfa='ordsets:fold/3',Pinned='fold(F,Acc,Set)->lists:foldl(F,Acc,Set)',Frame='tail; remove only own SourceWrapper; actual selected lists code owns validation'),dict(Mfa='ordsets:filter/2',Pinned='filter(F,Set)->lists:filter(F,Set)',Frame='tail; actual selected lists code'),dict(Mfa='ordsets:map/2',Pinned='from_list(lists:map(F,Set))',Frame='keep real ordsets:map/2 continuation at543 during actual lists call; remove before tail usort'),dict(Mfa='ordsets:filtermap/2',Pinned='from_list(lists:filtermap(F,Set))',Frame='keep real ordsets:filtermap/2 continuation at581 during actual lists call; remove before tail usort'),dict(Mfa='lists:zf/2',Pinned='zf(F,L)->filtermap(F,L)',Frame='alias strips only own zf SourceWrapper then dispatches actual lists:filtermap body')],KernelExtraction='Existing Fold/Filter/TransformList branches copied without semantic edits except correct module owner and public operation name. Existing OrdsetFullTrace/frame/source constants reused to avoid unrelated refactor. Other ListFunctions operations retained byte-for-byte after new four-operation dispatch.'),indent=2)+'\n',encoding='utf-8')
    print('Runtime source proposal prepared only; no build/test/Main write')
if __name__=='__main__':main()
