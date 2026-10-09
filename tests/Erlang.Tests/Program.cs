using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using Erlang;
using Erlang.Compiler;
using Erlang.Otp;

var tests = new List<(string Name, Func<Task> Body)>();
void Test(string name, Func<Task> body) => tests.Add((name, body));
void Check(bool value, string message = "Assertion failed") { if (!value) throw new InvalidOperationException(message); }
void Equal(Term a, Term b) => Check(a.Equals(b), $"Expected {b}, got {a}");
void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
Test("oracle/unicode-source-ascii-transport", () =>
{
    string source = "try ('𐀀' > '\uffff') of X -> {ok,X} end";
    string command = Erlang.Differential.OracleProtocol.EvaluationCommand(source);
    Check(command.All(c => c <= 127));
    string encoded = command.Split("base64:decode(\"", StringSplitOptions.None)[1].Split('"')[0];
    Check(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded)) == source + ".");
    Check(command.Contains("unicode:characters_to_list") && command.Contains("erl_scan:string") && command.Contains("erl_eval:exprs"));
    return Task.CompletedTask;
});
Test("oracle/source-quoting-roundtrip", () =>
{
    string source = "{\"quote\\\" slash\\\\\", 'Привет 🌍',\n42}";
    string command = Erlang.Differential.OracleProtocol.EvaluationCommand(source);
    Check(command.All(c => c <= 127) && !command.Contains('\n'));
    string encoded = command.Split("base64:decode(\"", StringSplitOptions.None)[1].Split('"')[0];
    Check(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded)) == source + ".");
    return Task.CompletedTask;
});
async Task<Term> Eval(string source)
{
    await using var runtime = new ProcessRuntime(); Term? result = null; var e = new Parser(source).ParseExpression(); Semantics.Validate(e);
    var p = runtime.Spawn(async c => { result = await Execution.EvaluateAsync(e, c); return Term.A("ok"); });
    Equal(await p.Completion.WaitAsync(TimeSpan.FromSeconds(3)), Term.A("normal")); return result!;
}
Test("terms/exact-numeric-equality", () => { Check(!Term.I(1).Equals(new FloatTerm(1))); Check(Term.I(1).NumericEquals(new FloatTerm(1))); return Task.CompletedTask; });
Test("terms/large-integer-double-comparison", () => { Check(new Integer(BigInteger.Parse("9007199254740993")).CompareTo(new FloatTerm(9007199254740992)) > 0); Check(new Integer(BigInteger.One << 2000).CompareTo(new FloatTerm(double.MaxValue)) > 0); return Task.CompletedTask; });
Test("terms/negative-fraction-comparison", () => { Check(Term.I(-1).CompareTo(new FloatTerm(-1.5)) > 0); Check(Term.I(0).CompareTo(new FloatTerm(double.Epsilon)) < 0); return Task.CompletedTask; });
Test("terms/type-order", () => { Term[] terms = [Term.I(1), Term.A("a"), new ReferenceTerm("n", 1), new FunctionTerm(0, (c, a) => ValueTask.FromResult<Term>(Term.A("ok"))), new PortTerm("n", 1), new Pid("n", 1), Term.Tuple(), new MapTerm([]), Nil.Value, Term.List(Term.I(0)), new BitString([])]; for (int i = 1; i < terms.Length; i++) Check(terms[i - 1].CompareTo(terms[i]) < 0); return Task.CompletedTask; });
Test("terms/tuple-arity-first", () => { Check(Term.Tuple(Term.I(999)).CompareTo(Term.Tuple(Term.I(0), Term.I(0))) < 0); return Task.CompletedTask; });
Test("terms/improper-list", () => { var l = new Cons(Term.I(1), Term.A("tail")); Equal(l, new Cons(Term.I(1), Term.A("tail"))); Throws<ErlangException>(() => Cons.Items(l).ToArray()); return Task.CompletedTask; });
Test("terms/map-exact-keys", () => { var m = new MapTerm([new(Term.I(1), Term.A("int")), new(new FloatTerm(1), Term.A("float"))]); Check(m.Entries.Count == 2); Equal(m.Get(Term.I(1)), Term.A("int")); Equal(m.Get(new FloatTerm(1)), Term.A("float")); return Task.CompletedTask; });
Test("terms/map-integer-before-all-floats", () => { var m = new MapTerm([new(new FloatTerm(-100), Term.A("float")), new(Term.I(100), Term.A("integer"))]); Check(m.Entries[0].Key is Integer); return Task.CompletedTask; });
Test("terms/signed-zero-otp29", () => { Term positive = new FloatTerm(0.0), negative = new FloatTerm(-0.0); Check(!positive.Equals(negative)); Check(positive.NumericEquals(negative)); Equal(ExternalTermFormat.Decode(ExternalTermFormat.Encode(negative)), negative); return Task.CompletedTask; });
Test("terms/atom-codepoint-order", () => { Check(Term.A("\U00010000").CompareTo(Term.A("\uffff")) > 0); return Task.CompletedTask; });
Test("compiler/quoted-unicode-valid-codepoint-order", async () => Equal(await Eval("'𐀀' > '\ufffd'"), Term.A("true")));
Test("compiler/quoted-unicode-illegal-character", () =>
{
    foreach (string invalid in new[] { "\ufffe", "\uffff", "\ud800", "\udfff" })
        foreach (string quote in new[] { "'", "\"" }) Throws<CompileException>(() => new Parser(quote + invalid + quote).ParseExpression());
    return Task.CompletedTask;
});
Test("terms/structural-hash", () => { Term[] a = [Term.Tuple(Term.I(1), Term.List(Term.A("a"))), new FloatTerm(-0.0), new BitString([255], 3), new MapTerm([new(Term.A("x"), Term.I(2))])]; foreach (var t in a) Check(t.GetHashCode() == ExternalTermFormat.Decode(ExternalTermFormat.Encode(t)).GetHashCode()); return Task.CompletedTask; });
Test("terms/unicode-list", () => { string s = "Привет 🌍"; Check(Term.Text(Term.String(s)) == s); return Task.CompletedTask; });
Test("terms/immutable-binary", () => { byte[] bytes = [255]; var b = new BitString(bytes, 3); bytes[0] = 0; Check(b.ToArray()[0] == 224); var copy = b.ToArray(); copy[0] = 0; Check(b.ToArray()[0] == 224); return Task.CompletedTask; });
Test("patterns/repeated-variable", () => { var p = new Pattern.Tuple([new Pattern.Variable("X"), new Pattern.Variable("X")]); var b = new Dictionary<string, Term>(); Check(!p.Match(Term.Tuple(Term.I(1), Term.I(2)), b)); Check(b.Count == 0); Check(p.Match(Term.Tuple(Term.I(1), Term.I(1)), b)); Equal(b["X"], Term.I(1)); return Task.CompletedTask; });
Test("patterns/already-bound", () => { var b = new Dictionary<string, Term> { { "X", Term.I(1) } }; Check(!new Pattern.Variable("X").Match(new FloatTerm(1), b)); Equal(b["X"], Term.I(1)); return Task.CompletedTask; });
Test("patterns/list-tail", () => { var p = new Pattern.List([new Pattern.Variable("H")], new Pattern.Variable("T")); var b = new Dictionary<string, Term>(); Check(p.Match(Term.List(Term.I(1), Term.I(2)), b)); Equal(b["T"], Term.List(Term.I(2))); return Task.CompletedTask; });
Test("mailbox/later-match-preserves-order", async () => { var m = new Mailbox(); m.Send(Term.I(1)); m.Send(Term.A("pick")); m.Send(Term.I(2)); Equal((await m.ReceiveAsync(t => t is Atom ? t : null, TimeSpan.Zero))!, Term.A("pick")); Equal((await m.ReceiveAsync(t => t, TimeSpan.Zero))!, Term.I(1)); Equal((await m.ReceiveAsync(t => t, TimeSpan.Zero))!, Term.I(2)); });
Test("mailbox/zero-timeout-retains", async () => { var m = new Mailbox(); m.Send(Term.I(1)); Check(await m.ReceiveAsync(t => t is Atom ? t : null, TimeSpan.Zero) == null); Check(m.Count == 1); });
Test("mailbox/timeout", async () => { var m = new Mailbox(); var sw = Stopwatch.StartNew(); Check(await m.ReceiveAsync(t => t, TimeSpan.FromMilliseconds(25)) == null); Check(sw.ElapsedMilliseconds >= 15); });
Test("mailbox/concurrent-arrival", async () => { var m = new Mailbox(); var receive = m.ReceiveAsync(t => t, TimeSpan.FromSeconds(2)); await Task.Yield(); m.Send(Term.I(42)); Equal((await receive)!, Term.I(42)); });
Test("mailbox/cancel", async () => { var m = new Mailbox(); using var cancel = new CancellationTokenSource(); var task = m.ReceiveAsync(t => t, null, cancel.Token).AsTask(); cancel.Cancel(); try { await task; throw new InvalidOperationException(); } catch (OperationCanceledException) { } });
Test("mailbox/sender-order-stress", async () => { var m = new Mailbox(); await Task.WhenAll(Enumerable.Range(0, 4).Select(sender => Task.Run(() => { for (int i = 0; i < 250; i++) m.Send(Term.Tuple(Term.I(sender), Term.I(i))); }))); for (int sender = 0; sender < 4; sender++) for (int i = 0; i < 250; i++) { int s = sender; var v = (TupleTerm)(await m.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.I(s)) ? x : null, TimeSpan.Zero))!; Equal(v.Items[1], Term.I(i)); } Check(m.Count == 0); });
Test("runtime/process-self-and-dictionary", async () => { await using var r = new ProcessRuntime(); var p = r.Spawn(async c => { Equal(await r.Modules.Call(c, "erlang", "self"), c.Self); Equal(await r.Modules.Call(c, "erlang", "put", Term.A("key"), Term.I(42)), Term.A("undefined")); Equal(await r.Modules.Call(c, "erlang", "get", Term.A("key")), Term.I(42)); Equal(await r.Modules.Call(c, "erlang", "erase", Term.A("key")), Term.I(42)); return Term.A("ok"); }); Equal(await p.Completion, Term.A("normal")); });
Test("runtime/registration-cleanup", async () => { await using var r = new ProcessRuntime(); var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var p = r.Spawn(async c => { await gate.Task; return Term.A("ok"); }); r.Register("test", p.Pid); Equal(r.WhereIs("test"), p.Pid); Throws<ErlangException>(() => r.Register("other", p.Pid)); gate.SetResult(); Equal(await p.Completion, Term.A("normal")); Equal(r.WhereIs("test"), Term.A("undefined")); });
Test("runtime/monitor-down", async () => { await using var r = new ProcessRuntime(); var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var target = r.Spawn(async c => { await release.Task; throw new ErlangException(Term.A("boom"), "exit"); }); Term? down = null; var observer = r.Spawn(async c => { var reference = r.Monitor(c, target.Pid); release.TrySetResult(); down = await c.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.A("DOWN")) && x.Items[1].Equals(reference) ? x : null, TimeSpan.FromSeconds(2)); return Term.A("ok"); }); Equal(await observer.Completion, Term.A("normal")); Check(down is TupleTerm { Items.Count: 5 } d && d.Items[4].Equals(Term.A("boom"))); });
Test("runtime/monitor-noproc-and-flush", async () => { await using var r = new ProcessRuntime(); var observer = r.Spawn(c => { var reference = r.Monitor(c, new Pid(r.Node, 999)); Check(c.Mailbox.Count == 1); r.Demonitor(c, reference, true); Check(c.Mailbox.Count == 0); return ValueTask.FromResult<Term>(Term.A("ok")); }); Equal(await observer.Completion, Term.A("normal")); });
Test("runtime/link-trap-exit", async () => { await using var r = new ProcessRuntime(); Term? signal = null; var parent = r.Spawn(async c => { c.TrapExits = true; var child = r.Spawn(x => throw new ErlangException(Term.A("boom"), "exit"), c, true); signal = await c.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.A("EXIT")) ? x : null, TimeSpan.FromSeconds(2)); return Term.A("ok"); }); Equal(await parent.Completion, Term.A("normal")); Check(signal is TupleTerm x && x.Items[2].Equals(Term.A("boom"))); });
Test("runtime/link-propagation", async () => { await using var r = new ProcessRuntime(); var parent = r.Spawn(async c => { r.Spawn(x => throw new ErlangException(Term.A("boom"), "exit"), c, true); await c.ReceiveAsync(t => t); return Term.A("unreachable"); }); Equal(await parent.Completion, Term.A("boom")); });
Test("runtime/normal-link-exit-ignored", async () => { await using var r = new ProcessRuntime(); var parent = r.Spawn(async c => { var child = r.Spawn(x => ValueTask.FromResult<Term>(Term.A("ok")), c, true); await child.Completion; Check(r.IsAlive(c.Self)); return Term.A("ok"); }); Equal(await parent.Completion, Term.A("normal")); });
Test("runtime/kill-untrappable", async () => { await using var r = new ProcessRuntime(); var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var target = r.Spawn(async c => { c.TrapExits = true; ready.SetResult(); await c.ReceiveAsync(t => t); return Term.A("unreachable"); }); await ready.Task; var sender = r.Spawn(c => { r.Exit(c, target.Pid, Term.A("kill")); return ValueTask.FromResult<Term>(Term.A("ok")); }); await sender.Completion; Equal(await target.Completion, Term.A("killed")); Check(!r.IsAlive(target.Pid)); });
Test("runtime/1000-waiting-processes", async () => { await using var r = new ProcessRuntime(); var ps = Enumerable.Range(0, 1000).Select(_ => r.Spawn(async c => { Equal((await c.ReceiveAsync(t => t, TimeSpan.FromSeconds(5)))!, Term.A("go")); return Term.A("ok"); })).ToArray(); foreach (var p in ps) r.Send(p.Pid, Term.A("go")); var reasons = await Task.WhenAll(ps.Select(p => p.Completion)); Check(reasons.All(x => x.Equals(Term.A("normal")))); });
Test("compiler/arithmetic-precedence", async () => Equal(await Eval("1 + 2 * 3"), Term.I(7)));
Test("compiler/exact-equality", async () => Equal(await Eval("{1 == 1.0, 1 =:= 1.0}"), Term.Tuple(Term.A("true"), Term.A("false"))));
Test("compiler/case-pattern-guard", async () => Equal(await Eval("case {hello, 42} of {hello, X} when is_integer(X), X > 0 -> X; _ -> bad end"), Term.I(42)));
Test("compiler/guard-error-rejects", async () => Equal(await Eval("case a of X when hd(X) == 1 -> bad; _ -> ok end"), Term.A("ok")));
Test("compiler/guard-alternative", async () => Equal(await Eval("case a of X when hd(X) == 1; is_atom(X) -> ok end"), Term.A("ok")));
Test("compiler/list-fun-map", async () => Equal(await Eval("lists:map(fun(X) -> X * 2 end, [1,2,3])"), Term.List(Term.I(2), Term.I(4), Term.I(6))));
Test("compiler/receive-after-zero", async () => Equal(await Eval("receive {hello, X} -> X after 0 -> timeout end"), Term.A("timeout")));
Test("compiler/binding-and-repeat", async () => Equal(await Eval("case {1,1} of {X,X} -> X; _ -> bad end"), Term.I(1)));
Test("compiler/single-assignment-mismatch", async () => { await using var r = new ProcessRuntime(); var e = new Parser("case ok of ok -> X = 1, X = 2 end").ParseExpression(); Semantics.Validate(e); var p = r.Spawn(c => Execution.EvaluateAsync(e, c)); Check((await p.Completion) is TupleTerm t && t.Items[0] is TupleTerm error && error.Items[0].Equals(Term.A("badmatch"))); });
Test("compiler/unbound-diagnostic", () => { Throws<CompileException>(() => Semantics.Validate(new Parser("X + 1").ParseExpression())); return Task.CompletedTask; });
Test("compiler/unsafe-variable-diagnostic", () => { Throws<CompileException>(() => new Parser("-module(a). -export([f/1]). f(A) -> case A of 1 -> X = 1; _ -> ok end, X.").ParseModule()); return Task.CompletedTask; });
Test("compiler/illegal-guard-diagnostic", () => { Throws<CompileException>(() => new Parser("-module(a). -export([f/1]). f(X) when io:format(\"~p\", [X]) -> ok.").ParseModule()); return Task.CompletedTask; });
Test("compiler/quoted-keyword-atom", async () => Equal(await Eval("{'receive', 'end'}"), Term.Tuple(Term.A("receive"), Term.A("end"))));
Test("compiler/module-clauses-private-function", async () => { var m = new Parser("-module(a). -export([f/1]). f(0) -> zero; f(X) when is_integer(X) -> g(X). g(X) -> X + 1.").ParseModule(); await using var r = new ProcessRuntime(); m.Register(r.Modules); Term? result = null; var p = r.Spawn(async c => { result = await r.Modules.Call(c, "a", "f", Term.I(2)); return Term.A("ok"); }); Equal(await p.Completion, Term.A("normal")); Equal(result!, Term.I(3)); Check(!r.Modules.Exports.Contains(("a", "g", 1))); });
Test("compiler/spawn-fun-child-self", async () => { await using var r = new ProcessRuntime(); Term? result = null; var e = new Parser("spawn(fun() -> self() end)").ParseExpression(); var p = r.Spawn(async c => { result = await Execution.EvaluateAsync(e, c); Check(result is Pid child && !child.Equals(c.Self)); return Term.A("ok"); }); Equal(await p.Completion, Term.A("normal")); });
Test("compiler/selective-receive-clause-order", async () => { await using var r = new ProcessRuntime(); Term? result = null; var e = new Parser("receive {n, X} when X > 10 -> high; {n, X} -> X after 1000 -> timeout end").ParseExpression(); var p = r.Spawn(async c => { result = await Execution.EvaluateAsync(e, c); return Term.A("ok"); }); r.Send(p.Pid, Term.Tuple(Term.A("other"), Term.I(0))); r.Send(p.Pid, Term.Tuple(Term.A("n"), Term.I(1))); r.Send(p.Pid, Term.Tuple(Term.A("n"), Term.I(20))); Equal(await p.Completion, Term.A("normal")); Equal(result!, Term.I(1)); });
Test("hybrid/csharp-strings-comments-raw-preserved", () => { string source = "class A { string s = \"receive {a} -> ok end.\"; string r = \"\"\"case x of a -> b end.\"\"\"; /* receive {a} -> ok end. */ int receive() => 1; }"; string generated = CodeGeneration.Preprocess(source, "a.cs"); Check(generated.EndsWith(source, StringComparison.Ordinal)); return Task.CompletedTask; });
Test("hybrid/source-map-and-lowering", () => { string source = "using Erlang; class A { async Task<Term> Run(ProcessContext erlangProcess) { return receive {hello, Name} -> Name after 0 -> ok end. } }"; string generated = CodeGeneration.Preprocess(source, "a.cs"); Check(generated.Contains("#line 1")); Check(generated.Contains("await global::Erlang.Compiler.Execution.EvaluateAsync")); Check(!generated.Contains("return receive")); return Task.CompletedTask; });
Test("hybrid/nested-case-and-fun", () => { string source = "class A { async Task Run(ProcessContext erlangProcess) { var result = case 1 of X -> fun(Y) -> X + Y end end. } }"; string generated = CodeGeneration.Preprocess(source, "a.cs"); Check(generated.Contains("Expr.Case")); Check(generated.Contains("Expr.Fun")); return Task.CompletedTask; });
Test("etf/reference-vectors", () => { Equal(ExternalTermFormat.Decode([131, 97, 42]), Term.I(42)); Equal(ExternalTermFormat.Decode([131, 100, 0, 2, 111, 107]), Term.A("ok")); Equal(ExternalTermFormat.Decode([131, 104, 2, 119, 2, 111, 107, 97, 42]), Term.Tuple(Term.A("ok"), Term.I(42))); Check(ExternalTermFormat.Encode(Term.I(42)).SequenceEqual(new byte[] { 131, 97, 42 })); return Task.CompletedTask; });
Test("etf/roundtrip-all-supported-kinds", () => { Term[] terms = [new Integer(BigInteger.One << 300), new Integer(-(BigInteger.One << 300)), Term.I(-42), new FloatTerm(1.5), Term.A("世界"), Term.List(Term.I(1)), new Cons(Term.A("x"), Term.A("tail")), Term.Tuple(Term.A("ok"), Nil.Value), new BitString([128], 1), new BitString([0, 255]), new Pid("n@h", ulong.MaxValue, 42), new PortTerm("n@h", ulong.MaxValue, 42), new ReferenceTerm("n@h", ulong.MaxValue, 42), new MapTerm([new(Term.I(1), Term.A("a")), new(new FloatTerm(1), Term.A("b"))])]; foreach (var t in terms) Equal(ExternalTermFormat.Decode(ExternalTermFormat.Encode(t)), t); return Task.CompletedTask; });
Test("etf/malformed-input-limits", () => { Throws<ErlangException>(() => ExternalTermFormat.Decode([131, 108, 255, 255, 255, 255])); Throws<ErlangException>(() => ExternalTermFormat.Decode([131, 97])); Throws<ErlangException>(() => ExternalTermFormat.Decode([131, 97, 1, 0])); Throws<ErlangException>(() => ExternalTermFormat.Decode([131, 119, 1, 255])); Throws<NotSupportedException>(() => ExternalTermFormat.Decode([131, 112])); return Task.CompletedTask; });
Test("etf/random-integer-property", () => { var random = new Random(42); for (int i = 0; i < 200; i++) { byte[] bytes = new byte[random.Next(1, 128)]; random.NextBytes(bytes); Term n = new Integer(new BigInteger(bytes)); Equal(ExternalTermFormat.Decode(ExternalTermFormat.Encode(n)), n); } return Task.CompletedTask; });
Test("modules/lists-member-exact", async () => Equal(await Eval("lists:member(1, [1.0])"), Term.A("false")));
Test("modules/lists-and-maps", async () => { Equal(await Eval("{length([1,2]), lists:reverse([1,2]), lists:sum([1,2,3]), hd([ok])}"), Term.Tuple(Term.I(2), Term.List(Term.I(2), Term.I(1)), Term.I(6), Term.A("ok"))); });
Test("compiler/tail-recursion-50000", async () => { var m = new Parser("-module(loop). -export([run/1]). run(0) -> ok; run(N) -> case N > 0 of true -> run(N - 1) end.").ParseModule(); await using var r = new ProcessRuntime(); m.Register(r.Modules); Term? result = null; var p = r.Spawn(async c => { result = await r.Modules.Call(c, "loop", "run", Term.I(50000)); return Term.A("ok"); }); Equal(await p.Completion.WaitAsync(TimeSpan.FromSeconds(10)), Term.A("normal")); Equal(result!, Term.A("ok")); });
Test("hybrid/ordinary-fun-call", () => { string source = "class C { int fun(int x)=>x; int F()=>fun(1); int G(){return fun(2);} }"; Check(CodeGeneration.Preprocess(source, "c.cs").EndsWith(source, StringComparison.Ordinal)); return Task.CompletedTask; });
Test("hybrid/csharp-switch-and-erlang-case", () => { string source = "class C { async Task F(ProcessContext erlangProcess) { switch(1){case 1: break; case 2: break; default: break;} var result = case 2 of X -> X end. } }"; var generated = CodeGeneration.Preprocess(source, "c.cs"); Check(generated.Contains("switch(1){case 1: break; case 2: break; default: break;}")); Check(generated.Contains("Expr.Case")); return Task.CompletedTask; });
Test("compiler/unsafe-variable-rebinding-diagnostic", () => { Throws<CompileException>(() => new Parser("-module(a). -export([f/1]). f(A) -> case A of 1 -> X = 1; _ -> ok end, X = 2.").ParseModule()); return Task.CompletedTask; });
Test("compiler/signed-zero-pattern", async () => Equal(await Eval("case -0.0 of 0.0 -> wrong; -0.0 -> ok end"), Term.A("ok")));
Test("otp/gen-server-call-cast-info-stop", async () =>
{
    await using var r = new ProcessRuntime(); var callback = new CounterServer(); var server = await GenServer.Start(r, callback, Term.I(0));
    var client = r.Spawn(async c => { GenServer.Cast(c, server.Pid, Term.I(2)); r.Send(server.Pid, Term.I(3)); Equal(await GenServer.Call(c, server.Pid, Term.A("get")), Term.I(5)); Equal(await GenServer.Call(c, server.Pid, Term.A("stop")), Term.A("ok")); return Term.A("ok"); });
    Equal(await client.Completion, Term.A("normal")); Equal(await server.Completion, Term.A("normal")); Check(callback.Terminated);
});
Test("otp/gen-server-crash-monitor", async () => { await using var r = new ProcessRuntime(); var server = await GenServer.Start(r, new CounterServer(), Term.I(0)); var client = r.Spawn(async c => { try { await GenServer.Call(c, server.Pid, Term.A("crash")); throw new InvalidOperationException("Expected crash"); } catch (ErlangException ex) { Check(ex.ExceptionClass == "exit"); } return Term.A("ok"); }); Equal(await client.Completion, Term.A("normal")); Check(!r.IsAlive(server.Pid)); });
Test("otp/gen-server-timeout", async () => { await using var r = new ProcessRuntime(); var server = await GenServer.Start(r, new CounterServer(), Term.I(0)); var client = r.Spawn(async c => { try { await GenServer.Call(c, server.Pid, Term.A("noreply"), TimeSpan.FromMilliseconds(20)); throw new InvalidOperationException("Expected timeout"); } catch (ErlangException ex) { Equal(ex.Reason, Term.A("timeout")); } return Term.A("ok"); }); Equal(await client.Completion, Term.A("normal")); });
Test("otp/gen-server-init-error", async () => { await using var r = new ProcessRuntime(); try { await GenServer.Start(r, new CounterServer(), Term.A("fail")); throw new InvalidOperationException("Expected init failure"); } catch (ErlangException ex) { Equal(ex.Reason, Term.A("init_failed")); } });
foreach (var strategy in Enum.GetValues<RestartStrategy>())
    Test("otp/supervisor-" + strategy, async () =>
    {
        await using var r = new ProcessRuntime(); int[] starts = [0, 0, 0]; var restartObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ChildSpec Spec(int index) => new(Term.I(index), c => { if (Interlocked.Increment(ref starts[index]) == 2 && index == 1) restartObserved.TrySetResult(); return ValueTask.FromResult(r.Spawn(async child => { await child.ReceiveAsync(t => t); return Term.A("ok"); }, c, true)); });
        var supervisor = await Supervisor.Start(r, [Spec(0), Spec(1), Spec(2)], strategy, 5); var initial = supervisor.Children.Select(x => x.Pid).ToArray();
        var killer = r.Spawn(c => { r.Exit(c, initial[1], Term.A("boom")); return ValueTask.FromResult<Term>(Term.A("ok")); }); await killer.Completion; await restartObserved.Task.WaitAsync(TimeSpan.FromSeconds(2));
        // Wait for the restart batch to finish publishing its complete child list.
        var deadline = Stopwatch.StartNew(); while (supervisor.Children.Count != 3 && deadline.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(1);
        Check(starts[0] == (strategy == RestartStrategy.OneForAll ? 2 : 1)); Check(starts[1] == 2); Check(starts[2] == (strategy == RestartStrategy.OneForOne ? 1 : 2));
        await Supervisor.Stop(r, supervisor); Equal(await supervisor.Process.Completion, Term.A("shutdown")); Check(supervisor.Children.Count == 0);
    });
Test("otp/supervisor-transient-temporary-normal", async () => { await using var r = new ProcessRuntime(); int starts = 0; var childReady = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously); var supervisor = await Supervisor.Start(r, [new ChildSpec(Term.A("t"), c => { starts++; var p = r.Spawn(async x => { await x.ReceiveAsync(t => t); return Term.A("ok"); }, c, true); childReady.SetResult(p.Pid); return ValueTask.FromResult(p); }, RestartPolicy.Transient)]); var pid = await childReady.Task; r.Send(pid, Term.A("finish")); var deadline = Stopwatch.StartNew(); while (supervisor.Children.Count != 0 && deadline.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(1); Check(starts == 1 && supervisor.Children.Count == 0); await Supervisor.Stop(r, supervisor); });
Test("otp/supervisor-restart-intensity", async () => { await using var r = new ProcessRuntime(); var trigger = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously); var supervisor = await Supervisor.Start(r, [new ChildSpec(Term.A("child"), c => { var p = r.Spawn(async x => { await x.ReceiveAsync(t => t); x.Exit(Term.A("boom")); return Term.A("ok"); }, c, true); trigger.TrySetResult(p.Pid); return ValueTask.FromResult(p); })], intensity: 0); r.Send(await trigger.Task, Term.A("crash")); Equal(await supervisor.Process.Completion.WaitAsync(TimeSpan.FromSeconds(2)), Term.A("shutdown")); Check(supervisor.Children.Count == 0); });
Test("otp/supervisor-temporary-abnormal-no-restart", async () => { await using var r = new ProcessRuntime(); int starts = 0; var supervisor = await Supervisor.Start(r, [new ChildSpec(Term.A("child"), c => { starts++; return ValueTask.FromResult(r.Spawn(async x => { await x.ReceiveAsync(t => t); x.Exit(Term.A("boom")); return Term.A("ok"); }, c, true)); }, RestartPolicy.Temporary)], intensity: 0); r.Send(supervisor.Children[0].Pid, Term.A("crash")); var deadline = Stopwatch.StartNew(); while (supervisor.Children.Count != 0 && deadline.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(1); Check(starts == 1 && supervisor.Children.Count == 0); Check(r.IsAlive(supervisor.Process.Pid)); await Supervisor.Stop(r, supervisor); });
Test("runtime/link-cascade-10000", async () => { await using var r = new ProcessRuntime(); ProcessContext? previous = null; var processes = new List<ProcessHandle>(); for (int i = 0; i < 10000; i++) { var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously); var p = r.Spawn(async c => { ready.SetResult(c); await c.ReceiveAsync(t => t); return Term.A("ok"); }, previous, previous is not null); processes.Add(p); previous = await ready.Task; } var killer = r.Spawn(c => { r.Exit(c, processes[0].Pid, Term.A("boom")); return ValueTask.FromResult<Term>(Term.A("ok")); }); await killer.Completion; var reasons = await Task.WhenAll(processes.Select(p => p.Completion)); Check(reasons.All(x => x.Equals(Term.A("boom")))); });
Test("runtime/spawn-monitor-immediate-exit-atomic", async () => { await using var r = new ProcessRuntime(); var parent = r.Spawn(async c => { for (int i = 0; i < 200; i++) { var spawn = r.SpawnMonitor(c, x => throw new ErlangException(Term.A("boom"), "exit")); var down = await c.ReceiveAsync(t => t is TupleTerm x && x.Items.Count == 5 && x.Items[1].Equals(spawn.Reference) ? x : null, TimeSpan.FromSeconds(1)); Check(down is not null && down.Items[4].Equals(Term.A("boom"))); } return Term.A("ok"); }); Equal(await parent.Completion, Term.A("normal")); });
Test("terms/bitstring-format", () => { Check(new BitString([224], 3).ToString() == "<<7:3>>"); return Task.CompletedTask; });
Test("compiler/large-literal-codegeneration", () => { var term = Term.String(new string('a', 10000)); var generated = CodeGeneration.TermCode(term); Check(generated.StartsWith("global::Erlang.Cons.From", StringComparison.Ordinal)); return Task.CompletedTask; });
Test("compiler/reserved-keywords-rejected", () => { Throws<CompileException>(() => new Parser("end").ParseExpression()); Throws<CompileException>(() => new Parser("if true -> ok end").ParseExpression()); return Task.CompletedTask; });
Test("compiler/unsupported-escape-diagnostic", () => { Throws<CompileException>(() => new Parser("\"\\x{41}\"").ParseExpression()); return Task.CompletedTask; });
Test("compiler/quoted-operator-atom", async () => Equal(await Eval("{'not', 'div'}"), Term.Tuple(Term.A("not"), Term.A("div"))));
Test("hybrid/nullable-context", () => { Check(CodeGeneration.Preprocess("class C { string? value; }", "c.cs").StartsWith("#nullable enable", StringComparison.Ordinal)); Check(CodeGeneration.Preprocess("class C {}", "c.cs", "disable").StartsWith("#nullable disable", StringComparison.Ordinal)); return Task.CompletedTask; });

