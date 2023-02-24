
using UnityEngine;

namespace CWJ.IoT
{
    [System.Serializable]
    public class SmartPlugOrConcent_State : DeviceState
    {
        public int act;

        [Tooltip("전력값, 단위 - 0.1watt")]
        public int metering;

        public override bool IsChanged(object other)
        {
            var otherState = (SmartPlugOrConcent_State)other;
            return act != otherState.act || metering != otherState.metering;
        }
    }

    [System.Serializable]
    public class SmartPlugOrConcent_Data : DeviceData<SmartPlugOrConcent_State> { }

    public class Smart_Plug : DeviceDongleCore<SmartPlugOrConcent_Data> { }
}