using RabbitMQ.Client;

namespace HSE.Contest.ClassLibrary.RabbitMQ
{
    public class RabbitHutch
    {
        private static ConnectionFactory _factory;
        private static IConnection _connection;
        private static IModel _channel;
        public static IBus CreateBus(RabbitMQConfig config, ContainerConfig curConfig)
        {
            _factory = new ConnectionFactory
            {
                HostName = config.GetHost(curConfig),
                Port = config.Port,
                DispatchConsumersAsync = true,
                UserName = config.Username,
                Password = config.Password
            };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            return new RabbitBus(_channel);
        }
        public static IBus CreateBus(
        string hostName,
        ushort hostPort,
        string virtualHost,
        string username,
        string password)
        {
            _factory = new ConnectionFactory
            {
                HostName = hostName,
                Port = hostPort,
                VirtualHost = virtualHost,
                UserName = username,
                Password = password,
                DispatchConsumersAsync = true
            };

            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            return new RabbitBus(_channel);
        }
    }
}
