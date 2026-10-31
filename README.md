# 《穿越到DND世界送快递我该怎么办》· 可玩原型

Unity **2022.3.62f3c1**。打开 `Assets/Scenes/MainMenu.unity` 开始，或运行 `Assets/Scenes/GamePlay.unity`。游戏内沿用英文与 Barlow Condensed Light 字体。

## 2026-10-31 核心流程统一

以用户提供的《系统设计》为准：到供应者领取一个订单批次 → 选路并按整数回合移动 → 处理道路事件 → 到接收者自动交付并记录评价 → 空载前往下一单 → 全部11批交付后通关。

- 保留六名角色与12件物资；同一供应者→接收者合为一批，因此为11批。女巫→吸血鬼的维修装置和血液同时领取、同时交付。一次只允许一个活动批次，不能中途取消、拆分或寄存。
- 所有未完成批次可在 Cargo 中查看来源、目的地及目标回合；必须到供应者所在节点才能领取。已完成批次从待接清单移除。
- 订单回合在领取时归零，移动和事件增加整数回合，空载移动和界面等待不计入订单评价。旧240分钟夜间预算不再参与规则，没有天亮提前结算。
- 道路基础成本暂取 max(2, ceil(旧分钟成本/6))，再按载具速度向上取整。行驶动画每8秒推进一个规则回合（默认4倍播放即每现实2秒一回合），F仅跳过动画，到事件或站点停止，不代选事件、不改变成本。
- 载具每移动一个回合消耗一个使用回合，事件可额外消耗；界面显示剩余回合。沿用原载具资源上限作为临时数值，指定节点免费恢复；跨订单保留剩余回合。
- 使用回合耗尽后自动切换幽灵，携带完整批次继续移动；幽灵速度为0.5，不消耗载具资源。重货和容量字段不再限制接单或运输，不再推车、自动维修或判限时件失败。
- 超过目标回合、违反货物运输条件均降低评价，但始终接受交付。每批记录实际/目标回合、是否迟到、货物条件是否违反；地图显示最近评价，通关页汇总11批记录。没有新增最终等级或分支结局。
- 只有全部物资均为 Delivered 才能通关。失败标记、累计时间、六人交谈和抵达酒吧均不能代替全部交付。

目标回合暂为普通批次12、两份60分钟旧限时餐点10、90分钟旧夜宵15；同批按最严格目标。道路、目标回合与载具资源上限仍需真人调优。载具解锁内容保留；固定事件、三类环境适配及旧车消失等其余系统不在本次核心流程调整范围内。

地图、暂停、教学、头像和货物抽屉沿用现有界面。Tab / Cargo 展开订单，点击或拖拽领取整批；到站自动交付，E交谈或继续行程，F快进到下一个事件或站点。

## 可编辑配置

`Assets/Resources/NightVoyage/Characters.asset` 是角色配置。在 Unity 中选中它，展开对应角色，将导入为 **Sprite (2D and UI)** 的图片拖入 `Portrait` 即可替换头像；也可修改 `Display Name`、`Personality` 和 `Accent`。`Stop Id` 对应下表，数组顺序不影响角色绑定。姓名用于英文游戏界面，中文性格字段供策划记录，不自动生成台词。开场白仍在 `Stops.csv`，追加对话在 `NightCourierModel.cs`。

| Stop Id | 角色 | 截图中的性格 | 占位头像 |
| --- | --- | --- | --- |
| 0 | 黑暗精灵 / Drow | 内向嘴拙妹 | drow.png |
| 1 | 狼人 / Wolfwoman | 豪爽大姐姐 | werewolf.png |
| 2 | 小熊精 / Little Bear | 嘟嘟囔囔、多愁善感的小妹妹 | bear.png |
| 3 | 吸血鬼 / Vampire | 骑士语 C 姐，原型按骑士角色扮演口吻处理 | vampire.png |
| 4 | 移民僵尸 / Granny Zombie | 呆萌老太 | zombie.png |
| 5 | 女巫 / Witch | 控场的妈妈 | witch.png |

六张 384×384 头像和通用缺图头像位于 `Assets/Art/Characters/Placeholders/`，可编辑矢量源文件在其 `Sources/` 子目录。缺失头像使用配置中的 `Fallback Portrait`。头像与姓名只在角色对话时显示，行驶及普通站点提示恢复原有字幕宽度；支持暂停、跳过文字与重开。

已对照用户提供的六人供需表检查全部十二件物资：角色与收发方向一致。干燥剂与酒标合为一件；维修装置与血液分为两件；狼人从吸血鬼、僵尸和女巫三处获得食物。保留既有派对磁带配送。角色表没有给出重量、酬金、时限，这些仍是原型数值。

