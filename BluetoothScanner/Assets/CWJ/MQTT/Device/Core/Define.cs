namespace CWJ.IoT
{
    public static class Define
    {
        public const string Topic_iot_hub_from = "/iot_hub/from";
        public const string Topic_iot_hub_to = "/iot_hub/to";
        public static class DataKeyword
        {
            public const string data = "data";
            public const string event_type = "event_type";
            public const string device_id = "device_id";
            public const string device_type = "device_type";
            public const string state = "state";
        }

        public enum IoTDeviceType
        {
            /// <summary>
            /// 도어센서
            /// </summary>
            door_sensor,

            /// <summary>
            /// 재실감지센서 
            /// </summary>
            motion_sensor,

            /// <summary>
            /// 온습도센서 
            /// </summary>
            temperature_humidity,

            /// <summary>
            /// 스위치 
            /// </summary>
            on_off_switch,

            /// <summary>
            /// 진동센서 
            /// </summary>
            vibration_sensor,

            /// <summary>
            /// 연기감지센서 
            /// </summary>
            smoke_sensor,

            /// <summary>
            /// 커튼 
            /// </summary>
            curtain,

            /// <summary>
            /// 유저버튼 
            /// </summary>
            user_button,

            /// <summary>
            /// 비상버튼 
            /// </summary>
            emergency_button,

            /// <summary>
            /// 릴레이 스위치 
            /// </summary>
            relay_switch,

            /// <summary>
            /// 조도센서 
            /// </summary>
            illuminance_sensor,

            /// <summary>
            /// 수분센서 
            /// </summary>
            water_sensor,

            /// <summary>
            /// 스마트플러그
            /// </summary>
            smart_plug,

            /// <summary>
            /// 조명 스위치
            /// </summary>
            light_switch,

            /// <summary>
            /// 스마트 콘센트
            /// </summary>
            smart_concent,

            /// <summary>
            /// 
            /// </summary>
            gaslock
        }
        public enum IoTEventType_Send
        {
            /// <summary>
            /// 모든 디바이스 상태 요청 (init)
            /// </summary>
            read_all_device=0,

            /// <summary>
            /// 디바이스 제어
            /// </summary>
            control_device
        }

        public enum IoTEventType_Recieve
        {
            /// <summary>
            /// 상태 수신
            /// </summary>
            update_device = 0,

            /// <summary>
            /// 모든 디바이스 상태 수신
            /// </summary>
            update_all_device,

            /// <summary>
            /// 허브에 디바이스 추가
            /// </summary>
            add_device,

            /// <summary>
            /// 허브에 디바이스 제거
            /// </summary>
            delete_device
        }
    }
}