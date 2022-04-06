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

        [Fact]
        public async Task Test_シナリオ_HttpStart()
        {
            var durableClientMock = new Mock<IDurableClient>();
            var functionName      = "Function1";
            var instanceId        = "123";

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

            var logger = new Mock<ILogger>();

            var result = await HelloSequence.HttpStart(new HttpRequestMessage
                                                       {
                                                           Content    = new StringContent("{}", Encoding.UTF8, "application/json")
                                                         , RequestUri = new Uri("http://localhost:7071/orchestrators/E1_HelloSequence")
                                                       }
                  , durableClientMock.Object
                  , functionName
                  , logger.Object);

            result.IsNotNull();
            result.Headers.RetryAfter.Delta.Is(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task Test_シナリオ_RunOrchestrator()
        {
            var durableOrchestrationContextMock = new Mock<IDurableOrchestrationContext>();
            var loggerMock                          = new Mock<ILogger>();

            durableOrchestrationContextMock.Setup(x => x.CallActivityAsync<string>("E1_SayHello", "Tokyo")).ReturnsAsync("Hello Tokyo!");
            durableOrchestrationContextMock.Setup(x => x.CallActivityAsync<string>("E1_SayHello", "Seattle")).ReturnsAsync("Hello Seattle!");
            durableOrchestrationContextMock.Setup(x => x.CallActivityAsync<string>("E1_SayHelloDirect", "London")).ReturnsAsync("Hello London");

            var result = await HelloSequence.RunOrchestrator(durableOrchestrationContextMock.Object, loggerMock.Object);

            result.Count.Is(3);
            result[0].Is("Hello Tokyo!");
            result[1].Is("Hello Seattle!");
            result[2].Is("Hello London");
        }

        [Fact]
        public void Test_シナリオ_SayHello()
        {
            var durableActivityContextMock = new Mock<IDurableActivityContext>();
            var loggerMock                 = new Mock<ILogger>();

            durableActivityContextMock.Setup(x => x.GetInput<string>()).Returns("John");

            HelloSequence.SayHello(durableActivityContextMock.Object, loggerMock.Object)
                         .Is("Hello John!");
        }

        [Fact]
        public void Test_シナリオ_SayHelloDirect()
        {
            var loggerMock = new Mock<ILogger>();

            HelloSequence.SayHelloDirect("John", loggerMock.Object)
                         .Is("Hello John");
        }
    }
}