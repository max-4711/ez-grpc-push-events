using Grpc.Core;
using System.Collections.Generic;
using System.Linq;

namespace GrpcEventSubscriptions.Server
{
    /// <summary>
    /// Stores *ALL* subscriptions
    /// </summary>
    /// <remarks>Designated lifecycle: Aligned with whole microservice; never dies</remarks>
    public class SubscriptionManager : ISubscriptionManager
    {
        private interface IStoredSubscription
        {
            string EventDescriptor { get; set; }
        }

        private class StoredSubscription<TEvent> : IStoredSubscription
        {
            public IServerStreamWriter<TEvent> EventStream { get; set; }

            public string EventDescriptor { get; set; }
        }

        private ICollection<IStoredSubscription> _subscriptionStorage = new List<IStoredSubscription>();

        public void DeleteSubscription<TEvent>(IServerStreamWriter<TEvent> subscription, string eventDescriptor)
        {
            var subscriptionToCancel = _subscriptionStorage.Where(x => x.EventDescriptor == eventDescriptor).OfType<StoredSubscription<TEvent>>().FirstOrDefault(x => x.EventStream == subscription);

            if (subscriptionToCancel != null)
            {
                _subscriptionStorage.Remove(subscriptionToCancel);
            }
        }

        public IEnumerable<IServerStreamWriter<TEvent>> GetSubscriptionsForEvent<TEvent>(string eventDescriptor)
        {
            return _subscriptionStorage.Where(x => x.EventDescriptor == eventDescriptor).OfType<StoredSubscription<TEvent>>().Select(x => x.EventStream);
        }

        public void SaveSubscription<TEvent>(IServerStreamWriter<TEvent> subscription, string eventDescriptor)
        {
            var subscriptionAlreadyStored = _subscriptionStorage.Where(x => x.EventDescriptor == eventDescriptor).OfType<StoredSubscription<TEvent>>().Any(x => x.EventStream == subscription);
            if (subscriptionAlreadyStored)
            {
                return;
            }

            _subscriptionStorage.Add(new StoredSubscription<TEvent>
            {
                EventDescriptor = eventDescriptor,
                EventStream = subscription
            });
        }
    }
}
