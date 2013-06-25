using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelTasks
{
    class Program
    {
        static void Main()
        {
            var list = Enumerable.Range(1, 100000).ToList();

            var time0 = Timer.Execute(() =>
            {
                var task = Calculate(list);
                Console.WriteLine("Sum: " + task);
            });
            Console.WriteLine("Elapsed (ms): " + time0);

            var time1 = Timer.Execute(() =>
                {
                    var task = CalculateAsync(list);
                    Console.WriteLine("Sum: " + task.Result);                    
                });
            Console.WriteLine("Elapsed via Async (ms): " + time1);

            var time2 = Timer.Execute(() =>
            {
                var task = CalculateParallel(list);
                Console.WriteLine("Sum: " + task);
            });
            Console.WriteLine("Elapsed via Parallelization (ms): " + time2);

            var time3 = Timer.Execute(() =>
            {
                var task = CalculateAsyncAndParallel(list);
                Console.WriteLine("Sum: " + task.Result);
            });
            Console.WriteLine("Elapsed via Async Parallelization (ms): " + time3);

            Console.Read();
        }

        private static long Calculate(List<int> list)
        {
            var sum = 0;
            foreach (var i in list)
            {
                var current = sum;
                sum =+ i + current;
            }
            return sum;
        }

        private async static Task<long> CalculateAsync(List<int> list)
        {
            var sum = 0;
            foreach (var i in list)
            {
                var current = sum;
                sum =+ await Task.Factory.StartNew(() => i + current);
            }
            return sum;
        }

        private static long CalculateParallel(List<int> list)
        {
            var sum = 0;
            Parallel.ForEach(list, i =>
                {
                    var current = sum;
                    sum =+  i + current;
                });
            return sum;
        }

        private async static Task<long> CalculateAsyncAndParallel(List<int> list)
        {
            var sum = 0;
            await list.ForEachAsync(list.Count(), async i =>
                {
                    var current = sum;
                    sum =+ await Task.Factory.StartNew(() => i + current);
                });
            return sum;
        }
    }

    public static class Timer
    {
        public static long Execute(Action timedAction, Action<long> reportAction)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();

            stopwatch.Start();
            timedAction();
            stopwatch.Stop();

            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            if (reportAction != null)
            {
                reportAction(elapsedMilliseconds);
            }

            return elapsedMilliseconds;
        }

        public static long Execute(Action timedAction)
        {
            return Timer.Execute(timedAction, null);
        }
    }

    public static class LoopExtensions
    {
        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }
    }
}
