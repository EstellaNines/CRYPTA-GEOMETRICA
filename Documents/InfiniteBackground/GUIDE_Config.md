# 无限背景系统配置指南

## 1. 资源准备 (Sprite Import)
在导入背景图片时，请确保以下设置以获得最佳效果：
*   **Texture Type**: Sprite (2D and UI)
*   **Sprite Mode**: Single
*   **Pivot**: Center (推荐)
*   **Wrap Mode**: Clamp (因为系统使用拼接方式，Clamp 可避免边缘伪影)
*   **Filter Mode**: Point (像素风) 或 Bilinear (高清风)，与项目风格一致。

## 2. 创建主题配置 (Theme Configuration)
背景数据通过 ScriptableObject 管理，方便不同关卡复用。

1.  在 **Project** 窗口右键，选择 `Create -> CryptaGeometrica -> Background -> Theme`。
2.  命名文件（例如 `Forest_Theme`）。
3.  在 Inspector 中配置 **Layers** 列表（建议从最远的背景开始配置）：

### 关键参数说明
*   **Sprite**: 背景图片。
*   **Sorting Order**: 渲染层级。建议使用负值（如 -100, -90...）确保在 Gameplay 元素之后。值越小越靠后。
*   **Parallax Factor (视差系数)**: 决定背景的深度感。
    *   **1.0**: 绝对跟随摄像机（看起来静止不动）。适用于**无穷远**的天体（月亮、星星）。
    *   **0.7 ~ 0.95**: 远景（远山、云层）。
    *   **0.3 ~ 0.6**: 中远景（树林背景）。
    *   **0.0**: 绝对静止（和普通游戏物体一样）。
*   **Y Offset**: 垂直偏移量，用于调整地平线高度。
*   **Scale Multiplier**: 缩放倍率，如果图片像素不够大，可适当放大。

## 3. 场景配置 (Scene Setup)
1.  在场景中创建一个空物体，命名为 `InfiniteBackgroundSystem`。
2.  添加组件 `InfiniteBackgroundManager`。
3.  将之前创建的 `Theme` 资源拖拽到 **Default Theme** 属性中。
4.  **Target Camera**: 默认为空（自动抓取 MainCamera）。如果使用 Cinemachine 或特定摄像机，请手动指定。

## 4. 快速测试工具
系统提供了一个编辑器工具用于快速验证资源。
1.  点击顶部菜单 `Tools -> Infinite Background -> Setup Test Scene`。
2.  这会自动创建一个包含摄像机和管理器的测试场景。
3.  运行场景，使用 **方向键** 移动摄像机，观察视差效果。

## 5. 常见问题
*   **背景有缝隙？**
    *   检查 Sprite 的 Import Settings，确保 `Pixels Per Unit` 设置正确。
    *   确保图片是可以左右无缝拼接的 (Seamless)。
*   **背景移动方向反了？**
    *   Parallax Factor 范围是 0~1。不应为负数。
    *   如果希望背景移动得比摄像机还快（前景遮挡），可以使用负数或 >1 的值（视差算法通用，但需自行测试效果）。

## 6. 自动适配与动态平铺 (Auto Fit & Dynamic Tiling) v1.1
针对背景图过小或无法覆盖高分辨率屏幕的问题，系统新增了以下功能：

### 动态瓦片数量 (Dynamic Tiling)
*   系统现在会自动计算屏幕宽度和图片宽度的比例。
*   如果图片较窄，会自动生成 5个、7个甚至更多瓦片，确保水平方向无黑边。
*   **无需配置**，自动生效。

### 自动高度适配 (Auto Fit Height)
如果背景图高度不足以覆盖屏幕（例如上下有黑边），可在 Theme 配置中开启：
*   **Auto Fit Height**: 勾选后，系统会自动缩放图片高度以填满屏幕。
*   **Maintain Aspect Ratio**: 勾选后（默认），在缩放高度时会同步缩放宽度，保持图片比例不变（避免变形）。如果不勾选，则只拉伸高度。

**使用建议**：对于天空、远景等图层，建议开启 `Auto Fit Height` + `Maintain Aspect Ratio`。

## 7. 颜色主题配置 (Color Tint) v1.2
系统支持为每一层背景单独配置颜色叠加，从而复用同一套灰度图片制作出不同颜色的主题（如红色地狱、黄色沙漠）。

### 配置方法
1.  在 `Theme` 配置文件的 Layer 设置中，找到 **Visual** 部分。
2.  修改 **Tint Color** 属性。
    *   **默认 (白色)**: 显示图片原色。
    *   **红色主题**: 将颜色设为红色 (FF0000) 或深红。
    *   **黄色主题**: 将颜色设为黄色 (FFFF00) 或琥珀色。
3.  **提示**: 为了获得最佳的染色效果，建议原始背景图片使用 **灰度图 (Grayscale)** 或 **白色主调** 的图片。

## 8. 快速创建新主题
1.  在 Project 窗口找到现有的 Theme 文件（如 `Default_Theme`）。
2.  按 `Ctrl + D` 复制文件。
3.  重命名为 `Red_Theme` 或 `Yellow_Theme`。
4.  修改新文件中的 **Tint Color** 即可。

