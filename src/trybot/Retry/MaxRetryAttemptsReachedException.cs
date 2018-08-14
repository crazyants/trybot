﻿using System;

namespace Trybot.Retry
{
    class MaxRetryAttemptsReachedException<TResult> : Exception
    {
        public TResult OperationResult { get; set; }

        public MaxRetryAttemptsReachedException(string message, Exception innerException, TResult result) : base(message, innerException)
        {
            this.OperationResult = result;
        }
    }
}
