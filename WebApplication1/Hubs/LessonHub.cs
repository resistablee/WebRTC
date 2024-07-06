using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using WebRTC.Models;

namespace WebRTC.Hubs
{
    public class LessonHub : Hub
    {
        private static ConcurrentDictionary<string, List<UserMessage>> lessonMessages = new ConcurrentDictionary<string, List<UserMessage>>();
        private static ConcurrentDictionary<string, List<string>> lessonUsers = new ConcurrentDictionary<string, List<string>>();

        // Bir ders için mesaj gönderme işlemini gerçekleştiren metot
        public async Task SendMessage(string lessonId, string user, string message)
        {
            var timestamp = DateTime.UtcNow;

            var msg = new UserMessage
            {
                User = user,
                Content = message,
                Timestamp = timestamp
            };

            if (lessonMessages.TryGetValue(lessonId, out var messages))
            {
                messages.Add(msg);
            }
            else
            {
                lessonMessages.TryAdd(lessonId, new List<UserMessage> { msg });
            }

            await Clients.Group(lessonId).SendAsync("ReceiveMessage", Context.ConnectionId, user, message, timestamp);
        }

        // Kullanıcının kamerasını açıp kapama işlemini gerçekleştiren metot
        public async Task ToggleCamera(string lessonId, string userId, bool isCameraOn)
        {
            await Clients.Group(lessonId).SendAsync("ToggleCamera", userId, isCameraOn);
        }

        public async Task SendOffer(string lessonId, string senderId, string targetId, string sdp)
        {
            await Clients.Client(targetId).SendAsync("ReceiveOffer", senderId, sdp);
        }

        public async Task SendAnswer(string lessonId, string senderId, string targetId, string sdp)
        {
            await Clients.Client(targetId).SendAsync("ReceiveAnswer", senderId, sdp);
        }

        public async Task SendCandidate(string lessonId, string senderId, string targetId, string candidate)
        {
            await Clients.Client(targetId).SendAsync("ReceiveCandidate", senderId, candidate);
        }

        // Kullanıcının bir ders odasına katılmasını sağlayan metot
        public async Task UserJoinedToLesson(string lessonId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lessonId);

            // Kullanıcıyı derse ekliyoruz
            lessonUsers.AddOrUpdate(lessonId, new List<string> { Context.ConnectionId }, (key, existingList) =>
            {
                existingList.Add(Context.ConnectionId);
                return existingList;
            });

            // Mevcut kullanıcıların listesini yeni kullanıcıya gönderiyoruz
            var users = lessonUsers[lessonId].Where(id => id != Context.ConnectionId).ToList();
            await Clients.Caller.SendAsync("ExistingUsers", users);

            // Diğer kullanıcılara yeni bir kullanıcının derse katıldığını bildiriyoruz
            await Clients.Group(lessonId).SendAsync("UserJoined", Context.ConnectionId);

            // Kullanıcıya önceki mesajları yüklüyoruz
            if (lessonMessages.TryGetValue(lessonId, out var messages))
            {
                await Clients.Caller.SendAsync("LoadMessages", messages);
            }

            // Kullanıcının ders odasına katıldığını bildiriyoruz
            await SendMessage(lessonId, "System", $"{Context.ConnectionId} joined the lesson.");
        }

        // Kullanıcının bir ders odasına katılmasını sağlayan metot
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"New user connected. ConnectionId: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        // Kullanıcının bir ders odasından ayrılmasını sağlayan metot
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var lessonId = lessonUsers.FirstOrDefault(x => x.Value.Contains(Context.ConnectionId)).Key;

            if (!string.IsNullOrEmpty(lessonId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, lessonId);

                if (lessonUsers.TryGetValue(lessonId, out var users))
                {
                    users.Remove(Context.ConnectionId);
                    if (users.Count == 0)
                    {
                        lessonUsers.TryRemove(lessonId, out _);
                    }
                }

                await Clients.Group(lessonId).SendAsync("UserLeft", Context.ConnectionId);

                // Kullanıcının ders odasına katıldığını bildiriyoruz
                await SendMessage(lessonId, "System", $"{Context.ConnectionId} left the lesson.");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}