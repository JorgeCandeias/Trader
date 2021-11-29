using Outcompute.Trader.Core.Tasks.Dataflow;
using System.Threading.Tasks.Dataflow;
using Xunit;

namespace Outcompute.Trader.Core.Tests
{
    public class BackpressureActionBlockTests
    {
        [Fact]
        public async Task CompletesWithEmpty()
        {
            // arrange
            var result = new List<IEnumerable<int>>();
            var block = new BackpressureActionBlock<int>(x => { result.Add(x); });

            // act
            block.Complete();

            // assert
            await block.Completion;
            Assert.Empty(result);
        }

        [Fact]
        public async Task CompletesWithOne()
        {
            // arrange
            var result = new List<IEnumerable<int>>();
            var block = new BackpressureActionBlock<int>(x => { result.Add(x); });

            // act
            block.Post(1);
            block.Complete();

            // assert
            await block.Completion;
            Assert.Collection(result,
                items =>
                {
                    Assert.Collection(items,
                        x =>
                        {
                            Assert.Equal(1, x);
                        });
                });
        }

        [Fact]
        public async Task SyncActionHandlesScenario()
        {
            // arrange
            var result = new List<IEnumerable<int>>();
            var semaphore = new SemaphoreSlim(0);
            var completed = new SemaphoreSlim(0);
            var block = new BackpressureActionBlock<int>(x =>
            {
                result.Add(x);

                completed.Release();
                semaphore.Wait();
            });

            // act - post 1 item - this will generate the first batch
            block.Post(1);
            await completed.WaitAsync();

            // act - post 2 items and release the first batch - these will queue up and form the second batch
            block.Post(2);
            block.Post(3);
            semaphore.Release();
            await completed.WaitAsync();

            // act - post 3 items and release the second batch - these will queue up and form the third batch
            block.Post(4);
            block.Post(5);
            block.Post(6);
            semaphore.Release();
            await completed.WaitAsync();

            // act - release the third batch
            semaphore.Release();
            block.Complete();

            // assert
            await block.Completion;
            Assert.Collection(result,
                items =>
                {
                    Assert.Collection(items,
                        x =>
                        {
                            Assert.Equal(1, x);
                        });
                },
                items =>
                {
                    Assert.Collection(items,
                        x => Assert.Equal(2, x),
                        x => Assert.Equal(3, x));
                },
                items =>
                {
                    Assert.Collection(items,
                        x => Assert.Equal(4, x),
                        x => Assert.Equal(5, x),
                        x => Assert.Equal(6, x));
                });
        }

        [Fact]
        public async Task AsyncActionHandlesScenario()
        {
            // arrange
            var result = new List<IEnumerable<int>>();
            var semaphore = new SemaphoreSlim(0);
            var completed = new SemaphoreSlim(0);
            var block = new BackpressureActionBlock<int>(async x =>
            {
                result.Add(x);

                completed.Release();
                await semaphore.WaitAsync();
            });

            // act - post 1 item - this will generate the first batch
            block.Post(1);
            await completed.WaitAsync();

            // act - post 2 items and release the first batch - these will queue up and form the second batch
            block.Post(2);
            block.Post(3);
            semaphore.Release();
            await completed.WaitAsync();

            // act - post 3 items and release the second batch - these will queue up and form the third batch
            block.Post(4);
            block.Post(5);
            block.Post(6);
            semaphore.Release();
            await completed.WaitAsync();

            // act - release the third batch
            semaphore.Release();
            block.Complete();

            // assert
            await block.Completion;
            Assert.Collection(result,
                items =>
                {
                    Assert.Collection(items,
                        x =>
                        {
                            Assert.Equal(1, x);
                        });
                },
                items =>
                {
                    Assert.Collection(items,
                        x => Assert.Equal(2, x),
                        x => Assert.Equal(3, x));
                },
                items =>
                {
                    Assert.Collection(items,
                        x => Assert.Equal(4, x),
                        x => Assert.Equal(5, x),
                        x => Assert.Equal(6, x));
                });
        }
    }
}