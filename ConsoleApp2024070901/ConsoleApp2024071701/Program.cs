using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Opc.Ua;
using OPCUaClient;
using OPCUaClient.Objects;

namespace ConsoleApp2024071701
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            // 创建一个应用配置对象，用于设置应用名称、唯一标识、类型、证书和安全策略
            var config = new ApplicationConfiguration()
            {
                ApplicationName = "appName",
                ApplicationUri = Utils.Format(@"urn:WIN-5K8TA8FJ7FB:Kepware.KEPServerEX.V6:UA%20Server"),
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault", SubjectName = "MyClientSubjectName" },
                    TrustedIssuerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities" },
                    TrustedPeerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications" },
                    RejectedCertificateStore = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\RejectedCertificates" },
                    AutoAcceptUntrustedCertificates = true,
                    RejectSHA1SignedCertificates = false,
                    MinimumCertificateKeySize = 1024,
                    NonceLength = 32,
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                TraceConfiguration = new TraceConfiguration()
            };

            // 验证应用配置对象
            config.Validate(ApplicationType.Client).Wait();

            // 设置证书验证事件，用于自动接受不受信任的证书
            if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += (s, e) => { e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
            }

            // 创建一个应用实例对象，用于检查证书
            var application = new ApplicationInstance(config);

            // 检查应用实例对象的证书
            bool check = application.CheckApplicationInstanceCertificate(false, 2048).Result;

            // 创建一个会话对象，用于连接到 OPC UA 服务器
            EndpointDescription endpointDescription = CoreClientUtils.SelectEndpoint("opc.tcp://192.168.2.91:49320", true);
            EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(config);
            ConfiguredEndpoint endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);
            Session session = Session.Create(config, endpoint, false, false, "DataCollector", 60000, new UserIdentity(), null).Result;

            // 
            DataValue value = session.ReadValue(nodeId: "ns=2;s=Devices.AA.Ramp1");
            Console.WriteLine("{0}, {1}, {2}", value.Value, value.SourceTimestamp, value.StatusCode);

            value = session.ReadValue(nodeId: "ns=2;s=DEVICESAA.AAAA.K.Boolean1");
            Console.WriteLine("{0}, {1}, {2}", value.Value, value.SourceTimestamp, value.StatusCode);


            // 创建一个订阅对象，用于订阅节点的值
            var subscription = new Subscription(session.DefaultSubscription) { PublishingInterval = 1000 };
            session.AddSubscription(subscription);
            subscription.Create();

            // 创建一个监视项对象，用于指定要订阅的节点
            MonitoredItem monitoredItem = new MonitoredItem()
            {
                DisplayName = "Ramp1",
                StartNodeId = "ns=2;s=Devices.AA.Ramp1"
            };

            // 添加一个通知事件，用于处理节点值的变化
            monitoredItem.Notification += (item, e) =>
            {
                foreach (var valueItem in item.DequeueValues())
                {
                    Console.WriteLine("{0}: {1}, {2}, {3}", item.DisplayName, valueItem.Value, valueItem.SourceTimestamp, valueItem.StatusCode);
                }
            };


            // 创建一个监视项对象，用于指定要订阅的节点
            MonitoredItem monitoredItem1 = new MonitoredItem()
            {
                DisplayName = "Sine1",
                StartNodeId = "ns=2;s=Devices.AA.Sine1"
            };

            // 添加一个通知事件，用于处理节点值的变化
            monitoredItem1.Notification += (item, e) =>
            {
                foreach (var valueItem in item.DequeueValues())
                {
                    Console.WriteLine("{0}: {1}, {2}, {3}", item.DisplayName, valueItem.Value, valueItem.SourceTimestamp, valueItem.StatusCode);
                }
            };

            // 将监视项对象添加到订阅对象中
            subscription.AddItem(monitoredItem);
            subscription.AddItem(monitoredItem1);

            // 应用订阅的变化
            subscription.ApplyChangesAsync().Wait();

            subscription.RemoveItem(monitoredItem);
            subscription.RemoveItem(monitoredItem1);

            value = session.ReadValue(nodeId: "ns=2;s=DEVICESAA.设备1.组1.testbool");
            Console.WriteLine("{0}, {1}, {2}, {3}", "testbool", value.Value, value.SourceTimestamp, value.StatusCode);
            // 写入数据到节点
            WriteValue value1 = new WriteValue()
            {
                NodeId = "ns=2;s=DEVICESAA.设备1.组1.testbool",
                AttributeId = 13u,
                Value = new DataValue(new Variant(true))
            };

            ResponseHeader response = session.Write(null, new WriteValueCollection { value1 }, out StatusCodeCollection statuses, out DiagnosticInfoCollection diagnostics);
            Console.WriteLine("Write result: {0} {1}", response.ServiceResult, response.Timestamp);

            //
            value = session.ReadValue(nodeId: "ns=2;s=DEVICESAA.设备1.组1.testbool");
            Console.WriteLine("{0}, {1}, {2}", value.Value, value.SourceTimestamp, value.StatusCode);




            value = session.ReadValue(nodeId: "ns=2;s=DEVICESAA.设备1.组1.testint");
            Console.WriteLine("{0}, {1}, {2}, {3}", "testint", value.Value, value.SourceTimestamp, value.StatusCode);
            // 写入数据到节点
            WriteValue value2 = new WriteValue()
            {
                NodeId = "ns=2;s=DEVICESAA.设备1.组1.testint",
                AttributeId = 13u,
                Value = new DataValue(new Variant((short)3))
            };

            response = session.Write(null, new WriteValueCollection { value2 }, out StatusCodeCollection statuses1, out DiagnosticInfoCollection diagnostics1);
            Console.WriteLine("Write result: {0} {1} ", response.ServiceResult, response.Timestamp);

            //
            value = session.ReadValue(nodeId: "ns=2;s=DEVICESAA.设备1.组1.testint");
            Console.WriteLine("{0}, {1}, {2}", value.Value, value.SourceTimestamp, value.StatusCode);


            // 读取数据

            ReadValueId readValue = new ReadValueId()
            {
                NodeId = "ns=2;s=DEVICESAA.设备1.组1.testbool",
                AttributeId = Attributes.Value
            };

            DataValueCollection dataValues;
            DiagnosticInfoCollection diagnosticInfos;
            session.Read(null, 11111, TimestampsToReturn.Neither, new[] { readValue }, out dataValues, out diagnosticInfos);

            // 检查读取的值
            if (dataValues.Count > 0 && StatusCode.IsGood(dataValues[0].StatusCode))
            {
                // 获取读取的值
                object readData = dataValues[0].Value;
                Console.WriteLine($"Node value after write: {readData}");
            }

            //
            value = session.ReadValue(nodeId: "ns=2;s=DEVICESAA.设备1.组1.teststring");
            Console.WriteLine("{0}, {1}, {2}, {3}", "teststring", value.Value, value.SourceTimestamp, value.StatusCode);
            // 写入数据到节点
             value2 = new WriteValue()
            {
                NodeId = "ns=2;s=DEVICESAA.设备1.组1.teststring",
                AttributeId = 13u,
                Value = new DataValue(new Variant("test string"))
            };

            response = session.Write(null, new WriteValueCollection { value2 }, out StatusCodeCollection statuses5, out DiagnosticInfoCollection diagnostics5);
            Console.WriteLine("Write result: {0} {1} ", response.ServiceResult, response.Timestamp);

            //
            value = session.ReadValue(nodeId: "ns=2;s=DEVICESAA.设备1.组1.teststring");
            Console.WriteLine("{0}, {1}, {2}", value.Value, value.SourceTimestamp, value.StatusCode);


            //*************************************************************************************

            // Create a instance
            UaClient client = new UaClient("test", "opc.tcp://192.168.2.91:49320", Utils.Format(@"urn:WIN-5K8TA8FJ7FB:Kepware.KEPServerEX.V6:UA%20Server"), true, true);
            //UaClient auth = new UaClient("test", "opc.tcp://192.168.2.91:49320", Utils.Format(@"urn:WIN-5K8TA8FJ7FB:Kepware.KEPServerEX.V6:UA%20Server"), true, true, "admin", "password");


            // Create a session on the server
            int timeOut = 30;
            client.Connect((uint)timeOut, true);

            // Read a tag

            Tag tag = client.Read("DEVICESAA.设备1.组1.testbool");
            //Or
            //tag = await client.Read("DEVICESAA.设备1.组1.testbool");


            Console.WriteLine($"Name: {tag.Name}");
            Console.WriteLine($"Address: {tag.Address}");
            Console.WriteLine($"Value: {tag.Value}");
            Console.WriteLine($"Quality: {tag.Quality}");
            Console.WriteLine($"Code: {tag.Code}");

            short valueInt = client.Read<short>("DEVICESAA.设备1.组1.testint");
            //value = await client.ReadAsync<int>("DEVICESAA.设备1.组1.testint");


            // Read multiple tags
            var address = new List<String>
            {
             "DEVICESAA.设备1.组1.testbool",
             "DEVICESAA.设备1.组1.testint",
              "DEVICESAA.设备1.组1.teststring"
            };

            var tags = client.Read(address);
            //Or
            //tags await = client.ReadAsync(address);

            foreach (var tagItem in tags)
            {
                Console.WriteLine($"Name: {tagItem.Name}");
                Console.WriteLine($"Address: {tagItem.Address}");
                Console.WriteLine($"Value: {tagItem.Value}");
                Console.WriteLine($"Quality: {tagItem.Quality}");
                Console.WriteLine($"Code: {tagItem.Code}");
            }

            // 
            // Write a tag
            client.Write("DEVICESAA.设备1.组1.testbool", true);
            //Or
            //await client.WriteAsync("Device.Counter.Model", "NewModel");


            // Write multiple tags
            tags = new List<Tag>
            {
              new Tag {
                Address = "DEVICESAA.设备1.组1.testbool",
                Value = false,
              },
              new Tag {
                Address = "DEVICESAA.设备1.组1.testint",
                Value = (short)5,
              },
              new Tag {
                Address = "DEVICESAA.设备1.组1.teststring",
                Value = "OtherModel",
              }
            };
            client.Write(tags);
            //Or
            //await client.WriteAsync(tags);

            // Monitoring a tag
            client.Monitoring("DEVICESAA.设备1.组1.testbool", 500, (_, e) => {
                // Anything you need to be executed when the value changes

                // Get the value of the tag being monitored
                var monitored = (MonitoredItemNotification)e.NotificationValue;
                Console.WriteLine(monitored.Value);
            });


            // Scan OPC UA Server
            var devices = client.Devices(true);
            //Or
            //devices = await client.DevicesAsync(true);

            foreach (var device in devices)
            {
                Console.WriteLine($"Name: {device.Name}");
                Console.WriteLine($"Address: {device.Address}");
                Console.WriteLine($"Groups: {device.Groups.Count()}");
                Console.WriteLine($"Tags: {device.Tags.Count()}");
            }

            // Scan group
            var groups = client.Groups("DEVICESAA", true);
            //Or
            //groups = await client.GroupAsync("Device", true);

            foreach (var group in groups)
            {
                Console.WriteLine($"Name: {group.Name}");
                Console.WriteLine($"Address: {group.Address}");
                Console.WriteLine($"Groups: {group.Groups.Count()}");
                Console.WriteLine($"Tags: {group.Tags.Count()}");
            }


            // Scan an address and recovery the tags
            tags = client.Tags("DEVICESAA.设备1.组1");
            //Or
            //tags = await client.TagsAsync("Device.Counter");

            foreach (var tagItem in tags)
            {
                Console.WriteLine($"Name: {tagItem.Name}");
                Console.WriteLine($"Address: {tagItem.Address}");
            }

            // Close session
            client.Disconnect();
            Console.ReadKey();
        }
    }
}
