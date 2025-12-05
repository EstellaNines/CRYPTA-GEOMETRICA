# 点阵文本系统使用说明

## 功能特性

- ✅ 5×7 点阵字体显示 A-Z 字母
- ✅ 双行布局（CRYPTA 左上角，GEOMETRICA 右下角）
- ✅ 打字机遍历动画（每个字母从 A 遍历到目标字母）
- ✅ 纯代码控制，无需 Animator
- ✅ **使用 UI Toolkit** 构建现代化 Inspector 界面
- ✅ **中文标签显示**，脚本变量名保持英文
- ✅ 丰富的可视化编辑工具和快速预设
- ✅ 支持自定义颜色、大小、间距

## 快速开始

### 1. 创建点的预制体

1. 在 Hierarchy 中创建 UI → Image
2. 命名为 `DotPrefab`
3. 设置为正方形（如 10×10）
4. 设置颜色为白色（代码会控制颜色）
5. 拖到 Project 窗口创建预制体
6. 删除 Hierarchy 中的临时对象

### 2. 创建点阵文本对象

1. 在 Canvas 下创建空物体，命名为 `DotMatrixText`
2. 添加 `DotMatrixText` 组件
3. 在 Inspector 中设置：
   - **Dot Prefab**: 拖入刚创建的 DotPrefab
   - **Text Line1**: CRYPTA
   - **Text Line2**: GEOMETRICA
   - **Dot Size**: 25
   - **Dot Spacing**: 3
   - **Letter Spacing**: 2

### 3. 调整布局位置

**方式一：使用容器拖拽（推荐）**

1. 点击"生成点阵文本"按钮
2. 在 Hierarchy 中会自动创建 `Line1Container` 和 `Line2Container`
3. 直接在场景中拖拽这两个容器来调整位置
4. 容器位置会实时保存

**方式二：使用初始锚点位置**

- **Line1 Anchor Position**: (-400, 200) - 左上角初始位置
- **Line2 Anchor Position**: (100, -100) - 右下角初始位置
- 点击"重置容器位置"按钮可将容器恢复到这些初始位置

### 4. 配置颜色

- **Dot On Color**: #00d4ff (霓虹蓝)
- **Dot Off Color**: #00d4ff 透明度 0.1

### 5. 配置动画

- **Enable Typewriter Animation**: ✓
- **Traverse Speed**: 0.1 秒/字母
- **Letter Delay**: 0.2 秒

### 6. 生成预览

点击 Inspector 中的 **"生成点阵文本"** 按钮

## UI Toolkit Inspector 界面

### Inspector 面板组织

使用 **UI Toolkit** 构建的现代化 Inspector 界面，所有标签为中文，但脚本变量名保持英文：

#### 【显示设置】
- **第一行文本** (`textLine1`): 输入第一行要显示的文字
- **第二行文本** (`textLine2`): 输入第二行要显示的文字

#### 【点阵设置】
- **点的预制体** (`dotPrefab`): 拖入 Image 预制体
- **点的大小** (`dotSize`): 调整点的像素大小
- **点之间的间距** (`dotSpacing`): 调整点之间的距离
- **字母之间的间距** (`letterSpacing`): 调整字母之间的距离
- **尺寸预设按钮**: 小尺寸(15px) | 中尺寸(25px) | 大尺寸(35px)

#### 【颜色设置】
- **点亮时的颜色** (`dotOnColor`): 点阵显示时的颜色
- **熄灭时的颜色** (`dotOffColor`): 点阵关闭时的颜色（注：熄灭的点会被完全隐藏，此颜色仅用于初始化）
- **颜色预设按钮**: 霓虹蓝 | 霓虹绿 | 橙红

#### 【动画设置】
- **启用打字机动画** (`enableTypewriterAnimation`): 开关动画效果
- **字母遍历速度（秒）** (`traverseSpeed`): 每个字母遍历的速度
- **字母间延迟（秒）** (`letterDelay`): 字母之间的延迟
- **速度预设按钮**: 快速 | 正常 | 慢速

