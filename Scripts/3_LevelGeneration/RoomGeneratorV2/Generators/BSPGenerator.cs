using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// BSP 空间分割生成器
    /// 使用二叉空间分割算法将房间区域递归划分为更小的子区域
    /// </summary>
    public class BSPGenerator
    {
        #region 字段
        
        private readonly RoomGenParamsV2 parameters;
        private readonly System.Random random;
        
        // 统计信息
        private int totalNodes;
        private int leafNodes;
        private int maxDepthReached;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 总节点数
        /// </summary>
        public int TotalNodes => totalNodes;
        
        /// <summary>
        /// 叶节点数
        /// </summary>
        public int LeafNodes => leafNodes;
        
        /// <summary>
        /// 实际达到的最大深度
        /// </summary>
        public int MaxDepthReached => maxDepthReached;
        
        #endregion

        #region 构造函数
        
        public BSPGenerator(RoomGenParamsV2 parameters, System.Random random)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.random = random ?? new System.Random();
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 生成 BSP 树
        /// </summary>
        /// <param name="bounds">根节点边界</param>
        /// <returns>BSP 树根节点</returns>
        public BSPNode Generate(RectInt bounds)
        {
            // 重置统计
            totalNodes = 0;
            leafNodes = 0;
            maxDepthReached = 0;
            
            // 递归分割
            BSPNode root = Split(bounds, 0);
            
            // BSP生成完成
            
            return root;
        }
        
        /// <summary>
        /// 使用默认边界生成 BSP 树（考虑边缘留空）
        /// </summary>
        public BSPNode Generate()
        {
            RectInt bounds = new RectInt(
                parameters.edgePadding,
                parameters.edgePadding,
                parameters.roomWidth - parameters.edgePadding * 2,
                parameters.roomHeight - parameters.edgePadding * 2
            );
            
            return Generate(bounds);
        }
        
        #endregion

        #region 核心算法
        
        /// <summary>
        /// 递归分割节点
        /// </summary>
        private BSPNode Split(RectInt bounds, int depth)
        {
            totalNodes++;
            maxDepthReached = Mathf.Max(maxDepthReached, depth);
            
            BSPNode node = new BSPNode(bounds, depth);
            
            // 检查终止条件
            if (ShouldStopSplitting(bounds, depth))
            {
                leafNodes++;
                return node;
            }
            
            // 决定分割方向
            SplitDirection direction = DetermineSplitDirection(bounds);
            node.splitDirection = direction;
            
            // 计算分割位置
            int splitPos = CalculateSplitPosition(bounds, direction);
            node.splitPosition = splitPos;
            
            // 验证分割是否有效
            if (!IsValidSplit(bounds, direction, splitPos))
            {
                // 分割无效，作为叶节点
                leafNodes++;
                return node;
            }
            
            // 执行分割
            (RectInt leftBounds, RectInt rightBounds) = PerformSplit(bounds, direction, splitPos);
            
            // 递归分割子节点
            node.left = Split(leftBounds, depth + 1);
            node.right = Split(rightBounds, depth + 1);
            
            return node;
        }
        
        /// <summary>
        /// 检查是否应该停止分割
        /// </summary>
        private bool ShouldStopSplitting(RectInt bounds, int depth)
        {
            // 条件 1: 达到最大深度
            if (depth >= parameters.maxBSPDepth)
            {
                return true;
            }
            
            // 条件 2: 宽度不足以继续分割
            if (bounds.width < parameters.minBSPSize * 2)
            {
                return true;
            }
            
            // 条件 3: 高度不足以继续分割
            if (bounds.height < parameters.minBSPSize * 2)
            {
                return true;
            }
            
            // 条件 4: 基于目标房间数量的智能停止
            // 只有当已经有足够叶节点时才考虑停止
            if (leafNodes >= parameters.targetRoomCount)
            {
                // 达到目标后，有较高概率停止
                if (random.NextDouble() < 0.7f)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 决定分割方向
        /// </summary>
        private SplitDirection DetermineSplitDirection(RectInt bounds)
        {
            float aspectRatio = (float)bounds.width / bounds.height;
            
            // 如果太宽，垂直分割（左右分）
            if (aspectRatio > 1.25f)
            {
                return SplitDirection.Vertical;
            }
            
            // 如果太高，水平分割（上下分）
            if (aspectRatio < 0.8f)
            {
                return SplitDirection.Horizontal;
            }
            
            // 接近正方形，随机选择
            return random.NextDouble() > 0.5 ? SplitDirection.Horizontal : SplitDirection.Vertical;
        }
        
        /// <summary>
        /// 计算分割位置
        /// </summary>
        private int CalculateSplitPosition(RectInt bounds, SplitDirection direction)
        {
            // 在配置的比例范围内随机选择
            float ratio = (float)(random.NextDouble() * 
                (parameters.splitRatioRange.y - parameters.splitRatioRange.x) + 
                parameters.splitRatioRange.x);
            
            if (direction == SplitDirection.Horizontal)
            {
                int splitY = bounds.y + Mathf.RoundToInt(bounds.height * ratio);
                
                // 确保两边都满足最小尺寸
                splitY = Mathf.Clamp(splitY, 
                    bounds.y + parameters.minBSPSize, 
                    bounds.yMax - parameters.minBSPSize);
                
                return splitY;
            }
            else
            {
                int splitX = bounds.x + Mathf.RoundToInt(bounds.width * ratio);
                
                splitX = Mathf.Clamp(splitX, 
                    bounds.x + parameters.minBSPSize, 
                    bounds.xMax - parameters.minBSPSize);
                
                return splitX;
            }
        }
        
        /// <summary>
        /// 验证分割是否有效
        /// </summary>
        private bool IsValidSplit(RectInt bounds, SplitDirection direction, int splitPos)
        {
            if (direction == SplitDirection.Horizontal)
            {
                int bottomHeight = splitPos - bounds.y;
                int topHeight = bounds.yMax - splitPos;
                
                return bottomHeight >= parameters.minBSPSize && 
                       topHeight >= parameters.minBSPSize;
            }
            else
            {
                int leftWidth = splitPos - bounds.x;
                int rightWidth = bounds.xMax - splitPos;
                
                return leftWidth >= parameters.minBSPSize && 
                       rightWidth >= parameters.minBSPSize;
            }
        }
        
        /// <summary>
        /// 执行分割，返回两个子区域
        /// </summary>
        private (RectInt, RectInt) PerformSplit(RectInt bounds, SplitDirection direction, int splitPos)
        {
            if (direction == SplitDirection.Horizontal)
            {
                // 水平分割：上下分
                RectInt bottom = new RectInt(
                    bounds.x, 
                    bounds.y, 
                    bounds.width, 
                    splitPos - bounds.y
                );
                
                RectInt top = new RectInt(
                    bounds.x, 
                    splitPos, 
                    bounds.width, 
                    bounds.yMax - splitPos
                );
                
                return (bottom, top);
            }
            else
            {
                // 垂直分割：左右分
                RectInt left = new RectInt(
                    bounds.x, 
                    bounds.y, 
                    splitPos - bounds.x, 
                    bounds.height
                );
                
                RectInt right = new RectInt(
                    splitPos, 
                    bounds.y, 
                    bounds.xMax - splitPos, 
                    bounds.height
                );
                
                return (left, right);
            }
        }
        
        #endregion

        #region 辅助方法
        
        /// <summary>
        /// 获取所有叶节点
        /// </summary>
        public static List<BSPNode> GetAllLeaves(BSPNode root)
        {
            return root?.GetLeaves() ?? new List<BSPNode>();
        }
        
        /// <summary>
        /// 获取指定深度的所有节点
        /// </summary>
        public static List<BSPNode> GetNodesAtDepth(BSPNode root, int targetDepth)
        {
            List<BSPNode> result = new List<BSPNode>();
            CollectNodesAtDepth(root, targetDepth, result);
            return result;
        }
        
        private static void CollectNodesAtDepth(BSPNode node, int targetDepth, List<BSPNode> result)
        {
            if (node == null) return;
            
            if (node.depth == targetDepth)
            {
                result.Add(node);
                return;
            }
            
            CollectNodesAtDepth(node.left, targetDepth, result);
            CollectNodesAtDepth(node.right, targetDepth, result);
        }
        
        /// <summary>
        /// 查找包含指定点的叶节点
        /// </summary>
        public static BSPNode FindLeafContaining(BSPNode root, Vector2Int point)
        {
            if (root == null) return null;
            
            if (!root.Contains(point)) return null;
            
            if (root.IsLeaf) return root;
            
            BSPNode leftResult = FindLeafContaining(root.left, point);
            if (leftResult != null) return leftResult;
            
            return FindLeafContaining(root.right, point);
        }
        
        /// <summary>
        /// 查找相邻的叶节点对（用于走廊连接）
        /// </summary>
        public static List<(BSPNode, BSPNode)> FindAdjacentLeafPairs(BSPNode root)
        {
            List<(BSPNode, BSPNode)> pairs = new List<(BSPNode, BSPNode)>();
            CollectAdjacentPairs(root, pairs);
            return pairs;
        }
        
        private static void CollectAdjacentPairs(BSPNode node, List<(BSPNode, BSPNode)> pairs)
        {
            if (node == null || node.IsLeaf) return;
            
            // 获取左右子树的代表房间
            BSPNode leftLeaf = GetRepresentativeLeaf(node.left);
            BSPNode rightLeaf = GetRepresentativeLeaf(node.right);
            
            if (leftLeaf != null && rightLeaf != null)
            {
                pairs.Add((leftLeaf, rightLeaf));
            }
            
            // 递归处理子节点
            CollectAdjacentPairs(node.left, pairs);
            CollectAdjacentPairs(node.right, pairs);
        }
        
        private static BSPNode GetRepresentativeLeaf(BSPNode node)
        {
            if (node == null) return null;
            if (node.IsLeaf) return node;
            
            // 优先返回左子树的叶节点
            BSPNode left = GetRepresentativeLeaf(node.left);
            if (left != null) return left;
            
            return GetRepresentativeLeaf(node.right);
        }
        
        #endregion
    }
}
