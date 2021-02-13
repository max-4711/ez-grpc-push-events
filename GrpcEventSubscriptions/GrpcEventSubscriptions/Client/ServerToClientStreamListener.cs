using Grpc.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcEventSubscriptions.Client
{
    /// <summary>
    /// Helper for the client: Wraps around a server-to-client-stream and fires an event, if a new object is received via the stream
    /// </summary>
    public class ServerToClientStreamListener<TSubscriptionEvent> : IDisposable //TODO: Interface extrahieren wenn fertig
    {
        public event EventHandler<TSubscriptionEvent>? ServerEventReceived;

        private CancellationTokenSource _cancellationTokenSource;
        protected IAsyncStreamReader<TSubscriptionEvent> _eventStream;

        protected ServerToClientStreamListener()
        {
            this._cancellationTokenSource = new CancellationTokenSource();
        }

        public ServerToClientStreamListener(AsyncServerStreamingCall<TSubscriptionEvent> streamingCall) : this()
        {
            this._eventStream = streamingCall.ResponseStream;
        }

        public async void StartListening()
        {
            await ConsumeSubscriptionStreamAsync(_eventStream, _cancellationTokenSource.Token); //TODO: Hier kann es noch Exceptions geben (-> wenn der Server beendet wird)
        }

        public async Task ListenAsync()
        {
            await ConsumeSubscriptionStreamAsync(_eventStream, _cancellationTokenSource.Token);
        }

        private async Task ConsumeSubscriptionStreamAsync(IAsyncStreamReader<TSubscriptionEvent> responseStream, CancellationToken token)
        {
            try
            {
                await foreach (var streamedEvent in responseStream.ReadAllAsync(token))
                {
                    ServerEventReceived?.Invoke(this, streamedEvent);
                }
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.Cancelled)
            {
                return;
            }
            catch (OperationCanceledException)
            {
                //TODO: Muss hier was passieren?
            }
        }

        public void Dispose()
        {
            this._cancellationTokenSource.Cancel();
            this._cancellationTokenSource.Dispose();
        }
    }
}
