# CSharpExtensions



## Analyzers

|Rule|Description| Related attributes|Documentation|
|----|-----------|-------------------|-------------|
|CSE001|Required properties initialization| `[InitRequired]` | [link](https://cezarypiatek.github.io/post/immutable-types-with-roslyn/#convenient-initialization) |
|CSE002|InitOnly member modification |`[InitOnly]`, `[InitOnlyOptional]`| [link](https://cezarypiatek.github.io/post/immutable-types-with-roslyn/#full-immutability) |
|CSE003|Type should have the same fields as twin type| `[TwinType]` | [link](http://localhost:1313/post/csharp-twin-types/#the-solution-extending-c-rules-with-custom-analyzer)|
|CSE004| Member with InitOnlyOptional requires default value| `[InitOnlyOptional]` ||
