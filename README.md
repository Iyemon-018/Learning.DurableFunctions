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

