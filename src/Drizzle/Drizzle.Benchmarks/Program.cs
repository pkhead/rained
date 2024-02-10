using System.Diagnostics;
using System.Reflection;
using BenchmarkDotNet.Running;
using Drizzle.Benchmarks;

var benchmark = new ImageQuadCopy();
benchmark.Setup();

for (var i = 0; i < 10; i++)
{
    benchmark.Bench();
}

var sw = new Stopwatch();
for (var i = 0; i < 30; i++)
{
    sw.Restart();
    benchmark.Bench();
    sw.Stop();
    Console.WriteLine(sw.Elapsed);
}