// Every registered MFA has a direct contract smoke test. Error/option completeness still needs OTP differential coverage.
var exportRegistry = new ModuleRegistry(); CoreModules.Register(exportRegistry);
foreach (var export in exportRegistry.Exports.OrderBy(x => x.Module).ThenBy(x => x.Function).ThenBy(x => x.Arity))
    Test($"mfa/{export.Module}:{export.Function}/{export.Arity}", async () =>
    {
        using var output = new StringWriter(); await using var runtime = new ProcessRuntime(output: output);
        var process = runtime.Spawn(async c =>
        {
            async ValueTask<Term> Call(params Term[] a) => await runtime.Modules.Call(c, export.Module, export.Function, a);
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
                ("erlang", "bit_size", 1) => ([new BitString([128,128], 9)], Term.I(9)),
                ("erlang", "byte_size", 1) => ([new BitString([128,128], 9)], Term.I(2)),
                ("erlang", "is_list", 1) => ([new Cons(Term.I(1), Term.A("tail"))], Term.A("true")),
                ("erlang", "is_pid", 1) => ([c.Self], Term.A("true")),
                ("erlang", "is_map", 1) => ([new MapTerm([])], Term.A("true")),
                ("erlang", "map_size", 1) => ([new MapTerm([new(Term.A("a"), Term.I(1))])], Term.I(1)),
                ("erlang", "map_get", 2) => ([Term.A("a"), new MapTerm([new(Term.A("a"), Term.I(42))])], Term.I(42)),
                ("erlang", "is_map_key", 2) => ([Term.I(1), new MapTerm([new(new FloatTerm(1), Term.A("float"))])], Term.A("false")),
                ("lists", "reverse", 1) => ([Term.List(Term.I(1), Term.I(2))], Term.List(Term.I(2), Term.I(1))),
                ("lists", "reverse", 2) => ([Term.List(Term.I(1), Term.I(2)), Term.A("tail")], new Cons(Term.I(2), new Cons(Term.I(1), Term.A("tail")))),
                ("lists", "append", 2) => ([Term.List(Term.I(1)), Term.A("tail")], new Cons(Term.I(1), Term.A("tail"))),
                ("lists", "member", 2) => ([Term.I(1), Term.List(new FloatTerm(1))], Term.A("false")),
                ("lists", "sum", 1) => ([Term.List(Term.I(1), new FloatTerm(2.5))], new FloatTerm(3.5)),
                ("maps", "get", 2) => ([Term.A("k"), new MapTerm([new(Term.A("k"), Term.I(42))])], Term.I(42)),
                ("maps", "size", 1) => ([new MapTerm([new(Term.A("k"), Term.I(42))])], Term.I(1)),
                _ => null
            };
            if (pure is { } test) { Equal(await Call(test.Args), test.Expected); return Term.A("ok"); }
            switch (export.Function)
            {
                case "self": Equal(await Call(), c.Self); break;
                case "make_ref": var first = await Call(); var second = await Call(); Check(first is ReferenceTerm && !first.Equals(second)); break;
                case "register": Equal(await Call(Term.A("name"), c.Self), Term.A("true")); Equal(runtime.WhereIs("name"), c.Self); break;
                case "whereis": runtime.Register("name", c.Self); Equal(await Call(Term.A("name")), c.Self); Equal(await Call(Term.A("absent")), Term.A("undefined")); break;
                case "unregister": runtime.Register("name", c.Self); Equal(await Call(Term.A("name")), Term.A("true")); Equal(runtime.WhereIs("name"), Term.A("undefined")); break;
                case "link": c.TrapExits = true; Equal(await Call(new Pid(runtime.Node, 999999)), Term.A("true")); var exit = await c.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.A("EXIT")) ? x : null, TimeSpan.Zero); Check(exit is not null && exit.Items[2].Equals(Term.A("noproc"))); break;
                case "unlink": var linked = runtime.Spawn(async x => { await x.ReceiveAsync(t => t); return Term.A("ok"); }, c, true); Equal(await Call(linked.Pid), Term.A("true")); runtime.Exit(c, linked.Pid, Term.A("boom")); Equal(await linked.Completion, Term.A("boom")); Check(runtime.IsAlive(c.Self)); break;
                case "monitor": var reference = await Call(Term.A("process"), new Pid(runtime.Node, 999999)); Check(reference is ReferenceTerm); var down = await c.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.A("DOWN")) ? x : null, TimeSpan.Zero); Check(down is not null && down.Items[1].Equals(reference) && down.Items[4].Equals(Term.A("noproc"))); break;
                case "demonitor": var monitored = runtime.Spawn(async x => { await x.ReceiveAsync(t => t); return Term.A("ok"); }); var monitor = runtime.Monitor(c, monitored.Pid); Equal(await Call(monitor), Term.A("true")); runtime.Exit(c, monitored.Pid, Term.A("kill")); await monitored.Completion; Check(c.Mailbox.Count == 0); break;
                case "process_flag": Equal(await Call(Term.A("trap_exit"), Term.A("true")), Term.A("false")); Check(c.TrapExits); break;
                case "exit" when export.Arity == 2: var target = runtime.Spawn(async x => { await x.ReceiveAsync(t => t); return Term.A("ok"); }); Equal(await Call(target.Pid, Term.A("kill")), Term.A("true")); Equal(await target.Completion, Term.A("killed")); break;
                case "exit": case "error": case "throw": try { await Call(Term.A("reason")); throw new InvalidOperationException("Expected exception"); } catch (ErlangException ex) { Equal(ex.Reason, Term.A("reason")); Check(ex.ExceptionClass == export.Function); } break;
                case "put": Equal(await Call(Term.A("k"), Term.I(1)), Term.A("undefined")); Equal(await Call(Term.A("k"), Term.I(2)), Term.I(1)); break;
                case "get": c.Dictionary[Term.A("k")] = Term.I(42); Equal(await Call(Term.A("k")), Term.I(42)); Equal(await Call(Term.A("missing")), Term.A("undefined")); break;
                case "erase": c.Dictionary[Term.A("k")] = Term.I(42); Equal(await Call(Term.A("k")), Term.I(42)); Check(!c.Dictionary.ContainsKey(Term.A("k"))); break;
                case "spawn": case "spawn_link": var selfObserved = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously); var fun = new FunctionTerm(0, (execution, a) => { selfObserved.SetResult(((ProcessContext)execution).Self); return ValueTask.FromResult<Term>(Term.A("ok")); }); var spawned = await Call(fun); Equal(await selfObserved.Task, spawned); Check(!spawned.Equals(c.Self)); break;
                case "spawn_monitor": var instant = new FunctionTerm(0, (execution, a) => ValueTask.FromResult<Term>(Term.A("ok"))); var pair = (TupleTerm)await Call(instant); Check(pair.Items[0] is Pid && pair.Items[1] is ReferenceTerm); var notification = await c.ReceiveAsync(t => t is TupleTerm x && x.Items.Count == 5 && x.Items[1].Equals(pair.Items[1]) ? x : null, TimeSpan.FromSeconds(1)); Check(notification is not null && notification.Items[4].Equals(Term.A("normal"))); break;
                case "map": var doubleFun = new FunctionTerm(1, (execution, a) => ValueTask.FromResult(CoreModules.Arithmetic("*", a[0], Term.I(2)))); Equal(await Call(doubleFun, Term.List(Term.I(1), Term.I(2))), Term.List(Term.I(2), Term.I(4))); break;
                case "format": Equal(await Call(Term.String("~s ~p~~ ~n"), Term.List(Term.String("hi"), Term.I(42))), Term.A("ok")); Check(output.ToString() == "hi 42~ \n"); break;
                default: throw new InvalidOperationException("Missing direct MFA test for " + export);
            }
            return Term.A("ok");
        });
        Equal(await process.Completion.WaitAsync(TimeSpan.FromSeconds(3)), Term.A("normal"));
    });


