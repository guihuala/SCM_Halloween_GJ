# 配置表系统

这个模块用于承载“普通游戏数据表”的运行时数据结构。编辑期源文件放在 `Assets/Config`，导入后的运行时资源放在 `Assets/Resources/Data`。

## 当前示例

- Excel：`Assets/Config/GameData/ExampleItems.xlsx`
- SO：`Assets/Resources/ConfigTables/ExampleItemTable.asset`
- 示例数据结构：`Assets/Scripts/GameData/ExampleItemTable.cs`
- 对话表 SO：`Assets/Resources/ConfigTables/DialogueTable.asset`
- 对话表结构：`Assets/Scripts/Framework/Dialogue/DialogueTable.cs`

对话表有单独导入窗口：

- `Tools/Template/Dialogue/Import Dialogue Excel`

这样普通游戏数据表和剧情/对话配置可以分开维护。通用导表工具仍能导入 `DialogueTable`，但推荐使用对话专用窗口，因为它会额外校验 `dialogueId + lineIndex`，并支持多工作表合并。

要让一个 SO 类型出现在自动创建下拉里，需要给 SO 类添加 `[ConfigTableAsset]`：

```csharp
[CreateAssetMenu(fileName = "ExampleItemTable", menuName = "Template/Config Table/Example Item Table")]
[ConfigTableAsset]
public class ExampleItemTable : ScriptableObject
{
    public List<ExampleItemData> items = new List<ExampleItemData>();
}
```

## 导入方式

打开 Unity 菜单：

- `Tools/Template/Config Tables/Import Excel To ScriptableObject`

在窗口中选择：

- Excel File：要导入的 `.xlsx`
- Sheet：要导入的工作表
- Target ScriptableObject：目标 SO，可留空
- New SO Type：目标 SO 留空时，选择要自动创建的 SO 类型
- Save Path：目标 SO 留空时，自动创建的 SO 保存路径
- List Field：目标 SO 上的 `List<T>` 字段

工具会用 Excel 第一行表头匹配 `T` 上的字段名，并把后续行转换成列表数据。

## 支持类型

- `string`
- `int`
- `float`
- `double`
- `bool`
- `enum`

当前可处理线性对话表，例如 `DialogueTable`。对话树、嵌套数组、复杂条件表达式、Ink/Yarn Spinner 等特殊结构建议以后做专门导入器，最后再转换为运行时使用的 `DialogueSequence` 或 `DialogueTable`。
