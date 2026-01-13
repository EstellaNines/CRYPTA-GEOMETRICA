using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;

namespace CryptaGeometrica.EnemyStateMachine.Editor
{
    /// <summary>
    /// GenericEnemyController的可视化编辑器
    /// 提供实时状态监控和可视化功能，继承OdinEditor以保持Odin功能
    /// </summary>
    [CustomEditor(typeof(GenericEnemyController))]
    public class GenericEnemyControllerEditor : OdinEditor
    {
        private GenericEnemyController controller;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            controller = (GenericEnemyController)target;
        }
        
        public override void OnInspectorGUI()
        {
            // 首先绘制Odin Inspector的默认内容（包括所有中文标签和按钮）
            base.OnInspectorGUI();
            
            // 然后添加运行时的额外信息
            if (Application.isPlaying)
            {
                GUILayout.Space(10);
                DrawRuntimeExtras();
            }
            
            // 强制重绘以实现实时更新
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
        
        /// <summary>
        /// 绘制运行时额外信息
        /// </summary>
        private void DrawRuntimeExtras()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("🎮 运行时监控", EditorStyles.boldLabel);
            
            if (controller?.StateMachine != null)
            {
                string currentState = controller.StateMachine.CurrentStateName ?? "未知";
                EditorGUILayout.LabelField($"当前状态: {currentState}");
                EditorGUILayout.LabelField($"生命值: {controller.CurrentHealth:F1}");
                EditorGUILayout.LabelField($"存活状态: {controller.IsAlive}");
            }
            
            EditorGUILayout.EndVertical();
        }
    }
}
