from pathlib import Path
import json,hashlib
root=Path(__file__).resolve().parents[2]
base=Path(__file__).resolve().parent
rows=[]
def emit(path,text):
    original=root/path
    p=base/'source'/path;p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(text.replace('\r\n','\n'),encoding='utf-8',newline='\n')
    rows.append({'Path':path,'Source':p.relative_to(base).as_posix(),'BaseSha256':hashlib.sha256(original.read_bytes()).hexdigest().upper() if original.exists() else None,'Sha256':hashlib.sha256(p.read_bytes()).hexdigest().upper()})
emit('src/Erlang.Terms/BinarySharingParameters.cs','''namespace Erlang;

internal static class BinarySharingParameters
{
    internal const int WorkChunk = 256;
}
''')
emit('src/Erlang.Terms/BinarySharingCursor.cs','''namespace Erlang;

internal sealed class BinarySharingCursor
{
    private readonly Stack<(Term Value, long Child)> pending = new();
    private readonly HashSet<Term> seen = new(ReferenceEqualityComparer.Instance);

    internal BinarySharingCursor(Term value) => pending.Push((value, -1));

    internal bool Step(int work, CancellationToken cancellation = default)
    {
        if (work <= 0)
            throw new ArgumentOutOfRangeException(nameof(work));
        while (work-- > 0 && pending.TryPop(out var frame))
        {
            cancellation.ThrowIfCancellationRequested();
            var value = frame.Value;
            long child = frame.Child;
            if (child < 0)
            {
                if (!seen.Add(value))
                    continue;
                if (value is BitString binary)
                {
                    binary.PinForSharing();
                    continue;
                }
                child = 0;
            }
            Term? next = value switch
            {
                Cons cell => child switch { 0 => cell.Head, 1 => cell.Tail, _ => null },
                TupleTerm tuple when child < tuple.Items.Count => tuple.Items[(int)child],
                NativeRecordTerm record when child < record.Values.Count => record.Values[(int)child],
                FunctionTerm function when child < function.Environment.Count => function.Environment[(int)child],
                MapTerm map when child / 2 < map.Entries.Count => child % 2 == 0 ? map.Entries[(int)(child / 2)].Key : map.Entries[(int)(child / 2)].Value,
                _ => null
            };
            if (next is not null)
            {
                pending.Push((value, child + 1));
                pending.Push((next, -1));
            }
        }

        return pending.Count == 0;
    }

    internal static void Pin(Term value, CancellationToken cancellation = default)
    {
        var cursor = new BinarySharingCursor(value);
        while (!cursor.Step(BinarySharingParameters.WorkChunk, cancellation)) { }
    }
}
''')
path='src/Erlang.Terms/BitString.cs'
s=(root/path).read_text(encoding='utf-8-sig')
needle='    internal void PinForMatch() => backing.Pin();'
assert needle in s
s=s.replace(needle,needle+'\n\n    internal void PinForSharing() => backing.Pin();')
emit(path,s)
path='src/Erlang.Serialization/ExternalTermFormat.cs'
s=(root/path).read_text(encoding='utf-8-sig')
needle='            case BitString b:\n                s.WriteByte'
assert needle in s
s=s.replace(needle,'            case BitString b:\n                b.PinForSharing();\n                s.WriteByte')
emit(path,s)
path='src/Erlang.Runtime/NifNativeBinaryCallbacks.cs'
s=(root/path).read_text(encoding='utf-8-sig')
needle='        if (value.StaticTerm is not BitString { IsBinary: true } binary)\n            return 0;\n        RequireDescriptor(output);'
assert needle in s
s=s.replace(needle,'        if (value.StaticTerm is not BitString binary)\n            return 0;\n        binary.PinForSharing();\n        if (!binary.IsBinary)\n            return 0;\n        RequireDescriptor(output);')
emit(path,s)
path='src/Erlang.Runtime/Mailbox.cs'
s=(root/path).read_text(encoding='utf-8-sig')
s=s.replace('    private readonly object gate = new();','    private readonly object gate = new();\n    internal Pid? Owner { get; init; }\n    internal bool CopySelfMessages { get; init; }')
needle='''            if (HaltRequested?.Invoke() == true)
                return;
            if (priority)'''
assert needle in s
s=s.replace(needle,'''            if (HaltRequested?.Invoke() == true)
                return;
            if (CopySelfMessages || priority || from is null || !from.Equals(Owner) || token is not null)
                BinarySharingCursor.Pin(message);
            if (priority)''',1)
needle='''            if (HaltRequested?.Invoke() == true)
                return;
            messages.AddLast(new Entry('''
assert needle in s
s=s.replace(needle,'''            if (HaltRequested?.Invoke() == true)
                return;
            BinarySharingCursor.Pin(message);
            messages.AddLast(new Entry(''',1)
emit(path,s)
path='src/Erlang.Runtime/ProcessContext.cs'
s=(root/path).read_text(encoding='utf-8-sig')
needle='''        Mailbox = new()
        {
            IncomingTerm'''
assert needle in s
s=s.replace(needle,'''        Mailbox = new()
        {
            Owner = pid,
            IncomingTerm''')
emit(path,s)
(base/'copy-plan.json').write_text(json.dumps({'Scope':'precise ETF/NIF and mailbox sharing events; port provider-specific applicability remains pending','Files':rows},indent=2)+'\n',encoding='utf-8')
print(len(rows))
