using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Cloudtoid.Interprocess.Tests
{
    public abstract class LoggingTestBase
    {
        public LoggingTestBase(ITestOutputHelper output)
        {
            Output = output;
        }

        public ITestOutputHelper Output { get; }

        protected void BeforeAfterTest(Action test, int callFramesAbove = 1)
        {
            var methodUnderTest = new StackFrame(callFramesAbove, false).GetMethod()!;
            Output.WriteLine("Before - " + methodUnderTest.Name);
            try
            {
                test();
            }
            finally
            {
                Output.WriteLine("After - " + methodUnderTest.Name);
            }
        }

        protected Task BeforeAfterTestAsync(Func<Task> test, int callFramesAbove = 1)
        {
            var methodUnderTest = new StackFrame(callFramesAbove, false).GetMethod()!;
            Output.WriteLine("Before - " + methodUnderTest.Name);
            try
            {
                return test();
            }
            finally
            {
                Output.WriteLine("After - " + methodUnderTest.Name);
            }
        }
    }
}