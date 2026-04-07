using System.Threading.Tasks;
using UnityEngine;

namespace SceneManagement
{
    public interface ILoadingScreen
    {
        Task FadeIn(float duration);
        Task FadeOut(float duration);
        void SetProgress(float progress);
    }
}
