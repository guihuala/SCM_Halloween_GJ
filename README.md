# 《穿越到DND世界送快递我该怎么办》· 可玩原型

Unity **2022.3.62f3c1**。打开 `Assets/Scenes/MainMenu.unity` 开始，或运行 `Assets/Scenes/GamePlay.unity`。游戏内沿用英文与 Barlow Condensed Light 字体。

## 2026-10-31 新版系统与随机事件

以最新《系统设计》《随机事件》和事件测试图为准：11笔订单，12条货物配置（维修装置与血液合批）。恢复女巫→小熊精的干燥剂/酒标/杂物、女巫→狼人的夜宵；鲜花、维修装置与怕热/怕水/易碎标签、3/4/5回合上限均按新表同步。稳定货物索引保留，避免破坏已有剧情和资源绑定。已到收件节点即自动交付；超时或受损为 Delivery failed，订单仍完成。全部完成后只显示完成数与顺利送达数。

幽灵1边/回合、无限使用；H/W/Q载具均2边/回合、5回合额度。仅H/W/Q站点提供对应车型，领取/换车消耗1订单回合，旧车消失，新车获得完整额度。保护只属于当前载具，不再永久叠加，不再维修或解锁。环境只对对应敏感货物造成不可恢复的受损，不额外增加高温移动时间或损耗。载具最后一回合的保护保留至事件回退结算结束。

全部圆形节点可点击；点击远处目的地会规划路线，每次出发执行一个移动回合，到回合终点停下，可换方向或按E继续原路线。遇到NPC/H/W/Q提前停止。方向键/WASD按方向选择相邻节点，F只跳过本回合动画。跨订单保留载具剩余额度，空载移动不计订单回合。

事件只在携货结束移动、停到指定节点时判定：C/J抽取7个延误事件（+1订单回合，不耗载具），N/P/E抽取5个倒退事件（只回到最后一条边的前一节点，不加回合/额度、不连锁）。每单最多一次，未触发保留机会，目的地优先交付。`EventRules.json` 暂设35%概率，可配置概率和两类节点列表。

两张复用插图位于 `Assets/Resources/NightVoyage/EventIllustrations/`：social用于街头人群/糖果/巡游，weather用于热泉/蝙蝠/风雾/滑坡；`Events.csv` 的 illustration 列控制绑定。弹窗保留暂停与确认按钮，插图不拦截点击。

验证：`Tools > Night Courier > Checks > V2.4 Updated Rules and Event Art`，完成后恢复原场景并保留编辑器。纯规则入口 `CourierChecks.RunAll()`；旧版依赖9单、永久保护、行程中途必出事件的历史冒烟测试尚未迁移，请使用新版入口。截图位于 `ArtPreviews/v24-updated-map.png`、`v24-event-social.png`、`v24-event-weather.png`。

## 可编辑配置

`Assets/Resources/NightVoyage/Characters.asset` 是角色配置。在 Unity 中选中它，展开对应角色，将导入为 **Sprite (2D and UI)** 的图片拖入 `Portrait` 即可替换头像；也可修改 `Display Name`、`Personality` 和 `Accent`。`Stop Id` 对应下表，数组顺序不影响角色绑定。姓名用于英文游戏界面，中文性格字段供策划记录，不自动生成台词。开场白仍在 `Stops.csv`，追加对话在 `NightCourierModel.cs`。

| Stop Id | 角色 | 截图中的性格 | 当前头像 |
| --- | --- | --- | --- |
| 0 | 黑暗精灵 / Drow | 内向嘴拙妹 | gamejam_portrait_drow.png |
| 1 | 狼人 / Wolfwoman | 豪爽大姐姐 | gamejam_portrait_werewolf.png |
| 2 | 小熊精 / Little Bear | 嘟嘟囔囔、多愁善感的小妹妹 | gameJam2026_LittleWerebear.png |
| 3 | 吸血鬼 / Vampire | 骑士语 C 姐，原型按骑士角色扮演口吻处理 | gamejam_portrait_vampire.png |
| 4 | 移民僵尸 / Granny Zombie | 呆萌老太 | gameJam2026_jiangshiGranny_02.png |
| 5 | 女巫 / Witch | 控场的妈妈 | witch.png |

