from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
DEST=OUT/'source/src/Erlang.Runtime'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
sort=ROOT/'src/Erlang.Runtime/ListSortOperations.cs'
source=sort.read_text()
anchor='internal static class ListSortOperations\n{'
assert source.count(anchor)==1
source=source.replace(anchor,anchor+'''
    public static Term Unique(ProcessContext context, Term list)
    {
        using var tail = context.EnterSourceTailCall(ListModuleNames.Module, ListModuleNames.UniqueSort);
        if (list is not Nil && list is not Cons { Tail: Nil or Cons })
            throw ListUniqueMergeTrace.Clause(
                context,
                ListModuleNames.UniqueSort,
                [list],
                ListUniqueMergeNames.UniqueSortLine,
                []
            );

        return Sort(list, true);
    }
''')
(DEST/sort.name).write_text(source,encoding='utf-8')
merge=ROOT/'src/Erlang.Runtime/ListMergeOperations.cs'
source=merge.read_text()
anchor='''    public static async ValueTask<Term> MergeMany(ProcessContext context, Term lists, bool unique)
    {
'''
assert source.count(anchor)==1
source=source.replace(anchor,anchor+'''        if (unique)
            return await ListUniqueMergeMachine.Run(context, lists);
''')
(DEST/merge.name).write_text(source,encoding='utf-8')
registry=ROOT/'src/Erlang.Runtime/ListModuleRegistrations.cs'
source=registry.read_text()
old='''        foreach (string operation in new[] { ListModuleNames.Sort, ListModuleNames.UniqueSort })
            Add(operation, 1, a => ListSortOperations.Sort(a[0], operation == ListModuleNames.UniqueSort));'''
new='''        Add(ListModuleNames.Sort, 1, a => ListSortOperations.Sort(a[0]));
        registry.Register(
            ListModuleNames.Module,
            ListModuleNames.UniqueSort,
            1,
            (context, arguments) => ValueTask.FromResult(ListSortOperations.Unique(context, arguments[0]))
        );'''
assert source.count(old)==1
source=source.replace(old,new)
(DEST/registry.name).write_text(source,encoding='utf-8')
files=[]
for p in sorted((OUT/'source').rglob('*')):
    if not p.is_file():continue
    rel=str(p.relative_to(OUT/'source')).replace('\\','/')
    files.append({'Source':str(p.relative_to(ROOT)).replace('\\','/'),'Destination':rel,'Sha256':sha(p),'Bytes':p.stat().st_size,'ExpectedCurrentSha256':sha(ROOT/rel) if (ROOT/rel).exists() else None})
plan={'Files':files,'Edits':[],'PreservedListModuleRegistrationsZf': 'ListCallbackSourceExecution.LegacyFilterMap' in source,'RegistrationOnlyUsortBlockBefore':old,'RegistrationOnlyUsortBlockAfter':new,'NoMainWrites':True,'NoBuildNetwork':True}
(OUT/'copy-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Files':len(files),'ZfPreserved':plan['PreservedListModuleRegistrationsZf'],'CopyPlanSha256':sha(OUT/'copy-plan.json')}))
