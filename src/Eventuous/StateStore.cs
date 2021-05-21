using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Eventuous {
    [PublicAPI]
    public class StateStore : IStateStore {
        readonly IEventStore      _eventStore;
        readonly IEventSerializer _serializer;

        public StateStore(IEventStore eventStore, IEventSerializer serializer) {
            _eventStore = Ensure.NotNull(eventStore, nameof(eventStore));
            _serializer = Ensure.NotNull(serializer, nameof(serializer));
        }

        public async Task<T> LoadState<T, TId>(StreamName stream, CancellationToken cancellationToken)
            where T : AggregateState<T, TId>, new() where TId : AggregateId {
            var state = new T();

            await _eventStore.ReadStream(stream, StreamReadPosition.Start, Fold, cancellationToken).Ignore();

            return state;

            void Fold(StreamEvent streamEvent) {
                var evt = Deserialize(streamEvent);
                if (evt == null) return;

                state = state.When(evt);
            }

            object? Deserialize(StreamEvent streamEvent)
                => _serializer.Deserialize(streamEvent.Data.AsSpan(), streamEvent.EventType);
        }
    }
}