六位角色头像位于 `Assets/Art/Sprite/Character/Portrait/`。缺失头像使用配置中的 `Fallback Portrait`；未配置回退图片时隐藏图像，保留姓名和对白。头像与姓名只在角色对话时显示，行驶及普通站点提示恢复原有字幕宽度；支持暂停、跳过文字与重开。

物资与回合限制按顶部新版系统说明；原有10条索引保留，新增两批使用10/11，鲜花和维修装置切换到已有的对应图标。

`Tools > Night Courier > Install Character Portraits (preserve settings)` 可为已有 HUD 安装头像区域，保留已有角色配置和图片；完整重建 UI 也会安装。`CourierCharacterInstaller.InstallAndCheck` 检查六人绑定、十件物资与九批订单、全段对话排版、自定义图片替换、缺图回退、行驶隐藏和重开。

`Assets/Resources/NightVoyage/` 下UTF-8 CSV在开局和重开时读取，保留列名与ID：

- `Stops.csv`：六处地点名称、招牌、人物开场白、地图位置、停放载具、维修权限。
- `Parcels.csv`：12条货物的名称、兼容重量/酬金、标签（含 `heat_sensitive`）、目标回合 `target_turns`、`origin` / `destination`。
- `Shift.csv`：旧夜间预算文件（兼容保留，当前不读取、不参与通关）。
- `Vehicles.csv`：三辆载具的名称、能力、兼容容量、使用回合上限、速度与兼容损耗。
- `Events.csv`：12个道路事件文案、效果与代价；包含分糖、骷髅狗、自拍少年、小蝙蝠、游行、间歇泉、浓雾、手电筒、泥石流、强风、硫磺蒸汽与花车。

默认订单、节点交互与结算规则位于 `NightCourierModel.cs`；回合移动、环境保护和节点事件位于 `CourierRoadEffects.cs`。事件 `effect` 只配置 Wait / Retreat，旧 Detour 枚举与风险字段仅兼容保留。实际延误固定+1订单回合，倒退免费回到最后一条边的前一节点；触发点、概率、每单上限见顶部。编辑CSV/JSON后重开生效。

原生UI面板位于 `Assets/Resources/Panel/Courier/`，单件货物卡位于 `Assets/Prefab/UI/CourierCargoRow.prefab`。`Tools > Night Courier > Rebuild UI Prefabs (overwrite layouts)` 会覆盖布局，不覆盖已有CSV。

## 地图主界面与行驶控制

地图页的 `Window` 现在四边贴齐画面，路线网络与站点按可用尺寸重新排布；右侧保留货物和载具操作，底部为路线代价与对话留出空间。道路、站点按钮和行驶光点使用同一套尺寸换算，扩大地图后仍可直接点击站点名称。

地图优先使用全宽画面，不再为主角立绘或固定货物侧栏预留空间。载具、剩余使用回合、当前订单回合与 Cargo 按钮集中到顶部；地图下方集中显示目的地、路线代价、风险与出发按钮。Ghost / Goolie 立绘保持已删除状态，安装排版和重建 UI 都不会重新加入。

按 **Tab** 或点击 **Cargo** 展开底部货物抽屉，分为 **Pick Up / On Board** 两栏；点击 Close 或再按 Tab 收起。抽屉覆盖地图底部，不会改变路网位置。点击或拖拽领取整批订单，顶部显示订单回合；行驶中可查看，停靠且没有活动订单时才能接单。M 也可打开货物面板。首次教学装好磁带后会收起抽屉，露出选路目标。

角色交谈时保留 190 高度与 30 号正文；非对话时隐藏底部提示条、文字与对话箭头，地图使用释放的空间。当前站点旁显示可点击的 Talk [E]；有未完成行程的中途站点显示 Continue to ... [E]。行驶和幽灵运输提示跟随下一站节点，事件与结算期间隐藏。跳过行驶按钮移至顶部。提示按站名位置选择上下方，并限制在地图边界内。货物抽屉与路线摘要只在实际对话时为对话框留位。

