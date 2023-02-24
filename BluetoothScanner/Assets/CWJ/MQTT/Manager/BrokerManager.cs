using System;
using MQTTnet;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Server;
using UnityEngine;
using CWJ.Singleton;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

namespace CWJ.IoT
{
    public class BrokerManager : SingletonBehaviour<BrokerManager>
    {
        private MqttServer broker;

        MqttServerOptionsBuilder serverOption = null;
        string userName, password;
        public IMqttNetLogger GetNewLogger()
        {
            IMqttNetLogger logger;
            if (MqttConnector.Config.isDebug)
            {
                var eventLogger = new MqttNetEventLogger("BrokerDebugLogger");
                eventLogger.LogMessagePublished += (o, eArg) =>
                {
                    if (eArg.LogMessage.Level == MqttNetLogLevel.Verbose)
                    {
                        return;
                    }
                    var trace =
                        $">> [{eArg.LogMessage.Timestamp:O}] [{eArg.LogMessage.ThreadId}] [{eArg.LogMessage.Source}] [{eArg.LogMessage.Level}]: {eArg.LogMessage.Message}";
                    if (eArg.LogMessage.Exception != null)
                        trace += Environment.NewLine + eArg.LogMessage.Exception.ToString();

                    if (eArg.LogMessage.Level == MqttNetLogLevel.Error)
                        Debug.LogError(trace);
                    else if (eArg.LogMessage.Level == MqttNetLogLevel.Warning)
                        Debug.LogWarning(trace);
                    else
                        print(trace);
                };
                logger = eventLogger;
            }
            else
                logger = new MqttNetNullLogger();
            return logger;
        }
        protected override async void _Start()
        {
            serverOption = MqttConnector.Instance.GetServerOption();
            broker = new MqttFactory(GetNewLogger()).CreateMqttServer(serverOption.Build());
            if (MqttConnector.Config.isDebug)
            {
                broker.ClientConnectedAsync += broker_ClientConnectedAsync;
                broker.ClientDisconnectedAsync += broker_ClientDisconnectedAsync;
                broker.ApplicationMessageNotConsumedAsync += broker_ApplicationMessageNotConsumedAsync;

                broker.ClientSubscribedTopicAsync += broker_ClientSubscribedTopicAsync;
                broker.ClientUnsubscribedTopicAsync += broker_ClientUnsubscribedTopicAsync;
                broker.StartedAsync += mqttServer_StartedAsync;
                broker.StoppedAsync += broker_StoppedAsync;
                broker.InterceptingPublishAsync += broker_InterceptingPublishAsync;
            }
            userName = MqttConnector.Config.userName;
            password = MqttConnector.Config.password;
            if (MqttConnector.Config.useValidateCredentials)
            {
                broker.ValidatingConnectionAsync += validator =>
                {
                    bool isVerified = ((validator.UserName ?? string.Empty) != userName || (validator.Password ?? string.Empty) != password);
                    validator.ReasonCode = isVerified
                                            ? MqttConnectReasonCode.Success : MqttConnectReasonCode.BadUserNameOrPassword;
                    if (!isVerified)
                    {
                        Debug.LogError($"Try Validate Credentials \nClientID: {validator.ClientId}\nUsername: {validator.UserName}\nPassword:{validator.Password}\nEndpoint:{validator.Endpoint}");
                    }
                    return Task.CompletedTask;
                };
            }

            await broker.StartAsync();
        }

        protected async override void _OnDestroy()
        {
            await ForceDisconnectingClient();
            await broker.StopAsync();
            broker.Dispose();
        }

        public async Task ForceDisconnectingClient(string clientId = null)
        {
            var allClients = (await broker.GetClientsAsync());
            MqttClientStatus[] mqttClientStatuses;
            if (clientId != null)
                mqttClientStatuses = new MqttClientStatus[1] { allClients.FirstOrDefault(c => c.Id == clientId) };
            else
                mqttClientStatuses = allClients.ToArray();

            for (int i = 0; i < mqttClientStatuses.Length; i++)
            {
                if (mqttClientStatuses[i] != null)
                    await mqttClientStatuses[i].DisconnectAsync();
            }
        }

        private Task broker_ClientSubscribedTopicAsync(ClientSubscribedTopicEventArgs arg)
        {
            Debug.Log($"Subscribed Topic : ClientID=【{arg.ClientId}】\nTopicFilter=【{arg.TopicFilter}】\n ");
            return Task.CompletedTask;
        }

        private Task broker_StoppedAsync(EventArgs arg)
        {
            Debug.Log($"Stopped : MQTT Server 종료");
            return Task.CompletedTask;
        }

        private Task broker_InterceptingPublishAsync(InterceptingPublishEventArgs arg)
        {
            Debug.Log($"Intercepting Publish：Client ID=【{arg.ClientId}】 Topic=【{arg.ApplicationMessage.Topic}】" +
                $"\n Payload=【{Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)}】" +
                $"\n qosLvl=【{arg.ApplicationMessage.QualityOfServiceLevel}】\n");
            return Task.CompletedTask;

        }

        private Task mqttServer_StartedAsync(EventArgs arg)
        {
            Debug.Log($"Started : MQTT Server 시작");
            return Task.CompletedTask;
        }

        private Task broker_ClientUnsubscribedTopicAsync(ClientUnsubscribedTopicEventArgs arg)
        {
            Debug.Log($"Unsubscribed Topic : ClientID=【{arg.ClientId}】\n TopicFilter=【{arg.TopicFilter}】\n  ");
            return Task.CompletedTask;
        }

        private Task broker_ApplicationMessageNotConsumedAsync(ApplicationMessageNotConsumedEventArgs arg)
        {
            Debug.Log($"MessageNotConsumed : SenderID=【{arg.SenderId}】 Topic=【{arg.ApplicationMessage.Topic}】" +
                $"\n Payload=【{Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)}】\n qosLv=【{arg.ApplicationMessage.QualityOfServiceLevel}】\n");
            return Task.CompletedTask;
        }


        private Task broker_ClientDisconnectedAsync(ClientDisconnectedEventArgs arg)
        {
            Debug.Log($"Client Disconnected：Client ID=【{arg.ClientId}】\n endpoint=【{arg.Endpoint}】\n  ");
            return Task.CompletedTask;
        }

        private Task broker_ClientConnectedAsync(ClientConnectedEventArgs arg)
        {
            Debug.Log($"Client Connected：Client ID=【{arg.ClientId}】\n endpoint=【{arg.Endpoint}】\n  ");
            return Task.CompletedTask;
        }
    }
}