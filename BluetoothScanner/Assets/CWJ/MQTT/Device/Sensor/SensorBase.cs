using Newtonsoft.Json.Linq;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CWJ.IoT
{
    [System.Serializable]
    public class Sensor_State : DeviceState
    {
        [Tooltip("0: undetect, 1:detected")]
        public int evt;

        public int low_batt;

        public override bool IsChanged(object other)
        {
            var otherState = (Sensor_State)other;
            return evt != otherState.evt || low_batt != otherState.low_batt;
        }
    }

    [System.Serializable]
    public class Sensor_Data : DeviceData<Sensor_State> { }

    public abstract class SensorBase : DeviceDongleCore<Sensor_Data>
    {
        [SerializeField, ErrorIfNull] string sensorKrName;
        [SerializeField] string detectedMsg = "센서가 감지했습니다";
        public string GetSensorMsg() => $"{sensorKrName} {detectedMsg}";

        public UnityEvent_String sensorDetectEvent = new UnityEvent_String();

        public override void OnChangeState(JObject state)
        {
            base.OnChangeState(state);

            if (deviceData.stateData.evt == 1)
                sensorDetectEvent?.Invoke(GetSensorMsg());
        }
    }
}

