![Icon](https://raw.githubusercontent.com/Scooletz/Thruster/master/package_icon.png)

# Thruster
Thruster is a fast and efficient implementation of a MemoryPool&lt;T>

## Design principles
Thruster is based on (some) understanding on nowadays CPUs and low level APIs provided by .NET. The most important desing principles are:
- padding - expanding an object or an array, to reduce a possibility of using the same CPU cache line by threads being run on different CPUs
- region memory mangagement - leasing chunks of memory from a specific region, which allows use of in-region addressing (offset, region number)
- no locks - for claiming and releasing memory when no resize is needed
- efficient data encoding - bitmasks used for leasing, are designed to use 1 `CAS` operation for renting (when no collisions) and one, branchless unconditional `Interlocked.Add` to release

## Benchmarks
Some single threaded benchmarks run with awesome BenchmarkDotNet. The nesting level represents multiple `.Rent` operations.

``` ini

BenchmarkDotNet=v0.11.0, OS=Windows 10.0.16299.547 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-6700HQ CPU 2.60GHz (Max: 2.20GHz) (Skylake), 1 CPU, 8 logical and 4 physical cores
Frequency=2531250 Hz, Resolution=395.0617 ns, Timer=TSC
.NET Core SDK=2.1.302
  [Host]     : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT


```
|                Method |      Mean |    Error |   StdDev |
|---------------------- |----------:|---------:|---------:|
|       Thruster_1_Rent |  74.54 ns | 1.557 ns | 4.048 ns |
|Thruster_2_nested_Rents | 139.32 ns | 2.765 ns | 2.958 ns |
|Thruster_3_nested_Rents | 207.78 ns | 3.479 ns | 3.255 ns |
|         Shared_1_Rent |  91.73 ns | 1.547 ns | 1.447 ns |
| Shared_2_nested_Rents | 287.89 ns | 5.087 ns | 4.510 ns |
| Shared_3_nested_Rents | 408.50 ns | 7.217 ns | 6.751 ns |

Multi threaded app depends on the nesting level even more. If there's no nesting, results are comparable. Any nesting will drastically point towards using Thruster.

## Icon

[Rocket Ship](https://thenounproject.com/term/rocket-ship/152486/) designed by [Joy Thomas](https://thenounproject.com/jthomas/) from The Noun Project
