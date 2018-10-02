﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Trybot.CircuitBreaker.Exceptions;
using Trybot.Operations;
using Trybot.Utils;

namespace Trybot.CircuitBreaker
{
    internal class CircuitBreakerBot<TResult> : ConfigurableBot<CircuitBreakerConfiguration<TResult>, TResult>
    {
        private readonly CircuitBreakerStrategy strategy;
        private readonly AtomicBool executionBarrier;

        internal CircuitBreakerBot(Bot<TResult> innerBot, CircuitBreakerConfiguration<TResult> configuration, CircuitBreakerStrategy strategy) : base(innerBot, configuration)
        {
            this.strategy = strategy;
            this.executionBarrier = new AtomicBool();
        }

        public override TResult Execute(IBotOperation<TResult> operation, ExecutionContext context, CancellationToken token)
        {
            var shouldLimitExecution = this.strategy.PreCheckCircuitState();
            if (shouldLimitExecution && !this.executionBarrier.CompareExchange(false, true))
                throw new HalfOpenExecutionLimitExceededException(Constants.HalfOpenExecutionLimitExceededExceptionMessage);

            try
            {
                var result = base.InnerBot.Execute(operation, context, token);

                if (base.Configuration.AcceptsResult(result))
                    this.strategy.OperationSucceeded();
                else
                    this.strategy.OperationFailed();

                return result;
            }
            catch (Exception exception)
            {
                if (base.Configuration.HandlesException(exception))
                    this.strategy.OperationFailed();

                throw;
            }
            finally
            {
                this.executionBarrier.SetValue(false);
            }
        }

        public override async Task<TResult> ExecuteAsync(IAsyncBotOperation<TResult> operation, ExecutionContext context, CancellationToken token)
        {
            var shouldLimitExecution = await this.strategy.PreCheckCircuitStateAsync(token, context.BotPolicyConfiguration.ContinueOnCapturedContext)
                .ConfigureAwait(context.BotPolicyConfiguration.ContinueOnCapturedContext);

            if (shouldLimitExecution && !this.executionBarrier.CompareExchange(false, true))
                throw new HalfOpenExecutionLimitExceededException(Constants.HalfOpenExecutionLimitExceededExceptionMessage);

            try
            {
                var result = await base.InnerBot.ExecuteAsync(operation, context, token)
                    .ConfigureAwait(context.BotPolicyConfiguration.ContinueOnCapturedContext);

                if (base.Configuration.AcceptsResult(result))
                    await this.strategy.OperationSucceededAsync(token, context.BotPolicyConfiguration.ContinueOnCapturedContext)
                        .ConfigureAwait(context.BotPolicyConfiguration.ContinueOnCapturedContext);
                else
                    await this.strategy.OperationFailedAsync(token, context.BotPolicyConfiguration.ContinueOnCapturedContext)
                        .ConfigureAwait(context.BotPolicyConfiguration.ContinueOnCapturedContext);

                return result;
            }
            catch (Exception exception)
            {
                if (base.Configuration.HandlesException(exception))
                    await this.strategy.OperationFailedAsync(token, context.BotPolicyConfiguration.ContinueOnCapturedContext)
                        .ConfigureAwait(context.BotPolicyConfiguration.ContinueOnCapturedContext);

                throw;
            }
            finally
            {
                this.executionBarrier.SetValue(false);
            }
        }
    }
}
