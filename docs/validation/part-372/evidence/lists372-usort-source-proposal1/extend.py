from pathlib import Path
O=Path(__file__).resolve().parent;R=O.parents[1]
s=(R/'artifacts/erlang371-lists-unique-source-proposal1/generate.py').read_text()
start=s.index("start=text.index('umergel(L) ->')")
end=s.index('at=0',start)
s=s[:start]+'''ranges=[]
begin=text.index('usort_1(X, [Y | L])')
ranges.append((begin,text.index('-doc',begin)))
begin=text.index('usplit_1(X, Y, [Z | L]')
ranges.append((begin,text.index('umergel(L) ->',begin)))
begin=text.index('umergel(L) ->')
ranges.append((begin,text.index('%% keysort/2',begin)))
tokens=[]
pattern=re.compile(r'\\s+|->|=<|==|[<>]|[A-Za-z_][A-Za-z_0-9]*|[\\[\\](),;.|=:]')
for start,end in ranges:
 fragment=text[start:end]
 clean=re.sub(r'%[^\\n]*',lambda m:' '*len(m.group()),fragment)
 pos=0
 for m in pattern.finditer(clean):
  assert clean[pos:m.start()].strip()=='',clean[pos:m.start()]
  if not m.group().isspace():tokens.append((m.group(),text.count('\\n',0,start+m.start())+1))
  pos=m.end()
 assert not clean[pos:].strip()
''' + s[end:]
s=s.replace('groups={}\n',"groups={('usort',1):[{'Name':'usort','Arity':1,'Line':2075,'ManualPinnedSourceCaseTree':True}]}\n")
old="return emit(a)+'.NumericEquals('+emit(b)+')' if op=='==' else 'TermOrder.Compare('+emit(a)+', '+emit(b)+') <= 0' if op=='=<' else None"
new="return emit(a)+'.NumericEquals('+emit(b)+')' if op=='==' else 'TermOrder.Compare('+emit(a)+', '+emit(b)+') '+{'=<':'<=','<':'<','>':'>'}[op]+' 0'"
assert old in s;s=s.replace(old,new)
anchor='for key,group in groups.items():\n    body+='
replacement="for key,group in groups.items():\n    if key==('usort',1):\n        body.extend((OUT/'usort-state.fragment.cs').read_text().splitlines());continue\n    body+="
assert anchor in s;s=s.replace(anchor,replacement)
old='''    internal static async ValueTask<Term> Run(ProcessContext context, Term lists)
    {
        using var tail = context.EnterSourceTailCall(ListModuleNames.Module, ListModuleNames.UniqueMerge);
        var continuations = new Stack<ListUniqueMergeContinuation>();
        var state = ListUniqueMergeState.Umergel1;
        Term[] arguments = [lists];'''
new='''    internal static ValueTask<Term> Run(ProcessContext context, Term lists)
        => Execute(context, lists, ListUniqueMergeState.Umergel1, ListModuleNames.UniqueMerge);

    internal static ValueTask<Term> Sort(ProcessContext context, Term list)
        => Execute(context, list, ListUniqueMergeState.Usort1, ListModuleNames.UniqueSort);

    private static async ValueTask<Term> Execute(ProcessContext context, Term input, ListUniqueMergeState initial, string function)
    {
        using var tail = context.EnterSourceTailCall(ListModuleNames.Module, function);
        var continuations = new Stack<ListUniqueMergeContinuation>();
        var state = initial;
        Term[] arguments = [input];'''
assert old in s;s=s.replace(old,new)
(O/'generate.py').write_text(s,encoding='utf-8')
