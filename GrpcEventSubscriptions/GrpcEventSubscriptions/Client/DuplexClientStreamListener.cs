using Grpc.Core;

namespace GrpcEventSubscriptions.Client
{
    /// <summary>
    /// Helper for the client: Wraps around a duplex-stream (client-to-server and server-to-client) and fires an event, if a new object is received via the server-to-client stream
    /// </summary>
    public class DuplexClientStreamListener<TSubscriptionEvent, TRequestEvent> : ServerToClientStreamListener<TSubscriptionEvent>
    {
        public DuplexClientStreamListener(AsyncDuplexStreamingCall<TRequestEvent, TSubscriptionEvent> duplexStreamingCall) : base()
        {
            base._eventStream = duplexStreamingCall.ResponseStream;
        }
    }
}
