using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using Erlang;
using Erlang.Compiler;
using Erlang.Otp;

var tests = new List<(string Name, Func<Task> Body)>();
void Test(string name, Func<Task> body) => tests.Add((name, body));
void Check(bool value, string message = "Assertion failed")
{
    if (!value)
        throw new InvalidOperationException(message);
}
void Equal(Term a, Term b) => Check(a.Equals(b), $"Expected {b}, got {a}");
void Throws<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }
    throw new InvalidOperationException("Expected " + typeof(T).Name);
}
Test(
    "patterns/alias-mismatch-transaction",
    () =>
{
    var bindings = new Dictionary<string, Term> { ["incoming"] = Term.I(7) };
    var pattern = new AliasPattern(new Pattern.Variable("fresh"), new Pattern.Literal(Term.I(2)));
    Check(!pattern.Match(Term.I(1), bindings));
    Check(bindings.Count == 1 && !bindings.ContainsKey("fresh"));
    Equal(bindings["incoming"], Term.I(7));

    return Task.CompletedTask;
}
);
Test(
    "patterns/alias-same-term-and-bindings",
    () =>
{
    var bindings = new Dictionary<string, Term>();
    var pattern = new AliasPattern(new Pattern.Variable("whole"), new Pattern.Tuple([new Pattern.Variable("part")]));
    var value = Term.Tuple(Term.I(42));
    Check(pattern.Match(value, bindings));
    Check(ReferenceEquals(bindings["whole"], value));
    Equal(bindings["part"], Term.I(42));

    return Task.CompletedTask;
}
);
foreach (string source in new[] { "1 ?= 1", "maybe end", "maybe ok else end", "maybe ok ?= wrong ?= nope end", "maybe (ok ?= ok) end", "maybe ok ?= wrong else _ -> ok ?= wrong end" })
{
    Test(
        "compiler/maybe/invalid-grammar/" + source,
        () =>
    {
        Throws<CompileException>(() => new Parser(source).ParseExpression());

        return Task.CompletedTask;
    }
    );
}
Test(
    "compiler/maybe/illegal-guard",
    () =>
{
    try
    {
        new Parser("-module(maybe_guard). -export([run/0]). run()->if maybe true end -> ok end.").ParseModule();
        Check(false);
    }
    catch (CompileException exception)
    {
        Check(exception.Code == "ERL007" && exception.Message == "maybe is not legal in a guard");
    }

    return Task.CompletedTask;
}
);
Test(
    "hybrid/maybe/csharp-method-preserved",
    () =>
{
    string source = "class C { int maybe() => 1; int M() { return maybe(); } }";
    string result = CodeGeneration.Preprocess(source, "maybe-method.cs");
    Check(result.EndsWith(source, StringComparison.Ordinal));

    return Task.CompletedTask;
}
);
Test(
    "hybrid/maybe/nested-and-following-csharp",
    () =>
{
    string result = CodeGeneration.Preprocess("var x = maybe ok ?= maybe ok end,42 else _ -> 0 end. var y = 7;", "maybe.cs");
    Check(result.Contains("Expr.Maybe(", StringComparison.Ordinal) && result.EndsWith(" var y = 7;", StringComparison.Ordinal));

    return Task.CompletedTask;
}
);
Test(
    "oracle/unicode-source-ascii-transport",
    () =>
{
    string source = "try ('𐀀' > '\uffff') of X -> {ok,X} end";
    string command = Erlang.Differential.OracleProtocol.EvaluationCommand(source);
    Check(command.All(c => c <= 127));
    string encoded = command.Split("base64:decode(\"", StringSplitOptions.None)[1].Split('"')[0];
    Check(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded)) == source + ".");
    Check(command.Contains("unicode:characters_to_list") && command.Contains("erl_scan:string") && command.Contains("erl_eval:exprs"));

    return Task.CompletedTask;
}
);
Test(
    "oracle/source-quoting-roundtrip",
    () =>
{
    string source = "{\"quote\\\" slash\\\\\", 'Привет 🌍',\n42}";
    string command = Erlang.Differential.OracleProtocol.EvaluationCommand(source);
    Check(command.All(c => c <= 127) && !command.Contains('\n'));
    string encoded = command.Split("base64:decode(\"", StringSplitOptions.None)[1].Split('"')[0];
    Check(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded)) == source + ".");

    return Task.CompletedTask;
}
);
Test(
    "style/documentation-comment-boundary",
    () =>
{
    string source = "class Example { public void First() {}\n/// <summary>Preserve this text.</summary>\npublic void Second() {} }";
    string formatted = SourceLayout.Apply(source);
    Check(formatted.Contains("/// <summary>Preserve this text.</summary>"));
    Check(formatted.Contains("}\n\n    ///"));
    Check(SourceLayout.Apply(formatted) == formatted);

    return Task.CompletedTask;
}
);
Test(
    "style/multiline-documentation-comment",
    () =>
{
    string source = "class Example { public void First() {}\n/// <summary>\n/// Preserve both lines.\n/// </summary>\npublic void Second() {} }";
    string formatted = SourceLayout.Apply(source);
    Check(formatted.Contains("/// <summary>\n    /// Preserve both lines.\n    /// </summary>"));
    Check(SourceLayout.Apply(formatted) == formatted);

    return Task.CompletedTask;
}
);
async Task<Term> Eval(string source)
{
    await using var runtime = new ProcessRuntime();
    Term? result = null;
    var e = new Parser(source).ParseExpression();
    Semantics.Validate(e);
    var p = runtime.Spawn(async c =>
 {
     result = await Execution.EvaluateAsync(e, c);

     return Term.A("ok");
 });
    Equal(await p.Completion.WaitAsync(TimeSpan.FromSeconds(3)), Term.A("normal"));

    return result!;
}
Test(
    "terms/common-atoms-reused",
    () =>
{
    string[] names = ["ok", "error", "true", "false", "undefined", "normal", "timeout", "badarg", "noproc", "shutdown"];
    foreach (string name in names)
    {
        var first = Term.A(name);
        var second = Term.A(new string(name.ToCharArray()));
        Check(ReferenceEquals(first, second));
        Equal(first, new Atom(name));
        Check(first.GetHashCode() == new Atom(name).GetHashCode());
    }

    return Task.CompletedTask;
}
);
Test(
    "terms/common-atoms-concurrent",
    () =>
{
    var expected = Term.A("ok");
    var results = new Atom[1024];
    Parallel.For(0, results.Length, index => results[index] = Term.A("ok"));
    Check(results.All(atom => ReferenceEquals(atom, expected)));

    return Task.CompletedTask;
}
);
Test(
    "terms/common-atoms-no-per-call-allocation",
    () =>
{
    var expected = Term.A("ok");
    long before = GC.GetAllocatedBytesForCurrentThread();
    bool same = true;
    for (int index = 0; index < 1024; index++)
        same &= ReferenceEquals(expected, Term.A("ok"));
    long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    Check(same && allocated == 0);

    return Task.CompletedTask;
}
);
Test(
    "terms/arbitrary-atoms-not-retained",
    () =>
{
    foreach (string name in new[] { "application_specific_atom", "OK", "", "𐀀", "ok\0" })
    {
        var first = Term.A(name);
        var second = Term.A(name);
        Check(!ReferenceEquals(first, second));
        Equal(first, second);
        Check(first.GetHashCode() == second.GetHashCode());
    }

    return Task.CompletedTask;
}
);
Test(
    "terms/atom-factory-null-contract",
    () =>
{
    try
    {
        Term.A(null!);
        Check(false);
    }
    catch (ArgumentNullException exception)
    {
        Check(exception.ParamName == "name");
    }

    return Task.CompletedTask;
}
);
Test(
    "terms/atom-reuse-parser-etf",
    () =>
{
    var atom = Term.A("ok");
    var parsed = new Parser("ok").ParseExpression();
    Check(parsed is Expr.Literal literal && ReferenceEquals(literal.Value, atom));
    Check(ReferenceEquals(ExternalTermFormat.Decode(ExternalTermFormat.Encode(atom)), atom));

    return Task.CompletedTask;
}
);
Test(
    "terms/exact-numeric-equality",
    () =>
 {
     Check(!Term.I(1).Equals(new FloatTerm(1)));
     Check(Term.I(1).NumericEquals(new FloatTerm(1)));

     return Task.CompletedTask;
 }
);
Test(
    "terms/large-integer-double-comparison",
    () =>
 {
     Check(new Integer(BigInteger.Parse("9007199254740993")).CompareTo(new FloatTerm(9007199254740992)) > 0);
     Check(new Integer(BigInteger.One << 2000).CompareTo(new FloatTerm(double.MaxValue)) > 0);

     return Task.CompletedTask;
 }
);
Test(
    "terms/integer-double-nearest-even",
    () =>
 {
     foreach ((string source, double expected) in new[] { ("9007199254740993", 9007199254740992.0), ("9007199254740995", 9007199254740996.0), ("-9007199254740995", -9007199254740996.0), ("18014398509481983", 18014398509481984.0) })
     {
         Check(new Integer(BigInteger.Parse(source)).TryToDouble(out double actual));
         Check(actual == expected);
     }

     return Task.CompletedTask;
 }
);
Test(
    "terms/integer-double-overflow",
    () =>
 {
     Check(!new Integer(BigInteger.One << 2000).TryToDouble(out _));
     BigInteger max = (BigInteger.One << 1024) - (BigInteger.One << 971);
     Check(new Integer(max + (BigInteger.One << 970) - 1).TryToDouble(out double value) && value == double.MaxValue);
     Check(!new Integer(max + (BigInteger.One << 970)).TryToDouble(out _));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/numeric-mixed-nearest-even",
    async () => Equal(
        await Eval("{9007199254740995+0.0,0.0+9007199254740995,9007199254740995*1.0,9007199254740995-9007199254740996.0}"),
        Term.Tuple(
            new FloatTerm(9007199254740996.0),
            new FloatTerm(9007199254740996.0),
            new FloatTerm(9007199254740996.0),
            new FloatTerm(0.0)
        )
    )
);
Test(
    "compiler/numeric-integer-division-rounding",
    async () =>
 {
     Equal(await Eval("9007199254740995/1"), new FloatTerm(9007199254740996.0));
     Equal(await Eval("1/9007199254740995"), new FloatTerm(1.0 / 9007199254740996.0));
 }
);
Test(
    "compiler/numeric-conversion-overflow-badarith",
    async () =>
 {
     string huge = (BigInteger.One << 2000).ToString(System.Globalization.CultureInfo.InvariantCulture);
     foreach (string source in new[] { "1/" + huge, huge + "*0.0", "0.0/" + huge })
         Equal(await MapError(source), Term.A("badarith"));
 }
);
Test(
    "compiler/numeric-conversion-guard-failure",
    async () =>
 {
     string huge = (BigInteger.One << 2000).ToString(System.Globalization.CultureInfo.InvariantCulture);
     Equal(await Eval("case " + huge + " of X when 0.0/X =:= 0.0 -> wrong; _ -> ok end"), Term.A("ok"));
 }
);
Test(
    "terms/negative-fraction-comparison",
    () =>
 {
     Check(Term.I(-1).CompareTo(new FloatTerm(-1.5)) > 0);
     Check(Term.I(0).CompareTo(new FloatTerm(double.Epsilon)) < 0);

     return Task.CompletedTask;
 }
);
Test(
    "terms/type-order",
    () =>
 {
     Term[] terms = [Term.I(1), Term.A("a"), new ReferenceTerm("n", 1), new FunctionTerm(0, (c, a) => ValueTask.FromResult<Term>(Term.A("ok"))), new PortTerm("n", 1), new Pid("n", 1), Term.Tuple(), new MapTerm([]), Nil.Value, Term.List(Term.I(0)), new BitString([])];
     for (int i = 1; i < terms.Length; i++)
         Check(terms[i - 1].CompareTo(terms[i]) < 0);

     return Task.CompletedTask;
 }
);
Test(
    "terms/tuple-arity-first",
    () =>
 {
     Check(Term.Tuple(Term.I(999)).CompareTo(Term.Tuple(Term.I(0), Term.I(0))) < 0);

     return Task.CompletedTask;
 }
);
Test(
    "terms/improper-list",
    () =>
 {
     var l = new Cons(Term.I(1), Term.A("tail"));
     Equal(l, new Cons(Term.I(1), Term.A("tail")));
     Throws<ErlangException>(() => Cons.Items(l).ToArray());

     return Task.CompletedTask;
 }
);
Test(
    "terms/map-exact-keys",
    () =>
 {
     var m = new MapTerm([new(Term.I(1), Term.A("int")), new(new FloatTerm(1), Term.A("float"))]);
     Check(m.Entries.Count == 2);
     Equal(m.Get(Term.I(1)), Term.A("int"));
     Equal(m.Get(new FloatTerm(1)), Term.A("float"));

     return Task.CompletedTask;
 }
);
Test(
    "terms/map-integer-before-all-floats",
    () =>
 {
     var m = new MapTerm([new(new FloatTerm(-100), Term.A("float")), new(Term.I(100), Term.A("integer"))]);
     Check(m.Entries[0].Key is Integer);

     return Task.CompletedTask;
 }
);
Test(
    "terms/signed-zero-otp29",
    () =>
 {
     Term positive = new FloatTerm(0.0), negative = new FloatTerm(-0.0);
     Check(!positive.Equals(negative));
     Check(positive.NumericEquals(negative));
     Equal(ExternalTermFormat.Decode(ExternalTermFormat.Encode(negative)), negative);

     return Task.CompletedTask;
 }
);
Test("terms/atom-codepoint-order", () =>
 {
     Check(Term.A("\U00010000").CompareTo(Term.A("\uffff")) > 0);

     return Task.CompletedTask;
 });
Test("compiler/quoted-unicode-valid-codepoint-order", async () => Equal(await Eval("'𐀀' > '\ufffd'"), Term.A("true")));
Test(
    "compiler/quoted-unicode-illegal-character",
    () =>
{
    foreach (string invalid in new[] { "\ufffe", "\uffff", "\ud800", "\udfff" })
        foreach (string quote in new[] { "'", "\"" })
            Throws<CompileException>(() => new Parser(quote + invalid + quote).ParseExpression());

    return Task.CompletedTask;
}
);
Test(
    "terms/structural-hash",
    () =>
 {
     Term[] a = [Term.Tuple(Term.I(1), Term.List(Term.A("a"))), new FloatTerm(-0.0), new BitString([255], 3), new MapTerm([new(Term.A("x"), Term.I(2))])];
     foreach (var t in a)
         Check(t.GetHashCode() == ExternalTermFormat.Decode(ExternalTermFormat.Encode(t)).GetHashCode());

     return Task.CompletedTask;
 }
);
Test("terms/unicode-list", () =>
 {
     string s = "Привет 🌍";
     Check(Term.Text(Term.String(s)) == s);

     return Task.CompletedTask;
 });
Test(
    "terms/immutable-binary",
    () =>
 {
     byte[] bytes = [255];
     var b = new BitString(bytes, 3);
     bytes[0] = 0;
     Check(b.ToArray()[0] == 224);
     var copy = b.ToArray();
     copy[0] = 0;
     Check(b.ToArray()[0] == 224);

     return Task.CompletedTask;
 }
);
Test(
    "patterns/repeated-variable",
    () =>
 {
     var p = new Pattern.Tuple([new Pattern.Variable("X"), new Pattern.Variable("X")]);
     var b = new Dictionary<string, Term>();
     Check(!p.Match(Term.Tuple(Term.I(1), Term.I(2)), b));
     Check(b.Count == 0);
     Check(p.Match(Term.Tuple(Term.I(1), Term.I(1)), b));
     Equal(b["X"], Term.I(1));

     return Task.CompletedTask;
 }
);
Test(
    "patterns/already-bound",
    () =>
 {
     var b = new Dictionary<string, Term> { { "X", Term.I(1) } };
     Check(!new Pattern.Variable("X").Match(new FloatTerm(1), b));
     Equal(b["X"], Term.I(1));

     return Task.CompletedTask;
 }
);
Test(
    "patterns/list-tail",
    () =>
 {
     var p = new Pattern.List([new Pattern.Variable("H")], new Pattern.Variable("T"));
     var b = new Dictionary<string, Term>();
     Check(p.Match(Term.List(Term.I(1), Term.I(2)), b));
     Equal(b["T"], Term.List(Term.I(2)));

     return Task.CompletedTask;
 }
);
Test(
    "mailbox/later-match-preserves-order",
    async () =>
 {
     var m = new Mailbox();
     m.Send(Term.I(1));
     m.Send(Term.A("pick"));
     m.Send(Term.I(2));
     Equal((await m.ReceiveAsync(t => t is Atom ? t : null, TimeSpan.Zero))!, Term.A("pick"));
     Equal((await m.ReceiveAsync(t => t, TimeSpan.Zero))!, Term.I(1));
     Equal((await m.ReceiveAsync(t => t, TimeSpan.Zero))!, Term.I(2));
 }
);
Test(
    "mailbox/zero-timeout-retains",
    async () =>
 {
     var m = new Mailbox();
     m.Send(Term.I(1));
     Check(await m.ReceiveAsync(t => t is Atom ? t : null, TimeSpan.Zero) == null);
     Check(m.Count == 1);
 }
);
Test(
    "mailbox/timeout",
    async () =>
 {
     var m = new Mailbox();
     var sw = Stopwatch.StartNew();
     Check(await m.ReceiveAsync(t => t, TimeSpan.FromMilliseconds(25)) == null);
     Check(sw.ElapsedMilliseconds >= 15);
 }
);
Test(
    "mailbox/concurrent-arrival",
    async () =>
 {
     var m = new Mailbox();
     var receive = m.ReceiveAsync(t => t, TimeSpan.FromSeconds(2));
     await Task.Yield();
     m.Send(Term.I(42));
     Equal((await receive)!, Term.I(42));
 }
);
Test(
    "mailbox/cancel",
    async () =>
 {
     var m = new Mailbox();
     using var cancel = new CancellationTokenSource();
     var task = m.ReceiveAsync(t => t, null, cancel.Token).AsTask();
     cancel.Cancel();
     try
     {
         await task;
         throw new InvalidOperationException();
     }
     catch (OperationCanceledException) { }
 }
);
Test(
    "mailbox/sender-order-stress",
    async () =>
 {
     var m = new Mailbox();
     await Task.WhenAll(Enumerable.Range(0, 4).Select(sender => Task.Run(() =>
     {
         for (int i = 0; i < 250; i++)
             m.Send(Term.Tuple(Term.I(sender), Term.I(i)));
     })));
     for (int sender = 0; sender < 4; sender++)
         for (int i = 0; i < 250; i++)
         {
             int s = sender;
             var v = (TupleTerm)(await m.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.I(s)) ? x : null, TimeSpan.Zero))!;
             Equal(v.Items[1], Term.I(i));
         }
     Check(m.Count == 0);
 }
);
Test(
    "runtime/process-self-and-dictionary",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var p = r.Spawn(async c =>
     {
         Equal(await r.Modules.Call(c, "erlang", "self"), c.Self);
         Equal(await r.Modules.Call(
             c,
             "erlang",
             "put",
             Term.A("key"),
             Term.I(42)
         ), Term.A("undefined"));
         Equal(await r.Modules.Call(
             c,
             "erlang",
             "get",
             Term.A("key")
         ), Term.I(42));
         Equal(await r.Modules.Call(
             c,
             "erlang",
             "erase",
             Term.A("key")
         ), Term.I(42));

         return Term.A("ok");
     });
     Equal(await p.Completion, Term.A("normal"));
 }
);
Test(
    "runtime/registration-cleanup",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
     var p = r.Spawn(async c =>
     {
         await gate.Task;

         return Term.A("ok");
     });
     r.Register("test", p.Pid);
     Equal(r.WhereIs("test"), p.Pid);
     Throws<ErlangException>(() => r.Register("other", p.Pid));
     gate.SetResult();
     Equal(await p.Completion, Term.A("normal"));
     Equal(r.WhereIs("test"), Term.A("undefined"));
 }
);
Test(
    "runtime/monitor-down",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
     var target = r.Spawn(async c =>
     {
         await release.Task;
         throw new ErlangException(Term.A("boom"), "exit");
     });
     Term? down = null;
     var observer = r.Spawn(async c =>
     {
         var reference = r.Monitor(c, target.Pid);
         release.TrySetResult();
         down = await c.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.A("DOWN")) && x.Items[1].Equals(reference) ? x : null, TimeSpan.FromSeconds(2));

         return Term.A("ok");
     });
     Equal(await observer.Completion, Term.A("normal"));
     Check(down is TupleTerm { Items.Count: 5 } d && d.Items[4].Equals(Term.A("boom")));
 }
);
Test(
    "runtime/monitor-noproc-and-flush",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var observer = r.Spawn(c =>
     {
         var reference = r.Monitor(c, new Pid(r.Node, 999));
         Check(c.Mailbox.Count == 1);
         r.Demonitor(c, reference, true);
         Check(c.Mailbox.Count == 0);

         return ValueTask.FromResult<Term>(Term.A("ok"));
     });
     Equal(await observer.Completion, Term.A("normal"));
 }
);
Test(
    "runtime/link-trap-exit",
    async () =>
 {
     await using var r = new ProcessRuntime();
     Term? signal = null;
     var parent = r.Spawn(async c =>
     {
         c.TrapExits = true;
         var child = r.Spawn(x => throw new ErlangException(Term.A("boom"), "exit"), c, true);
         signal = await c.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.A("EXIT")) ? x : null, TimeSpan.FromSeconds(2));

         return Term.A("ok");
     });
     Equal(await parent.Completion, Term.A("normal"));
     Check(signal is TupleTerm x && x.Items[2].Equals(Term.A("boom")));
 }
);
Test(
    "runtime/link-propagation",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var parent = r.Spawn(async c =>
     {
         r.Spawn(x => throw new ErlangException(Term.A("boom"), "exit"), c, true);
         await c.ReceiveAsync(t => t);

         return Term.A("unreachable");
     });
     Equal(await parent.Completion, Term.A("boom"));
 }
);
Test(
    "runtime/normal-link-exit-ignored",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var parent = r.Spawn(async c =>
     {
         var child = r.Spawn(x => ValueTask.FromResult<Term>(Term.A("ok")), c, true);
         await child.Completion;
         Check(r.IsAlive(c.Self));

         return Term.A("ok");
     });
     Equal(await parent.Completion, Term.A("normal"));
 }
);
Test(
    "runtime/kill-untrappable",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
     var target = r.Spawn(async c =>
     {
         c.TrapExits = true;
         ready.SetResult();
         await c.ReceiveAsync(t => t);

         return Term.A("unreachable");
     });
     await ready.Task;
     var sender = r.Spawn(c =>
     {
         r.Exit(c, target.Pid, Term.A("kill"));

         return ValueTask.FromResult<Term>(Term.A("ok"));
     });
     await sender.Completion;
     Equal(await target.Completion, Term.A("killed"));
     Check(!r.IsAlive(target.Pid));
 }
);
Test(
    "runtime/1000-waiting-processes",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var ps = Enumerable.Range(0, 1000).Select(_ => r.Spawn(async c =>
     {
         Equal((await c.ReceiveAsync(t => t, TimeSpan.FromSeconds(5)))!, Term.A("go"));

         return Term.A("ok");
     })).ToArray();
     foreach (var p in ps)
         r.Send(p.Pid, Term.A("go"));
     var reasons = await Task.WhenAll(ps.Select(p => p.Completion));
     Check(reasons.All(x => x.Equals(Term.A("normal"))));
 }
);
Test("compiler/arithmetic-precedence", async () => Equal(await Eval("1 + 2 * 3"), Term.I(7)));
Test(
    "compiler/exact-equality",
    async () => Equal(await Eval("{1 == 1.0, 1 =:= 1.0}"), Term.Tuple(Term.A("true"), Term.A("false")))
);
Test(
    "compiler/case-pattern-guard",
    async () => Equal(await Eval("case {hello, 42} of {hello, X} when is_integer(X), X > 0 -> X; _ -> bad end"), Term.I(42))
);
Test(
    "compiler/guard-error-rejects",
    async () => Equal(await Eval("case a of X when hd(X) == 1 -> bad; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/guard-alternative",
    async () => Equal(await Eval("case a of X when hd(X) == 1; is_atom(X) -> ok end"), Term.A("ok"))
);
Test(
    "compiler/list-fun-map",
    async () => Equal(await Eval("lists:map(fun(X) -> X * 2 end, [1,2,3])"), Term.List(Term.I(2), Term.I(4), Term.I(6)))
);
Test(
    "compiler/receive-after-zero",
    async () => Equal(await Eval("receive {hello, X} -> X after 0 -> timeout end"), Term.A("timeout"))
);
Test("compiler/binding-and-repeat", async () => Equal(await Eval("case {1,1} of {X,X} -> X; _ -> bad end"), Term.I(1)));
Test(
    "compiler/single-assignment-mismatch",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var e = new Parser("case ok of ok -> X = 1, X = 2 end").ParseExpression();
     Semantics.Validate(e);
     var p = r.Spawn(c => Execution.EvaluateAsync(e, c));
     Check((await p.Completion) is TupleTerm t && t.Items[0] is TupleTerm error && error.Items[0].Equals(Term.A("badmatch")));
 }
);
Test(
    "compiler/unbound-diagnostic",
    () =>
 {
     Throws<CompileException>(() => Semantics.Validate(new Parser("X + 1").ParseExpression()));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/unsafe-variable-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("-module(a). -export([f/1]). f(A) -> case A of 1 -> X = 1; _ -> ok end, X.").ParseModule());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/illegal-guard-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("-module(a). -export([f/1]). f(X) when io:format(\"~p\", [X]) -> ok.").ParseModule());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/quoted-keyword-atom",
    async () => Equal(await Eval("{'receive', 'end'}"), Term.Tuple(Term.A("receive"), Term.A("end")))
);
Test(
    "compiler/module-clauses-private-function",
    async () =>
 {
     var m = new Parser("-module(a). -export([f/1]). f(0) -> zero; f(X) when is_integer(X) -> g(X). g(X) -> X + 1.").ParseModule();
     await using var r = new ProcessRuntime();
     m.Register(r.Modules);
     Term? result = null;
     var p = r.Spawn(async c =>
     {
         result = await r.Modules.Call(
             c,
             "a",
             "f",
             Term.I(2)
         );

         return Term.A("ok");
     });
     Equal(await p.Completion, Term.A("normal"));
     Equal(result!, Term.I(3));
     Check(!r.Modules.Exports.Contains(("a", "g", 1)));
 }
);
Test(
    "compiler/spawn-fun-child-self",
    async () =>
 {
     await using var r = new ProcessRuntime();
     Term? result = null;
     var e = new Parser("spawn(fun() -> self() end)").ParseExpression();
     var p = r.Spawn(async c =>
     {
         result = await Execution.EvaluateAsync(e, c);
         Check(result is Pid child && !child.Equals(c.Self));

         return Term.A("ok");
     });
     Equal(await p.Completion, Term.A("normal"));
 }
);
Test(
    "compiler/selective-receive-clause-order",
    async () =>
 {
     await using var r = new ProcessRuntime();
     Term? result = null;
     var e = new Parser("receive {n, X} when X > 10 -> high; {n, X} -> X after 1000 -> timeout end").ParseExpression();
     var p = r.Spawn(async c =>
     {
         result = await Execution.EvaluateAsync(e, c);

         return Term.A("ok");
     });
     r.Send(p.Pid, Term.Tuple(Term.A("other"), Term.I(0)));
     r.Send(p.Pid, Term.Tuple(Term.A("n"), Term.I(1)));
     r.Send(p.Pid, Term.Tuple(Term.A("n"), Term.I(20)));
     Equal(await p.Completion, Term.A("normal"));
     Equal(result!, Term.I(1));
 }
);
Test(
    "hybrid/csharp-strings-comments-raw-preserved",
    () =>
 {
     string source = "class A { string s = \"receive {a} -> ok end.\"; string r = \"\"\"case x of a -> b end.\"\"\"; /* receive {a} -> ok end. */ int receive() => 1; }";
     string generated = CodeGeneration.Preprocess(source, "a.cs");
     Check(generated.EndsWith(source, StringComparison.Ordinal));

     return Task.CompletedTask;
 }
);
Test(
    "hybrid/source-map-and-lowering",
    () =>
 {
     string source = "using Erlang; class A { async Task<Term> Run(ProcessContext erlangProcess) { return receive {hello, Name} -> Name after 0 -> ok end. } }";
     string generated = CodeGeneration.Preprocess(source, "a.cs");
     Check(generated.Contains("#line 1"));
     Check(generated.Contains("await global::Erlang.Compiler.Execution.EvaluateAsync"));
     Check(!generated.Contains("return receive"));

     return Task.CompletedTask;
 }
);
Test(
    "hybrid/nested-case-and-fun",
    () =>
 {
     string source = "class A { async Task Run(ProcessContext erlangProcess) { var result = case 1 of X -> fun(Y) -> X + Y end end. } }";
     string generated = CodeGeneration.Preprocess(source, "a.cs");
     Check(generated.Contains("Expr.Case"));
     Check(generated.Contains("Expr.Fun"));

     return Task.CompletedTask;
 }
);
Test(
    "etf/reference-vectors",
    () =>
 {
     Equal(ExternalTermFormat.Decode([131, 97, 42]), Term.I(42));
     Equal(ExternalTermFormat.Decode([131, 100, 0, 2, 111, 107]), Term.A("ok"));
     Equal(ExternalTermFormat.Decode([131, 104, 2, 119, 2, 111, 107, 97, 42]), Term.Tuple(Term.A("ok"), Term.I(42)));
     Check(ExternalTermFormat.Encode(Term.I(42)).SequenceEqual(new byte[] { 131, 97, 42 }));

     return Task.CompletedTask;
 }
);
Test(
    "etf/roundtrip-all-supported-kinds",
    () =>
 {
     Term[] terms = [new Integer(BigInteger.One << 300), new Integer(-(BigInteger.One << 300)), Term.I(-42), new FloatTerm(1.5), Term.A("世界"), Term.List(Term.I(1)), new Cons(Term.A("x"), Term.A("tail")), Term.Tuple(Term.A("ok"), Nil.Value), new BitString([128], 1), new BitString([0, 255]), new Pid("n@h", ulong.MaxValue, 42), new PortTerm("n@h", ulong.MaxValue, 42), new ReferenceTerm("n@h", ulong.MaxValue, 42), new MapTerm([new(Term.I(1), Term.A("a")), new(new FloatTerm(1), Term.A("b"))])];
     foreach (var t in terms)
         Equal(ExternalTermFormat.Decode(ExternalTermFormat.Encode(t)), t);

     return Task.CompletedTask;
 }
);
Test(
    "etf/malformed-input-limits",
    () =>
 {
     Throws<ErlangException>(() => ExternalTermFormat.Decode([131, 108, 255, 255, 255, 255]));
     Throws<ErlangException>(() => ExternalTermFormat.Decode([131, 97]));
     Throws<ErlangException>(() => ExternalTermFormat.Decode([131, 97, 1, 0]));
     Throws<ErlangException>(() => ExternalTermFormat.Decode([131, 119, 1, 255]));
     Throws<NotSupportedException>(() => ExternalTermFormat.Decode([131, 112]));

     return Task.CompletedTask;
 }
);
Test(
    "etf/random-integer-property",
    () =>
 {
     var random = new Random(42);
     for (int i = 0; i < 200; i++)
     {
         byte[] bytes = new byte[random.Next(1, 128)];
         random.NextBytes(bytes);
         Term n = new Integer(new BigInteger(bytes));
         Equal(ExternalTermFormat.Decode(ExternalTermFormat.Encode(n)), n);
     }

     return Task.CompletedTask;
 }
);
Test("modules/lists-member-exact", async () => Equal(await Eval("lists:member(1, [1.0])"), Term.A("false")));
Test(
    "modules/lists-and-maps",
    async () =>
 {
     Equal(
         await Eval("{length([1,2]), lists:reverse([1,2]), lists:sum([1,2,3]), hd([ok])}"),
         Term.Tuple(
             Term.I(2),
             Term.List(Term.I(2), Term.I(1)),
             Term.I(6),
             Term.A("ok")
         )
     );
 }
);
Test(
    "compiler/tail-recursion-50000",
    async () =>
 {
     var m = new Parser("-module(loop). -export([run/1]). run(0) -> ok; run(N) -> case N > 0 of true -> run(N - 1) end.").ParseModule();
     await using var r = new ProcessRuntime();
     m.Register(r.Modules);
     Term? result = null;
     var p = r.Spawn(async c =>
     {
         result = await r.Modules.Call(
             c,
             "loop",
             "run",
             Term.I(50000)
         );

         return Term.A("ok");
     });
     Equal(await p.Completion.WaitAsync(TimeSpan.FromSeconds(10)), Term.A("normal"));
     Equal(result!, Term.A("ok"));
 }
);
Test(
    "hybrid/ordinary-fun-call",
    () =>
 {
     string source = "class C { int fun(int x)=>x; int F()=>fun(1); int G(){return fun(2);} }";
     Check(CodeGeneration.Preprocess(source, "c.cs").EndsWith(source, StringComparison.Ordinal));

     return Task.CompletedTask;
 }
);
Test(
    "hybrid/csharp-switch-and-erlang-case",
    () =>
 {
     string source = "class C { async Task F(ProcessContext erlangProcess) { switch(1){case 1: break; case 2: break; default: break;} var result = case 2 of X -> X end. } }";
     var generated = CodeGeneration.Preprocess(source, "c.cs");
     Check(generated.Contains("switch(1){case 1: break; case 2: break; default: break;}"));
     Check(generated.Contains("Expr.Case"));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/unsafe-variable-rebinding-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("-module(a). -export([f/1]). f(A) -> case A of 1 -> X = 1; _ -> ok end, X = 2.").ParseModule());

     return Task.CompletedTask;
 }
);
Test("compiler/signed-zero-pattern", async () => Equal(await Eval("case -0.0 of 0.0 -> wrong; -0.0 -> ok end"), Term.A("ok")));
Test(
    "otp/gen-server-call-cast-info-stop",
    async () =>
{
    await using var r = new ProcessRuntime();
    var callback = new CounterServer();
    var server = await GenServer.Start(r, callback, Term.I(0));
    var client = r.Spawn(async c =>
 {
     GenServer.Cast(c, server.Pid, Term.I(2));
     r.Send(server.Pid, Term.I(3));
     Equal(await GenServer.Call(c, server.Pid, Term.A("get")), Term.I(5));
     Equal(await GenServer.Call(c, server.Pid, Term.A("stop")), Term.A("ok"));

     return Term.A("ok");
 });
    Equal(await client.Completion, Term.A("normal"));
    Equal(await server.Completion, Term.A("normal"));
    Check(callback.Terminated);
}
);
Test(
    "otp/gen-server-crash-monitor",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var server = await GenServer.Start(r, new CounterServer(), Term.I(0));
     var client = r.Spawn(async c =>
     {
         try
         {
             await GenServer.Call(c, server.Pid, Term.A("crash"));
             throw new InvalidOperationException("Expected crash");
         }
         catch (ErlangException ex)
         {
             Check(ex.ExceptionClass == "exit");
         }

         return Term.A("ok");
     });
     Equal(await client.Completion, Term.A("normal"));
     Check(!r.IsAlive(server.Pid));
 }
);
Test(
    "otp/gen-server-timeout",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var server = await GenServer.Start(r, new CounterServer(), Term.I(0));
     var client = r.Spawn(async c =>
     {
         try
         {
             await GenServer.Call(
                 c,
                 server.Pid,
                 Term.A("noreply"),
                 TimeSpan.FromMilliseconds(20)
             );
             throw new InvalidOperationException("Expected timeout");
         }
         catch (ErlangException ex)
         {
             Equal(ex.Reason, Term.A("timeout"));
         }

         return Term.A("ok");
     });
     Equal(await client.Completion, Term.A("normal"));
 }
);
Test(
    "otp/gen-server-init-error",
    async () =>
 {
     await using var r = new ProcessRuntime();
     try
     {
         await GenServer.Start(r, new CounterServer(), Term.A("fail"));
         throw new InvalidOperationException("Expected init failure");
     }
     catch (ErlangException ex)
     {
         Equal(ex.Reason, Term.A("init_failed"));
     }
 }
);
foreach (var strategy in Enum.GetValues<RestartStrategy>())
    Test(
        "otp/supervisor-" + strategy,
        async () =>
    {
        await using var r = new ProcessRuntime();
        int[] starts = [0, 0, 0];
        var restartObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        ChildSpec Spec(int index) => new(
            Term.I(index),
            c =>
 {
     if (Interlocked.Increment(ref starts[index]) == 2 && index == 1)
         restartObserved.TrySetResult();

     return ValueTask.FromResult(r.Spawn(async child =>
     {
         await child.ReceiveAsync(t => t);

         return Term.A("ok");
     }, c, true));
 }
        );
        var supervisor = await Supervisor.Start(
            r,
            [Spec(0), Spec(1), Spec(2)],
            strategy,
            5
        );
        var initial = supervisor.Children.Select(x => x.Pid).ToArray();
        var killer = r.Spawn(c =>
 {
     r.Exit(c, initial[1], Term.A("boom"));

     return ValueTask.FromResult<Term>(Term.A("ok"));
 });
        await killer.Completion;
        await restartObserved.Task.WaitAsync(TimeSpan.FromSeconds(2));
        // Wait for the restart batch to finish publishing its complete child list.
        var deadline = Stopwatch.StartNew();
        while (supervisor.Children.Count != 3 && deadline.Elapsed < TimeSpan.FromSeconds(2))
            await Task.Delay(1);
        Check(starts[0] == (strategy == RestartStrategy.OneForAll ? 2 : 1));
        Check(starts[1] == 2);
        Check(starts[2] == (strategy == RestartStrategy.OneForOne ? 1 : 2));
        await Supervisor.Stop(r, supervisor);
        Equal(await supervisor.Process.Completion, Term.A("shutdown"));
        Check(supervisor.Children.Count == 0);
    }
    );
