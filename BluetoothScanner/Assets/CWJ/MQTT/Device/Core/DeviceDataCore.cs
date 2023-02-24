using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

using UnityEngine;

namespace CWJ.IoT
{
    using static CWJ.IoT.Define;

    [System.Serializable]
    public class DeviceDataRoot
    {
        [Readonly] public IoTDeviceType device_type;
        [Readonly] public string device_id;
        public JObject state;
        public JObject GetStateJsonObj() => state;

        public void SetData(DeviceDataRoot deviceDataRoot)
        {
            this.device_id = deviceDataRoot.device_id;
            this.device_type = deviceDataRoot.device_type;
            UpdateState(state = deviceDataRoot.state);
        }

        public virtual void UpdateState(JObject state) { }

        public override int GetHashCode()
        {
            return HashCodeHelper.GetHashCode(device_id, state);
        }

        public bool IsStateChanged(DeviceDataRoot other) => !JToken.DeepEquals(state, other.state);

        public virtual void SendControlPayload(string[] sendKeywords, IoTEventType_Send eventType_Send = IoTEventType_Send.control_device) { }
        public void SendControlPayload(string sendKeyword, IoTEventType_Send eventType_Send = IoTEventType_Send.control_device)
        {
            SendControlPayload(new string[1] { sendKeyword }, eventType_Send);
        }
    }

    [System.Serializable]
    public class DeviceData<TState> : DeviceDataRoot where TState : DeviceState
    {
        public TState stateData;

        public override void UpdateState(JObject state)
        {
            if (state != null)
                stateData = JsonConvert.DeserializeObject<TState>(state.ToString());
        }

        struct SendStruct
        {
            public string event_type;
            public Data data;
            public struct Data
            {
                public string device_id;
                public TState state;

                public Data(string device_id, TState state)
                {
                    this.device_id = device_id;
                    this.state = state;
                }
            }

            public SendStruct(string deviceId, TState state, IoTEventType_Send eventType)
            {
                this.event_type = eventType.ToString();
                this.data = new Data(deviceId, state);
            }
        }

        public override void SendControlPayload(string[] sendKeywords, IoTEventType_Send eventType_Send = IoTEventType_Send.control_device)
        {
            if (string.IsNullOrEmpty(device_id) || stateData == null || eventType_Send == IoTEventType_Send.read_all_device)
            {
                return;
            }

            var sendJsonObj = JObject.FromObject(new SendStruct(this.device_id, this.stateData, eventType_Send));
            if (sendKeywords.LengthSafe() > 0)
            {
                var properties = ((JObject)sendJsonObj[DataKeyword.data][DataKeyword.state]).Properties();

                properties.ToArray().ForEach(p =>
                {
                    if (!sendKeywords.IsExists(p.Name))
                        p.Remove();
                });

                //if (properties.Count() == 0)
                //{
                //    return;
                //}
            }

            string jsonStr = sendJsonObj.ToString();

            _ = SendingManager.Instance.Async_SendMessage(Topic_iot_hub_to, jsonStr);
        }
    }

    public abstract class DeviceState
    {
        public abstract bool IsChanged(object other);
    }
}