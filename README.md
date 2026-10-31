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

选中目的地后，预览卡可切换 **Safe route / Fastest route**。默认安全路线按当前整批货物的条件避开受损路面，并考虑载具额度耗尽后失去保护的情况；没有安全路线时仍可出发，但会明确列出预计损坏的货物。预览显示累计订单回合、预计超时、载具耗尽和事件风险，回合估算不包含随机事件。

从 **S · Parcel Depot（快递站）** 开始。打开 Orders 查看任意订单，自行选择供应者、配送顺序和路线；空载赶路不启动订单计时。首次配送教学会持续到实际交付，中间路口会提示按 E 继续。通关后以配送相册展示成绩：货物插画、收件人头像、配送结果和实际／目标回合；滚动查看全部订单，悬停卡片查看路线及详细受损原因。重开会重新提供教学。

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

路网源文件为 `Assets/Scripts/Prototype/CourierStreetMap.cs`。地图底图和动态路线共用 1200×820 坐标系。

## 替换地图背景

地图按“背景图片 → 手绘基础路线 → 动态高亮/事件标记 → 地点建筑 → 移动标记”分层。背景不拦截点击；选路、路径高亮、事件标记、行进效果仍由实时路网生成。

1. 在 Unity 选择 `Assets/Resources/NightVoyage/MapArtwork.asset`，把新背景拖入 **Terrain**；**Terrain Tint** 保持白色即可保留原画颜色。当前使用的底图是 `Assets/Art/Sprite/MapPlaceholder/terrain_integrated.png`，透明的不规则边缘直接融合到界面底色。
2. 新背景按 **1200×820** 绘制，推荐导出 **2400×1640 PNG**（相同比例的整数倍也可以）。图片导入类型设为 **Sprite (2D and UI)**、Sprite Mode 设为 **Single**、Mesh Type 设为 **Full Rect**。保持整张画布，不要裁掉四周空白。旧占位图比例不同，会铺到统一画布；正式替换图请按上述比例制作。
3. 使用 `Tools > Night Courier > Export Map Art Guide` 导出 `ArtPreviews/MapArtGuide/map-route-guide.svg` 和节点坐标 CSV。SVG 为透明参考层，包含实际道路弯曲、平行路段及节点；坐标从左上角计算。绘制时对齐参考，导出背景前隐藏参考层。
4. 背景只画地形、河流、树林、装饰。道路、节点圆点、地点建筑、幽灵与游戏文字已有独立层，避免重复画入底图。建筑仍在 **Landmarks** 中分别替换。
5. **Terrain Edge Feather** 控制四周融入界面背景的宽度，当前为0，使用图片自身绘制的透明收边；提高数值可额外叠加Shader羽化。羽化由UI Shader作用于底图，原PNG、路线、地标和点击区域保持独立。
6. 覆盖原 PNG 时保留 `.meta`；使用不同文件名时重新拖入 **Terrain**。重新打开地图即可看到替换。切换窗口比例时，背景和路线会等比缩放、居中对齐，空余区域留边。清空 **Terrain** 会回退到程序区域色块，路线继续可用。

更换美术不会改变路网位置；若想移动道路或节点，需要同步修改 `CourierStreetMap.cs` 并重新导出参考图。

## 替换手绘路线

当前手绘路线是 `Assets/Art/Sprite/MapPlaceholder/routes_handdrawn.png`，由用户提供的 `map-route-guide 2.png` 接入。它是 **2400×1640 透明PNG**，与背景共用完整地图画布；不要裁掉透明留白。

- 在 `MapArtwork.asset` 的 **Route Artwork** 字段替换路线图。也可直接覆盖同名 PNG，并保留 `.meta`。
- PNG 负责常态道路和节点圆点，系统自动隐藏重复的基础线和圆点。选中、悬停、行进路线、事件标记、文字和幽灵仍由游戏动态叠加。
- 清空 **Route Artwork** 时自动恢复程序道路与圆点。**Terrain** 仍独立控制背景图。
- 修改画笔造型和线条颜色可以直接换图；移动路口或改变连通关系，需要同步修改 `CourierStreetMap.cs`。

