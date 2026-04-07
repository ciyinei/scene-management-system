using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneManagement
{
    public class ClassicSceneLoad : ISceneLoadingStrategy
    {
        public async Task LoadScenesAsync(IEnumerable<SceneReference> scenes, IProgress<float> progress)
        {
            var sceneList = scenes.Where(s => s != null && s.IsValid).ToList();
            int sceneCount = sceneList.Count;

            if (sceneCount == 0) return;

            string sceneNames = string.Join(", ", sceneList.Select(s => s.SceneName));
            Debug.Log($"Loading {sceneCount} scenes: {sceneNames}");

            float baseProgress = 0f;

            foreach (var sceneRef in sceneList)
            {
                await LoadSingleSceneAsync(sceneRef, sceneCount, baseProgress, progress);
                baseProgress += 1f / sceneCount;
            }

            Debug.Log($"Loaded {sceneCount} scenes: {sceneNames}");
        }

        private async Task LoadSingleSceneAsync(SceneReference sceneRef, int totalScenes, float baseProgress,
            IProgress<float> progress)
        {
            var op = SceneManager.LoadSceneAsync(sceneRef.SceneName, LoadSceneMode.Additive);

            if (op == null)
            {
                Debug.LogError($"Failed to start loading scene: {sceneRef.SceneName}");
                throw new InvalidOperationException($"Scene '{sceneRef.SceneName}' could not be loaded");
            }

            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                float currentProgress = baseProgress + (op.progress / 0.9f) / totalScenes;
                progress?.Report(currentProgress);
                await Task.Yield();
            }

            op.allowSceneActivation = true;

            while (!op.isDone)
            {
                await Task.Yield();
            }

            progress?.Report(baseProgress + 1f / totalScenes);
        }

        public async Task UnloadScenesAsync(IEnumerable<SceneReference> scenes)
        {
            var sceneList = scenes.Where(s => s != null).ToList();
            int sceneCount = sceneList.Count;

            if (sceneCount == 0) return;

            string sceneNames = string.Join(", ", sceneList.Select(s => s.SceneName));
            Debug.Log($"Unloading {sceneCount} scenes: {sceneNames}");

            foreach (var sceneRef in sceneList)
            {
                var scene = SceneManager.GetSceneByName(sceneRef.SceneName);

                if (!scene.IsValid() || !scene.isLoaded)
                {
                    Debug.LogWarning($"Scene '{sceneRef.SceneName}' not loaded, skipping unload");
                    continue;
                }

                var op = SceneManager.UnloadSceneAsync(sceneRef.SceneName);

                if (op == null)
                {
                    Debug.LogError($"Failed to start unloading scene: {sceneRef.SceneName}");
                    continue;
                }

                while (!op.isDone)
                {
                    await Task.Yield();
                }
            }

            Debug.Log($"Unloaded {sceneCount} scenes: {sceneNames}");
        }

        public void SetActiveScene(SceneReference sceneRef)
        {
            if (sceneRef == null || !sceneRef.IsValid) return;

            var scene = SceneManager.GetSceneByName(sceneRef.SceneName);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException($"Active scene '{sceneRef.SceneName}' not loaded");
            }

            SceneManager.SetActiveScene(scene);
        }

        // Subscene methods - no-op for classic loading
        public Task LoadSubscenesAsync(IEnumerable<SceneReference> subscenes) => Task.CompletedTask;
        public Task UnloadSubscenesAsync(IEnumerable<SceneReference> subscenes) => Task.CompletedTask;
    }
}