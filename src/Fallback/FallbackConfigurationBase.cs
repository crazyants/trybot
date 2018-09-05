﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Trybot.Fallback
{
    public class FallbackConfigurationBase
    {
        internal Func<Exception, bool> FallbackPolicy { get; set; }

        internal Action<Exception, ExecutionContext> FallbackHandler { get; set; }

        internal Func<Exception, ExecutionContext, CancellationToken, Task> AsyncFallbackHandler { get; set; }

        internal bool HandlesException(Exception exception) =>
            this.FallbackPolicy?.Invoke(exception) ?? false;

        internal void RaiseRetryEvent(Exception exception, ExecutionContext context) =>
            this.FallbackHandler?.Invoke(exception, context);

        internal async Task RaiseRetryEventAsync(Exception exception, ExecutionContext context, CancellationToken token)
        {
            if (this.AsyncFallbackHandler == null)
                return;

            await this.AsyncFallbackHandler(exception, context, token)
                .ConfigureAwait(context.BotPolicyConfiguration.ContinueOnCapturedContext);
        }
    }
}
