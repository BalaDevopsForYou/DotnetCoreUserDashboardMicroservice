

using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace UserDashBoardMicroservice.Consumer
{
    public class UserDashboardConsumer : IDisposable
    {
        private readonly ConnectionFactory _factory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IConfiguration _configuration;

        public UserDashboardConsumer(IConfiguration configuration)
        {
            _configuration = configuration;

            var connectionString = _configuration.GetConnectionString("RabbitMQConnection");
            _factory = new ConnectionFactory() { Uri = new Uri(connectionString) };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public async Task<UserData> StartListeningAsync()
        {
            try
            {
                _channel.QueueDeclare(queue: "user_login_queue",
                                      durable: false,
                                      exclusive: false,
                                      autoDelete: false,
                                      arguments: null);

                var tcs = new TaskCompletionSource<UserData>();

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine(" [x] Received user login message: {0}", message);
                    var userData = await DisplayUserDataAsync(message);
                    tcs.TrySetResult(userData);
                };

                _channel.BasicConsume(queue: "user_login_queue", autoAck: true, consumer: consumer);

                Console.WriteLine("User Dashboard Microservice is listening for user login messages...");

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting listening: {ex.Message}");
                throw;
            }
        }

        private async Task<UserData> DisplayUserDataAsync(string message)
        {
            var userData = Newtonsoft.Json.JsonConvert.DeserializeObject<UserData>(message);

            Console.WriteLine("User Name: {0}", userData.Name);
            Console.WriteLine("Email: {0}", userData.Email);
            Console.WriteLine("Contact: {0}", userData.Contact);
            Console.WriteLine("Image: {0}", userData.Image);
            Console.WriteLine("Role: {0}", userData.Role);
            Console.WriteLine("Role Description: {0}", userData.RoleDescription);
            Console.WriteLine("---------------------------------");

            return userData;
        }

        public void CloseConnection()
        {
            // Implement logic to close the connection if needed
        }

        public async Task DisposeAsync()
        {
            await Task.Run(() =>
            {
                _channel.Dispose();
                _connection.Dispose();
            });
        }

        public void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();
        }
    }
}
