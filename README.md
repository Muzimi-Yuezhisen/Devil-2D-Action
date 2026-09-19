# Devil-2D-Action

使用 Unity 6000.0.77f1 开发的 2D 横版动作游戏原型。当前版本是一段可游玩的战斗演示关卡，并非完整商业游戏。

## 已实现

- 玩家有限状态机：移动、跳跃、墙跳、冲刺、三段普攻、弹反与技能施放。
- 三类敌人及分层关卡遭遇；物理与元素伤害、异常状态、受击反馈和地刺伤害。
- 以 ScriptableObject 配置属性与技能；击败敌人、开启宝箱和拾取 Buff 可参与技能点与技能树循环。
- 主菜单、音频、视差背景，以及暂停、胜利、失败和重开流程。
- 编辑器 Play Mode 冒烟测试，覆盖菜单进入、玩家、敌人、战斗、属性、技能、技能树、场景摄像机、音频与胜负闭环。

## 运行方式

1. 使用 Unity Hub 以 **Unity 6000.0.77f1** 打开仓库根目录。
2. 打开 `Assets/Scenes/MainMenu.unity`，点击 Play。
3. 在主菜单选择“开始游戏”进入演示关卡。

主要操作：`A/D` 移动，`Space` 跳跃，鼠标左键普攻，`Shift` 冲刺，`Q` 弹反，`F` 使用碎片技能，`L` 打开技能树，`Esc` 暂停或关闭面板，`M` 静音。

## 回归验证

在 Unity 编辑器菜单运行 `Tools > Devil > Run Combat Smoke Test`。成功时控制台输出 `[CombatSmoke] PASSED`。批处理方法为 `CombatPlayModeSmokeRunner.Run`。

## 素材来源

第三方敌人素材的来源与许可证见 `Assets/ThirdParty/OpenDuelyst/SOURCE.md`；音频来源与许可证见 `Assets/Audio/README.md`；中文字体许可证见 `Assets/Resources/Fonts/LICENSE.txt`。
