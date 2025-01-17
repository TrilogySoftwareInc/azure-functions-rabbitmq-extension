﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal sealed class RabbitMQService : IRabbitMQService
    {
        public RabbitMQService(string connectionString, bool disableCertificateValidation)
        {
            var connectionFactory = new ConnectionFactory
            {
                Uri = new Uri(connectionString),
            };

            if (disableCertificateValidation && connectionFactory.Ssl.Enabled)
            {
                connectionFactory.Ssl.AcceptablePolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
            }

            this.Model = connectionFactory.CreateConnection().CreateModel();
            this.PublishBatchLock = new object();
        }

        public RabbitMQService(string connectionString, string queueName, bool disableCertificateValidation)
            : this(connectionString, disableCertificateValidation)
        {
            this.RabbitMQModel = new RabbitMQModel(this.Model);
            _ = queueName ?? throw new ArgumentNullException(nameof(queueName));

            this.Model.QueueDeclarePassive(queueName); // Throws exception if queue doesn't exist
            this.BasicPublishBatch = this.Model.CreateBasicPublishBatch();
        }

        public IRabbitMQModel RabbitMQModel { get; }

        public IModel Model { get; }

        public IBasicPublishBatch BasicPublishBatch { get; private set; }

        public object PublishBatchLock { get; }

        // Typically called after a flush
        public void ResetPublishBatch()
        {
            this.BasicPublishBatch = this.Model.CreateBasicPublishBatch();
        }
    }
}
