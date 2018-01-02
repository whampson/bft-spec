﻿using System;
using Xunit;

namespace Cascara.Tests.Extensions
{
    public class AssertExtensions : Assert
    {
        public static T ThrowsWithMessage<T>(Func<object> testCode, string message)
            where T : Exception
        {
            var ex = Assert.Throws<T>(testCode);
            Assert.Equal(message, ex.Message);

            return ex;
        }

        public static T ThrowsWithMessage<T>(Func<object> testCode, string fmt, params object[] args)
            where T : Exception
        {
            string message = string.Format(fmt, args);

            var ex = Assert.Throws<T>(testCode);
            Assert.Equal(message, ex.Message);

            return ex;
        }

        public static T ThrowsAnyWithMessage<T>(Func<object> testCode, string message)
            where T : Exception
        {
            var ex = Assert.ThrowsAny<T>(testCode);
            Assert.Equal(message, ex.Message);

            return ex;
        }

        public static T ThrowsAnyWithMessage<T>(Func<object> testCode, string fmt, params object[] args)
            where T : Exception
        {
            string message = string.Format(fmt, args);

            var ex = Assert.ThrowsAny<T>(testCode);
            Assert.Equal(message, ex.Message);

            return ex;
        }
    }
}