async Task<Term> MapError(string source)
{
    await using var runtime = new ProcessRuntime(); Term? reason = null;
    var expression = new Parser(source).ParseExpression(); Semantics.Validate(expression);
    var process = runtime.Spawn(async c =>
    {
        try { await Execution.EvaluateAsync(expression, c); throw new InvalidOperationException("Expected map error"); }
        catch (ErlangException ex) { Check(ex.ExceptionClass == "error"); reason = ex.Reason; }
        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal")); return reason!;
}
Test("compiler/map-empty-and-nonmap-pattern", async () => Equal(await Eval("case #{a => 1} of #{} -> case a of #{} -> wrong; _ -> ok end end"), Term.A("ok")));
Test("compiler/map-duplicate-last-wins", async () => Equal(await Eval("maps:get(a, #{a => 1, a => 42})"), Term.I(42)));
Test("compiler/map-exact-numeric-keys", async () => Equal(await Eval("case #{1 => integer, 1.0 => float, 0.0 => positive, -0.0 => negative} of #{1 := A, 1.0 := B, 0.0 := C, -0.0 := D} -> {A,B,C,D} end"), Term.Tuple(Term.A("integer"), Term.A("float"), Term.A("positive"), Term.A("negative"))));
Test("compiler/map-assoc-and-exact-update", async () => Equal(await Eval("case #{a => 1} of M -> N = M#{a := 2, b => 42}, {maps:get(a,M),maps:get(a,N),maps:get(b,N)} end"), Term.Tuple(Term.I(1), Term.I(2), Term.I(42))));
Test("compiler/map-mixed-duplicate-update", async () => Equal(await Eval("maps:get(a, #{}#{a => 1, a := 2, a => 3})"), Term.I(3)));
Test("compiler/map-chained-update", async () => Equal(await Eval("maps:get(a, #{}#{a => 1}#{a := 42})"), Term.I(42)));
Test("compiler/map-badkey", async () => Equal(await MapError("#{}#{missing := 1}"), Term.Tuple(Term.A("badkey"), Term.A("missing"))));
Test("compiler/map-exact-before-assoc-fails", async () => Equal(await MapError("#{}#{a := 1, a => 2}"), Term.Tuple(Term.A("badkey"), Term.A("a"))));
Test("compiler/map-badmap-empty-update", async () => Equal(await MapError("atom#{}"), Term.Tuple(Term.A("badmap"), Term.A("atom"))));
Test("compiler/map-error-expression-before-badmap", async () => Equal(await MapError("atom#{a := error(blurf)}"), Term.A("blurf")));
Test("compiler/map-error-expression-before-badkey", async () => Equal(await MapError("#{}#{a := error(blurf)}"), Term.A("blurf")));
Test("compiler/map-subset-nested-repeat", async () => Equal(await Eval("case #{a => {7,7}, extra => ok} of #{a := {X,X}} -> X; _ -> no end"), Term.I(7)));
Test("compiler/map-repeated-variable-reject", async () => Equal(await Eval("case #{a => 1,b => 1.0} of #{a := X,b := X} -> wrong; _ -> ok end"), Term.A("ok")));
Test("compiler/map-duplicate-pattern-keys", async () => Equal(await Eval("case #{a => 42} of #{a := X,a := Y} -> {X,Y} end"), Term.Tuple(Term.I(42), Term.I(42))));
Test("compiler/map-bound-key-and-guard-expression", async () => Equal(await Eval("case 1 of K -> case #{2 => 42} of #{K + 1 := V} -> V end end"), Term.I(42)));
Test("compiler/map-structured-key", async () => Equal(await Eval("case #{{a,[1]} => 42} of #{{a,[1]} := X} -> X end"), Term.I(42)));
Test("compiler/map-guard-key-error-rejects", async () => Equal(await Eval("case #{a => 1} of #{hd(atom) := X} -> wrong; _ -> ok end"), Term.A("ok")));
Test("compiler/map-construction-in-guard", async () => Equal(await Eval("case ok of X when #{a => 1} =:= #{a => 1} -> X end"), Term.A("ok")));
Test("compiler/map-update-guard-error-rejects", async () => Equal(await Eval("case #{} of M when M#{a := 1} =:= #{} -> wrong; _ -> ok end"), Term.A("ok")));
Test("compiler/map-unbound-key-diagnostic", () => { Throws<CompileException>(() => new Parser("-module(m). -export([f/2]). f(K, #{K := V}) -> V.").ParseModule()); return Task.CompletedTask; });
Test("compiler/map-sibling-binding-key-diagnostic", () => { Throws<CompileException>(() => Semantics.Validate(new Parser("case #{a => b,b => 1} of #{a := K,K := V} -> V end").ParseExpression())); return Task.CompletedTask; });
Test("compiler/map-assoc-pattern-diagnostic", () => { Throws<CompileException>(() => new Parser("case #{} of #{a => X} -> X end").ParseExpression()); return Task.CompletedTask; });
Test("compiler/map-exact-construction-diagnostic", () => { Throws<CompileException>(() => Semantics.Validate(new Parser("#{a := 1}").ParseExpression())); return Task.CompletedTask; });
Test("compiler/map-illegal-key-call-diagnostic", () => { Throws<CompileException>(() => Semantics.Validate(new Parser("case #{} of #{put(k,1) := X} -> X end").ParseExpression())); return Task.CompletedTask; });
Test("patterns/map-rollback", () => { var p = Parser.ToPattern(new Parser("#{a := X,b := X}").ParseExpression()); var b = new Dictionary<string,Term>(); Check(!p.Match(new MapTerm([new(Term.A("a"),Term.I(1)),new(Term.A("b"),Term.I(2))]), b)); Check(b.Count == 0); return Task.CompletedTask; });
Test("compiler/map-receive-preserves-unmatched", async () =>
{
    await using var runtime = new ProcessRuntime(); Term? value = null;
    var expression = new Parser("receive #{a := X,b := X} -> X after 1000 -> timeout end").ParseExpression(); Semantics.Validate(expression);
    var p = runtime.Spawn(async c => { value = await Execution.EvaluateAsync(expression,c); Check(c.Mailbox.Count == 1); return Term.A("ok"); });
    runtime.Send(p.Pid,new MapTerm([new(Term.A("a"),Term.I(1)),new(Term.A("b"),Term.I(2))]));
    runtime.Send(p.Pid,new MapTerm([new(Term.A("a"),Term.I(42)),new(Term.A("b"),Term.I(42))]));
    Equal(await p.Completion,Term.A("normal")); Equal(value!,Term.I(42));
});
Test("hybrid/map-case-directive-trivia", () => { var generated = CodeGeneration.Preprocess("class C { async Task F(ProcessContext erlangProcess) { var x = case #{a => 1} of #{a := X} -> X end. } }", "m.cs"); Check(generated.Contains("Expr.Map")); Check(generated.Contains("MapPatternField")); return Task.CompletedTask; });

Test("compiler/map-closure-captured-key", async () => Equal(await Eval("case a of K -> F = fun(#{K := V}) -> V end, F(#{a => 42}) end"), Term.I(42)));
Test("compiler/map-closure-key-value-shadow", async () => Equal(await Eval("case a of K -> F = fun(#{K := K}) -> K end, F(#{a => 42}) end"), Term.I(42)));
Test("compiler/map-context-guard-key", async () => Equal(await Eval("case #{self() => 42} of #{self() := V} -> V end"), Term.I(42)));
Test("hybrid/map-inline-receive", () => { var generated = CodeGeneration.Preprocess("class C { async Task F(ProcessContext erlangProcess) { var x = receive #{a := X} -> X end. } }", "m.cs"); Check(generated.Contains("MapPatternField")); return Task.CompletedTask; });
Test("hybrid/map-case-remote-call", () => { var generated = CodeGeneration.Preprocess("class C { async Task F(ProcessContext erlangProcess) { var x = case maps:get(a,#{a => 42}) of X -> X end. } }", "m.cs"); Check(generated.Contains("Expr.Case")); Check(generated.Contains("Expr.Map")); return Task.CompletedTask; });
Test("compiler/map-bifs-positive-negative-types", async () => Equal(await Eval("{is_map(#{}),is_map([]),map_size(#{a => 1,b => 2}),is_map_key(a,#{a => 1}),is_map_key(b,#{a => 1})}"), Term.Tuple(Term.A("true"),Term.A("false"),Term.I(2),Term.A("true"),Term.A("false"))));
Test("compiler/map-get-exact-key-badkey", async () => Equal(await MapError("map_get(1.0,#{1 => value})"), Term.Tuple(Term.A("badkey"),new FloatTerm(1))));
Test("compiler/map-get-badmap", async () => Equal(await MapError("map_get(key,not_map)"), Term.Tuple(Term.A("badmap"),Term.A("not_map"))));
Test("compiler/map-size-badmap", async () => Equal(await MapError("map_size([])"), Term.Tuple(Term.A("badmap"),Nil.Value)));
Test("compiler/is-map-key-badmap", async () => Equal(await MapError("is_map_key(key,42)"), Term.Tuple(Term.A("badmap"),Term.I(42))));
Test("compiler/map-key-signed-zero", async () => Equal(await Eval("{is_map_key(-0.0,#{0.0 => a}),is_map_key(0.0,#{0.0 => a}),map_get(-0.0,#{-0.0 => b})}"), Term.Tuple(Term.A("false"),Term.A("true"),Term.A("b"))));
Test("compiler/map-guard-bifs-qualified", async () => Equal(await Eval("case #{a => 42} of M when erlang:is_map(M), erlang:map_size(M) =:= 1, erlang:is_map_key(a,M), erlang:map_get(a,M) =:= 42 -> ok; _ -> no end"),Term.A("ok")));
Test("compiler/map-guard-missing-key-alternative", async () => Equal(await Eval("case #{} of M when map_get(a,M) =:= 42; map_size(M) =:= 0 -> ok; _ -> no end"),Term.A("ok")));
Test("compiler/map-guard-nonmap-rejection", async () => Equal(await Eval("case atom of M when map_size(M) =:= 0; is_map_key(a,M); map_get(a,M) =:= 1 -> no; _ -> ok end"),Term.A("ok")));
Test("compiler/map-pattern-key-map-get", async () => Equal(await Eval("case #{a => key} of Keys -> case #{key => 42} of #{map_get(a,Keys) := Value} -> Value end end"),Term.I(42)));
Test("compiler/map-pattern-key-map-get-failure", async () => Equal(await Eval("case #{} of Keys -> case #{key => 42} of #{map_get(a,Keys) := Value} -> no; _ -> ok end end"),Term.A("ok")));
Test("compiler/maps-module-not-guard-legal", () => { Throws<CompileException>(() => Semantics.Validate(new Parser("case #{} of M when maps:get(a,M) =:= 42 -> ok end").ParseExpression())); return Task.CompletedTask; });
Test("compiler/bits-empty-default-bytes", async () => { Equal(await Eval("<<>>"),new BitString([])); Equal(await Eval("<<1,2,255>>"),new BitString([1,2,255])); });
Test("compiler/bits-truncate-negative", async () => Equal(await Eval("<<511,-1,16:4,31:4>>"),new BitString([255,255,15])));
Test("compiler/bits-unaligned-concatenation", async () => Equal(await Eval("<<5:3,17:5,3:2>>"),new BitString([177,192],10)));
Test("compiler/bits-big-little-native", async () => { Equal(await Eval("<<4660:16/big>>"),new BitString([18,52])); Equal(await Eval("<<4660:16/little>>"),new BitString([52,18])); Equal(await Eval("<<4660:16/native>>"),new BitString(BitConverter.IsLittleEndian ? [52,18] : [18,52])); });
Test("compiler/bits-little-partial-octet", async () => Equal(await Eval("<<291:12/little>>"),new BitString([35,16],12)));
Test("compiler/bits-explicit-unit", async () => Equal(await Eval("<<4660:2/unit:8>>"),new BitString([18,52])));
Test("compiler/bits-string-literal", async () => Equal(await Eval("<<\"abc\">>"),new BitString([97,98,99])));
Test("compiler/bits-bound-expression-size", async () => Equal(await Eval("case 4 of S -> <<(2+3):(S+1)>> end"),new BitString([40],5)));
Test("compiler/bits-binary-prefix", async () => Equal(await Eval("<<(<<1,2,3>>):2/binary>>"),new BitString([1,2])));
Test("compiler/bits-bitstring-interpolation", async () => Equal(await Eval("<<1:1,(<<2:2>>)/bitstring,3:2>>"),new BitString([216],5)));
Test("compiler/bits-binary-unit-one", async () => Equal(await Eval("<<(<<1:1>>)/binary-unit:1>>"),new BitString([128],1)));
Test("compiler/bits-zero-size", async () => Equal(await Eval("<<-123:0,42:8>>"),new BitString([42])));
Test("compiler/bits-bad-value", async () => Equal(await MapError("<<atom>>"),Term.A("badarg")));
Test("compiler/bits-negative-size", async () => Equal(await MapError("<<1:-1>>"),Term.A("badarg")));
Test("compiler/bits-float-size", async () => Equal(await MapError("<<1:1.0>>"),Term.A("badarg")));
Test("compiler/bits-short-binary", async () => Equal(await MapError("<<(<<1>>):2/binary>>"),Term.A("badarg")));
Test("compiler/bits-binary-unit-alignment", async () => Equal(await MapError("<<(<<1:1>>)/binary>>"),Term.A("badarg")));
Test("compiler/bits-invalid-unit-diagnostic", () => { Throws<CompileException>(() => new Parser("<<1:8/unit:0>>").ParseExpression()); Throws<CompileException>(() => new Parser("<<1:8/unit:257>>").ParseExpression()); return Task.CompletedTask; });
Test("compiler/bits-unsupported-float-utf-diagnostic", () => { Throws<CompileException>(() => new Parser("<<1.0/float>>").ParseExpression()); Throws<CompileException>(() => new Parser("<<65/utf8>>").ParseExpression()); return Task.CompletedTask; });
Test("compiler/bits-duplicate-spec-diagnostic", () => { Throws<CompileException>(() => new Parser("<<1:8/big-little>>").ParseExpression()); return Task.CompletedTask; });
Test("compiler/bits-pattern-default", async () => Equal(await Eval("case <<42>> of <<X>> -> X end"), Term.I(42)));
Test("compiler/bits-pattern-signed", async () => Equal(await Eval("case <<255>> of <<X:8/signed>> -> X end"), Term.I(-1)));
Test("compiler/bits-pattern-unsigned", async () => Equal(await Eval("case <<255>> of <<X:8/unsigned>> -> X end"), Term.I(255)));
Test("compiler/bits-pattern-signed-little", async () => Equal(await Eval("case <<-257:16/little>> of <<X:16/signed-little>> -> X end"), Term.I(-257)));
Test("compiler/bits-pattern-little-partial", async () => Equal(await Eval("case <<291:12/little>> of <<X:12/little>> -> X end"), Term.I(291)));
Test("compiler/bits-pattern-native", async () => Equal(await Eval("case <<4660:16/native>> of <<X:16/native>> -> X end"), Term.I(4660)));
Test("compiler/bits-pattern-prior-size", async () => Equal(await Eval("case <<3,5:3,2:2>> of <<N, X:N, Rest/bitstring>> -> {X,Rest} end"), Term.Tuple(Term.I(5), new BitString([128],2))));
Test("compiler/bits-pattern-bound-size-expression", async () => Equal(await Eval("case 3 of N -> case <<17:5>> of <<X:(N+2)>> -> X end end"), Term.I(17)));
Test("compiler/bits-pattern-shadow-size", async () => Equal(await Eval("case 8 of L -> F = fun(<<L:L,B:L>>) -> B end, F(<<16:8,7:16>>) end"), Term.I(7)));
Test("compiler/bits-pattern-shadow-repeat", async () => Equal(await Eval("case 8 of L -> F = fun(<<L:L,B:L,L:L>>) -> B; (_) -> no end, F(<<16:8,7:16,16:16>>) end"), Term.I(7)));
Test("compiler/fun-clause-local-shadow-capture", async () => Equal(await Eval("case 42 of X -> F = fun({X}) -> X; (_) -> X end, F(atom) end"), Term.I(42)));
Test("compiler/fun-guard-fallback-capture", async () => Equal(await Eval("case 42 of X -> F = fun(X) when is_integer(X) -> X; (_) -> X end, F(atom) end"), Term.I(42)));
Test("compiler/fun-bit-clause-fallback-capture", async () => Equal(await Eval("case {8,42} of {L,B} -> F = fun(<<L:L,B:L>>) -> {L,B}; (_) -> {L,B} end, F(atom) end"), Term.Tuple(Term.I(8),Term.I(42))));
Test("compiler/bits-pattern-repeat-mismatch", async () => Equal(await Eval("case <<1,2>> of <<X,X>> -> wrong; _ -> ok end"), Term.A("ok")));
Test("compiler/bits-pattern-bound-value", async () => Equal(await Eval("case 42 of X -> case <<42>> of <<X>> -> ok; _ -> no end end"), Term.A("ok")));
Test("compiler/bits-pattern-short-extra-nonbits", async () => { foreach (string source in new[] { "<<1:7>>", "<<1,2>>", "atom" }) Equal(await Eval("case " + source + " of <<X:8>> -> wrong; _ -> ok end"), Term.A("ok")); });
Test("compiler/bits-pattern-zero-signed", async () => Equal(await Eval("case <<>> of <<X:0/signed>> -> X end"), Term.I(0)));
Test("compiler/bits-pattern-binary-prefix", async () => Equal(await Eval("case <<1,2,3>> of <<B:2/binary,T/binary>> -> {B,T} end"), Term.Tuple(new BitString([1,2]),new BitString([3]))));
Test("compiler/bits-pattern-unaligned-prefix", async () => Equal(await Eval("case <<1:1,5:3>> of <<_:1,B:3/bitstring>> -> B end"), new BitString([160],3)));
Test("compiler/bits-pattern-unit", async () => Equal(await Eval("case <<1,2>> of <<X:2/unit:8>> -> X end"), Term.I(258)));
Test("compiler/bits-pattern-rest-unit-mismatch", async () => Equal(await Eval("case <<1:1>> of <<B/binary>> -> wrong; _ -> ok end"), Term.A("ok")));
Test("compiler/bits-pattern-invalid-size-alternative", async () => { foreach (string size in new[] { "-1", "atom", "1.0", "999999999999999999999999", "(hd(atom))" }) Equal(await Eval("case <<1>> of <<X:" + size + ">> -> wrong; _ -> ok end"), Term.A("ok")); });
Test("compiler/bits-pattern-string-prefix", async () => Equal(await Eval("case <<\"OK\",42>> of <<\"OK\",X>> -> X end"), Term.I(42)));
Test("compiler/bits-pattern-literal-no-truncation", async () => Equal(await Eval("case <<0>> of <<256>> -> wrong; _ -> ok end"), Term.A("ok")));
Test("compiler/bits-pattern-signed-literal", async () => Equal(await Eval("case <<255>> of <<-1:8/signed>> -> ok; _ -> no end"), Term.A("ok")));
Test("compiler/bits-pattern-empty", async () => Equal(await Eval("case <<>> of <<>> -> ok; _ -> no end"), Term.A("ok")));
Test("compiler/bits-pattern-rollback", () => { var p = Parser.ToPattern(new Parser("<<N,1>>").ParseExpression()); var b = new Dictionary<string,Term>(); Check(!p.Match(new BitString([42,2]),b)); Check(b.Count == 0); return Task.CompletedTask; });
Test("compiler/bits-pattern-forward-size-diagnostic", () => { Throws<CompileException>(() => Semantics.Validate(new Parser("case <<1>> of <<X:N,N>> -> X end").ParseExpression())); return Task.CompletedTask; });
Test("compiler/bits-pattern-nonlast-rest-diagnostic", () => { Throws<CompileException>(() => new Parser("case <<1>> of <<B/binary,X>> -> X end").ParseExpression()); return Task.CompletedTask; });
Test("compiler/bits-pattern-size-call-diagnostic", () => { Throws<CompileException>(() => Semantics.Validate(new Parser("case <<1>> of <<X:(lists:sum([8]))>> -> X end").ParseExpression())); return Task.CompletedTask; });
Test("compiler/bits-pattern-nested-diagnostic", () => { Throws<CompileException>(() => new Parser("case <<1>> of <<(<<X>>)/binary>> -> X end").ParseExpression()); return Task.CompletedTask; });
Test("compiler/bits-pattern-map-value", async () => Equal(await Eval("case #{a => <<3,5:3>>} of #{a := <<N,X:N>>} -> X end"), Term.I(5)));
Test("compiler/bits-pattern-match-badmatch", async () => Equal(await MapError("<<X:16>> = <<1>>"),Term.Tuple(Term.A("badmatch"),new BitString([1]))));
Test("compiler/bits-pattern-receive-preserves-unmatched", async () =>
{
    await using var runtime = new ProcessRuntime();
    var process = runtime.Spawn(async ctx =>
    {
        ctx.Mailbox.Send(Term.A("earlier")); ctx.Mailbox.Send(new BitString([2,42])); ctx.Mailbox.Send(new BitString([1,7]));
        var expression = new Parser("receive <<1,X>> -> X after 0 -> no end").ParseExpression(); Semantics.Validate(expression);
        Equal(await Execution.EvaluateAsync(expression,ctx), Term.I(7));
        Equal((await ctx.ReceiveAsync(t => t,TimeSpan.Zero))!,Term.A("earlier"));
        Equal((await ctx.ReceiveAsync(t => t,TimeSpan.Zero))!,new BitString([2,42]));
        return Term.A("ok");
    });
    Equal(await process.Completion,Term.A("normal"));
});
Test("compiler/bits-guard-and-map-key", async () => Equal(await Eval("case #{<<1,2>> => 42} of #{<<1,2>> := V} when <<1:3>> =:= <<1:3>> -> V end"),Term.I(42)));
Test("compiler/bits-unit-without-size-diagnostic", () => { Throws<CompileException>(() => new Parser("<<1/unit:8>>").ParseExpression()); return Task.CompletedTask; });
Test("compiler/bits-string-modifier-pending-diagnostic", () => { Throws<CompileException>(() => new Parser("<<\"ab\":16/little>>").ParseExpression()); return Task.CompletedTask; });
Test("compiler/bits-identical-specifiers", async () => Equal(await Eval("<<1:8/integer-integer-big-big-unit:1-unit:1>>"),new BitString([1])));
Test("compiler/bits-alias-specifier-unit-conflict", () => { Throws<CompileException>(() => new Parser("<<(<<1>>)/bytes-unit:1>>").ParseExpression()); Throws<CompileException>(() => new Parser("<<(<<1>>)/unit:8-bits>>").ParseExpression()); return Task.CompletedTask; });
Test("compiler/bits-explicit-all-binary", async () => Equal(await Eval("<<(<<1,2>>):all/binary>>"),new BitString([1,2])));
Test("compiler/bit-size-byte-rounding", async () => Equal(await Eval("{bit_size(<<>>),byte_size(<<>>),bit_size(<<1:1>>),byte_size(<<1:1>>),bit_size(<<1:8>>),byte_size(<<1:8>>),bit_size(<<1:9>>),byte_size(<<1:9>>)}"), Term.Tuple(Term.I(0),Term.I(0),Term.I(1),Term.I(1),Term.I(8),Term.I(1),Term.I(9),Term.I(2))));
Test("compiler/bitstring-predicate-distinction", async () => Equal(await Eval("{is_bitstring(<<>>),is_binary(<<>>),is_bitstring(<<1:1>>),is_binary(<<1:1>>),is_bitstring([])}"), Term.Tuple(Term.A("true"),Term.A("true"),Term.A("true"),Term.A("false"),Term.A("false"))));
Test("compiler/bit-size-invalid-badarg", async () => Equal(await MapError("bit_size(atom)"),Term.A("badarg")));
Test("compiler/byte-size-invalid-badarg", async () => Equal(await MapError("byte_size([1,2])"),Term.A("badarg")));
Test("compiler/bit-bifs-qualified-guard", async () => Equal(await Eval("case <<1:9>> of Bits when erlang:is_bitstring(Bits), erlang:bit_size(Bits) =:= 9, erlang:byte_size(Bits) =:= 2 -> ok; _ -> no end"),Term.A("ok")));
Test("compiler/bit-bif-guard-failure-alternative", async () => Equal(await Eval("case atom of X when bit_size(X) =:= 0; byte_size(X) =:= 0; is_atom(X) -> ok end"),Term.A("ok")));
Test("compiler/bit-bif-map-pattern-key", async () => Equal(await Eval("case <<1:9>> of B -> case #{2 => 42} of #{byte_size(B) := X} -> X end end"),Term.I(42)));
var results = new List<object>();
int failed = 0;
foreach (var test in tests)
{
    var watch = Stopwatch.StartNew(); try { await test.Body().WaitAsync(TimeSpan.FromSeconds(15)); Console.WriteLine("PASS " + test.Name); results.Add(new { test.Name, Status = "Passed", Milliseconds = watch.ElapsedMilliseconds }); }
    catch (Exception ex) { failed++; Console.WriteLine("FAIL " + test.Name + ": " + ex); results.Add(new { test.Name, Status = "Failed", Error = ex.ToString(), Milliseconds = watch.ElapsedMilliseconds }); }
}
Console.WriteLine($"{tests.Count - failed}/{tests.Count} passed");
string? report = args.FirstOrDefault(); if (report is not null) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report))!); await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { Passed = tests.Count - failed, Failed = failed, Tests = results }, new JsonSerializerOptions { WriteIndented = true })); }
if (failed == 0 && args.Length > 1) await File.WriteAllTextAsync(args[1], JsonSerializer.Serialize(exportRegistry.Exports.Select(e => new { e.Module, e.Function, e.Arity, Status = "Partially compatible", Evidence = $"mfa/{e.Module}:{e.Function}/{e.Arity}", Limits = "Single direct contract case plus feature regressions; complete error/options and OTP differential verification pending" }), new JsonSerializerOptions { WriteIndented = true }));
return failed == 0 ? 0 : 1;

internal sealed class CounterServer : IGenServer
{
    public bool Terminated { get; private set; }
    public ValueTask<Term> Init(ProcessContext context, Term arguments) => arguments.Equals(Term.A("fail")) ? throw new ErlangException("init_failed") : ValueTask.FromResult(arguments);
    public ValueTask<ServerResult> HandleCall(ProcessContext context, Term request, ServerFrom from, Term state)
        => request.Equals(Term.A("crash")) ? throw new ErlangException("boom") : ValueTask.FromResult(request.Equals(Term.A("stop")) ? new ServerResult(state, Term.A("ok"), Term.A("normal")) : new ServerResult(state, request.Equals(Term.A("noreply")) ? null : state));
    public ValueTask<ServerResult> HandleCast(ProcessContext context, Term request, Term state) => ValueTask.FromResult(new ServerResult(CoreModules.Arithmetic("+", state, request)));
    public ValueTask<ServerResult> HandleInfo(ProcessContext context, Term message, Term state) => ValueTask.FromResult(new ServerResult(CoreModules.Arithmetic("+", state, message)));
    public ValueTask Terminate(ProcessContext context, Term reason, Term state) { Terminated = true; return ValueTask.CompletedTask; }
}
