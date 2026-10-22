from pathlib import Path
import json,hashlib,subprocess
root=Path(__file__).resolve().parents[2]
out=root/'artifacts/binary-backing369-followup1'
out.mkdir(exist_ok=True)
rows=[]
def emit(path,text):
    source=root/path
    target=out/'source'/path
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(text.replace('\r\n','\n'),encoding='utf-8',newline='\n')
    rows.append({'Path':path,'Source':target.relative_to(out).as_posix(),'BaseSha256':hashlib.sha256(source.read_bytes()).hexdigest().upper(),'Sha256':hashlib.sha256(target.read_bytes()).hexdigest().upper()})
path='src/Erlang.Terms/BitString.cs'
s=(root/path).read_text(encoding='utf-8-sig')
assert 'private readonly BinaryBacking backing;' in s
s=s.replace('private readonly int bitOffset;', 'private readonly long bitOffset;')
s=s.replace('bytes.AsSpan(bitOffset / BitStringLayout.BitsPerByte, ByteLength)', 'bytes.AsSpan((int)(bitOffset / BitStringLayout.BitsPerByte), ByteLength)')
s=s.replace('private BitString(BinaryBacking storage, int offset, int count, object? token = null)', 'private BitString(BinaryBacking storage, long offset, int count, object? token = null)')
s=s.replace('bytes[(bitOffset + i) / BitStringLayout.BitsPerByte]', 'bytes[(int)((bitOffset + i) / BitStringLayout.BitsPerByte)]')
s=s.replace('(BitStringStorage.HighBit - (bitOffset + i) % BitStringLayout.BitsPerByte)', '(BitStringStorage.HighBit - (int)((bitOffset + i) % BitStringLayout.BitsPerByte))')
needle='    internal static BitString FromOwnedBytes(byte[] bytes) => new(bytes, true);'
assert needle in s
s=s.replace(needle,needle+'''

    internal static BitString FromOwnedBacking(byte[] parent, int byteOffset, int bitLength)
    {
        if (byteOffset < 0 || bitLength < 0 || (long)byteOffset * BitStringLayout.BitsPerByte + bitLength > (long)parent.Length * BitStringLayout.BitsPerByte)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var storage = new BinaryBacking(parent, true);

        return new BitString(storage, (long)byteOffset * BitStringLayout.BitsPerByte, bitLength);
    }
''')
emit(path,s)
path='src/Erlang.Terms/BinaryBacking.cs'
s=(root/path).read_text(encoding='utf-8-sig')
needle='    internal static (BinaryBacking Backing, object Writer) FreshAppend(byte[] complete)'
assert needle in s
s=s.replace(needle,'''    internal static (BinaryBacking Backing, object Writer) InitializeWritable(int initialBytes)
    {
        if (initialBytes < 0 || initialBytes > Array.MaxLength)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        var result = new BinaryBacking(new byte[initialBytes], true);
        var token = new object();
        result.writer = token;
        result.apparentBytes = 0;

        return (result, token);
    }

'''+needle)
emit(path,s)
# Add the compiler-facing initial writable value without choosing an optimizer plan.
bitrow=next(r for r in rows if r['Path']=='src/Erlang.Terms/BitString.cs')
target=out/bitrow['Source']
s=target.read_text(encoding='utf-8')
needle='    internal static BitString FromOwnedBytes(byte[] bytes) => new(bytes, true);'
s=s.replace(needle,needle+'''

    internal static BitString InitializeWritable(int initialBytes)
    {
        var initialized = BinaryBacking.InitializeWritable(initialBytes);

        return new BitString(initialized.Backing, 0, 0, initialized.Writer);
    }
''')
target.write_text(s,encoding='utf-8',newline='\n')
bitrow['Sha256']=hashlib.sha256(target.read_bytes()).hexdigest().upper()
path='src/Erlang.Runtime/NifNativeBinaryStorage.cs'
s=(root/path).read_text(encoding='utf-8-sig')
needle='    internal void RetainEnvironment()'
assert needle in s
s=s.replace(needle,'''
    internal BitString FreezeView(nuint offset, nuint length)
    {
        lock (gate)
        {
            if (pointer == 0)
                throw new InvalidOperationException(NifNativeBinaryNames.ExpiredStorage);
            if (offset > size || length > size - offset)
                throw new InvalidOperationException(NifNativeBinaryNames.SubBinaryRange);
            if (length > NifNativeBinaryNames.MaximumTermBytes)
                throw new NotSupportedException(NifNativeBinaryNames.TermSizeBoundary);
            if (length <= NifNativeBinaryNames.SmallSubBinaryBytes)
                return new BitString(new ReadOnlySpan<byte>((byte*)pointer + offset, (int)length));
            if (size > (nuint)Array.MaxLength)
                throw new NotSupportedException(NifNativeBinaryNames.TermSizeBoundary);
            var parent = new ReadOnlySpan<byte>((byte*)pointer, (int)size).ToArray();

            return BitString.FromOwnedBacking(parent, (int)offset, checked((int)length * BitStringLayout.BitsPerByte));
        }
    }

'''+needle)
emit(path,s)
path='src/Erlang.Runtime/NifNativeBinaryNode.cs'
s=(root/path).read_text(encoding='utf-8-sig')
assert 'return new BitString(new ReadOnlySpan<byte>(Data, (int)length));' in s
s=s.replace('return new BitString(new ReadOnlySpan<byte>(Data, (int)length));','return storage.FreezeView(offset, length);')
emit(path,s)
path='src/Erlang.Compiler/ComprehensionExecution.cs'
s=(root/path).read_text(encoding='utf-8-sig')
assert '                byte[] bytes = input.ToArray();' in s
s=s.replace('                byte[] bytes = input.ToArray();','                input.PinForMatch();\n                byte[] bytes = input.ToArray();')
emit(path,s)
path='src/Erlang.Compiler/ZipGeneratorCursor.cs'
s=(root/path).read_text(encoding='utf-8-sig')
assert '            bits = input;\n            bytes = input.ToArray();' in s
s=s.replace('            bits = input;\n            bytes = input.ToArray();','            input.PinForMatch();\n            bits = input;\n            bytes = input.ToArray();')
emit(path,s)
(out/'copy-plan.json').write_text(json.dumps({'Scope':'NIF retained snapshot and pre-snapshot generator pin; source only','Files':rows},indent=2)+'\n',encoding='utf-8')
auth=out/'authority'
auth.mkdir(exist_ok=True)
p=subprocess.run(['git','-C',str(root.parent/'otp'),'show','ad05823719d77c8faee87348ea39513d4e2f99c5:lib/compiler/src/beam_ssa_bc_size.erl'],capture_output=True,check=True)
(auth/'beam_ssa_bc_size.erl').write_bytes(p.stdout)
print(out)