`Tools > Night Courier > Install Character Portraits (preserve settings)` 可为已有 HUD 安装头像区域，保留已有角色配置和图片；完整重建 UI 也会安装。`CourierCharacterInstaller.InstallAndCheck` 检查六人绑定、十二件物资与十一批订单、全段对话排版、自定义图片替换、缺图回退、行驶隐藏和重开。

`Assets/Resources/NightVoyage/` 下UTF-8 CSV在开局和重开时读取，保留列名与ID：

- `Stops.csv`：六处地点名称、招牌、人物开场白、地图位置、停放载具、维修权限。
- `Parcels.csv`：十二件物资的名称、兼容重量、酬金、标签、目标回合 `target_turns`，以及新增的 `origin` / `destination` 节点ID。
- `Shift.csv`：旧夜间预算文件（兼容保留，当前不读取、不参与通关）。
- `Vehicles.csv`：三辆载具的名称、能力、兼容容量、使用回合上限、速度与兼容损耗。
- `Events.csv`：12个道路事件文案、效果与代价；包含分糖、骷髅狗、自拍少年、小蝙蝠、游行、间歇泉、浓雾、手电筒、泥石流、强风、硫磺蒸汽与花车。

物资数量及默认内容、换车解锁、节点追加对话、结算规则在 `NightCourierModel.cs`。事件 `effect` 为 Wait / Detour / Retreat：等待按 `safe_turns` 增加订单回合（默认1，不耗载具）；绕路为本路段局部便道，增加 `safe_turns` 个实际移动回合（暂定2，正常耗载具）；倒退沿原路回到本段出发节点，按当前速度计移动回合，保留订单和原目的地，可重新选路或继续。每段道路中途等概率抽取一个事件，返程不再触发；事件统一为单个确认按钮。旧风险字段仅兼容保留，不再参与这些事件结算。编辑CSV后重开生效。

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

- `Tools > Night Courier > Run Rule Checks`：2853项断言，覆盖六节点互达、十二项关系、单批次接单、整数回合、软性评价、幽灵运输、不同帧率与快进、完整配送和全部交付通关。
- `CourierPrefabBuilder.InstallTravelAndRun`：11步行驶控制检查，包含原生按钮点选、地图/事件暂停、跳过成本、中途停靠与幽灵兜底。
- `NightCourierPanelSmokeTest.Run`：新版13步场景检查，覆盖接单、地图选路、中途停靠、暂停、事件、交付、无全局截止、全部订单评价、通关和重开。
- `CourierTextJuiceSmokeTest.Run`：对话字幕动画检查，覆盖补全、箭头推进、暂停、自然落定、富文本和减少动态效果。
- `GameFlowSmokeTest.Run`：通用菜单与场景流程检查。

## 按钮提示与可复用反馈 Prefab

交互提示现在独立为两个资源，可直接在 Unity 中打开编辑：

- `Assets/Resources/Feedback/CourierHint.prefab`：提示背景、描边、字体、字号、内边距、最大宽度，以及 `hoverDelay`（默认0.22秒）和 `fadeDuration`（0.12秒）。鼠标短暂停留后淡入，键盘选中立即显示；条件变化时原位更新文案并重新排版。
- `Assets/Resources/Feedback/CourierInteractionEffect.prefab`：四边亮框、点击脉冲、可用/不可用颜色和播放时长。`reducedMotion` 可取消缩放与过渡，保留状态提示。效果使用独立子物体，不改变按钮布局和点击范围，也不拦截鼠标。

`CourierButtonFeedback` 挂在按钮上，自动播放悬停、键盘选中、按压和点击反馈；也可通过 UnityEvent 调用它的 `Play()`。`effectPrefab` 可替换成另一个带 `CourierInteractionEffect` 的 Prefab；不指定时加载共享资源。动画使用不受游戏暂停影响的时间。

`CourierIconHint.message` 是具体提示文案，同一组件支持鼠标和键盘。不可点击按钮仍可用鼠标查看具体条件：例如必须先交付当前订单、必须到供应节点取货、旧皮卡需要从酒吧送来维修装置。事件按钮提示会显示等待、绕路或返回节点的实际效果。

`Tools > Night Courier > Install Interaction Feedback (preserve layouts)` 为现有面板和货物卡接入提示，不重建布局；已有的两个反馈 Prefab 不会被覆盖，便于保留手工调整。完整重建面板也会自动执行此接入。

检查入口 `CourierFeedbackPrefabBuilder.InstallAndCheck` 生成资源并运行 `CourierFeedbackSmokeTest`，验证鼠标/键盘、不可用条件、文字排版、屏幕边界、暂停动画、射线穿透与关闭清理。