当前地图使用 `terrain_integrated.png` 底图、你更新的 `landmarks_atlas.png` 地标图集和 `routes_handdrawn.png` 手绘路线。已清理被替换的旧AI地图资源、占位地标单图及旧生成预览和备份。

## 修改地标图集

六个地标共用 `Assets/Art/Sprite/MapPlaceholder/Landmarks/landmarks_atlas.png`。直接修改这张透明 PNG，保留同名 `.meta`，Unity 会继续使用原切片和地点绑定，无须逐个重新拖图。

- 当前尺寸 **1536×1024**，固定 **3列×2行**，每格 **512×512**。
- 从左到右：上排为地点 ID **0、1、2**，下排为 **3、4、5**；切片名固定为 `landmark_00`～`landmark_05`。ID 对应 `MapArtwork.asset > Landmarks > Stop Id`，不依赖列表排序。
- 保留透明底和整张画布，每个地标画在自己的格子里；当前底部落脚点在格子横向50%、距底部约11%处（`MapArtwork.asset` 的 Anchor）；绘制新图时保持同一落脚位置。格子边缘保留透明留白，避免相邻图片串色。不要把标号、网格线或地点文字画入 PNG。
- 如需改变整张图集分辨率，保持3×2的正方形格子，再执行 `Tools > Night Courier > Configure Landmark Atlas`，重新按3×2切片；名称和绑定保持不变。默认画布尺寸下，仅改画面像素不需要运行菜单。
- 图集配置工具只切片和绑定当前PNG，不生成或覆盖图片；PNG缺失时会提示补回资源。
- 地标位置和大小仍在 `MapArtwork.asset` 的 **Anchor / Offset / Scale** 调整。图集仅改变图片组织方式，地图道路、节点位置和点击逻辑保持原样。

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

所有tooltip、目的地提示、货物卡与常规弹窗使用浅纸色底、深色文字；对话框1840×260、正文36号，教学框1440×152、标题30号/说明28号。地图幽灵标记使用 `Assets/Resources/NightVoyage/VehicleIcons/Ghost_.png`，并在地图Prefab的 `ghostArtwork` 中显式绑定。大号阅读框效果：`ArtPreviews/light-ui-large-reading.png`；浅色暂停窗：`ArtPreviews/light-popup-pause.png`。

## 可用性修正

Orders 使用右侧竖栏，待领取与已装载上下排列；Talk / Continue 固定在地图上方。地点标牌使用场所名称（SUBWAY / FOREST / MEAD / TOWER / MUSEUM / BAR）。地图圆点、文字和地标共用选路交互；提示框优先显示在控件上方或下方，避开右侧数值。正文使用常规无衬线字体，地图与订单字号增大；Goolie 对话复用现有幽灵立绘作为头像。

事件保留每个合资格路口 35% 的概率，每单最多一次，空载不触发；不同路口的机会累计，因此经过 2 个合资格路口至少遇到一次的概率为 57.75%。每次新开局与重开使用新的随机种子。验收入口：`Tools > Night Courier > Checks > Usability and Depot Start`。

## 界面字体

界面统一使用 Nunito SemiBold（600），来自 Google Fonts 官方 Nunito 项目，圆润字形适配手绘氛围，常规宽度方便阅读长对话、订单及数字。字体位于 `Assets/Resources/NightVoyage/Fonts/Nunito-SemiBold.ttf`，由官方可变字体固定为 600 字重；预制体、场景、运行时提示框与 UI 生成工具均已绑定。OFL 1.1 授权副本位于 `Assets/Art/Fonts/Nunito-OFL.txt` 及 `Assets/StreamingAssets/Nunito-OFL.txt`，后者随构建发布。来源：https://github.com/google/fonts/tree/main/ofl/nunito 。

## 图标界面

常驻界面仅保留剧情、地点名、订单名和关键数字。订单开关、关闭、对话、继续、路线模式、换车、快进、事件确认与结算操作使用现有手绘图标；详情与快捷键通过悬停或键盘焦点提示查看。火焰／雨伞／高脚杯分别表示怕热、防水、易碎保护，订单数字旁的时钟表示当前／目标回合。教学改为左下角步骤图标与进度，悬停查看当前说明，关闭图标结束指引。
