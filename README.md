# dotnet-grpc-examples

Bu proje, **.NET 9** kullanarak yapılmış basit bir gRPC mesajlaşma örneğidir.
Hem **server** hem de **client** tarafını içermektedir ve unary, server-streaming ve bidirectional streaming iletişim türlerini göstermektedir.

---

## Özellikler

### Server
- `SendMessage`: Yeni mesaj gönderme (Unary RPC)
- `GetMessages`: Son mesajları listeleme (Unary RPC)
- `SubscribeMessages`: Gerçek zamanlı mesaj aboneliği (Server Streaming)
- `Chat`: Çoklu client arasında etkileşimli sohbet (Bidirectional Streaming)

### Client
- Konsol tabanlı interaktif client
- Komutlar:
  - `/send <text>` → mesaj gönderir
  - `/get` → son 5 mesajı listeler
  - `/subscribe` → gerçek zamanlı mesaj akışına abone olur
  - `/chat` → çift yönlü sohbet başlatır
  - `/exit` → güvenli çıkış yapar

---

## Çalıştırma Adımları

1. **Sunucuyu başlatın**
```bash
cd grpcServer
dotnet run
```

2. **Client'ı başlatın**
```bash
cd grpcClient
dotnet run
```

3. **Örnek Komutlar**
```bash
/send Merhaba gRPC!
/get
/subscribe
/chat
/exit
```


