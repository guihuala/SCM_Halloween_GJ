# 配置表系统

这个模块用于承载“普通游戏数据表”的运行时数据结构。编辑期源文件放在 `Assets/Config`，导入后的运行时资源放在 `Assets/Resources/Data`。

## 当前示例

- Excel：`Assets/Config/GameData/ExampleItems.xlsx`
- SO：`Assets/Resources/ConfigTables/ExampleItemTable.asset`
- 示例数据结构：`Assets/Scripts/GameData/ExampleItemTable.cs`
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

当前用于平面数据行，例如物品参数。嵌套数组和复杂剧情结构不属于此通用导表工具的处理范围。夜航原型的对白由 `CourierRun.TalkToNpc()` 提供。
