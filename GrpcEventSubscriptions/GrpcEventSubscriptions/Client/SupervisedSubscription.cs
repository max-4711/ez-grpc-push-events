using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcEventSubscriptions.Client
{
    public class SupervisedSubscription<TReceivedEvent> : IDisposable
    {
        private readonly Func<AsyncServerStreamingCall<TReceivedEvent>> _subscriptionFactory;
        private readonly Action<object?, TReceivedEvent> _eventHandler;
        private readonly ILogger? _logger;

        private AsyncServerStreamingCall<TReceivedEvent>? subscription;
        private ServerToClientStreamListener<TReceivedEvent>? subscriptionListener;

        private CancellationTokenSource? workingTaskCancellationTokenSource;

        private Thread? subscriptionThread;
        private bool isListening = false;
        private int retryTimeoutMilliseconds;

        public SupervisedSubscription(Func<AsyncServerStreamingCall<TReceivedEvent>> subscriptionFactory, Action<object?, TReceivedEvent> eventHandler, ILogger? logger)
        {
            _subscriptionFactory = subscriptionFactory;
            _eventHandler = eventHandler;
            _logger = logger;
        }

        public void StartListening(int retryTimeoutMilliseconds = 30000)
        {
            this.retryTimeoutMilliseconds = retryTimeoutMilliseconds;

            if (isListening)
                return;

            isListening = true;

            workingTaskCancellationTokenSource = new CancellationTokenSource();

            subscriptionThread = new Thread(HoldSubscription);
            subscriptionThread.IsBackground = false;
            subscriptionThread.Start();
        }

        private async void HoldSubscription()
        {
            while (workingTaskCancellationTokenSource != null && !workingTaskCancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    subscription = _subscriptionFactory.Invoke();
                    subscriptionListener = new ServerToClientStreamListener<TReceivedEvent>(subscription);
                    subscriptionListener.ServerEventReceived += _eventHandler.Invoke;
                    _logger?.LogDebug($"Subscription of type {nameof(TReceivedEvent)} prepared. Beginning to listen...");
                    await subscriptionListener.ListenAsync();
                }
                catch (RpcException)
                {
                    _logger?.LogWarning($"Encountered a RpcException while trying to configure the {nameof(TReceivedEvent)} subscription. Retrying in {retryTimeoutMilliseconds / 1000} seconds...");
                    await Task.Delay(retryTimeoutMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Encountered an unexpected exception while trying to configure the {nameof(TReceivedEvent)} subscription: {ex} ({ex.Message}). Retrying in {retryTimeoutMilliseconds / 1000} seconds...");
                    await Task.Delay(retryTimeoutMilliseconds);
                }
            }
        }

        public void Dispose()
        {
            subscriptionThread?.Abort();
            subscriptionThread = null;

            if (subscriptionListener != null)
            {
                subscriptionListener.ServerEventReceived -= _eventHandler.Invoke;
                subscriptionListener.Dispose();
                subscriptionListener = null;
            }

            subscription?.Dispose();
            subscription = null;

            workingTaskCancellationTokenSource?.Cancel();
            workingTaskCancellationTokenSource?.Dispose();
            workingTaskCancellationTokenSource = null;

            isListening = false;
        }
    }
}
