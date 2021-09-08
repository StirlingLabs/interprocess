using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cloudtoid.Interprocess
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers what is needed to create and consume shared-memory queues that are
        /// cross-process accessible.
        /// Use <see cref="IQueueFactory"/> to access the queue.
        /// </summary>
        public static IServiceCollection AddInterprocessQueue(this IServiceCollection services)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));

            Util.Ensure64Bit();
            services.TryAddSingleton<IQueueFactory, QueueFactory>();
            return services;
        }
    }
}
