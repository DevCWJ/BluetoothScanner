using Newtonsoft.Json.Linq;

using UnityEngine;

namespace CWJ.IoT
{
    [System.Serializable]
    public class Curtain_State : DeviceState
    {
        [Tooltip("0~100 percentage")]
        public int dimmer;

        [Tooltip("0:none, 1:open, 2:close, 3:stop:, 4:learn, 5:reverse_learn")]
        public int act;

        public enum ActType
        {
            @null = -1,
            done = 0,
            open = 1,
            close = 2,
            stop = 3,
            learn = 4,
            reverse_learn = 5
        }

        public ActType GetActType() => CWJ.EnumUtil.ToEnum<ActType>(act);

        public override bool IsChanged(object other)
        {
            var otherState = (Curtain_State)other;
            return act != otherState.act || dimmer != otherState.dimmer;
        }
    }

    [System.Serializable]
    public class Curtain_Data : DeviceData<Curtain_State> { }

    public class Curtain : DeviceDongleCore<Curtain_Data>
    {

        protected override void WhenUpdateState()
        {
            var newAct = deviceData.stateData.GetActType();
            var newDimmer = deviceData.stateData.dimmer;
            if (newAct == lastAct && newDimmer == lastDimmer)
            {
                return;
            }

            curtainStateEvent?.Invoke(lastAct, newAct, newDimmer);
            lastAct = newAct;
            lastDimmer = newDimmer;
        }

        [InvokeButton]
        public void SendAct(Curtain_State.ActType actType)
        {
            deviceData.stateData.act = actType.ToInt();
            SendCurState(nameof(deviceData.stateData.act));
        }

        public UnityEngine.Events.UnityEvent<Curtain_State.ActType, Curtain_State.ActType, float> curtainStateEvent = new UnityEngine.Events.UnityEvent<Curtain_State.ActType, Curtain_State.ActType, float>();
        Curtain_State.ActType lastAct = Curtain_State.ActType.@null;
        int lastDimmer = -1;

        public void SendOpen()
        {
            SendAct(Curtain_State.ActType.open);
        }
        public void SendClose()
        {
            SendAct(Curtain_State.ActType.close);
        }

    }

} 