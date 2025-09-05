using Grpc.Core;
using Grpc.Net.Client;
using grpcMessageClient;

class Program
{
    private static Message.MessageClient _client;
    private static string _username;
    private static CancellationTokenSource _cts = new();
    private static Task _subscribeTask = null;
    private static Task _chatReadTask = null;
    private static AsyncDuplexStreamingCall<MessageRequest, MessageResponse> _chatCall = null;

    static async Task Main(string[] args)
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:5119");
        _client = new Message.MessageClient(channel);

        Console.WriteLine("Kullanıcı adını giriniz: ");
        _username = Console.ReadLine();
        Console.WriteLine("Komutlar: /send, /get, /subscribe, /chat, /exit");

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;

            var split = input.Split(' ', 2);
            var command = split[0];
            var argument = split.Length > 1 ? split[1] : "";
            switch (command)
            {
                case "/send":
                    await SendMessage(argument);
                    break;

                case "/get":
                    await GetMessages();
                    break;

                case "/subscribe":
                    await SubscribeMessages();
                    break;

                case "/chat":
                    await StartChat();
                    break;

                case "/exit":
                    await Exit();
                    return;

                default:
                    if (_chatCall != null)
                        await SendChatMessage(input);
                    else
                        Console.WriteLine("Bilinmeyen komut.");
                    break;
            }
        }
    }

    private static async Task SendMessage(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        var response = await _client.SendMessageAsync(new MessageRequest
        {
            Sender = _username,
            Text = text
        });
        Console.WriteLine($"Gönderildi: {response.Id} - {response.Text}");
    }

    private static async Task GetMessages()
    {
        var response = await _client.GetMessageAsync(new GetMessageRequest
        {
            Sender = _username,
            Limit = 5
        });
        Console.WriteLine("Mesajlar:");
        foreach (var msg in response.Messages)
        {
            Console.WriteLine($"{msg.Sender}: {msg.Text} ({msg.Timestamp})");
        }
    }

    private static async Task SubscribeMessages()
    {
        if (_subscribeTask != null)
        {
            Console.WriteLine("Zaten abone olunmuş.");
            return;
        }

        _subscribeTask = Task.Run(async () =>
        {
            using var call = _client.SubscribeMessages(new SubscribeRequest { Channel = "general" });
            await foreach (var msg in call.ResponseStream.ReadAllAsync(_cts.Token))
            {
                Console.WriteLine($"[Subscribe] {msg.Sender}: {msg.Text}");
            }
        });
        Console.WriteLine("Abonelik başlatıldı.");
    }

    private static async Task StartChat()
    {
        if (_chatCall != null)
        {
            Console.WriteLine("Zaten chat başlatılmış.");
            return;
        }

        _chatCall = _client.Chat();
        _chatReadTask = Task.Run(async () =>
        {
            await foreach (var msg in _chatCall.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine($"[Chat] {msg.Sender}: {msg.Text}");
            }
        });
        Console.WriteLine("Chat başlatıldı. Mesaj yazıp enter ile gönderin.");
    }

    private static async Task SendChatMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        await _chatCall.RequestStream.WriteAsync(new MessageRequest
        {
            Sender = _username,
            Text = text
        });
    }

    private static async Task Exit()
    {
        _cts.Cancel();

        if (_chatCall != null)
            await _chatCall.RequestStream.CompleteAsync();

        if (_chatReadTask != null)
            await _chatReadTask;

        if (_subscribeTask != null)
            await _subscribeTask;

        Console.WriteLine("Çıkış yapıldı.");
    }
}


