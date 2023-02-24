using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Linq;
using MQTTnet;
using MQTTnet.Client;
using CWJ.Serializable;
using System.Threading.Tasks;

namespace CWJ.IoT
{
    using static CWJ.IoT.Define;

    public class ReceivingManager : CWJ.Singleton.SingletonBehaviour<ReceivingManager>, IConnectable
    {
        [SerializeField] string _clientID = @"unity_subscribe";
        [SerializeField] string defaultReceiveTopic = Topic_iot_hub_from;

        [SerializeField] DictionaryVisualized<string, DeviceDataRoot> deviceDataCacheDicById = null;

        [NonSerialized, VisualizeField] IDeviceDongle[] allDeviceDonglesInScene = null;
        [NonSerialized] Dictionary<string, IDeviceDongle> dongleCacheDicById = null;

        [SerializeField] int maxActionsPerFrame = 10;

        #region IConnectable interface
        public string clientID => _clientID;

        public int connectOrderIndex => 0;
        public MonoBehaviour GetTargetObj() => this;
        public IMqttClient client { get; set; }
        public bool isAutoReconnect => true;
        public IMqttClient GetNewClient(MqttConfigurationObj config)
        {
            if (client != null)
            {
                return client;
            }
            client = new MqttFactory().CreateMqttClient();
            client.ConnectedAsync += async e =>
            {
                if(e.ConnectResult.ResultCode == MqttClientConnectResultCode.Success)
                {
                    await client.SubscribeAsync(defaultReceiveTopic, config.qosLevel);
                    if (config.isDebug)
                        await client.SubscribeAsync("$SYS/");
                }

            };

            client.ApplicationMessageReceivedAsync += arg =>
            {
                QueuedPayload.Enqueue(
                new QueuedEvent
                {
                    payload = arg.ApplicationMessage.Payload,
                    callback = OnPayloadReceived
                });
                return Task.CompletedTask;
            };
            if (config.isDebug)
            {
                client.ApplicationMessageReceivedAsync += arg =>
                {
                    string output = JsonConvert.SerializeObject(arg, new JsonSerializerSettings()
                    {
                        Formatting = Formatting.Indented
                    });
                    Debug.Log("[Receiving] " + output);
                    return Task.CompletedTask;
                };
            }

            return client;
        }

        public void OnBeforeConnect()
        {


            if (payloadFirstProcess == null)
            {
                payloadFirstProcess = new Dictionary<IoTEventType_Recieve, Action<string>>(capacity: 4)
                    {
                        { IoTEventType_Recieve.update_all_device, First_OnUpdateAllDevice },
                        { IoTEventType_Recieve.update_device, First_OnUpdateDevice },
                        { IoTEventType_Recieve.add_device, First_OnAddDevice },
                        { IoTEventType_Recieve.delete_device, First_OnDeleteDevice },
                    };
            }
        }

        public void OnAfterConnect(bool isConnected)
        {
            Debug.Log(clientID+"connected: "+isConnected);
            this.enabled = this.isConnected;
        }

        #endregion

        bool isConnected = false;
        protected override void _Awake()
        {
            allDeviceDonglesInScene = null;
            dongleCacheDicById = null;
            payloadFirstProcess = null;
            deviceDataCacheDicById = new DictionaryVisualized<string, DeviceDataRoot>();
            QueuedPayload = new Queue<QueuedEvent>();
            this.enabled = false;
        }

        protected override void _OnEnable()
        {
            if (allDeviceDonglesInScene == null)
                allDeviceDonglesInScene = FindUtil.FindInterfaces<IDeviceDongle>(true, false);

            if (dongleCacheDicById != null)
                dongleCacheDicById.Clear();
            dongleCacheDicById = new Dictionary<string, IDeviceDongle>(capacity: allDeviceDonglesInScene.Length);
        }

        public UnityEvent_String onReceiveMsgEvent = null;
        [NonSerialized] Dictionary<IoTEventType_Recieve, Action<string>> payloadFirstProcess = null;


        [InvokeButton]
        void OnPayloadReceived(string payload)
        {
#if UNITY_EDITOR
            Debug.Log($"{"Received message".SetColor(new Color().GetOrientalBlue())} : \"{payload}\" \nto topic {defaultReceiveTopic}");
#endif
            var store = JObject.Parse(payload);

            bool isValidEvtType = EnumUtil.TryToEnum(store[DataKeyword.event_type].ToString(), out IoTEventType_Recieve eventType);
            if (!isValidEvtType)
            {
                return;
            }

            payloadFirstProcess[eventType].Invoke(store[DataKeyword.data].ToString());
            onReceiveMsgEvent?.Invoke(payload);
#if UNITY_EDITOR
            Debug.Log("Process Success");
#endif
        }

        #region First Receive

        void First_OnUpdateAllDevice(string dataListJsonStr)
        {
            var receiveDatas = JArray.Parse(dataListJsonStr)
                .Where(j => j[DataKeyword.device_type]?.ToString()?.CanConvertToEnum<IoTDeviceType>() ?? false)
                .Select(j => JsonConvert.DeserializeObject<DeviceDataRoot>(j.ToString())).ToArray(); //ToString()없이 JToken -> JsonConvert 되는지?

            if (receiveDatas.Length == 0)
            {
                return;
            }

            var deleteDevices = deviceDataCacheDicById.Select((keyValue) => keyValue.Value)
                .Where((d) => !receiveDatas.IsExists(newData => newData.device_id == d.device_id)).ToArray();
            //if (deleteDevices.Length > 0)
            //    deleteDevices.ForEach(Core_OnDeleteDevice);

            var existingDatas = receiveDatas.FindAllWithMisMatch((d) => deviceDataCacheDicById.ContainsKey(d.device_id), out var addDevices);
            existingDatas.ForEach(Core_OnUpdateDevice);
            addDevices.ForEach(Core_OnAddDevice);
        }