Test(
    "otp/supervisor-transient-temporary-normal",
    async () =>
 {
     await using var r = new ProcessRuntime();
     int starts = 0;
     var childReady = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously);
     var supervisor = await Supervisor.Start(
         r,
         [new ChildSpec(
             Term.A("t"),
             c =>
 {
 starts++;
 var p = r.Spawn(async x =>
 {
 await x.ReceiveAsync(t => t);

 return Term.A("ok");
 }, c, true);
 childReady.SetResult(p.Pid);

 return ValueTask.FromResult(p);
 },
             RestartPolicy.Transient
         )]
     );
     var pid = await childReady.Task;
     r.Send(pid, Term.A("finish"));
     var deadline = Stopwatch.StartNew();
     while (supervisor.Children.Count != 0 && deadline.Elapsed < TimeSpan.FromSeconds(2))
         await Task.Delay(1);
     Check(starts == 1 && supervisor.Children.Count == 0);
     await Supervisor.Stop(r, supervisor);
 }
);
Test(
    "otp/supervisor-restart-intensity",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var trigger = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously);
     var supervisor = await Supervisor.Start(
         r,
         [new ChildSpec(
             Term.A("child"),
             c =>
 {
 var p = r.Spawn(async x =>
 {
 await x.ReceiveAsync(t => t);
 x.Exit(Term.A("boom"));

 return Term.A("ok");
 }, c, true);
 trigger.TrySetResult(p.Pid);

 return ValueTask.FromResult(p);
 }
         )],
         intensity: 0
     );
     r.Send(await trigger.Task, Term.A("crash"));
     Equal(await supervisor.Process.Completion.WaitAsync(TimeSpan.FromSeconds(2)), Term.A("shutdown"));
     Check(supervisor.Children.Count == 0);
 }
);
Test(
    "otp/supervisor-temporary-abnormal-no-restart",
    async () =>
 {
     await using var r = new ProcessRuntime();
     int starts = 0;
     var supervisor = await Supervisor.Start(
         r,
         [new ChildSpec(
             Term.A("child"),
             c =>
 {
 starts++;

 return ValueTask.FromResult(r.Spawn(async x =>
 {
 await x.ReceiveAsync(t => t);
 x.Exit(Term.A("boom"));

 return Term.A("ok");
 }, c, true));
 },
             RestartPolicy.Temporary
         )],
         intensity: 0
     );
     r.Send(supervisor.Children[0].Pid, Term.A("crash"));
     var deadline = Stopwatch.StartNew();
     while (supervisor.Children.Count != 0 && deadline.Elapsed < TimeSpan.FromSeconds(2))
         await Task.Delay(1);
     Check(starts == 1 && supervisor.Children.Count == 0);
     Check(r.IsAlive(supervisor.Process.Pid));
     await Supervisor.Stop(r, supervisor);
 }
);
Test(
    "runtime/link-cascade-10000",
    async () =>
 {
     await using var r = new ProcessRuntime();
     ProcessContext? previous = null;
     var processes = new List<ProcessHandle>();
     for (int i = 0; i < 10000; i++)
     {
         var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
         var p = r.Spawn(async c =>
         {
             ready.SetResult(c);
             await c.ReceiveAsync(t => t);

             return Term.A("ok");
         }, previous, previous is not null);
         processes.Add(p);
         previous = await ready.Task;
     }
     var killer = r.Spawn(c =>
     {
         r.Exit(c, processes[0].Pid, Term.A("boom"));

         return ValueTask.FromResult<Term>(Term.A("ok"));
     });
     await killer.Completion;
     var reasons = await Task.WhenAll(processes.Select(p => p.Completion));
     Check(reasons.All(x => x.Equals(Term.A("boom"))));
 }
);
Test(
    "runtime/spawn-monitor-immediate-exit-atomic",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var parent = r.Spawn(async c =>
     {
         for (int i = 0; i < 200; i++)
         {
             var spawn = r.SpawnMonitor(c, x => throw new ErlangException(Term.A("boom"), "exit"));
             var down = await c.ReceiveAsync(t => t is TupleTerm x && x.Items.Count == 5 && x.Items[1].Equals(spawn.Reference) ? x : null, TimeSpan.FromSeconds(1));
             Check(down is not null && down.Items[4].Equals(Term.A("boom")));
         }

         return Term.A("ok");
     });
     Equal(await parent.Completion, Term.A("normal"));
 }
);
Test("terms/bitstring-format", () =>
 {
     Check(new BitString([224], 3).ToString() == "<<7:3>>");

     return Task.CompletedTask;
 });
