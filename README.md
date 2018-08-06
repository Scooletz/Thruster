![Icon](https://raw.githubusercontent.com/Scooletz/Thruster/master/package_icon.png)

# Thruster
Thruster is a fast and efficient implementation of a MemoryPool&lt;T>

## Design principles
Thruster is based on (some) understanding on nowadays CPUs and low level APIs provided by .NET. The most important desing principles are:
- padding - expanding an object or an array, to reduce a possibility of using the sampe CPU cache line by threads being run on different CPUs
- region memory mangagement - leasing chunks of memory from a specific region, which allows using an in-region addressing (offset, region number)
- no locks - for claiming and releasing memory when no resize is needed
- efficient data encoding - bitmasks used for leasing, are designed to use 1 `CAS` operation for renting (when no collisions) and one, branchless unconditional `Interlocked.Add` to release

## Icon

[Rocket Ship](https://thenounproject.com/term/rocket-ship/152486/) designed by [Joy Thomas](https://thenounproject.com/jthomas/) from The Noun Project
