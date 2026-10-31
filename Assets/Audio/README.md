# Night Courier 音效

12个短音效取自 Kenney 的 CC0 音效包，可以用于商业项目；原始授权保存在 `Licenses/`。游戏运行时自动加载 `Assets/Resources/Audio/Courier/`，不需要重建场景或 Prefab。替换同名 OGG 即可换音色，保留对应 `.meta`。

来源：
- Interface Sounds：https://kenney.nl/assets/interface-sounds
- RPG Audio：https://kenney.nl/assets/rpg-audio
- 下载镜像：https://opengameart.org/sites/default/files/kenney_interfaceSounds.zip
- 下载镜像：https://opengameart.org/sites/default/files/RPGsounds_Kenney.zip

| 游戏文件 | 原始文件 | 触发 |
|---|---|---|
| click | click_003 | 按钮鼠标点击、键盘确认 |
| hover | tick_001 | 可用按钮悬停、键盘选中；80ms限频 |
| dialogue | pluck_001 | 对话进入下一句，包含E键 |
| cargo | bookPlace2 | 成功接单；点击与拖入均支持 |
| departure | metalLatch | 从站点启程 |
| arrival | confirmation_003 | 未交货的到站 |
| delivered | confirmation_001 | 交付订单 |
| encounter | question_002 | 道路事件出现 |
| damage | error_003 | 新增货损 |
| possession | maximize_003 | 切换载具或幽灵 |
| ending | confirmation_004 | 结局 |
| repair | metalClick | 维修恢复载具耐久 |

`CourierSoundscape` 使用导入的音乐、风声和引擎循环，已移除运行时合成的临时声音。事件音效共享一个 AudioSource，避免每次点击创建播放器；合批货物只播放一次接单/交付提示，同时发生的结局、交付、事件等提示按优先级播放，避免声音堆叠。重开不会重播上一局变化。暂停时停止推进声音并暂停播放，恢复后继续。

音效与背景音乐分别路由到现有 `Main.mixer` 的 Sfx/BGM 组，跟随设置中的主音量和分类音量。没有混音器时读取同一套音量设置作为回退。修正了主音量滑块此前没有更新混音器的问题，以及主音量为零时计算对数产生无穷值的问题。

验证入口：`CourierAudioSmokeTest.Run`。建议在项目副本用 Unity batchmode 调用；检查结束会退出编辑器。覆盖音频导入解码、核心规则、按钮键盘/不可用状态、悬停限频、接单、启程、事件、交付、合批去重、暂停、主音量/分类音量、重开。

## 万圣节夜间配送循环

资源位于 `Assets/Resources/Audio/Courier/Loops/`，自动接入，无需手工拖入场景。`NightCourierPrototype.radioMusic` 仍可覆盖默认音乐。

| 资源 | 作者与授权 | 选择与处理 |
|---|---|---|
| night_waltz.ogg | Wolfgang_，Skeleton Waltz Theme / Bone Yard Waltz，CC BY 4.0 | 原始可循环版本；轻巧诡谲的华尔兹，贴合怪物小镇的夜间配送。音乐保持原始文件，站点对话时压低音量。 |
| night_wind.wav | Luke.RUSTLTD，wind1，CC0 | 作者以 PureData 制作的长段起伏风声；去直流、调整音量、2秒首尾交叉淡化，约58秒循环。站点低声、行驶增强、暴风路段进一步增强。 |
| vehicle_engine.wav | qubodup / Iwan Gabovitch，Car Engine Loop 96kHz, 4s，CC BY 3.0 | 汽车录音的 normalized 版本；混为单声道、去直流、调整音量、50ms交叉淡化，约3.95秒循环。踏板车用更高音调，旧皮卡用更低音调，均由同一录音适配；自行车与幽灵关闭发动机层。 |

音乐与环境声均渐入渐出，避免启程、停车和事件时突然切换。录音处理脚本为 `Tools/Audio/prepare_ambience.py`（依赖 NumPy）。原始下载链接、作者、授权链接和修改说明保存在 `Assets/Audio/Licenses/Night-Ambience.txt`。

CC BY 署名显示在游戏设置页底部，也随构建打包至 `StreamingAssets/AudioCredits.txt`；详细版本位于 `Resources/Audio/Courier/AudioCredits.txt`。对应授权全文保存在 `Assets/Audio/Licenses/`。后续发包请保留署名文件和设置页署名。

音频检查另覆盖3个循环的导入、混音组、载具音调切换、自行车/幽灵无发动机、暴风风声增强，以及署名文本的高度和点击穿透。
