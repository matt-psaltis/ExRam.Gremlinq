﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace ExRam.Gremlinq.Core
{
    public interface IQueryFragmentDeserializer
    {
        object? TryDeserialize(object serializedData, Type fragmentType, IGremlinQueryEnvironment environment);

        IQueryFragmentDeserializer Override<TSerialized>(Func<TSerialized, Type, IGremlinQueryEnvironment, Func<object?>, IQueryFragmentDeserializer, object?> deserializer);
    }

    public interface IGremlinQueryExecutionResultDeserializer
    {
        IAsyncEnumerable<TElement> Deserialize<TElement>(object executionResult, IGremlinQueryEnvironment environment);

        IGremlinQueryExecutionResultDeserializer ConfigureFragmentDeserializer(Func<IQueryFragmentDeserializer, IQueryFragmentDeserializer> transformation);
    }

    public static class QueryFragmentDeserializer
    {
        private sealed class FragmentDeserializerImpl : IQueryFragmentDeserializer
        {
            private readonly IImmutableDictionary<Type, Delegate> _dict;
            private ConcurrentDictionary<Type, Delegate?>? _fastDict;

            public FragmentDeserializerImpl(IImmutableDictionary<Type, Delegate> dict)
            {
                _dict = dict;
            }

            public object? TryDeserialize(object serializedData, Type fragmentType, IGremlinQueryEnvironment environment)
            {
                return TryGetDeserializer(serializedData.GetType()) is Func<object, Type, IGremlinQueryEnvironment, Func<object?>, IQueryFragmentDeserializer, object?> del
                    ? del(serializedData, fragmentType, environment, () => throw new NotSupportedException(), this)
                    : serializedData;
            }

            public IQueryFragmentDeserializer Override<TSerialized>(Func<TSerialized, Type, IGremlinQueryEnvironment, Func<object?>, IQueryFragmentDeserializer, object?> deserializer)
            {
                return new FragmentDeserializerImpl(
                    _dict.SetItem(
                        typeof(TSerialized),
                        _dict.TryGetValue(typeof(TSerialized), out var existingAtomSerializer)
                            ? new Func<object, Type, IGremlinQueryEnvironment, Func<object?>, IQueryFragmentDeserializer, object?>((atom, type, env, baseSerializer, recurse) => deserializer((TSerialized)atom, type, env, () => ((Func<object, Type, IGremlinQueryEnvironment, Func<object?>, IQueryFragmentDeserializer, object?>)existingAtomSerializer)(atom!, type, env, baseSerializer, recurse), recurse))
                            : new Func<object, Type, IGremlinQueryEnvironment, Func<object?>, IQueryFragmentDeserializer, object?>((atom, type, env, baseSerializer, recurse) => deserializer((TSerialized)atom, type, env, baseSerializer, recurse))));
            }

            private Delegate? TryGetDeserializer(Type staticType)
            {
                var fastDict = Volatile.Read(ref _fastDict);
                if (fastDict == null)
                    Volatile.Write(ref _fastDict, fastDict = new ConcurrentDictionary<Type, Delegate?>());

                return fastDict
                    .GetOrAdd(
                        staticType,
                        (closureType, @this) => @this.InnerLookup(closureType),
                        this);
            }

            private Delegate? InnerLookup(Type actualType)
            {
                if (_dict.TryGetValue(actualType, out var ret))
                    return ret;

                foreach (var implementedInterface in actualType.GetInterfaces())
                {
                    if (InnerLookup(implementedInterface) is { } interfaceSerializer)
                        return interfaceSerializer;
                }

                if (actualType.BaseType is { } baseType)
                {
                    if (InnerLookup(baseType) is { } baseSerializer)
                        return baseSerializer;
                }

                return null;
            }
        }

        public static readonly IQueryFragmentDeserializer Identity = new FragmentDeserializerImpl(ImmutableDictionary<Type, Delegate>.Empty);
    }
}
