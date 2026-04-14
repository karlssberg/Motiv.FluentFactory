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
CVJG0013 | Converj | Error | FluentRoot type missing partial modifier
CVJG0014 | Converj | Warning | Inaccessible parameter type in FluentRoot
CVJG0015 | Converj | Warning | FluentRoot accessibility exceeds target type
CVJG0016 | Converj | Error | Ambiguous fluent method chain
CVJG0017 | Converj | Warning | Empty TerminalVerb with TerminalMethod.None
CVJG0018 | Converj | Error | Invalid MethodPrefix
CVJG0019 | Converj | Error | Target type not assignable to ReturnType
CVJG0020 | Converj | Warning | ReturnType equals concrete target type
CVJG0021 | Converj | Error | ReturnType specified with TerminalMethod.None
CVJG0022 | Converj | Error | Optional parameters cause ambiguous fluent method chain
CVJG0023 | Converj | Error | Conflicting type constraints produce duplicate method signatures
CVJG0024 | Converj | Error | Custom step has no storage for constructor parameter
CVJG0025 | Converj | Warning | FluentParameter on type without FluentRoot
CVJG0026 | Converj | Warning | FluentParameter on static FluentRoot type
CVJG0027 | Converj | Error | FluentParameter property has no getter
CVJG0028 | Converj | Error | Duplicate FluentParameter mapping
CVJG0029 | Converj | Error | FluentParameter type mismatch
CVJG0030 | Converj | Warning | FluentParameter has no matching target parameter
CVJG0031 | Converj | Error | FluentParameter partial overlap
CVJG0032 | Converj | Info | FluentParameter overrides FluentMethod
CVJG0033 | Converj | Error | Static/instance method name collision
CVJG0035 | Converj | Error | FluentStorage property has no getter
CVJG0036 | Converj | Error | Duplicate FluentStorage mapping
CVJG0037 | Converj | Error | Multiple FluentTargets with TerminalMethod.None
CVJG0038 | Converj | Error | FluentMethod on property without setter
CVJG0039 | Converj | Warning | Property support excluded from TerminalMethod.None
CVJG0040 | Converj | Error | Property name clashes with constructor parameter
CVJG0041 | Converj | Error | Duplicate fluent property method name
CVJG0042 | Converj | Info | FluentMethod without name has no effect on parameter
CVJG0043 | Converj | Error | Ambiguous entry method
CVJG0044 | Converj | Warning | FluentTarget on instance method
CVJG0045 | Converj | Warning | Tuple parameter has unnamed elements
CVJG0046 | Converj | Error | [This] must be on the first parameter
CVJG0047 | Converj | Error | [This] is not supported on instance methods
CVJG0048 | Converj | Info | [This] is redundant on extension method parameter
CVJG0049 | Converj | Error | FluentRoot must be static for extension method targets
CVJG0050 | Converj | Error | FluentCollectionMethod on non-collection parameter
CVJG0051 | Converj | Error | Cannot derive accumulator method name
CVJG0052 | Converj | Error | Accumulator method name collision
