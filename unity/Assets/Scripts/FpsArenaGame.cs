using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class FpsArenaGame : MonoBehaviour
{
    public static FpsArenaGame I;
    CharacterController controller;
    Camera viewCamera;
    Text objectiveLabel, ammoLabel, messageLabel;
    RectTransform reticle;
    AudioSource audioSource;
    AudioClip shotClip, reloadClip, hitClip;
    Material concrete, darkMetal, orange, targetMat, weaponMat;
    int ammo = 8, reserve = 48, targetsDown;
    bool isReloading;
    float reloadEnds, nextShot, verticalVelocity, messageEnds;
    readonly List<PracticeDummy> dummies = new List<PracticeDummy>();
    MobileStick stick;
    Transform weaponRoot;
    Vector2 lookLast;
    int lookFinger = -1;

    void Awake()
    {
        I = this;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        try
        {
            BuildMaterials();
            BuildArena();
            BuildPlayer();
            BuildHud();
            BuildAudio();
            UpdateHud();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            ShowStartupError(e);
        }
    }

    Material MakeMaterial(Color tint)
    {
        Material template = Resources.Load<Material>("ArenaMaterial");
        if (template == null) throw new System.Exception("Falta Assets/Resources/ArenaMaterial.mat");
        Material m = new Material(template);
        m.color = tint;
        return m;
    }

    void BuildMaterials()
    {
        concrete = MakeMaterial(new Color(.24f, .29f, .34f));
        darkMetal = MakeMaterial(new Color(.055f, .085f, .12f));
        orange = MakeMaterial(new Color(1f, .28f, .055f));
        targetMat = MakeMaterial(new Color(.83f, .12f, .075f));
        weaponMat = MakeMaterial(new Color(.075f, .105f, .13f));
    }

    GameObject Box(string label, Vector3 pos, Vector3 size, Material mat, Transform parent = null)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = label;
        if (parent != null)
        {
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = size;
        }
        else
        {
            g.transform.position = pos;
            g.transform.localScale = size;
        }
        Renderer r = g.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        return g;
    }

    void BuildArena()
    {
        RenderSettings.ambientLight = new Color(.55f, .59f, .64f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(.25f, .31f, .37f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 15;
        RenderSettings.fogEndDistance = 36;

        Box("Range floor", new Vector3(0, -.25f, 0), new Vector3(24, .5f, 26), concrete);
        Box("Rear wall", new Vector3(0, 3, 12.8f), new Vector3(24, 6, .5f), darkMetal);
        Box("Left wall", new Vector3(-12, 3, 0), new Vector3(.5f, 6, 26), darkMetal);
        Box("Right wall", new Vector3(12, 3, 0), new Vector3(.5f, 6, 26), darkMetal);
        Box("Rear orange stripe", new Vector3(0, .42f, 12.51f), new Vector3(23, .12f, .12f), orange);
        Box("Left orange stripe", new Vector3(-11.72f, .42f, 0), new Vector3(.12f, .12f, 25), orange);
        Box("Right orange stripe", new Vector3(11.72f, .42f, 0), new Vector3(.12f, .12f, 25), orange);

        for (int i = -2; i <= 2; i++)
        {
            Box("Floor lane marking", new Vector3(i * 3.2f, .012f, 1.5f), new Vector3(.045f, .02f, 17), orange);
            Box("Target bay", new Vector3(i * 3.2f, 2.35f, 11.9f), new Vector3(2.45f, .06f, .12f), orange);
            Box("Ceiling light", new Vector3(i * 4.2f, 5.7f, 2f), new Vector3(2.2f, .08f, .5f), orange);
        }
        Box("Ceiling", new Vector3(0, 6.2f, 0), new Vector3(24, .35f, 26), darkMetal);

        GameObject lightObject = new GameObject("Range key light");
        Light mainLight = lightObject.AddComponent<Light>();
        mainLight.type = LightType.Directional;
        mainLight.intensity = 1.1f;
        lightObject.transform.rotation = Quaternion.Euler(48, -28, 0);
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 3.2f;
            SpawnDummy(new Vector3(x, 0, 7.6f + (i % 2) * 1.2f));
        }
    }

    void BuildPlayer()
    {
        GameObject playerObject = new GameObject("FPS Player");
        playerObject.transform.position = new Vector3(0, 0, -7.5f);
        controller = playerObject.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = .32f;
        controller.center = new Vector3(0, .9f, 0);
        controller.stepOffset = .3f;

        GameObject cameraObject = new GameObject("FPS View");
        cameraObject.transform.SetParent(playerObject.transform, false);
        cameraObject.transform.localPosition = new Vector3(0, 1.58f, 0);
        viewCamera = cameraObject.AddComponent<Camera>();
        viewCamera.tag = "MainCamera";
        viewCamera.fieldOfView = 72;
        viewCamera.nearClipPlane = .04f;
        viewCamera.clearFlags = CameraClearFlags.SolidColor;
        viewCamera.backgroundColor = new Color(.25f, .31f, .37f);

        weaponRoot = new GameObject("First person weapon").transform;
        weaponRoot.SetParent(viewCamera.transform, false);
        weaponRoot.localPosition = new Vector3(.33f, -.27f, .57f);
        Box("Blaster body", Vector3.zero, new Vector3(.19f, .2f, .58f), weaponMat, weaponRoot);
        Box("Blaster top rail", new Vector3(0, .11f, .01f), new Vector3(.09f, .035f, .48f), orange, weaponRoot);
        Box("Blaster muzzle", new Vector3(0, -.015f, .34f), new Vector3(.12f, .12f, .18f), darkMetal, weaponRoot);
    }

    void BuildHud()
    {
        GameObject canvasObject = new GameObject("HUD - landscape");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        if (EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        Panel(canvas.transform, "Top information plate", new Vector2(.5f, 1), new Vector2(570, 108), new Color(.025f, .045f, .065f, .88f), new Vector2(0, -60));
        objectiveLabel = Label(canvas.transform, "ENTRENAMIENTO   /   OBJETIVOS  00", new Vector2(.5f, 1), new Vector2(520, 35), 25, new Color(.92f, .96f, 1), TextAnchor.MiddleCenter, new Vector2(0, -42));
        ammoLabel = Label(canvas.transform, "08  /  48", new Vector2(1, 1), new Vector2(260, 70), 34, Color.white, TextAnchor.MiddleCenter, new Vector2(-170, -58));
        Label(canvas.transform, "BALAS", new Vector2(1, 1), new Vector2(100, 25), 15, new Color(.62f, .72f, .8f), TextAnchor.MiddleCenter, new Vector2(-170, -20));

        GameObject stickBase = Panel(canvas.transform, "Move joystick", new Vector2(0, 0), new Vector2(190, 190), new Color(.035f, .07f, .1f, .5f), new Vector2(145, 140));
        Image stickImage = stickBase.GetComponent<Image>();
        stickImage.raycastTarget = true;
        GameObject thumb = Panel(stickBase.transform, "Joystick thumb", new Vector2(.5f, .5f), new Vector2(76, 76), new Color(.95f, .98f, 1f, .78f), Vector2.zero);
        thumb.GetComponent<Image>().raycastTarget = false;
        stick = stickBase.AddComponent<MobileStick>();
        stick.Initialize(thumb.GetComponent<RectTransform>(), 53);
        Label(canvas.transform, "MOVE", new Vector2(0, 0), new Vector2(120, 28), 16, new Color(.76f, .84f, .9f), TextAnchor.MiddleCenter, new Vector2(145, 32));

        AddButton(canvas.transform, "FIRE", new Vector2(1, 0), new Vector2(150, 150), new Color(.94f, .22f, .075f, .96f), new Vector2(-135, 148), Shoot, 27);
        AddButton(canvas.transform, "RECARGAR", new Vector2(1, 0), new Vector2(150, 66), new Color(.09f, .19f, .27f, .96f), new Vector2(-135, 45), Reload, 18);
        AddButton(canvas.transform, "MUÑECO", new Vector2(1, 0), new Vector2(150, 55), new Color(.11f, .27f, .24f, .95f), new Vector2(-310, 45), SpawnExtraDummy, 16);

        GameObject reticleObject = Panel(canvas.transform, "Aim reticle", new Vector2(.5f, .5f), new Vector2(16, 16), new Color(1, .8f, .45f, .95f), Vector2.zero);
        reticleObject.GetComponent<Image>().raycastTarget = false;
        reticle = reticleObject.GetComponent<RectTransform>();
        Panel(canvas.transform, "Reticle hole", new Vector2(.5f, .5f), new Vector2(5, 5), new Color(.04f, .055f, .07f, 1), Vector2.zero).GetComponent<Image>().raycastTarget = false;
        messageLabel = Label(canvas.transform, "", new Vector2(.5f, .25f), new Vector2(520, 52), 20, Color.white, TextAnchor.MiddleCenter, Vector2.zero);
    }

    GameObject Panel(Transform parent, string name, Vector2 anchor, Vector2 size, Color color, Vector2 offset)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
        Image image = go.AddComponent<Image>();
        image.color = color;
        return go;
    }

    Text Label(Transform parent, string text, Vector2 anchor, Vector2 size, int fontSize, Color color, TextAnchor alignment, Vector2 offset)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
        Text t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.color = color;
        t.alignment = alignment;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void AddButton(Transform parent, string title, Vector2 anchor, Vector2 size, Color color, Vector2 offset, UnityEngine.Events.UnityAction action, int fontSize)
    {
        GameObject go = Panel(parent, title + " button", anchor, size, color, offset);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        button.onClick.AddListener(action);
        Label(go.transform, title, new Vector2(.5f, .5f), size - new Vector2(14, 12), fontSize, Color.white, TextAnchor.MiddleCenter, Vector2.zero);
    }

    void BuildAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0;
        shotClip = Tone(115, .085f);
        reloadClip = Tone(420, .15f);
        hitClip = Tone(760, .075f);
    }

    AudioClip Tone(float frequency, float duration)
    {
        int sampleRate = 22050, count = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float envelope = 1f - (float)i / count;
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / sampleRate) * envelope * .32f;
        }
        AudioClip clip = AudioClip.Create("Generated SFX", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void Update()
    {
        if (controller == null || viewCamera == null) return;
        Vector2 input = stick != null ? stick.Value : Vector2.zero;
        float keyboardX = Input.GetAxisRaw("Horizontal"), keyboardY = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(keyboardX) + Mathf.Abs(keyboardY) > .01f) input = new Vector2(keyboardX, keyboardY).normalized;
        Vector3 motion = (controller.transform.forward * input.y + controller.transform.right * input.x) * 4.4f;
        if (controller.isGrounded) verticalVelocity = -1.5f;
        else verticalVelocity -= 18f * Time.deltaTime;
        motion.y = verticalVelocity;
        controller.Move(motion * Time.deltaTime);
        UpdateTouchLook();
        if (Input.GetMouseButton(0) && Input.mousePosition.x > Screen.width * .5f)
        {
            transform.Rotate(0, Input.GetAxis("Mouse X") * 2.1f, 0);
            RotatePitch(-Input.GetAxis("Mouse Y") * 2.1f);
        }
        if (Input.GetKeyDown(KeyCode.Space)) Shoot();
        if (Input.GetKeyDown(KeyCode.R)) Reload();
        if (isReloading && Time.time >= reloadEnds)
        {
            int needed = Mathf.Min(8 - ammo, reserve);
            ammo += needed; reserve -= needed; isReloading = false; UpdateHud();
        }
        if (messageLabel != null && Time.time > messageEnds) messageLabel.text = "";
    }

    void UpdateTouchLook()
    {
        if (Input.touchCount == 0) { lookFinger = -1; return; }
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.position.x < Screen.width * .45f) continue;
            if (t.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId)) continue;
                lookFinger = t.fingerId; lookLast = t.position;
            }
            else if (t.fingerId == lookFinger && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary))
            {
                Vector2 delta = t.position - lookLast;
                lookLast = t.position;
                transform.Rotate(0, delta.x * .13f, 0);
                RotatePitch(-delta.y * .13f);
            }
            else if (t.fingerId == lookFinger && (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)) lookFinger = -1;
        }
    }

    void RotatePitch(float amount)
    {
        Vector3 angles = viewCamera.transform.localEulerAngles;
        float pitch = angles.x > 180 ? angles.x - 360 : angles.x;
        pitch = Mathf.Clamp(pitch + amount, -78, 78);
        viewCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
    }

    void Shoot()
    {
        if (Time.time < nextShot || isReloading) return;
        nextShot = Time.time + .24f;
        if (ammo <= 0) { Reload(); return; }
        ammo--; UpdateHud();
        if (audioSource != null) audioSource.PlayOneShot(shotClip);
        if (weaponRoot != null) { weaponRoot.localPosition = new Vector3(.33f, -.29f, .49f); CancelInvoke(nameof(ResetWeapon)); Invoke(nameof(ResetWeapon), .07f); }
        if (Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward, out RaycastHit hit, 90))
        {
            PracticeDummy dummy = hit.collider.GetComponentInParent<PracticeDummy>();
            if (dummy != null) { dummy.Hit(hit.point, viewCamera.transform.forward); if (audioSource != null) audioSource.PlayOneShot(hitClip); }
        }
    }

    void ResetWeapon() { if (weaponRoot != null) weaponRoot.localPosition = new Vector3(.33f, -.27f, .57f); }

    void Reload()
    {
        if (isReloading || ammo >= 8 || reserve <= 0) return;
        isReloading = true; reloadEnds = Time.time + 1.05f;
        if (audioSource != null) audioSource.PlayOneShot(reloadClip);
        UpdateHud();
    }

    void UpdateHud()
    {
        if (objectiveLabel != null) objectiveLabel.text = "ENTRENAMIENTO   /   OBJETIVOS  " + targetsDown.ToString("00");
        if (ammoLabel != null) ammoLabel.text = isReloading ? "RECARGANDO" : ammo.ToString("00") + "  /  " + reserve.ToString("00");
    }

    public void RegisterTargetDown()
    {
        targetsDown++;
        UpdateHud();
        ShowMessage("OBJETIVO DERRIBADO");
    }

    void SpawnExtraDummy()
    {
        float x = Random.Range(-7f, 7f);
        SpawnDummy(new Vector3(x, 0, Random.Range(6f, 10f)));
        ShowMessage("OBJETIVO DE PRÁCTICA CREADO");
    }

    void ShowMessage(string text)
    {
        if (messageLabel == null) return;
        messageLabel.text = text; messageEnds = Time.time + 1.6f;
    }

    void SpawnDummy(Vector3 position)
    {
        GameObject root = new GameObject("Practice mannequin");
        root.transform.position = position;
        PracticeDummy dummy = root.AddComponent<PracticeDummy>();
        Material mannequin = new Material(targetMat);
        Rigidbody torso = MakePart(root.transform, "Torso", new Vector3(0, 1.17f, 0), new Vector3(.42f, .62f, .26f), mannequin, true);
        Rigidbody head = MakePart(root.transform, "Head", new Vector3(0, 1.68f, 0), new Vector3(.31f, .31f, .31f), mannequin, false);
        Rigidbody pelvis = MakePart(root.transform, "Pelvis", new Vector3(0, .72f, 0), new Vector3(.38f, .28f, .25f), mannequin, true);
        Rigidbody leftArm = MakePart(root.transform, "Left arm", new Vector3(-.36f, 1.18f, 0), new Vector3(.19f, .48f, .19f), mannequin, false);
        Rigidbody rightArm = MakePart(root.transform, "Right arm", new Vector3(.36f, 1.18f, 0), new Vector3(.19f, .48f, .19f), mannequin, false);
        Rigidbody leftLeg = MakePart(root.transform, "Left leg", new Vector3(-.13f, .32f, 0), new Vector3(.22f, .62f, .24f), mannequin, false);
        Rigidbody rightLeg = MakePart(root.transform, "Right leg", new Vector3(.13f, .32f, 0), new Vector3(.22f, .62f, .24f), mannequin, false);
        Connect(head, torso, root.transform.TransformPoint(new Vector3(0, 1.45f, 0)));
        Connect(pelvis, torso, root.transform.TransformPoint(new Vector3(0, .88f, 0)));
        Connect(leftArm, torso, root.transform.TransformPoint(new Vector3(-.24f, 1.35f, 0)));
        Connect(rightArm, torso, root.transform.TransformPoint(new Vector3(.24f, 1.35f, 0)));
        Connect(leftLeg, pelvis, root.transform.TransformPoint(new Vector3(-.13f, .58f, 0)));
        Connect(rightLeg, pelvis, root.transform.TransformPoint(new Vector3(.13f, .58f, 0)));
        dummy.SetParts(new Rigidbody[] { torso, head, pelvis, leftArm, rightArm, leftLeg, rightLeg });
        dummies.Add(dummy);
    }

    Rigidbody MakePart(Transform root, string name, Vector3 localPosition, Vector3 size, Material material, bool box)
    {
        GameObject part = GameObject.CreatePrimitive(box ? PrimitiveType.Cube : PrimitiveType.Capsule);
        part.name = name;
        part.transform.SetParent(root, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = size;
        if (name == "Left arm") part.transform.localRotation = Quaternion.Euler(0, 0, -28);
        else if (name == "Right arm") part.transform.localRotation = Quaternion.Euler(0, 0, 28);
        else if (name == "Left leg") part.transform.localRotation = Quaternion.Euler(0, 0, 5);
        else if (name == "Right leg") part.transform.localRotation = Quaternion.Euler(0, 0, -5);
        part.GetComponent<Renderer>().sharedMaterial = material;
        Rigidbody body = part.AddComponent<Rigidbody>();
        body.mass = name == "Torso" ? 3 : 1;
        body.isKinematic = true;
        return body;
    }

    void Connect(Rigidbody child, Rigidbody parent, Vector3 worldJoint)
    {
        CharacterJoint joint = child.gameObject.AddComponent<CharacterJoint>();
        joint.connectedBody = parent;
        joint.enableProjection = true;
        joint.projectionDistance = .08f;
        joint.anchor = child.transform.InverseTransformPoint(worldJoint);
        joint.connectedAnchor = parent.transform.InverseTransformPoint(worldJoint);
    }

    void ShowStartupError(System.Exception e)
    {
        if (viewCamera == null)
        {
            GameObject cameraObject = new GameObject("Startup fallback camera");
            viewCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            viewCamera.clearFlags = CameraClearFlags.SolidColor;
            viewCamera.backgroundColor = new Color(.06f, .09f, .13f);
        }
        GameObject canvasObject = new GameObject("Startup error screen");
        Canvas canvas = canvasObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();
        Text text = Label(canvas.transform, "NO SE PUDO INICIAR LA ARENA\n" + e.GetType().Name + ": " + e.Message,
            new Vector2(.5f, .5f), new Vector2(1100, 240), 28, Color.white, TextAnchor.MiddleCenter, Vector2.zero);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
    }
}

public class MobileStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    RectTransform thumb;
    float radius = 50;
    public Vector2 Value { get; private set; }
    public void Initialize(RectTransform knob, float maxRadius) { thumb = knob; radius = maxRadius; }
    public void OnPointerDown(PointerEventData e) { OnDrag(e); }
    public void OnDrag(PointerEventData e)
    {
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out local);
        Value = Vector2.ClampMagnitude(local / radius, 1);
        if (thumb != null) thumb.anchoredPosition = Value * radius;
    }
    public void OnPointerUp(PointerEventData e) { Value = Vector2.zero; if (thumb != null) thumb.anchoredPosition = Vector2.zero; }
}

public class PracticeDummy : MonoBehaviour
{
    Rigidbody[] bodies;
    bool toppled;
    public void SetParts(Rigidbody[] parts) { bodies = parts; }
    public void Hit(Vector3 point, Vector3 direction)
    {
        if (toppled || bodies == null) return;
        toppled = true;
        foreach (Rigidbody body in bodies)
        {
            body.isKinematic = false;
            body.AddForceAtPosition(direction * 4.2f, point, ForceMode.Impulse);
        }
        if (FpsArenaGame.I != null) FpsArenaGame.I.RegisterTargetDown();
    }
}

