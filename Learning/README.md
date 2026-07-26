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

## 面试向（独立栏目）

| 栏目 | 学习网页 | 说明 |
|------|----------|------|
| 设计决策与面试 | [index.html](./设计决策与面试/index.html) | 理解「为什么这样设计」· 对比 · 演进（非背诵稿） |

## 工程工具（独立栏目）

| 栏目 | 学习网页 | 说明 |
|------|----------|------|
| Git 版本管理 | [index.html](./Git版本管理/index.html) | commit / 分支 / GitHub · 结合本仓库 · 简历表述 |

**浏览器入口：** [index.html](./index.html)（课程模块 / 面试向 / 工程工具）

## 参考

- 作者框架图：[`Assets/struct.pdf`](../Assets/struct.pdf)
- 源码根目录：`Assets/Scripts/`
- 共享样式：`Learning/_assets/style.css`

## 使用方式

1. 打开 [index.html](./index.html) 或上表链接
2. 课程按 1→8 学实现；想弄清「为什么」时看 **设计决策**
3. 工程协作：看 **Git 版本管理**
4. 对照 Unity Hierarchy / Inspector 与源码阅读
