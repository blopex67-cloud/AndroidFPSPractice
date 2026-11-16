using UnityEngine;
public class Bootstrap:MonoBehaviour{
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Init(){if(Object.FindObjectOfType<FpsArenaGame>()==null)new GameObject("FPS Arena").AddComponent<FpsArenaGame>();}
}
