﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams.BatchContainer;
using Orleans.Streams.Cache;

namespace Orleans.Streams
{
    public class RabbitMqAdapterFactory<TSerializer> : IQueueAdapterFactory where TSerializer : IBatchContainerSerializer, new()
    {
        private string _providerName;
        private IQueueAdapterCache _cache;
        private IStreamQueueMapper _mapper;
        private Task<IStreamFailureHandler> _failureHandler;
        private IQueueAdapter _adapter;
        private RabbitMqStreamProviderOptions _options;

        public void Init(IProviderConfiguration config, string providerName, Logger logger, IServiceProvider serviceProvider)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrEmpty(providerName)) throw new ArgumentNullException(nameof(providerName));

            _options = new RabbitMqStreamProviderOptions(config);
            _providerName = providerName;
            _cache = new ConcurrentQueueAdapterCache(_options.CacheSize);
            _mapper = new HashRingBasedStreamQueueMapper(_options.NumberOfQueues, _options.QueueNamePrefix);
            _failureHandler = Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler(false));

            var serializer = typeof(TSerializer) == typeof(DefaultBatchContainerSerializer)
                ? new DefaultBatchContainerSerializer(serviceProvider.GetRequiredService<SerializationManager>())
                : (IBatchContainerSerializer) new TSerializer();

            _adapter = new RabbitMqAdapter(_options, serializer, _mapper, _providerName, logger);
        }

        public Task<IQueueAdapter> CreateAdapter() => Task.FromResult(_adapter);
        public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId) => _failureHandler;
        public IQueueAdapterCache GetQueueAdapterCache() => _cache;
        public IStreamQueueMapper GetStreamQueueMapper() => _mapper;
    }
}