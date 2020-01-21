﻿using Microsoft.Extensions.Configuration;
using System;

namespace NBB.Messaging.Abstractions
{
    public class DefaultTopicRegistry : ITopicRegistry
    {
        private readonly IConfiguration _configuration;

        public DefaultTopicRegistry(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetTopicForMessageType(Type messageType, bool includePrefix = true) =>
            new TopicRulesResolver().Then(
                new TopicNameResolver(includePrefix, _configuration)).Execute(
               DefaultTopicProcessor.GetTopic(messageType, _configuration));

        public string GetTopicForName(string topicName, bool includePrefix = true) =>
            new TopicRulesResolver().Then(
            new TopicNameResolver(includePrefix, _configuration)).Execute(topicName);

        public string GetTopicForTopicPrefix(Type messageType, string topicPrefix) =>
            new TopicPrefixConcatenateProcessor(topicPrefix).Execute(
                  DefaultTopicProcessor.GetTopic(messageType, _configuration));

        public string GetSharedTopicPrefix()
            => _configuration.GetSection("Messaging")["SharedTopicPrefix"];
    }
}