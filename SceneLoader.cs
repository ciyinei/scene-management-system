using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace SceneManagement
{
    public class SceneLoader : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private MonoBehaviour loadingScreenComponent; // Assign MB that implements ILoadingScreen
        
        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float bufferTime = 0.3f;

        private ILoadingScreen loadingScreen;
        private ISceneLoadingStrategy loadingStrategy;
        
        // Events for decoupled communication
        public event Action OnSaveRequested;
        public event Action OnLoadRequested;
        public event Action OnScenesLoaded;
        public event Action OnTransitionComplete;

        private void Awake()
        {
            // Resolve interface from assigned component
            loadingScreen = loadingScreenComponent as ILoadingScreen;
            if (loadingScreen == null && loadingScreenComponent != null)
            {
                Debug.LogError($"Assigned component {loadingScreenComponent.GetType().Name} does not implement ILoadingScreen!");
            }
            
            // Default strategy - can be swapped via property or method
            loadingStrategy = new ClassicSceneLoad();
        }

        /// <summary>
        /// Allows runtime strategy swapping (e.g., for ECS support later)
        /// </summary>
        public void SetLoadingStrategy(ISceneLoadingStrategy strategy)
        {
            loadingStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        /// <summary>
        /// Requests save through event. Listeners (like GameFlowController) handle actual saving.
        /// </summary>
        public async Task RequestSaveAsync()
        {
            if (loadingScreen != null)
            {
                await loadingScreen.FadeIn(fadeInDuration);
            }
            
            // Raise event - GameFlowController handles the actual save
            OnSaveRequested?.Invoke();
            
            // Small buffer for synchronous operations
            await Task.Delay(TimeSpan.FromSeconds(bufferTime));
        }

        /// <summary>
        /// Executes scene transition with loading screen and event notifications.
        /// </summary>
        public async Task LoadSceneAsync(SceneTransition transition)
        {
            if (loadingStrategy == null)
            {
                throw new InvalidOperationException("No loading strategy set!");
            }

            try
            {
                // Progress reporter for loading screen
                var progress = new Progress<float>(p => loadingScreen?.SetProgress(p));
                
                // 1. Unload subscenes (strategy handles implementation)
                if (transition.SubscenesToUnload.Count > 0)
                {
                    await loadingStrategy.UnloadSubscenesAsync(transition.SubscenesToUnload);
                }
                
                // 2. Unload scenes
                if (transition.ScenesToUnload.Count > 0)
                {
                    await loadingStrategy.UnloadScenesAsync(transition.ScenesToUnload);
                }
                
                // 3. Load new scenes
                if (transition.ScenesToLoad.Count > 0)
                {
                    await loadingStrategy.LoadScenesAsync(transition.ScenesToLoad, progress);
                }
                
                // 4. Set active scene
                if (transition.ActiveScene != null)
                {
                    loadingStrategy.SetActiveScene(transition.ActiveScene);
                }
                
                // 5. Buffer for GameObject activation
                await Task.Delay(TimeSpan.FromSeconds(bufferTime));
                
                // 6. Notify that scenes are loaded - GameFlowController can load data now
                OnScenesLoaded?.Invoke();
                
                // 7. Raise load request event
                OnLoadRequested?.Invoke();
                
                // 8. Buffer for data injection
                await Task.Delay(TimeSpan.FromSeconds(bufferTime));
                
                // 9. Load subscenes (strategy handles implementation)
                if (transition.SubScenesToLoad.Count > 0)
                {
                    Debug.Log($"Found {transition.SubScenesToLoad.Count} subscenes to load");
                    await loadingStrategy.LoadSubscenesAsync(transition.SubScenesToLoad);
                }
                
                // 10. Initialize scene objects
                await WaitForSceneInitialization();
                
                // 11. Final buffer for instantiation
                await Task.Delay(TimeSpan.FromSeconds(bufferTime));
                
                // 12. Fade out
                if (loadingScreen != null)
                {
                    await loadingScreen.FadeOut(fadeOutDuration);
                }
                
                OnTransitionComplete?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Scene transition failed: {ex.Message}");
                throw; // Re-throw to let caller handle
            }
        }

        private async Task WaitForSceneInitialization()
        {
            var initializers = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OfType<ISceneInitializer>()
                .ToList();
            
            Debug.Log($"Found {initializers.Count} initializers");
            
            foreach (var initializer in initializers)
            {
                try
                {
                    await initializer.PreInitializeAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"PreInitialization {initializer.GetType().Name} failed: {ex.Message}");
                }
            }
            
            
            foreach (var initializer in initializers)
            {
                try
                {
                    await initializer.InitializeAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Initialization {initializer.GetType().Name} failed: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Interface for objects that need async initialization after scene load.
    /// </summary>
    public interface ISceneInitializer
    {
        Task PreInitializeAsync();
        Task InitializeAsync();
    }
}