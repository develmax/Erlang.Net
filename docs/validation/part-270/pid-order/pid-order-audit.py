import subprocess,pathlib,json,hashlib
root=pathlib.Path.cwd(); out=root/'artifacts/pid-order-evidence';out.mkdir(exist_ok=True)
for source,ranges in [('erts/emulator/beam/utils.c',[(1714,1726),(1777,1812),(2217,2243)]),('erts/emulator/beam/external.c',[])]:
 data=subprocess.check_output(['git','-C','../otp','show','OTP-29.1.1:'+source]);(out/pathlib.Path(source).name).write_bytes(data)
 if ranges:
  lines=data.decode().splitlines();(out/'ordering-source-excerpts.txt').write_text('\n\n'.join('\n'.join(f'{i+1}: {lines[i]}' for i in range(a-1,b)) for a,b in ranges))
p=root/'artifacts/pid-order-prototype/src/Erlang.Terms/TermOrder.cs';old=p.read_text()
needle='''            case (Pid x, Pid y):
                return Identity('''
new=old.replace(needle,'''            case (Pid x, Pid y):
                return PidIdentity(''',1)
new=new.replace('''            case (PortTerm x, PortTerm y):
                return Identity(''','''            case (PortTerm x, PortTerm y):
                return PortIdentity(''',1)
new=new.replace('''    private static int Identity(''','''    private static int PidIdentity(
        string an,
        uint ac,
        ulong ai,
        string bn,
        uint bc,
        ulong bi
    )
    {
        int c = ai.CompareTo(bi);
        if (c != 0)
            return c;
        c = Codepoints(an, bn);

        return c != 0 ? c : ac.CompareTo(bc);
    }

    private static int PortIdentity(''',1)
new=new.replace('int c = string.CompareOrdinal(an, bn);','int c = Codepoints(an, bn);',1)
(out/'TermOrder.before.cs').write_text(old);(out/'TermOrder.after.cs').write_text(new)
(out/'source-audit.json').write_text(json.dumps({'Pin':subprocess.check_output(['git','-C','../otp','rev-parse','OTP-29.1.1^{commit}']).decode().strip(),'PidPackedRepresentation':'ExternalTermFormat reader id | ((ulong)serial << EtfPidLayout.SerialBitShift); SerialBitShift32. Unsigned ulong ordering therefore serial then number.','PidNativeOrder':'utils.c pid_common serial, number, CMP_NODES atom codepoint ordering then uint32 creation','PortNativeOrder':'external-port CMP_NODES then unsigned uint64 number; distinct from PID','OwnedPaths':['src/Erlang.Terms/TermOrder.cs','tools/Erlang.Differential/CompiledModuleCases.cs','tools/Erlang.Differential/IdentifierOrderCases.cs'],'NativeExecuted':False},indent=2))
print('prepared exact old/new source and original excerpts')
