# 音频系统

围绕当前项目的真实音频实现，解释 `AudioCue → AudioManager → AudioSource → AudioListener` 的完整数据流。

**不参与 Unity 编译。** 浏览器打开 [index.html](./index.html)。

## 学习标记

该模块为 **协助实现 · 重点掌握**。面试前应能独立说明：

- 为什么业务层传 `AudioCue`，而不是传文件名或 `AudioClip`
- `DontDestroyOnLoad` 单例怎样防止重复创建
- 双音乐声源怎样用 `unscaledDeltaTime` 交叉淡化
- 为什么短音效需要声源池，以及池满时怎样降级
- Master / Music / SFX 音量如何应用并通过 `PlayerPrefs` 持久化
- CC0 素材如何记录来源、许可与哈希

## 关联源码

- `Assets/Scripts/Audio/AudioCue.cs`
- `Assets/Scripts/Audio/AudioManager.cs`
- `Assets/Scripts/CombatFlowController.cs`
- `Assets/Scripts/Entity/Entity_Combat.cs`
- `Assets/Editor/CombatPlayModeSmokeRunner.cs`
- `Assets/Audio/README.md`
