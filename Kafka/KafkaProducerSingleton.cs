using Confluent.Kafka;
using System;
using System.Configuration;

public class KafkaProducerSingleton
{
    private static readonly Lazy<IProducer<Null, string>> producerInstance = new Lazy<IProducer<Null, string>>(() =>
    {
        var config = new ProducerConfig { BootstrapServers = ConfigurationManager.AppSettings["KafkaServers"] };
        return new ProducerBuilder<Null, string>(config).Build();
    });

    public static IProducer<Null, string> Producer => producerInstance.Value;

    public static void Dispose()
    {
        if (producerInstance.IsValueCreated)
        {
            Producer.Flush(TimeSpan.FromSeconds(5));
            Producer.Dispose();
        }
    }
}
