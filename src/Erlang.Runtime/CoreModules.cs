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
            "erlang",
            "self",
            0,
            (c, a) => c.Self
        );
        Add(
            "erlang",
            "length",
            1,
            (c, a) => Term.I(Cons.Items(a[0]).LongCount())
        );
        Add(
            "erlang",
            "hd",
            1,
            (c, a) => a[0] is Cons l ? l.Head : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            "erlang",
            "tl",
            1,
            (c, a) => a[0] is Cons l ? l.Tail : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            "erlang",
            "element",
            2,
            (c, a) => a[0] is Integer i && a[1] is TupleTerm t && i.Value > 0 && i.Value <= t.Items.Count ? t.Items[(int)i.Value - 1] : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            "erlang",
            "tuple_size",
            1,
            (c, a) => a[0] is TupleTerm t ? Term.I(t.Items.Count) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            "erlang",
            "is_atom",
            1,
            (c, a) => Boolean(a[0] is Atom)
        );
        Add(
            "erlang",
            "is_integer",
            1,
            (c, a) => Boolean(a[0] is Integer)
        );
        Add(
            "erlang",
            "is_float",
            1,
            (c, a) => Boolean(a[0] is FloatTerm)
        );
        Add(
            "erlang",
            "is_number",
            1,
            (c, a) => Boolean(a[0] is Integer or FloatTerm)
        );
        Add(
            "erlang",
            "is_tuple",
            1,
            (c, a) => Boolean(a[0] is TupleTerm)
        );
        Add(
            "erlang",
            "is_binary",
            1,
            (c, a) => Boolean(a[0] is BitString b && b.IsBinary)
        );
        Add(
            "erlang",
            "is_bitstring",
            1,
            (c, a) => Boolean(a[0] is BitString)
        );
        Add(
            "erlang",
            "bit_size",
            1,
            (c, a) => a[0] is BitString b ? Term.I(b.BitLength) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            "erlang",
            "byte_size",
            1,
            (c, a) => a[0] is BitString b ? Term.I((b.BitLength + 7L) / 8) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            "erlang",
            "is_list",
            1,
            (c, a) => Boolean(a[0] is Cons or Nil)
        );
        Add(
            "erlang",
            "is_pid",
            1,
            (c, a) => Boolean(a[0] is Pid)
        );
        Add(
            "erlang",
            "is_map",
            1,
            (c, a) => Boolean(a[0] is MapTerm)
        );
        Add(
            "erlang",
            "map_size",
            1,
            (c, a) => a[0] is MapTerm m ? Term.I(m.Entries.Count) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[0]))
        );
        Add(
            "erlang",
            "map_get",
            2,
            (c, a) => a[1] is MapTerm m ? m.Get(a[0]) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[1]))
        );
        Add(
            "erlang",
            "is_map_key",
            2,
            (c, a) => a[1] is MapTerm m ? Boolean(m.TryGet(a[0], out _)) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[1]))
        );
        Add(
            "erlang",
            "make_ref",
            0,
            (c, a) => new ReferenceTerm(c.Runtime.Node, (ulong)Interlocked.Increment(ref nextRef))
        );
        Add(
            "erlang",
            "register",
            2,
            (c, a) =>
 {
     c.Runtime.Register(AtomName(a[0]), PidValue(a[1]));

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            "erlang",
            "whereis",
            1,
            (c, a) => c.Runtime.WhereIs(AtomName(a[0]))
        );
        Add(
            "erlang",
            "unregister",
            1,
            (c, a) =>
 {
     c.Runtime.Unregister(AtomName(a[0]));

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            "erlang",
            "link",
            1,
            (c, a) =>
 {
     c.Runtime.Link(c, PidValue(a[0]));

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            "erlang",
            "unlink",
            1,
            (c, a) =>
 {
     c.Runtime.Unlink(c, PidValue(a[0]));

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            "erlang",
            "monitor",
            2,
            (c, a) => AtomName(a[0]) == "process" ? c.Runtime.Monitor(c, PidValue(a[1])) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            "erlang",
            "demonitor",
            1,
            (c, a) =>
 {
     if (a[0] is not ReferenceTerm reference)
         throw new ErlangException(ErlangErrorReasons.BadArgument);

     return Boolean(c.Runtime.Demonitor(c, reference));
 }
        );
        Add(
            "erlang",
            "process_flag",
            2,
            (c, a) =>
 {
     if (AtomName(a[0]) != "trap_exit")
         throw new ErlangException(ErlangErrorReasons.BadArgument);
     bool previous = c.TrapExits;
     c.TrapExits = Bool(a[1]);

     return Boolean(previous);
 }
        );
        Add(
            "erlang",
            "exit",
            1,
            (c, a) => throw new ErlangException(a[0], ErlangExceptionClasses.Exit)
        );
        Add(
            "erlang",
            "exit",
            2,
            (c, a) =>
 {
     c.Runtime.Exit(c, PidValue(a[0]), a[1]);

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            "erlang",
            "error",
            1,
            (c, a) => throw new ErlangException(a[0])
        );
        Add(
            "erlang",
            "throw",
            1,
            (c, a) => throw new ErlangException(a[0], ErlangExceptionClasses.Throw)
        );
        Add(
            "erlang",
            "put",
            2,
            (c, a) =>
 {
     var previous = c.Dictionary.GetValueOrDefault(a[0], Term.A(ProcessDictionaryAtoms.Undefined));
     c.Dictionary[a[0]] = a[1];

     return previous;
 }
        );
        Add(
            "erlang",
            "get",
            1,
            (c, a) => c.Dictionary.GetValueOrDefault(a[0], Term.A(ProcessDictionaryAtoms.Undefined))
        );
        Add(
            "erlang",
            "erase",
            1,
            (c, a) =>
 {
     var previous = c.Dictionary.GetValueOrDefault(a[0], Term.A(ProcessDictionaryAtoms.Undefined));
     c.Dictionary.Remove(a[0]);

     return previous;
 }
        );
        Add(
            "lists",
            "reverse",
            1,
            (c, a) => Cons.From(Cons.Items(a[0]).Reverse())
        );
        Add(
            "lists",
            "reverse",
            2,
            (c, a) => Cons.From(Cons.Items(a[0]).Reverse(), a[1])
        );
        Add(
            "lists",
            "append",
            2,
            (c, a) => Cons.From(Cons.Items(a[0]), a[1])
        );
        Add(
            "lists",
            "member",
            2,
            (c, a) => Boolean(Cons.Items(a[1]).Any(x => x.Equals(a[0])))
        );
        Add(
            "lists",
            "sum",
            1,
            (c, a) => Cons.Items(a[0]).Aggregate((Term)Term.I(0), (x, y) => Arithmetic("+", x, y))
        );
        r.Register(
            "lists",
            "map",
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
            "erlang",
            "spawn",
            1,
            (c, a) =>
 {
     if (a[0] is not FunctionTerm { Arity: 0 } f)
         throw new ErlangException(ErlangErrorReasons.BadArgument);

     return c.Runtime.Spawn(child => f.Invoke(child, [])).Pid;
 }
        );
        Add(
            "erlang",
            "spawn_link",
            1,
            (c, a) =>
 {
     if (a[0] is not FunctionTerm { Arity: 0 } f)
         throw new ErlangException(ErlangErrorReasons.BadArgument);

     return c.Runtime.Spawn(child => f.Invoke(child, []), c, true).Pid;
 }
        );
        Add(
            "erlang",
            "spawn_monitor",
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
            "maps",
            "get",
            2,
            (c, a) => a[1] is MapTerm m ? m.Get(a[0]) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[1]))
        );
        Add(
            "maps",
            "size",
            1,
            (c, a) => a[0] is MapTerm m ? Term.I(m.Entries.Count) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[0]))
        );
        r.Register(
            "io",
            "format",
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
            if (op == "/" && y.Value == 0 || op is "div" or "rem" && y.Value == 0)
                throw new ErlangException(ErlangErrorReasons.BadArithmetic);
            if (op == "/")
            {
                if (!x.TryToDouble(out double numerator) || !y.TryToDouble(out double denominator))
                    throw new ErlangException(ErlangErrorReasons.BadArithmetic);

                return new FloatTerm(numerator / denominator);
            }

            return op switch
            {
                "+" => new Integer(x.Value + y.Value),
                "-" => new Integer(x.Value - y.Value),
                "*" => new Integer(x.Value * y.Value),
                "div" => new Integer(x.Value / y.Value),
                "rem" => new Integer(x.Value % y.Value),
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
            "+" => left + right,
            "-" => left - right,
            "*" => left * right,
            "/" => left / right,
            _ => throw new ErlangException(ErlangErrorReasons.BadArithmetic)
        });
    }
}
