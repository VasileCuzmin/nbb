﻿// Copyright (c) TotalSoft.
// This source code is licensed under the MIT license.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NBB.EventStore.Abstractions;
using NBB.EventStore.AdoNet.Migrations;
using NBB.MultiTenancy.Abstractions;
using NBB.MultiTenancy.Abstractions.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Xunit;

namespace NBB.EventStore.IntegrationTests
{
    [Collection("EventStoreDB")]
    public class EventStoreDnIntegrationTests : IClassFixture<EnvironmentFixture>
    {
        [Fact]
        public void EventStore_AppendEventsToStreamAsync_with_expected_version_should_be_thread_safe()
        {
            PrepareDb();
            var container = BuildAdoRepoServiceProvider();
            var stream = Guid.NewGuid().ToString();
            const int streamVersion = 0;
            const int threadCount = 10;
            var threads = new List<Thread>();

            var concurrencyExceptionCount = 0;

            using var scope = container.CreateScope();
            var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

            for (var i = 0; i < threadCount; i++)
            {
                var t = new Thread(() =>
                {
                    //var newVersion = Interlocked.Increment(ref streamVersion);
                    try
                    {
                        eventStore.AppendEventsToStreamAsync(stream,
                                new[] { new TestEvent(Guid.NewGuid()) }, streamVersion,
                                CancellationToken.None)
                            .Wait();
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref concurrencyExceptionCount);
                    }
                });
                t.Start();
                threads.Add(t);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            var events = eventStore.GetEventsFromStreamAsync(stream, null, CancellationToken.None).Result;

            events.Count.Should().Be(1);
            concurrencyExceptionCount.Should().Be(threadCount - 1);
        }

        [Fact]
        public void EventStore_AppendEventsToStreamAsync_with_expected_version_any_should_be_thread_safe()
        {
            PrepareDb();
            var container = BuildAdoRepoServiceProvider();
            var stream = Guid.NewGuid().ToString();
            var threadCount = 10;
            var threads = new List<Thread>();


            using var scope = container.CreateScope();
            var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

            for (var i = 0; i < threadCount; i++)
            {
                var t = new Thread(() =>
                {
                    eventStore.AppendEventsToStreamAsync(stream,
                        new[] { new TestEvent(Guid.NewGuid()) }, null, CancellationToken.None).Wait();
                });
                t.Start();
                threads.Add(t);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            var events = eventStore.GetEventsFromStreamAsync(stream, null, CancellationToken.None).Result;

            events.Count.Should().Be(threadCount);
        }

        private static IServiceProvider BuildAdoRepoServiceProvider()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var isDevelopment = string.Equals(environment, "development", StringComparison.OrdinalIgnoreCase);

            if (isDevelopment)
            {
                configurationBuilder.AddUserSecrets(Assembly.GetExecutingAssembly());
            }

            var configuration = configurationBuilder.Build();


            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();


            services.AddEventStore()
                .WithNewtownsoftJsonEventStoreSeserializer()
                .WithAdoNetEventRepository();

            services.AddMultitenancy(configuration)
                .AddSingleton(Mock.Of<ITenantContextAccessor>(x =>
                    x.TenantContext == new TenantContext(new Tenant(Guid.NewGuid(), null))))
                .WithMultiTenantAdoNetEventRepository();


            var container = services.BuildServiceProvider();
            return container;
        }

        private static void PrepareDb()
        {
            new AdoNetEventStoreDatabaseMigrator(isTestHost: true).ReCreateDatabaseObjects(null).Wait();
        }
    }

    public record TestEvent(Guid EventId);
}
