using System;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SceneManagement
{
    [CreateAssetMenu(fileName = "SceneReference", menuName = "Scene Management/Scene Reference")]
    public class SceneReference : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector] private string sceneName;
        [SerializeField, HideInInspector] private int sceneBuildIndex = -1;
        
        // This is what designers drag scenes into
        [SerializeField] private UnityEngine.Object sceneAsset;
        
        public string SceneName => sceneName;
        public int BuildIndex => sceneBuildIndex;
        public bool IsValid => !string.IsNullOrEmpty(sceneName) && sceneBuildIndex >= 0;
        
        /// <summary>
        /// Implicit conversion to string. Returns the scene name or empty string if reference is null.
        /// </summary>
        public static implicit operator string(SceneReference reference) => reference?.SceneName ?? string.Empty;
        
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            UpdateSceneInfo();
#endif
        }
        
        public void OnAfterDeserialize()
        {
            // Currently empty, but reserved for future runtime scene info synchronization
        }

#if UNITY_EDITOR
        private void UpdateSceneInfo()
        {
            if (sceneAsset == null)
            {
                sceneName = null;
                sceneBuildIndex = -1;
                return;
            }
            
            var assetPath = AssetDatabase.GetAssetPath(sceneAsset);
            if (string.IsNullOrEmpty(assetPath)) 
            {
                ResetSceneInfo();
                return;
            }
            
            // Check if it's actually a scene
            if (!assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                var assetName = sceneAsset?.name ?? "Unknown";
                Debug.LogWarning($"Asset '{assetName}' is not a scene file!", this);
                ResetSceneInfo();
                return;
            }
            
            // Extract scene name once, reuse it
            string extractedSceneName = Path.GetFileNameWithoutExtension(assetPath);
            
            // Find the scene in build settings
            var scenesArray = EditorBuildSettings.scenes;
            if (scenesArray == null || scenesArray.Length == 0)
            {
                var assetName = sceneAsset?.name ?? "Unknown";
                Debug.LogWarning($"Scene '{assetName}' is not in Build Settings (no scenes configured)!", this);
                ResetSceneInfo();
                return;
            }
            
            var buildScene = scenesArray.FirstOrDefault(s => s != null && s.path == assetPath);
            
            // Check if scene was found and is enabled
            if (buildScene != null && !string.IsNullOrEmpty(buildScene.path) && buildScene.enabled)
            {
                sceneName = extractedSceneName;
                sceneBuildIndex = Array.IndexOf(scenesArray, buildScene);
            }
            else
            {
                // Scene not in build settings or is disabled
                var assetName = sceneAsset?.name ?? "Unknown";
                Debug.LogWarning($"Scene '{assetName}' is not in Build Settings or is disabled!", this);
                ResetSceneInfo();
            }
        }
        
        /// <summary>
        /// Resets the scene reference to an invalid state.
        /// </summary>
        private void ResetSceneInfo()
        {
            sceneName = null;
            sceneBuildIndex = -1;
        }
        
        /// <summary>
        /// Called by SOStages or other container ScriptableObjects to validate the scene is in build settings.
        /// </summary>
        public void ValidateInBuildSettings()
        {
            UpdateSceneInfo();
        }
#endif
    }
}