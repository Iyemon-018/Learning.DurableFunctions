# Learning.DurableFunctions

`Durable Functions`を学習するためのリポジトリです。

## 参考資料

- [Durable Functions の概要 \- Azure \| Microsoft Docs](https://docs.microsoft.com/ja-jp/azure/azure-functions/durable/durable-functions-overview?tabs=csharp)
- [Azure Durable Functions のドキュメント \| Microsoft Docs](https://docs.microsoft.com/ja-jp/azure/azure-functions/durable/)
- [多分わかりやすいDurable Functions 〜サーバーレスは次のステージへ〜【入門編】 \| SIOS Tech\. Lab](https://tech-lab.sios.jp/archives/12991)

## 試す

### [C\# を使用して Azure で最初の永続関数を作成する \| Microsoft Docs](https://docs.microsoft.com/ja-jp/azure/azure-functions/durable/durable-functions-create-first-csharp?pivots=code-editor-visualstudio)

[Function1.cs](/Learning.DurableFunctions/Function1.cs)

1. 関数の URL をブラウザなどで実行する。
1. 応答の`statusQueryGetUri`に設定された URL をブラウザなどで実行する。
1. 応答結果で取得した JSON がオーケストレーション関数`RunOrchestrator`の実行結果となる。  
   `RunOrchestrator`の戻り値は応答の`output`に格納される。

### [Durable Functions での関数チェーン \- Azure \| Microsoft Docs](https://docs.microsoft.com/ja-jp/azure/azure-functions/durable/durable-functions-sequence?tabs=csharp)

[HelloSequence.cs](/Learning.DurableFunctions/HelloSequence.cs)

1. `POST http://localhost:7071/api/orchestrators/E1_HelloSequence HTTP/1.1`を呼び出す。
1. 応答結果のヘッダー`Location`の URL を実行する。
1. 応答結果がオーケストレーション関数で実行された結果となる。

ログはこうなった。
`Saying hello to XXX. from XXX.`が３回出力されている。
異なる関数から呼び出されたログであることがわかる。

```log
[2022-03-30T13:59:08.914Z] Host lock lease acquired by instance ID '000000000000000000000000F2D5F557'.
[2022-03-30T13:59:54.921Z] Executing 'HelloSequence_HttpStart' (Reason='This function was programmatically called via the host APIs.', Id=292a67d2-fb96-403d-8e65-b460672737e4)
[2022-03-30T13:59:55.043Z] Started orchestration with ID = 'ba8520b8e42448a1b3ea7c805f52ca90'.
[2022-03-30T13:59:55.061Z] Executed 'HelloSequence_HttpStart' (Succeeded, Id=292a67d2-fb96-403d-8e65-b460672737e4, Duration=157ms)
[2022-03-30T13:59:55.128Z] Executing 'E1_HelloSequence' (Reason='(null)', Id=64c66c14-a0cb-4b2a-be54-aea944de8d7c)
[2022-03-30T13:59:55.154Z] Executed 'E1_HelloSequence' (Succeeded, Id=64c66c14-a0cb-4b2a-be54-aea944de8d7c, Duration=27ms)
[2022-03-30T13:59:55.212Z] Executing 'E1_SayHello' (Reason='(null)', Id=1501bcb7-2623-4003-b9df-20de8fc79620)
[2022-03-30T13:59:55.216Z] Saying hello to Tokyo. from SayHello
[2022-03-30T13:59:55.217Z] Executed 'E1_SayHello' (Succeeded, Id=1501bcb7-2623-4003-b9df-20de8fc79620, Duration=6ms)
[2022-03-30T13:59:55.262Z] Executing 'E1_HelloSequence' (Reason='(null)', Id=f54ebb5e-a3bd-41af-921d-01a55a2d1f2b)
[2022-03-30T13:59:55.271Z] Executed 'E1_HelloSequence' (Succeeded, Id=f54ebb5e-a3bd-41af-921d-01a55a2d1f2b, Duration=9ms)
[2022-03-30T13:59:55.285Z] Executing 'E1_SayHello' (Reason='(null)', Id=44d092ee-495b-4a97-b0d1-b62a70b8d2c4)
[2022-03-30T13:59:55.287Z] Saying hello to Seattle. from SayHello
[2022-03-30T13:59:55.287Z] Executed 'E1_SayHello' (Succeeded, Id=44d092ee-495b-4a97-b0d1-b62a70b8d2c4, Duration=2ms)
[2022-03-30T13:59:55.303Z] Executing 'E1_HelloSequence' (Reason='(null)', Id=51e6ee54-0c19-412e-aa7a-39bbf99c9a00)
[2022-03-30T13:59:55.304Z] Executed 'E1_HelloSequence' (Succeeded, Id=51e6ee54-0c19-412e-aa7a-39bbf99c9a00, Duration=2ms)
[2022-03-30T13:59:55.317Z] Executing 'E1_SayHelloDirect' (Reason='(null)', Id=d52d3cd1-2651-44ff-805d-40b12a72a53a)
[2022-03-30T13:59:55.318Z] Saying hello to London. from SayHelloDirect.
[2022-03-30T13:59:55.319Z] Executed 'E1_SayHelloDirect' (Succeeded, Id=d52d3cd1-2651-44ff-805d-40b12a72a53a, Duration=2ms)
[2022-03-30T13:59:55.334Z] Executing 'E1_HelloSequence' (Reason='(null)', Id=3fd1b99c-38d9-458e-9f9e-966b137606a5)
[2022-03-30T13:59:55.339Z] Executed 'E1_HelloSequence' (Succeeded, Id=3fd1b99c-38d9-458e-9f9e-966b137606a5, Duration=4ms)
```

`POST`するときにリクエストボディも受け取りたい場合も対応できる。
オーケストレーション関数まで、リクエストボディで取得した値を引き渡したい場合は以下のように、第２引数に設定する。

```cs
string instanceId = await starter.StartNewAsync(functionName, eventData.testvalue);
```

オーケストレーション関数で受け取る場合は以下のようになる。

```cs
public static async Task<List<string>> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
{
    ...
    var content = context.GetInput<string>();
    ...
}
```

