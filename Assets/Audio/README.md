# No Spooky, Just Parcels — 音效

音效由 `CourierSoundscape` 自动加载，路由至现有混音器Sfx/BGM组，跟随主音量和分类音量。暂停冻结播放与待播队列，重开清除上一局提示。

## 配置与替换

- 原有交互音效位于 `Assets/Resources/Audio/Courier/`，覆盖同名文件并保留 `.meta` 即可替换。
- `Assets/Resources/NightVoyage/AudioCues.json` 配置 `cue`、`path`（相对Resources、无扩展名）和 `gain`（0–1），也可覆盖原有音效键。
- `Events.csv` 的 `sound_cue` 为每个事件指定音效；可让多个事件复用同一个键。修改后重开本局生效。
- 已配置的音效缺失时会警告；未识别的事件音效回退到通用encounter提示。

| 音效键 | 用途 |
| --- | --- |
| `event_candy` / `event_dog` | 分糖、骷髅狗 |
| `event_camera` / `event_bat` | 自拍少年、小蝙蝠 |
| `event_parade` / `event_hunters` | 巡游与花车、抓鬼探险 |
| `event_steam` / `event_wind` / `event_slide` | 热泉与蒸汽、风雾、冻土滑坡 |
| `event_wait` / `event_retreat` | 确认延误、确认倒退 |
| `exhausted` | 载具额度耗尽 |
| `possession` | 换车，包括同车型换新车；主动切回幽灵 |
| `cargo` / `departure` / `arrival` | 接单、出发、抵达普通节点 |
| `damage` / `delivered` / `ending` | 货损、交付、全部完成 |
| `click` / `hover` / `dialogue` | 按钮点击、悬停、推进对话 |

新的12个短音效位于 `Assets/Resources/Audio/Courier/Events/`，为本项目程序合成的风格化提示音。可通过 `python3 Tools/Audio/generate_event_cues.py` 重建，无外部依赖；适合先验证反馈节奏，也可替换为正式录音。旧repair资源兼容保留，当前玩法不触发维修音。

音效读取已提交的游戏动作，不依赖帧末状态猜测，因此快进也不会漏掉出发、到站、事件或货损。同一批次货损去重，其余提示按动作顺序间隔播放，交付与货损、最后交付与结束均保留。

## 循环与授权

`Loops/` 内的night_waltz、night_wind、vehicle_engine分别用于音乐、夜风与行驶声。H/W/Q共用引擎录音，通过音调与音量区分；幽灵关闭引擎。`NightCourierPrototype.radioMusic` 可覆盖默认音乐。

原有交互提示来自Kenney CC0音效包；音乐为Wolfgang_的Skeleton Waltz Theme / Bone Yard Waltz（CC BY 4.0），引擎录音来自qubodup / Iwan Gabovitch（CC BY 3.0），夜风来自Luke.RUSTLTD（CC0）。原始来源与修改说明保留在 `Assets/Audio/Licenses/`。发布时保留游戏设置页署名、Resources与StreamingAssets中的AudioCredits文本。

验证：`Tools > Night Courier > Checks > Audio (keep editor open)`。测试恢复音量与原场景，不退出编辑器。
