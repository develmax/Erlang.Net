from pathlib import Path
import hashlib,json,re
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
DEST=OUT/'source/src/Erlang.Runtime'
DEST.mkdir(parents=True,exist_ok=True)
PIN=ROOT/'artifacts/erlang371-root/lists.erl.pinned.txt'
text=PIN.read_text()
ranges=[]
begin=text.index('usort_1(X, [Y | L])')
ranges.append((begin,text.index('-doc',begin)))
begin=text.index('usplit_1(X, Y, [Z | L]')
ranges.append((begin,text.index('umergel(L) ->',begin)))
begin=text.index('umergel(L) ->')
ranges.append((begin,text.index('%% keysort/2',begin)))
tokens=[]
pattern=re.compile(r'\s+|->|=<|==|[<>]|[A-Za-z_][A-Za-z_0-9]*|[\[\](),;.|=:]')
for start,end in ranges:
 fragment=text[start:end]
 clean=re.sub(r'%[^\n]*',lambda m:' '*len(m.group()),fragment)
 pos=0
 for m in pattern.finditer(clean):
  assert clean[pos:m.start()].strip()=='',clean[pos:m.start()]
  if not m.group().isspace():tokens.append((m.group(),text.count('\n',0,start+m.start())+1))
  pos=m.end()
 assert not clean[pos:].strip()
at=0
def peek(): return tokens[at][0] if at<len(tokens) else None
def eat(s=None):
    global at
    t=tokens[at];at+=1
    assert s is None or t[0]==s,(s,t)
    return t
def expression():
    tok,line=eat()
    if tok=='[':
        items=[]
        tail=('nil',line)
        if peek()!=']':
            while True:
                items.append(expression())
                if peek()==',': eat(',');continue
                if peek()=='|':eat('|');tail=expression()
                break
        eat(']')
        term=('list',items,tail,line)
    else:
        if peek()==':':
            eat(':');remote=tok;tok,_=eat()
        else:remote=None
        if peek()=='(':
            eat('(');args=[]
            if peek()!=')':
                while True:
                    args.append(expression())
                    if peek()!=',':break
                    eat(',')
            eat(')');term=('call',remote,tok,args,line)
        else:
            term=('var' if tok[0].isupper() or tok.startswith('_') else 'atom',tok,line)
    if peek()=='=':
        eat('=');alias,_=eat();term=('alias',term,alias,line)
    return term
clauses=[]
while at<len(tokens):
    name,line=eat();eat('(');args=[]
    if peek()!=')':
        while True:
            args.append(expression())
            if peek()!=',':break
            eat(',')
    eat(')');guard=None
    if peek()=='when':
        eat('when');left=expression();op,_=eat();right=expression();guard=(left,op,right)
    eat('->');body=expression();separator,_=eat();assert separator in ('.',';')
    clauses.append({'Name':name,'Arity':len(args),'Line':line,'Args':args,'Guard':guard,'Body':body})
groups={('usort',1):[{'Name':'usort','Arity':1,'Line':2075,'ManualPinnedSourceCaseTree':True}]}
for clause in clauses:groups.setdefault((clause['Name'],clause['Arity']),[]).append(clause)
def enum(key):return key[0][0].upper()+key[0][1:]+str(key[1])
def nameconst(name):return name[0].upper()+name[1:]
def emit(expr):
    tag=expr[0]
    if tag=='var':return 'v'+expr[1]
    if tag=='atom':return 'ListUniqueMergeNames.'+nameconst(expr[1])+'Term'
    if tag=='nil':return 'Nil.Value'
    if tag=='list':
        tail=emit(expr[2])
        for item in reversed(expr[1]):tail='new Cons('+emit(item)+', '+tail+')'
        return tail
    raise AssertionError(expr)
def match(expr,subject,number):
    tag=expr[0]
    if tag=='var':return [] if expr[1]=='_' else [subject+' is var v'+expr[1]]
    if tag in ('nil','list'):
        if tag=='nil' or not expr[1]:return [subject+' is Nil']
        cell='cell'+str(number[0]);number[0]+=1
        rest=('list',expr[1][1:],expr[2],expr[3]) if len(expr[1])>1 else expr[2]
        return [subject+' is Cons '+cell]+match(expr[1][0],cell+'.Head',number)+match(rest,cell+'.Tail',number)
    if tag=='alias':return match(expr[1],subject,number)+[subject+' is var v'+expr[2]]
    if tag=='atom':return [subject+' is Atom { Name: ListUniqueMergeNames.'+nameconst(expr[1])+' }']
    raise AssertionError(expr)
def guardcode(guard):
    a,op,b=guard
    return emit(a)+'.NumericEquals('+emit(b)+')' if op=='==' else 'TermOrder.Compare('+emit(a)+', '+emit(b)+') '+{'=<':'<=','<':'<','>':'>'}[op]+' 0'
def findnested(call):
    for i,arg in enumerate(call[3]):
        if arg[0]=='list' and len(arg[1])==1 and arg[1][0][0]=='call':return i,arg[1][0],arg[2]
    return None
def dispatch(call,level=0):
    remote,name,args,line=call[1:]
    if remote is not None:
        assert remote=='lists' and name=='reverse' and len(args)==2
        return ['result = await ListUniqueMergeTrace.Reverse(context, '+emit(args[0])+', '+emit(args[1])+', continuations);','complete = true;','break;']
    assert (name,len(args)) in groups,(name,len(args))
    return ['state = ListUniqueMergeState.'+enum((name,len(args)))+';','arguments = ['+', '.join(emit(a) for a in args)+'];','continue;']
