using Grpc.Core;
using System.Collections.Generic;

namespace GrpcEventSubscriptions.Server
{
    public interface ISubscriptionManager
    {
        IEnumerable<IServerStreamWriter<TEvent>> GetSubscriptionsForEvent<TEvent>(string eventDescriptor);

        void SaveSubscription<TEvent>(IServerStreamWriter<TEvent> subscription, string eventDescriptor);

        void DeleteSubscription<TEvent>(IServerStreamWriter<TEvent> subscription, string eventDescriptor);
    }
}
