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
        public async Task Test_シナリオ_HttpStart()
        {
            var durableClientMock = new Mock<IDurableClient>();
            var functionName      = "Function1";
            var instanceId        = "123";

            // IDurableClient.StartNewAsync
            // IDurableClient.CreateCheckStatusResponse
            // この２つは、HttpStart メソッド内部で呼び出されるのでモックが必要になる。
            // 以下の .Returns の値はサンプルなので、実際のコードだと適宜修正する必要がある。
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

            // Durable Functions では応答結果に、IDurableClient.CreateCheckStatusResponse の戻り値が返るような仕組みになっている。
            // この値が正常に返ってきているかどうかを以下で検証している。
            // CreateCheckStatusResponse をモックにしているので、モックで指定した値が返ってきていれば問題ない、という判断になっている。
            result.Headers.RetryAfter.Delta.Is(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task Test_シナリオ_RunOrchestrator()
        {
            var durableOrchestrationContextMock = new Mock<IDurableOrchestrationContext>();

            // IDurableOrchestrationContext.CallActivityAsync で指定したアクティビティ関数名と input の値は正確に指定しないと
            // Moq の ReturnsAsync の値が正しく返ってこないので注意する。
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
        public void Test_シナリオ_SayHello()
        {
            var durableActivityContextMock = new Mock<IDurableActivityContext>();

            durableActivityContextMock.Setup(x => x.GetInput<string>()).Returns("John");

            HelloSequence.SayHello(durableActivityContextMock.Object, _loggerMock.Object)
                         .Is("Hello John!");
        }

        [Fact]
        public void Test_シナリオ_SayHelloDirect()
        {
            HelloSequence.SayHelloDirect("John", _loggerMock.Object)
                         .Is("Hello John");
        }
    }
}