# 核心战斗闭环与稳定性

以本项目真实实现为例，串起「场景启动 → 战斗 → 死亡 → 胜负 → 暂停/重开 → 自动验证」，并解释本轮修复的运行时缺陷。

## 学习重点标记

- **你原有实现 · 常规复习**：原有系统主体，重点复习设计动机与类职责
- **协助实现或修复 · 重点掌握**：本轮补全内容，必须能独立画调用链、解释根因、说明取舍并定位源码

这些标记只用于安排学习优先级，不改变面试页面中的完整项目叙述。

**不参与 Unity 编译。** 浏览器打开 [index.html](./index.html)。

## 学习目标

- 能画出 `CombatFlowController`、`Entity_Health`、玩家/敌人状态机之间的数据流
- 能解释为什么使用实例事件，而不是让生命组件直接操作 UI
- 能解释同一帧多次状态切换、多个 Collider 重复伤害等隐蔽 Bug
- 能说明动画事件的优点、风险和超时兜底边界
- 能讲清 Play Mode 冒烟测试验证了什么、没有验证什么
- 面试时能从「问题、原因、方案、取舍、验证」五步组织回答

## 当前回归范围

每次运行时代码或资源修改后都要执行完整 Demo 冒烟测试。当前自动覆盖：玩家组件与状态、输入和三段连招配置、敌人组件与状态、生命/死亡、属性映射、三类技能、技能树节点与默认解锁、主摄像机/Cinemachine、视差、EventSystem、音频资源、暂停/恢复以及最终击杀胜利。动画观感、打击手感和 UI 遮挡仍需进入 Game View 人工确认。

## 关联源码

- `Assets/Scripts/CombatFlowController.cs`
- `Assets/Scripts/Entity/Entity_Health.cs`
- `Assets/Scripts/Entity/Entity_Combat.cs`
- `Assets/Scripts/StateMachine/StateMachine.cs`
- `Assets/Scripts/Player/PlayerStates/`
- `Assets/Scripts/Enemy/EnemyStates/`
- `Assets/Scripts/SkillSystem/SkillObject_*.cs`
- `Assets/Editor/CombatPlayModeSmokeRunner.cs`

## 推荐学习顺序

1. 先看总架构和胜负数据流
2. 再看状态机与伤害管线的稳定性修复
3. 阅读测试部分，理解如何证明功能真的跑通
4. 最后关闭答案，完成面试题和自测题
