using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcEventSubscriptions.Server
{
    /// <summary>
    /// Helper for the server: Simplifies managing subscriptions and sending out certain events
    /// </summary>
    /// <remarks>Designated Lifecycle: One instance gRPC-service instance</remarks>
    public class ServerSubscriptionHook<TEvent> : IServerSubscriptionHook<TEvent>
    {
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ILogger<ServerSubscriptionHook<TEvent>> _logger;

        public ServerSubscriptionHook(ISubscriptionManager subscriptionManager, ILogger<ServerSubscriptionHook<TEvent>> logger)
        {
            this._subscriptionManager = subscriptionManager;
            this._logger = logger;
        }

        public async Task SendEventAsync(TEvent eventObject, string eventDescriptor)
        {
            var subscribers = _subscriptionManager.GetSubscriptionsForEvent<TEvent>(eventDescriptor.ToString());

            foreach (var subscriber in subscribers)
            {
                try
                {
                    await subscriber.WriteAsync(eventObject);
                }
                catch (Exception ex)
                {
                    this._logger.LogWarning(ex.Message);
                }
            }
        }

        private static Task AwaitCancellation(CancellationToken token)
        {
            var completion = new TaskCompletionSource<object>();
            token.Register(() => completion.SetResult(null));
            return completion.Task;
        }

        public async Task SendDomainEventAsync(TEvent eventObject)
        {
            await this.SendEventAsync(eventObject, typeof(TEvent).ToString());
        }

        public async Task HookEventSubscriptionAsync(IServerStreamWriter<TEvent> subscription, CancellationToken cancellationToken, string eventDescriptor)
        {
            var eventDescriptorString = eventDescriptor.ToString();

            this._subscriptionManager.SaveSubscription(subscription, eventDescriptorString);

            await AwaitCancellation(cancellationToken);

            this._subscriptionManager.DeleteSubscription(subscription, eventDescriptorString);
        }

        public async Task HookDomainEventSubscriptionAsync(IServerStreamWriter<TEvent> subscription, CancellationToken cancellationToken)
        {
            await this.HookEventSubscriptionAsync(subscription, cancellationToken, typeof(TEvent).ToString());
        }
    }
}
