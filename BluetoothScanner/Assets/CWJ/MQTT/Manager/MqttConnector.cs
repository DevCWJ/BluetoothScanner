using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;

using MQTTnet.Client;
using UnityEngine;
using System;
using MQTTnet.Server;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

namespace CWJ.IoT
{
    public interface IConnectable : MqttConnector._IConnectable { }
    public interface IDebugConnectable : MqttConnector._IConnectable { }

    public class MqttConnector : CWJ.Singleton.SingletonBehaviour<MqttConnector>
    {
        public interface _IConnectable
        {
            int connectOrderIndex { get; }
            string clientID { get; }
            IMqttClient GetNewClient(MqttConfigurationObj config);
            IMqttClient client { get; set; }
            bool isAutoReconnect { get; }
            void OnBeforeConnect();
            void OnAfterConnect(bool isConnected);
            MonoBehaviour GetTargetObj();
        }

        [SerializeField] MqttConfigurationObj config;
        public static MqttConfigurationObj Config => Instance.config;

        List<X509Certificate2> publicCertChain = null;
        X509Certificate2 serverCertificate;
        X509Certificate2 clientCertificate;
        //X509Certificate2Collection certificates = userStore.Certificates.Find(X509FindType.FindByThumbprint, m_configuration.CertificateThumbprint, m_configuration.ValidateCertificate);
        public MqttClientOptionsBuilder GetClientOption(string clientID)
        {
            var clientOption = new MqttClientOptionsBuilder()
                            .WithClientId(clientID)
                            .WithTcpServer(config.host, config.port)
                            //.WithCleanSession()
                            .WithTimeout(TimeSpan.FromSeconds(10));

            if (config.useValidateCredentials)
            {
                clientOption.WithCredentials(config.userName, config.password);
            }

            if (config.useSsl)
            {
                clientOption.WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    Certificates = new X509Certificate2[] { clientCertificate },
                    UseTls = true,
                    SslProtocol = SslProtocols.Tls12,
                    IgnoreCertificateChainErrors = true,
                    IgnoreCertificateRevocationErrors = true,
                    AllowUntrustedCertificates = true,
                    CertificateValidationHandler = context =>
                    {
                        context.Chain.ChainPolicy.VerificationFlags = config.allowUnknownCA
                            ? X509VerificationFlags.AllowUnknownCertificateAuthority
                            : X509VerificationFlags.NoFlag;

                        return true;
                    }
                });
            }

