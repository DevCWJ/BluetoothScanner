using System.Threading.Tasks;
using MQTTnet.Client;

using Newtonsoft.Json.Linq;
using UnityEngine;
using MQTTnet.Protocol;
using MQTTnet;

namespace CWJ.IoT
{
    using static CWJ.IoT.Define;

    public class SendingManager : CWJ.Singleton.SingletonBehaviour<SendingManager>, IConnectable
    {
        [SerializeField] string _clientID = @"unity_publish";
        [SerializeField] string defaultSendTopic = Topic_iot_hub_to;

        #region IConnectable interface
        public int connectOrderIndex => 1;
        public MonoBehaviour GetTargetObj() => this;
        public IMqttClient client { get; set; }
        public IMqttClient GetNewClient(MqttConfigurationObj config)
        {
            if (client != null)
            {
                return client;
            }
            client = new MqttFactory().CreateMqttClient();
            return client;
        }
        public string clientID => _clientID;

        public bool isAutoReconnect => true;

        public void OnBeforeConnect() { }

        public void OnAfterConnect(bool isConnected)
        {
            Debug.Log("connected: "+isConnected);
            this.enabled = this.isConnected && (!string.IsNullOrEmpty(pollingPayload) && !string.IsNullOrEmpty(pollingTopic));
        }


        #endregion

        MqttQualityOfServiceLevel qosLevel;
        bool isConnected = false;

        protected override void _Awake()
        {
            MqttConnector.Instance.afterAllConnectedEvent += SendMessage_ReadAllDevice;
            qosLevel = MqttConnector.Config.qosLevel;
            this.enabled = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="payload"></param>
        /// <param name="retain"> 서버가 메시지를 보관하는지 여부. 새 구독자가 연결하면 메시지가 즉시 수신됩니다.</param>
        /// <returns></returns>
        public async Task Async_SendMessage(string topic, string payload, bool retain = false)
        {
            if (client == null || !client.IsConnected)
            {
                Debug.LogError("Tried to send MQTT message, but client is not connected");
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"{"Send message".SetColor(new Color().GetLightRed())} : \"{payload}\" \nto topic {topic}{(retain ? " (retained)" : "")}");
#endif
            
            await client.PublishStringAsync(topic, payload, qosLevel, retain);
        }

        [InvokeButton]
        void SendMessage_ReadAllDevice()
        {
            SendMessage(JObject.FromObject(new { event_type = IoTEventType_Send.read_all_device.ToString() }).ToString());
        }
        [InvokeButton]
        void SendMessage(string payload, string topic = null, bool isRetain = false)
        {
            if (topic == null)
                topic = defaultSendTopic;

            _ = Async_SendMessage(topic ?? defaultSendTopic, payload, isRetain);
        }

        private float timer = 0;
        [Tooltip("반복해서 보낼 payload의 topic")]
        [SerializeField] string pollingTopic;
        [Tooltip("반복해서 보낼 payload")]
        [SerializeField] string pollingPayload;
        [Tooltip("반복해서 보내는 주기")]
        [SerializeField, Min(0.1f)] float pollingInterval = 1.5f;

        protected override void _OnEnable()
        {
            timer = 0;
        }
        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= pollingInterval)
            {
                timer = 0;
                _ = Async_SendMessage(pollingTopic, pollingPayload, retain: false);
            }
        }


    }
}