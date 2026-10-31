# No Spooky, Just Parcels

万圣夜，幽灵快递员 Goolie 在六位怪物邻居之间运送包裹。通过规划路线、换车和应对随机事件，完成全部 **11 笔订单**。

## 运行

- Unity **2022.3.62f3c1**。
- 打开 `Assets/Scenes/MainMenu.unity` 开始；直接测试玩法可打开 `Assets/Scenes/GamePlay.unity`。
- 游戏名以主菜单 `mainmenu_title.png` 为准；界面为英文。

## 操作

| 操作 | 控制 |
| --- | --- |
| 查看地点、规划路线 | 点击地图节点，再点击出发 |
| 选择相邻方向 | WASD / 方向键 |
| 对话、继续已规划路线 | E |
| 查看订单、领取整批货物 | Tab 或 Orders；点击或拖拽领取 |
| 跳过当前移动动画 | F |
| 暂停 | Esc 或暂停按钮 |

只能在供应者所在地接单，同时持有一笔订单，接单后不可取消。抵达收件节点自动交付；空载移动不计订单回合。

## 当前规则

- 共享地图包含32个节点，只有显式节点连通；线条交叉不代表可以转向。
- 幽灵每回合移动1边，无使用额度；H耐热、W防水、Q减震载具每回合最多移动2边，各有5回合额度，遇NPC或载具站提前停止。
- 在H/W/Q站领取或更换对应载具，消耗1订单回合；旧车消失，新车获得完整额度。跨订单保留剩余额度，耗尽后切换幽灵。
- 高温、积水、颠簸分别影响怕热、怕水、易碎货物；当前对应车型提供保护。受损不可恢复，环境不额外增加移动回合。
- 携货结束移动后，C/J可能触发延误（+1订单回合），N/P/E可能触发倒退（退回刚经过的前一节点）。默认概率35%，每单最多一次；未触发不消耗机会，目的地优先交付。
- 事件不额外消耗载具额度，回退沿用刚结束回合的保护。超时或受损记为配送失败，但仍可交付并完成该单。
- 全部订单完成后显示简单结束画面与顺利送达数量，不设最终星级或奖励。

## 常用配置

配置位于 `Assets/Resources/NightVoyage/`，修改后重开本局生效。

| 文件 | 内容 |
| --- | --- |
| `Parcels.csv` | 12条货物配置、供应者/收件人、标签及回合上限；相同供收双方合为一单 |
| `Vehicles.csv`、`Stops.csv` | 载具参数、角色地点名称和开场白 |
| `Events.csv`、`EventRules.json` | 12个事件的文案、效果、插图、`sound_cue`，以及触发概率和节点 |
| `AudioCues.json` | 音效键、Resources资源路径、单独音量（0–1） |
| `Story.json` | 对话文本及触发条件；新增订单尚无专属剧情 |
| `Characters.asset`、`CargoIcons.asset`、`VehicleIcons.asset`、`MapArtwork.asset` | 头像、货物、载具和地图图片绑定 |
| `Palette.asset` | 共享界面配色 |

路网源文件为 `Assets/Scripts/Prototype/CourierStreetMap.cs`，核对清单位于 `Assets/Art/MapPlaceholder/map_layout.json`。

## 音画资源

- 主菜单：`Assets/Art/Sprite/UI/Mainmenu/`。
- 事件插图：`Assets/Resources/NightVoyage/EventIllustrations/`，social与weather两张复用；生成记录保存在同目录。
- HUD手绘素材：`Assets/Resources/NightVoyage/HUD/`；保留 `.meta` 可直接替换同名素材。
- 音效：`Assets/Resources/Audio/Courier/`；新增事件短音效位于 `Events/`，配置与替换方法见 [音效说明](Assets/Audio/README.md)。
- 音乐与既有录音署名见 `Assets/Resources/Audio/Courier/AudioCredits.txt`、`Assets/StreamingAssets/AudioCredits.txt` 和 `Assets/Audio/Licenses/`，发布时保留。

## 验证与维护

- `Tools > Night Courier > Run Rule Checks`：订单、移动、环境、事件和通关规则。
- `Tools > Night Courier > Checks > V2.4 Updated Rules and Event Art`：配置、地图点击、换车、事件插图与确认。
- `Tools > Night Courier > Checks > Audio (keep editor open)`：新音效导入、快进、并发提示、换车、载具行驶声、暂停和重开。
- 检查完成后恢复原场景；预览输出到 `ArtPreviews/`。
- 旧版依赖9单与永久保护的历史检查不再作为当前版本验收入口。
- `Rebuild UI Prefabs (overwrite layouts)` 会覆盖面板布局；仅替换素材或配置时无需运行。
