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

            // �N���C�A���g�֐�����n���ꂽ�p�����[�^�� context.GetInput<>() �Ŏ擾���邱�Ƃ��ł���B
            // ���̌Ăяo�����͈����̐���ς��Ȃ��Ă�������ɃR�[�h�̉ǐ����Ⴍ�Ȃ肻���Ȃ̂ŁA������ǉ������ق������������H
            // �Ⴆ�Έȉ��̃R�[�h�ł͂ǂ�Ȓl���擾�ł���̂����r���[����Ƃ��Ƃ����f������B
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
            // ������ IDurableActivityContext �̓I�[�P�X�g���[�V�����֐��Őݒ肳�ꂽ�p�����[�^���擾���邱�Ƃ��ł���B
            // ������ string name �̂悤�ɂ��Ă����ʂ͕ς��Ȃ��B
            // �����̐���ς������ꍇ�� IDurableActivityContext �̂ق������������H
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

            // ����Ȋ����Ń��N�G�X�g�{�f�B���擾���邵�ăI�[�P�X�g���[�V�����֐��ɂ킽�����Ƃ��ł���B
            dynamic eventData  = await req.Content.ReadAsAsync<object>();
            string instanceId = await starter.StartNewAsync(functionName, eventData.testvalue);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}