; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
CVJG0001 | FluentFactory | Error | Unreachable fluent constructor
CVJG0002 | FluentFactory | Warning | Multiple fluent method contains superseded method
CVJG0003 | FluentFactory | Warning | Fluent method template not compatible
CVJG0004 | FluentFactory | Error | All fluent method template incompatible
CVJG0005 | FluentFactory | Error | Fluent method template not static
CVJG0006 | FluentFactory | Info | Fluent method template superseded
CVJG0007 | FluentFactory | Error | Invalid CreateVerb
CVJG0008 | FluentFactory | Error | Duplicate create method name
CVJG0009 | FluentFactory | Error | FluentConstructor target type missing FluentFactory attribute
CVJG0010 | FluentFactory | Error | CreateVerb specified with CreateMethod.None
CVJG0011 | FluentFactory | Warning | Unsupported parameter modifier
CVJG0012 | FluentFactory | Warning | Inaccessible constructor
CVJG0013 | FluentFactory | Error | Factory type missing partial modifier
CVJG0014 | FluentFactory | Warning | Inaccessible parameter type in fluent factory
CVJG0015 | FluentFactory | Warning | Factory accessibility exceeds target type
CVJG0016 | FluentFactory | Error | Ambiguous fluent method chain
CVJG0017 | FluentFactory | Warning | Empty CreateVerb with CreateMethod.None
