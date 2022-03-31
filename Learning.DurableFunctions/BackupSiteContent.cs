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
    using System;
    using System.IO;
    using System.Linq;
    using Azure.Storage.Blobs;
    using Microsoft.WindowsAzure.Storage.Blob;

    public static class BackupSiteContent
    {
        /// <summary>
        /// パラメータで指定されたディレクトリ配下にあるファイルを Azure Blob Storage にアップロードして、そのファイルの合計サイズを返すオーケストレーション関数です。
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [FunctionName("E2_BackupSiteContent")]
        public static async Task<long> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            string rootDirectory = context.GetInput<string>()?.Trim();
            if (string.IsNullOrEmpty(rootDirectory))
            {
                rootDirectory = Directory.GetParent(typeof(BackupSiteContent).Assembly.Location).FullName;
            }

            // 入力パラメータで受け取ったディレクトリから再起ファイルパスのリストを取得するための関数を呼び出す。
            string[] files = await context.CallActivityAsync<string[]>("E2_GetFileList", rootDirectory);

            // 受け取ったファイルパスを Azure Blog Storage にアップロードする。
            var tasks = new Task<long>[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                tasks[i] = context.CallActivityAsync<long>("E2_CopyFileToBlob", files[i]);
            }

            await Task.WhenAll(tasks);

            // アップロードしたファイルの合計サイズを戻り値とする。
            long totalBytes = tasks.Sum(t => t.Result);
            return totalBytes;
        }

        /// <summary>
        /// 引数に指定したルートディレクトリ配下にあるファイルを再帰的に検索して、すべてのファイルパスを返す。
        /// </summary>
        /// <param name="rootDirectory"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("E2_GetFileList")]
        public static string[] GetFileList([ActivityTrigger] string rootDirectory, ILogger log)
        {
            // この関数はオーケストレーション関数では実装すべきではない。
            // オーケストレーション関数の基本ルールの１つに「ローカルファイルシステムへの I/O 操作を行うべきではない」があるため。
            // オーケストレーション関数は冪等でなければならないが、アクティビティ関数はその必要がない。
            // なので、アクティビティ関数側でローカルファイルシステムへアクセスしている。
            // cf. https://docs.microsoft.com/ja-jp/azure/azure-functions/durable/durable-functions-code-constraints#orchestrator-code-constraints
            log.LogInformation($"Searching for files under `{rootDirectory}`...");
            string[] files = Directory.GetFiles(rootDirectory, "*", SearchOption.AllDirectories);
            log.LogInformation($"Found {files.Length} file(s) under {rootDirectory}");

            return files;
        }

        /// <summary>
        /// 引数に指定したファイルパスをディスクから読み取り、Azure Blob Storage の"backups"コンテナー内に非同期ストリーミングする。
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="binder"></param>
        /// <param name="log"></param>
        /// <returns>アップロードしたファイルのバイトサイズを返します。</returns>
        [FunctionName("E2_CopyFileToBlob")]
        public static async Task<long> CopyFileToBlob([ActivityTrigger] string filePath, Binder binder, ILogger log)
        {
            long   byteCount      = new FileInfo(filePath).Length;
            string blobPath       = filePath.Substring(Path.GetPathRoot(filePath).Length).Replace('\\', '/');
            string outputLocation = $"backups/{blobPath}";

            log.LogInformation($"Copy `{filePath}` to `{outputLocation}`. Total bytes = {byteCount}.");

            await using Stream source      = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using Stream destination = await binder.BindAsync<CloudBlobStream>(new BlobAttribute(outputLocation));
            
            await source.CopyToAsync(destination);

            return byteCount;
        }
    }
}