#### 【布局设置】
- **第一行容器** (`line1Container`): 第一行文本的容器 Transform
- **第二行容器** (`line2Container`): 第二行文本的容器 Transform
- **第一行初始位置** (`line1AnchorPosition`): 容器的初始锚点位置
- **第二行初始位置** (`line2AnchorPosition`): 容器的初始锚点位置

#### 【快速操作】
- **生成点阵文本**: 蓝色按钮，生成/重新生成点阵
- **清除点阵文本**: 橙色按钮，清除容器内字符
- **重置容器位置**: 绿色按钮，恢复初始位置
- **重新播放动画**: 黄色按钮，仅运行时可用

#### 【状态信息】
- 实时显示两行文本内容
- 显示点大小和字母间距
- 显示容器创建状态（✓ 已创建 / ✗ 未创建）

### 容器系统

生成点阵文本后，会自动创建两个容器：
- **Line1Container**: 第一行文本的容器
- **Line2Container**: 第二行文本的容器

**优势**：
- 可在场景中直接拖拽调整位置
- 每行文字作为整体移动
- 位置调整更直观方便

## 动画效果说明

### 打字机遍历动画

每个字母按以下流程显示：

1. 从字母 'A' 开始显示
2. 快速遍历 A → B → C → ... → 目标字母
3. 到达目标字母后固定显示
4. 延迟后显示下一个字母

### 点的显示机制

- **点亮的点**: 使用 `dotOnColor` 颜色显示，GameObject 激活
- **熄灭的点**: GameObject 完全隐藏（SetActive(false)），不占用渲染资源
- **优势**: 更清晰的视觉效果，更好的性能表现

**示例**: 显示 "CRYPTA"

- C: A → B → C (固定)
- R: A → B → C → ... → R (固定)
- Y: A → B → C → ... → Y (固定)
- ...

## 代码结构

### DotMatrixFont.cs

- 静态类，存储所有字母的 5×7 点阵数据
- 提供 `GetCharacterMatrix(char)` 方法获取点阵

### DotMatrixText.cs

- 主控制器，管理整体文本显示
- 处理双行布局和动画播放
- 包含 `DotMatrixCharacter` 内部类管理单个字符

### DotMatrixTextEditor.cs

- 自定义 Inspector 编辑器
- 提供可视化编辑和快速预设功能

## 高级定制

### 修改字母点阵

在 `DotMatrixFont.cs` 中修改对应字母的 `bool[,]` 数组：

```csharp
fontData['A'] = new bool[,]
{
    { false, true, true, true, false },  // 第1行
    { true, false, false, false, true }, // 第2行
    // ... 共7行
};
```

### 添加新字符

在 `InitializeFontData()` 方法中添加新字符定义：

```csharp
fontData['0'] = new bool[,] { /* 5×7 数组 */ };
```

### 自定义动画

修改 `DotMatrixCharacter.PlayTraverseAnimation()` 方法实现自定义动画逻辑。

## 性能优化建议

- 点的数量 = 字母数 × 35 (5×7)
- "CRYPTA GEOMETRICA" = 16 字母 × 35 = 560 个 Image
- 建议使用对象池优化（如需频繁重新生成）

## 常见问题

**Q: 点阵显示不出来？**
A: 检查 Dot Prefab 是否正确设置，确保包含 Image 组件

**Q: 动画不播放？**
A: 确保勾选了 "Enable Typewriter Animation"

**Q: 布局位置不对？**
A: 调整 Line1/Line2 Anchor Position 参数

**Q: 颜色不显示？**
A: 检查 Dot On Color 的 Alpha 值是否为 1

## 版本信息

- 版本: 1.0
- 创建日期: 2025-11-17
- 适用于: Unity 2020.3+
