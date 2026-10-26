using System.Numerics;
using System.Runtime.InteropServices;

namespace Erlang;

// Genuine public driver term instruction stream, not NIF term handles or
// unvalidated native Eterm pointers. Own tokens resolve actual admitted terms.
internal sealed class ErlDriverTermProgram420
{
    private readonly ErlDriverPrimitiveAccess420 memory;
    private readonly Func<nuint, Term> resolve;
    private readonly Func<ReadOnlyMemory<byte>, Term> decode;

    internal ErlDriverTermProgram420(
        ErlDriverPrimitiveAccess420 memory,
        Func<nuint, Term> resolve,
        Func<ReadOnlyMemory<byte>, Term> decode
    )
    {
        this.memory = memory;
        this.resolve = resolve;
        this.decode = decode;
    }

    private static nuint Word(nint program, int length, ref int index)
    {
        if (index >= length) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        nuint value = unchecked((nuint)Marshal.ReadInt64(program, checked(index * IntPtr.Size)));
        index++;

        return value;
    }

    private static byte[] Bytes(nint source, nuint size, bool nullAllowed)
    {
        if (source == nint.Zero && (!nullAllowed || size != 0))
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        var bytes = new byte[checked((int)size)];
        if (bytes.Length != 0) Marshal.Copy(
            source,
            bytes,
            0,
            bytes.Length
        );

        return bytes;
    }

    private static Term Pop(List<Term> stack)
    {
        if (stack.Count == 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        Term value = stack[^1];
        stack.RemoveAt(stack.Count - 1);

        return value;
    }

    private static Term[] Take(List<Term> stack, nuint arity)
    {
        int count = checked((int)arity);
        if (count > stack.Count) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        int first = stack.Count - count;
        var arguments = new Term[count];
        stack.CopyTo(
            first,
            arguments,
            0,
            count
        );
        stack.RemoveRange(first, count);

        return arguments;
    }

    private Term Typed<T>(nuint token) where T : Term
    {
        Term term = resolve(token);
        if (term is not T) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

        return term;
    }

    internal Term Read(nint program, int length)
    {
        if (program == nint.Zero || length <= 0)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        var stack = new List<Term>();
        int index = 0;
        while (index < length)
        {
            var tag = (ErlDriverTermTag420)Word(program, length, ref index);
            switch (tag)
            {
                case ErlDriverTermTag420.Nil:
                    stack.Add(Nil.Value);
                    break;

                case ErlDriverTermTag420.Atom:
                    stack.Add(Typed<Atom>(Word(program, length, ref index)));
                    break;

                case ErlDriverTermTag420.Port:
                    stack.Add(Typed<PortTerm>(Word(program, length, ref index)));
                    break;

                case ErlDriverTermTag420.Pid:
                    stack.Add(Typed<Pid>(Word(program, length, ref index)));
                    break;

                case ErlDriverTermTag420.Integer:
                    stack.Add(new Integer(unchecked((long)Word(program, length, ref index))));
                    break;

                case ErlDriverTermTag420.Unsigned:
                    stack.Add(new Integer(new BigInteger((ulong)Word(program, length, ref index))));
                    break;

                case ErlDriverTermTag420.Integer64:
                case ErlDriverTermTag420.Unsigned64:
                    {
                        nint pointer = unchecked((nint)Word(program, length, ref index));
                        if (pointer == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                        long integer = Marshal.ReadInt64(pointer);
                        stack.Add(new Integer(tag == ErlDriverTermTag420.Integer64
                            ? new BigInteger(integer) : new BigInteger(unchecked((ulong)integer))));
                        break;
                    }

                case ErlDriverTermTag420.Float:
                    {
                        nint pointer = unchecked((nint)Word(program, length, ref index));
                        if (pointer == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                        double number = BitConverter.Int64BitsToDouble(Marshal.ReadInt64(pointer));
                        if (!double.IsFinite(number)) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                        stack.Add(new FloatTerm(number));
                        break;
                    }

                case ErlDriverTermTag420.Binary:
                    {
                        nint binary = unchecked((nint)Word(program, length, ref index));
                        nuint size = Word(program, length, ref index);
                        nuint offset = Word(program, length, ref index);
                        memory.ValidateSlice(binary, offset, size);
                        memory.RetainBinary(binary);
                        try
                        {
                            stack.Add(new BitString(Bytes(checked(memory.BinaryBytes(binary) + checked((nint)offset)), size, true)));
                        }
                        finally
                        {
                            memory.ReleaseBinary(binary);
                        }
                        break;
                    }

                case ErlDriverTermTag420.BufferBinary:
                    {
                        nint source = unchecked((nint)Word(program, length, ref index));
                        nuint size = Word(program, length, ref index);
                        stack.Add(new BitString(Bytes(source, size, true)));
                        break;
                    }

                case ErlDriverTermTag420.String:
                case ErlDriverTermTag420.StringCons:
                    {
                        nint source = unchecked((nint)Word(program, length, ref index));
                        nuint size = Word(program, length, ref index);
                        byte[] characters = Bytes(source, size, false);
                        Term tail = tag == ErlDriverTermTag420.StringCons ? Pop(stack) : Nil.Value;
                        for (int position = characters.Length - 1; position >= 0; position--)
                            tail = new Cons(Term.I(characters[position]), tail);
                        stack.Add(tail);
                        break;
                    }

                case ErlDriverTermTag420.Tuple:
                    stack.Add(new TupleTerm(Take(stack, Word(program, length, ref index))));
                    break;

                case ErlDriverTermTag420.List:
                    {
                        nuint arity = Word(program, length, ref index);
                        if (arity == 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                        Term[] elements = Take(stack, arity);
                        Term list = elements[^1];
                        for (int position = elements.Length - 2; position >= 0; position--)
                            list = new Cons(elements[position], list);
                        stack.Add(list);
                        break;
                    }

                case ErlDriverTermTag420.Map:
                    {
                        int count = checked((int)Word(program, length, ref index));
                        Term[] elements = Take(stack, checked((nuint)count * 2));
                        var entries = new Dictionary<Term, Term>();
                        for (int position = 0; position < elements.Length; position += 2)
                            if (!entries.TryAdd(elements[position], elements[position + 1]))
                                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                        stack.Add(new MapTerm(entries));
                        break;
                    }

                case ErlDriverTermTag420.ExternalTerm:
                    {
                        nint source = unchecked((nint)Word(program, length, ref index));
                        nuint size = Word(program, length, ref index);
                        if (size == 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                        stack.Add(decode(Bytes(source, size, false)));
                        break;
                    }

                default:
                    throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            }
        }
        if (stack.Count != 1) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

        return stack[0];
    }
}
