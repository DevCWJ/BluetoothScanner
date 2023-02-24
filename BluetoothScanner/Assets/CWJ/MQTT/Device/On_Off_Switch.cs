using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CWJ.IoT
{

    [System.Serializable]
    public class OnOff_State : DeviceState
    {
        public int evt_1;
        public int evt_2;
        public int evt_3;
        public int evt_4;
        public int act_1;
        public int act_2;
        public int act_3;
        public int act_4;

        public override bool IsChanged(object other)
        {
            var otherState = (OnOff_State)other;
            return
                    evt_1 != otherState.evt_1
                || evt_2 != otherState.evt_2
                || evt_3 != otherState.evt_3
                || evt_4 != otherState.evt_4
                || act_1 != otherState.act_1
                || act_2 != otherState.act_2
                || act_3 != otherState.act_3
                || act_4 != otherState.act_4;
        }
    }

    [System.Serializable]
    public class OnOff_Data : DeviceData<OnOff_State> { }
    public class On_Off_Switch : DeviceDongleCore<OnOff_Data>
    {
        [SerializeField, ErrorIfNull] string sensorKrName;
        [SerializeField] string detectedMsg = "센서가 감지했습니다";
        public string GetSensorMsg() => $"{sensorKrName} {detectedMsg}";

        public UnityEvent_String sensorDetectEvent = new UnityEvent_String();

        public override void OnChangeState(JObject state)
        {
            base.OnChangeState(state);
            sensorDetectEvent?.Invoke(GetSensorMsg());
        }
    }
}