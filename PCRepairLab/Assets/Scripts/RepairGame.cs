using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PcFixLab
{
    public enum GameScreen { Menu, Workshop, Completed, Finished, Failed }
    public sealed class RepairGame : MonoBehaviour
    {
        public WorkshopWorld World {get;private set;}
        public CareerProfile Profile {get;private set;}
        public SimulationSession Session {get;private set;}
        public GameScreen Screen {get;private set;}=GameScreen.Menu;
        public Interactable Selected {get;private set;}
        const float W=1440,H=900;
        const string SaveKey="UIPC_CAREER_V2";
        bool paused,showHelp,showCredits,smokeMode,soundOn=true,confirmReset;
        [NonSerialized] bool initialized;
        string overlay="",toast="",testResult="Sin prueba de arranque.",verificationPath;
        float toastUntil,yaw=-20,pitch=48,distance=6.2f,saveClock;
        float uiScale,uiOffsetX,uiOffsetY;
        PartKind shopKind;
        Interactable hovered;Vector3 previousMouse;
        Font font;Texture2D circle;AudioSource audioSource;
        AudioClip clickSound,successSound,errorSound;
        readonly Dictionary<string,GUIStyle> styles=new Dictionary<string,GUIStyle>();
        readonly Color ink=new Color(.88f,.93f,.92f),muted=new Color(.51f,.63f,.65f),mint=new Color(.36f,.88f,.74f),orange=new Color(1,.62f,.36f);
        readonly Color panel=new Color(.035f,.065f,.082f,.97f),line=new Color(.14f,.23f,.25f);
        ContractDefinition Contract=>Session?.Contract;
        bool Modal=>paused||showHelp||showCredits||overlay!=""||confirmReset;
        void OnEnable(){initialized=false;}
        void Start(){InitializeGame();}
        void InitializeGame()
        {
            // Unity pierde modelos C# no serializables al recargar scripts en Play Mode.
            // Reconstruir la vista y restaurar el guardado evita referencias a perfiles y diccionarios perdidos.
            foreach(var previous in FindObjectsByType<WorkshopWorld>(FindObjectsSortMode.None))
            {previous.gameObject.SetActive(false);Destroy(previous.gameObject);}
            foreach(var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if(camera.gameObject.name=="Camera · orbital de taller"&&camera.transform.parent==null)
                {camera.gameObject.SetActive(false);Destroy(camera.gameObject);}
            styles.Clear();if(font!=null)Destroy(font);if(circle!=null)Destroy(circle);
            paused=false;showHelp=false;showCredits=false;overlay="";confirmReset=false;Time.timeScale=1;Screen=GameScreen.Menu;Session=null;Selected=null;hovered=null;
            var args=Environment.GetCommandLineArgs();smokeMode=Array.IndexOf(args,"--smoke-test")>=0;
#if UNITY_EDITOR
            smokeMode|=UnityEditor.SessionState.GetBool("PcFixLab.RunVerification",false);
            UnityEditor.SessionState.SetBool("PcFixLab.RunVerification",false);
#endif
            int oi=Array.IndexOf(args,"--verify-output");
            verificationPath=oi>=0&&oi+1<args.Length?args[oi+1]:Path.GetFullPath(Path.Combine(Application.dataPath,"..",Application.isEditor?"..":".","Verification"));
            font=Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI","Arial"},18);circle=MakeCircle();
            World=new GameObject("U-iPC · modular workshop").AddComponent<WorkshopWorld>();World.Build();World.InitializeParts();
            audioSource=GetComponent<AudioSource>();
            if(audioSource==null)audioSource=gameObject.AddComponent<AudioSource>();
            audioSource.Stop();audioSource.playOnAwake=false;audioSource.volume=.16f;
            clickSound=Tone(620,.045f);successSound=Tone(880,.18f);errorSound=Tone(190,.10f);
            LoadCareer();
            if(Session!=null)World.BindSession(Session);
            initialized=true;
            if(smokeMode)StartCoroutine(SmokeTest());
        }
        void LoadCareer()
        {
            Profile=new CareerProfile();if(smokeMode)return;
            try
            {
                string json=PlayerPrefs.GetString(SaveKey,"");
                if(json!="")
                {
                    var data=JsonUtility.FromJson<CareerSnapshot>(json);Profile=CareerProfile.Restore(data);
                    if(data.active!=null)Session=SimulationSession.Restore(Profile,data.active);
                }
                else if(PlayerPrefs.GetInt("CompletedOrders",0)>0)
                {
                    int n=Mathf.Clamp(PlayerPrefs.GetInt("CompletedOrders"),0,3);
                    var d=new CareerSnapshot{balance=700+Mathf.Max(0,PlayerPrefs.GetInt("Credits")),inventory=Profile.ExportStock(),records=Enumerable.Range(0,n).Select(i=>new LevelRecord{level=i,score=85,stars=3}).ToArray()};
                    Profile=CareerProfile.Restore(d);
                }
            }
            catch(Exception e)
            {
                PlayerPrefs.SetString(SaveKey+"_backup",PlayerPrefs.GetString(SaveKey,""));
                Profile=new CareerProfile();Session=null;
                Debug.LogWarning("U-iPC: se conservó una copia del guardado no válido. "+e.Message);
                Notify("Se inició un perfil nuevo; el guardado anterior se conservó como copia.");
            }
        }
        void Save()
        {
            if(smokeMode||Profile==null)return;
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(Profile.Export(Session?.Export())));PlayerPrefs.Save();
        }
        void OnApplicationQuit(){Save();}
        void OnDestroy(){Save();Time.timeScale=1;}
        void Update()
        {
            if(!initialized){InitializeGame();return;}
            if(World==null)return;
            Vector3 delta=Input.mousePosition-previousMouse;previousMouse=Input.mousePosition;
            if(Input.GetKeyDown(KeyCode.Escape))
            {if(overlay!="")overlay="";else if(showHelp)showHelp=false;else if(showCredits)showCredits=false;else if(confirmReset)confirmReset=false;else if(Screen==GameScreen.Workshop)paused=!paused;}
            if(Input.GetKeyDown(KeyCode.H))showHelp=!showHelp;
            if(Input.GetKeyDown(KeyCode.R)&&!Modal)ResetCamera();
            Time.timeScale=Modal?0:1;
            if(Screen==GameScreen.Workshop&&!Modal)
            {
                Session.Tick(Time.unscaledDeltaTime);saveClock+=Time.unscaledDeltaTime;
                if(saveClock>=10){saveClock=0;Save();}
                if(Session.Status==JobStatus.Failed){Screen=GameScreen.Failed;World.StopFans();Save();}
                if(!IsOverUi())
                {
                    if(Input.GetMouseButton(1)&&!Input.GetMouseButtonDown(1)){yaw+=delta.x*.22f;pitch=Mathf.Clamp(pitch-delta.y*.22f,24,76);}
                    distance=Mathf.Clamp(distance-Input.mouseScrollDelta.y*.38f,3.6f,8.5f);
                    var ray=World.SceneCamera.ScreenPointToRay(Input.mousePosition);
                    hovered=Physics.Raycast(ray,out RaycastHit hit,100)?hit.collider.GetComponentInParent<Interactable>():null;
                    if(Input.GetMouseButtonUp(0)&&hovered!=null)Select(hovered);
                }else hovered=null;
            }else hovered=null;
            Vector3 target=World.CameraTarget;if(Screen==GameScreen.Menu)target+=new Vector3(-.65f,.12f,0);
            float a=yaw*Mathf.Deg2Rad,b=pitch*Mathf.Deg2Rad;
            World.SceneCamera.transform.position=target+new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Sin(b),-Mathf.Cos(a)*Mathf.Cos(b))*distance;
            World.SceneCamera.transform.LookAt(target);
        }
        Vector2 UiMouse()
        {float s=Mathf.Min(UnityEngine.Screen.width/W,UnityEngine.Screen.height/H);return new Vector2((Input.mousePosition.x-(UnityEngine.Screen.width-W*s)/2)/s,((UnityEngine.Screen.height-Input.mousePosition.y)-(UnityEngine.Screen.height-H*s)/2)/s);}
        bool IsOverUi(){var m=UiMouse();return m.x<320||m.x>1100||m.y<170||m.y>620;}
        public void Select(Interactable item){if(Screen!=GameScreen.Workshop)return;Selected=item;Play(clickSound);}
        public void StartWorkshop(){if(Session!=null&&Session.Status!=JobStatus.Delivered){Resume();return;}StartContract(Profile.Unlocked);}
        void StartContract(int level)
        {
            if(Session!=null&&(Session.Status==JobStatus.Active||Session.Status==JobStatus.Ready)){Notify("Ya hay un encargo activo. Continúa o cancélalo desde pausa.");return;}
            Session=new SimulationSession(Profile,level);BindSession();
        }
        void Resume(){BindSession();}
        void BindSession()
        {
            World.BindSession(Session);Selected=null;paused=false;overlay="";ResetCamera();
            Screen=Session.Status==JobStatus.Delivered?GameScreen.Completed:Session.Status==JobStatus.Failed?GameScreen.Failed:GameScreen.Workshop;
            testResult=Session.Powered?(Session.BootProblem()??"POST correcto. Revisa estabilidad y entrega."):"Equipo apagado.";
            World.SetMonitor(testResult,Session.Powered);Save();
            Notify("Revisa el síntoma, diagnostica y respeta el presupuesto.");
        }
        void Execute(OperationResult result)
        {
            Notify(result.Message);Play(result.Success?clickSound:errorSound);
            World.SetPower(Session.Connected);World.SetCover(Session.CoverOpen);
            foreach(var p in World.Parts.Values)World.RefreshPart(p);
            if(!Session.Powered)World.StopFans();
            if(Session.Status==JobStatus.Delivered){Screen=GameScreen.Completed;overlay="";World.Customer.Celebrate();Play(successSound);}
            if(Session.Status==JobStatus.Failed){Screen=GameScreen.Failed;overlay="";}
            Save();
        }
        public void RestartOrder(){if(Session==null||Session.Status==JobStatus.Delivered)return;Session=Session.Retry();BindSession();}
        public void NextOrder(){if(Session==null||Session.Status!=JobStatus.Delivered)return;Session=null;Screen=GameScreen.Menu;Save();}
        public void NewWorkshop(){Profile=new CareerProfile();Session=null;confirmReset=false;Screen=GameScreen.Menu;Save();}
        public bool ToggleConnection(){var r=Session.ToggleConnection();Execute(r);return r.Success;}
        public bool ToggleCover(){var r=Session.ToggleCover();Execute(r);return r.Success;}
        public bool Perform(RepairablePart part,RepairAction action)
        {
            if(action==RepairAction.Diagnose&&Contract.Difficulty!=Difficulty.Guided){Selected=part;overlay="tests";return true;}
            if(action==RepairAction.Replace){Selected=part;shopKind=part.Kind;overlay="shop";return true;}
            var r=Session.Perform(part.Kind,action);Execute(r);return r.Success;
        }
        public void PowerDown(){if(Session.Powered)RunTest();}
        public bool RunTest()
        {
            var r=Session.TestBoot();testResult=r.Message;Execute(r);World.SetMonitor(r.Message,Session.Powered);return r.Success;
        }
        void ResetCamera(){yaw=-20;pitch=48;distance=6.2f;}
        void Notify(string s){toast=s;toastUntil=Time.unscaledTime+8;}
        void Play(AudioClip clip){if(soundOn&&audioSource!=null)audioSource.PlayOneShot(clip);}
        AudioClip Tone(float hz,float duration)
        {
            int n=(int)(22050*duration);var data=new float[n];for(int i=0;i<n;i++)data[i]=Mathf.Sin(i*hz*Mathf.PI*2/22050)*Mathf.Sin(Mathf.PI*i/n)*.35f;
            var c=AudioClip.Create("Original workshop tone",n,1,22050,false);c.SetData(data,0);return c;
        }
        void OnGUI()
        {
            if(!initialized||World==null||font==null||Profile==null)return;
            uiScale=Mathf.Min(UnityEngine.Screen.width/W,UnityEngine.Screen.height/H);
            uiOffsetX=(UnityEngine.Screen.width-W*uiScale)/2;uiOffsetY=(UnityEngine.Screen.height-H*uiScale)/2;
            var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(new Vector3(uiOffsetX,uiOffsetY,0),Quaternion.identity,new Vector3(uiScale,uiScale,1));
            DrawHeader();
            if(Screen==GameScreen.Menu)DrawCampaign();
            else if(Screen==GameScreen.Workshop){DrawBrief();DrawInspector();DrawToolbar();DrawBottom();}
            else DrawOutcome();
            Text(new Rect(26,873,1000,17),"U-iPC 2.0  /  LIZBETH VALERIO GARCIA  /  SIMULACIÓN LOCAL",10,muted);
            if(Screen==GameScreen.Workshop&&Time.unscaledTime<toastUntil&&!Modal)
            {Card(new Rect(335,691,760,71),panel,8);Text(new Rect(350,702,730,49),toast,14,ink);}
            if(paused)DrawPause();if(showHelp)DrawHelp();if(showCredits)DrawCredits();if(overlay!="")DrawOverlay();if(confirmReset)DrawReset();
            GUI.matrix=old;
        }
        GUIStyle Style(int size,bool bold,TextAnchor alignment)
        {
            string key=size+":"+bold+":"+alignment;
            if(!styles.TryGetValue(key,out GUIStyle style))
            {
                style=new GUIStyle(GUI.skin.label){font=font,fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,
                    alignment=alignment,wordWrap=true,richText=false,padding=new RectOffset(0,0,0,0)};styles[key]=style;
            }
            return style;
        }
        void Text(Rect r,string text,int size,Color color,bool bold=false,TextAnchor align=TextAnchor.UpperLeft)
        { var s=Style(size,bold,align);s.normal.textColor=color;GUI.Label(r,text,s); }
        void Fill(Rect r,Color color){Color old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        void Disc(Rect r,Color color){Color old=GUI.color;GUI.color=color;GUI.DrawTexture(r,circle);GUI.color=old;}
        void Card(Rect r,Color color,float radius=12)
        {
            Fill(new Rect(r.x+radius,r.y,r.width-2*radius,r.height),color);
            Fill(new Rect(r.x,r.y+radius,r.width,r.height-2*radius),color);
            float d=radius*2;Disc(new Rect(r.x,r.y,d,d),color);Disc(new Rect(r.xMax-d,r.y,d,d),color);
            Disc(new Rect(r.x,r.yMax-d,d,d),color);Disc(new Rect(r.xMax-d,r.yMax-d,d,d),color);
        }
        bool Button(Rect r,string label,bool primary=false,bool enabled=true)
        {
            bool hover=r.Contains(Event.current.mousePosition);
            Color c=primary?mint:new Color(.10f,.17f,.19f);
            if(hover && enabled)c=Color.Lerp(c,Color.white,.10f);if(!enabled)c=new Color(.085f,.12f,.135f);
            Card(r,c,7);Text(r,label,14,enabled?(primary?new Color(.025f,.10f,.09f):ink):muted,true,TextAnchor.MiddleCenter);
            bool was=GUI.enabled;GUI.enabled=enabled;bool clicked=GUI.Button(r,GUIContent.none,GUIStyle.none);GUI.enabled=was;
            if(clicked)Play(clickSound);return clicked;
        }
        void Chip(Rect r,string text,Color color)
        {Card(r,new Color(color.r*.17f,color.g*.17f,color.b*.17f,.98f),6);Disc(new Rect(r.x+10,r.y+11,6,6),color);Text(new Rect(r.x+24,r.y,r.width-30,r.height),text,11,color,true,TextAnchor.MiddleLeft);}

        void DrawHeader()
        {
            Card(new Rect(24,22,1392,66),panel,11);
            Card(new Rect(40,36,38,38),mint,8);Text(new Rect(40,36,38,38),"U",25,panel,true,TextAnchor.MiddleCenter);
            Text(new Rect(91,31,210,27),"U-iPC",23,ink,true);Text(new Rect(92,61,310,14),"SIMULADOR DE REPARACIÓN  /  CARRERA",9,muted);
            if(Profile!=null)
            {Chip(new Rect(680,40,170,28),Profile.Balance+" CR",mint);Chip(new Rect(860,40,150,28),"REP "+Profile.Reputation,orange);Text(new Rect(1024,43,112,24),Profile.Xp+" XP",13,ink,true);}
            if(Button(new Rect(1155,37,107,35),soundOn?"SONIDO: SÍ":"SONIDO: NO"))soundOn=!soundOn;
            if(Button(new Rect(1272,37,127,35),"AYUDA [H]"))showHelp=true;
        }
        void DrawCampaign()
        {
            Text(new Rect(30,116,1000,43),"Tu carrera en reparación de PC",31,ink,true);
            Text(new Rect(31,167,1130,28),"12 contratos · 3 escenarios · 5 componentes · diagnóstico, recambios y pruebas bajo carga",16,muted);
            for(int w=0;w<3;w++)
            {
                float x=24+w*470;Card(new Rect(x,216,452,548),panel,13);
                Text(new Rect(x+20,236,410,29),"0"+(w+1)+"  "+CampaignCatalog.WorldName((WorldKind)w),21,ink,true);
                Text(new Rect(x+20,275,410,25),CampaignCatalog.DifficultyName((Difficulty)w)+(w==0?" · sin límite de errores":w==1?" · 6 errores · 12 minutos":" · 4 errores · 10 minutos"),13,w==0?mint:orange);
                for(int j=0;j<4;j++)
                {
                    int level=w*4+j;var c=CampaignCatalog.Contracts[level];float y=319+j*104;
                    Card(new Rect(x+16,y,420,94),new Color(.075f,.12f,.14f),8);
                    Text(new Rect(x+29,y+10,280,24),(level+1).ToString("00")+"  "+c.Title,15,ink,true);
                    int stars=Profile.Stars(level);
                    Text(new Rect(x+29,y+43,280,32),stars>0?new string('★',stars)+" · "+c.Reward+" CR":c.Reward+" CR · presupuesto "+c.PartsBudget+" CR",12,stars>0?mint:muted);
                    bool active=Session!=null&&Session.Status!=JobStatus.Delivered;
                    string label=active&&Session.Contract.Index==level?"SEGUIR":level>Profile.Unlocked?"BLOQUEADO":stars>0?"REPETIR":"ACEPTAR";
                    if(Button(new Rect(x+313,y+32,108,37),label,level==Profile.Unlocked,level<=Profile.Unlocked&&(!active||Session.Contract.Index==level)))
                    {if(active)Resume();else StartContract(level);}
                }
            }
            if(Button(new Rect(26,792,250,43),"CÓMO JUGAR"))showHelp=true;
            if(Button(new Rect(289,792,250,43),"CRÉDITOS Y POO"))showCredits=true;
            if(Button(new Rect(1020,792,180,43),"NUEVA CARRERA"))confirmReset=true;
            if(Button(new Rect(1211,792,205,43),"SALIR"))Application.Quit();
        }
        void DrawToolbar()
        {
            if(Button(new Rect(333,112,182,39),"DIAGNÓSTICO"))overlay="tests";
            if(Button(new Rect(525,112,182,39),"TIENDA / STOCK")){shopKind=Selected is RepairablePart p?p.Kind:PartKind.Memory;overlay="shop";}
            if(Button(new Rect(717,112,182,39),"BITÁCORA"))overlay="journal";
            if(Button(new Rect(909,112,182,39),"PISTA"))Execute(Session.Hint());
        }
        void DrawBrief()
        {
            Card(new Rect(24,112,282,726),panel,12);
            Text(new Rect(44,134,242,20),"CONTRATO "+(Contract.Index+1).ToString("00")+" / 12",12,mint,true);
            Text(new Rect(44,173,242,53),Contract.Title,23,ink,true);
            Text(new Rect(44,239,242,22),Contract.Client+" · "+Contract.Role,12,orange,true);
            Text(new Rect(44,279,242,108),"“"+Contract.Story+"”",15,ink);
            Fill(new Rect(44,400,242,1),line);
            Text(new Rect(44,420,242,21),"CONTROL DEL ENCARGO",11,muted,true);
            float left=Mathf.Max(0,Contract.TimeLimit-Session.Elapsed);
            string time=Contract.Difficulty==Difficulty.Guided?"Aprendiz · sin plazo":"Tiempo "+((int)left/60).ToString("00")+":"+((int)left%60).ToString("00");
            Text(new Rect(44,458,242,26),time,18,ink,true);
            Text(new Rect(44,502,242,25),"Errores: "+Session.Mistakes+(Contract.MaxMistakes==0?" / práctica":"/"+Contract.MaxMistakes),15,Session.Mistakes>0?orange:mint);
            Text(new Rect(44,542,242,45),"Piezas: "+Session.Spent+" / "+Contract.PartsBudget+" CR\nPago por servicio: "+Contract.Reward+" CR",14,ink);
            int n=Session.Parts.Count(p=>p.Diagnosed);
            Text(new Rect(44,610,242,47),"Componentes comprobados: "+n+"/5\nCarga estable: "+(Session.StressPassed?"SÍ":Contract.NeedsStress?"PENDIENTE":"opcional"),13,muted);
            Text(new Rect(44,692,242,27),Session.Powered?"PC ENCENDIDA":Session.Connected?"CABLE CONECTADO":"PC DESCONECTADA",14,Session.Connected?orange:mint,true);
            Text(new Rect(44,742,242,60),"Apaga → desconecta → abre.\nMonta → cierra → conecta → prueba.\nEntrega sólo al comprobar el equipo.",12,ink);
        }
        void DrawInspector()
        {
            Card(new Rect(1110,112,306,726),panel,12);
            Text(new Rect(1130,134,266,20),"INSPECCIÓN",11,muted,true);
            Text(new Rect(1130,172,266,55),Selected?.DisplayName??"Selecciona una pieza",23,ink,true);
            if(Selected is RepairablePart part)
            {
                var state=part.Condition;var item=PartCatalog.Find(state.Sku);
                Text(new Rect(1130,241,266,45),item==null?part.Specification:item.Name+" · "+item.Socket,14,muted);
                Chip(new Rect(1130,302,266,28),state.HardwareConfirmed&&state.IsFaulty?"AVERÍA CONFIRMADA":state.Diagnosed?"PRUEBA REGISTRADA":"SIN DIAGNÓSTICO",state.HardwareConfirmed&&state.IsFaulty?orange:mint);
                int row=0;bool safe=!Session.Connected&&!Session.Powered&&Session.CoverOpen;
                Action<string,Action,bool> action=(s,fn,enabled)=>{if(Button(new Rect(1130,350+row*43,266,36),s,false,enabled))fn();row++;};
                action("INSPECCIÓN VISUAL",()=>Execute(Session.Diagnose(part.Kind,DiagnosticTool.Visual)),safe);
                action(Contract.Difficulty==Difficulty.Guided?"PRUEBA GUIADA":"ELEGIR PRUEBA",()=>{if(Contract.Difficulty==Difficulty.Guided)Execute(Session.Diagnose(part.Kind,state.CorrectTool));else overlay="tests";},safe);
                if(state.LooseCable&&state.Diagnosed)action("ASEGURAR CONECTOR",()=>Perform(part,RepairAction.Reconnect),safe);
                if(state.HasDust&&state.Diagnosed)action("LIMPIAR POLVO",()=>Perform(part,RepairAction.Clean),safe);
                if(!state.IsInstalled&&state.NeedsPaste)action("LIMPIAR Y CAMBIAR PASTA",()=>Perform(part,RepairAction.ApplyPaste),safe&&state.CanPerform(RepairAction.ApplyPaste));
                if(state.IsInstalled)action("RETIRAR COMPONENTE",()=>Perform(part,RepairAction.Remove),safe&&state.CanPerform(RepairAction.Remove));
                else if(state.CanPerform(RepairAction.Replace))action("ELEGIR RECAMBIO",()=>Perform(part,RepairAction.Replace),safe);
                else action("VOLVER A MONTAR",()=>Perform(part,RepairAction.Reinstall),safe&&state.CanPerform(RepairAction.Reinstall));
            }
            else if(Selected is CaseCover)
            {Text(new Rect(1130,245,266,80),"Abre únicamente con el equipo apagado y desconectado.",16,muted);if(Button(new Rect(1130,369,266,42),Session.CoverOpen?"CERRAR TAPA":"ABRIR TAPA"))ToggleCover();}
            else if(Selected is PowerConnector)
            {Text(new Rect(1130,245,266,80),"Apaga la PC antes de manipular su alimentación.",16,muted);if(Button(new Rect(1130,369,266,42),Session.Connected?"DESCONECTAR":"CONECTAR"))ToggleConnection();}
            else Text(new Rect(1130,245,266,170),Selected is WorkshopCharacter ch?ch.Speak():testResult,15,muted);
            Fill(new Rect(1130,660,266,1),line);Text(new Rect(1130,676,266,19),"ACCESOS AL EQUIPO",10,muted,true);
            string[] names={"RAM","SSD","CPU/FAN","FUENTE","GPU"};
            for(int i=0;i<5;i++){int k=i;if(Button(new Rect(1130+(i%3)*91,708+(i/3)*43,84,35),names[i]))Select(World.Parts[(PartKind)k]);}
            if(Button(new Rect(1312,751,84,35),"TAPA"))Select(World.Cover);
            if(Button(new Rect(1130,798,266,29),"PAUSA [ESC]"))paused=true;
        }
        void DrawBottom()
        {
            Card(new Rect(330,774,767,64),panel,10);
            if(Button(new Rect(342,787,239,39),Session.Powered?"APAGAR PC":"ENCENDER Y PROBAR",true))RunTest();
            if(Button(new Rect(591,787,239,39),Session.Connected?"DESCONECTAR":"CONECTAR"))ToggleConnection();
            if(Button(new Rect(840,787,244,39),Session.CoverOpen?"CERRAR TAPA":"ABRIR TAPA"))ToggleCover();
            if(Button(new Rect(402,627,300,43),"PRUEBA DE ESTABILIDAD +45s")){var r=Session.StressTest();testResult=r.Message;Execute(r);World.SetMonitor(r.Message,Session.Powered);}
            if(Button(new Rect(717,627,300,43),"ENTREGAR EQUIPO",true))Execute(Session.Deliver());
            Text(new Rect(345,846,730,19),"CLIC: seleccionar  /  DERECHO + ARRASTRAR: girar  /  RUEDA: zoom  /  R: restablecer",11,muted,false,TextAnchor.MiddleCenter);
            if(hovered!=null)Chip(new Rect(470,171,500,28),"SELECCIONAR / "+hovered.DisplayName.ToUpperInvariant(),mint);
        }
        void Shade(){Fill(new Rect(-uiOffsetX/uiScale,-uiOffsetY/uiScale,UnityEngine.Screen.width/uiScale,UnityEngine.Screen.height/uiScale),new Color(.01f,.025f,.035f,.87f));}
        void ModalBase(string title)
        {Shade();Card(new Rect(350,163,740,608),panel,16);Text(new Rect(382,190,655,43),title,27,ink,true);if(Button(new Rect(970,710,88,35),"CERRAR"))overlay="";}
        void DrawOverlay()
        {
            ModalBase(overlay=="shop"?"Recambios e inventario":overlay=="tests"?"Banco de diagnóstico":"Bitácora del encargo");
            if(overlay=="shop")
            {
                for(int i=0;i<5;i++){int k=i;if(Button(new Rect(380+i*137,249,128,37),PartCatalog.PartName((PartKind)i),shopKind==(PartKind)i))shopKind=(PartKind)k;}
                Text(new Rect(382,306,676,41),"Saldo "+Profile.Balance+" CR · piezas "+Session.Spent+"/"+Contract.PartsBudget+" CR\nCompra un recambio compatible. El presupuesto no equivale al saldo del taller.",14,muted);
                int row=0;
                foreach(var item in PartCatalog.Items.Where(i=>i.Kind==shopKind))
                {
                    float y=365+row*89;bool compatible=Contract.Compatible(item,shopKind);
                    Card(new Rect(379,y,680,78),new Color(.075f,.12f,.14f),7);
                    Text(new Rect(391,y+8,369,23),item.Name+" · "+item.Price+" CR",16,ink,true);
                    Text(new Rect(391,y+39,369,24),item.Socket+" · stock "+Profile.Stock(item.Id)+(compatible?" · compatible":" · no cumple el contrato"),12,compatible?mint:orange);
                    if(Button(new Rect(769,y+17,130,39),"COMPRAR"))Execute(Session.Buy(item.Id));
                    var state=Session.Parts[(int)shopKind];
                    if(Button(new Rect(911,y+17,137,39),"INSTALAR",true,Profile.Stock(item.Id)>0&&state.CanPerform(RepairAction.Replace)))
                        Execute(Session.Perform(shopKind,RepairAction.Replace,item.Id));
                    row++;
                }
                Text(new Rect(382,654,550,43),"Los precios son créditos ficticios. Un reintento restaura el saldo y el stock iniciales del contrato.",13,muted);
            }
            else if(overlay=="tests")
            {
                Text(new Rect(382,250,676,51),"Selecciona una pieza y una prueba. Se utiliza un banco aislado: PC apagada, cable desconectado y tapa abierta.",15,muted);
                for(int i=0;i<5;i++){int k=i;if(Button(new Rect(380+i*137,315,128,36),PartCatalog.PartName((PartKind)i),Selected is RepairablePart p&&p.Kind==(PartKind)i))Select(World.Parts[(PartKind)k]);}
                for(int i=0;i<6;i++)
                {
                    var tool=(DiagnosticTool)i;float x=382+(i%2)*345,y=376+(i/2)*65;
                    if(Button(new Rect(x,y,330,49),PartCatalog.ToolName(tool)+(i==0?" +5s":" +25s"),false,Selected is RepairablePart))
                    {var p=(RepairablePart)Selected;Execute(Session.Diagnose(p.Kind,tool));}
                }
                Text(new Rect(383,592,671,94),toast,16,ink);
            }
            else
            {
                string[] entries=Session.Journal.AsEnumerable().Reverse().Take(8).ToArray();
                for(int i=0;i<entries.Length;i++){Text(new Rect(382,250+i*53,676,45),(i+1)+". "+entries[i],14,i==0?mint:ink);}
            }
        }
        void DrawOutcome()
        {
            Shade();Card(new Rect(418,182,604,548),panel,16);
            bool done=Screen==GameScreen.Completed;
            Text(new Rect(458,228,524,93),done?"Equipo reparado y entregado":"Encargo suspendido",33,ink,true,TextAnchor.MiddleCenter);
            Text(new Rect(463,345,514,84),done?"Calidad "+Session.Score+"/100 · "+Profile.Stars(Contract.Index)+" estrellas\n+"+Contract.Reward+" CR · saldo "+Profile.Balance+" CR":"Se agotó el plazo o el límite de errores.\nReintentar devuelve las compras de este encargo.\nLa reputación baja 5 puntos al reiniciar un fallo.",18,done?mint:orange,false,TextAnchor.MiddleCenter);
            Text(new Rect(463,458,514,41),CampaignCatalog.WorldName(Contract.World)+" · "+Contract.Title,15,muted,false,TextAnchor.MiddleCenter);
            if(Button(new Rect(465,550,510,49),done?"VOLVER A LA CAMPAÑA":"REINTENTAR CONTRATO",true))
            {if(done)NextOrder();else RestartOrder();}
            if(Button(new Rect(465,616,510,40),"IR AL MENÚ")){Screen=GameScreen.Menu;paused=false;Save();}
        }
        void DrawPause()
        {
            Shade();Card(new Rect(480,215,480,482),panel,16);Text(new Rect(519,254,402,48),"Taller en pausa",29,ink,true);
            if(Button(new Rect(519,330,402,45),"CONTINUAR",true))paused=false;
            if(Button(new Rect(519,389,402,42),"REINTENTAR Y RESTAURAR COMPRAS"))RestartOrder();
            if(Button(new Rect(519,445,402,42),"GUARDAR Y VOLVER AL MENÚ")){Save();paused=false;Screen=GameScreen.Menu;}
            if(Button(new Rect(519,501,402,42),"CANCELAR ENCARGO Y VOLVER"))
            {Session=Session.Retry();Session=null;paused=false;Screen=GameScreen.Menu;Save();}
            if(Button(new Rect(519,557,402,42),"GUARDAR Y SALIR")){Save();Application.Quit();}
            Text(new Rect(519,631,402,34),"El tiempo se detiene al abrir ayuda, tienda, pruebas o pausa.",12,muted);
        }
        void DrawHelp()
        {
            Shade();Card(new Rect(360,139,720,624),panel,16);
            Text(new Rect(398,180,640,47),"Una reparación basada en evidencia",28,ink,true);
            Text(new Rect(398,255,640,334),
                "1. Acepta un contrato en el mapa y observa su síntoma con una prueba de arranque.\n\n"+
                "2. Apaga, desconecta y abre. Inspecciona y elige una prueba específica en DIAGNÓSTICO. Las conexiones flojas deben corregirse antes de confirmar averías.\n\n"+
                "3. Reasienta o reconecta cuando baste. Si la avería interna está confirmada, retira la pieza, compra un recambio compatible e instálalo desde TIENDA / STOCK.\n\n"+
                "4. Para temperatura: limpia el polvo, retira el disipador, cambia pasta y vuelve a montar.\n\n"+
                "5. Cierra, conecta, arranca y prueba estabilidad cuando se exija. Finalmente pulsa ENTREGAR EQUIPO.",17,ink);
            Text(new Rect(398,614,640,51),"Aprendiz: práctica sin plazo. Técnica y especialista: límite de tiempo y errores. Pedir una pista cuesta calidad y 30 segundos en niveles avanzados.",14,orange);
            if(Button(new Rect(398,690,640,42),"ENTENDIDO",true))showHelp=false;
        }
        void DrawCredits()
        {
            Shade();Card(new Rect(390,182,660,538),panel,16);
            Text(new Rect(430,223,580,43),"U-iPC 2.0",31,ink,true);
            Text(new Rect(430,302,580,126),"Lizbeth Valerio Garcia\nUniversidad Tecnológica de Oriental\nOptativa de Videojuegos · Oliver Peregrina Luna\nCódigo, modelos y sonidos con asistencia de IA.",18,ink);
            Text(new Rect(430,456,580,125),"POO: Interactable y RepairablePart, cinco subclases de piezas, SimulationSession y CareerProfile.\nSimulación por reglas: no es un diagnóstico eléctrico real.\nLos precios, plazos y temperaturas son valores del juego.",16,muted);
            if(Button(new Rect(430,634,580,43),"VOLVER",true))showCredits=false;
        }
        void DrawReset()
        {
            Shade();Card(new Rect(459,287,522,306),panel,16);
            Text(new Rect(494,326,452,56),"¿Empezar una carrera nueva?",25,ink,true);
            Text(new Rect(494,405,452,49),"Se reinician saldo, inventario, encargos y estrellas del perfil actual.",16,orange);
            if(Button(new Rect(494,505,217,42),"CONSERVAR",true))confirmReset=false;
            if(Button(new Rect(726,505,220,42),"REINICIAR PERFIL"))NewWorkshop();
        }
        static Texture2D MakeCircle()
        {
            var t=new Texture2D(64,64,TextureFormat.RGBA32,false){name="Original UI circle",filterMode=FilterMode.Bilinear};
            var px=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++)px[y*64+x]=new Color(1,1,1,Mathf.Clamp01(32-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32))));
            t.SetPixels(px);t.Apply();return t;
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(verificationPath,name+".png"));
            yield return null;yield return null;
        }
        IEnumerator SmokeTest()
        {
            Directory.CreateDirectory(verificationPath);yield return new WaitForSecondsRealtime(1);
            yield return Capture("01-campaign");
            bool passed=true;var notes=new List<string>();
            for(int i=0;i<12;i++)
            {
                Session=new SimulationSession(Profile,i);BindSession();
                Execute(Session.TestBoot());if(Session.Powered)Execute(Session.TestBoot());Execute(Session.ToggleConnection());Execute(Session.ToggleCover());
                for(int j=0;j<5;j++)
                {
                    var part=Session.Parts[j];if(part.IsHealthy)continue;
                    Execute(Session.Diagnose((PartKind)j,DiagnosticTool.Visual));
                    if(part.LooseCable)Execute(Session.Perform((PartKind)j,RepairAction.Reconnect));
                    if(part.Unseated){Execute(Session.Perform((PartKind)j,RepairAction.Remove));Execute(Session.Perform((PartKind)j,RepairAction.Reinstall));}
                    Execute(Session.Diagnose((PartKind)j,part.CorrectTool));
                    if(part.IsFaulty)
                    {
                        Execute(Session.Perform((PartKind)j,RepairAction.Remove));
                        var item=PartCatalog.Items.Where(it=>Session.Contract.Compatible(it,(PartKind)j)).OrderBy(it=>it.Price).First();
                        if(Profile.Stock(item.Id)==0)Execute(Session.Buy(item.Id));
                        Execute(Session.Perform((PartKind)j,RepairAction.Replace,item.Id));
                    }
                    if(part.HasDust)Execute(Session.Perform((PartKind)j,RepairAction.Clean));
                    if(part.NeedsPaste){Execute(Session.Perform((PartKind)j,RepairAction.Remove));Execute(Session.Perform((PartKind)j,RepairAction.ApplyPaste));Execute(Session.Perform((PartKind)j,RepairAction.Reinstall));}
                }
                if(i==0||i==4||i==8){yield return new WaitForSecondsRealtime(.4f);yield return Capture("world-"+i/4);}
                Execute(Session.ToggleCover());Execute(Session.ToggleConnection());Execute(Session.TestBoot());
                if(Session.Contract.NeedsStress)Execute(Session.StressTest());Execute(Session.Deliver());
                bool ok=Session.Status==JobStatus.Delivered;passed &= ok;notes.Add((ok?"PASS ":"FAIL ")+(i+1)+" "+Session.Contract.Title);
            }
            yield return Capture("05-final-contract");
            File.WriteAllLines(Path.Combine(verificationPath,"smoke-test-v2.txt"),notes);
            Debug.Log("UIPC SMOKE: "+(passed?"PASSED":"FAILED")+" · 12 contracts");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit(passed?0:2);
#endif
        }
    }
}
