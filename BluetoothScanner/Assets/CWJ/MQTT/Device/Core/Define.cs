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
            /// �����
            /// </summary>
            door_sensor,

            /// <summary>
            /// ��ǰ������� 
            /// </summary>
            motion_sensor,

            /// <summary>
            /// �½������� 
            /// </summary>
            temperature_humidity,

            /// <summary>
            /// ����ġ 
            /// </summary>
            on_off_switch,

            /// <summary>
            /// �������� 
            /// </summary>
            vibration_sensor,

            /// <summary>
            /// ���Ⱘ������ 
            /// </summary>
            smoke_sensor,

            /// <summary>
            /// Ŀư 
            /// </summary>
            curtain,

            /// <summary>
            /// ������ư 
            /// </summary>
            user_button,

            /// <summary>
            /// ����ư 
            /// </summary>
            emergency_button,

            /// <summary>
            /// ������ ����ġ 
            /// </summary>
            relay_switch,

            /// <summary>
            /// �������� 
            /// </summary>
            illuminance_sensor,

            /// <summary>
            /// ���м��� 
            /// </summary>
            water_sensor,

            /// <summary>
            /// ����Ʈ�÷���
            /// </summary>
            smart_plug,

            /// <summary>
            /// ���� ����ġ
            /// </summary>
            light_switch,

            /// <summary>
            /// ����Ʈ �ܼ�Ʈ
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
            /// ��� ����̽� ���� ��û (init)
            /// </summary>
            read_all_device=0,

            /// <summary>
            /// ����̽� ����
            /// </summary>
            control_device
        }

        public enum IoTEventType_Recieve
        {
            /// <summary>
            /// ���� ����
            /// </summary>
            update_device = 0,

            /// <summary>
            /// ��� ����̽� ���� ����
            /// </summary>
            update_all_device,

            /// <summary>
            /// ��꿡 ����̽� �߰�
            /// </summary>
            add_device,

            /// <summary>
            /// ��꿡 ����̽� ����
            /// </summary>
            delete_device
        }
    }
}