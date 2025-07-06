# ikerEcsProject
# 猫狗模拟游戏 / Cat & Dog Simulator

## 项目概述 / Project Overview
一个简单的猫狗互动模拟游戏，包含两个版本：
A simple cat-dog interaction simulation with two versions:
- **标准版**：基于GameObject的传统实现
  **Standard**: Traditional GameObject-based
- **ECS版**：高性能ECS实现
  **ECS**: High-performance Entity Component System

## 环境要求 / Requirements
- **Unity版本**：6000.0.33f1
  **Unity Version**: 6000.0.33f1

## 运行方式 / How to Run
1. 用Unity 6000.0.33f1打开项目
   Open the project in Unity 6000.0.33f1
2. **从标题场景开始运行**：`Assets/Scenes/TitleScene.unity`
   **Start from title scene**: `Assets/Scenes/TitleScene.unity`
3. 在标题界面选择`Standard`或`ECS`版本
   Choose `Standard` or `ECS` version in title screen

## 游戏规则 / Game Rules
- 🐱 猫会随机移动并拉屎
  Cats wander around and poop
- 🐶 狗会找到并吃掉猫屎
  Dogs find and eat cat poop
- 🌀 狗吃够一定数量的屎后会分裂（复制一只）
  Dogs split (duplicate) after eating enough poops