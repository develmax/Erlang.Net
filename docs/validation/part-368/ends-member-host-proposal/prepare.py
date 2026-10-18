import hashlib
import json
from pathlib import Path

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]
TARGET = OUT/'source/tests/Erlang.Tests'
TARGET.mkdir(parents=True,exist_ok=True)
checks = ROOT/'tests/Erlang.Tests/QueueCooperativeHostChecks.cs'
names = ROOT/'tests/Erlang.Tests/QueueCooperativeHostNames.cs'
source = checks.read_text()
old = '            QueueModuleNames.Out\n'
new = '''            QueueModuleNames.Out,
            QueueModuleNames.OutReverse,
            QueueModuleNames.Drop,
            QueueModuleNames.DropReverse,
            QueueModuleNames.Get,
            QueueModuleNames.GetReverse,
            QueueModuleNames.Peek,
            QueueModuleNames.Member
'''
assert source.count(old)==1
source=source.replace(old,new)
old='        Require(Cons.Items(input).Count() == QueueCooperativeHostNames.Cells);'
new='''        foreach (bool cancel in new[] { false, true })
        {
            try
            {
                await Boundary(QueueModuleNames.Member, input, cancel, frontMember: true);
            }
            catch (Exception failure)
            {
                failures.Add(new InvalidOperationException(QueueModuleNames.Member, failure));
            }
        }
'''+old
assert source.count(old)==1
source=source.replace(old,new)
source=source.replace('Boundary(string function, Term input, bool cancel)', 'Boundary(string function, Term input, bool cancel, bool frontMember = false)')
old='''        Term argument = function == QueueModuleNames.FromList ? input
            : Term.Tuple(input, function == QueueModuleNames.Out ? Term.List(Nil.Value) : Nil.Value);'''
new='''        Term[] arguments = function switch
        {
            QueueModuleNames.FromList => [input],
            QueueModuleNames.Out => [Term.Tuple(input, Term.List(Nil.Value))],
            QueueModuleNames.OutReverse => [Term.Tuple(Term.List(Nil.Value), input)],
            QueueModuleNames.DropReverse or QueueModuleNames.GetReverse => [Term.Tuple(Nil.Value, input)],
            QueueModuleNames.Member => [Term.I(QueueCooperativeHostNames.MissingMember),
                frontMember ? Term.Tuple(Nil.Value, input) : Term.Tuple(input, Nil.Value)],
            _ => [Term.Tuple(input, Nil.Value)]
        };'''
assert source.count(old)==1
source=source.replace(old,new)
assert source.count('                        argument\n')==1
source=source.replace('                        argument\n','                        arguments\n')
old='''        if (function == QueueModuleNames.Length)
'''
new='''        if (function is QueueModuleNames.Get or QueueModuleNames.GetReverse)
        {
            Require(value is Nil);
            return;
        }
        if (function == QueueModuleNames.Peek)
        {
            Require(value.Equals(Term.Tuple(Term.A(QueueCoreHostNames.Value), Nil.Value)));
            return;
        }
        if (function == QueueModuleNames.Member)
        {
            Require(value.Equals(Term.A(QueueCoreHostNames.False)));
            return;
        }
'''+old
assert source.count(old)==1
source=source.replace(old,new)
source=source.replace('if (function == QueueModuleNames.Out)\n', 'if (function is QueueModuleNames.Out or QueueModuleNames.OutReverse)\n')
old='''        Require(retained.Length == QueueCooperativeHostNames.Cells && retained.All(item => item is Nil));'''
new='''        int expectedCount = function is QueueModuleNames.Drop or QueueModuleNames.DropReverse
            ? QueueCooperativeHostNames.Cells - 1 : QueueCooperativeHostNames.Cells;
        Require(retained.Length == expectedCount && retained.All(item => item is Nil));'''
assert source.count(old)==1
source=source.replace(old,new)
(TARGET/checks.name).write_text(source,encoding='utf-8')
source=names.read_text()
old='    internal const int Cells = 256017;'
assert source.count(old)==1
source=source.replace(old,old+'\n    internal const int MissingMember = 1;')
(TARGET/names.name).write_text(source,encoding='utf-8')

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def write(name,value):
    (OUT/name).write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')

files=[]
for path in sorted(TARGET.iterdir()):
    destination='tests/Erlang.Tests/'+path.name
    files.append({'Source':path.relative_to(ROOT).as_posix(),'Destination':destination,'BaseSha256':sha(ROOT/destination),'Bytes':path.stat().st_size,'Sha256':sha(path)})
write('copy-plan.json',{'Status':'SOURCE_ONLY_TEST_PROPOSAL_NOT_BUILT','Files':files,'RuntimeChanges':0,'NewTestNames':0})
write('coverage.json',{
 'ExistingUntouchedCases':'Four core operations normal/cancel plus five own reverse-entry boundaries.',
 'AddedCases':[
  {'Function':'out_r/1','Path':'rear singleton, long front; f2r length/split/reverse'},
  {'Function':'drop/1','Path':'long rear-only; direct reverse'},
  {'Function':'drop_r/1','Path':'long front-only; direct reverse'},
  {'Function':'get/1','Path':'long rear-only; iterative lists:last'},
  {'Function':'get_r/1','Path':'long front-only; iterative lists:last'},
  {'Function':'peek/1','Path':'long rear-only; lists:last with pending value tuple'},
  {'Function':'member/2','Path':'missing integer probe; long rear then empty front'},
  {'Function':'member/2','Path':'empty rear then long front tail path'}],
 'NewBoundaries':16,'InputCells':256017,'EachBoundary':'Actual public registry call; observer progress and actual reduction count; normal result or kill/canceled result; physical finally unwind, process death and runtime still usable.',
 'NoRaceSleep':'Reuses deterministic captured real continuation helper and bounded waits; cancellation occurs at first actual yield.',
 'AliasApplicability':'cons/in_r/snoc constant-time need no traversal-yield assertion. head/get, tail/drop, init/liat/lait/drop_r, last/daeh/get_r share exact implementations; value/error/native profiles cover their distinct wrappers.',
 'ExplicitLimit':'Does not prove cancellation during later higher filter/delete reconstruction phases. Those require separate phase-aware witnesses, not a first-yield label.',
 'NoActualExecution':True})
(OUT/'README.md').write_text('''# Queue ends/member cooperative controls proposal\n\nTwo guarded test-only replacements extend the existing HOST name and bounded continuation helper. No runtime file changes or additional test names are needed. Existing four core normal/cancel paths and five reverse-entry controls remain. Eight new representative linear paths each run normal and cancel: out_r balancing, drop/drop_r reverse, get/get_r and peek last traversal, member rear/front search. All use public actor/registry APIs and actual observer reductions.\n\nInput256017 is enough for member16 and reverse40cells/reduction to hit a real own slice. Normal results are checked, not only completion; cancellation must unwind physically and leave the runtime usable. Every independent boundary is collected into the existing aggregate. Shared alias implementations avoid duplicate long allocations for identical loops.\n\nThis is source-only, unbuilt and unrun. Existing Main tests and prior seals remain unchanged. It does not claim cancellation during a later higher-operation reconstruction phase. Root may apply only when the two base hashes match, style/build/run, and preserve failures as actual evidence.\n''',encoding='utf-8')
write('seal.json',{'Files':[{'Path':p.relative_to(OUT).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(OUT.rglob('*')) if p.is_file() and p.name!='seal.json']})
print(json.dumps({'SealSha256':sha(OUT/'seal.json'),'CopyPlanSha256':sha(OUT/'copy-plan.json')}))