body=[]
for key,group in groups.items():
    if key==('usort',1):
        body.extend((OUT/'usort-state.fragment.cs').read_text().splitlines());continue
    body+=['                case ListUniqueMergeState.'+enum(key)+':','                {']
    for c in group:
        conditions=[];n=[0]
        for i,arg in enumerate(c['Args']):conditions+=match(arg,f'arguments[{i}]',n)
        if c['Guard']:conditions.append(guardcode(c['Guard']))
        assert all(x is not None for x in conditions)
        body+=['                    {','                        if ('+' && '.join(conditions or ['true'])+')','                        {']
        value=c['Body']
        lines=[]
        if value[0]=='call':
            nested=findnested(value)
            if nested:
                index,child,tail=nested
                outer_args=['Nil.Value' if i==index else emit(a) for i,a in enumerate(value[3])]
                lines+=['continuations.Push(new ListUniqueMergeContinuation(',
                        '    ListUniqueMergeState.'+enum((value[2],len(value[3])))+',',
                        '    ['+', '.join(outer_args)+'],',
                        '    '+str(index)+',',
                        '    '+emit(tail)+',',
                        '    state,',
                        '    ListUniqueMergeNames.CallLine'+str(child[4]),
                        '));']
                lines+=dispatch(child)
            else:lines+=dispatch(value)
        else:lines+=['result = '+emit(value)+';','complete = true;','break;']
        body+=['                            '+s for s in lines]
        body+=['                        }','                    }']
    body+=['                    throw ListUniqueMergeTrace.Clause(context, state, arguments, continuations);','                }']
machine='''namespace Erlang;

internal static class ListUniqueMergeMachine
{
    internal static ValueTask<Term> Run(ProcessContext context, Term lists)
        => Execute(context, lists, ListUniqueMergeState.Umergel1, ListModuleNames.UniqueMerge);

    internal static ValueTask<Term> Sort(ProcessContext context, Term list)
        => Execute(context, list, ListUniqueMergeState.Usort1, ListModuleNames.UniqueSort);

    private static async ValueTask<Term> Execute(ProcessContext context, Term input, ListUniqueMergeState initial, string function)
    {
        using var tail = context.EnterSourceTailCall(ListModuleNames.Module, function);
        var continuations = new Stack<ListUniqueMergeContinuation>();
        var state = initial;
        Term[] arguments = [input];
        while (true)
        {
            await context.ReduceAsync();
            Term? result = null;
            bool complete = false;
            switch (state)
            {
'''+ '\n'.join(body)+'''
                default:
                    throw new InvalidOperationException(ListUniqueMergeNames.InvalidState);
            }
            if (complete)
            {
                if (!continuations.TryPop(out var continuation))
                    return result!;
                state = continuation.State;
                arguments = continuation.Arguments;
                arguments[continuation.ResultArgument] = new Cons(result!, continuation.ResultTail);
            }
        }
    }
}
'''
(DEST/'ListUniqueMergeMachine.cs').write_text(machine,encoding='utf-8')
(DEST/'ListUniqueMergeState.cs').write_text('namespace Erlang;\n\ninternal enum ListUniqueMergeState\n{\n'+',\n'.join('    '+enum(k) for k in groups)+'\n}\n',encoding='utf-8')
calllines=sorted({c['Body'][4] for c in clauses if c['Body'][0]=='call' and findnested(c['Body'])})
# A nested call's own token owns the continuation line.
calllines=sorted({findnested(c['Body'])[1][4] for c in clauses if c['Body'][0]=='call' and findnested(c['Body'])})
names='namespace Erlang;\n\ninternal static class ListUniqueMergeNames\n{\n'
names+='    internal const string Asc = "asc";\n    internal const string Desc = "desc";\n    internal const string File = "lists.erl";\n    internal const string InvalidState = "Unique merge entered an invalid source state.";\n    internal const int UniqueSortLine = 2075;\n'
names+='    internal const string SourceFileKey = "file";\n    internal const string SourceLineKey = "line";\n'
names+='    internal static readonly Atom AscTerm = Term.A(Asc);\n    internal static readonly Atom DescTerm = Term.A(Desc);\n'
for name in dict.fromkeys(k[0] for k in groups):names+='    internal const string '+nameconst(name)+' = "'+name+'";\n'
for ln in calllines:names+='    internal const int CallLine'+str(ln)+' = '+str(ln)+';\n'
names+='\n    internal static string Function(ListUniqueMergeState state) => state switch\n    {\n'
names+=',\n'.join('        ListUniqueMergeState.'+enum(k)+' => '+nameconst(k[0]) for k in groups)+',\n        _ => throw new InvalidOperationException(InvalidState)\n    };\n'
names+='\n    internal static int Line(ListUniqueMergeState state) => state switch\n    {\n'
names+=',\n'.join('        ListUniqueMergeState.'+enum(k)+' => '+str(group[0]['Line']) for k,group in groups.items())+',\n        _ => throw new InvalidOperationException(InvalidState)\n    };\n'
names+='\n    internal static int Arity(ListUniqueMergeState state) => state switch\n    {\n'
names+=',\n'.join('        ListUniqueMergeState.'+enum(k)+' => '+str(k[1]) for k in groups)+',\n        _ => throw new InvalidOperationException(InvalidState)\n    };\n}\n'
(DEST/'ListUniqueMergeNames.cs').write_text(names,encoding='utf-8')
(OUT/'parsed-source-clauses.json').write_text(json.dumps({'PinnedSha256':hashlib.sha256(PIN.read_bytes()).hexdigest().upper(),'StartLine':text.count('\n',0,start)+1,'EndLine':text.count('\n',0,end)+1,'Functions':len(groups),'Clauses':clauses},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Functions':len(groups),'Clauses':len(clauses),'PendingCallLines':calllines,'MachineLines':len(machine.splitlines())}))
