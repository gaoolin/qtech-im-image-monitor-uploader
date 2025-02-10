using Confluent.Kafka;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Text;

namespace ImMonitorUploader.Sinks
{
    public class KafkaLogSink : ILogEventSink, IDisposable
    {
        private readonly IProducer<Null, string> _producer;
        private readonly string _topic;

        public KafkaLogSink(string kafkaServers, string topic)
        {
            _topic = topic;

            var config = new ProducerConfig
            {
                BootstrapServers = kafkaServers
            };

            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public void Emit(LogEvent logEvent)
        {
            // 构建日志消息
            var logMessage = new StringBuilder();
            logMessage.Append(logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            logMessage.Append(" [").Append(logEvent.Level).Append("] ");
            logMessage.Append(logEvent.RenderMessage());

            try
            {
                // 将日志发送到 Kafka
                _producer.Produce(_topic, new Message<Null, string> { Value = logMessage.ToString() });
                _producer.Flush(TimeSpan.FromSeconds(5)); // 等待消息发送完成
            }
            catch (Exception ex)
            {
               Log.Error($"Kafka 日志发送失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _producer?.Dispose();
        }
    }
}
