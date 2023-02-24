
using Newtonsoft.Json.Linq;

using UnityEngine;

namespace CWJ.IoT
{
    [System.Serializable]
    public class LightSwitch_State : DeviceState
    {
        [Tooltip("0:off / 1:on ")]
        public int act_1;
        public int act_2;
        public int act_3;
        public int act_4;
        public int act_5;

        public void SetAct(bool isOn)
        {
            int act = isOn ? 1 : 0;
            act_1 = act;
            act_2 = act;
            act_3 = act;
            act_4 = act;
            act_5 = act;
        }

        public override bool IsChanged(object other)
        {
            var otherState = (LightSwitch_State)other;
            return act_1 != otherState.act_1 ||
                act_2 != otherState.act_2 ||
                act_3 != otherState.act_3 ||
                act_4 != otherState.act_4 ||
                act_5 != otherState.act_5;
        }
    }

    [System.Serializable]
    public class LightSwitch_Data : DeviceData<LightSwitch_State> { }

    public class Light_Switch : DeviceDongleCore<LightSwitch_Data>
    {

        public void LightOff()
        {
            SetSwitchLight(false);
        }

        public void LightOn()
        {
            SetSwitchLight(true);
        }

        void SetSwitchLight(bool isOn)
        {
            deviceData.stateData.SetAct(isOn);
            SendCurState(null);
        }

        public UnityEvent_Bool onChangeLightState = new UnityEvent_Bool();

        protected override void WhenUpdateState()
        {
            onChangeLightState?.Invoke(deviceData.stateData.act_1 == 1);
        }
    }
}