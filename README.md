# CSharpExtensions

Nuget packge: https://www.nuget.org/packages/SmartAnalyzers.CSharpExtensions.Annotations/

Articles that explain implemented concepts:
- [Immutable types in C# with Roslyn](https://cezarypiatek.github.io/post/immutable-types-with-roslyn/)
- [Improving non-nullable reference types handling](https://cezarypiatek.github.io/post/better-non-nullable-handling/)
- [Twin types - properties synchronization without inheritance](https://cezarypiatek.github.io/post/csharp-twin-types/)
- [Pure functions and unused return values](https://cezarypiatek.github.io/post/pure-functions-and-unused-return-value/)

## Analyzers

|Rule|Description| Related attributes|Documentation|
|----|-----------|-------------------|-------------|
|CSE001|Required properties initialization| `[InitRequired]`, `[InitRequiredForNotNull]` | [link](https://cezarypiatek.github.io/post/immutable-types-with-roslyn/#convenient-initialization) |
|CSE002|InitOnly member modification |`[InitOnly]`, `[InitOnlyOptional]`| [link](https://cezarypiatek.github.io/post/immutable-types-with-roslyn/#full-immutability) |
|CSE003|Type should have the same fields as twin type| `[TwinType]` | [link](https://cezarypiatek.github.io/post/csharp-twin-types/#the-solution-extending-c-rules-with-custom-analyzer)|
|CSE004| Member with InitOnlyOptional requires default value| `[InitOnlyOptional]` ||
|CSE005| Return value unused | | [link](https://cezarypiatek.github.io/post/pure-functions-and-unused-return-value/)|
|CSE006| Expression too complex | | |
|CSE007| Return disposable value unused | | Same as `SE005` but tracks only `IDisposable` and `IAsyncDisposable` values |
|CSE008| Return async result unused | | Same as `SE005` but tracks only `IAsyncResult` (`Task`, `ValueTask`) values |


## Configuration

Add `CSharpExtensions.json` file with custom configuration:

```json
{
  "CSE005": {
    "IgnoredReturnTypes": [ 
        "Microsoft.Extensions.DependencyInjection.IServiceCollection",
        "Microsoft.Extensions.Configuration.IConfigurationBuilder",
        "Microsoft.Extensions.Logging.ILoggingBuilder"
        ] 
  } 
}
```

Include config file as `AdditionalFile` in `csproj`:

```xml
<ItemGroup>
    <AdditionalFiles Include="CSharpExtensions.json" />
</ItemGroup>
```

## Migrating from Constructors to Initialization Blocks

**Step 1:** Configure `CSharpExtensions` to report `CSE001` even when constructor invocation is present

```json
{
  "CSE001": {
    "SkipWhenConstructorUsed":  false 
  } 
}
```

**Step 2:** Execute `"Try to move initialization from constructor to init block"` CodeFix for `CSE001` occurrences.

![](/doc/constructor_to_init.gif)

## Init-related attributes vs [init keyword](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init) (C# 9+ feature)

|Feature | Required in init block | Can be modified outside init block| Application level|
|----|----|---|---|
| `init` keyword | NO | NO| Property|
|`[InitRequired]`| YES | YES | Property. Class|
|`[InitOnly]`| YES | NO | Property. Class|
|`[InitOnlyOptional]`| NO | NO | Property. Class|
|`[InitRequiredForNotNull]`| YES for non-null references and not `Nullable<T>` | YES | assembly|
