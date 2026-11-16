using UnityEngine;
public class Bootstrap:MonoBehaviour{
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void SetLandscape(){Screen.orientation=ScreenOrientation.LandscapeLeft;Screen.autorotateToPortrait=false;Screen.autorotateToPortraitUpsideDown=false;Screen.autorotateToLandscapeLeft=true;Screen.autorotateToLandscapeRight=true;Screen.sleepTimeout=SleepTimeout.NeverSleep;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Init(){EnsureGame();}
 void Awake(){EnsureGame();}
 static void EnsureGame(){if(Object.FindObjectOfType<FpsArenaGame>()==null)new GameObject("FPS Arena").AddComponent<FpsArenaGame>();}
}