Test(
    "compiler/large-literal-codegeneration",
    () =>
 {
     var term = Term.String(new string('a', 10000));
     var generated = CodeGeneration.TermCode(term);
     Check(generated.StartsWith("global::Erlang.Cons.From", StringComparison.Ordinal));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/reserved-keywords-rejected",
    () =>
 {
     Throws<CompileException>(() => new Parser("end").ParseExpression());
     Throws<CompileException>(() => new Parser("try ok end").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/unsupported-escape-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("\"\\x{41}\"").ParseExpression());

     return Task.CompletedTask;
 }
);
Test("compiler/quoted-operator-atom", async () => Equal(await Eval("{'not', 'div'}"), Term.Tuple(Term.A("not"), Term.A("div"))));
Test(
    "hybrid/nullable-context",
    () =>
 {
     Check(CodeGeneration.Preprocess("class C { string? value; }", "c.cs").StartsWith("#nullable enable", StringComparison.Ordinal));
     Check(CodeGeneration.Preprocess("class C {}", "c.cs", "disable").StartsWith("#nullable disable", StringComparison.Ordinal));

     return Task.CompletedTask;
 }
);

// Every registered MFA has a direct contract smoke test. Error/option completeness still needs OTP differential coverage.
var exportRegistry = new ModuleRegistry();
CoreModules.Register(exportRegistry);
foreach (var export in exportRegistry.Exports.OrderBy(x => x.Module).ThenBy(x => x.Function).ThenBy(x => x.Arity))
    Test(
        $"mfa/{export.Module}:{export.Function}/{export.Arity}",
        async () =>
    {
        using var output = new StringWriter();
        await using var runtime = new ProcessRuntime(output: output);
        var process = runtime.Spawn(async c =>
        {
            async ValueTask<Term> Call(params Term[] a) => await runtime.Modules.Call(
                c,
                export.Module,
                export.Function,
                a
            );
            (Term[] Args, Term Expected)? pure = (export.Module, export.Function, export.Arity) switch
            {
                ("erlang", "length", 1) => ([Term.List(Term.I(1), Term.I(2))], Term.I(2)),
                ("erlang", "hd", 1) => ([Term.List(Term.A("x"))], Term.A("x")),
                ("erlang", "tl", 1) => ([Term.List(Term.I(1), Term.I(2))], Term.List(Term.I(2))),
                ("erlang", "element", 2) => ([Term.I(2), Term.Tuple(Term.A("a"), Term.A("b"))], Term.A("b")),
                ("erlang", "tuple_size", 1) => ([Term.Tuple(Term.A("a"))], Term.I(1)),
                ("erlang", "is_atom", 1) => ([Term.A("a")], Term.A("true")),
                ("erlang", "is_integer", 1) => ([new FloatTerm(1)], Term.A("false")),
                ("erlang", "is_float", 1) => ([new FloatTerm(1)], Term.A("true")),
                ("erlang", "is_number", 1) => ([Term.I(1)], Term.A("true")),
                ("erlang", "is_tuple", 1) => ([Term.Tuple()], Term.A("true")),
                ("erlang", "is_binary", 1) => ([new BitString([128], 1)], Term.A("false")),
                ("erlang", "is_bitstring", 1) => ([new BitString([128], 1)], Term.A("true")),
                ("erlang", "bit_size", 1) => ([new BitString([128, 128], 9)], Term.I(9)),
                ("erlang", "byte_size", 1) => ([new BitString([128, 128], 9)], Term.I(2)),
                ("erlang", "is_list", 1) => ([new Cons(Term.I(1), Term.A("tail"))], Term.A("true")),
                ("erlang", "is_pid", 1) => ([c.Self], Term.A("true")),
                ("erlang", "is_map", 1) => ([new MapTerm([])], Term.A("true")),
                ("erlang", "map_size", 1) => ([new MapTerm([new(Term.A("a"), Term.I(1))])], Term.I(1)),
                ("erlang", "map_get", 2) => ([Term.A("a"), new MapTerm([new(Term.A("a"), Term.I(42))])], Term.I(42)),
                ("erlang", "is_map_key", 2) => ([Term.I(1), new MapTerm([new(new FloatTerm(1), Term.A("float"))])], Term.A("false")),
                ("lists", "keyfind", 3) => ([Term.I(1), Term.I(1), Term.List(Term.Tuple(new FloatTerm(1), Term.A("found")))], Term.Tuple(new FloatTerm(1), Term.A("found"))),
                ("lists", "keymember", 3) => ([Term.A("a"), Term.I(1), Term.List(Term.Tuple(Term.A("a")))], Term.A("true")),
                ("lists", "keysearch", 3) => ([Term.A("a"), Term.I(1), Term.List(Term.Tuple(Term.A("a")))], Term.Tuple(Term.A("value"), Term.Tuple(Term.A("a")))),
                ("lists", "nth", 2) => ([Term.I(2), Term.List(Term.A("a"), Term.A("b"))], Term.A("b")),
                ("lists", "nthtail", 2) => ([Term.I(1), new Cons(Term.A("a"), Term.A("tail"))], Term.A("tail")),
                ("lists", "last", 1) => ([Term.List(Term.I(1), Term.A("last"))], Term.A("last")),
                ("lists", "split", 2) => ([Term.I(1), Term.List(Term.A("a"), Term.A("b"))], Term.Tuple(Term.List(Term.A("a")), Term.List(Term.A("b")))),
                ("lists", "seq", 2) => ([Term.I(1), Term.I(3)], Term.List(Term.I(1), Term.I(2), Term.I(3))),
                ("lists", "seq", 3) => ([Term.I(5), Term.I(1), Term.I(-2)], Term.List(Term.I(5), Term.I(3), Term.I(1))),
                ("lists", "reverse", 1) => ([Term.List(Term.I(1), Term.I(2))], Term.List(Term.I(2), Term.I(1))),
                ("lists", "reverse", 2) => ([Term.List(Term.I(1), Term.I(2)), Term.A("tail")], new Cons(Term.I(2), new Cons(Term.I(1), Term.A("tail")))),
                ("lists", "append", 2) => ([Term.List(Term.I(1)), Term.A("tail")], new Cons(Term.I(1), Term.A("tail"))),
                ("lists", "append", 1) => ([Term.List(Term.List(Term.I(1)), Term.A("tail"))], new Cons(Term.I(1), Term.A("tail"))),
                ("lists", "duplicate", 2) => ([Term.I(2), Term.A("a")], Term.List(Term.A("a"), Term.A("a"))),
                ("lists", "flatten", 1) => ([Term.List(Term.List(Term.A("a")), Term.A("b"))], Term.List(Term.A("a"), Term.A("b"))),
                ("lists", "flatten", 2) => ([Term.List(Term.List(Term.A("a"))), new Cons(Term.A("b"), Term.A("tail"))], new Cons(Term.A("a"), new Cons(Term.A("b"), Term.A("tail")))),
                ("lists", "member", 2) => ([Term.I(1), Term.List(new FloatTerm(1))], Term.A("false")),
                ("lists", "sum", 1) => ([Term.List(Term.I(1), new FloatTerm(2.5))], new FloatTerm(3.5)),
                ("maps", "get", 2) => ([Term.A("k"), new MapTerm([new(Term.A("k"), Term.I(42))])], Term.I(42)),
                ("maps", "size", 1) => ([new MapTerm([new(Term.A("k"), Term.I(42))])], Term.I(1)),
                _ => null
            };
            if (pure is { } test)
            {
                Equal(await Call(test.Args), test.Expected);

                return Term.A("ok");
            }
            switch (export.Function)
            {
                case "self":
                    Equal(await Call(), c.Self);
                    break;
                case "make_ref":
                    var first = await Call();
                    var second = await Call();
                    Check(first is ReferenceTerm && !first.Equals(second));
                    break;
                case "register":
                    Equal(await Call(Term.A("name"), c.Self), Term.A("true"));
                    Equal(runtime.WhereIs("name"), c.Self);
                    break;
                case "whereis":
                    runtime.Register("name", c.Self);
                    Equal(await Call(Term.A("name")), c.Self);
                    Equal(await Call(Term.A("absent")), Term.A("undefined"));
                    break;
                case "unregister":
                    runtime.Register("name", c.Self);
                    Equal(await Call(Term.A("name")), Term.A("true"));
                    Equal(runtime.WhereIs("name"), Term.A("undefined"));
                    break;
                case "link":
                    c.TrapExits = true;
                    Equal(await Call(new Pid(runtime.Node, 999999)), Term.A("true"));
                    var exit = await c.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.A("EXIT")) ? x : null, TimeSpan.Zero);
                    Check(exit is not null && exit.Items[2].Equals(Term.A("noproc")));
                    break;
                case "unlink":
                    var linked = runtime.Spawn(async x =>
 {
     await x.ReceiveAsync(t => t);

     return Term.A("ok");
 }, c, true);
                    Equal(await Call(linked.Pid), Term.A("true"));
                    runtime.Exit(c, linked.Pid, Term.A("boom"));
                    Equal(await linked.Completion, Term.A("boom"));
                    Check(runtime.IsAlive(c.Self));
                    break;
                case "monitor":
                    var reference = await Call(Term.A("process"), new Pid(runtime.Node, 999999));
                    Check(reference is ReferenceTerm);
                    var down = await c.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.A("DOWN")) ? x : null, TimeSpan.Zero);
                    Check(down is not null && down.Items[1].Equals(reference) && down.Items[4].Equals(Term.A("noproc")));
                    break;
                case "demonitor":
                    var monitored = runtime.Spawn(async x =>
 {
     await x.ReceiveAsync(t => t);

     return Term.A("ok");
 });
                    var monitor = runtime.Monitor(c, monitored.Pid);
                    Equal(await Call(monitor), Term.A("true"));
                    runtime.Exit(c, monitored.Pid, Term.A("kill"));
                    await monitored.Completion;
                    Check(c.Mailbox.Count == 0);
                    break;
                case "process_flag":
                    Equal(await Call(Term.A("trap_exit"), Term.A("true")), Term.A("false"));
                    Check(c.TrapExits);
                    break;
                case "exit" when export.Arity == 2:
                    var target = runtime.Spawn(async x =>
 {
     await x.ReceiveAsync(t => t);

     return Term.A("ok");
 });
                    Equal(await Call(target.Pid, Term.A("kill")), Term.A("true"));
                    Equal(await target.Completion, Term.A("killed"));
                    break;
                case "exit":
                case "error":
                case "throw":
                    try
                    {
                        await Call(Term.A("reason"));
                        throw new InvalidOperationException("Expected exception");
                    }
                    catch (ErlangException ex)
                    {
                        Equal(ex.Reason, Term.A("reason"));
                        Check(ex.ExceptionClass == export.Function);
                    }
                    break;
                case "put":
                    Equal(await Call(Term.A("k"), Term.I(1)), Term.A("undefined"));
                    Equal(await Call(Term.A("k"), Term.I(2)), Term.I(1));
                    break;
                case "get":
                    c.Dictionary[Term.A("k")] = Term.I(42);
                    Equal(await Call(Term.A("k")), Term.I(42));
                    Equal(await Call(Term.A("missing")), Term.A("undefined"));
                    break;
                case "erase":
                    c.Dictionary[Term.A("k")] = Term.I(42);
                    Equal(await Call(Term.A("k")), Term.I(42));
                    Check(!c.Dictionary.ContainsKey(Term.A("k")));
                    break;
                case "spawn":
                case "spawn_link":
                    var selfObserved = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var fun = new FunctionTerm(
                        0,
                        (execution, a) =>
 {
     selfObserved.SetResult(((ProcessContext)execution).Self);

     return ValueTask.FromResult<Term>(Term.A("ok"));
 }
                    );
                    var spawned = await Call(fun);
                    Equal(await selfObserved.Task, spawned);
                    Check(!spawned.Equals(c.Self));
                    break;
                case "spawn_monitor":
                    var instant = new FunctionTerm(0, (execution, a) => ValueTask.FromResult<Term>(Term.A("ok")));
                    var pair = (TupleTerm)await Call(instant);
                    Check(pair.Items[0] is Pid && pair.Items[1] is ReferenceTerm);
                    var notification = await c.ReceiveAsync(t => t is TupleTerm x && x.Items.Count == 5 && x.Items[1].Equals(pair.Items[1]) ? x : null, TimeSpan.FromSeconds(1));
                    Check(notification is not null && notification.Items[4].Equals(Term.A("normal")));
                    break;
                case "map":
                    var doubleFun = new FunctionTerm(1, (execution, a) => ValueTask.FromResult(CoreModules.Arithmetic("*", a[0], Term.I(2))));
                    Equal(await Call(doubleFun, Term.List(Term.I(1), Term.I(2))), Term.List(Term.I(2), Term.I(4)));
                    break;
                case "format":
                    Equal(await Call(Term.String("~s ~p~~ ~n"), Term.List(Term.String("hi"), Term.I(42))), Term.A("ok"));
                    Check(output.ToString() == "hi 42~ \n");
                    break;
                default:
                    throw new InvalidOperationException("Missing direct MFA test for " + export);
            }

            return Term.A("ok");
        });
        Equal(await process.Completion.WaitAsync(TimeSpan.FromSeconds(3)), Term.A("normal"));
    }
    );


