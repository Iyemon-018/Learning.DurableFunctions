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
    using System.Security.AccessControl;
    using System.Security.Principal;
    using Azure.Storage.Blobs;
    using Microsoft.WindowsAzure.Storage.Blob;

    public static class BackupSiteContent
    {
        /// <summary>
        /// �p�����[�^�Ŏw�肳�ꂽ�f�B���N�g���z���ɂ���t�@�C���� Azure Blob Storage �ɃA�b�v���[�h���āA���̃t�@�C���̍��v�T�C�Y��Ԃ��I�[�P�X�g���[�V�����֐��ł��B
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

            // ���̓p�����[�^�Ŏ󂯎�����f�B���N�g������ċN�t�@�C���p�X�̃��X�g���擾���邽�߂̊֐����Ăяo���B
            string[] files = await context.CallActivityAsync<string[]>("E2_GetFileList", rootDirectory);

            // �t�@���A�E�g�E�t�@���C���̊̂͂���B
            // ���ׂẴA�N�e�B�r�e�B�֐��� Task �őҋ@������ await Task.WhenAll(tasks) �ŕ�����s������B
            // ����ɂ�� E2_GetFileList �̂悤�Ƀt�@�C���̗񋓂�
            // E2_CopyFileToBlob �̂悤�ɃA�b�v���[�h�𕪂��Ă����G�ɂȂ�Ȃ������b�g���L��B
            // ���̂悤�ȏ�ԊǗ��ƒ����������Ɏ��s����悤�ȃP�[�X�ɂ����� �t�@���A�E�g�E�t�@���C�� �͗L���Ȏ�i�ƂȂ�B

            // �󂯎�����t�@�C���p�X�� Azure Blog Storage �ɃA�b�v���[�h����B
            var tasks = new Task<long>[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                tasks[i] = context.CallActivityAsync<long>("E2_CopyFileToBlob", files[i]);
            }

            await Task.WhenAll(tasks);

            // �^�X�N���s��ɑ����ĂȂɂ�����Ă������B

            // �A�b�v���[�h�����t�@�C���̍��v�T�C�Y��߂�l�Ƃ���B
            long totalBytes = tasks.Sum(t => t.Result);
            return totalBytes;
        }

        /// <summary>
        /// �����Ɏw�肵�����[�g�f�B���N�g���z���ɂ���t�@�C�����ċA�I�Ɍ������āA���ׂẴt�@�C���p�X��Ԃ��B
        /// </summary>
        /// <param name="rootDirectory"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("E2_GetFileList")]
        public static string[] GetFileList([ActivityTrigger] string rootDirectory, ILogger log)
        {
            // ���̊֐��̓I�[�P�X�g���[�V�����֐��ł͎������ׂ��ł͂Ȃ��B
            // �I�[�P�X�g���[�V�����֐��̊�{���[���̂P�Ɂu���[�J���t�@�C���V�X�e���ւ� I/O ������s���ׂ��ł͂Ȃ��v�����邽�߁B
            // �I�[�P�X�g���[�V�����֐��͙p���łȂ���΂Ȃ�Ȃ����A�A�N�e�B�r�e�B�֐��͂��̕K�v���Ȃ��B
            // �Ȃ̂ŁA�A�N�e�B�r�e�B�֐����Ń��[�J���t�@�C���V�X�e���փA�N�Z�X���Ă���B
            // cf. https://docs.microsoft.com/ja-jp/azure/azure-functions/durable/durable-functions-code-constraints#orchestrator-code-constraints
            log.LogInformation($"Searching for files under `{rootDirectory}`...");
            string[] files = Directory.GetFiles(rootDirectory, "*", SearchOption.AllDirectories);
            log.LogInformation($"Found {files.Length} file(s) under {rootDirectory}");

            return files;
        }

        /// <summary>
        /// �����Ɏw�肵���t�@�C���p�X���f�B�X�N����ǂݎ��AAzure Blob Storage ��"backups"�R���e�i�[���ɔ񓯊��X�g���[�~���O����B
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="binder"></param>
        /// <param name="log"></param>
        /// <returns>�A�b�v���[�h�����t�@�C���̃o�C�g�T�C�Y��Ԃ��܂��B</returns>
        [FunctionName("E2_CopyFileToBlob")]
        public static async Task<long> CopyFileToBlob([ActivityTrigger] string filePath, ILogger log)
        {
            // Blob Storage �� .NET �̃o�[�W�����ɂ���Ď��s���Ă����̂ŁA�Ƃ肠�������[�J���X�g���[�W�Ńt�@�C�����R�s�[������@�ɐ؂�ւ��Ă���B
            long   byteCount      = new FileInfo(filePath).Length;
            //string blobPath       = filePath.Substring(Path.GetPathRoot(filePath).Length).Replace('\\', '/');
            //string outputLocation = $"backups/{blobPath}";
            string blobPath       = Path.Combine(Path.GetTempPath(), nameof(BackupSiteContent));
            string outputLocation = Path.Combine(blobPath, Path.GetFileName(filePath));
            if (!Directory.Exists(blobPath))
            {
                Directory.CreateDirectory(blobPath);
            }

            log.LogInformation($"Copy `{filePath}` to `{outputLocation}`. Total bytes = {byteCount}.");

            //await using Stream source      = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            //await using Stream destination = await binder.BindAsync<CloudBlobStream>(new BlobAttribute(outputLocation));
            
            //await source.CopyToAsync(destination);

            File.Copy(filePath, outputLocation);

            return byteCount;
        }
    }
}