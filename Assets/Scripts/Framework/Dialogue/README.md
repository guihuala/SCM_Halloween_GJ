# Dialogue System

这是一个基于 `UIManager` 的通用对话模块，默认面板名为 `DialoguePanel`，路径已登记到 `Assets/Resources/Data/UIDataListSO.asset`。

## 已支持

- 逐字显示 / 打字机效果
- 点击对话框、按空格或回车继续
- 自动播放开关
- 快速跳过
- 按住 Ctrl 加速逐字显示
- 角色名、头像、立绘、表情头像
- 普通对话、旁白、内心独白、系统提示四种样式
- 按对话 ID 播放
- 通过配置表导入对话数据
- 使用项目统一本地化 key
- Unity 富文本
- `{变量}` 文本替换
- 分支选项、条件显示、选项效果

## 配置表导入

对话也可以使用可视化编辑器直接改：

- 菜单：`Tools/Template/Dialogue/Dialogue Editor`
- 默认进入节点视图：每个对话组显示为一个节点，分支跳转会画成连线
- 节点可以拖拽，坐标保存到 Editor-only 的 `Assets/Editor/Config/DialogueEditorLayout.asset`
- 支持框选/多选移动、鼠标滚轮缩放、中键或右键拖动画布平移、一键整理布局、回到选中节点
- 右键节点画布可以新增对话节点，或删除当前选中的对话组
- 校验面板会检查重复行号、缺失本地化 key、断开的跳转目标，以及条件/效果格式；有问题的节点会变成黄色或红色
- 工具栏可以导出当前 `DialogueTable` 为 Excel，并显示最近导入、导出、编辑器修改时间
- 可以切换到列表视图；节点或列表中选中对话组后，在下方/右侧编辑台词行、文本 key、对应本地化文本、表现和分支选项
- 保存内容会写回 `DialogueTable.asset` 和 `LocalizationTable.asset`

对话表推荐使用独立导入窗口，避免和普通游戏数据混在一起：

- 菜单：`Tools/Template/Dialogue/Import Dialogue Excel`
- 默认 Excel：`Assets/Config/Dialogue/DialogueTable.xlsx`
- 默认输出：`Assets/Resources/ConfigTables/DialogueTable.asset`

导入窗口支持：

- 导入单个工作表
- 导入全部工作表并合并
- 自动创建或覆盖 `DialogueTable.asset`
- 校验 `dialogueId + lineIndex` 是否重复
- 导入后保留已有节点布局，并记录最近导入来源

这意味着一个 Excel 文件里可以有多张工作表，例如：

```text
Village
MainQuest
Tutorial
```

每张工作表里仍然可以配置多组对话，运行时用 `dialogueId` 区分。

`DialogueTable` 也保留了 `[ConfigTableAsset]`，必要时仍可用通用导表工具导入：

- 菜单：`Tools/Template/Config Tables/Import Excel To ScriptableObject`
- 新建 SO 类型：`DialogueTable`
- 保存路径：`Assets/Resources/ConfigTables/DialogueTable.asset`
- 列表字段：`lines`

Excel 表头需要和 `DialogueTableRow` 字段名一致：

```text
dialogueId,lineIndex,speakerName,localizationKey,portraitResource,standingResource,expression,style,autoPlayDelay
```

在 `DialogueTable` 资源中，分支选项保存为 `options` 列表，方便在 Inspector 中折叠和扩展。

Excel 示例表为了方便横向填写，仍然提供 4 组选项列；导入时会自动转换成 `options` 列表：

```text
option1LocalizationKey,option1NextDialogueId,option1Condition,option1Effects
option2LocalizationKey,option2NextDialogueId,option2Condition,option2Effects
option3LocalizationKey,option3NextDialogueId,option3Condition,option3Effects
option4LocalizationKey,option4NextDialogueId,option4Condition,option4Effects
```

如果选项填写了 `optionXNextDialogueId`，点击后会跳到对应对话 ID。没有填写时，会继续播放当前对话组的下一句。

`optionXCondition` 用于隐藏或解锁选项，支持：

```text
charm>=5
item:key
flag:key
quest:necklace=completed
!flag:questAccepted
```

`optionXEffects` 用于写入简单运行时变量，多个效果用英文分号分隔：

```text
quest:necklace=accepted
add:favor.oldman=1
set:moral=10
flag:questAccepted
item:key
removeItem:key
```

这些状态保存在 `DialogueRuntimeState` 中，只是对话模板层的轻量状态容器，方便原型验证。`SaveManager.SaveGame()` 会把它们写入 `SaveData.dialogue`，`LoadGame()` 会恢复它们，`NewGame()` 会清空旧分支状态。正式项目可以在选项回调里替换成任务、好感度、阵营、背包等独立系统。

同一个 `dialogueId` 的多行会按 `lineIndex` 排序后播放。例如：

```text
template_dialogue_test_001
npc_village_oldman_001
```

`style` 可填写：`Normal`、`Narration`、`InnerThought`、`System`。

`portraitResource` 和 `standingResource` 是 `Resources` 下的 Sprite 路径，例如 `Characters/OldMan/portrait_normal`。

`localizationKey` 和 `optionXLocalizationKey` 都直接读取现有 `LocalizationTable`，语言切换完全跟随 `LocalizationManager.CurrentLanguage`。对话表只负责流程、角色、表现和分支，不再保存各语言正文。

当前运行时入口使用导入后的 ScriptableObject。CSV、JSON、Ink、Yarn Spinner 可以后续接入为“编辑器导入源”或“运行时数据源”，最终仍转换成同一份 `DialogueTable` 或 `DialogueSequence`，UI 层不需要改。

## 基本调用

可以通过 `Create > Game Template > Dialogue > Dialogue Sequence` 创建对话资产，也可以在代码里临时组装：

```csharp
var sequence = new DialogueSequence();
sequence.lines.Add(new DialogueLine
{
    speakerName = "村长",
    text = "勇者，你终于来了。",
    style = DialogueBoxStyle.Normal
});

sequence.lines.Add(new DialogueLine
{
    text = "风从远处的山谷吹来。",
    style = DialogueBoxStyle.Narration
});

DialogueManager.Instance.Play(sequence);
```

按配置表 ID 播放：

```csharp
DialogueVariableResolver.Set("playerName", "桂花");
DialogueManager.Instance.PlayById("npc_village_oldman_001");
```

## 角色配置

可以通过 `Create > Game Template > Dialogue > Character Profile` 创建角色配置：

- `Display Name`：默认角色名
- `Default Portrait`：默认头像
- `Default Standing`：默认立绘
- `Expressions`：按表达式名称切换头像，例如 `happy`、`angry`

如果 `DialogueLine` 里填写了 `speakerName`、`portraitOverride` 或 `standingOverride`，会优先使用这一句自己的覆盖配置。
