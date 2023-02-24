using MQTTnet.Protocol;

using System;
using System.Security.Cryptography.X509Certificates;

using UnityEngine;

namespace CWJ.IoT
{
    [CreateAssetMenu(fileName = "New Mqtt Config", menuName = "CWJ.IoT/Mqtt Config")]
    public class MqttConfigurationObj : ScriptableObject
    {
        public MqttConfigurationObj()
        {
            //Default Setting
        }

        public bool isDebug;
        public bool isLocalHost;
        public string host; //"a1ckgl5crw8i5l-ats.iot.ap-northeast-2.amazonaws.com"
        public int port;
        public MqttQualityOfServiceLevel qosLevel;
        [Header("ID/PW Credentials")]
        public bool useValidateCredentials;
        public string userName;
        public string password;
        [Header("SSL Certificate")]
        public bool useSsl;
        public ScriptableCertificate caCertAsset;
        public ScriptableCertificate brokerCertAsset;
        public ScriptableCertificate clientCertAsset;
        public bool allowUnknownCA;

        [Header("Certificate2 String Data")]
        [SerializeField] string certificatePfxBuffer; //@"MIIKXgIBAzCCChoGCSqGSIb3DQEHAaCCCgsEggoHMIIKAzCCBgwGCSqGSIb3DQEHAaCCBf0EggX5MIIF9TCCBfEGCyqGSIb3DQEMCgECoIIE/jCCBPowHAYKKoZIhvcNAQwBAzAOBAhlNOszsMGGRwICB9AEggTYTg5GgAC/uATSU1xCfcVWh7f+dicIUZ7v7uGSA1xlMzzu8ZCvE5fnK5kH7nXyKs84z8gyCHiNHHK3McxMOED3jjd15hNR6LfR72kBr/BpjfQ0myA8XEpTy9sGYvOBTcZclQt3+ttdAJmCHDPwm7lFx0NyKDjgXUAJurQ4KX04zyAAi9GcctfEpZgl9RkH5b12QnYJOurP4zz94uvM1Nk5vj3bUD1q+XMZ2dSwk/iRIi3fLrmnAP5abiG/ym4rluEC2rTkmfXbrG4NkU9s3cpZXhmWyqCDMeQgItR36P4a+b/jUOtY/JWOLoR/vQNNl74Cyl22mKB2FcSJbPoh0ca5XrSw80iISH09RVcq6bWcu6FU5anmnK+7K41HC0bvdsrBxIPPWtkVAqxz9tCksovG+5mM8y2c2/SwWEqL1PUkM0R8Tn/HqucJuZVL6Yle7UdYU4U6hvJRsndTd08lRRwrJn6vCeR7A25EktyBy5p2/sQJCq0Kds0SBvfJeHgl4HVLTtwf0JpeSPY7hT4jYPTHM+D3T7jajw9J8fmYTK4isYV/bmHpDnzVTXA09y5+SeHKVF66iZ2YHs8WGOHFKhtC/RBIUQdqQfB8AccFZtxk/0hDPC3hkBHAdxZkO+EyoNtQqdJePV5tYY7t4qac8zgtoMwOTA05OUwD4/0bKJVBLI1EuxUdNsraZZh1HWx6XWyYGLegvw0WtblSoubRtZWHw4sQL70YyBwxNjfJOZyLmHwKsvpvQF82owbwCUikoAv/Q/XSOZkQFGhh3l8B+prA7qFBnTZIq0J/7X3UL2S4Tu2oq32dErHfhx3BTpCloBsgiUIL8Ms07T1B62fnkWpdeAeb+GARxrw/10RcWMWehqSl+4zEzWhRRgrt4LmNtpIYI1rV+4fx/LQRzcmWFI7pVxgiiV9Oi3vHlQ2n1V/840rXD5ggxoGH2FzVYxoIzwlFO8WwF8TOtv3lhFafvrlcwagNFVxlXU+0JajxGoAUusMkPolJnFrcGNp7eSwkQLjdSsstd13gly2pZS/uxuXGJC51WAFJMLMhXPr2p9V4o3G+BEM3gbBGsL+SHRwIcI1kF7tGZ+PEwSV+mFcu/uh3I7Nep+sDVkcgTZCl8xWUvseAtHyQs3X4REqPO0fVzUWnkKW7O59JXbSxHLl6s2AwgsbaKpeKvNoMyV2tE7ZQIbrQbT801v90joRXlveqEQTuS8Z9hVV9inFdex5L+R0HMIPYCe+QekUfv+3y8Pqrgg9Omeyc4MTmP3UASYeMGC8oAYDlf3hC08n0ZVPqtreyoVqVFPwA+Cz24lfjj2543AM+THTB1g5gqTZX0+b44GghsPtXMCYDwK6AhQg13AgetwWsG8qc8julVBsgVoUCaCrE6vywGZ2do27nYWHMCaMxfGZJf00vl7Yc7Sq2Qv5YLEu1VxEiPjZ+fO24UsKYJkSVTsKX93wGNVNpUSgqjXkB/YTmccWyUkTA07uJz9xqMWUAg/aR7J/xW1F2ggrqWadUrBsUQyIoEJPOOIpgu7AUu6S9RmyCDFzrqZu8s/rHGMIRHpvVp1mBJi256bq9xAWIfdFSJSuDrlF4qgnlTOQ/N33Hnpj9sVWQq5X6eGthrefJu38LqfuflwEQudic/itWjgtFuJK28DGB3zATBgkqhkiG9w0BCRUxBgQEAQAAADBbBgkqhkiG9w0BCRQxTh5MAHsANgA0ADYAMQAyAEUANAA5AC0ANgA3AEIARAAtADQAQgBCADAALQBBADQARAA4AC0AMQA0ADEAMgBFADAANQA3ADYAMgBCAEEAfTBrBgkrBgEEAYI3EQExXh5cAE0AaQBjAHIAbwBzAG8AZgB0ACAARQBuAGgAYQBuAGMAZQBkACAAQwByAHkAcAB0AG8AZwByAGEAcABoAGkAYwAgAFAAcgBvAHYAaQBkAGUAcgAgAHYAMQAuADAwggPvBgkqhkiG9w0BBwagggPgMIID3AIBADCCA9UGCSqGSIb3DQEHATAcBgoqhkiG9w0BDAEDMA4ECANfFoESbosnAgIH0ICCA6iVlTbjl6dkqFDbCImxpJgBQg8sHkOFb5L1MFfaKgetJZl7xpSm1AolOD/ziUjIzEQ6Fx3prMFYBS39JqddFpiz2s0aNmb+UV2KQvdxQNSqdRlloSAQHlT9Li0t/Vbw17S1vXbT9Hr+tmhIA54nEBYMC157B3DilA6bJ6SxYFAT0hpc1//qEXbA9EbQFWhPEBxW1UfQfKhtH2FUi0punvKEaahMRDcLh72yRV64B2NkcmPcptTUxkUMusgbIrOFl2dkiO7ZZfcymLt6UF+Zo1gNg/gHXQL/rsbREi2i8WuQQddQzkx+MxtzC2domAN1w1KPSbVH88NwuzHBX6v3J4GEVwZJaAbbYSKTTpycfU3grp3c8+M1a3LmIqXrhlUcdcMOa2K33OxQzgaPHU3Aa3SWjf08fcB7p7HBSlW5emCm0U93raa0kmr/9JXmw7riJZcusaMJxxJDXHlh7qN9CTk+GP0Azn0InYReEVXPiLlOE/Bmv3htmbwMVkk7mTRMwGPtRnLRjGYvgfeWL4ZVhckNYUJkNZ4YIGOR1TQUppM1wkRQ7ob8rzdgGT+LfXmjoGiynbQbyMVycO4wIdqPwQU+tBEhpQmxMqat+p+SVQBvlV70V+es0ZDmVlxMuXS0Zh803dFUkP/gQMlSMxL4KjUtdrrA9yzxkwWBP8FDXOJNEEEtLBYqPzNZUjQojxCbQ1etm4HjQ4uLoLny8QlVIgB5IR1ysNeQQ8W1L+/Pd61hdBoGK1dkvX3q4dE1hA20dfgzDaSwKuj1zPI902N6iPWJs1PmzkLBA93mVz4MpV2ZMz4NYzDbABvQbDcS6ZQOU7NC05/fWsfglhETT1qZflnWAq6yqptlzu7udGdxi+YMDDRR1k6JqQtlukrS+XgGLm2nXgUpTsbHkFGjZxmT0+YTDrTBgdpP+bRO/hZYTzCmHFhruVftThl9x9mgZKSaPf3fbM60bVjAdbh0PsoUdgHVGIthz5vRJTDvai3EJARkDrifkwDyo0khrYhtAKBRC66k+1UCTeBqAE1ZxwziMigo2qR7Hqnv0gV6L450p4nIxtI0wE8ckZFrwX5zARd14SjJKji/0bEJUYm1N91zH/4hghThVNgFkyjujobTG6B9A0wPq29eoUsTBCd4E18szWFkgJaRyhUvr8madXhjVFwnAs9vu0/9oQFTNYBZJLnQJo+dX9trjnTB/eRMPczXVbgRFlvgNDLav5ez4YJO0zYT3uOLcwUlmgEwOzAfMAcGBSsOAwIaBBQPeTqMx2ZqYxUXZnzJA7ZzGgVrIAQU7UCSWkg/gKcDFyVItMHiUsNtJAYCAgfQ";
        [SerializeField] string pfxPassword;

