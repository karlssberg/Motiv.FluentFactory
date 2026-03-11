# Deferred Items - Phase 07

## Pre-existing Build Failures (from 07-01 refactoring)

The 07-01 plan commits (0288400, 84ebf9d, 286be64) introduced references to symbols that don't exist yet:
- `FluentMethodTemplateSuperseded` - referenced in test files but not defined
- `ContainsSupersededFluentMethodTemplate` - referenced in test files but not defined
- `UnreachableConstructor` - referenced in test files but not defined
- `IncompatibleFluentMethodTemplate` - referenced in FluentFactoryGenerator but moved/renamed incorrectly
- `AllFluentMethodTemplatesIncompatible` - referenced in FluentModelFactory but not available
- `FluentConstructorTargetTypeMissingFluentFactory` - referenced in FluentConstructorValidator but not available
- Multiple RS1019 warnings treated as errors due to diagnostic ID conflicts between FluentFactoryGenerator and FluentDiagnostics

These prevent a clean build+test from succeeding. They are NOT caused by the 07-02 changes.
