using System.Numerics;

namespace Erlang;

public static class CoreModules
{
    public static void Register(ModuleRegistry r)
    {
        void Add(
            string module,
            string name,
            int arity,
            Func<ProcessContext, IReadOnlyList<Term>, Term> f
        ) => r.Register(
            module,
            name,
            arity,
            (c, a) => ValueTask.FromResult(f(c, a))
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Self,
            0,
            (c, a) => c.Self
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Length,
            1,
            (c, a) => Term.I(Cons.Items(a[0]).LongCount())
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Head,
            1,
            (c, a) => a[0] is Cons l ? l.Head : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Tail,
            1,
            (c, a) => a[0] is Cons l ? l.Tail : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Element,
            2,
            (c, a) => a[0] is Integer i && a[1] is TupleTerm t && i.Value > 0 && i.Value <= t.Items.Count ? t.Items[(int)i.Value - 1] : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.TupleSize,
            1,
            (c, a) => a[0] is TupleTerm t ? Term.I(t.Items.Count) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsAtom,
            1,
            (c, a) => Boolean(a[0] is Atom)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsInteger,
            1,
            (c, a) => Boolean(a[0] is Integer)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsFloat,
            1,
            (c, a) => Boolean(a[0] is FloatTerm)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsNumber,
            1,
            (c, a) => Boolean(a[0] is Integer or FloatTerm)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsTuple,
            1,
            (c, a) => Boolean(a[0] is TupleTerm)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsBinary,
            1,
            (c, a) => Boolean(a[0] is BitString b && b.IsBinary)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsBitString,
            1,
            (c, a) => Boolean(a[0] is BitString)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.BitSize,
            1,
            (c, a) => a[0] is BitString b ? Term.I(b.BitLength) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.ByteSize,
            1,
            (c, a) => a[0] is BitString b ? Term.I((b.BitLength + 7L) / 8) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsList,
            1,
            (c, a) => Boolean(a[0] is Cons or Nil)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsPid,
            1,
            (c, a) => Boolean(a[0] is Pid)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsMap,
            1,
            (c, a) => Boolean(a[0] is MapTerm)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.MapSize,
            1,
            (c, a) => a[0] is MapTerm m ? Term.I(m.Entries.Count) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[0]))
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.MapGet,
            2,
            (c, a) => a[1] is MapTerm m ? m.Get(a[0]) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[1]))
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsMapKey,
            2,
            (c, a) => a[1] is MapTerm m ? Boolean(m.TryGet(a[0], out _)) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[1]))
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.MakeReference,
            0,
            (c, a) => new ReferenceTerm(c.Runtime.Node, (ulong)Interlocked.Increment(ref nextRef))
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Register,
            2,
            (c, a) =>
 {
     c.Runtime.Register(AtomName(a[0]), PidValue(a[1]));

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.WhereIs,
            1,
            (c, a) => c.Runtime.WhereIs(AtomName(a[0]))
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Unregister,
            1,
            (c, a) =>
 {
     c.Runtime.Unregister(AtomName(a[0]));

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Link,
            1,
            (c, a) =>
 {
     c.Runtime.Link(c, PidValue(a[0]));

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Unlink,
            1,
            (c, a) =>
 {
     c.Runtime.Unlink(c, PidValue(a[0]));

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Monitor,
            2,
            (c, a) => AtomName(a[0]) == ProcessMonitorKinds.Process ? c.Runtime.Monitor(c, PidValue(a[1])) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Demonitor,
            1,
            (c, a) =>
 {
     if (a[0] is not ReferenceTerm reference)
         throw new ErlangException(ErlangErrorReasons.BadArgument);

     return Boolean(c.Runtime.Demonitor(c, reference));
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.ProcessFlag,
            2,
            (c, a) =>
 {
     if (AtomName(a[0]) != ProcessFlagNames.TrapExit)
         throw new ErlangException(ErlangErrorReasons.BadArgument);
     bool previous = c.TrapExits;
     c.TrapExits = Bool(a[1]);

     return Boolean(previous);
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Exit,
            1,
            (c, a) => throw new ErlangException(a[0], ErlangExceptionClasses.Exit)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Exit,
            2,
            (c, a) =>
 {
     c.Runtime.Exit(c, PidValue(a[0]), a[1]);

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Error,
            1,
            (c, a) => throw new ErlangException(a[0])
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Throw,
            1,
            (c, a) => throw new ErlangException(a[0], ErlangExceptionClasses.Throw)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Put,
            2,
            (c, a) =>
 {
     var previous = c.Dictionary.GetValueOrDefault(a[0], Term.A(ProcessDictionaryAtoms.Undefined));
     c.Dictionary[a[0]] = a[1];

     return previous;
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Get,
            1,
            (c, a) => c.Dictionary.GetValueOrDefault(a[0], Term.A(ProcessDictionaryAtoms.Undefined))
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Erase,
            1,
            (c, a) =>
 {
     var previous = c.Dictionary.GetValueOrDefault(a[0], Term.A(ProcessDictionaryAtoms.Undefined));
     c.Dictionary.Remove(a[0]);

     return previous;
 }
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Reverse,
            1,
            (c, a) => ListOperations.Reverse(a[0])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Reverse,
            2,
            (c, a) => ListOperations.Reverse(a[0], a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Append,
            2,
            (c, a) => Cons.From(Cons.Items(a[0]), a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Append,
            1,
            (c, a) => ListOperations.Append(a[0])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Duplicate,
            2,
            (c, a) => ListOperations.Duplicate(a[0], a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Flatten,
            1,
            (c, a) => ListOperations.Flatten(a[0])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Flatten,
            2,
            (c, a) => ListOperations.Flatten(a[0], a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Member,
            2,
            (c, a) => Boolean(Cons.Items(a[1]).Any(x => x.Equals(a[0])))
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Sum,
            1,
            (c, a) => Cons.Items(a[0]).Aggregate((Term)Term.I(0), (x, y) => Arithmetic(ErlangOperators.Plus, x, y))
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.KeyFind,
            3,
            (c, a) => ListOperations.KeyFind(a[0], a[1], a[2])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.KeyMember,
            3,
            (c, a) => ListOperations.KeyMember(a[0], a[1], a[2])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.KeySearch,
            3,
            (c, a) => ListOperations.KeySearch(a[0], a[1], a[2])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Nth,
            2,
            (c, a) => ListOperations.Nth(a[0], a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.NthTail,
            2,
            (c, a) => ListOperations.NthTail(a[0], a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Last,
            1,
            (c, a) => ListOperations.Last(a[0])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Split,
            2,
            (c, a) => ListOperations.Split(a[0], a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Sequence,
            2,
            (c, a) => ListOperations.Sequence(a[0], a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Sequence,
            3,
            (c, a) => ListOperations.Sequence(a[0], a[1], a[2])
        );
        r.Register(
            ListModuleNames.Module,
            ListModuleNames.Map,
            2,
            async (c, a) =>
 {
     if (a[0] is not FunctionTerm f)
         throw new ErlangException(ErlangErrorReasons.BadArgument);
     var result = new List<Term>();
     foreach (var x in Cons.Items(a[1]))
         result.Add(await f.Invoke(c, [x]));

     return Cons.From(result);
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Spawn,
            1,
            (c, a) =>
 {
     if (a[0] is not FunctionTerm { Arity: 0 } f)
         throw new ErlangException(ErlangErrorReasons.BadArgument);

     return c.Runtime.Spawn(child => f.Invoke(child, [])).Pid;
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.SpawnLink,
            1,
            (c, a) =>
 {
     if (a[0] is not FunctionTerm { Arity: 0 } f)
         throw new ErlangException(ErlangErrorReasons.BadArgument);

     return c.Runtime.Spawn(child => f.Invoke(child, []), c, true).Pid;
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.SpawnMonitor,
            1,
            (c, a) =>
 {
     if (a[0] is not FunctionTerm { Arity: 0 } f)
         throw new ErlangException(ErlangErrorReasons.BadArgument);
     var spawned = c.Runtime.SpawnMonitor(c, child => f.Invoke(child, []));

     return Term.Tuple(spawned.Process.Pid, spawned.Reference);
 }
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Get,
            2,
            (c, a) => a[1] is MapTerm m ? m.Get(a[0]) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[1]))
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Size,
            1,
            (c, a) => a[0] is MapTerm m ? Term.I(m.Entries.Count) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[0]))
        );
        r.Register(
            ListModuleNames.Module,
            ListModuleNames.Sort,
            2,
            (c, a) => ComparatorSort.Sort(a[0], a[1], c)
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Iterator,
            1,
            (c, a) => MapIteration.Iterator(a[0])
        );
        r.Register(
            MapModuleNames.Module,
            MapModuleNames.Iterator,
            2,
            (c, a) => MapIteration.Iterator(a[0], a[1], c)
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Next,
            1,
            (c, a) => MapIteration.Next(a[0])
        );
        r.Register(
            IoModuleNames.Module,
            IoModuleNames.Format,
            2,
            async (c, a) =>
        {
            string format = Term.Text(a[0]);
            var args = Cons.Items(a[1]).ToArray();
            int n = 0;
            var output = new System.Text.StringBuilder();
            for (int i = 0; i < format.Length; i++)
            {
                if (format[i] != '~')
                {
                    output.Append(format[i]);
                    continue;
                }
                if (++i == format.Length)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                switch (format[i])
                {
                    case 'n':
                        output.Append('\n');
                        break;
                    case '~':
                        output.Append('~');
                        break;
                    case 's':
                        if (n >= args.Length)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        output.Append(args[n] is Atom atom ? atom.Name : Term.Text(args[n]));
                        n++;
                        break;
                    case 'p':
                    case 'w':
                        if (n >= args.Length)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        output.Append(args[n++]);
                        break;
                    default:
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                }
            }
            if (n != args.Length)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            await c.Runtime.Output.WriteAsync(output.ToString());

            return Term.A(IoResultAtoms.Ok);
        }
        );
    }

    private static long nextRef = 1L << 40;

    public static string AtomName(Term t) => t is Atom a ? a.Name : throw new ErlangException(ErlangErrorReasons.BadArgument);

    public static Pid PidValue(Term t) => t is Pid p ? p : throw new ErlangException(ErlangErrorReasons.BadArgument);

    public static Term Boolean(bool value) => Term.A(value ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);

    public static bool Bool(Term t) => t is Atom a && a.Name is ErlangBooleanAtoms.True or ErlangBooleanAtoms.False ? a.Name == ErlangBooleanAtoms.True : throw new ErlangException(ErlangErrorReasons.BadArgument);

    public static Term Arithmetic(string op, Term a, Term b)
    {
        if (a is Integer x && b is Integer y)
        {
            if (op == ErlangOperators.Divide && y.Value == 0 || op is ErlangOperators.IntegerDivide or ErlangOperators.Remainder && y.Value == 0)
                throw new ErlangException(ErlangErrorReasons.BadArithmetic);
            if (op == ErlangOperators.Divide)
            {
                if (!x.TryToDouble(out double numerator) || !y.TryToDouble(out double denominator))
                    throw new ErlangException(ErlangErrorReasons.BadArithmetic);

                return new FloatTerm(numerator / denominator);
            }

            return op switch
            {
                ErlangOperators.Plus => new Integer(x.Value + y.Value),
                ErlangOperators.Minus => new Integer(x.Value - y.Value),
                ErlangOperators.Multiply => new Integer(x.Value * y.Value),
                ErlangOperators.IntegerDivide => new Integer(x.Value / y.Value),
                ErlangOperators.Remainder => new Integer(x.Value % y.Value),
                ErlangOperators.BitwiseAnd => new Integer(x.Value & y.Value),
                ErlangOperators.BitwiseOr => new Integer(x.Value | y.Value),
                ErlangOperators.BitwiseXor => new Integer(x.Value ^ y.Value),
                ErlangOperators.ShiftLeft => Shift(x.Value, y.Value),
                ErlangOperators.ShiftRight => Shift(x.Value, -y.Value),
                _ => throw new ErlangException(ErlangErrorReasons.BadArithmetic)
            };
        }
        double left = a switch
        {
            Integer i when i.TryToDouble(out double converted) => converted,
            FloatTerm f => f.Value,
            _ => throw new ErlangException(ErlangErrorReasons.BadArithmetic)
        };
        double right = b switch
        {
            Integer i when i.TryToDouble(out double converted) => converted,
            FloatTerm f => f.Value,
            _ => throw new ErlangException(ErlangErrorReasons.BadArithmetic)
        };

        return new FloatTerm(op switch
        {
            ErlangOperators.Plus => left + right,
            ErlangOperators.Minus => left - right,
            ErlangOperators.Multiply => left * right,
            ErlangOperators.Divide => left / right,
            _ => throw new ErlangException(ErlangErrorReasons.BadArithmetic)
        });
    }

    private static Term Shift(BigInteger value, BigInteger count)
    {
        if (value.IsZero || count.IsZero)
            return new Integer(value);
        if (count.Sign < 0)
        {
            var magnitude = -count;
            if (magnitude >= value.GetBitLength())
                return Term.I(value.Sign < 0 ? -1 : 0);

            return new Integer(value >> (int)magnitude);
        }
        if (count > IntegerShiftLimits.MaximumResultBits || count + value.GetBitLength() > IntegerShiftLimits.MaximumResultBits)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);

        return new Integer(value << (int)count);
    }
}
