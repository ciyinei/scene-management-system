using System;
using System.Collections.Generic;
using System.Linq;

namespace SceneManagement
{
    public readonly struct SceneTransition
    {
        public IReadOnlyList<SceneReference> ScenesToLoad { get; }
        public IReadOnlyList<SceneReference> ScenesToUnload { get; }
        public IReadOnlyList<SceneReference> SubScenesToLoad { get; }
        public IReadOnlyList<SceneReference> SubscenesToUnload { get; }
        public SceneReference ActiveScene { get; }
        public bool SaveBeforeTransition { get; }

        public SceneTransition(
            IReadOnlyList<SceneReference> load = null,
            IReadOnlyList<SceneReference> unload = null,
            IReadOnlyList<SceneReference> subscenesToLoad = null,
            IReadOnlyList<SceneReference> subscenesToUnload = null,
            SceneReference activeScene = null,
            bool saveBeforeTransition = true)
        {
            ScenesToLoad = load ?? Array.Empty<SceneReference>();
            ScenesToUnload = unload ?? Array.Empty<SceneReference>();
            SubScenesToLoad = subscenesToLoad ?? Array.Empty<SceneReference>();
            SubscenesToUnload = subscenesToUnload ?? Array.Empty<SceneReference>();
            ActiveScene = activeScene;
            SaveBeforeTransition = saveBeforeTransition;
        }

        public static Builder Create() => new Builder();

        public class Builder
        {
            private readonly List<SceneReference> load = new();
            private readonly List<SceneReference> unload = new();
            private readonly List<SceneReference> subLoad = new();
            private readonly List<SceneReference> subUnload = new();
            private SceneReference activeScene;
            private bool saveBeforeTransition = true;

            public Builder Load(params SceneReference[] scenes)
            {
                load.AddRange(scenes.Where(s => s != null && s.IsValid));
                return this;
            }

            public Builder Unload(params SceneReference[] scenes)
            {
                unload.AddRange(scenes.Where(s => s != null));
                return this;
            }

            public Builder LoadSubscenes(params SceneReference[] scenes)
            {
                subLoad.AddRange(scenes.Where(s => s != null && s.IsValid));
                return this;
            }

            public Builder UnloadSubscenes(params SceneReference[] scenes)
            {
                subUnload.AddRange(scenes.Where(s => s != null));
                return this;
            }

            public Builder SetActive(SceneReference scene)
            {
                activeScene = scene;
                return this;
            }
            
            public Builder WithoutSaving() { saveBeforeTransition = false; return this; }

            public SceneTransition Build()
            {
                return new SceneTransition(
                    load.AsReadOnly(),
                    unload.AsReadOnly(),
                    subLoad.AsReadOnly(),
                    subUnload.AsReadOnly(),
                    activeScene,
                    saveBeforeTransition
                );
            }
            
            // Convenience factory methods
            public static SceneTransition LoadOnly(params SceneReference[] scenes)
            {
                return new SceneTransition(load: scenes, activeScene: scenes.FirstOrDefault());
            }

            public static SceneTransition UnloadOnly(params SceneReference[] scenes)
            {
                return new SceneTransition(unload: scenes);
            }
        }
    }
}