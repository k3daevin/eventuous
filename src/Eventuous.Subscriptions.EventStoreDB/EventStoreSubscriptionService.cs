using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Eventuous.Subscriptions.EventStoreDB {
    [PublicAPI]
    public abstract class EventStoreSubscriptionService : SubscriptionService {
        readonly SubscriptionGapMeasure? _measure;
        readonly ILogger?                _log;
        readonly Logging?                _debugLog;

        protected EventStoreClient EventStoreClient { get; }

        protected EventStoreSubscriptionService(
            EventStoreClient           eventStoreClient,
            string                     subscriptionId,
            ICheckpointStore           checkpointStore,
            IEnumerable<IEventHandler> eventHandlers,
            IEventSerializer?          eventSerializer = null,
            ILoggerFactory?            loggerFactory   = null,
            SubscriptionGapMeasure?    measure         = null
        ) : base(subscriptionId, checkpointStore, eventHandlers, eventSerializer, loggerFactory, measure) {
            EventStoreClient = Ensure.NotNull(eventStoreClient, nameof(eventStoreClient));
        }

        protected override async Task<EventPosition> GetLastEventPosition(CancellationToken cancellationToken) {
            var read = EventStoreClient.ReadAllAsync(
                Direction.Backwards,
                Position.End,
                1,
                cancellationToken: cancellationToken
            );

            var events = await read.ToArrayAsync(cancellationToken);
            return new EventPosition(events[0].Event.Position.CommitPosition, events[0].Event.Created);
        }
    }
}