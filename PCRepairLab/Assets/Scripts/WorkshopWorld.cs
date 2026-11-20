using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PcFixLab
{
    public sealed class FittedWorldLabel : MonoBehaviour
    {
        public float LineHeight;
        TextMesh textMesh;
        string lastText;
        int remainingFrames=4;
        void LateUpdate()
        {
            if(textMesh==null)textMesh=GetComponent<TextMesh>();
            if(textMesh.text!=lastText){lastText=textMesh.text;remainingFrames=4;}
            if(remainingFrames<=0 || string.IsNullOrEmpty(lastText))return;
            remainingFrames--;
            var bounds=GetComponent<Renderer>().localBounds;
            if(bounds.size.y>.0001f)
            {
                int lines=lastText.Split('\n').Length;
                transform.localScale=Vector3.one*(LineHeight*lines/bounds.size.y);
            }
        }
    }

    public abstract class WorkshopCharacter : Interactable
    {
        public string CharacterName { get; protected set; }
        public abstract string Speak();
        public override string DisplayName => CharacterName;
        public override string Description => Speak();
    }

    public sealed class CustomerCharacter : WorkshopCharacter
    {
        Transform head, leftArm, rightArm;
        Renderer shirt;
        string dialogue = "Bienvenido al taller. Cada reparación empieza con un buen diagnóstico.";
        float celebration;
        public void Initialize(Transform headTransform, Transform armA, Transform armB, Renderer shirtRenderer)
        { head = headTransform; leftArm = armA; rightArm = armB; shirt = shirtRenderer; CharacterName = "LIZ · ASISTENTE"; }
        public void Configure(RepairOrder order)
        { CharacterName = order.ClientName; dialogue = order.Story; shirt.material.color = order.ClientColor; }
        public void Celebrate() { celebration = 4f; }
        public override string Speak() => dialogue;
        void Update()
        {
            if (head == null) return;
            float t = Time.time;
            head.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.3f) * 2, Mathf.Sin(t * .65f) * 7, 0);
            celebration = Mathf.Max(0, celebration - Time.deltaTime);
            rightArm.localRotation = Quaternion.Euler(0, 0, celebration > 0 ? -125 + Mathf.Sin(t * 9) * 18 : -8 + Mathf.Sin(t * 1.2f) * 3);
            leftArm.localRotation = Quaternion.Euler(0, 0, 8 + Mathf.Sin(t) * 3);
        }
    }

    public sealed class WorkshopWorld : MonoBehaviour
    {
        public Camera SceneCamera { get; private set; }
        public CaseCover Cover { get; private set; }
        public PowerConnector Power { get; private set; }
        public CustomerCharacter Customer { get; private set; }
        public TestMonitor Monitor { get; private set; }
        public Dictionary<PartKind, RepairablePart> Parts { get; } = new Dictionary<PartKind, RepairablePart>();
        public Transform FanRotor { get; private set; }
        public Transform Dust { get; private set; }
        public Transform Paste { get; private set; }
        public Vector3 CameraTarget => new Vector3(.15f, 1.18f, .05f);
        Transform coverPivot, plug, coolingRotor;
        TextMesh monitorText;
        Renderer monitorScreen, led;
        Material dark, black, metal, teal, green, gold, orange, wood, white, lightGray, rubber;
        readonly List<Material> ownedMaterials = new List<Material>();
        readonly Dictionary<RepairablePart, Vector3> targetPositions = new Dictionary<RepairablePart, Vector3>();
        bool coverOpen = true, plugged = true, fanRunning;
        Font labelFont;
        Transform[] environments;
        WorldKind activeWorld;

        public void Build()
        {
            labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dark = Mat("Powder coated graphite", new Color(.055f,.081f,.10f), .35f,.38f);
            black = Mat("Black polymer", new Color(.018f,.025f,.032f), .1f,.3f);
            metal = Mat("Brushed aluminium", new Color(.48f,.56f,.60f), .7f,.55f);
            teal = Mat("Workshop mint", new Color(.19f,.73f,.64f), .15f,.4f);
            green = Mat("PCB solder mask", new Color(.035f,.20f,.15f), .18f,.3f);
            gold = Mat("Copper contacts", new Color(.78f,.56f,.19f), .65f,.5f);
            orange = Mat("Safety orange", new Color(.98f,.43f,.20f), .05f,.3f);
            wood = Mat("Warm oak", new Color(.39f,.23f,.13f), 0,.32f);
            white = Mat("Warm white", new Color(.82f,.85f,.81f), 0,.35f);
            lightGray = Mat("Light aluminium", new Color(.67f,.73f,.73f), .5f,.5f);
            rubber = Mat("Rubber mat", new Color(.035f,.12f,.125f), 0,.1f);
            BuildLighting(); BuildRoom(); BuildDesk(); BuildComputer(); BuildTools(); BuildMonitor(); BuildCustomer();BuildEnvironments();
            var cameraObject = new GameObject("Camera · orbital de taller");
            cameraObject.transform.SetParent(transform,false);
            SceneCamera = cameraObject.AddComponent<Camera>();
            SceneCamera.fieldOfView = 42; SceneCamera.nearClipPlane = .05f; SceneCamera.farClipPlane = 45;
            SceneCamera.backgroundColor = new Color(.055f,.085f,.10f);
            SceneCamera.clearFlags = CameraClearFlags.SolidColor;
            SceneCamera.allowHDR = false; SceneCamera.allowMSAA = true;
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
            SetMonitor("U-iPC\nESTACIÓN DE PRUEBAS\n\nLISTA PARA TRABAJAR", true);
        }

        Material Mat(string name, Color color, float metallic = 0, float smooth = .35f)
        {
            var template = Resources.Load<Material>("WorkshopStandard");
            var m = template != null ? new Material(template) : new Material(Shader.Find("Standard"));
            m.name = name; m.color = color;
            m.enableInstancing = true;
            m.SetFloat("_Metallic", metallic); m.SetFloat("_Glossiness", smooth);
            ownedMaterials.Add(m); return m;
        }
        GameObject Root(string name, Transform parent, Vector3 p)
        { var o = new GameObject(name); o.transform.SetParent(parent, false); o.transform.localPosition = p; return o; }
        GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 p, Vector3 scale, Material material, Vector3? angles = null)
        {
            var o = GameObject.CreatePrimitive(type); o.name = name; o.transform.SetParent(parent, false);
            o.transform.localPosition = p; o.transform.localScale = scale;
            if (angles.HasValue) o.transform.localRotation = Quaternion.Euler(angles.Value);
            Destroy(o.GetComponent<Collider>());
            o.GetComponent<Renderer>().sharedMaterial = material;
            return o;
        }
        GameObject Box(string name, Transform parent, Vector3 p, Vector3 size, Material mat, bool bevel = false)
        {
            if (!bevel) return Shape(name, PrimitiveType.Cube, parent, p, size, mat);
            var o = Root(name, parent, p);
            o.AddComponent<MeshFilter>().sharedMesh = BeveledBox(size, Mathf.Min(.035f, Mathf.Min(size.x, Mathf.Min(size.y,size.z)) * .22f));
            o.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return o;
        }
        void ColliderFor(GameObject obj, Vector3 center, Vector3 size)
        { var c = obj.AddComponent<BoxCollider>(); c.center = center; c.size = size; }
        TextMesh Label(string text, Transform parent, Vector3 p, float size, Color color, Vector3? angles = null)
        {
            var o = Root("Label · " + text.Replace('\n',' '), parent, p);
            if (angles.HasValue) o.transform.localRotation = Quaternion.Euler(angles.Value);
            var t = o.AddComponent<TextMesh>(); t.text = text; t.font = labelFont; t.fontSize = 64;
            t.characterSize = size; t.color = color; t.anchor = TextAnchor.MiddleCenter; t.alignment = TextAlignment.Center;
            o.GetComponent<Renderer>().sharedMaterial = labelFont.material;
            o.AddComponent<FittedWorldLabel>().LineHeight=size;
            return t;
        }
        void BuildLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.47f,.58f,.65f);
            RenderSettings.ambientEquatorColor = new Color(.31f,.38f,.40f);
            RenderSettings.ambientGroundColor = new Color(.12f,.13f,.14f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.065f,.10f,.12f); RenderSettings.fogStartDistance = 12; RenderSettings.fogEndDistance = 28;
            var key = Root("Key · soft workshop daylight", transform, Vector3.zero).AddComponent<Light>();
            key.type = LightType.Directional; key.color = new Color(1,.89f,.75f); key.intensity = 1.2f;
            key.transform.rotation = Quaternion.Euler(48,-28,0); key.shadows = LightShadows.Soft;
            key.shadowStrength = .65f; key.shadowBias = .04f; key.shadowNormalBias = .2f;
            var fill = Root("Fill · cool rim", transform, new Vector3(0,4,3)).AddComponent<Light>();
            fill.type = LightType.Point; fill.color = new Color(.43f,.78f,.85f); fill.intensity = 3; fill.range = 9; fill.shadows = LightShadows.None;
            QualitySettings.antiAliasing = 4; QualitySettings.shadowDistance = 14;
            QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.vSyncCount = 1; Application.targetFrameRate = 60;
        }
        void BuildRoom()
        {
            var floor = Mat("Floor", new Color(.085f,.12f,.14f), .05f,.25f);
            Box("Workshop floor", transform, new Vector3(0,-.1f,0), new Vector3(24,.2f,24), floor);
            var wall = Mat("Workshop wall", new Color(.11f,.18f,.205f));
            Box("Back wall", transform, new Vector3(0,2.5f,3.3f), new Vector3(16,5,.12f), wall);
            Box("Wall strip", transform, new Vector3(0,.20f,3.2f), new Vector3(16,.24f,.07f), dark);
            Box("Peg board", transform, new Vector3(-.4f,2.05f,3.15f), new Vector3(3.8f,1.25f,.12f), dark, true);
            for (int x = 0; x < 17; x++) for (int y = 0; y < 5; y++)
                Box("Peg hole", transform, new Vector3(-2.0f+x*.2f,1.65f+y*.2f,3.08f), new Vector3(.025f,.025f,.01f), metal);
            Box("Shelf", transform, new Vector3(-3.0f,1.7f,2.9f), new Vector3(1.5f,.08f,.5f), wood, true);
            for (int i=0;i<3;i++) Box("Spare parts box",transform,new Vector3(-3.5f+i*.45f,1.95f,2.9f),new Vector3(.38f,.40f,.38f),i==1?teal:dark,true);
            Label("U-iPC", transform, new Vector3(.4f,3.12f,3.12f), .15f, new Color(.38f,.84f,.75f));
            Label("REPARAR  ·  APRENDER  ·  VOLVER A EMPEZAR",transform,new Vector3(.4f,2.78f,3.11f),.034f, new Color(.6f,.72f,.73f));
        }
        void BuildDesk()
        {
            Box("Workbench oak top",transform,new Vector3(0,.87f,0),new Vector3(6.3f,.18f,3.6f),wood,true);
            Box("Desk front edge",transform,new Vector3(0,.85f,-1.79f),new Vector3(6.2f,.08f,.035f),gold,true);
            foreach(float x in new[]{-2.65f,2.65f}) foreach(float z in new[]{-1.3f,1.3f})
                Box("Steel desk leg",transform,new Vector3(x,.4f,z),new Vector3(.13f,.8f,.13f),dark,true);
            Box("Anti-static mat",transform,new Vector3(.0f,.969f,-.06f),new Vector3(3.5f,.018f,2.25f),rubber,true);
            for(int i=0;i<15;i++) Box("Mat grid",transform,new Vector3(-1.65f+i*.23f,.980f,-.06f),new Vector3(.006f,.001f,2.15f),green);
            for(int i=0;i<10;i++) Box("Mat grid",transform,new Vector3(0,.981f,-1.09f+i*.23f),new Vector3(3.38f,.001f,.006f),green);
            Box("Bench front badge",transform,new Vector3(-2.35f,.85f,-1.812f),new Vector3(.65f,.10f,.015f),dark,true);
            Label("FIX / 01",transform,new Vector3(-2.35f,.85f,-1.826f),.036f,Color.white);
        }
        void BuildComputer()
        {
            var chassis = Root("PC · modular service chassis",transform,new Vector3(.1f,1.025f,.1f)).transform;
            Box("Chassis base",chassis,Vector3.zero,new Vector3(2.65f,.09f,1.56f),dark,true);
            Box("Back panel",chassis,new Vector3(0,.31f,.77f),new Vector3(2.65f,.59f,.055f),dark,true);
            Box("Front panel",chassis,new Vector3(0,.31f,-.77f),new Vector3(2.65f,.59f,.055f),dark,true);
            Box("Left panel",chassis,new Vector3(-1.3f,.31f,0),new Vector3(.055f,.59f,1.56f),black,true);
            Box("Right panel",chassis,new Vector3(1.3f,.31f,0),new Vector3(.055f,.59f,1.56f),dark,true);
            for(int i=0;i<17;i++) Box("Vent slot",chassis,new Vector3(-1.335f,.16f+i*.02f,0),new Vector3(.008f,.008f,1.1f),metal);
            for(int i=0;i<10;i++) Box("Front intake",chassis,new Vector3(.6f+i*.04f,.28f,-.805f),new Vector3(.017f,.25f,.008f),black);
            led=Shape("Power LED",PrimitiveType.Sphere,chassis,new Vector3(-1.02f,.3f,-.807f),new Vector3(.035f,.035f,.012f),teal).GetComponent<Renderer>();
            Label("NOVA / S1",chassis,new Vector3(-.55f,.30f,-.806f),.032f,new Color(.7f,.77f,.76f));
            var board=Root("Motherboard · decorative base",chassis,new Vector3(-.33f,.105f,0)).transform;
            Box("PCB",board,Vector3.zero,new Vector3(1.72f,.035f,1.35f),green,true);
            for(int i=0;i<18;i++)
            {
                float z=-.60f+(i%9)*.15f;
                Box("Copper trace",board,new Vector3(-.73f+(i/9)*1.15f,.021f,z),new Vector3(.31f,.002f,.008f),gold);
            }
            for(int i=0;i<7;i++)
            {
                Shape("Capacitor",PrimitiveType.Cylinder,board,new Vector3(-.65f+i*.16f,.065f,.55f),new Vector3(.055f,.045f,.055f),metal);
                Box("VRM chip",board,new Vector3(-.7f+i*.11f,.041f,-.53f),new Vector3(.07f,.035f,.07f),black);
            }
            Box("CPU socket",board,new Vector3(-.22f,.026f,.02f),new Vector3(.55f,.025f,.55f),black,true);
            Box("Processor",board,new Vector3(-.22f,.055f,.02f),new Vector3(.38f,.04f,.38f),metal,true);
            var paste=Root("Thermal paste · clean CPU",board,new Vector3(-.22f,.079f,.02f));
            Shape("Paste dot",PrimitiveType.Sphere,paste.transform,Vector3.zero,new Vector3(.17f,.009f,.17f),lightGray); Paste=paste.transform; paste.SetActive(false);
            var cooling=Root("Cooling assembly",transform,chassis.TransformPoint(new Vector3(-.55f,.16f,.02f)));
            Parts[PartKind.Cooling]=cooling.AddComponent<CoolingAssembly>();
            for(int i=0;i<12;i++) Box("Heatsink fin",cooling.transform,new Vector3(-.22f+i*.04f,.13f,0),new Vector3(.016f,.25f,.48f),metal);
            Box("Fan frame",cooling.transform,new Vector3(0,.28f,0),new Vector3(.55f,.055f,.55f),black,true);
            coolingRotor=Root("CPU fan rotor",cooling.transform,new Vector3(0,.315f,0)).transform; FanRotor=coolingRotor;
            MakeFan(coolingRotor,.46f); ColliderFor(cooling,new Vector3(0,.16f,0),new Vector3(.60f,.38f,.60f));
            var dust=Root("Dust deposits",cooling.transform,Vector3.zero); Dust=dust.transform;
            var dirt=Mat("Dust",new Color(.42f,.33f,.20f),0,.0f);
            var random=new System.Random(71);
            for(int i=0;i<30;i++) Shape("Dust",PrimitiveType.Sphere,dust.transform,new Vector3((float)random.NextDouble()*.46f-.23f,.354f,(float)random.NextDouble()*.46f-.23f),new Vector3(.05f,.012f,.04f),dirt);
            dust.SetActive(false);
            var ram=Root("RAM · service module",transform,chassis.TransformPoint(new Vector3(.30f,.135f,0)));
            Parts[PartKind.Memory]=ram.AddComponent<MemoryModule>(); MakeRam(ram.transform,Vector3.zero);
            ColliderFor(ram,new Vector3(0,.13f,0),new Vector3(.15f,.30f,1.04f));
            MakeRam(chassis,new Vector3(.50f,.135f,0));
            Box("PCIe socket",board,new Vector3(-.08f,.05f,-.39f),new Vector3(.95f,.04f,.065f),black,true);
            Box("Chipset",board,new Vector3(.48f,.055f,-.25f),new Vector3(.24f,.07f,.25f),metal,true);
            Label("NOVA / B4",board,new Vector3(-.28f,.025f,-.32f),.025f,white.color,new Vector3(90,0,0));
            var ssd=Root("SSD · service drive",transform,chassis.TransformPoint(new Vector3(.99f,.11f,-.28f)));
            Parts[PartKind.Storage]=ssd.AddComponent<StorageDrive>();
            Box("SSD case",ssd.transform,Vector3.zero,new Vector3(.46f,.10f,.73f),black,true);
            Box("SSD top label",ssd.transform,new Vector3(0,.055f,0),new Vector3(.38f,.006f,.57f),metal,true);
            Box("SSD brand stripe",ssd.transform,new Vector3(0,.06f,-.18f),new Vector3(.38f,.006f,.085f),teal);
            Label("NOVA\nSSD / 480",ssd.transform,new Vector3(0,.061f,.03f),.033f,black.color,new Vector3(90,0,0));
            ColliderFor(ssd,Vector3.zero,new Vector3(.49f,.14f,.76f));
            var psu=Root("PSU · service unit",transform,chassis.TransformPoint(new Vector3(.98f,.19f,.45f)));
            Parts[PartKind.PowerSupply]=psu.AddComponent<PowerSupplyUnit>();
            Box("PSU",psu.transform,Vector3.zero,new Vector3(.53f,.36f,.48f),dark,true);
            for(int i=0;i<7;i++)Box("PSU grille",psu.transform,new Vector3(-.21f+i*.065f,.188f,0),new Vector3(.018f,.01f,.36f),metal);
            ColliderFor(psu,Vector3.zero,new Vector3(.56f,.40f,.50f));
            var gpu=Root("GPU · PCIe expansion card",transform,chassis.TransformPoint(new Vector3(-.28f,.20f,-.42f)));
            Parts[PartKind.Graphics]=gpu.AddComponent<GraphicsCard>();
            Box("GPU board",gpu.transform,Vector3.zero,new Vector3(1.04f,.045f,.30f),green,true);
            Box("GPU cooler",gpu.transform,new Vector3(0,.065f,0),new Vector3(.96f,.09f,.27f),dark,true);
            foreach(float x in new[]{-.25f,.25f}){var rotor=Root("GPU fan",gpu.transform,new Vector3(x,.12f,0)).transform;MakeFan(rotor,.24f);}
            Box("GPU bracket",gpu.transform,new Vector3(-.55f,.045f,0),new Vector3(.035f,.16f,.35f),metal,true);
            ColliderFor(gpu,new Vector3(0,.06f,0),new Vector3(1.12f,.22f,.34f));
            Cable("SATA cable",chassis,new[]{new Vector3(.95f,.10f,-.61f),new Vector3(.8f,.09f,-.65f),new Vector3(.35f,.08f,-.65f),new Vector3(.15f,.12f,-.55f)},orange,.018f);
            Cable("Main power harness",chassis,new[]{new Vector3(.9f,.22f,.24f),new Vector3(.76f,.16f,.19f),new Vector3(.68f,.09f,-.20f),new Vector3(.62f,.14f,-.43f)},black,.04f);
            coverPivot=Root("Cover hinge",chassis,new Vector3(0,.625f,.78f)).transform;
            var cover=Root("Removable access panel",coverPivot,Vector3.zero); Cover=cover.AddComponent<CaseCover>();
            Box("Access panel",cover.transform,new Vector3(0,0,-.78f),new Vector3(2.69f,.035f,1.58f),dark,true);
            for(int i=0;i<12;i++) Box("Lid ventilation",cover.transform,new Vector3(-.83f+i*.15f,.020f,-.77f),new Vector3(.045f,.008f,.8f),black,true);
            Box("Panel mint accent",cover.transform,new Vector3(0,.024f,-1.38f),new Vector3(2.32f,.008f,.025f),teal);
            Label("U-iPC",cover.transform,new Vector3(0,-.026f,-.76f),.10f,new Color(.30f,.49f,.48f),new Vector3(-90,0,0));
            ColliderFor(cover,new Vector3(0,0,-.78f),new Vector3(2.69f,.05f,1.58f));
            coverPivot.localRotation=Quaternion.Euler(103,0,0);
            var power=Root("Power connector · interactive",transform,new Vector3(1.60f,1.10f,.66f)); Power=power.AddComponent<PowerConnector>();
            plug=power.transform; Box("Power plug",power.transform,Vector3.zero,new Vector3(.20f,.13f,.16f),black,true);
            Box("Power plug indicator",power.transform,new Vector3(0,.07f,0),new Vector3(.12f,.009f,.04f),orange);
            ColliderFor(power,Vector3.zero,new Vector3(.29f,.23f,.24f));
            Cable("Mains cord",transform,new[]{new Vector3(1.64f,1.08f,.66f),new Vector3(1.9f,1.03f,.75f),new Vector3(2.12f,.99f,.12f),new Vector3(2.2f,.99f,-.6f),new Vector3(2.9f,.98f,-.8f)},black,.025f);
        }
        void MakeRam(Transform parent,Vector3 p)
        {
            var r=Root("DDR4 geometry",parent,p).transform;
            Box("RAM PCB",r,new Vector3(0,.12f,0),new Vector3(.035f,.23f,.97f),green,true);
            for(int i=0;i<7;i++) foreach(float x in new[]{-.026f,.026f}) Box("Memory chip",r,new Vector3(x,.15f,-.37f+i*.12f),new Vector3(.019f,.12f,.095f),black,true);
            Box("RAM top edge",r,new Vector3(0,.25f,0),new Vector3(.06f,.025f,.99f),teal,true);
            for(int i=0;i<22;i++) Box("Gold contact",r,new Vector3(-.020f,.018f,-.44f+i*.042f),new Vector3(.007f,.035f,.022f),gold);
            foreach(float z in new[]{-.53f,.53f}) Box("Slot latch",r,new Vector3(0,.06f,z),new Vector3(.09f,.09f,.06f),white,true);
        }
        void MakeFan(Transform parent,float diameter)
        {
            Shape("Fan hub",PrimitiveType.Cylinder,parent,Vector3.zero,new Vector3(diameter*.25f,.022f,diameter*.25f),teal);
            for(int i=0;i<7;i++)
            {
                float a=i*Mathf.PI*2/7;
                var blade=Box("Fan blade",parent,new Vector3(Mathf.Sin(a)*diameter*.23f,0,Mathf.Cos(a)*diameter*.23f),new Vector3(diameter*.18f,.018f,diameter*.38f),dark,true);
                blade.transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg+32,0);
            }
        }
        void BuildTools()
        {
            var tray=Root("Tools and spare parts",transform,new Vector3(-2.25f,.99f,-.35f)).transform;
            Box("Tool tray",tray,Vector3.zero,new Vector3(1.15f,.05f,1.55f),dark,true);
            Box("Driver shaft",tray,new Vector3(-.25f,.07f,-.10f),new Vector3(.035f,.035f,.70f),metal,true);
            Box("Driver handle",tray,new Vector3(-.25f,.075f,.37f),new Vector3(.13f,.11f,.35f),orange,true);
            Shape("Thermal paste tube",PrimitiveType.Cylinder,tray,new Vector3(.15f,.09f,.20f),new Vector3(.10f,.25f,.10f),white,new Vector3(90,0,0));
            Box("Tube plunger",tray,new Vector3(.15f,.09f,.54f),new Vector3(.18f,.025f,.05f),teal,true);
            Box("Brush handle",tray,new Vector3(.38f,.08f,-.15f),new Vector3(.09f,.055f,.54f),wood,true);
            for(int i=0;i<8;i++) Box("Brush bristles",tray,new Vector3(.35f+(i%3)*.03f,.09f,-.47f),new Vector3(.018f,.07f,.13f),gold);
            Box("Parts bin",transform,new Vector3(.0f,1.0f,-1.40f),new Vector3(1.55f,.05f,.44f),dark,true);
            Label("PIEZAS / RECAMBIOS",transform,new Vector3(0,1.03f,-1.41f),.026f,new Color(.62f,.76f,.74f),new Vector3(90,0,0));
            var mug=Root("Coffee mug",transform,new Vector3(2.50f,1.05f,-1.05f)).transform;
            Shape("Cup",PrimitiveType.Cylinder,mug,new Vector3(0,.11f,0),new Vector3(.23f,.14f,.23f),white);
            Shape("Coffee",PrimitiveType.Cylinder,mug,new Vector3(0,.255f,0),new Vector3(.19f,.003f,.19f),wood);
        }
        void BuildMonitor()
        {
            var monitor=Root("Diagnostic monitor",transform,new Vector3(-2.10f,1.54f,1.05f)); Monitor=monitor.AddComponent<TestMonitor>();
            Box("Monitor foot",monitor.transform,new Vector3(0,-.51f,.04f),new Vector3(.54f,.045f,.36f),dark,true);
            Box("Monitor stem",monitor.transform,new Vector3(0,-.30f,.07f),new Vector3(.10f,.40f,.10f),metal,true);
            Box("Monitor frame",monitor.transform,Vector3.zero,new Vector3(1.12f,.72f,.075f),black,true);
            var screen=Mat("Monitor screen",new Color(.018f,.085f,.08f),0,.2f); screen.EnableKeyword("_EMISSION"); screen.SetColor("_EmissionColor",new Color(.015f,.06f,.05f));
            monitorScreen=Box("Screen",monitor.transform,new Vector3(0,.015f,-.041f),new Vector3(1.025f,.59f,.008f),screen,true).GetComponent<Renderer>();
            monitorText=Label("",monitor.transform,new Vector3(0,.015f,-.049f),.050f,new Color(.42f,.93f,.77f));
            Label("NOVA",monitor.transform,new Vector3(0,-.32f,-.045f),.018f,Color.gray);
            ColliderFor(monitor,Vector3.zero,new Vector3(1.12f,.72f,.11f));
        }
        void BuildCustomer()
        {
            var npc=Root("Character · customer",transform,new Vector3(2.35f,0,1.5f));
            Customer=npc.AddComponent<CustomerCharacter>();
            var skin=Mat("Character skin",new Color(.72f,.45f,.29f));
            var hair=Mat("Character hair",new Color(.08f,.045f,.035f));
            var shirt=Mat("Character shirt",new Color(.30f,.60f,.54f));
            var body=Shape("Shirt",PrimitiveType.Capsule,npc.transform,new Vector3(0,1.21f,0),new Vector3(.48f,.39f,.32f),shirt);
            foreach(float x in new[]{-.13f,.13f})
            {
                Shape("Trouser leg",PrimitiveType.Capsule,npc.transform,new Vector3(x,.50f,0),new Vector3(.19f,.29f,.21f),dark);
                Box("Shoe",npc.transform,new Vector3(x,.11f,-.065f),new Vector3(.21f,.14f,.36f),black,true);
            }
            var head=Root("Head pivot",npc.transform,new Vector3(0,1.90f,0)).transform;
            Shape("Head",PrimitiveType.Sphere,head,Vector3.zero,new Vector3(.43f,.50f,.40f),skin);
            Shape("Hair",PrimitiveType.Sphere,head,new Vector3(0,.15f,.03f),new Vector3(.45f,.27f,.39f),hair);
            foreach(float x in new[]{-.085f,.085f})
            {
                Shape("Eye white",PrimitiveType.Sphere,head,new Vector3(x,.015f,-.182f),new Vector3(.065f,.07f,.035f),white);
                Shape("Pupil",PrimitiveType.Sphere,head,new Vector3(x,.012f,-.202f),new Vector3(.029f,.038f,.012f),black);
                Box("Eyebrow",head,new Vector3(x,.077f,-.18f),new Vector3(.075f,.018f,.02f),hair,true);
            }
            Shape("Nose",PrimitiveType.Sphere,head,new Vector3(0,-.04f,-.21f),new Vector3(.07f,.08f,.08f),skin);
            Box("Smile",head,new Vector3(0,-.115f,-.185f),new Vector3(.10f,.018f,.018f),hair,true);
            var armA=Root("Left shoulder",npc.transform,new Vector3(-.30f,1.47f,0)).transform;
            var armB=Root("Right shoulder",npc.transform,new Vector3(.30f,1.47f,0)).transform;
            foreach(var arm in new[]{armA,armB})
            {
                Shape("Sleeve",PrimitiveType.Capsule,arm,new Vector3(0,-.13f,0),new Vector3(.18f,.17f,.19f),shirt);
                Shape("Forearm",PrimitiveType.Capsule,arm,new Vector3(0,-.36f,0),new Vector3(.13f,.15f,.14f),skin);
                Shape("Hand",PrimitiveType.Sphere,arm,new Vector3(0,-.53f,-.015f),new Vector3(.14f,.16f,.12f),skin);
            }
            Customer.Initialize(head,armA,armB,body.GetComponent<Renderer>());
            ColliderFor(npc,new Vector3(0,1.25f,0),new Vector3(.85f,1.65f,.55f));
        }
        void Cable(string name,Transform parent,Vector3[] points,Material mat,float width)
        {
            var o=Root(name,parent,Vector3.zero); var line=o.AddComponent<LineRenderer>();
            line.useWorldSpace=false; line.positionCount=points.Length; line.SetPositions(points); line.widthMultiplier=width;
            line.numCapVertices=4; line.numCornerVertices=4; line.sharedMaterial=mat; line.shadowCastingMode=ShadowCastingMode.Off;
        }
        public void SetCover(bool open,bool instant=false)
        { coverOpen=open; if(instant) coverPivot.localRotation=Quaternion.Euler(open?103:0,0,0); }
        public void SetPower(bool connected) { plugged=connected; }
        public void SetMonitor(string text,bool powered)
        { monitorText.text=text; monitorText.color=powered?new Color(.45f,.96f,.78f):new Color(.15f,.25f,.24f); fanRunning=powered; }
        public void StopFans() { fanRunning=false; }
        public void RefreshPart(RepairablePart part)
        {
            int i=(int)part.Kind;
            targetPositions[part]=part.IsInstalled?part.MountedPosition:new Vector3(-1.15f+i*.55f,1.09f,-1.44f);
            Dust.gameObject.SetActive(Parts[PartKind.Cooling].HasDust);
            Paste.gameObject.SetActive(!Parts[PartKind.Cooling].IsInstalled && !Parts[PartKind.Cooling].NeedsPaste);
        }
        public void Configure(RepairOrder order)
        {
            foreach(var pair in Parts)
            {
                pair.Value.transform.position=pair.Value.MountedPosition==Vector3.zero?pair.Value.transform.position:pair.Value.MountedPosition;
                pair.Value.Configure(pair.Key==order.Target); RefreshPart(pair.Value);
            }
            Customer.Configure(order); SetCover(false,true); SetPower(true);
            SetMonitor("EN ESPERA\n\nPulsa ENCENDER Y PROBAR",false);
        }
        public void InitializeParts()
        { foreach(var p in Parts.Values) p.Configure(false); }
        public void BindSession(SimulationSession session)
        {
            foreach(var p in Parts.Values){p.BindState(session.Parts[(int)p.Kind]);RefreshPart(p);}
            Customer.Configure(new RepairOrder(session.Contract));SetEnvironment(session.Contract.World);
            SetCover(session.CoverOpen,true);SetPower(session.Connected);
        }
        void BuildEnvironments()
        {
            environments=new Transform[3];
            for(int w=0;w<3;w++)environments[w]=Root("World · "+CampaignCatalog.WorldName((WorldKind)w),transform,Vector3.zero).transform;
            // Un solo taller activo. Las decoraciones no añaden simulación física ni texturas grandes.
            Label("TALLER DE BARRIO",environments[0],new Vector3(-.4f,2.3f,3.04f),.09f,white.color);
            for(int i=0;i<3;i++)
            {
                Box("Campus training PC",environments[1],new Vector3(-2.7f+i*1.5f,1.1f,2.7f),new Vector3(.48f,.65f,.48f),lightGray,true);
                Box("Campus desk",environments[1],new Vector3(-2.7f+i*1.5f,.73f,2.7f),new Vector3(1.20f,.08f,.8f),white,true);
            }
            Label("LABORATORIO  /  HARDWARE",environments[1],new Vector3(-.4f,2.35f,3.04f),.08f,teal.color);
            foreach(float x in new[]{-3.5f,3.5f})
            {
                Box("Service rack",environments[2],new Vector3(x,1.25f,2.6f),new Vector3(.85f,2.5f,.65f),black,true);
                for(int r=0;r<9;r++)
                {
                    Box("Rack drawer",environments[2],new Vector3(x,.25f+r*.25f,2.24f),new Vector3(.73f,.18f,.03f),metal,true);
                    Box("Rack LED",environments[2],new Vector3(x+.26f,.25f+r*.25f,2.21f),new Vector3(.035f,.04f,.01f),teal);
                }
            }
            Label("SERVICIO PROFESIONAL  /  DIAGNÓSTICO",environments[2],new Vector3(-.4f,2.35f,3.04f),.065f,teal.color);
            SetEnvironment(WorldKind.Home);
        }
        public void SetEnvironment(WorldKind kind)
        {
            activeWorld=kind;if(environments==null)return;
            for(int i=0;i<3;i++)environments[i].gameObject.SetActive(i==(int)kind);
            wood.color=kind==WorldKind.Home?new Color(.39f,.23f,.13f):kind==WorldKind.Campus?new Color(.52f,.59f,.59f):new Color(.13f,.19f,.22f);
            RenderSettings.ambientSkyColor=kind==WorldKind.Home?new Color(.47f,.58f,.65f):kind==WorldKind.Campus?new Color(.63f,.69f,.73f):new Color(.34f,.48f,.60f);
        }
        void Update()
        {
            if(coverPivot==null)return;
            coverPivot.localRotation=Quaternion.Slerp(coverPivot.localRotation,Quaternion.Euler(coverOpen?103:0,0,0),Time.deltaTime*8);
            plug.localPosition=Vector3.Lerp(plug.localPosition,new Vector3(plugged?1.60f:1.85f,1.10f,.66f),Time.deltaTime*10);
            foreach(var entry in targetPositions)entry.Key.transform.position=Vector3.Lerp(entry.Key.transform.position,entry.Value,Time.deltaTime*9);
            if(fanRunning) FanRotor.Rotate(Vector3.up,Time.deltaTime*700,Space.Self);
            led.sharedMaterial=fanRunning?teal:black;
        }
        void OnDestroy() { foreach(var m in ownedMaterials) if(m!=null)Destroy(m); }

        // Malla original: seis caras, doce biseles y ocho esquinas.
        static Mesh BeveledBox(Vector3 size,float bevel)
        {
            Vector3 h=size*.5f, q=h-Vector3.one*bevel;
            var v=new List<Vector3>();var tris=new List<int>();
            Action<Vector3[]> face=points=>{
                int n=v.Count; Vector3 center=Vector3.zero;foreach(var p in points)center+=p;
                if(Vector3.Dot(Vector3.Cross(points[1]-points[0],points[2]-points[0]),center)<0)Array.Reverse(points);
                v.AddRange(points);for(int i=1;i<points.Length-1;i++){tris.Add(n);tris.Add(n+i);tris.Add(n+i+1);}
            };
            foreach(int s in new[]{-1,1})
            {
                face(new[]{new Vector3(s*h.x,-q.y,-q.z),new Vector3(s*h.x,q.y,-q.z),new Vector3(s*h.x,q.y,q.z),new Vector3(s*h.x,-q.y,q.z)});
                face(new[]{new Vector3(-q.x,s*h.y,-q.z),new Vector3(q.x,s*h.y,-q.z),new Vector3(q.x,s*h.y,q.z),new Vector3(-q.x,s*h.y,q.z)});
                face(new[]{new Vector3(-q.x,-q.y,s*h.z),new Vector3(q.x,-q.y,s*h.z),new Vector3(q.x,q.y,s*h.z),new Vector3(-q.x,q.y,s*h.z)});
            }
            foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1})
            {
                face(new[]{new Vector3(-q.x,a*h.y,b*q.z),new Vector3(q.x,a*h.y,b*q.z),new Vector3(q.x,a*q.y,b*h.z),new Vector3(-q.x,a*q.y,b*h.z)});
                face(new[]{new Vector3(a*h.x,-q.y,b*q.z),new Vector3(a*h.x,q.y,b*q.z),new Vector3(a*q.x,q.y,b*h.z),new Vector3(a*q.x,-q.y,b*h.z)});
                face(new[]{new Vector3(a*h.x,b*q.y,-q.z),new Vector3(a*h.x,b*q.y,q.z),new Vector3(a*q.x,b*h.y,q.z),new Vector3(a*q.x,b*h.y,-q.z)});
                foreach(int c in new[]{-1,1})face(new[]{new Vector3(a*h.x,b*q.y,c*q.z),new Vector3(a*q.x,b*h.y,c*q.z),new Vector3(a*q.x,b*q.y,c*h.z)});
            }
            var mesh=new Mesh{name="Original beveled box"};mesh.SetVertices(v);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