            return clientOption;
        }

        public MqttServerOptionsBuilder GetServerOption()
        {
            var serverOption = new MqttServerOptionsBuilder().WithConnectionBacklog(100);

            //if (Application.isMobilePlatform)
            //{
                    //serverOption.WithDefaultEndpointBoundIPAddress(Dns.GetHostEntry(Dns.GetHostName()).AddressList[0])
            //    .WithDefaultEndpointBoundIPV6Address(IPAddress.None);
            //}

            IPAddress iPAddress = null;
            if (!string.IsNullOrEmpty(config.host))
            {
                try
                {
                    iPAddress = IPAddress.Parse(config.host);
                }
                catch (Exception exception)
                {
                    iPAddress = null;
                    Debug.LogError($"Could not use configured broker address {config.host}. Will fall back to default endpoint.{exception.ToString()}");
                }
            }

            bool isUseSsl = config.useSsl;
            if (isUseSsl)
            {
                if (config.brokerCertAsset?.GetRelativePath() != null)
                {
                    serverOption.WithoutDefaultEndpoint();
                    try
                    {
                        var certPath = Application.streamingAssetsPath;
                        var serverCert = new X509Certificate2(
                            Path.Combine(certPath, config.brokerCertAsset.GetRelativePath()),
                            config.brokerCertAsset.password,
                            X509KeyStorageFlags.Exportable);

                        if (serverCert.HasPrivateKey)
                        {
                            serverOption = serverOption.WithEncryptionCertificate(
                                    serverCert.Export(X509ContentType.Pkcs12, config.brokerCertAsset.password),
                                    new MqttServerCertificateCredentials()
                                    { Password = config.brokerCertAsset.password })
                                .WithEncryptedEndpoint()
                                .WithEncryptedEndpointPort(config.port)
                                .WithEncryptionSslProtocol(SslProtocols.Tls12)
                                .WithEncryptedEndpointBoundIPAddress(iPAddress);

                            serverCertificate = serverCert;
                        }

                        serverOption = serverOption.WithClientCertificate(
                        (sender, certificate, chain, sslPolicyErrors) =>
                        {
                            // TODO: Add client certificate validation
                            return true;
                        });
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) =>
                        {
                            //TODO: Certificate Validation as soon as Mono supconfig.ports it.
                            return true;
                        };

                    }
                    catch(Exception e)
                    {
                        Debug.LogError($"Could not config.useSsl + config.brokerCertAsset. Will fall back to default endpoint.{e.ToString()}");

                        isUseSsl = false;
                    }

                }
                else
                {
                    Debug.LogError("Improperly configured path for broker/server cert.");
                    isUseSsl = false;
                }
            }

            if (!isUseSsl)
            {
                serverOption.WithoutEncryptedEndpoint().WithDefaultEndpoint().WithDefaultEndpointPort(config.port);
                if (iPAddress != null)
                {
                    try
                    {
                        serverOption.WithDefaultEndpointBoundIPAddress(iPAddress);
                    }
                    catch (Exception exception)
                    {
                        serverOption.WithDefaultEndpointBoundIPAddress(Dns.GetHostEntry(Dns.GetHostName()).AddressList[0]);
                            //.WithDefaultEndpointBoundIPV6Address(IPAddress.None);
                        Debug.LogError($"Could not use configured broker address {config.host}. Will fall back to default endpoint.{exception.ToString()}");
                    }
                }
            }


            return serverOption;

        }

        [SerializeField] bool isConnectOnStart = true;

        public Action afterAllConnectedEvent;

        int connectableLength;
        [NonSerialized] _IConnectable[] connectablesInScene = null;

        bool isConnectLoading;
        [InvokeButton]
        public void ConnectAllClient()
        {
            if (isConnectLoading)
            {
                Debug.LogError("실행중");
                return;
            }
            isConnectLoading = true;

            if (config.useSsl)
            {
                if (publicCertChain == null)
                    publicCertChain = new List<X509Certificate2>();
                //var certPath = Application.streamingAssetsPath;
                //if (config.caCertAsset?.GetRelativePath() != null)
                //{
                //    publicCertChain.Add(serverCertificate);
                //    var caCert = new X509Certificate2(Path.Combine(certPath, config.caCertAsset.GetRelativePath()), config.caCertAsset.credentials.config.password);
                //    publicCertChain.Add(caCert);
                //}
                //else
                //    Debug.LogError($"Improperly configured path for CA cert.");
                clientCertificate = config.GetClientCertificate();
                if (clientCertificate == null)
                {
                    Debug.LogError("certificatePfxBuffer is null");
                }
            }

            if (config.isDebug)
                connectablesInScene = FindUtil.FindInterfaces<_IConnectable>(false, false).OrderBy(x => x.connectOrderIndex).ToArray();
            else
                connectablesInScene = FindUtil.FindInterfaces<IConnectable>(false, false).OrderBy(x => x.connectOrderIndex).ToArray();

            for (int i = 0; i < connectablesInScene.Length; i++)
            {
                int index = i;
                UniTask.RunOnThreadPool(() => ConnectClient(index)).Forget();
            }
        }

        protected override async void _Start()
        {
            if (isConnectOnStart)
            {
                await UniTask.Yield();
                ConnectAllClient();
            }
        }

        private async UniTaskVoid ConnectClient(int index, bool isAutoReconnect = true)
        {
            await UniTask.Yield();

            var connectable = connectablesInScene[index];
            if (connectable == null)
            {
                return;
            }
            connectable.OnBeforeConnect();

            var target = connectable.GetTargetObj();
            string clientId = $"[{index}]{target.GetType().Name}({connectable.clientID})";

            if (connectable.client == null)
                connectable.client = connectable.GetNewClient(config);

            bool isError = false;

            try
            {
                await connectable.client.ConnectAsync(GetClientOption(clientId).Build());
            }
            catch (Exception e)
            {
                isError = true;
                Debug.LogError($"{clientId}: Error while ConnectAsync.\n" + e.ToString(), target);
            }
            bool isConnected = connectable.client != null && connectable.client.IsConnected;
            if (!isConnected)
            {
                if (!isError)
                {
                    Debug.LogError($"{clientId} cannot connected.", target);
                }
                if (connectable.client != null)
                    connectable.client.Dispose();
                connectable.client = null;
            }
            else
            {
                Debug.Log($"{clientId} Client Connected", target);
                if (connectable.isAutoReconnect)
                {
                    connectable.client.DisconnectedAsync += async (e) =>
                    {
                        if (!CWJ.MonoBehaviourEventHelper.IS_QUIT)
                        {
                            connectable.client.Dispose(); connectable.client = null;
                            await UniTask.RunOnThreadPool(() => (ConnectClient(index, false)));
                        }
                    };
                }

                connectable.client.DisconnectedAsync += (e) =>
                {
                    if (!CWJ.MonoBehaviourEventHelper.IS_QUIT)
                        connectable.client.Dispose(); connectable.client = null;
                    return Task.CompletedTask;
                };
            }

            connectable.OnAfterConnect(isConnected);

            if (connectableLength - 1 == index)
            {
                afterAllConnectedEvent?.Invoke();
                isConnectLoading = false;
            }
        }

        public void DisconnectAllClient()
        {
            if (connectablesInScene.LengthSafe() == 0)
            {
                return;
            }

            foreach (var item in connectablesInScene)
            {
                if (item.client != null && item.client.IsConnected)
                    item.client.DisconnectAsync();
            }
        }

        protected override void _OnApplicationQuit()
        {
            DisconnectAllClient();
        }

        //[InvokeButton]
        //public void ConnectAll()
        //{
        //    if (allConnectionCor != null)
        //    {
        //        return;
        //    }
        //    allConnectionCor = StartCoroutine(DO_ConnectAll());
        //}

        //[InvokeButton]
        //public IEnumerator DO_ConnectAll()
        //{
        //    if (config.useSsl)
        //    {
        //        if (publicCertChain == null)
        //            publicCertChain = new List<X509Certificate2>();
        //        //var certPath = Application.streamingAssetsPath;
        //        //if (config.caCertAsset?.GetRelativePath() != null)
        //        //{
        //        //    publicCertChain.Add(serverCertificate);
        //        //    var caCert = new X509Certificate2(Path.Combine(certPath, config.caCertAsset.GetRelativePath()), config.caCertAsset.credentials.config.password);
        //        //    publicCertChain.Add(caCert);
        //        //}
        //        //else
        //        //    Debug.LogError($"Improperly configured path for CA cert.");
        //        clientCertificate = mqttSetting.GetNewClientCertificate();
        //        if(clientCertificate == null)
        //        {
        //            Debug.LogError("certificatePfxBuffer is null");
        //        }
        //    }
        //    var connectablList = new List<_IConnectable>();
        //    connectablList.AddRange(FindUtil.FindInterfaces<IConnectable>(false, false).OrderBy(x => x.connectOrderIndex));

        //    if (config.isDebug)
        //        connectablList.AddRange(FindUtil.FindInterfaces<IDebugConnectable>(false, false).OrderBy(x => x.connectOrderIndex));
        //    connectables = connectablList.ToArray();

        //    connectableLength = connectables.Length;
        //    //var appCommonGuid = Guid.NewGuid().ToString();
        //    for (int i = 0; i < connectableLength; i++)
        //    {
        //        yield return StartCoroutine(DoConnect(i, connectables[i]));
        //    }
        //    allConnectionCor = null;
        //}



        //protected override void _Start()
        //{
        //    if (isConnectOnStart)
        //        ConnectAll();
        //}


        //private IEnumerator DoConnect(int i, _IConnectable connectable, bool isAutoReconnect = true)
        //{
        //    if (connectable == null)
        //    {
        //        yield break;
        //    }
        //    connectable.OnBeforeConnect();
        //    string clientId = $"[{i}]{connectable.GetTargetObj().GetType().Name}({connectable.clientID})";

        //    if (connectable.client == null)
        //        connectable.client = connectable.GetNewClient(configureSettingObj);

        //    var clientOptions = GetClientOption(clientId).Build();
        //    var target = connectable.GetTargetObj();

        //    if (connectable.isAutoReconnect)
        //        connectable.client.DisconnectedAsync += async (e) =>
        //        {
        //            if (isAutoReconnect && !CWJ.SingletonHelper.IS_QUIT)
        //            {
        //                await connectable.client.DisconnectAsync();
        //                connectable.client.Dispose(); connectable.client = null;
        //                StartCoroutine(DoConnect(i, connectable, isAutoReconnect));
        //            }
        //        };

        //    bool isError = false;
        //    Task task = null;
        //    try
        //    {
        //        task = connectable.client.ConnectAsync(clientOptions);
        //    }
        //    catch (Exception e)
        //    {
        //        isError = true;
        //        Debug.LogError($"{clientId}: Error while ConnectAsync.\n" + e.ToString(), connectable.GetTargetObj());
        //    }

        //    if (task != null && !task.IsCompleted)
        //        yield return new WaitUntil(() => task.IsCompleted);

        //    if (!connectable.client.IsConnected)
        //    {
        //        if (!isError)
        //        {
        //            Debug.LogError($"{connectable.GetTargetObj().name}{clientId} cannot connected.", target);
        //        }

        //        connectable.client.Dispose();
        //        connectable.client = null;
        //    }


        //    bool isConnected = connectable.client != null && connectable.client.IsConnected;
        //    if (isConnected)
        //        Debug.Log($"{clientId} Client Connected", target);
        //    else
        //        Debug.LogError($"{clientId} Client Connect Error", target);

        //    connectable.OnAfterConnect(isConnected);

        //    Debug.LogError(connectableLength + " " + i);
        //    if (connectableLength - 1 == i)
        //    {
        //        afterAllConnectedEvent?.Invoke();
        //    }
        //}




        //private IPAddress MyIP()
        //{
        //    try
        //    {
        //        IPHostEntry config.host = Dns.GetHostEntry(Dns.GetHostName());

        //        return config.host.AddressList.FirstOrDefault(o => o.AddressFamily == AddressFamily.InterNetwork);
        //    }
        //    catch
        //    {
        //        return null;
        //    }

        //}


        //class SimpleLogger : IMqttNetLogger
        //{
        //    readonly object _consoleSyncRoot = new object();

        //    public bool IsEnabled => true;

        //    Action<object>[] printLogArr = new Action<object>[4] { Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError };


        //    public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
        //    {
        //        if (parameters?.Length > 0)
        //        {
        //            message = string.Format(message, parameters);
        //        }

        //        lock (_consoleSyncRoot)
        //        {
        //            //Console.ForegroundColor = foregroundColor;
        //            printLogArr[logLevel.ToInt()](message);

        //            if (exception != null)
        //            {
        //                Debug.LogError(exception.ToString());
        //            }
        //        }
        //    }
        //}
    }
}