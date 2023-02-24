using CWJ.IoT;

using MQTTnet;
using MQTTnet.Client;

using UnityEngine;
using System.Threading.Tasks;
using System.Text;

public class MqttConnectTester : MonoBehaviour, IDebugConnectable
{
    public IMqttClient client { get; set; }
    public int connectOrderIndex => 99;
    public IMqttClient GetNewClient(MqttConfigurationObj config)
    {
        var newClient = new MqttFactory().CreateMqttClient();
        newClient.ConnectedAsync += async e =>
        {
            if (newClient.IsConnected)
            {
                await newClient.SubscribeAsync("#", config.qosLevel);
                await client.SubscribeAsync("$SYS/");
            }
        };
        newClient.ApplicationMessageReceivedAsync += arg =>
        {
            Debug.Log($"{arg.ApplicationMessage.Topic}: {Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)}");
            return Task.CompletedTask;
        };
        return newClient;
    }

    [SerializeField] string _clientID = "DebugForUnity";
    public string clientID => _clientID;
    public MonoBehaviour GetTargetObj() => this;

    public bool isAutoReconnect => false;

    public void OnBeforeConnect() { }
    public void OnAfterConnect(bool isConnected) { }


}