`Tools > Night Courier > Arrange Map Page (Map and Cargo Drawer)` 安装当前排版。`CourierMapPageInstaller.InstallAndCheck` 检查地图全宽、立绘移除状态、货物双栏、原生展开/装货/收起/出发点击、暂停和重开，并导出实际画面。

参考取舍：Mini Motorways 的地图优先与边缘工具布局、FTL 的节点路线表达，以及 SCS 的小型状态组件与按需详情思路。参考页面：[Mini Motorways](https://dinopoloclub.com/games/mini-motorways/)、[FTL](https://store.steampowered.com/app/212680/FTL_Faster_Than_Light/)、[SCS Route Advisor 设计说明](https://blog.scssoft.com/2026/02/the-new-route-advisor-why-and-how-its.html)。

停靠、地图、行驶与事件都有暂停入口；暂停会冻结计时、地图光点和路段进度，恢复后保留未处理的事件。沿用共享按钮回馈，保留窗口0.18秒滑入，并在地图连线上显示移动光点、已走路段与进度。街景、车辆横版演出及下车走动已从当前玩法画面移除。界面按压、悬停与菜单过渡仍可在暂停时播放。

`Tools > Night Courier > Use Map-only Presentation (preserve layout)` 安装地图常驻界面并移除街景对象。

`Tools > Night Courier > Add Travel Controls (preserve layouts)` 只为已有面板添加控制，不重建现有布局；完整重建面板时也会自动安装。

## 独立教学面板

教学资源现在是 `Assets/Resources/Panel/Courier/CourierTutorialPanel.prefab`，通过 `UIManager` 注册与打开。HUD、地图、事件和结算面板均不再嵌入教学实例，旧的 `Assets/Prefab/UI/CourierTutorial.prefab` 已迁移并保留资源 GUID。

`NightCourierPrototype` 在教学需要显示时创建唯一面板；`CourierTutorialView` 根据当前步骤查找 HUD、地图或事件控件，在最上层更新提示和高亮框。装货、站点选择、出发与事件按钮可直接点击，只有 Skip / Done 按钮接收教学自身的点击。暂停时隐藏教学，恢复后继续；完成、跳过或游戏结束时关闭。沿用此前的单局会话行为：已跳过的教学在重开班次时不会重新出现。

`Tools > Night Courier > Install Fullscreen Map and Tutorial Panel` 用于迁移已有布局，完整重建 UI 也会执行。`CourierPresentationInstaller.InstallAndCheck` 检查全屏尺寸、道路与站点对齐、唯一教学实例、跨面板高亮、原生点击、暂停、跳过、到站与重开。

## 检查入口

- `Tools > Night Courier > Run Rule Checks`：2267项断言，覆盖六节点互达、十项关系、单批次接单、整数回合、软性评价、幽灵运输、不同帧率与快进、完整配送和全部交付通关。
- `CourierPrefabBuilder.InstallTravelAndRun`：11步行驶控制检查，包含原生按钮点选、地图/事件暂停、跳过成本、中途停靠与幽灵兜底。
- `NightCourierPanelSmokeTest.Run`：新版13步场景检查，覆盖接单、地图选路、中途停靠、暂停、事件、交付、无全局截止、全部订单评价、通关和重开。
- `CourierTextJuiceSmokeTest.Run`：对话字幕动画检查，覆盖补全、箭头推进、暂停、自然落定、富文本和减少动态效果。
- `GameFlowSmokeTest.Run`：通用菜单与场景流程检查。

## 按钮提示与可复用反馈 Prefab

交互提示现在独立为两个资源，可直接在 Unity 中打开编辑：

- `Assets/Resources/Feedback/CourierHint.prefab`：提示背景、描边、字体、字号、内边距、最大宽度，以及 `hoverDelay`（默认0.22秒）和 `fadeDuration`（0.12秒）。鼠标短暂停留后淡入，键盘选中立即显示；条件变化时原位更新文案并重新排版。
- `Assets/Resources/Feedback/CourierInteractionEffect.prefab`：四边亮框、点击脉冲、可用/不可用颜色和播放时长。`reducedMotion` 可取消缩放与过渡，保留状态提示。效果使用独立子物体，不改变按钮布局和点击范围，也不拦截鼠标。

`CourierButtonFeedback` 挂在按钮上，自动播放悬停、键盘选中、按压和点击反馈；也可通过 UnityEvent 调用它的 `Play()`。`effectPrefab` 可替换成另一个带 `CourierInteractionEffect` 的 Prefab；不指定时加载共享资源。动画使用不受游戏暂停影响的时间。

`CourierIconHint.message` 是具体提示文案，同一组件支持鼠标和键盘。不可点击按钮仍可用鼠标查看具体条件：例如必须先交付当前订单、必须到供应节点取货、必须到H/W/Q换车。事件按钮提示显示延误或倒退一个节点的实际效果。

`Tools > Night Courier > Install Interaction Feedback (preserve layouts)` 为现有面板和货物卡接入提示，不重建布局；已有的两个反馈 Prefab 不会被覆盖，便于保留手工调整。完整重建面板也会自动执行此接入。

检查入口 `CourierFeedbackPrefabBuilder.InstallAndCheck` 生成资源并运行 `CourierFeedbackSmokeTest`，验证鼠标/键盘、不可用条件、文字排版、屏幕边界、暂停动画、射线穿透与关闭清理。

此前的交互反馈检查结果属于界面更新记录；本次核心流程验证结果以本次运行日志为准。

## 对话字幕动画

HUD 对话使用 `CourierTextJuice` 逐字淡入、轻微上浮与弹性落定。整句文字一次排版，播放时只变更字符顶点，换行不会随逐字播放跳动；行驶状态提示直接显示。按 E 或对话箭头先补全当前句，再次操作进入下一句，游戏暂停时动画同步暂停。

可在 HUD 的字幕 Text 上添加 `CourierTextJuice` 调整 `charactersPerSecond`（默认42）、`settleDuration`（0.24秒）、`rise`（8）和 `pop`（0.16）；未添加时 HUD 自动挂载默认组件。`reducedMotion` 开启后直接显示完整文字。

## 货物图标（SVG 源文件）

10件货物使用同一套256×256 SVG设计：深色圆角描边，薄荷绿、暖金、奶油白与淡紫配色，各图透明背景。源文件位于 `Assets/Art/Sprite/Cargo/Sources/`；游戏内使用对应的384×384透明PNG，位于 `Assets/Resources/NightVoyage/CargoIcons/`。总览在 `ArtPreviews/cargo-icons.svg` 和 `ArtPreviews/cargo-icons.png`，总览自身的深色底仅用于预览。

`Assets/Resources/NightVoyage/CargoIcons.asset` 按 `Parcels.csv` 的稳定ID绑定10张Sprite，可在Inspector中替换。货物卡片左侧显示图标；散热装置和血液的合批卡同时显示两张，拖拽时也保留货物图标。图片不拦截点击，文字按可用宽度换行并自动增加卡片高度。缺少图片时仍保留名称和配送信息。

`Tools > Night Courier > Install Cargo Artwork (preserve layouts)` 将图标接入现有货物卡片，保留地图与抽屉布局；完整重建货物卡片也会接入。`CourierCargoArtInstaller.InstallAndCheck` 验证10个资源映射、合批双图、拖拽图像、文字排版、暂停、交付和重开。

重新生成：`python3 Tools/Art/generate_cargo_icons.py`（需要Node.js和sharp；可通过 `CARGO_SHARP_MODULE` 指定sharp模块）。只生成SVG可加 `--svg-only`，无需图片渲染依赖。生成后运行上述安装入口更新Unity导入设置。

## 载具图标

滑板车、单车、旧皮卡使用与货物相同的透明SVG风格，源文件位于 `Assets/Art/Sprite/Vehicles/Sources/`，运行时384×384透明Sprite位于 `Assets/Resources/NightVoyage/VehicleIcons/`。`VehicleIcons.asset` 按 `Vehicles.csv` 的0/1/2 ID映射图片，可在Inspector替换。

地图顶部显示当前载具，换车按钮显示可用载具；旧皮卡未解锁时保留锁定图示。耗尽切换幽灵时隐藏车辆图片并显示幽灵标识。既有载具详情面板也接入当前和停放载具图片，所有图片均不拦截点击。

`Tools > Night Courier > Install Vehicle Artwork (preserve layouts)` 为已有面板接入图标；完整重建地图面板也会接入。`CourierVehicleArtInstaller.InstallAndCheck` 验证三种映射、换车、锁定、详情、暂停、幽灵兜底和重开。

## 地图占位美术接入

当前地图使用 `Assets/Resources/NightVoyage/MapArtwork.asset` 配置纯地形底图与六个地标，可直接在Inspector中替换为手绘Sprite，按Stop Id绑定。地形随地图容器伸展，地标保持比例并使用统一脚底锚点；对话、订单抽屉和不同地图尺寸共用原节点坐标。完整重建地图UI也会接入美术层。

道路和路线高亮由原 `CourierRouteGraphic` 绘制；移动进度、光点及呼吸光晕在单独的 `TravelOverlay` 绘制，保持可见与暂停行为。地标图像的点击区域挂在原节点按钮上，名称和节点标记仍可点击。

`Tools > Night Courier > Install Map Artwork (preserve layouts)` 为现有面板安装，`CourierMapArtworkInstaller.InstallAndCheck` 验证接入。替换步骤、Anchor/Scale/Offset与分层规范见 `Assets/Art/MapPlaceholder/README_地图重绘与动效.md`。

地图移动标记现复用 `VehicleIcons.asset` 的当前载具图片；途中耗尽后切换同款幽灵标识。原路线高亮、行驶进度与呼吸光晕保留，暂停时图标和光晕同时冻结，到站后交通工具图标留在当前节点，行驶光晕停止。`RoadMap / CourierMapArtwork` 的 `Traveler Size` 可调整图标大小。`CourierMapTravelerSmokeTest.InstallAndCheck` 验证三种车型、途中切换、暂停、到站、重开与缺图兜底。

## 剧情文案配置

`Assets/Resources/NightVoyage/Story.json` 按本次提供的《文案》配置英文译文，每句保留 `sourceChinese` 原文。包含 Gooy 的信与 Goolie 开场，以及六名角色的寄件和收件剧情，共16段64句。背景资料也保存在该文件。`language` 当前为 `en`；中文原文仅用于核对，现有字体仍按英文界面使用。

首次对话先播放站长的信，随后进入 Drow 寄件对白。未寄出的货物触发对应寄件段落，已交付的货物触发收件段落；收件优先，每段每局只播放一次，重开后重置。最后一单先播放收件对白，再显示结算。发言人随台词切换；Goolie、Gooy 和旁白暂不显示 NPC 头像。

订单表以顶部新版规则和 `Parcels.csv` 为准。已有剧情段落和中文原文保留；未为新增的干燥剂/酒标/杂物与夜宵虚构对白，NPC通用对话及订单列表可正常接单。

`CourierStorySmokeTest.Run` 检查剧情触发、顺序、去重、64句英文排版、最后一单对白与结算，以及重开。

## HUD 手绘素材

当前使用手绘版29个图标与7类界面底板，共36张。运行时透明PNG位于 `Assets/Resources/NightVoyage/HUD/`，完整手绘原稿与切片坐标位于 `Assets/Art/Sprite/HUD/HandDrawn/`。旧版SVG底稿已清理。

替换时覆盖同名PNG并保留 `.meta`，现有Sprite引用和九宫格边界会保留。Unity图集位于 `Assets/Art/Atlases/CourierHUD.spriteatlasv2`；重绘及重新导出方法见 `Assets/Art/Atlases/README_HUD图集.md`。`Tools > Night Courier > Install HUD Artwork (preserve layouts)` 可补齐接入，完整重建也会执行。

## 地图区域与对话优先

地图移除了固定的 ROUTE NETWORK 标题、分隔线和底部目的地摘要，地图扩展到顶部工具区以下的剩余空间。选中异地节点后，在地标旁显示浮动目的地卡：路线回合、载具消耗、道路警告和出发按钮；打开货物抽屉、行驶或对话时隐藏。

地标悬停/键盘焦点边框跟随地标图片实际矩形，旧按钮底板高亮已关闭，名称和地标仍可点击。对话期间地图以 `CourierRoutePanel.dialogueMapAlpha`（默认0.28）显示，并锁定地图、货物、换车和维修操作；暂停与对话推进保留。对话结束后恢复，不改变路网位置或尺寸。`CourierMapPageInstaller.InstallAndCheck` 检查空间利用、浮动提示、地标边框、对话锁定与恢复，以及抽屉、行程、暂停和重开。

## HUD 动效与点击特效

对话底板、头像、姓名和继续按钮以 0.28 秒滑入淡入、0.18 秒滑出淡出；顶部行程信息、暂停和跳过行程按钮随状态平滑切换。退出开始即关闭对应控件的点击，快速反向切换从当前姿态接续，行程进度也会平滑追上实际值。界面过渡使用不受暂停影响的时间，字幕逐字动画仍随游戏暂停。

共享 `CourierButtonFeedback` 新增短促的扩散光圈和四点火花，不需要额外贴图，也不接收射线。沿用 `CourierInteractionEffect.reducedMotion` 可关闭点击特效；HUD 的 `Reduced Motion` 可关闭滑动、进度插值及字幕逐字动画。`CourierHudMotionSmokeTest.Run` 验证显示、退出输入阻断、反向切换、位置稳定、暂停、减少动态效果及点击特效清理。

地图悬停预览：停靠且无对话时，鼠标悬停在非当前节点，会用绿色显示从当前节点出发的完整路线；移开后清除预览，点击选择目的地仍保留原有浮动详情与出发操作。道路不再因与当前节点相邻而自动着色，已走过的路段恢复普通路色。到站后保留当前交通工具标记，途中耗尽时使用幽灵标记；重开恢复起始滑板车。`CourierMapTravelerSmokeTest.Run` 覆盖悬停/移开、当前节点、三种载具、幽灵切换、暂停、到站标记、旧路恢复、重开和缺图兜底。

所有常规弹窗（暂停、设置、确认、制作名单、事件、货物、载具和结算）统一使用向上滑入淡入、向下滑出淡出：内容进入 0.28 秒，退出 0.18 秒，遮罩先淡入。地图主体和教学高亮保持原位并淡化；货物抽屉使用与 HUD 相同的 48 单位滑动距离和进出节奏。关闭立即停止交互与悬浮提示，退出完成后销毁，快速重新打开可从当前姿态接续。`BasePanel` 的 `Reduced Panel Motion` 可停用位移并即时完成过渡；重建快递界面时保留统一的时长配置。`PanelTransitionSmokeTest.Run` 检查 11 类面板的滑动、透明度、输入阻断、关闭打断、重新打开和减少动态效果。

## HUD Sprite Atlas

`Assets/Art/Atlases/CourierHUD.spriteatlasv2` 将36张 HUD 素材自动打包为1024×1024图集，保留独立源图和现有绑定。透明PNG与坐标表位于 `ArtPreviews/hud-atlas.png`、`ArtPreviews/hud-atlas.json`。可用 `Tools > Night Courier > Build HUD Sprite Atlas and Export` 重新导出；重绘与导入说明见 `Assets/Art/Atlases/README_HUD图集.md`。

## V2.4 共享街口地图

运行时路网定义在 `Assets/Scripts/Prototype/CourierStreetMap.cs`，按新图坐标建立 32 个节点：六个角色地点、十二个入口、十个街口及 S/H/W/Q。仅显式节点连通，线条交叉不自动生成街口；C-J 保留普通与积水两条弧线。`Assets/Art/MapPlaceholder/map_layout.json` 是这套节点及连接的核对清单，旧占位底图不再参与显示。

角色地点沿用原 ID（黑暗精灵0、狼人1、小熊精2、吸血鬼3、僵尸4、女巫5），订单、角色配置和剧情绑定不变。新图上的编号为1、2、6、3、4、5。两个入口共用同一角色地点；配送只在抵达角色地点时结算，不把入口或普通街口当作收件人，也不穿过其他角色地点抄近路。

所有32个节点均可选择，六名角色沿用原ID，H/W/Q使用6/7/8，其余入口与街口使用9–31。寻路同时考虑当前回合位置、载具速度、剩余额度与站点提前结束，同回合数按曲线长度择短。实际出发只执行一个移动回合；地图和动画共用曲线数据，事件规则见顶部。

## 装卸货反馈

`CourierCargoFeedback` 观察已经提交的货物状态变化：点击或拖拽装货后，使用对应物品图标沿弧线飞入顶部后备箱标识，带两层淡拖尾和落点光圈；到站自动交付时，从后备箱飞向收件站。批次中的物品逐件错开发出，并显示 `+N on board` / `Delivered ×N` 浮动提示；货物状态和订单计数通过 `CourierValuePulse` 短促弹跳。拖拽装货从松手位置接续，失败操作不播放，实际装货和交付立即生效。

特效位于面板内、滚动裁切区域外，不拦截点击，使用不受暂停影响的时间；关闭面板、重开或启用 `Reduced Panel Motion` 会清理特效并恢复文字缩放。沿用现有到站自动卸货规则。`CourierCargoMotionSmokeTest.Run` 检查点击/拖拽、重复操作、批量、暂停、交付、减少动态效果与清理；`CourierMapPageSmokeTest.Run` 覆盖地图、装货抽屉和行程回归。

### H / W / Q 功能站与路况

以顶部新版系统说明为准：站点换车，不再安装永久保护；高温/积水/颠簸只检查货物标签与当前载具。

## 通用界面手绘素材统一

暂停、确认、通用结算、加载界面、设置滑块、货物抽屉滚动条，以及设置/制作名单的关闭按钮均接入 `NightVoyage/HUD` 手绘素材。所有面板 Prefab 已移除 Unity 内置 Sprite；不可见点击区、遮罩和焦点边线继续使用普通 Graphic。主菜单纸片、设置专用插画和制作名单底图保留其已绘制的独立素材。

`Tools > Night Courier > Install Hand-drawn Shared UI (preserve layouts)` 可为通用控件补齐接入；原 HUD 安装与完整 UI 重建也会执行该步骤。`CourierSharedArtInstaller.InstallAndCheck` 检查所有面板和UI卡片的内置 Sprite、旧独立图标及 HUD 图标绑定。

替换后的14张旧独立图标及其 `.meta` 已清理；仍用于主菜单/设置的 `UI_atlas_1.png`、制作名单底图、所有在用手绘素材和 `HUD/HandDrawn/` 原稿均保留。

## HUD 可读性

顶部载具图标增至92，状态图标48，主要状态文字32–34号，订单按钮280×76、暂停/对话继续按钮84×84；行驶进度与快进位于第二行，停靠时隐藏重复的行程状态。地图从顶部184处开始，为两行工具控件留出间距。目的地卡、出发按钮、站点标签和设施提示同步放大；设施提示会避开角色地标和文字。教学条位于放大后的工具区下方。修复滑板车图片失效的目录绑定，保留现有手绘图片。效果预览：`ArtPreviews/hud-readable.png`。

## 全游戏色卡

`Assets/Resources/NightVoyage/Palette.asset` 统一管理深蓝灰背景、暖纸白文字、薄荷绿操作、暖金目标、高温珊瑚红、积水雾蓝与颠簸沙金。菜单、HUD、弹窗、货物、提示和地图共用配色，保留原始手绘图片。`Tools > Night Courier > Apply Unified Game Palette` 重新应用并验证按钮文字对比度。完整色卡说明：`Assets/Art/GamePalette.md`；预览：`ArtPreviews/game-palette.png`。
