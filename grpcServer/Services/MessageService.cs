using System.Collections.Concurrent;
using Grpc.Core;
using grpcMessageServer;

public class MessageService : Message.MessageBase
{
    private static readonly ConcurrentBag<MessageResponse> _messages = new();
    private static readonly List<IServerStreamWriter<MessageResponse>> _subscribers = new();
    private static readonly object _lock = new();

    public override Task<MessageResponse> SendMessage(MessageRequest request, ServerCallContext context)
    {
        var message = new MessageResponse
        {
            Id = Guid.NewGuid().ToString(),
            Sender = request.Sender,
            Text = request.Text,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _messages.Add(message);

        lock (_lock)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.WriteAsync(message);
            }
        }

        return Task.FromResult(message);
    }

    public override Task<GetMessageResponse> GetMessage(GetMessageRequest request, ServerCallContext context)
    {
        var response = new GetMessageResponse();
        var messages = _messages.Where(m => string.IsNullOrEmpty(request.Sender) || m.Sender == request.Sender)
        .OrderByDescending(m => m.Timestamp)
        .Take(request.Limit > 0 ? request.Limit : 10)
        .ToList();
        response.Messages.AddRange(messages);
        return Task.FromResult(response);
    }

    public override async Task SubscribeMessages(SubscribeRequest request, IServerStreamWriter<MessageResponse> responseStream, ServerCallContext context)
    {
        lock (_lock)
        {
            _subscribers.Add(responseStream);
        }
        while (!context.CancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000);
        }
        lock (_lock)
        {
            _subscribers.Remove(responseStream);
        }
    }

    public override async Task Chat(IAsyncStreamReader<MessageRequest> requestStream, IServerStreamWriter<MessageResponse> responseStream, ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync())
        {
            var msg = new MessageResponse
            {
                Id = Guid.NewGuid().ToString(),
                Sender = request.Sender,
                Text = request.Text,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            _messages.Add(msg);
            await responseStream.WriteAsync(msg);
        }
    }
    
}