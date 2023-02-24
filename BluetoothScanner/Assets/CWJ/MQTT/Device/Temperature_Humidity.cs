using Newtonsoft.Json.Linq;

using TMPro;

using UnityEngine;

namespace CWJ.IoT
{
    [System.Serializable]
    public class TempsHumidity_State : DeviceState
    {
        [Tooltip("%")] public int hum;
        [Tooltip("265 : 26.5")] public int temp;
        public int low_batt;

        public override bool IsChanged(object other)
        {
            var otherState = (TempsHumidity_State)other;
            return hum != otherState.hum || temp != otherState.temp || low_batt != otherState.low_batt;
        }
    }

    [System.Serializable]
    public class TempsHumidity_Data : DeviceData<TempsHumidity_State> { }

    public class Temperature_Humidity : DeviceDongleCore<TempsHumidity_Data>
    {
        [SerializeField] TMPro.TextMeshPro tempText;
        [SerializeField] TMPro.TextMeshPro humText;

        protected override void WhenUpdateState()
        {
            if (tempText != null)
                tempText.text = $"온도 {deviceData.stateData.temp * 0.1f}°C";
            if (humText != null)
                humText.text = $"습도 {deviceData.stateData.hum}%";
        }
    }

}