此前的交互反馈检查结果属于界面更新记录；本次核心流程验证结果以本次运行日志为准。

## 对话字幕动画

HUD 对话使用 `CourierTextJuice` 逐字淡入、轻微上浮与弹性落定。整句文字一次排版，播放时只变更字符顶点，换行不会随逐字播放跳动；行驶状态提示直接显示。按 E 或对话箭头先补全当前句，再次操作进入下一句，游戏暂停时动画同步暂停。

可在 HUD 的字幕 Text 上添加 `CourierTextJuice` 调整 `charactersPerSecond`（默认42）、`settleDuration`（0.24秒）、`rise`（8）和 `pop`（0.16）；未添加时 HUD 自动挂载默认组件。`reducedMotion` 开启后直接显示完整文字。

## 货物图标（SVG 源文件）

12件货物使用同一套256×256 SVG设计：深色圆角描边，薄荷绿、暖金、奶油白与淡紫配色，各图透明背景。源文件位于 `Assets/Art/Sprite/Cargo/Sources/`；游戏内使用对应的384×384透明PNG，位于 `Assets/Resources/NightVoyage/CargoIcons/`。总览在 `ArtPreviews/cargo-icons.svg` 和 `ArtPreviews/cargo-icons.png`，总览自身的深色底仅用于预览。

`Assets/Resources/NightVoyage/CargoIcons.asset` 按 `Parcels.csv` 的稳定ID绑定12张Sprite，可在Inspector中替换。货物卡片左侧显示图标；维修装置和血液的合批卡同时显示两张，拖拽时也保留货物图标。图片不拦截点击，文字按可用宽度换行并自动增加卡片高度。缺少图片时仍保留名称和配送信息。

`Tools > Night Courier > Install Cargo Artwork (preserve layouts)` 将图标接入现有货物卡片，保留地图与抽屉布局；完整重建货物卡片也会接入。`CourierCargoArtInstaller.InstallAndCheck` 验证12个资源映射、合批双图、拖拽图像、文字排版、暂停、交付和重开。

重新生成：`python3 Tools/Art/generate_cargo_icons.py`（需要Node.js和sharp；可通过 `CARGO_SHARP_MODULE` 指定sharp模块）。只生成SVG可加 `--svg-only`，无需图片渲染依赖。生成后运行上述安装入口更新Unity导入设置。

## 载具图标

滑板车、单车、旧皮卡使用与货物相同的透明SVG风格，源文件位于 `Assets/Art/Sprite/Vehicles/Sources/`，运行时384×384透明Sprite位于 `Assets/Resources/NightVoyage/VehicleIcons/`。`VehicleIcons.asset` 按 `Vehicles.csv` 的0/1/2 ID映射图片，可在Inspector替换。

地图顶部显示当前载具，换车按钮显示可用载具；旧皮卡未解锁时保留锁定图示。耗尽切换幽灵时隐藏车辆图片并显示幽灵标识。既有载具详情面板也接入当前和停放载具图片，所有图片均不拦截点击。

`Tools > Night Courier > Install Vehicle Artwork (preserve layouts)` 为已有面板接入图标；完整重建地图面板也会接入。`CourierVehicleArtInstaller.InstallAndCheck` 验证三种映射、换车、锁定、详情、暂停、幽灵兜底和重开。

## 地图占位美术接入

当前地图使用 `Assets/Resources/NightVoyage/MapArtwork.asset` 配置纯地形底图与六个地标，可直接在Inspector中替换为手绘Sprite，按Stop Id绑定。地形随地图容器伸展，地标保持比例并使用统一脚底锚点；对话、订单抽屉和不同地图尺寸共用原节点坐标。完整重建地图UI也会接入美术层。

道路和路线高亮由原 `CourierRouteGraphic` 绘制；移动进度、光点及呼吸光晕在单独的 `TravelOverlay` 绘制，保持可见与暂停行为。地标图像的点击区域挂在原节点按钮上，名称和节点标记仍可点击。

`Tools > Night Courier > Install Map Artwork (preserve layouts)` 为现有面板安装，`CourierMapArtworkInstaller.InstallAndCheck` 验证接入。替换步骤、Anchor/Scale/Offset与分层规范见 `Assets/Art/MapPlaceholder/README_地图重绘与动效.md`。

地图移动标记现复用 `VehicleIcons.asset` 的当前载具图片；途中耗尽后切换同款幽灵标识。原路线高亮、行驶进度与呼吸光晕保留，暂停时图标和光晕同时冻结，到站隐藏。`RoadMap / CourierMapArtwork` 的 `Traveler Size` 可调整图标大小。`CourierMapTravelerSmokeTest.InstallAndCheck` 验证三种车型、途中切换、暂停、到站、重开与缺图兜底。
