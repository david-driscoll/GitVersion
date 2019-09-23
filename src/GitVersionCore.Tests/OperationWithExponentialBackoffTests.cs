using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using GitVersion.Helpers;
using GitVersion.Log;
using GitVersionCore.Tests.Mocks;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class OperationWithExponentialBackoffTests : TestBase
    {
        [Test]
        public void RetryOperationThrowsWhenNegativeMaxRetries()
        {
            Action action = () => new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(), new NullLog(), () => { }, -1);
            action.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        public void RetryOperationThrowsWhenThreadSleepIsNull()
        {
            Action action = () => new OperationWithExponentialBackoff<IOException>(null, new NullLog(), () => { });
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public async Task OperationIsNotRetriedOnInvalidException()
        {
            void Operation()
            {
                throw new Exception();
            }

            var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(), new NullLog(), Operation);
            Task action = retryOperation.ExecuteAsync();
            await action.ShouldThrowAsync<Exception>();
        }

        [Test]
        public async Task OperationIsRetriedOnIOException()
        {
            var operationCount = 0;

            void Operation()
            {
                operationCount++;
                if (operationCount < 2)
                {
                    throw new IOException();
                }
            }

            var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(), new NullLog(), Operation);
            await retryOperation.ExecuteAsync();

            operationCount.ShouldBe(2);
        }

        [Test]
        public async Task OperationIsRetriedAMaximumNumberOfTimesAsync()
        {
            const int numberOfRetries = 3;
            var operationCount = 0;

            void Operation()
            {
                operationCount++;
                throw new IOException();
            }

            var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(), new NullLog(), Operation, numberOfRetries);
            Task action = retryOperation.ExecuteAsync();
            await action.ShouldThrowAsync<AggregateException>();

            operationCount.ShouldBe(numberOfRetries + 1);
        }

        [Test]
        public async Task OperationDelayDoublesBetweenRetries()
        {
            const int numberOfRetries = 3;
            var expectedSleepMSec = 500;
            var sleepCount = 0;

            void Operation() => throw new IOException();

            Task Validator(int u)
            {
                return Task.Run(() =>
                {
                    sleepCount++;
                    u.ShouldBe(expectedSleepMSec);
                    expectedSleepMSec *= 2;
                });
            }

            var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(Validator), new NullLog(), Operation, numberOfRetries);
            Task action = retryOperation.ExecuteAsync();
            await action.ShouldThrowAsync<AggregateException>();

            // action.ShouldThrow<AggregateException>();

            sleepCount.ShouldBe(numberOfRetries);
        }

        [Test]
        public async Task TotalSleepTimeForSixRetriesIsAboutThirtySecondsAsync()
        {
            const int numberOfRetries = 6;
            int totalSleep = 0;

            void Operation()
            {
                throw new IOException();
            }

            Task Validator(int u)
            {
                return Task.Run(() => { totalSleep += u; });
            }

            var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(Validator), new NullLog(), Operation, numberOfRetries);

            Task action = retryOperation.ExecuteAsync();
            await action.ShouldThrowAsync<AggregateException>();
            // Action action = () => retryOperation.ExecuteAsync();
            // action.ShouldThrow<AggregateException>();

            // Exact number is 31,5 seconds
            totalSleep.ShouldBe(31500);
        }
    }
}
