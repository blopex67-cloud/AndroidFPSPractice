using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class FpsArenaGame : MonoBehaviour
{
    public static FpsArenaGame I;
    public CharacterController player;
    public Camera cam;
    public Text hud;
    public int ammo = 8, reserve = 48, targetsDown;
    public bool reloading;
    AudioSource audioSource;
    AudioClip shotClip, reloadClip, hitClip;
    float fireAt, reloadDone;
    readonly List<PracticeDummy> dummies = new List<PracticeDummy>();
    Material floorMat, wallMat, weaponMat, trimMat;

    void Awake(){ I=this; }
    void Start(){ Application.targetFrameRate=60; BuildWorld(); BuildPlayer(); BuildHud(); BuildAudio(); }
    Material Mat(Color c){ var m=new Material(Shader.Find("Standard")); m.color=c; m.enableInstancing=true; return m; }
    GameObject Cube(string n, Vector3 pos, Vector3 scale, Material m, Transform parent=null){ var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=n; g.transform.position=pos; g.transform.localScale=scale; if(parent)g.transform.SetParent(parent,true); g.GetComponent<Renderer>().sharedMaterial=m; return g; }
    void BuildWorld(){
        floorMat=Mat(new Color(.19f,.23f,.28f)); wallMat=Mat(new Color(.57f,.62f,.68f)); weaponMat=Mat(new Color(.10f,.13f,.17f)); trimMat=Mat(new Color(.95f,.35f,.06f));
        Cube("Arena floor",new Vector3(0,-.25f,0),new Vector3(22,.5f,22),floorMat);
        Cube("North wall",new Vector3(0,2.3f,10.8f),new Vector3(22,5,0.4f),wallMat); Cube("South wall",new Vector3(0,2.3f,-10.8f),new Vector3(22,5,.4f),wallMat);
        Cube("East wall",new Vector3(10.8f,2.3f,0),new Vector3(.4f,5,22),wallMat); Cube("West wall",new Vector3(-10.8f,2.3f,0),new Vector3(.4f,5,22),wallMat);
        Cube("Back accent",new Vector3(0,.25f,10.55f),new Vector3(22,.18f,.14f),trimMat);
        var light=new GameObject("Arena light").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.15f; light.transform.rotation=Quaternion.Euler(48,-32,0); RenderSettings.ambientLight=new Color(.52f,.58f,.67f); RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        for(int i=0;i<4;i++) SpawnDummy(new Vector3((i-1.5f)*3f,0,5.5f+(i%2)*1.3f));
        for(int i=-1;i<=1;i++) Cube("Target stand",new Vector3(i*4,.35f,7.9f),new Vector3(.6f,.7f,.6f),weaponMat);
    }
    void BuildPlayer(){
        var p=new GameObject("Player"); p.transform.position=new Vector3(0,0,-6.5f); player=p.AddComponent<CharacterController>(); player.height=1.8f; player.radius=.34f; player.center=new Vector3(0,.9f,0); player.slopeLimit=45; player.stepOffset=.3f;
        var c=new GameObject("Player camera"); c.transform.SetParent(p.transform); c.transform.localPosition=new Vector3(0,1.58f,0); cam=c.AddComponent<Camera>(); cam.fieldOfView=75; cam.nearClipPlane=.03f; cam.tag="MainCamera";
        var gun=Cube("Training blaster",new Vector3(.32f,-.25f,.65f),new Vector3(.18f,.2f,.65f),weaponMat,cam.transform); gun.transform.localRotation=Quaternion.Euler(2,0,0);
        Cube("Blaster barrel",new Vector3(.32f,-.17f,1.01f),new Vector3(.1f,.1f,.35f),trimMat,cam.transform);
    }
    void BuildHud(){
        var canvasGo=new GameObject("Mobile HUD"); var canvas=canvasGo.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=10;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; canvasGo.GetComponent<CanvasScaler>().referenceResolution=new Vector2(1280,720); canvasGo.GetComponent<CanvasScaler>().matchWidthOrHeight=.5f; canvasGo.AddComponent<GraphicRaycaster>();
        var ev=new GameObject("EventSystem"); ev.AddComponent<EventSystem>(); ev.AddComponent<StandaloneInputModule>();
        hud=Label(canvas.transform,"",new Vector2(.5f,.96f),new Vector2(.5f,.96f),new Vector2(500,70),32,Color.white,TextAnchor.MiddleCenter);
        Label(canvas.transform,"MOVE",new Vector2(.1f,.13f),new Vector2(.1f,.13f),new Vector2(150,45),20,new Color(1,1,1,.85f),TextAnchor.MiddleCenter);
        Button(canvas.transform,"FIRE",new Vector2(.88f,.19f),new Vector2(160,100),new Color(.85f,.2f,.08f),()=>Shoot());
        Button(canvas.transform,"RELOAD",new Vector2(.88f,.06f),new Vector2(160,58),new Color(.16f,.26f,.38f),()=>Reload());
        Button(canvas.transform,"RAGDOLL",new Vector2(.72f,.07f),new Vector2(165,58),new Color(.22f,.34f,.22f),()=>SpawnDummy(new Vector3(Random.Range(-5,5),0,Random.Range(2,8))));
        Label(canvas.transform,"Arrastra el lado derecho para mirar",new Vector2(.76f,.42f),new Vector2(.76f,.42f),new Vector2(350,45),17,new Color(1,1,1,.75f),TextAnchor.MiddleCenter);
    }
    Text Label(Transform parent,string s,Vector2 amin,Vector2 amax,Vector2 size,int fs,Color col,TextAnchor anchor){ var g=new GameObject("Label"); g.transform.SetParent(parent,false); var r=g.AddComponent<RectTransform>(); r.anchorMin=amin;r.anchorMax=amax;r.anchoredPosition=Vector2.zero;r.sizeDelta=size; var t=g.AddComponent<Text>();t.text=s;t.font=Resources.GetBuiltinResource<Font>("Arial.ttf");t.fontSize=fs;t.color=col;t.alignment=anchor;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;return t; }
    void Button(Transform parent,string s,Vector2 anchor,Vector2 size,Color color,UnityEngine.Events.UnityAction action){ var g=new GameObject(s+" Button");g.transform.SetParent(parent,false);var r=g.AddComponent<RectTransform>();r.anchorMin=anchor;r.anchorMax=anchor;r.anchoredPosition=Vector2.zero;r.sizeDelta=size;var im=g.AddComponent<Image>();im.color=color;var b=g.AddComponent<Button>();b.targetGraphic=im;b.onClick.AddListener(action);var t=Label(g.transform,s,new Vector2(0,0),new Vector2(1,1),Vector2.zero,22,Color.white,TextAnchor.MiddleCenter);t.fontStyle=FontStyle.Bold; }
    void BuildAudio(){ audioSource=gameObject.AddComponent<AudioSource>(); audioSource.spatialBlend=0; shotClip=Tone("Shot",120,.09f);reloadClip=Tone("Reload",480,.18f);hitClip=Tone("Hit",760,.08f); }
    AudioClip Tone(string n,float hz,float sec){int sr=22050, count=(int)(sr*sec);float[] d=new float[count];for(int i=0;i<count;i++){float env=1f-(float)i/count;d[i]=Mathf.Sin(2*Mathf.PI*hz*i/sr)*env*.35f;}var a=AudioClip.Create(n,count,1,sr,false);a.SetData(d,0);return a;}
    void Update(){
        if(player==null)return;
        float x=Input.GetAxisRaw("Horizontal"),z=Input.GetAxisRaw("Vertical"); Vector3 move=player.transform.right*x+player.transform.forward*z;
        if(Input.touchCount>0){ Touch t=Input.GetTouch(0); if(t.position.x<Screen.width*.48f){Vector2 v=new Vector2(t.position.x/Screen.width-.12f,t.position.y/Screen.height-.13f);move=player.transform.right*Mathf.Clamp(v.x*3,-1,1)+player.transform.forward*Mathf.Clamp(v.y*3,-1,1);} }
        player.Move((move*4.1f+Vector3.down*1.5f)*Time.deltaTime);
        if(Input.GetMouseButton(0)&&Input.mousePosition.x>Screen.width*.5f){cam.transform.localRotation*=Quaternion.Euler(-Input.GetAxis("Mouse Y")*2,0,0);player.transform.Rotate(0,Input.GetAxis("Mouse X")*2,0);}
        if(Input.GetKeyDown(KeyCode.Space))Shoot();if(Input.GetKeyDown(KeyCode.R))Reload();
        foreach(Touch t in Input.touches){if(t.phase==TouchPhase.Moved && t.position.x>Screen.width*.45f){float sens=.12f;player.transform.Rotate(0,t.deltaPosition.x*sens,0);cam.transform.Rotate(-t.deltaPosition.y*sens,0,0);var e=cam.transform.localEulerAngles;e.x=(e.x>180?e.x-360:e.x);e.x=Mathf.Clamp(e.x,-75,75);cam.transform.localEulerAngles=new Vector3(e.x,0,0);}}
        if(reloading&&Time.time>reloadDone){int n=Mathf.Min(8-ammo,reserve);ammo+=n;reserve-=n;reloading=false;}
        hud.text="ARENA DE PRÁCTICA     OBJETIVOS: "+targetsDown+"     " +(reloading?"RECARGANDO":"MUNICIÓN  "+ammo+" / "+reserve);
    }
    public void Shoot(){ if(Time.time<fireAt||reloading)return; fireAt=Time.time+.28f;if(ammo<=0){Reload();return;}ammo--;audioSource.PlayOneShot(shotClip);if(Physics.Raycast(cam.transform.position,cam.transform.forward,out RaycastHit h,80)){var d=h.collider.GetComponentInParent<PracticeDummy>();if(d){d.Hit(h.point,cam.transform.forward);audioSource.PlayOneShot(hitClip);}} }
    public void Reload(){if(reloading||ammo>=8||reserve<=0)return;reloading=true;reloadDone=Time.time+1.05f;audioSource.PlayOneShot(reloadClip);}
    public void SpawnDummy(Vector3 pos){var root=new GameObject("Ragdoll practice dummy");root.transform.position=pos;root.AddComponent<PracticeDummy>();BuildRagdoll(root.transform);dummies.Add(root.GetComponent<PracticeDummy>());}
    void BuildRagdoll(Transform root){
        var mat=Mat(new Color(.9f,.12f,.08f)); var head=Body(root,"Head",new Vector3(0,1.65f,0),new Vector3(.38f,.38f,.34f),mat);
        var torso=Body(root,"Torso",new Vector3(0,1.15f,0),new Vector3(.52f,.65f,.3f),mat); Link(head,torso,CharacterJointType.Spherical);
        var pelvis=Body(root,"Pelvis",new Vector3(0,.72f,0),new Vector3(.42f,.3f,.3f),mat); Link(torso,pelvis,CharacterJointType.Spherical);
        var lu=Body(root,"Left upper arm",new Vector3(-.43f,1.25f,0),new Vector3(.22f,.48f,.22f),mat);Link(torso,lu,CharacterJointType.Hinge);
        var ru=Body(root,"Right upper arm",new Vector3(.43f,1.25f,0),new Vector3(.22f,.48f,.22f),mat);Link(torso,ru,CharacterJointType.Hinge);
        var ll=Body(root,"Left leg",new Vector3(-.16f,.32f,0),new Vector3(.24f,.65f,.25f),mat);Link(pelvis,ll,CharacterJointType.Hinge);
        var rl=Body(root,"Right leg",new Vector3(.16f,.32f,0),new Vector3(.24f,.65f,.25f),mat);Link(pelvis,rl,CharacterJointType.Hinge);
    }
    Rigidbody Body(Transform p,string n,Vector3 pos,Vector3 size,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Capsule);g.name=n;g.transform.SetParent(p,true);g.transform.position=p.position+pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;var rb=g.AddComponent<Rigidbody>();rb.isKinematic=true;return rb;}
    void Link(Rigidbody a,Rigidbody b,CharacterJointType kind){var j=b.gameObject.AddComponent<CharacterJoint>();j.connectedBody=a;j.enableProjection=true;j.projectionDistance=.12f;j.anchor=new Vector3(0,.5f,0);j.connectedAnchor=new Vector3(0,-.5f,0);}
    enum CharacterJointType{Spherical,Hinge}
}
public class PracticeDummy:MonoBehaviour{
    bool down;
    public void Hit(Vector3 p,Vector3 dir){if(down)return;down=true;var r=GetComponentsInChildren<Rigidbody>();foreach(var b in r){b.isKinematic=false;b.AddForceAtPosition(dir*3.2f,p,ForceMode.Impulse);}if(FpsArenaGame.I!=null){FpsArenaGame.I.targetsDown++;}}
}