        X509Certificate2 ConvertBufferToCertificate(string buffer, string password)
        {
            if (string.IsNullOrEmpty(buffer))
            {
                return null;
            }

            int startIndex = buffer.IndexOf('"');
            if (startIndex >= 0)
            {
                var splits = buffer.Split('"');
                if (splits.Length > 1)
                    buffer = splits[1];
            }

            return new X509Certificate2(Convert.FromBase64String(buffer), password, X509KeyStorageFlags.Exportable);
        }
        public X509Certificate2 GetClientCertificate()
        {
            return ConvertBufferToCertificate(certificatePfxBuffer, pfxPassword);
        }

#if UNITY_EDITOR
        const string localHost = "127.0.0.1";

        private void OnValidate()
        {
            if (!isLocalHost)
            {
                if (host == null)
                    host = string.Empty;
                else if (host.Length > 0)
                    host = host.Trim();
            }
            else
            {
                if (host != localHost)
                    host = localHost;
            }

            if (useValidateCredentials)
            {
                if (userName == null) userName = string.Empty;
                else if (userName.Length > 0) userName = userName.Trim();
                if (password == null) password = string.Empty;
                else if (password.Length > 0) password = password.Trim();
            }

            if (!isDebug)
            {
                if (isDebug = FindUtil.FindInterfaces<IDebugConnectable>(includeInactive: false, includeDontDestroyOnLoadObjs: true).Length > 0)
                {
                    Debug.LogError("IDebugConnectable 가 포함된 Component가 Scene에 존재하여 isDebug가 활성화됐습니다");
                }
            }
        }

        [ContextMenu("Test - ConvertBufferToCertificate")]
        void Test_ConvertBufferToCertificate()
        {
            try
            {
                var c = GetClientCertificate();
                Debug.Log("Certificate 변환 잘됨\n" + c.FriendlyName);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }
#endif

    }
}