        bool ConvertToDevice(string jsonStr, out DeviceDataRoot targetData)
        {
            try
            {
                targetData = JsonConvert.DeserializeObject<DeviceDataRoot>(jsonStr);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                targetData = null;
            }

            return targetData != null;
        }
        void First_OnUpdateDevice(string dataJsonStr)
        {
            if (!ConvertToDevice(dataJsonStr, out var updateDevice))
            {
                return;
            }
            Core_OnUpdateDevice(updateDevice);
        }


        void First_OnAddDevice(string dataJsonStr)
        {
            Debug.LogError("[TEST] 추가신호\n" + dataJsonStr);
            if (!ConvertToDevice(dataJsonStr, out var addDevice))
            {
                return;
            }
            Core_OnAddDevice(addDevice);
        }

        void First_OnDeleteDevice(string dataJsonStr)
        {
            Debug.LogError("[TEST] 삭제신호\n" + dataJsonStr);
            if (!ConvertToDevice(dataJsonStr, out var deleteDevice))
            {
                return;
            }
            Core_OnDeleteDevice(deleteDevice);
        }
        #endregion

        #region Core 

        bool TryFindDongle(DeviceDataRoot targetData, out IDeviceDongle dongle, bool isCreateWhenNull = false)
        {
            if (!dongleCacheDicById.TryGetValue(targetData.device_id, out dongle))
            {
                int index = -1;
                var disabledDonbles = allDeviceDonglesInScene.FindAll(d => d.GetDeviceType() == targetData.device_type && !d.IsInUseDongle());
                if (disabledDonbles.Any())
                {
                    string deviceID = targetData.device_id;
                    index = disabledDonbles.FindIndex(d => !string.IsNullOrEmpty(d.GetFixedDeviceID()) && deviceID.Equals(d.GetFixedDeviceID()));
                    if (index < 0)
                        index = disabledDonbles.FindIndex(d => string.IsNullOrEmpty(d.GetFixedDeviceID()));
                }

                if (index >= 0)
                    dongleCacheDicById.Add(targetData.device_id, dongle = disabledDonbles[index]);
                else if (isCreateWhenNull)
                {
                    Debug.LogError($"{targetData.device_type}-{targetData.device_id} 의 deviceDongle instance가 부족하여 add하지 못했다");
                }

                return false;
            }
            return true;
        }

        void Core_OnUpdateDevice(DeviceDataRoot updateDevice)
        {
            if (updateDevice == null || string.IsNullOrWhiteSpace(updateDevice.device_id))
            {
                return;
            }
            if (!deviceDataCacheDicById.TryGetValue(updateDevice.device_id, out var oldDeviceData))
            {
                Core_OnAddDevice(updateDevice);
                return;
            }

            if (!TryFindDongle(updateDevice, out var idongle))
            {
                return;
            }

            if (!updateDevice.IsStateChanged(oldDeviceData))
            {
                return;
            }

            if (idongle != null)
                idongle.OnChangeState(updateDevice.GetStateJsonObj());

            oldDeviceData.SetData(updateDevice);
#if UNITY_EDITOR
            Debug.Log($"{"[Device.state 변경됨]".SetColor(Color.yellow)}: {updateDevice.device_type.ToString()}-{updateDevice.device_id}\n{updateDevice.GetStateJsonObj().ToString()}");
#endif
        }

        void Core_OnAddDevice(DeviceDataRoot addDevice)
        {
            if (!TryFindDongle(addDevice, out var idongle, isCreateWhenNull: true))
            {
            }
            if (idongle != null)
                idongle.OnAddDevice(addDevice);

            if (!deviceDataCacheDicById.ContainsKey(addDevice.device_id))
                deviceDataCacheDicById.Add(addDevice.device_id, addDevice);
#if UNITY_EDITOR
            Debug.Log($"{"[Device 추가됨]".SetColor(Color.cyan)}: {addDevice.device_type.ToString()}-{addDevice.device_id}");
#endif
        }

        void Core_OnDeleteDevice(DeviceDataRoot deleteDevice)
        {
            if (!TryFindDongle(deleteDevice, out var idongle))
            {
                return;
            }
            if (idongle != null)
                idongle.OnDeleteDevice(deleteDevice);
            if (deviceDataCacheDicById.ContainsKey(deleteDevice.device_id))
                deviceDataCacheDicById.Remove(deleteDevice.device_id);
#if UNITY_EDITOR
            Debug.Log($"{"[Device 제거됨]".SetColor(Color.red)}: {deleteDevice.device_type.ToString()}-{deleteDevice.device_id}");
#endif
        }

        #endregion

        static Queue<QueuedEvent> QueuedPayload;
        public struct QueuedEvent
        {
            public byte[] payload;
            public Action<string> callback;
        }

        void Update()
        {
            lock (QueuedPayload)
            {
                int i = 0;
                while (QueuedPayload.Count > 0 && i < maxActionsPerFrame)
                {
                    QueuedEvent queued = QueuedPayload.Dequeue();

                    try
                    {
                        if (queued.payload.LengthSafe() > 0)
                        {
                            var payloadStr = System.Text.Encoding.UTF8.GetString(queued.payload);
                            if (!string.IsNullOrEmpty(payloadStr))
                                queued.callback.Invoke(payloadStr);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                    }
                    ++i;
                }
            }
        }
    }
}