; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MFFG0001 | FluentFactory | Error | Unreachable fluent constructor
MFFG0002 | FluentFactory | Warning | Multiple fluent method contains superseded method
MFFG0003 | FluentFactory | Warning | Fluent method template not compatible
MFFG0004 | FluentFactory | Error | All fluent method template incompatible
MFFG0005 | FluentFactory | Error | Fluent method template not static
MFFG0006 | FluentFactory | Info | Fluent method template superseded
MFFG0007 | FluentFactory | Error | Invalid CreateVerb
MFFG0008 | FluentFactory | Error | Duplicate create method name
MFFG0009 | FluentFactory | Error | FluentConstructor target type missing FluentFactory attribute
MFFG0010 | FluentFactory | Error | CreateVerb specified with CreateMethod.None
MFFG0011 | FluentFactory | Warning | Unsupported parameter modifier
MFFG0012 | FluentFactory | Warning | Inaccessible constructor
MFFG0013 | FluentFactory | Error | Factory type missing partial modifier
MFFG0014 | FluentFactory | Warning | Inaccessible parameter type in fluent factory
MFFG0015 | FluentFactory | Warning | Factory accessibility exceeds target type
