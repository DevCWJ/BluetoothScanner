using Newtonsoft.Json.Linq;

using UnityEngine;
using UnityEngine.Events;

namespace CWJ.IoT
{
    using static CWJ.IoT.Define;
	public interface IDeviceDongle
    {
		string GetFixedDeviceID();
		bool IsInUseDongle();
		GameObject GetObject();
		IoTDeviceType GetDeviceType();
		DeviceDataRoot GetDeviceData();
        void OnChangeState(JObject state);
		void OnAddDevice(DeviceDataRoot deviceData);
        void OnDeleteDevice(DeviceDataRoot deviceData);

    }
	public abstract class DeviceDongleCore<T> : MonoBehaviour, IDeviceDongle where T : DeviceDataRoot, new()
	{
        [SerializeField, Readonly] IoTDeviceType fixedDeviceType;
		[SerializeField] string _fixedDeviceId;
		public string GetFixedDeviceID() => _fixedDeviceId;
		public bool IsInUseDongle() => (deviceData != null && !string.IsNullOrEmpty(deviceData.device_id));
		public T deviceData;

        public UnityEvent<DeviceDongleCore<T>> onDeviceAddEvent = new UnityEvent<DeviceDongleCore<T>>();
		public UnityEvent<DeviceDongleCore<T>> onStateChangeEvent = new UnityEvent<DeviceDongleCore<T>>();
		public UnityEvent<DeviceDongleCore<T>> onDeviceDeleteEvent = new UnityEvent<DeviceDongleCore<T>>();
		public GameObject GetObject() => gameObject;
        public IoTDeviceType GetDeviceType() => fixedDeviceType;
        public DeviceDataRoot GetDeviceData() => deviceData;

#if UNITY_EDITOR
        private void Reset()
		{
			InitDeviceType();
		}
        private void OnValidate()
        {
            InitDeviceType();
        }

        void InitDeviceType()
		{
			if (!Application.isPlaying)
			{
				if (deviceData == null)
					this.deviceData = new T();

				string className = this.GetType().Name;
                if (!className.ToLower().TryToEnum<IoTDeviceType>(out var deviceTypeByScriptName))
				{
					Debug.LogError($"{className}( || {className.ToLower()}) 은(는) {nameof(IoTDeviceType)}에 없는 이름입니다");
					return;
				}
				else
				{
                    if (fixedDeviceType != deviceTypeByScriptName)
                    {
                        fixedDeviceType = deviceTypeByScriptName;
                        CWJ.AccessibleEditor.EditorSetDirty.SetObjectDirty(this);
                    }
                }
			}
		}
#endif

		public virtual void OnChangeState(JObject state)
		{
            if (!IsInUseDongle())
            {
                return;
            }
            deviceData.UpdateState(state);
            onStateChangeEvent?.Invoke(this);
			this.enabled = true;
			WhenUpdateState();
        }


        protected virtual void WhenUpdateState()
		{

		}

		public virtual void OnAddDevice(DeviceDataRoot addData)
		{
			if (deviceData == null)
				this.deviceData = new T();

			this.deviceData.SetData(addData);
			gameObject.name = gameObject.name + "." + deviceData.device_id;
			onDeviceAddEvent?.Invoke(this);
			this.enabled = true;
			WhenUpdateState();
		}

		public virtual void OnDeleteDevice(DeviceDataRoot deleteData)
		{
			//gameObject.SetActive(false);
			if (!IsInUseDongle() || deleteData.device_id != deviceData.device_id)
			{
				return;
			}
			this.enabled = false;
			onDeviceDeleteEvent?.Invoke(this);
        }


        [InvokeButton]
        public void SendCurState(string sendKeyword)
        {
            deviceData.SendControlPayload(sendKeyword);
        }

        private void Awake()
        {
            this.enabled = IsInUseDongle();
        }
        private void OnEnable()
        {

        }
        private void OnDisable()
        {

        }
    }

}