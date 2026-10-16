from pathlib import Path
import hashlib,json
root=Path('artifacts/proc-lib337-candidate/src/Erlang.Otp.Behaviours'); out=Path('artifacts/proc-lib-format-raw-fix'); (out/'source').mkdir(exist_ok=True);(out/'before').mkdir(exist_ok=True)
names=['ProcLibReportFormatting.cs','ProcLibReportFormatNames.cs','ProcLibReportExceptionFormatter.cs','ProcLibReportExceptionFormatNames.cs']
rows=[]
for name in names:
 b=(root/name).read_bytes();(out/'before'/name).write_bytes(b);t=b.decode('utf-8')
 if name=='ProcLibReportFormatting.cs':
  for constant in ['ModuleSeparator','AritySeparator']:
   old=f'Term.String(ProcLibReportFormatNames.{constant})';new=f'Term.I(ProcLibReportFormatNames.{constant}Character)';assert t.count(old)==1;t=t.replace(old,new)
 if name=='ProcLibReportFormatNames.cs':
  marker='    public const string ModuleSeparator = ":";';assert marker in t;t=t.replace(marker,marker+"\n    public const char ModuleSeparatorCharacter = ':';\n    public const char AritySeparatorCharacter = '/';")
 if name=='ProcLibReportExceptionFormatter.cs':
  t=t.replace('S(ProcLibReportExceptionFormatNames.StackIndent)','StackIndent()')
  old='Spaces(checked((int)Measure(prefix, Term.A(ProcLibReportControlNames.Unicode))))';assert t.count(old)==1;t=t.replace(old,'Padding(checked((int)Measure(prefix, Term.A(ProcLibReportControlNames.Unicode))))')
  old='Term text = printer.Render(';assert t.count(old)==1;t=t.replace(old,'Term rendered = printer.Render(')
  old='        return (text, Subtract(limit, Measure(text, encoding)));';assert t.count(old)==1;t=t.replace(old,'        Term text = Term.List(rendered);\n\n'+old)
  marker='    private static Term Spaces(int count) => S(new string(\' \', count));';assert marker in t
  t=t.replace(marker,marker+'''

    private static Term StackIndent()
        => new Cons(S(ProcLibReportExceptionFormatNames.StackIndentPrefix), S(ProcLibReportExceptionFormatNames.ReasonIndent));

    private static Term Padding(int count)
    {
        if (count <= ProcLibReportExceptionFormatNames.FlatPaddingMaximum)
            return Spaces(count);
        Term half = Padding(count / ProcLibReportExceptionFormatNames.PaddingDivisor);

        return count % ProcLibReportExceptionFormatNames.PaddingDivisor == 0
            ? new Cons(half, half)
            : new Cons(Term.I(ProcLibReportExceptionFormatNames.PaddingCharacter), new Cons(half, half));
    }
''')
 if name=='ProcLibReportExceptionFormatNames.cs':
  marker='    public const string StackIndent = "      ";';assert marker in t;t=t.replace(marker,marker+'''
    public const string StackIndentPrefix = "  ";
    public const char PaddingCharacter = ' ';
    public const int FlatPaddingMaximum = 3;
    public const int PaddingDivisor = 2;''')
 data=t.replace('\r\n','\n').encode();(out/'source'/name).write_bytes(data);rows.append(dict(Path=name,ParentBytes=len(b),ParentSha256=hashlib.sha256(b).hexdigest().upper(),Bytes=len(data),Sha256=hashlib.sha256(data).hexdigest().upper()))
freeze=dict(Status='SOURCE_PROPOSAL_RAW_IOLIST_SHAPE_FIX',Files=rows,PinnedOtp='ad05823719d77c8faee87348ea39513d4e2f99c5',OriginalComparison='artifacts/root-formatter-original-initial-comparison.json',SdkExecuted=False,MainChanged=False,BusyCandidateChanged=False,RequiredValidation='Repeat all76 exact decoded term comparisons; no flattening of raw fixtures.')
(out/'source-freeze.json').write_text(json.dumps(freeze,indent=2)+'\n',encoding='utf-8');print(hashlib.sha256((out/'source-freeze.json').read_bytes()).hexdigest().upper())
