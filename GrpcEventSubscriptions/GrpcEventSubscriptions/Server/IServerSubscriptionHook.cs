using Grpc.Core;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcEventSubscriptions.Server
{
    public interface IServerSubscriptionHook<TEvent>
    {
        Task SendEventAsync(TEvent eventObject, string eventDescriptor);
        Task SendDomainEventAsync(TEvent eventObject);

        Task HookEventSubscriptionAsync(IServerStreamWriter<TEvent> subscription, CancellationToken cancellationToken, string eventDescriptor);
        Task HookDomainEventSubscriptionAsync(IServerStreamWriter<TEvent> subscription, CancellationToken cancellationToken);
    }
}
