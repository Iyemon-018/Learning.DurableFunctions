using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Learning.DurableFunctions
{
    public static class HelloSequence
    {
        [FunctionName("E1_HelloSequence")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context
            , ILogger log)
        {
            var outputs = new List<string>();

            // クライアント関数から渡されたパラメータは context.GetInput<>() で取得することができる。
            // この呼び出し方は引数の数を変えなくていい代わりにコードの可読性が低くなりそうなので、引数を追加したほうがいいかも？
            // 例えば以下のコードではどんな値が取得できるのかレビューするときとか判断難しそう。
            var content = context.GetInput<string>();
            log.LogInformation($"Request Content is {content}.");

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>("E1_SayHello", "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>("E1_SayHello", "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>("E1_SayHelloDirect", "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("E1_SayHello")]
        public static string SayHello([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            // 引数の IDurableActivityContext はオーケストレーション関数で設定されたパラメータを取得することができる。
            // 引数は string name のようにしても結果は変わらない。
            // 引数の数を変えたい場合は IDurableActivityContext のほうがいいかも？
            var name = context.GetInput<string>();
            log.LogInformation($"Saying hello to {name}. from {nameof(SayHello)}");
            return $"Hello {name}!";
        }

        [FunctionName("E1_SayHelloDirect")]
        public static string SayHelloDirect([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}. from {nameof(SayHelloDirect)}.");
            return $"Hello {name}";
        }

        [FunctionName("HelloSequence_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
                [HttpTrigger(AuthorizationLevel.Anonymous, methods: "post", Route = "orchestrators/{functionName}")]
                HttpRequestMessage req
              , [DurableClient] IDurableOrchestrationClient starter
              , string                                      functionName
              , ILogger                                     log)
        {
            // Function input comes from the request content.

            // こんな感じでリクエストボディも取得するしてオーケストレーション関数にわたすことができる。
            dynamic eventData  = await req.Content.ReadAsAsync<object>();
            string instanceId = await starter.StartNewAsync(functionName, eventData.testvalue);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}