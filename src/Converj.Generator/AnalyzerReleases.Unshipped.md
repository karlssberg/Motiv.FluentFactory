; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
CVJG0001 | Converj | Error | Unreachable fluent constructor
CVJG0002 | Converj | Warning | Multiple fluent method contains superseded method
CVJG0003 | Converj | Warning | Fluent method template not compatible
CVJG0004 | Converj | Error | All fluent method template incompatible
CVJG0005 | Converj | Error | Fluent method template not static
CVJG0006 | Converj | Info | Fluent method template superseded
CVJG0007 | Converj | Error | Invalid TerminalVerb
CVJG0008 | Converj | Error | Duplicate terminal method name
CVJG0009 | Converj | Error | FluentTarget root type missing FluentRoot attribute
CVJG0010 | Converj | Error | TerminalVerb specified with TerminalMethod.None
CVJG0011 | Converj | Warning | Unsupported parameter modifier
CVJG0012 | Converj | Warning | Inaccessible constructor
CVJG0013 | Converj | Error | Root type missing partial modifier
CVJG0014 | Converj | Warning | Inaccessible parameter type in fluent builder
CVJG0015 | Converj | Warning | Root accessibility exceeds target type
CVJG0016 | Converj | Error | Ambiguous fluent method chain
CVJG0017 | Converj | Warning | Empty TerminalVerb with TerminalMethod.None
CVJG0044 | Converj | Warning | FluentTarget on instance method
