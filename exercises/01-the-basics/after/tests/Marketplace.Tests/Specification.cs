﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Marketplace.Framework;

namespace Marketplace.Tests
{
    public abstract class Specification<TAggregate, TCommand> where TAggregate  : Aggregate, new()
    {
        public Specification()
        {
            History = Given();
            Command = When();

            var sut = new TAggregate();
            sut.Load(History);
            var store = SpecificationAggregateStore.For(sut);

            try
            {
                GetHandler(store)(Command).Wait();
            }
            catch (Exception exception)
            {
                CaughtException = exception;
            }

            RaisedEvents = store.RaisedEvents;
        }

        public TCommand Command { get; }

        public object[] History { get; }

        public object[] RaisedEvents { get; }

        public Exception CaughtException { get; }

        public abstract Func<TCommand, Task> GetHandler(SpecificationAggregateStore store);

        public abstract object[] Given();

        public abstract TCommand When();

        public class SpecificationAggregateStore : IAggregateStore
        {
            public static SpecificationAggregateStore For(Aggregate aggregate) => new SpecificationAggregateStore(aggregate);

            private Aggregate _aggregate;

            private SpecificationAggregateStore(Aggregate aggregate) => _aggregate = aggregate;

            public object[] RaisedEvents { get; private set; }

            public Task<T> Load<T>(string aggregateId) where T : Aggregate, new()
                => Task.FromResult((T)_aggregate);

            public Task Save<T>(T aggregate) where T : Aggregate
            {
                _aggregate = aggregate;

                RaisedEvents = _aggregate.GetChanges();

                var nextVersion =  aggregate.Version + aggregate.GetChanges().Length;

                return Task.FromResult<(long NextExpectedVersion, long LogPosition, long CommitPosition)>(
                    (nextVersion, nextVersion, nextVersion));
            }
        }
    }
}