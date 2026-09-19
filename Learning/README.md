# Devil 项目学习笔记

本文件夹用于梳理和理解项目逻辑，**不参与 Unity 编译**。分类与课程 / `Assets/struct.pdf` 一致。

## 模块索引（按学习顺序）

| # | 模块 | 学习网页 | 状态 |
|---|------|----------|------|
| 1 | 有限状态机 | [index.html](./有限状态机/index.html) | ✅ |
| 2 | 玩家状态机 | [index.html](./玩家状态机/index.html) | ✅ |
| 3 | 关卡和摄像机设置 | [index.html](./关卡和摄像机设置/index.html) | ✅ |
| 4 | 敌人状态机 | [index.html](./敌人状态机/index.html) | ✅ |
| 5 | 战斗系统 | [index.html](./战斗系统/index.html) | ✅ 已同步 AttackData |
| 6 | 属性系统 | [index.html](./属性系统/index.html) | ✅ 已同步 GetAttackData |
| 7 | 技能树系统与UI | [index.html](./技能树系统与UI/index.html) | ✅ 已同步 UpgradeData |
| 8 | 核心技能系统 | [index.html](./核心技能系统/index.html) | ✅ 完整重写（Dash/Shard） |
| 9 | 音频系统 | [index.html](./音频系统/index.html) | ✅ 协助实现 · 重点掌握 |
| 10 | 中文本地化与字体 | [设计决策与面试](./设计决策与面试/index.html#g-localization) | ✅ 协助实现 · 重点掌握 |

## 串联理解（独立栏目）

| 栏目 | 学习网页 | 说明 |
|------|----------|------|
| 数据流动与场景 | [index.html](./数据流动与场景/index.html) | 进游戏前配置 · 按「发生的一件事」串调用链与数据流 · 13 场景 |
| 核心战斗闭环与稳定性 | [index.html](./核心战斗闭环与稳定性/index.html) | 战斗启动到胜负重开 · 稳定性修复 · 自动测试 · 面试训练 |

## 面试向（独立栏目）

| 栏目 | 学习网页 | 说明 |
|------|----------|------|
| 设计决策与面试 | [index.html](./设计决策与面试/index.html) | 理解「为什么这样设计」· 对比 · 演进（非背诵稿） |

## 工程工具（独立栏目）

| 栏目 | 学习网页 | 说明 |
|------|----------|------|
| Git 版本管理 | [index.html](./Git版本管理/index.html) | commit / 分支 / GitHub · 结合本仓库 · 简历表述 |

**浏览器入口：** [index.html](./index.html)（课程模块 / 串联理解 / 面试向 / 工程工具）

## 参考

- 作者框架图：[`Assets/struct.pdf`](../Assets/struct.pdf)
- 源码根目录：`Assets/Scripts/`
- 共享样式：`Learning/_assets/style.css`

## 使用方式

1. 打开 [index.html](./index.html) 或上表链接
2. 课程按 1→9 学实现；音频系统为本轮协助实现内容，需要重点掌握
3. 想复盘本次战斗闭环、稳定性修复并练面试追问时看 **核心战斗闭环与稳定性**
4. 想弄清「为什么」时看 **设计决策**
5. 工程协作：看 **Git 版本管理**
6. 对照 Unity Hierarchy / Inspector 与源码阅读