async Task<Term> MapError(string source)
{
    await using var runtime = new ProcessRuntime();
    Term? reason = null;
    var expression = new Parser(source).ParseExpression();
    Semantics.Validate(expression);
    var process = runtime.Spawn(async c =>
    {
        try
        {
            await Execution.EvaluateAsync(expression, c);
            throw new InvalidOperationException("Expected map error");
        }
        catch (ErlangException ex)
        {
            Check(ex.ExceptionClass == "error");
            reason = ex.Reason;
        }

        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal"));

    return reason!;
}
Test(
    "compiler/map-empty-and-nonmap-pattern",
    async () => Equal(await Eval("case #{a => 1} of #{} -> case a of #{} -> wrong; _ -> ok end end"), Term.A("ok"))
);
Test("compiler/map-duplicate-last-wins", async () => Equal(await Eval("maps:get(a, #{a => 1, a => 42})"), Term.I(42)));
Test(
    "compiler/map-exact-numeric-keys",
    async () => Equal(
        await Eval("case #{1 => integer, 1.0 => float, 0.0 => positive, -0.0 => negative} of #{1 := A, 1.0 := B, 0.0 := C, -0.0 := D} -> {A,B,C,D} end"),
        Term.Tuple(
            Term.A("integer"),
            Term.A("float"),
            Term.A("positive"),
            Term.A("negative")
        )
    )
);
Test(
    "compiler/map-assoc-and-exact-update",
    async () => Equal(
        await Eval("case #{a => 1} of M -> N = M#{a := 2, b => 42}, {maps:get(a,M),maps:get(a,N),maps:get(b,N)} end"),
        Term.Tuple(Term.I(1), Term.I(2), Term.I(42))
    )
);
Test("compiler/map-mixed-duplicate-update", async () => Equal(await Eval("maps:get(a, #{}#{a => 1, a := 2, a => 3})"), Term.I(3)));
Test("compiler/map-chained-update", async () => Equal(await Eval("maps:get(a, #{}#{a => 1}#{a := 42})"), Term.I(42)));
Test(
    "compiler/map-badkey",
    async () => Equal(await MapError("#{}#{missing := 1}"), Term.Tuple(Term.A("badkey"), Term.A("missing")))
);
Test(
    "compiler/map-exact-before-assoc-fails",
    async () => Equal(await MapError("#{}#{a := 1, a => 2}"), Term.Tuple(Term.A("badkey"), Term.A("a")))
);
Test(
    "compiler/map-badmap-empty-update",
    async () => Equal(await MapError("atom#{}"), Term.Tuple(Term.A("badmap"), Term.A("atom")))
);
Test(
    "compiler/map-error-expression-before-badmap",
    async () => Equal(await MapError("atom#{a := error(blurf)}"), Term.A("blurf"))
);
Test("compiler/map-error-expression-before-badkey", async () => Equal(await MapError("#{}#{a := error(blurf)}"), Term.A("blurf")));
Test(
    "compiler/map-subset-nested-repeat",
    async () => Equal(await Eval("case #{a => {7,7}, extra => ok} of #{a := {X,X}} -> X; _ -> no end"), Term.I(7))
);
Test(
    "compiler/map-repeated-variable-reject",
    async () => Equal(await Eval("case #{a => 1,b => 1.0} of #{a := X,b := X} -> wrong; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/map-duplicate-pattern-keys",
    async () => Equal(await Eval("case #{a => 42} of #{a := X,a := Y} -> {X,Y} end"), Term.Tuple(Term.I(42), Term.I(42)))
);
Test(
    "compiler/map-bound-key-and-guard-expression",
    async () => Equal(await Eval("case 1 of K -> case #{2 => 42} of #{K + 1 := V} -> V end end"), Term.I(42))
);
Test(
    "compiler/map-structured-key",
    async () => Equal(await Eval("case #{{a,[1]} => 42} of #{{a,[1]} := X} -> X end"), Term.I(42))
);
Test(
    "compiler/map-guard-key-error-rejects",
    async () => Equal(await Eval("case #{a => 1} of #{hd(atom) := X} -> wrong; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/map-construction-in-guard",
    async () => Equal(await Eval("case ok of X when #{a => 1} =:= #{a => 1} -> X end"), Term.A("ok"))
);
Test(
    "compiler/map-update-guard-error-rejects",
    async () => Equal(await Eval("case #{} of M when M#{a := 1} =:= #{} -> wrong; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/map-unbound-key-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("-module(m). -export([f/2]). f(K, #{K := V}) -> V.").ParseModule());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/map-sibling-binding-key-diagnostic",
    () =>
 {
     Throws<CompileException>(() => Semantics.Validate(new Parser("case #{a => b,b => 1} of #{a := K,K := V} -> V end").ParseExpression()));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/map-assoc-pattern-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("case #{} of #{a => X} -> X end").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/map-exact-construction-diagnostic",
    () =>
 {
     Throws<CompileException>(() => Semantics.Validate(new Parser("#{a := 1}").ParseExpression()));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/map-illegal-key-call-diagnostic",
    () =>
 {
     Throws<CompileException>(() => Semantics.Validate(new Parser("case #{} of #{put(k,1) := X} -> X end").ParseExpression()));

     return Task.CompletedTask;
 }
);
Test(
    "patterns/map-rollback",
    () =>
 {
     var p = Parser.ToPattern(new Parser("#{a := X,b := X}").ParseExpression());
     var b = new Dictionary<string, Term>();
     Check(!p.Match(new MapTerm([new(Term.A("a"), Term.I(1)), new(Term.A("b"), Term.I(2))]), b));
     Check(b.Count == 0);

     return Task.CompletedTask;
 }
);
Test(
    "compiler/map-receive-preserves-unmatched",
    async () =>
{
    await using var runtime = new ProcessRuntime();
    Term? value = null;
    var expression = new Parser("receive #{a := X,b := X} -> X after 1000 -> timeout end").ParseExpression();
    Semantics.Validate(expression);
    var p = runtime.Spawn(async c =>
 {
     value = await Execution.EvaluateAsync(expression, c);
     Check(c.Mailbox.Count == 1);

     return Term.A("ok");
 });
    runtime.Send(p.Pid, new MapTerm([new(Term.A("a"), Term.I(1)), new(Term.A("b"), Term.I(2))]));
    runtime.Send(p.Pid, new MapTerm([new(Term.A("a"), Term.I(42)), new(Term.A("b"), Term.I(42))]));
    Equal(await p.Completion, Term.A("normal"));
    Equal(value!, Term.I(42));
}
);
Test(
    "hybrid/map-case-directive-trivia",
    () =>
 {
     var generated = CodeGeneration.Preprocess("class C { async Task F(ProcessContext erlangProcess) { var x = case #{a => 1} of #{a := X} -> X end. } }", "m.cs");
     Check(generated.Contains("Expr.Map"));
     Check(generated.Contains("MapPatternField"));

     return Task.CompletedTask;
 }
);

Test(
    "compiler/map-closure-captured-key",
    async () => Equal(await Eval("case a of K -> F = fun(#{K := V}) -> V end, F(#{a => 42}) end"), Term.I(42))
);
Test(
    "compiler/map-closure-key-value-shadow",
    async () => Equal(await Eval("case a of K -> F = fun(#{K := K}) -> K end, F(#{a => 42}) end"), Term.I(42))
);
Test(
    "compiler/map-context-guard-key",
    async () => Equal(await Eval("case #{self() => 42} of #{self() := V} -> V end"), Term.I(42))
);
Test(
    "hybrid/map-inline-receive",
    () =>
 {
     var generated = CodeGeneration.Preprocess("class C { async Task F(ProcessContext erlangProcess) { var x = receive #{a := X} -> X end. } }", "m.cs");
     Check(generated.Contains("MapPatternField"));

     return Task.CompletedTask;
 }
);
Test(
    "hybrid/map-case-remote-call",
    () =>
 {
     var generated = CodeGeneration.Preprocess("class C { async Task F(ProcessContext erlangProcess) { var x = case maps:get(a,#{a => 42}) of X -> X end. } }", "m.cs");
     Check(generated.Contains("Expr.Case"));
     Check(generated.Contains("Expr.Map"));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/map-bifs-positive-negative-types",
    async () => Equal(
        await Eval("{is_map(#{}),is_map([]),map_size(#{a => 1,b => 2}),is_map_key(a,#{a => 1}),is_map_key(b,#{a => 1})}"),
        Term.Tuple(
            Term.A("true"),
            Term.A("false"),
            Term.I(2),
            Term.A("true"),
            Term.A("false")
        )
    )
);
Test(
    "compiler/map-get-exact-key-badkey",
    async () => Equal(await MapError("map_get(1.0,#{1 => value})"), Term.Tuple(Term.A("badkey"), new FloatTerm(1)))
);
Test(
    "compiler/map-get-badmap",
    async () => Equal(await MapError("map_get(key,not_map)"), Term.Tuple(Term.A("badmap"), Term.A("not_map")))
);
Test("compiler/map-size-badmap", async () => Equal(await MapError("map_size([])"), Term.Tuple(Term.A("badmap"), Nil.Value)));
Test(
    "compiler/is-map-key-badmap",
    async () => Equal(await MapError("is_map_key(key,42)"), Term.Tuple(Term.A("badmap"), Term.I(42)))
);
Test(
    "compiler/map-key-signed-zero",
    async () => Equal(
        await Eval("{is_map_key(-0.0,#{0.0 => a}),is_map_key(0.0,#{0.0 => a}),map_get(-0.0,#{-0.0 => b})}"),
        Term.Tuple(Term.A("false"), Term.A("true"), Term.A("b"))
    )
);
Test(
    "compiler/map-guard-bifs-qualified",
    async () => Equal(
        await Eval("case #{a => 42} of M when erlang:is_map(M), erlang:map_size(M) =:= 1, erlang:is_map_key(a,M), erlang:map_get(a,M) =:= 42 -> ok; _ -> no end"),
        Term.A("ok")
    )
);
Test(
    "compiler/map-guard-missing-key-alternative",
    async () => Equal(await Eval("case #{} of M when map_get(a,M) =:= 42; map_size(M) =:= 0 -> ok; _ -> no end"), Term.A("ok"))
);
Test(
    "compiler/map-guard-nonmap-rejection",
    async () => Equal(
        await Eval("case atom of M when map_size(M) =:= 0; is_map_key(a,M); map_get(a,M) =:= 1 -> no; _ -> ok end"),
        Term.A("ok")
    )
);
Test(
    "compiler/map-pattern-key-map-get",
    async () => Equal(await Eval("case #{a => key} of Keys -> case #{key => 42} of #{map_get(a,Keys) := Value} -> Value end end"), Term.I(42))
);
Test(
    "compiler/map-pattern-key-map-get-failure",
    async () => Equal(await Eval("case #{} of Keys -> case #{key => 42} of #{map_get(a,Keys) := Value} -> no; _ -> ok end end"), Term.A("ok"))
);
Test(
    "compiler/maps-module-not-guard-legal",
    () =>
 {
     Throws<CompileException>(() => Semantics.Validate(new Parser("case #{} of M when maps:get(a,M) =:= 42 -> ok end").ParseExpression()));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-empty-default-bytes",
    async () =>
 {
     Equal(await Eval("<<>>"), new BitString([]));
     Equal(await Eval("<<1,2,255>>"), new BitString([1, 2, 255]));
 }
);
Test("compiler/bits-truncate-negative", async () => Equal(await Eval("<<511,-1,16:4,31:4>>"), new BitString([255, 255, 15])));
Test("compiler/bits-unaligned-concatenation", async () => Equal(await Eval("<<5:3,17:5,3:2>>"), new BitString([177, 192], 10)));
Test(
    "compiler/bits-big-little-native",
    async () =>
 {
     Equal(await Eval("<<4660:16/big>>"), new BitString([18, 52]));
     Equal(await Eval("<<4660:16/little>>"), new BitString([52, 18]));
     Equal(await Eval("<<4660:16/native>>"), new BitString(BitConverter.IsLittleEndian ? [52, 18] : [18, 52]));
 }
);
Test("compiler/bits-little-partial-octet", async () => Equal(await Eval("<<291:12/little>>"), new BitString([35, 16], 12)));
Test("compiler/bits-explicit-unit", async () => Equal(await Eval("<<4660:2/unit:8>>"), new BitString([18, 52])));
Test("compiler/bits-string-literal", async () => Equal(await Eval("<<\"abc\">>"), new BitString([97, 98, 99])));
Test(
    "compiler/bits-bound-expression-size",
    async () => Equal(await Eval("case 4 of S -> <<(2+3):(S+1)>> end"), new BitString([40], 5))
);
Test("compiler/bits-binary-prefix", async () => Equal(await Eval("<<(<<1,2,3>>):2/binary>>"), new BitString([1, 2])));
Test(
    "compiler/bits-bitstring-interpolation",
    async () => Equal(await Eval("<<1:1,(<<2:2>>)/bitstring,3:2>>"), new BitString([216], 5))
);
Test("compiler/bits-binary-unit-one", async () => Equal(await Eval("<<(<<1:1>>)/binary-unit:1>>"), new BitString([128], 1)));
Test("compiler/bits-zero-size", async () => Equal(await Eval("<<-123:0,42:8>>"), new BitString([42])));
Test("compiler/bits-bad-value", async () => Equal(await MapError("<<atom>>"), Term.A("badarg")));
Test("compiler/bits-negative-size", async () => Equal(await MapError("<<1:(-1)>>"), Term.A("badarg")));
Test(
    "compiler/bits-size-prefix-requires-parentheses",
    async () =>
{
    foreach (string source in new[] { "<<1:-1>>", "<<1:+8>>", "<<1:bnot 1>>", "<<1:not true>>" })
        Throws<CompileException>(() => new Parser(source).ParseExpression());
    Equal(await Eval("<<1:(+8)>>"), new BitString([1]));
    Equal(await Eval("<<-1>>"), new BitString([255]));
}
);
Test("compiler/bits-float-size", async () => Equal(await MapError("<<1:1.0>>"), Term.A("badarg")));
Test("compiler/bits-short-binary", async () => Equal(await MapError("<<(<<1>>):2/binary>>"), Term.A("badarg")));
Test("compiler/bits-binary-unit-alignment", async () => Equal(await MapError("<<(<<1:1>>)/binary>>"), Term.A("badarg")));
Test(
    "compiler/bits-invalid-unit-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("<<1:8/unit:0>>").ParseExpression());
     Throws<CompileException>(() => new Parser("<<1:8/unit:257>>").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-utf-size-unit-diagnostic",
    () =>
 {
     foreach (string type in new[] { "utf8", "utf16", "utf32" })
         foreach (string source in new[] { "<<65:8/" + type + ">>", "<<65/" + type + "-unit:1>>" })
             Throws<CompileException>(() => new Parser(source).ParseExpression());

     return Task.CompletedTask;
 }
);
Test("compiler/bits-float16-vector", async () => Equal(await Eval("<<1.5:16/float>>"), new BitString([62, 0])));
Test("compiler/bits-float32-vector", async () => Equal(await Eval("<<1.5:32/float>>"), new BitString([63, 192, 0, 0])));
Test("compiler/bits-float64-default", async () => Equal(await Eval("<<1.5/float>>"), new BitString([63, 248, 0, 0, 0, 0, 0, 0])));
Test(
    "compiler/bits-float-little-native",
    async () =>
 {
     Equal(await Eval("<<1.5:16/float-little,1.5:32/float-little>>"), new BitString([0, 62, 0, 0, 192, 63]));
     Equal(await Eval("<<1.5:16/float-native>>"), new BitString(BitConverter.IsLittleEndian ? [0, 62] : [62, 0]));
 }
);
Test(
    "compiler/bits-float-integer-coercion",
    async () => Equal(await Eval("<<1:16/float,2:32/float>>"), new BitString([60, 0, 64, 0, 0, 0]))
);
Test("compiler/bits-float-unit", async () => Equal(await Eval("<<1.5:2/float-unit:8>>"), new BitString([62, 0])));
Test(
    "compiler/bits-float-bad-size",
    async () =>
 {
     foreach (string source in new[] { "<<1.0:0/float>>", "<<1.0:8/float>>", "<<1.0:128/float>>" })
         Equal(await MapError(source), Term.A("badarg"));
 }
);
Test(
    "compiler/bits-float-bad-value",
    async () =>
 {
     Equal(await MapError("<<atom/float>>"), Term.A("badarg"));
     Equal(
         await MapError("<<" + (BigInteger.One << 2000).ToString(System.Globalization.CultureInfo.InvariantCulture) + "/float>>"),
         Term.A("badarg")
     );
 }
);
Test(
    "compiler/bits-float16-round-once",
    async () =>
 {
     Equal(await Eval("<<1.00048828125:16/float>>"), new BitString([60, 0]));
     Equal(await Eval("<<1.000488282181322574615478515625:16/float>>"), new BitString([60, 1]));
     Equal(await Eval("<<1.00048840045928955078125:16/float>>"), new BitString([60, 1]));
 }
);
Test(
    "compiler/bits-float16-subnormal-vectors",
    async () =>
 {
     Equal(await Eval("<<3.039836883544921875e-6:16/float>>"), new BitString([0, 51]));
     Equal(await Eval("<<2.98023223876953125e-7:16/float>>"), new BitString([0, 5]));
     Equal(await Eval("<<3.0517578125e-5:16/float>>"), new BitString([2, 0]));
 }
);
Test(
    "compiler/bits-float-narrow-overflow",
    async () =>
 {
     Equal(await Eval("<<1000000000:16/float>>"), new BitString([124, 0]));
     Equal(await Eval("<<1.0e100:32/float>>"), new BitString([127, 128, 0, 0]));
 }
);
Test(
    "compiler/bits-float-pattern16-rounded",
    async () => Equal(await Eval("case <<0.1:16/float>> of <<F:16/float>> -> F end"), new FloatTerm(0.0999755859375))
);
Test(
    "compiler/bits-float-integer-rounding",
    async () =>
{
    Equal(
        await Eval("case <<9007199254740995/float,-9007199254740995/float,9007199254740993/float>> of <<A/float,B/float,C/float>> -> {A,B,C} end"),
        Term.Tuple(new FloatTerm(9007199254740996.0), new FloatTerm(-9007199254740996.0), new FloatTerm(9007199254740992.0))
    );
    Equal(await Eval("case <<9007199254740995/float>> of <<9007199254740995/float>> -> ok; _ -> no end"), Term.A("ok"));
    BigInteger anchor = BigInteger.One << 100;
    Equal(
        await Eval("case <<" + (anchor + (BigInteger.One << 47) + 1) + "/float>> of <<F/float>> -> F end"),
        new FloatTerm(Math.ScaleB(1.0, 100) + Math.ScaleB(1.0, 48))
    );
}
);
Test(
    "compiler/bits-float-integer-max-boundary",
    async () =>
{
    BigInteger max = (BigInteger.One << 1024) - (BigInteger.One << 971), halfway = max + (BigInteger.One << 970);
    Equal(await Eval("case <<" + (halfway - 1) + "/float>> of <<F/float>> -> F end"), new FloatTerm(double.MaxValue));
    Equal(await MapError("<<" + halfway + "/float>>"), Term.A("badarg"));
}
);
Test(
    "compiler/bits-float-pattern32",
    async () => Equal(await Eval("case <<1.5:32/float>> of <<F:32/float>> -> F end"), new FloatTerm(1.5))
);
Test(
    "compiler/bits-float-pattern64-default",
    async () => Equal(await Eval("case <<1.5/float>> of <<F/float>> -> F end"), new FloatTerm(1.5))
);
Test(
    "compiler/bits-float-pattern-unaligned",
    async () => Equal(
        await Eval("case <<1:1,1.5:16/float-little,5:3>> of <<_:1,F:16/float-little,T:3>> -> {F,T} end"),
        Term.Tuple(new FloatTerm(1.5), Term.I(5))
    )
);
Test("compiler/bits-float-pattern-zero", async () => Equal(await Eval("case <<>> of <<F:0/float>> -> F end"), new FloatTerm(0.0)));
Test(
    "compiler/bits-float-pattern-nonfinite-reject",
    async () =>
 {
     foreach (string bits in new[] { "<<31744:16>>", "<<32256:16>>", "<<2139095040:32>>", "<<2143289344:32>>", "<<9218868437227405312:64>>", "<<9221120237041090560:64>>" })
     {
         int size = bits.Contains(":16") ? 16 : bits.Contains(":32") ? 32 : 64;
         Equal(await Eval("case " + bits + " of <<F:" + size + "/float>> -> wrong; _ -> ok end"), Term.A("ok"));
     }
 }
);
Test(
    "compiler/bits-float-pattern-invalid-short",
    async () =>
 {
     Equal(await Eval("case <<0:8>> of <<F:8/float>> -> wrong; _ -> ok end"), Term.A("ok"));
     Equal(await Eval("case <<0:8>> of <<F:16/float>> -> wrong; _ -> ok end"), Term.A("ok"));
 }
);
Test(
    "compiler/bits-float-pattern-numeric-literal",
    async () => Equal(await Eval("case <<1:32/float>> of <<1:32/float>> -> ok; _ -> no end"), Term.A("ok"))
);
Test(
    "compiler/bits-float-pattern-bound-integer",
    async () => Equal(await Eval("case 1 of X -> case <<1:32/float>> of <<X:32/float>> -> wrong; _ -> ok end end"), Term.A("ok"))
);
Test(
    "compiler/bits-float-signed-zero",
    async () =>
 {
     Equal(await Eval("<<-0.0:16/float>>"), new BitString([128, 0]));
     Equal(await Eval("case <<-0.0:32/float>> of <<F:32/float>> -> F end"), new FloatTerm(-0.0));
     Equal(await Eval("case <<-0.0:16/float>> of <<0.0:16/float>> -> wrong; <<-0.0:16/float>> -> ok end"), Term.A("ok"));
 }
);
Test(
    "compiler/bits-float-pattern-prior-width",
    async () => Equal(await Eval("case <<16,1.5:16/float>> of <<N,F:N/float>> -> F end"), new FloatTerm(1.5))
);
Test(
    "compiler/bits-float-guard-and-map-key",
    async () => Equal(
        await Eval("case #{<<1.5:16/float>> => 42} of #{<<1.5:16/float>> := X} when <<1:16/float>> =:= <<1.0:16/float>> -> X end"),
        Term.I(42)
    )
);
Test(
    "compiler/bits-float-unit-without-size-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("<<1.0/float-unit:8>>").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-float16-all-finite-roundtrip",
    async () =>
{
    var pattern = Parser.ToPattern(new Parser("<<F:16/float>>").ParseExpression());
    await using var runtime = new ProcessRuntime();
    var process = runtime.Spawn(async ctx =>
    {
        for (int bits = 0; bits <= ushort.MaxValue; bits++)
        {
            if ((bits & 0x7c00) == 0x7c00)
                continue;
            var input = new BitString([(byte)(bits >> 8), (byte)bits]);
            var bindings = new Dictionary<string, Term>();
            Check(pattern.Match(input, bindings, ctx));
            var expression = new Expr.Bits([new BitSegment(new Expr.Literal(bindings["F"]), new Expr.Literal(Term.I(16)), "float")]);
            Equal(await Execution.EvaluateAsync(expression, ctx), input);
        }

        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal"));
}
);
Test(
    "compiler/bits-duplicate-spec-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("<<1:8/big-little>>").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-utf8-boundary-vectors",
    async () =>
{
    (int Code, byte[] Bytes)[] vectors = [(0, [0]), (127, [127]), (128, [194, 128]), (2047, [223, 191]), (2048, [224, 160, 128]), (55295, [237, 159, 191]), (57344, [238, 128, 128]), (65535, [239, 191, 191]), (65536, [240, 144, 128, 128]), (1114111, [244, 143, 191, 191])];
    foreach (var vector in vectors)
        Equal(await Eval("<<" + vector.Code + "/utf8>>"), new BitString(vector.Bytes));
}
);
Test(
    "compiler/bits-utf16-surrogate-vector",
    async () =>
 {
     Equal(await Eval("<<128512/utf16>>"), new BitString([216, 61, 222, 0]));
     Equal(await Eval("<<128512/utf16-little>>"), new BitString([61, 216, 0, 222]));
 }
);
Test(
    "compiler/bits-utf32-vector",
    async () =>
 {
     Equal(await Eval("<<128512/utf32>>"), new BitString([0, 1, 246, 0]));
     Equal(await Eval("<<128512/utf32-little>>"), new BitString([0, 246, 1, 0]));
 }
);
Test(
    "compiler/bits-utf-endian-native",
    async () =>
 {
     foreach (string type in new[] { "utf16", "utf32" })
         Equal(
             await Eval("<<128512/" + type + "-native>>"),
             await Eval("<<128512/" + type + (BitConverter.IsLittleEndian ? "-little" : "-big") + ">>")
         );
     Equal(await Eval("<<128512/utf8-little>>"), await Eval("<<128512/utf8-big>>"));
 }
);
Test(
    "compiler/bits-utf-invalid-values",
    async () =>
 {
     foreach (string type in new[] { "utf8", "utf16", "utf32" })
         foreach (string value in new[] { "-1", "55296", "57343", "1114112", "9007199254740993", "1.0", "atom", "[65]" })
             Equal(await MapError("<<(" + value + ")/" + type + ">>"), Term.A("badarg"));
 }
);
Test(
    "compiler/bits-utf-undefined-size-diagnostic",
    () =>
 {
     foreach (string type in new[] { "utf8", "utf16", "utf32" })
         foreach (string source in new[] { "<<65:undefined/" + type + ">>", "case <<65>> of <<X:undefined/" + type + ">> -> X end" })
             Throws<CompileException>(() => new Parser(source).ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-utf-noncharacters",
    async () =>
 {
     foreach (string type in new[] { "utf8", "utf16", "utf32" })
         Equal(
             await Eval("case <<65534/" + type + ",65535/" + type + ">> of <<A/" + type + ",B/" + type + ">> -> {A,B} end"),
             Term.Tuple(Term.I(65534), Term.I(65535))
         );
 }
);
Test(
    "compiler/bits-utf8-prefix-rest",
    async () => Equal(
        await Eval("case <<240,159,152,128,42>> of <<X/utf8,Rest/binary>> -> {X,Rest} end"),
        Term.Tuple(Term.I(128512), new BitString([42]))
    )
);
Test(
    "compiler/bits-utf16-pattern-pair",
    async () => Equal(await Eval("case <<216,61,222,0>> of <<X/utf16>> -> X end"), Term.I(128512))
);
Test(
    "compiler/bits-utf32-pattern",
    async () => Equal(await Eval("case <<0,246,1,0>> of <<X/utf32-little>> -> X end"), Term.I(128512))
);
Test(
    "compiler/bits-utf8-invalid-sequences",
    async () =>
 {
     foreach (string bytes in new[] { "128", "192,175", "193,191", "224,128,128", "237,160,128", "240,128,128,128", "244,144,128,128", "245,128,128,128", "254", "255", "226,130", "194,65" })
         Equal(await Eval("case <<" + bytes + ">> of <<X/utf8,Rest/binary>> -> wrong; _ -> ok end"), Term.A("ok"));
 }
);
Test(
    "compiler/bits-utf16-invalid-sequences",
    async () =>
 {
     foreach (string bytes in new[] { "216,0", "220,0", "216,0,0,65", "220,0,216,0", "0" })
         Equal(await Eval("case <<" + bytes + ">> of <<X/utf16,Rest/binary>> -> wrong; _ -> ok end"), Term.A("ok"));
 }
);
Test(
    "compiler/bits-utf32-invalid-scalars",
    async () =>
 {
     foreach (string value in new[] { "55296", "57343", "1114112", "4294967295" })
         Equal(await Eval("case <<" + value + ":32>> of <<X/utf32>> -> wrong; _ -> ok end"), Term.A("ok"));
     Equal(await Eval("case <<0,0,65>> of <<X/utf32>> -> wrong; _ -> ok end"), Term.A("ok"));
 }
);
Test(
    "compiler/bits-utf-unaligned",
    async () =>
 {
     foreach (string type in new[] { "utf8", "utf16-little", "utf32-big" })
         for (int offset = 1; offset <= 7; offset++)
             Equal(
                 await Eval("case <<1:" + offset + ",128512/" + type + ",5:3>> of <<_:" + offset + ",X/" + type + ",T:3>> -> {X,T} end"),
                 Term.Tuple(Term.I(128512), Term.I(5))
             );
 }
);
Test(
    "compiler/bits-utf-truncated-bit-tail",
    async () => Equal(await Eval("case <<240,159,152,64:7>> of <<X/utf8>> -> wrong; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/bits-utf-binding-rollback",
    async () => Equal(await Eval("case <<65,128>> of <<X/utf8,Y/utf8>> -> wrong; <<X:16>> -> X end"), Term.I(16768))
);
Test("compiler/bits-utf-size-binding", async () => Equal(await Eval("case <<3/utf8,5:3>> of <<N/utf8,X:N>> -> X end"), Term.I(5)));
Test(
    "compiler/bits-utf-literal-and-bound",
    async () =>
 {
     Equal(await Eval("case <<128512/utf8>> of <<128512/utf8>> -> ok; _ -> no end"), Term.A("ok"));
     Equal(await Eval("case 128512 of X -> case <<128512/utf16>> of <<X/utf16>> -> X end end"), Term.I(128512));
 }
);
Test(
    "compiler/bits-utf-string-construction",
    async () =>
 {
     Equal(await Eval("<<\"A😀\"/utf8>>"), new BitString([65, 240, 159, 152, 128]));
     Equal(await Eval("<<\"A😀\"/utf16-little>>"), new BitString([65, 0, 61, 216, 0, 222]));
     Equal(await Eval("<<\"\"/utf32>>"), new BitString([]));
 }
);
Test(
    "compiler/bits-utf-string-pattern",
    async () => Equal(await Eval("case <<\"A😀\"/utf8,42>> of <<\"A😀\"/utf8,X>> -> X end"), Term.I(42))
);
Test(
    "compiler/bits-utf-guard-map-key",
    async () => Equal(await Eval("case #{<<128512/utf8>> => 42} of #{<<128512/utf8>> := X} when <<65/utf8>> =:= <<65>> -> X end"), Term.I(42))
);
Test(
    "compiler/bits-utf-deterministic-roundtrips",
    async () =>
{
    await using var runtime = new ProcessRuntime();
    var process = runtime.Spawn(async ctx =>
    {
        var random = new Random(14014);
        foreach (string type in new[] { "utf8", "utf16", "utf16-little", "utf32", "utf32-little" })
        {
            Pattern pattern = Parser.ToPattern(new Parser("<<_:3,X/" + type + ",T:2>>").ParseExpression());
            for (int i = 0; i < 256; i++)
            {
                int scalar = random.Next(0x110000);
                if (scalar is >= 0xd800 and <= 0xdfff)
                {
                    i--;
                    continue;
                }
                var input = await Execution.EvaluateAsync(new Parser("<<5:3," + scalar + "/" + type + ",2:2>>").ParseExpression(), ctx);
                var bindings = new Dictionary<string, Term>();
                Check(pattern.Match(input, bindings, ctx));
                Equal(bindings["X"], Term.I(scalar));
                Equal(bindings["T"], Term.I(2));
            }
        }

        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal"));
}
);
Test("compiler/bits-pattern-default", async () => Equal(await Eval("case <<42>> of <<X>> -> X end"), Term.I(42)));
Test("compiler/bits-pattern-signed", async () => Equal(await Eval("case <<255>> of <<X:8/signed>> -> X end"), Term.I(-1)));
Test("compiler/bits-pattern-unsigned", async () => Equal(await Eval("case <<255>> of <<X:8/unsigned>> -> X end"), Term.I(255)));
Test(
    "compiler/bits-pattern-signed-little",
    async () => Equal(await Eval("case <<-257:16/little>> of <<X:16/signed-little>> -> X end"), Term.I(-257))
);
Test(
    "compiler/bits-pattern-little-partial",
    async () => Equal(await Eval("case <<291:12/little>> of <<X:12/little>> -> X end"), Term.I(291))
);
Test(
    "compiler/bits-pattern-native",
    async () => Equal(await Eval("case <<4660:16/native>> of <<X:16/native>> -> X end"), Term.I(4660))
);
Test(
    "compiler/bits-pattern-prior-size",
    async () => Equal(
        await Eval("case <<3,5:3,2:2>> of <<N, X:N, Rest/bitstring>> -> {X,Rest} end"),
        Term.Tuple(Term.I(5), new BitString([128], 2))
    )
);
Test(
    "compiler/bits-pattern-bound-size-expression",
    async () => Equal(await Eval("case 3 of N -> case <<17:5>> of <<X:(N+2)>> -> X end end"), Term.I(17))
);
Test(
    "compiler/bits-pattern-shadow-size",
    async () => Equal(await Eval("case 8 of L -> F = fun(<<L:L,B:L>>) -> B end, F(<<16:8,7:16>>) end"), Term.I(7))
);
Test(
    "compiler/bits-pattern-shadow-repeat",
    async () => Equal(await Eval("case 8 of L -> F = fun(<<L:L,B:L,L:L>>) -> B; (_) -> no end, F(<<16:8,7:16,16:16>>) end"), Term.I(7))
);
Test(
    "compiler/fun-clause-local-shadow-capture",
    async () => Equal(await Eval("case 42 of X -> F = fun({X}) -> X; (_) -> X end, F(atom) end"), Term.I(42))
);
Test(
    "compiler/fun-guard-fallback-capture",
    async () => Equal(await Eval("case 42 of X -> F = fun(X) when is_integer(X) -> X; (_) -> X end, F(atom) end"), Term.I(42))
);
Test(
    "compiler/fun-bit-clause-fallback-capture",
    async () => Equal(
        await Eval("case {8,42} of {L,B} -> F = fun(<<L:L,B:L>>) -> {L,B}; (_) -> {L,B} end, F(atom) end"),
        Term.Tuple(Term.I(8), Term.I(42))
    )
);
Test(
    "compiler/bits-pattern-repeat-mismatch",
    async () => Equal(await Eval("case <<1,2>> of <<X,X>> -> wrong; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/bits-pattern-bound-value",
    async () => Equal(await Eval("case 42 of X -> case <<42>> of <<X>> -> ok; _ -> no end end"), Term.A("ok"))
);
Test(
    "compiler/bits-pattern-short-extra-nonbits",
    async () =>
 {
     foreach (string source in new[] { "<<1:7>>", "<<1,2>>", "atom" })
         Equal(await Eval("case " + source + " of <<X:8>> -> wrong; _ -> ok end"), Term.A("ok"));
 }
);
Test("compiler/bits-pattern-zero-signed", async () => Equal(await Eval("case <<>> of <<X:0/signed>> -> X end"), Term.I(0)));
Test(
    "compiler/bits-pattern-binary-prefix",
    async () => Equal(await Eval("case <<1,2,3>> of <<B:2/binary,T/binary>> -> {B,T} end"), Term.Tuple(new BitString([1, 2]), new BitString([3])))
);
Test(
    "compiler/bits-pattern-unaligned-prefix",
    async () => Equal(await Eval("case <<1:1,5:3>> of <<_:1,B:3/bitstring>> -> B end"), new BitString([160], 3))
);
Test("compiler/bits-pattern-unit", async () => Equal(await Eval("case <<1,2>> of <<X:2/unit:8>> -> X end"), Term.I(258)));
Test(
    "compiler/bits-pattern-rest-unit-mismatch",
    async () => Equal(await Eval("case <<1:1>> of <<B/binary>> -> wrong; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/bits-pattern-invalid-size-alternative",
    async () =>
 {
     foreach (string size in new[] { "(-1)", "atom", "1.0", "999999999999999999999999", "(hd(atom))" })
         Equal(await Eval("case <<1>> of <<X:" + size + ">> -> wrong; _ -> ok end"), Term.A("ok"));
 }
);
Test(
    "compiler/bits-pattern-string-prefix",
    async () => Equal(await Eval("case <<\"OK\",42>> of <<\"OK\",X>> -> X end"), Term.I(42))
);
Test(
    "compiler/bits-pattern-literal-no-truncation",
    async () => Equal(await Eval("case <<0>> of <<256>> -> wrong; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/bits-pattern-signed-literal",
    async () => Equal(await Eval("case <<255>> of <<-1:8/signed>> -> ok; _ -> no end"), Term.A("ok"))
);
Test("compiler/bits-pattern-empty", async () => Equal(await Eval("case <<>> of <<>> -> ok; _ -> no end"), Term.A("ok")));
Test(
    "compiler/bits-pattern-rollback",
    () =>
 {
     var p = Parser.ToPattern(new Parser("<<N,1>>").ParseExpression());
     var b = new Dictionary<string, Term>();
     Check(!p.Match(new BitString([42, 2]), b));
     Check(b.Count == 0);

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-pattern-forward-size-diagnostic",
    () =>
 {
     Throws<CompileException>(() => Semantics.Validate(new Parser("case <<1>> of <<X:N,N>> -> X end").ParseExpression()));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-pattern-nonlast-rest-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("case <<1>> of <<B/binary,X>> -> X end").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-pattern-size-call-diagnostic",
    () =>
 {
     Throws<CompileException>(() => Semantics.Validate(new Parser("case <<1>> of <<X:(lists:sum([8]))>> -> X end").ParseExpression()));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-pattern-nested-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("case <<1>> of <<(<<X>>)/binary>> -> X end").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-pattern-map-value",
    async () => Equal(await Eval("case #{a => <<3,5:3>>} of #{a := <<N,X:N>>} -> X end"), Term.I(5))
);
Test(
    "compiler/bits-pattern-match-badmatch",
    async () => Equal(await MapError("<<X:16>> = <<1>>"), Term.Tuple(Term.A("badmatch"), new BitString([1])))
);
Test(
    "compiler/bits-pattern-receive-preserves-unmatched",
    async () =>
{
    await using var runtime = new ProcessRuntime();
    var process = runtime.Spawn(async ctx =>
    {
        ctx.Mailbox.Send(Term.A("earlier"));
        ctx.Mailbox.Send(new BitString([2, 42]));
        ctx.Mailbox.Send(new BitString([1, 7]));
        var expression = new Parser("receive <<1,X>> -> X after 0 -> no end").ParseExpression();
        Semantics.Validate(expression);
        Equal(await Execution.EvaluateAsync(expression, ctx), Term.I(7));
        Equal((await ctx.ReceiveAsync(t => t, TimeSpan.Zero))!, Term.A("earlier"));
        Equal((await ctx.ReceiveAsync(t => t, TimeSpan.Zero))!, new BitString([2, 42]));

        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal"));
}
);
Test(
    "compiler/bits-guard-and-map-key",
    async () => Equal(await Eval("case #{<<1,2>> => 42} of #{<<1,2>> := V} when <<1:3>> =:= <<1:3>> -> V end"), Term.I(42))
);
Test(
    "compiler/bits-unit-without-size-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("<<1/unit:8>>").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-string-little-per-codepoint",
    async () => Equal(await Eval("<<\"ab\":16/little>>"), new BitString([97, 0, 98, 0]))
);
Test("compiler/bits-string-unit-per-codepoint", async () => Equal(await Eval("<<\"AB\":2/unit:8>>"), new BitString([0, 65, 0, 66])));
Test("compiler/bits-string-unaligned", async () => Equal(await Eval("<<1:1,\"AB\":4,3:2>>"), new BitString([137, 96], 11)));
Test("compiler/bits-string-unicode-integer-truncation", async () => Equal(await Eval("<<\"Ā😀\"/integer>>"), new BitString([0, 0])));
Test("compiler/bits-string-float-half", async () => Equal(await Eval("<<\"AB\":16/float>>"), new BitString([84, 16, 84, 32])));
Test(
    "compiler/bits-string-float-single-little",
    async () => Equal(await Eval("<<\"A\":32/float-little>>"), new BitString([0, 0, 130, 66]))
);
Test("compiler/bits-string-float-default", async () => Equal(await Eval("<<\"A\"/float>>"), new BitString([64, 80, 64, 0, 0, 0, 0, 0])));
Test(
    "compiler/bits-string-size-evaluated-once",
    async () => Equal(
        await Eval("case ok of ok -> put(counter,0),B = <<\"ab\":(put(counter,get(counter)+1))>>, {B,get(counter)} end"),
        Term.Tuple(new BitString([], 0), Term.I(1))
    )
);
Test(
    "compiler/bits-string-size-binding",
    async () => Equal(await Eval("case ok of ok -> B = <<\"ab\":(S = 8)>>,{B,S} end"), Term.Tuple(new BitString([97, 98]), Term.I(8)))
);
Test(
    "compiler/bits-empty-string-valid-modifiers",
    async () => Equal(await Eval("<<\"\":16/little,\"\":32/float,42>>"), new BitString([42]))
);
Test("compiler/bits-empty-string-size-evaluation", async () => Equal(await MapError("<<\"\":(error(boom))>>"), Term.A("boom")));
Test("compiler/bits-string-zero-width", async () => Equal(await Eval("<<\"abc\":0,42>>"), new BitString([42])));
Test(
    "compiler/bits-string-invalid-sizes",
    async () =>
{
    foreach (string source in new[] { "<<\"ab\":(-1)>>", "<<\"\":(-1)>>", "case all of S -> <<\"\":S>> end", "<<\"ab\":1.0>>", "<<\"\":1/float>>", "<<\"ab\":0/float>>", "<<\"a\"/binary>>", "<<\"\"/binary>>", "<<[65,66]:8>>", "<<[]:8>>" })
        Equal(await MapError(source), Term.A("badarg"));
}
);
Test(
    "compiler/bits-string-modifier-pattern-diagnostic",
    () =>
{
    foreach (string source in new[] { "case <<0,97>> of <<\"a\":16>> -> yes end", "case <<97>> of <<\"a\"/integer>> -> yes end", "case <<>> of <<\"\":0>> -> yes end", "fun(<<\"a\"/float>>) -> ok end" })
    {
        try
        {
            new Parser(source).ParseExpression();
            Check(false);
        }
        catch (CompileException exception)
        {
            Check(exception.Code == "ERL004");
        }
    }

    return Task.CompletedTask;
}
);
Test(
    "compiler/bits-string-guard-and-map-key",
    async () => Equal(
        await Eval("case #{<<\"ab\":16/little>> => 42} of #{<<\"ab\":16/little>> := X} when <<\"A\":16/float>> =:= <<84,16>> -> X end"),
        Term.I(42)
    )
);
Test(
    "compiler/bits-numeric-literal-all-diagnostic",
    () =>
{
    foreach (string source in new[] { "<<\"\":all>>", "<<\"ab\":all>>", "<<1:all>>", "<<1:all/float>>", "<<1.0:all/float>>", "<<\"\":(all)/float>>" })
    {
        try
        {
            new Parser(source).ParseExpression();
            Check(false);
        }
        catch (CompileException exception)
        {
            Check(exception.Code == "ERL002");
        }
    }

    return Task.CompletedTask;
}
);
Test(
    "compiler/bits-identical-specifiers",
    async () => Equal(await Eval("<<1:8/integer-integer-big-big-unit:1-unit:1>>"), new BitString([1]))
);
Test(
    "compiler/bits-alias-specifier-unit-conflict",
    () =>
 {
     Throws<CompileException>(() => new Parser("<<(<<1>>)/bytes-unit:1>>").ParseExpression());
     Throws<CompileException>(() => new Parser("<<(<<1>>)/unit:8-bits>>").ParseExpression());

     return Task.CompletedTask;
 }
);
Test("compiler/bits-explicit-all-binary", async () => Equal(await Eval("<<(<<1,2>>):all/binary>>"), new BitString([1, 2])));
Test(
    "compiler/bit-size-byte-rounding",
    async () => Equal(
        await Eval("{bit_size(<<>>),byte_size(<<>>),bit_size(<<1:1>>),byte_size(<<1:1>>),bit_size(<<1:8>>),byte_size(<<1:8>>),bit_size(<<1:9>>),byte_size(<<1:9>>)}"),
        Term.Tuple(
            Term.I(0),
            Term.I(0),
            Term.I(1),
            Term.I(1),
            Term.I(8),
            Term.I(1),
            Term.I(9),
            Term.I(2)
        )
    )
);
Test(
    "compiler/bitstring-predicate-distinction",
    async () => Equal(
        await Eval("{is_bitstring(<<>>),is_binary(<<>>),is_bitstring(<<1:1>>),is_binary(<<1:1>>),is_bitstring([])}"),
        Term.Tuple(
            Term.A("true"),
            Term.A("true"),
            Term.A("true"),
            Term.A("false"),
            Term.A("false")
        )
    )
);
Test("compiler/bit-size-invalid-badarg", async () => Equal(await MapError("bit_size(atom)"), Term.A("badarg")));
Test("compiler/byte-size-invalid-badarg", async () => Equal(await MapError("byte_size([1,2])"), Term.A("badarg")));
Test(
    "compiler/bit-bifs-qualified-guard",
    async () => Equal(
        await Eval("case <<1:9>> of Bits when erlang:is_bitstring(Bits), erlang:bit_size(Bits) =:= 9, erlang:byte_size(Bits) =:= 2 -> ok; _ -> no end"),
        Term.A("ok")
    )
);
Test(
    "compiler/bit-bif-guard-failure-alternative",
    async () => Equal(await Eval("case atom of X when bit_size(X) =:= 0; byte_size(X) =:= 0; is_atom(X) -> ok end"), Term.A("ok"))
);
Test(
    "compiler/bit-bif-map-pattern-key",
    async () => Equal(await Eval("case <<1:9>> of B -> case #{2 => 42} of #{byte_size(B) := X} -> X end end"), Term.I(42))
);
var results = new List<object>();
foreach (var sample in new (string Name, string Source, Term Expected)[]
{
    ("duplicate-zero", "lists:duplicate(0,anything)", Nil.Value),
    ("duplicate-one", "lists:duplicate(1,a)", Term.List(Term.A("a"))),
    ("duplicate-value", "lists:duplicate(3,{a,1})", Term.List(Term.Tuple(Term.A("a"), Term.I(1)), Term.Tuple(Term.A("a"), Term.I(1)), Term.Tuple(Term.A("a"), Term.I(1)))),
    ("duplicate-improper-element", "lists:duplicate(2,[a|tail])", Term.List(new Cons(Term.A("a"), Term.A("tail")), new Cons(Term.A("a"), Term.A("tail")))),
    ("flatten-empty", "lists:flatten([])", Nil.Value),
    ("flatten-flat", "lists:flatten([a,b,c])", Term.List(Term.A("a"), Term.A("b"), Term.A("c"))),
    ("flatten-nested-empty", "lists:flatten([[],[[],[]],[]])", Nil.Value),
    ("flatten-mixed-opaque-leaves", "lists:flatten([a,[[b],[]],{[c]},<<1>>])", Term.List(
        Term.A("a"),
        Term.A("b"),
        Term.Tuple(Term.List(Term.A("c"))),
        new BitString([1])
    )),
    ("flatten-unicode-list", "lists:flatten([\"A😀\",[66]])", Term.List(Term.I(65), Term.I(128512), Term.I(66))),
    ("flatten-two-empty-keeps-tail", "lists:flatten([],[[]])", Term.List(Nil.Value)),
    ("flatten-two-preserves-nested-tail", "lists:flatten([[a],b],[[c]])", Term.List(Term.A("a"), Term.A("b"), Term.List(Term.A("c")))),
    ("flatten-two-improper-tail", "lists:flatten([[a],b],[c|tail])", new Cons(Term.A("a"), new Cons(Term.A("b"), new Cons(Term.A("c"), Term.A("tail")))))
})
    Test($"lists/{sample.Name}", async () => Equal(await Eval(sample.Source), sample.Expected));

foreach (var sample in new (string Name, string Source)[]
{
    ("duplicate-negative", "lists:duplicate(-1,a)"),
    ("duplicate-big-negative", "lists:duplicate(-999999999999999999999999,a)"),
    ("duplicate-float-count", "lists:duplicate(0.0,a)"),
    ("duplicate-atom-count", "lists:duplicate(atom,a)"),
    ("duplicate-list-count", "lists:duplicate([],a)"),
    ("flatten-nonlist", "lists:flatten(atom)"),
    ("flatten-tuple-input", "lists:flatten({a})"),
    ("flatten-improper-outer", "lists:flatten([a|tail])"),
    ("flatten-improper-nested", "lists:flatten([[a|tail]])"),
    ("flatten-two-invalid-tail", "lists:flatten([],atom)"),
    ("flatten-two-invalid-input", "lists:flatten(atom,[])"),
    ("flatten-two-improper-input", "lists:flatten([a|tail],[])"),
    ("flatten-two-improper-nested", "lists:flatten([[a|tail]],[b|tail])")
})
    Test($"lists/{sample.Name}", async () => Equal(await MapError(sample.Source), Term.A("function_clause")));

Test(
    "lists/duplicate-local-length-limit",
    async () =>
{
    Equal(await MapError("lists:duplicate(2147483648,a)"), Term.A("system_limit"));
    Equal(await MapError("lists:duplicate(999999999999999999999999,a)"), Term.A("system_limit"));
}
);
Test(
    "lists/duplicate-flatten-deep-wide-identity",
    async () =>
{
    await using var runtime = new ProcessRuntime();
    Term marker = Term.Tuple(Term.List(Term.A("marker")));
    Term deep = marker;
    Term wide = Nil.Value;
    for (int i = 0; i < 100000; i++)
    {
        deep = Term.List(deep);
        wide = new Cons(marker, wide);
    }
    Term suffix = new Cons(Term.List(Term.A("tail-value")), Term.A("tail"));
    var process = runtime.Spawn(async context =>
    {
        Term copies = await runtime.Modules.Call(
            context,
            "lists",
            "duplicate",
            [Term.I(100000), marker]
        );
        Check(Cons.Items(copies).Count() == 100000);
        Check(Cons.Items(copies).All(value => ReferenceEquals(value, marker)));
        var flattened = (Cons)await runtime.Modules.Call(
            context,
            "lists",
            "flatten",
            [deep, suffix]
        );
        Check(ReferenceEquals(flattened.Head, marker));
        Check(ReferenceEquals(flattened.Tail, suffix));
        Term flatWide = await runtime.Modules.Call(
            context,
            "lists",
            "flatten",
            [wide]
        );
        Check(Cons.Items(flatWide).Count() == 100000);
        Check(Cons.Items(flatWide).All(value => ReferenceEquals(value, marker)));

        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal"));
}
);
foreach (var sample in new (string Name, string Source, Term Expected)[]
{
    ("append-empty", "lists:append([])", Nil.Value),
    ("append-singleton-atom", "lists:append([tail])", Term.A("tail")),
    ("append-singleton-number", "lists:append([42])", Term.I(42)),
    ("append-singleton-improper", "lists:append([[a|tail]])", new Cons(Term.A("a"), Term.A("tail"))),
    ("append-many", "lists:append([[a,b],[],[c,d]])", Term.List(
        Term.A("a"),
        Term.A("b"),
        Term.A("c"),
        Term.A("d")
    )),
    ("append-arbitrary-final-tail", "lists:append([[a],[b],tail])", new Cons(Term.A("a"), new Cons(Term.A("b"), Term.A("tail")))),
    ("append-empty-prefix-arbitrary-tail", "lists:append([[],42])", Term.I(42)),
    ("append-improper-final-list", "lists:append([[a],[b|tail]])", new Cons(Term.A("a"), new Cons(Term.A("b"), Term.A("tail")))),
    ("member-empty", "lists:member(a,[])", Term.A("false")),
    ("member-found-before-improper", "lists:member(a,[a|tail])", Term.A("true")),
    ("member-found-later-before-improper", "lists:member(b,[a,b|tail])", Term.A("true")),
    ("member-tuple-exact", "lists:member({1},[{1.0}])", Term.A("false")),
    ("member-map-exact-keys", "lists:member(#{1=>a},[#{1.0=>a}])", Term.A("false")),
    ("member-map-equal", "lists:member(#{a=>1,b=>2},[#{b=>2,a=>1}])", Term.A("true")),
    ("member-bits-exact", "lists:member(<<1:1>>,[<<1:2>>])", Term.A("false")),
    ("member-improper-value-equal", "lists:member([a|tail],[[a|tail]])", Term.A("true")),
    ("member-signed-zero", "lists:member(0.0,[-0.0])", Term.A("false"))
})
    Test($"lists/{sample.Name}", async () => Equal(await Eval(sample.Source), sample.Expected));

foreach (var sample in new (string Name, string Source, string Reason)[]
{
    ("append-nonlist", "lists:append(atom)", "function_clause"),
    ("append-improper-outer", "lists:append([[a]|tail])", "function_clause"),
    ("append-invalid-prefix", "lists:append([atom,[]])", "badarg"),
    ("append-improper-prefix", "lists:append([[a|tail],[]])", "badarg"),
    ("append-outer-error-before-prefix", "lists:append([atom|tail])", "function_clause"),
    ("append-outer-error-after-prefix", "lists:append([atom,[a]|tail])", "function_clause"),
    ("member-nonlist", "lists:member(a,atom)", "badarg"),
    ("member-absent-improper", "lists:member(b,[a|tail])", "badarg"),
    ("member-numeric-inexact-improper", "lists:member(1,[1.0|tail])", "badarg")
})
    Test($"lists/{sample.Name}", async () => Equal(await MapError(sample.Source), Term.A(sample.Reason)));

Test(
    "lists/append-long-outer-suffix-identity",
    async () =>
{
    await using var runtime = new ProcessRuntime();
    Term suffix = new Cons(Term.A("marker"), Term.A("tail"));
    Term lists = Term.List(suffix);
    for (int i = 0; i < 100000; i++)
        lists = new Cons(Nil.Value, lists);
    var process = runtime.Spawn(async context =>
    {
        Term result = await runtime.Modules.Call(
            context,
            "lists",
            "append",
            [lists]
        );
        Check(ReferenceEquals(result, suffix));
        var copied = (Cons)await runtime.Modules.Call(
            context,
            "lists",
            "append",
            [Term.List(Term.List(Term.I(1)), suffix)]
        );
        Check(ReferenceEquals(copied.Tail, suffix));

        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal"));
}
);
foreach (var sample in new (string Name, string Source, Term Expected)[]
{
    ("last-singleton", "lists:last([a])", Term.A("a")),
    ("last-nested", "lists:last([a,[],{b,2}])", Term.Tuple(Term.A("b"), Term.I(2))),
    ("split-empty", "lists:split(0,[])", Term.Tuple(Nil.Value, Nil.Value)),
    ("split-zero", "lists:split(0,[a,b])", Term.Tuple(Nil.Value, Term.List(Term.A("a"), Term.A("b")))),
    ("split-middle", "lists:split(1,[a,b,c])", Term.Tuple(Term.List(Term.A("a")), Term.List(Term.A("b"), Term.A("c")))),
    ("split-exact", "lists:split(2,[a,b])", Term.Tuple(Term.List(Term.A("a"), Term.A("b")), Nil.Value)),
    ("split-improper-zero", "lists:split(0,[a|tail])", Term.Tuple(Nil.Value, new Cons(Term.A("a"), Term.A("tail")))),
    ("split-improper-middle", "lists:split(1,[a,b|tail])", Term.Tuple(Term.List(Term.A("a")), new Cons(Term.A("b"), Term.A("tail")))),
    ("split-improper-exact", "lists:split(2,[a,b|tail])", Term.Tuple(Term.List(Term.A("a"), Term.A("b")), Term.A("tail"))),
    ("split-integer-tail", "lists:split(1,[a|42])", Term.Tuple(Term.List(Term.A("a")), Term.I(42)))
})
    Test($"lists/{sample.Name}", async () => Equal(await Eval(sample.Source), sample.Expected));

foreach (var sample in new (string Name, string Source, string Reason)[]
{
    ("last-empty", "lists:last([])", "function_clause"),
    ("last-nonlist", "lists:last(atom)", "function_clause"),
    ("last-improper", "lists:last([a,b|tail])", "function_clause"),
    ("split-negative", "lists:split(-1,[a])", "badarg"),
    ("split-float-count", "lists:split(1.0,[a])", "badarg"),
    ("split-atom-count", "lists:split(atom,[a])", "badarg"),
    ("split-nonlist-zero", "lists:split(0,atom)", "badarg"),
    ("split-empty-overrun", "lists:split(1,[])", "badarg"),
    ("split-proper-overrun", "lists:split(3,[a,b])", "badarg"),
    ("split-improper-overrun", "lists:split(3,[a,b|tail])", "function_clause"),
    ("split-huge-proper", "lists:split(999999999999999999999999,[a])", "badarg"),
    ("split-huge-improper", "lists:split(999999999999999999999999,[a|tail])", "function_clause")
})
    Test($"lists/{sample.Name}", async () => Equal(await MapError(sample.Source), Term.A(sample.Reason)));

Test(
    "lists/last-split-long-list-identity",
    async () =>
{
    await using var runtime = new ProcessRuntime();
    Term marker = Term.Tuple(Term.A("marker"));
    Term suffix = Term.List(marker);
    Term list = suffix;
    for (int i = 0; i < 100000; i++)
        list = new Cons(Term.I(i), list);
    var process = runtime.Spawn(async context =>
    {
        Term last = await runtime.Modules.Call(
            context,
            "lists",
            "last",
            [list]
        );
        Check(ReferenceEquals(last, marker));
        var split = (TupleTerm)await runtime.Modules.Call(
            context,
            "lists",
            "split",
            [Term.I(100000), list]
        );
        Check(ReferenceEquals(split.Items[1], suffix));
        Check(Cons.Items(split.Items[0]).Count() == 100000);
        Check(Cons.Items(list).Count() == 100001);

        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal"));
}
);
Test(
    "lists/nth-improper-prefix",
    async () => Equal(await Eval("{lists:nth(1,[a|tail]),lists:nth(2,[a,b|tail])}"), Term.Tuple(Term.A("a"), Term.A("b")))
);
Test(
    "lists/nth-invalid-function-clause",
    async () =>
{
    foreach (string source in new[] { "lists:nth(0,[a])", "lists:nth(-1,[a])", "lists:nth(1.0,[a])", "lists:nth(2,[a|tail])", "lists:nth(1,[])", "lists:nth(1,atom)", "lists:nth(999999999999999999999999,[a])" })
        Equal(await MapError(source), Term.A("function_clause"));
}
);
Test(
    "lists/nthtail-empty-and-improper",
    async () => Equal(
        await Eval("{lists:nthtail(0,[]),lists:nthtail(0,[a|tail]),lists:nthtail(2,[a,b|tail])}"),
        Term.Tuple(Nil.Value, new Cons(Term.A("a"), Term.A("tail")), Term.A("tail"))
    )
);
Test(
    "lists/nthtail-invalid-function-clause",
    async () =>
{
    foreach (string source in new[] { "lists:nthtail(0,atom)", "lists:nthtail(-1,[a])", "lists:nthtail(1.0,[a])", "lists:nthtail(3,[a,b|tail])", "lists:nthtail(1,[])" })
        Equal(await MapError(source), Term.A("function_clause"));
}
);
Test(
    "lists/seq-default-empty-and-negative",
    async () => Equal(
        await Eval("{lists:seq(2,1),lists:seq(-2,1)}"),
        Term.Tuple(Nil.Value, Term.List(
            Term.I(-2),
            Term.I(-1),
            Term.I(0),
            Term.I(1)
        ))
    )
);
Test(
    "lists/seq-two-arg-error-contract",
    async () =>
{
    foreach (string source in new[] { "lists:seq(3,1)", "lists:seq(1.0,3)", "lists:seq(1,atom)" })
        Equal(await MapError(source), Term.A("function_clause"));
}
);
Test(
    "lists/seq-step-boundaries",
    async () => Equal(
        await Eval("{lists:seq(1,6,2),lists:seq(6,1,-2),lists:seq(3,1,2),lists:seq(3,5,-2)}"),
        Term.Tuple(
            Term.List(Term.I(1), Term.I(3), Term.I(5)),
            Term.List(Term.I(6), Term.I(4), Term.I(2)),
            Nil.Value,
            Nil.Value
        )
    )
);
Test("lists/seq-zero-step", async () => Equal(await Eval("lists:seq(7,7,0)"), Term.List(Term.I(7))));
Test(
    "lists/seq-three-arg-error-contract",
    async () =>
{
    foreach (string source in new[] { "lists:seq(1,2,0)", "lists:seq(4,1,2)", "lists:seq(3,6,-2)", "lists:seq(1,3,1.0)", "lists:seq(1.0,3,1)", "lists:seq(1,atom,1)" })
        Equal(await MapError(source), Term.A("badarg"));
}
);
Test(
    "lists/seq-big-integer-values",
    async () => Equal(
        await Eval("lists:seq(9223372036854775808,9223372036854775812,2)"),
        Term.List(
            new Integer(BigInteger.Parse("9223372036854775808")),
            new Integer(BigInteger.Parse("9223372036854775810")),
            new Integer(BigInteger.Parse("9223372036854775812"))
        )
    )
);
Test("lists/seq-length-system-limit", async () => Equal(await MapError("lists:seq(0,2147483647)"), Term.A("system_limit")));

Test(
    "lists/reverse-short-error-contract",
    async () =>
 {
     foreach (string source in new[] { "lists:reverse(atom)", "lists:reverse([a|tail])" }) Equal(await MapError(source), Term.A("function_clause"));
 }
);
Test(
    "lists/reverse-long-improper-badarg",
    async () =>
 {
     foreach (string source in new[] { "lists:reverse([a,b|tail])", "lists:reverse([a,b,c|tail])", "lists:reverse(atom,tail)", "lists:reverse([a|tail],[])" }) Equal(await MapError(source), Term.A("badarg"));
 }
);
Test(
    "lists/reverse-tail-and-empty",
    async () =>
 {
     Equal(await Eval("lists:reverse([],tail)"), Term.A("tail"));
     Equal(await Eval("lists:reverse([a,b],tail)"), new Cons(Term.A("b"), new Cons(Term.A("a"), Term.A("tail"))));
 }
);
Test(
    "lists/keyfind-skip-and-first",
    async () => Equal(
        await Eval("lists:keyfind(a,2,[atom,{}, {a},{x,a,first},{y,a,second}])"),
        Term.Tuple(Term.A("x"), Term.A("a"), Term.A("first"))
    )
);
Test(
    "lists/keyfind-improper-prefix",
    async () => Equal(await Eval("lists:keyfind(a,1,[{a,found}|tail])"), Term.Tuple(Term.A("a"), Term.A("found")))
);
Test(
    "lists/keysearch-missing",
    async () =>
 {
     foreach (string source in new[] { "lists:keyfind(a,2,[{},atom,{a}])", "lists:keymember(a,1,[])", "lists:keysearch(a,1,[{b}])" }) Equal(await Eval(source), Term.A("false"));
 }
);
Test(
    "lists/keysearch-invalid-position",
    async () =>
 {
     foreach (string source in new[] { "lists:keyfind(a,0,[])", "lists:keymember(a,-1,[{a}])", "lists:keysearch(a,1.0,[])", "lists:keyfind(a,576460752303423488,[])" }) Equal(await MapError(source), Term.A("badarg"));
 }
);
Test(
    "lists/keysearch-improper-missing",
    async () =>
 {
     foreach (string source in new[] { "lists:keyfind(a,1,[{b}|tail])", "lists:keymember(a,1,atom)", "lists:keysearch(a,1,[atom|tail])" }) Equal(await MapError(source), Term.A("badarg"));
 }
);
Test(
    "lists/keysearch-large-position",
    async () => Equal(await Eval("lists:keyfind(a,576460752303423487,[{a}])"), Term.A("false"))
);
Test(
    "lists/keysearch-small-integer-rounded-float",
    async () => Equal(await Eval("lists:keyfind(9007199254740993,1,[{9007199254740992.0}])"), Term.Tuple(new FloatTerm(9007199254740992d)))
);
Test(
    "lists/keysearch-float-key-exact-rational",
    async () => Equal(await Eval("lists:keyfind(9007199254740992.0,1,[{9007199254740993}])"), Term.A("false"))
);
Test(
    "lists/keysearch-nested-numeric-key",
    async () => Equal(
        await Eval("lists:keysearch({[1],#{a=>2}},1,[{{[1.0],#{a=>2.0}},found}])"),
        Term.Tuple(
            Term.A("value"),
            Term.Tuple(
                Term.Tuple(Term.List(new FloatTerm(1)), new MapTerm(new[] { new KeyValuePair<Term, Term>(Term.A("a"), new FloatTerm(2)) })),
                Term.A("found")
            )
        )
    )
);

Test(
    "terms/map-index-exact-and-structural-keys",
    () =>
{
    var map = new MapTerm([
        new(Term.I(1), Term.A("integer")),
        new(new FloatTerm(1), Term.A("float")),
        new(Term.Tuple(Term.List(Term.I(2))), Term.A("nested")),
        new(new FloatTerm(0), Term.A("first")),
        new(new FloatTerm(-0d), Term.A("last"))
    ]);
    Equal(map.Get(Term.I(1)), Term.A("integer"));
    Equal(map.Get(new FloatTerm(1)), Term.A("float"));
    Equal(map.Get(Term.Tuple(Term.List(Term.I(2)))), Term.A("nested"));
    Equal(map.Get(new FloatTerm(0)), Term.A("first"));
    Equal(map.Get(new FloatTerm(-0d)), Term.A("last"));
    Check(!map.TryGet(Term.I(0), out var missing) && missing is null);
    Check(map.Entries.Count == 5);

    return Task.CompletedTask;
}
);
Test(
    "terms/map-index-owned-input-and-readonly-entries",
    () =>
{
    var input = new Dictionary<Term, Term> { [Term.A("a")] = Term.I(1) };
    var map = new MapTerm(input);
    input[Term.A("a")] = Term.I(2);
    Equal(map.Get(Term.A("a")), Term.I(1));
    var entries = (IList<KeyValuePair<Term, Term>>)map.Entries;
    Throws<NotSupportedException>(() => entries[0] = new(Term.A("a"), Term.I(3)));
    Equal(map.Get(Term.A("a")), Term.I(1));

    return Task.CompletedTask;
}
);
Test(
    "terms/map-index-nested-map-key-order",
    () =>
{
    var key1 = new MapTerm([new(Term.A("a"), Term.I(1)), new(Term.A("b"), Term.I(2))]);
    var key2 = new MapTerm([new(Term.A("b"), Term.I(2)), new(Term.A("a"), Term.I(1))]);
    var map = new MapTerm([new(key1, Term.A("found"))]);
    Equal(map.Get(key2), Term.A("found"));
    Check(map.TryGet(key2, out var value));
    Equal(value!, Term.A("found"));

    return Task.CompletedTask;
}
);
foreach (var fixture in Erlang.Differential.BeginOperatorCases.All.Concat(Erlang.Differential.ExpressionListCases.All).Concat(Erlang.Differential.MapBindingCases.All).Concat(Erlang.Differential.BitEvaluationCases.All).Concat(Erlang.Differential.BitEmptyStringCases.All).Concat(Erlang.Differential.MatchTimingCases.All).Concat(Erlang.Differential.TryExpressionCases.All).Concat(Erlang.Differential.CatchPatternCases.All).Concat(Erlang.Differential.StackGuardScopeCases.All).Concat(Erlang.Differential.MaybeExpressionCases.All).Concat(Erlang.Differential.AliasPatternCases.All).Concat(Erlang.Differential.ListComprehensionCases.All))
{
    Test(
        "compiler/operators/" + fixture.Name,
        async () =>
    {
        var expression = new Parser(fixture.Source).ParseExpression();
        Semantics.Validate(expression);
        await using var runtime = new ProcessRuntime();
        Term? outcome = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                outcome = Term.Tuple(Term.A("ok"), await Execution.EvaluateAsync(expression, context));
            }
            catch (ErlangException exception)
            {
                outcome = Term.Tuple(Term.A("error"), Term.A(exception.ExceptionClass), exception.Reason);
            }

            return Term.A("ok");
        });
        Equal(await process.Completion, Term.A("normal"));
        Equal(outcome!, fixture.Expected);
    }
    );
}
Test(
    "compiler/operators/chained-comparison-diagnostic",
    () =>
{
    foreach (string source in new[] { "1 < 2 < 3", "1 == 1 =:= true", "1 >= 0 /= false" })
        Throws<CompileException>(() => new Parser(source).ParseExpression());

    return Task.CompletedTask;
}
);
Test(
    "compiler/operators/empty-block-diagnostic",
    () =>
{
    Throws<CompileException>(() => new Parser("begin end").ParseExpression());

    return Task.CompletedTask;
}
);
Test(
    "compiler/operators/block-catch-illegal-guard",
    () =>
{
    foreach (string source in new[] { "if (begin true end) -> yes end", "if (catch true) -> yes end" })
    {
        try
        {
            Semantics.Validate(new Parser(source).ParseExpression());
            Check(false);
        }
        catch (CompileException exception)
        {
            Check(exception.Code == "ERL007");
        }
    }

    return Task.CompletedTask;
}
);
Test(
    "hybrid/operators/begin-and-catch",
    () =>
{
    string source = "class A { async Task F(ProcessContext erlangProcess) { var x = begin X=40,if X>0 -> begin X+2 end end end. var y = catch throw(done). } }";
    string generated = CodeGeneration.Preprocess(source, "a.cs");
    Check(generated.Contains("Expr.Block") && generated.Contains("Expr.Catch"));
    Check(!generated.Contains("var y = catch"));

    return Task.CompletedTask;
}
);
Test(
    "hybrid/operators/csharp-try-catch-preserved",
    () =>
{
    string source = "class A { void F() { try { throw new Exception(); } catch(Exception e) { Console.WriteLine(e); } catch { } } string s=\"begin catch value end.\"; int begin()=>42; }";
    Check(CodeGeneration.Preprocess(source, "a.cs").EndsWith(source, StringComparison.Ordinal));

    return Task.CompletedTask;
}
);
Test(
    "compiler/operators/catch-logical-stack-frame",
    async () =>
{
    var value = await Eval("catch error(boom)");
    Check(value is TupleTerm { Items.Count: 2 } outer && outer.Items[0].Equals(Term.A("EXIT")));
    var reasonAndStack = (TupleTerm)((TupleTerm)value).Items[1];
    var frame = (TupleTerm)Cons.Items(reasonAndStack.Items[1]).First();
    Equal(frame.Items[0], Term.A("erlang"));
    Equal(frame.Items[1], Term.A("error"));
    Equal(frame.Items[2], Term.List(Term.A("boom")));
}
);
foreach (var fixture in Erlang.Differential.IfExpressionCases.All)
{
    Test(
        "compiler/if/" + fixture.Name,
        async () =>
    {
        var expression = new Parser(fixture.Source).ParseExpression();
        Semantics.Validate(expression);
        await using var runtime = new ProcessRuntime();
        Term? outcome = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                outcome = Term.Tuple(Term.A("ok"), await Execution.EvaluateAsync(expression, context));
            }
            catch (ErlangException exception)
            {
                outcome = Term.Tuple(Term.A("error"), Term.A(exception.ExceptionClass), exception.Reason);
            }

            return Term.A("ok");
        });
        Equal(await process.Completion, Term.A("normal"));
        Equal(outcome!, fixture.Expected);
    }
    );
}
Test(
    "compiler/if/empty-syntax",
    () =>
{
    Throws<CompileException>(() => new Parser("if end").ParseExpression());

    return Task.CompletedTask;
}
);
Test(
    "compiler/if/illegal-guards",
    () =>
{
    foreach (string source in new[] { "if put(key,value) -> ok end", "if X=1 -> ok end", "if lists:member(a,[]) -> ok end", "if (if true -> true end) -> ok end", "if self() ! message -> ok end" })
    {
        try
        {
            Semantics.Validate(new Parser(source).ParseExpression());
            Check(false);
        }
        catch (CompileException exception)
        {
            Check(exception.Code == "ERL007");
        }
    }

    return Task.CompletedTask;
}
);
Test(
    "compiler/if/unsafe-rematch",
    () =>
{
    try
    {
        Semantics.Validate(new Parser("case ok of ok -> if true -> X=1; false -> no end,X=2 end").ParseExpression());
        Check(false);
    }
    catch (CompileException exception)
    {
        Check(exception.Code == "ERL006" && exception.Message == "Unsafe match variable 'X'");
    }

    return Task.CompletedTask;
}
);
Test(
    "hybrid/if/csharp-statements-preserved",
    () =>
{
    string source = "class A { int F(bool x) { if(x) { return 1; } else if(!x) return 2; return 3; } string s=\"if true -> no end.\"; /* if false -> no end. */ }";
    Check(CodeGeneration.Preprocess(source, "a.cs").EndsWith(source, StringComparison.Ordinal));

    return Task.CompletedTask;
}
);
Test(
    "hybrid/if/nested-and-following-csharp",
    () =>
{
    string source = "class A { async Task F(ProcessContext erlangProcess) { var x = if true -> if false -> no; true -> 42 end end. if(x.Equals(Term.I(42))) return; var y = case ok of ok -> if true -> 7 end end. } }";
    string generated = CodeGeneration.Preprocess(source, "a.cs");
    Check(generated.Contains("Expr.If") && generated.Contains("Expr.Case"));
    Check(generated.Contains("if(x.Equals(Term.I(42))) return;"));
    Check(!generated.Contains("var x = if"));

    return Task.CompletedTask;
}
);
Test(
    "hybrid/if/parenthesized-guard-alternatives",
    () =>
{
    string source = "class A { async Task<Term> F(ProcessContext erlangProcess) { return if (false); (true) -> 42 end. } }";
    string generated = CodeGeneration.Preprocess(source, "a.cs");
    Check(generated.Contains("Expr.GuardAlternatives") && !generated.Contains("return if"));

    return Task.CompletedTask;
}
);
Test(
    "hybrid/if/missing-terminator",
    () =>
{
    Throws<CompileException>(() => CodeGeneration.Preprocess("class A { async Task F(ProcessContext erlangProcess) { var x = if true -> ok end; } }", "a.cs"));

    return Task.CompletedTask;
}
);
foreach (string source in new[]
{
    "try ok end",
    "try ok of catch _ -> no end",
    "try error(reason) catch error:R:[] -> R end"
})
{
    Test(
        "compiler/try/invalid-grammar/" + source,
        () =>
    {
        Throws<CompileException>(() => new Parser(source).ParseExpression());

        return Task.CompletedTask;
    }
    );
}
foreach (var fixture in new (string Source, string Code, string Message)[]
{
    ("begin S=[],try error(reason) catch error:R:S -> R end end", "ERL006", "Stacktrace variable 'S' must be fresh"),
    ("try throw(reason) catch C:S:S -> C end", "ERL006", "Stacktrace variable 'S' must be fresh"),
    ("try error(reason) catch error:R:S when is_list(S) -> R end", "ERL007", "Stacktrace variable 'S' is not legal in a guard")
})
{
    Test(
        "compiler/try/stack-diagnostic/" + fixture.Source,
        () =>
    {
        try
        {
            Semantics.Validate(new Parser(fixture.Source).ParseExpression());
            Check(false);
        }
        catch (CompileException exception)
        {
            Check(exception.Code == fixture.Code && exception.Message == fixture.Message);
        }

        return Task.CompletedTask;
    }
    );
}
Test(
    "hybrid/try/csharp-statements-preserved",
    () =>
{
    string source = "class A { int F() { try { return 1; } catch (Exception) { return 2; } finally { G(); } } void G() {} }";
    Check(CodeGeneration.Preprocess(source, "a.cs").EndsWith(source, StringComparison.Ordinal));

    return Task.CompletedTask;
}
);
Test(
    "hybrid/try/nested-and-following-csharp",
    () =>
{
    string source = "class A { async Task F(ProcessContext erlangProcess) { var x = try try throw(7) catch N -> N end after ignored end. try { G(); } finally { G(); } } }";
    string generated = CodeGeneration.Preprocess(source, "a.cs");
    Check(generated.Contains("Expr.Try") && !generated.Contains("var x = try"));
    Check(generated.Contains("try { G(); } finally { G(); }"));

    return Task.CompletedTask;
}
);
foreach (var fixture in Erlang.Differential.CompiledModuleCases.All)
{
    Test(
        "oracle/compiled-module/" + fixture.Name,
        async () =>
    {
        using var artifact = Erlang.Differential.GeneratedModuleCompiler.Compile(fixture.Source);
        Equal(await Erlang.Differential.CompiledModuleExecution.Run(artifact), fixture.Expected);
        Check(artifact.CSharpSha256.Length == 64);
    }
    );
}
foreach (var fixture in Erlang.Differential.CompiledDiagnosticCases.All)
{
    Test(
        "oracle/compiler-diagnostic/" + fixture.Name,
        () =>
    {
        try
        {
            using var artifact = Erlang.Differential.GeneratedModuleCompiler.Compile(fixture.Source);
            Check(false);
        }
        catch (CompileException exception)
        {
            Check(exception.Code == Erlang.Differential.CompiledDiagnosticExpectations.Code(fixture));
            Check(exception.Message == Erlang.Differential.CompiledDiagnosticExpectations.Message(fixture));
        }

        return Task.CompletedTask;
    }
    );
}
Test(
    "oracle/compiler-diagnostic-ascii-protocol",
    () =>
{
    string command = Erlang.Differential.CompiledModuleProtocol.DiagnosticCommand("-module(scope_unicode). -export([run/0]). run()-><<('𐀀'):S>>.");
    Check(command.All(character => character <= 127));
    Check(command.Contains("compile:forms") && command.Contains("lists:usort"));
    Check(!command.Contains("code:load_binary"));

    return Task.CompletedTask;
}
);
Test(
    "oracle/compiled-module-ascii-protocol",
    () =>
{
    string command = Erlang.Differential.CompiledModuleProtocol.Command("-module(unicode_fixture). -export([run/0]). run()->'𐀀'.");
    Check(command.All(character => character <= 127));
    Check(command.Contains("compile:forms"));
    Check(command.Contains("code:load_binary"));
    Check(!command.Contains("erl_eval:exprs"));

    return Task.CompletedTask;
}
);
Test(
    "oracle/compiled-module-repeat-context",
    async () =>
{
    var fixture = Erlang.Differential.CompiledModuleCases.All[0];
    using var first = Erlang.Differential.GeneratedModuleCompiler.Compile(fixture.Source);
    using var second = Erlang.Differential.GeneratedModuleCompiler.Compile(fixture.Source);
    Equal(await Erlang.Differential.CompiledModuleExecution.Run(first), fixture.Expected);
    Equal(await Erlang.Differential.CompiledModuleExecution.Run(second), fixture.Expected);
    Check(first.CSharpSha256 == second.CSharpSha256);
}
);
Test(
    "oracle/compiled-module-invalid-source",
    () =>
{
    Throws<Erlang.Compiler.CompileException>(() => Erlang.Differential.GeneratedModuleCompiler.Compile("-module(invalid). -export([run/0]). run()-><<1:all>>."));

    return Task.CompletedTask;
}
);

int failed = 0;
foreach (var test in tests)
{
    var watch = Stopwatch.StartNew();
    try
    {
        await test.Body().WaitAsync(TimeSpan.FromSeconds(15));
        Console.WriteLine("PASS " + test.Name);
        results.Add(new
        {
            test.Name,
            Status = "Passed",
            Milliseconds = watch.ElapsedMilliseconds
        });
    }
    catch (Exception ex)
    {
        failed++;
        Console.WriteLine("FAIL " + test.Name + ": " + ex);
        results.Add(new
        {
            test.Name,
            Status = "Failed",
            Error = ex.ToString(),
            Milliseconds = watch.ElapsedMilliseconds
        });
    }
}
Console.WriteLine($"{tests.Count - failed}/{tests.Count} passed");
string? report = args.FirstOrDefault();
if (report is not null)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report))!);
    await File.WriteAllTextAsync(
        report,
        JsonSerializer.Serialize(new
        {
            Passed = tests.Count - failed,
            Failed = failed,
            Tests = results
        }, new JsonSerializerOptions { WriteIndented = true })
    );
}
if (failed == 0 && args.Length > 1)
    await File.WriteAllTextAsync(
        args[1],
        JsonSerializer.Serialize(
            exportRegistry.Exports.Select(e => new { e.Module, e.Function, e.Arity, Status = "Partially compatible", Evidence = $"mfa/{e.Module}:{e.Function}/{e.Arity}", Limits = "Single direct contract case plus feature regressions; complete error/options and OTP differential verification pending" }),
            new JsonSerializerOptions { WriteIndented = true }
        )
    );
return failed == 0 ? 0 : 1;
