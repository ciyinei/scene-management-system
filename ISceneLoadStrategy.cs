using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SceneManagement
{
    public interface ISceneLoadingStrategy
    {
        Task LoadScenesAsync(IEnumerable<SceneReference> scenes, IProgress<float> progress);
        Task UnloadScenesAsync(IEnumerable<SceneReference> scenes);
        void SetActiveScene(SceneReference scene);
        
        // Subscene support (no-op in Classic implementation)
        Task LoadSubscenesAsync(IEnumerable<SceneReference> subscenes);
        Task UnloadSubscenesAsync(IEnumerable<SceneReference> subscenes);
    }
}
