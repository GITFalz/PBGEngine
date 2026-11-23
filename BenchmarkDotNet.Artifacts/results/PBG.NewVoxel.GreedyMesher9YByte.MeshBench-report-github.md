```

BenchmarkDotNet v0.15.8, Linux Pop!_OS 22.04 LTS
AMD Ryzen 5 5600G with Radeon Graphics 3.49GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.203
  [Host] : .NET 9.0.4 (9.0.4, 9.0.425.16305), X64 RyuJIT x86-64-v3
  Dry    : .NET 9.0.4 (9.0.4, 9.0.425.16305), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method | Mean     | Error | Allocated |
|------- |---------:|------:|----------:|
| Mesh   | 13.63 ms |    NA |     144 B |
