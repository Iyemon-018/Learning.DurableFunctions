namespace Learning.DurableFunctions.Tests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using ChainingAssertion;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public class HelloSequenceTest
    {
        // cf. https://docs.microsoft.com/ja-jp/azure/azure-functions/durable/durable-functions-unit-testing

        private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();

        [Fact]
        public async Task Test_�V�i���I_HttpStart()
        {
            var durableClientMock = new Mock<IDurableClient>();
            var functionName      = "Function1";
            var instanceId        = "123";

            // IDurableClient.StartNewAsync
            // IDurableClient.CreateCheckStatusResponse
            // ���̂Q�́AHttpStart ���\�b�h�����ŌĂяo�����̂Ń��b�N���K�v�ɂȂ�B
            // �ȉ��� .Returns �̒l�̓T���v���Ȃ̂ŁA���ۂ̃R�[�h���ƓK�X�C������K�v������B
            durableClientMock.Setup(x => x.StartNewAsync(functionName, It.IsAny<object>()))
                             .ReturnsAsync(instanceId);

            durableClientMock.Setup(x => x.CreateCheckStatusResponse(It.IsAny<HttpRequestMessage>(), instanceId, It.IsAny<bool>()))
                             .Returns(new HttpResponseMessage
                                      {
                                          StatusCode = HttpStatusCode.OK
                                        , Content    = new StringContent(string.Empty)
                                        , Headers =
                                          {
                                              RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(10))
                                          }
                                      });

            var result = await HelloSequence.HttpStart(new HttpRequestMessage
                                                       {
                                                           Content    = new StringContent("{}", Encoding.UTF8, "application/json")
                                                         , RequestUri = new Uri("http://localhost:7071/orchestrators/E1_HelloSequence")
                                                       }
                  , durableClientMock.Object
                  , functionName
                  , _loggerMock.Object);

            result.IsNotNull();

            // Durable Functions �ł͉������ʂɁAIDurableClient.CreateCheckStatusResponse �̖߂�l���Ԃ�悤�Ȏd�g�݂ɂȂ��Ă���B
            // ���̒l������ɕԂ��Ă��Ă��邩�ǂ������ȉ��Ō��؂��Ă���B
            // CreateCheckStatusResponse �����b�N�ɂ��Ă���̂ŁA���b�N�Ŏw�肵���l���Ԃ��Ă��Ă���Ζ��Ȃ��A�Ƃ������f�ɂȂ��Ă���B
            result.Headers.RetryAfter.Delta.Is(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task Test_�V�i���I_RunOrchestrator()
        {
            var durableOrchestrationContextMock = new Mock<IDurableOrchestrationContext>();

            // IDurableOrchestrationContext.CallActivityAsync �Ŏw�肵���A�N�e�B�r�e�B�֐����� input �̒l�͐��m�Ɏw�肵�Ȃ���
            // Moq �� ReturnsAsync �̒l���������Ԃ��Ă��Ȃ��̂Œ��ӂ���B
            durableOrchestrationContextMock.Setup(x => x.CallActivityAsync<string>("E1_SayHello", "Tokyo")).ReturnsAsync("Hello Tokyo!");
            durableOrchestrationContextMock.Setup(x => x.CallActivityAsync<string>("E1_SayHello", "Seattle")).ReturnsAsync("Hello Seattle!");
            durableOrchestrationContextMock.Setup(x => x.CallActivityAsync<string>("E1_SayHelloDirect", "London")).ReturnsAsync("Hello London");

            var result = await HelloSequence.RunOrchestrator(durableOrchestrationContextMock.Object, _loggerMock.Object);

            result.Count.Is(3);
            result[0].Is("Hello Tokyo!");
            result[1].Is("Hello Seattle!");
            result[2].Is("Hello London");
        }

        [Fact]
        public void Test_�V�i���I_SayHello()
        {
            var durableActivityContextMock = new Mock<IDurableActivityContext>();

            durableActivityContextMock.Setup(x => x.GetInput<string>()).Returns("John");

            HelloSequence.SayHello(durableActivityContextMock.Object, _loggerMock.Object)
                         .Is("Hello John!");
        }

        [Fact]
        public void Test_�V�i���I_SayHelloDirect()
        {
            HelloSequence.SayHelloDirect("John", _loggerMock.Object)
                         .Is("Hello John");
        }
    }
}