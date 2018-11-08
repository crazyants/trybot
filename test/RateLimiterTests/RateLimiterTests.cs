﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Trybot.RateLimiter;
using Trybot.RateLimiter.Exceptions;

namespace Trybot.Tests.RateLimiterTests
{
    [TestClass]
    public class RateLimiterTests
    {
        private IBotPolicy CreatePolicyWithRateLimit(RateLimiterConfiguration configuration) =>
            new BotPolicy(config => config
                .Configure(botconfig => botconfig
                    .RateLimit(configuration)));

        private IBotPolicy<T> CreatePolicyWithResultWithRateLimit<T>(RateLimiterConfiguration configuration) =>
            new BotPolicy<T>(config => config
                .Configure(botconfig => botconfig
                    .RateLimit(configuration)));

        private RateLimiterConfiguration CreateConfiguration(int count, TimeSpan interval) =>
            new RateLimiterConfiguration()
                .MaxAmountOfAllowedOperations(count)
                .WithinTimeInterval(interval);

        [TestMethod]
        public void RateLimit_Sliding_Ok()
        {
            var called = false;
            var policy = this.CreatePolicyWithRateLimit(this.CreateConfiguration(5, TimeSpan.FromSeconds(10)));
            policy.Execute(() => { called = true; });
            Assert.IsTrue(called);
        }

        [TestMethod]
        public async Task RateLimit_Sliding_Async_Ok()
        {
            var called = false;
            var policy = this.CreatePolicyWithRateLimit(this.CreateConfiguration(5, TimeSpan.FromSeconds(10)));
            await policy.ExecuteAsync(() => { called = true; });
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void RateLimit_Sliding_Result_Ok()
        {
            var policy = this.CreatePolicyWithResultWithRateLimit<int>(this.CreateConfiguration(5, TimeSpan.FromSeconds(10)));
            var test = policy.Execute(() => 5);
            Assert.AreEqual(5, test);
        }

        [TestMethod]
        public async Task RateLimit_Sliding_Result_Async_Ok()
        {
            var policy = this.CreatePolicyWithResultWithRateLimit<int>(this.CreateConfiguration(5, TimeSpan.FromSeconds(10)));
            var test = await policy.ExecuteAsync(() => 5);
            Assert.AreEqual(5, test);
        }

        [TestMethod]
        public void RateLimit_Fixed_Ok()
        {
            var called = false;
            var policy = this.CreatePolicyWithRateLimit(this.CreateConfiguration(5, TimeSpan.FromSeconds(10)).UseStrategy(RateLimiterStrategy.FixedWindow));
            policy.Execute(() => { called = true; });
            Assert.IsTrue(called);
        }

        [TestMethod]
        public async Task RateLimit_Fixed_Async_Ok()
        {
            var called = false;
            var policy = this.CreatePolicyWithRateLimit(this.CreateConfiguration(5, TimeSpan.FromSeconds(10)).UseStrategy(RateLimiterStrategy.FixedWindow));
            await policy.ExecuteAsync(() => { called = true; });
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void RateLimit_Sliding_Reject()
        {
            var policy = this.CreatePolicyWithRateLimit(this.CreateConfiguration(2, TimeSpan.FromSeconds(2)));

            policy.Execute(() => { });
            policy.Execute(() => { });
            var exception = Assert.ThrowsException<RateLimitExceededException>(() => policy.Execute(() => { }));
            Assert.IsTrue(exception.RetryAfter > TimeSpan.Zero);
            Thread.Sleep(exception.RetryAfter);
            policy.Execute(() => { });
        }

        [TestMethod]
        public void RateLimit_Fixed_Reject()
        {
            var policy = this.CreatePolicyWithRateLimit(this.CreateConfiguration(2, TimeSpan.FromSeconds(2)).UseStrategy(RateLimiterStrategy.FixedWindow));

            policy.Execute(() => { });
            policy.Execute(() => { });
            var exception = Assert.ThrowsException<RateLimitExceededException>(() => policy.Execute(() => { }));
            Assert.IsTrue(exception.RetryAfter > TimeSpan.Zero);
            Thread.Sleep(exception.RetryAfter);
            policy.Execute(() => { });
        }

        [TestMethod]
        public void RateLimit_Sliding_Reject_Allow_Again()
        {
            var policy = this.CreatePolicyWithRateLimit(this.CreateConfiguration(5, TimeSpan.FromSeconds(5)));

            policy.Execute(() => { });
            Thread.Sleep(4000);
            policy.Execute(() => { });
            policy.Execute(() => { });
            policy.Execute(() => { });
            policy.Execute(() => { });
            Assert.ThrowsException<RateLimitExceededException>(() => policy.Execute(() => { }));
            Thread.Sleep(2000);
            policy.Execute(() => { });
            Assert.ThrowsException<RateLimitExceededException>(() => policy.Execute(() => { }));
            Thread.Sleep(4000);
            policy.Execute(() => { });
        }

        [TestMethod]
        public void RateLimit_FixedWindow_Reject_Allow_Again()
        {
            var policy = this.CreatePolicyWithRateLimit(this.CreateConfiguration(5, TimeSpan.FromSeconds(5)).UseStrategy(RateLimiterStrategy.FixedWindow));

            policy.Execute(() => { });
            Thread.Sleep(4000);
            policy.Execute(() => { });
            policy.Execute(() => { });
            policy.Execute(() => { });
            policy.Execute(() => { });
            Assert.ThrowsException<RateLimitExceededException>(() => policy.Execute(() => { }));
            Thread.Sleep(2000);
            policy.Execute(() => { });
            policy.Execute(() => { });
            policy.Execute(() => { });
            policy.Execute(() => { });
        }
    }
}
