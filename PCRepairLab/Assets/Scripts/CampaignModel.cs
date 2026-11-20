using System;
using System.Collections.Generic;
using System.Linq;
namespace PcFixLab
{
    public enum WorldKind { Home, Campus, Service }
    public enum Difficulty { Guided, Technician, Expert }
    public enum JobStatus { Active, Ready, Delivered, Failed }
    public sealed class ShopItem
    {
        public string Id,Name,Socket;public PartKind Kind;public int Price,Capacity;
        public ShopItem(string id,string name,PartKind kind,string socket,int capacity,int price){Id=id;Name=name;Kind=kind;Socket=socket;Capacity=capacity;Price=price;}
    }
    public static class PartCatalog
    {
        public static readonly ShopItem[] Items={
            new ShopItem("ram8","RAM DDR4 8 GB",PartKind.Memory,"DDR4",8,90),new ShopItem("ram16","RAM DDR4 16 GB",PartKind.Memory,"DDR4",16,150),new ShopItem("ddr5","RAM DDR5 16 GB",PartKind.Memory,"DDR5",16,160),
            new ShopItem("ssd480","SSD SATA 480 GB",PartKind.Storage,"SATA",480,120),new ShopItem("ssd1000","SSD SATA 1 TB",PartKind.Storage,"SATA",1000,210),new ShopItem("nvme","SSD NVMe M.2 1 TB",PartKind.Storage,"NVMe",1000,230),
            new ShopItem("cooler","Disipador LGA1700",PartKind.Cooling,"LGA1700",1,110),new ShopItem("amdcooler","Disipador AM5",PartKind.Cooling,"AM5",1,110),
            new ShopItem("psu450","Fuente ATX 450 W",PartKind.PowerSupply,"ATX",450,180),new ShopItem("psu650","Fuente ATX 650 W",PartKind.PowerSupply,"ATX",650,300),
            new ShopItem("gpu4","GPU PCIe 4 GB",PartKind.Graphics,"PCIe",4,250),new ShopItem("gpu8","GPU PCIe 8 GB",PartKind.Graphics,"PCIe",8,500)
        };
        public static ShopItem Find(string id)=>Array.Find(Items,i=>i.Id==id);
        public static string DefaultSku(PartKind k)=>k==PartKind.Memory?"ram8":k==PartKind.Storage?"ssd480":k==PartKind.Cooling?"cooler":k==PartKind.PowerSupply?"psu650":"gpu8";
        public static string PartName(PartKind k)=>k==PartKind.Memory?"RAM":k==PartKind.Storage?"SSD":k==PartKind.Cooling?"Disipador":k==PartKind.PowerSupply?"Fuente ATX":"Tarjeta gráfica";
        public static string ToolName(DiagnosticTool t)=>t==DiagnosticTool.Visual?"Inspección visual":t==DiagnosticTool.Memory?"Prueba de memoria":t==DiagnosticTool.Smart?"Diagnóstico SMART":t==DiagnosticTool.Thermal?"Sonda térmica":t==DiagnosticTool.Power?"Probador de fuente":"Prueba de vídeo";
    }
    public sealed class ContractDefinition
    {
        public int Index,Reward,PartsBudget,TimeLimit,MaxMistakes;public string Client,Role,Title,Story;
        public WorldKind World;public Difficulty Difficulty;public PartFault[] Faults;public bool NeedsStress;
        public int MinimumMemory=8,MinimumStorage=480,MinimumPower=450,MinimumGraphics=4;
        public bool Compatible(ShopItem item,PartKind part)
        {
            if(item==null||item.Kind!=part)return false;
            string socket=part==PartKind.Memory?"DDR4":part==PartKind.Storage?"SATA":part==PartKind.Cooling?"LGA1700":part==PartKind.PowerSupply?"ATX":"PCIe";
            int min=part==PartKind.Memory?MinimumMemory:part==PartKind.Storage?MinimumStorage:part==PartKind.PowerSupply?MinimumPower:part==PartKind.Graphics?MinimumGraphics:1;
            return item.Socket==socket&&item.Capacity>=min;
        }
    }
    public static class CampaignCatalog
    {
        public static readonly ContractDefinition[] Contracts=Build();
        public static string WorldName(WorldKind w)=>w==WorldKind.Home?"Taller de barrio":w==WorldKind.Campus?"Laboratorio universitario":"Servicio profesional";
        public static string DifficultyName(Difficulty d)=>d==Difficulty.Guided?"Aprendiz":d==Difficulty.Technician?"Técnica":"Especialista";
        static ContractDefinition[] Build()
        {
            var list=new List<ContractDefinition>();
            Action<string,string,string,string,PartFault[],int,int> add=(client,role,title,story,faults,reward,budget)=>{
                int i=list.Count;var dif=i<4?Difficulty.Guided:i<8?Difficulty.Technician:Difficulty.Expert;
                list.Add(new ContractDefinition{Index=i,Client=client,Role=role,Title=title,Story=story,World=(WorldKind)(i/4),Difficulty=dif,Faults=faults,Reward=reward,PartsBudget=budget,TimeLimit=i<4?1200:i<8?720:600,MaxMistakes=i<4?0:i<8?6:4,NeedsStress=i>=4});
            };
            PartFault N=PartFault.None,H=PartFault.Hardware,L=PartFault.LooseCable,U=PartFault.Unseated,D=PartFault.Dust,P=PartFault.DryPaste;
            add("ANA","ESTUDIANTE","Primer arranque","La PC emite pitidos y no muestra imagen. Necesito entregar un proyecto.",new[]{H,N,N,N,N},180,150);
            add("MARIO","VECINO","Una conexión olvidada","Después de mover el equipo el disco dejó de aparecer. Comprueba antes de cambiar piezas.",new[]{N,L,N,N,N},150,150);
            add("SOFÍA","PROFESORA","El ventilador pide ayuda","Hay mucho polvo y el equipo se calienta al abrir varios programas.",new[]{N,N,D|P,N,N},220,150);
            add("ELENA","COMERCIANTE","Sin señales de vida","No se enciende ningún indicador. Revisa la alimentación del equipo.",new[]{N,N,N,H,N},360,350);
            add("DIEGO","DISEÑADOR","Dos problemas en el aula","El equipo fue trasladado. A veces hay pitidos y tampoco encuentra el disco.",new[]{U,H,N,N,N},440,300);
            add("PAULA","INVESTIGADORA","Estable en reposo","Enciende bien, pero se apaga bajo carga. Necesito comprobar la estabilidad.",new[]{N,N,D|P,N,N},380,200);
            add("LUIS","ESTUDIANTE DE 3D","Pantalla en negro","La computadora enciende, pero no obtiene salida de vídeo. Comprueba la GPU.",new[]{N,N,N,N,H},560,550);
            add("CLARA","DOCENTE","Equipo de prácticas","Tras un montaje de clase hay fallas de memoria y conexiones que revisar.",new[]{H,L,N,L,N},680,250);
            add("ANDRÉS","ARQUITECTO","La estación de trabajo","La fuente y la tarjeta de vídeo fallan. Requiere fuente de 650 W y GPU de 8 GB.",new[]{N,N,N,H,H},1200,1000);
            add("MARTA","ADMINISTRADORA","No culpes sólo al disco","El almacenamiento no responde y el sistema se calienta. Descarta primero conexiones.",new[]{N,H|L,D|P,N,N},820,350);
            add("LUCÍA","EDITORA","Falla intermitente","El traslado dejó memoria mal asentada, polvo y una GPU defectuosa. Necesito RAM de 16 GB y vídeo de 8 GB.",new[]{H|U,N,D|P,N,H},1350,1000);
            add("CARLOS","RESPONSABLE DE TI","Certificación del taller","Equipo con varias averías. Comprueba los cinco componentes y entrega un sistema estable.",new[]{H,H,D|P,H,H},1800,1500);
            foreach(var c in list)if(c.Index>=8){c.MinimumPower=650;c.MinimumGraphics=8;}
            list[10].MinimumMemory=16;return list.ToArray();
        }
    }
    [Serializable] public sealed class InventoryEntry {public string id;public int count;}
    [Serializable] public sealed class LevelRecord {public int level,stars,score;}
    [Serializable] public sealed class CareerSnapshot
    {public int version=2,balance=700,reputation=70,xp;public InventoryEntry[] inventory;public LevelRecord[] records;public JobSnapshot active;}
    [Serializable] public sealed class JobSnapshot
    {public int level,mistakes,spent,status,startBalance,startReputation;public float elapsed;public bool connected=true,powered,coverOpen,initialTest,stressPassed;public PartSnapshot[] parts;public InventoryEntry[] startInventory;public string[] journal;}
    public sealed class CareerProfile
    {
        public int Balance {get;private set;}=700;public int Reputation {get;private set;}=70;public int Xp {get;private set;}
        readonly Dictionary<string,int> stock=new Dictionary<string,int>();readonly Dictionary<int,LevelRecord> records=new Dictionary<int,LevelRecord>();
        public int Completed=>records.Count;public int Unlocked=>Math.Min(11,records.Count);
        public CareerProfile(){stock["ram8"]=1;}
        public int Stock(string sku)=>stock.TryGetValue(sku,out int n)?n:0;
        public bool Pay(int n){if(n<0||n>Balance)return false;Balance-=n;return true;}
        public void AddStock(string sku,int n){stock[sku]=Math.Max(0,Stock(sku)+n);}
        public void Reward(int level,int reward,int score)
        {Balance+=reward;Xp+=Math.Max(10,score);Reputation=Math.Min(100,Reputation+Math.Max(1,score/25));if(!records.TryGetValue(level,out LevelRecord r)||score>r.score)records[level]=new LevelRecord{level=level,score=score,stars=score>=85?3:score>=65?2:1};}
        public int Stars(int n)=>records.TryGetValue(n,out LevelRecord r)?r.stars:0;
        public InventoryEntry[] ExportStock()=>stock.Select(p=>new InventoryEntry{id=p.Key,count=p.Value}).ToArray();
        public void RestoreCheckpoint(int balance,int reputation,InventoryEntry[] items)
        {Balance=Math.Max(0,balance);Reputation=Math.Max(0,Math.Min(100,reputation));stock.Clear();if(items!=null)foreach(var i in items)if(i!=null&&PartCatalog.Find(i.id)!=null&&i.count>=0)stock[i.id]=i.count;}
        public CareerSnapshot Export(JobSnapshot active=null)=>new CareerSnapshot{balance=Balance,reputation=Reputation,xp=Xp,inventory=ExportStock(),records=records.Values.ToArray(),active=active};
        public static CareerProfile Restore(CareerSnapshot d)
        {
            if(d==null||d.version!=2)throw new ArgumentException("Formato de guardado no compatible");
            var p=new CareerProfile();p.RestoreCheckpoint(d.balance,d.reputation,d.inventory);p.Xp=Math.Max(0,d.xp);
            if(d.records!=null)foreach(var r in d.records)if(r!=null&&r.level>=0&&r.level<12&&r.score>=0&&r.score<=100)p.records[r.level]=new LevelRecord{level=r.level,score=r.score,stars=Math.Max(1,Math.Min(3,r.stars))};
            for(int i=0;i<12;i++)if(!p.records.ContainsKey(i)){foreach(int k in p.records.Keys.Where(k=>k>i).ToArray())p.records.Remove(k);break;}
            return p;
        }
    }
    public sealed class OperationResult
    {public bool Success;public string Message;public OperationResult(bool success,string message){Success=success;Message=message;}}
    public sealed class SimulationSession
    {
        public CareerProfile Profile {get;private set;}public ContractDefinition Contract {get;private set;}public PartState[] Parts {get;private set;}
        public JobStatus Status {get;private set;}=JobStatus.Active;
        public bool Connected {get;private set;}=true;public bool Powered {get;private set;}public bool CoverOpen {get;private set;}public bool InitialTest {get;private set;}public bool StressPassed {get;private set;}
        public float Elapsed {get;private set;}public int Mistakes {get;private set;}public int Spent {get;private set;}
        public int Score=>Math.Max(0,100-Mistakes*12-(Elapsed>Contract.TimeLimit*.8f?10:0));
        public readonly List<string> Journal=new List<string>();int startBalance,startReputation;InventoryEntry[] startInventory;
        public SimulationSession(CareerProfile p,int level)
        {
            if(p==null||level<0||level>=12||level>p.Unlocked)throw new ArgumentException("Contrato no desbloqueado");
            Profile=p;Contract=CampaignCatalog.Contracts[level];startBalance=p.Balance;startReputation=p.Reputation;startInventory=p.ExportStock();
            Parts=Enumerable.Range(0,5).Select(i=>new PartState((PartKind)i,Contract.Faults[i],i==0&&Contract.MinimumMemory==16?"ram16":PartCatalog.DefaultSku((PartKind)i))).ToArray();Record("Contrato aceptado. Precios y tiempos son valores de juego.");
        }
        bool Active=>Status==JobStatus.Active||Status==JobStatus.Ready;bool Safe=>!Connected&&!Powered&&CoverOpen;
        void Record(string s){Journal.Add(s);if(Journal.Count>24)Journal.RemoveAt(0);}
        OperationResult Ok(string s){Record(s);return new OperationResult(true,s);}
        OperationResult Reject(string s,bool penalty=true)
        {if(penalty&&Active){Mistakes++;if(Contract.MaxMistakes>0&&Mistakes>=Contract.MaxMistakes){Status=JobStatus.Failed;Powered=false;s+=" Encargo suspendido: límite de errores.";}}Record(s);return new OperationResult(false,s);}
        public void Tick(float seconds)
        {if(!Active||seconds<0)return;Elapsed+=seconds;if(Contract.Difficulty!=Difficulty.Guided&&Elapsed>=Contract.TimeLimit){Status=JobStatus.Failed;Powered=false;Record("Se agotó el plazo. Puedes reintentar el encargo.");}}
        public OperationResult ToggleConnection()
        {if(!Active)return Reject("El encargo terminó.",false);if(Powered)return Reject("Apaga antes de desconectar.");if(!Connected&&CoverOpen)return Reject("Cierra la tapa antes de conectar.");Connected=!Connected;return Ok(Connected?"Alimentación conectada.":"Cable desconectado. Ya puedes abrir.");}
        public OperationResult ToggleCover()
        {if(!Active)return Reject("El encargo terminó.",false);if(Connected||Powered)return Reject("Primero apaga y desconecta.");if(CoverOpen&&Parts.Any(p=>!p.IsInstalled))return Reject("Faltan componentes por montar.");CoverOpen=!CoverOpen;return Ok(CoverOpen?"Acceso abierto.":"Tapa cerrada.");}
        public OperationResult Diagnose(PartKind kind,DiagnosticTool tool)
        {
            if(!Active)return Reject("El encargo terminó.",false);if(!Safe)return Reject("El banco aislado exige PC apagada, desconectada y abierta.");
            var part=Parts[(int)kind];if(tool!=DiagnosticTool.Visual&&tool!=part.CorrectTool)return Reject("Prueba no adecuada para esa pieza.");
            if(tool!=DiagnosticTool.Visual&&!part.IsInstalled)return Reject("Monta la pieza para ejecutar esta prueba de banco.");
            Tick(tool==DiagnosticTool.Visual?5:25);if(!Active)return Reject("El plazo se agotó.",false);
            StressPassed=false;return Ok(PartCatalog.PartName(kind)+": "+part.RunDiagnostic(tool));
        }
        public OperationResult Buy(string sku)
        {
            if(!Active)return Reject("El encargo terminó.",false);var i=PartCatalog.Find(sku);if(i==null)return Reject("Recambio desconocido.");
            if(!Contract.Compatible(i,i.Kind))return Reject("Recambio incompatible con la ranura o requisitos del cliente.");
            if(Spent+i.Price>Contract.PartsBudget)return Reject("La compra supera el presupuesto del encargo.");
            if(!Profile.Pay(i.Price))return Reject("Saldo insuficiente. Reintentar recupera las compras de este encargo.",false);
            Spent+=i.Price;Profile.AddStock(i.Id,1);return Ok("Compra: "+i.Name+" (-"+i.Price+" CR).");
        }
        public OperationResult Perform(PartKind kind,RepairAction action,string sku=null)
        {
            if(!Active)return Reject("El encargo terminó.",false);if(!Safe)return Reject("Primero apaga, desconecta y abre.");var p=Parts[(int)kind];
            if(action==RepairAction.Diagnose){if(Contract.Difficulty!=Difficulty.Guided)return Reject("Selecciona una prueba en DIAGNÓSTICO.");return Diagnose(kind,p.CorrectTool);}
            if(!p.CanPerform(action))return Reject("Acción fuera de secuencia. Inspecciona y comprueba la pieza.");
            if(action==RepairAction.Replace){var i=PartCatalog.Find(sku);if(!Contract.Compatible(i,kind))return Reject("Recambio incompatible.");if(Profile.Stock(i.Id)<1)return Reject("Sin stock. Abre la tienda del taller.",false);Profile.AddStock(i.Id,-1);p.SetSku(i.Id);}
            p.TryPerform(action);StressPassed=false;Status=JobStatus.Active;Tick(4);
            return Ok(PartCatalog.PartName(kind)+": "+(action==RepairAction.Remove?"pieza retirada.":action==RepairAction.Replace?"recambio instalado.":action==RepairAction.Clean?"polvo retirado.":action==RepairAction.ApplyPaste?"pasta nueva aplicada.":action==RepairAction.Reconnect?"conector asegurado.":"pieza montada y asentada."));
        }
        public float Temperature(float load=1){var p=Parts[2];return 27+load*20+(p.HasDust?24:0)+(p.NeedsPaste?20:0)+(p.IsFaulty?30:0);}
        public string BootProblem()
        {
            if(Parts.Any(p=>!Contract.Compatible(PartCatalog.Find(p.Sku),p.Kind)))return "COMPATIBILIDAD INCORRECTA · comprobar requisitos del contrato";
            if(!Parts[3].IsHealthy)return "SIN ALIMENTACIÓN ESTABLE · revisar fuente o conector";
            if(Parts.Any(p=>!p.IsInstalled))return "MONTAJE INCOMPLETO";
            if(!Parts[0].IsHealthy)return "POST NO COMPLETADO · memoria no disponible";
            if(!Parts[4].IsHealthy)return "SIN SEÑAL DE VÍDEO · revisar ruta gráfica";
            if(!Parts[1].IsHealthy)return "UNIDAD NO DETECTADA · revisar conexión y estado";
            return Temperature()>85?"PROTECCIÓN TÉRMICA · "+Temperature().ToString("0")+" °C":null;
        }
        public OperationResult TestBoot()
        {
            if(!Active)return Reject("El encargo terminó.",false);if(Powered){Powered=false;return Ok("PC apagada.");}
            if(!Connected||CoverOpen)return Reject("Cierra y conecta el equipo antes de encender.");InitialTest=true;Tick(8);if(!Active)return Reject("El plazo se agotó.",false);
            string problem=BootProblem();Powered=problem==null||(!problem.StartsWith("SIN ALIMENTACIÓN")&&!problem.StartsWith("PROTECCIÓN"));
            if(problem!=null){Status=JobStatus.Active;return Ok(problem);}Status=JobStatus.Ready;
            return Ok("POST CORRECTO · CPU "+Temperature().ToString("0")+" °C. "+(Contract.NeedsStress?"Comprueba estabilidad antes de entregar.":"Listo para entregar."));
        }
        public OperationResult StressTest()
        {
            if(!Active)return Reject("El encargo terminó.",false);if(!Powered||!Connected||CoverOpen)return Reject("Arranca la PC cerrada antes de la prueba de carga.");
            Tick(45);if(!Active)return Reject("El plazo se agotó.",false);if(BootProblem()!=null||Parts.Any(p=>!p.IsHealthy))return Ok("PRUEBA DE CARGA FALLIDA. Todavía existe una avería.");
            StressPassed=true;Status=JobStatus.Ready;return Ok("ESTABILIDAD CORRECTA · carga simulada de 45 s · CPU "+Temperature().ToString("0")+" °C.");
        }
        public OperationResult Deliver()
        {
            if(!Active)return Reject("La entrega ya se registró o el encargo terminó.",false);
            if(Status!=JobStatus.Ready||!Powered||CoverOpen||!Connected||Parts.Any(p=>!p.IsHealthy||!p.IsInstalled))return Reject("El equipo debe arrancar y quedar completo.");
            if(Contract.NeedsStress&&!StressPassed)return Reject("El cliente exige una prueba de estabilidad.");
            Status=JobStatus.Delivered;Profile.Reward(Contract.Index,Contract.Reward,Score);return Ok("Entrega: +"+Contract.Reward+" CR · calidad "+Score+"/100.");
        }
        public OperationResult Hint()
        {if(!Active)return Reject("El encargo terminó.",false);if(Contract.Difficulty!=Difficulty.Guided){Tick(30);Mistakes++;if(Contract.MaxMistakes>0&&Mistakes>=Contract.MaxMistakes){Status=JobStatus.Failed;Powered=false;}}var p=Parts.FirstOrDefault(x=>!x.IsHealthy);return Ok(p==null?"Comprueba arranque, estabilidad y entrega.":"Revisa "+PartCatalog.PartName(p.Kind)+" con "+PartCatalog.ToolName(p.CorrectTool)+". Descarta conexiones antes de sustituir.");}
        public SimulationSession Retry()
        {if(Status==JobStatus.Delivered)throw new InvalidOperationException("El encargo ya fue entregado");Profile.RestoreCheckpoint(startBalance,Math.Max(0,startReputation-(Status==JobStatus.Failed?5:0)),startInventory);return new SimulationSession(Profile,Contract.Index);}
        public JobSnapshot Export()=>new JobSnapshot{level=Contract.Index,mistakes=Mistakes,spent=Spent,status=(int)Status,startBalance=startBalance,startReputation=startReputation,elapsed=Elapsed,connected=Connected,powered=Powered,coverOpen=CoverOpen,initialTest=InitialTest,stressPassed=StressPassed,parts=Parts.Select(p=>p.Export()).ToArray(),startInventory=startInventory,journal=Journal.ToArray()};
        public static SimulationSession Restore(CareerProfile profile,JobSnapshot d)
        {
            if(d==null||d.level<0||d.level>profile.Unlocked||d.level>11||d.status<0||d.status>3||d.parts==null||d.parts.Length!=5)throw new ArgumentException("Encargo guardado no válido");
            var s=new SimulationSession(profile,d.level);var parts=d.parts.Select(PartState.Restore).ToArray();for(int i=0;i<5;i++)if(parts[i].Kind!=(PartKind)i)throw new ArgumentException("Orden de piezas no válido");
            s.Parts=parts;s.Mistakes=Math.Max(0,d.mistakes);s.Spent=Math.Max(0,d.spent);s.Elapsed=Math.Max(0,d.elapsed);s.Connected=d.connected;s.Powered=d.powered;s.CoverOpen=d.coverOpen;s.InitialTest=d.initialTest;s.StressPassed=d.stressPassed;s.Status=(JobStatus)d.status;
            if(s.CoverOpen&&s.Connected||s.Powered&&(!s.Connected||s.CoverOpen))throw new ArgumentException("Estado eléctrico incoherente");
            s.startBalance=Math.Max(0,d.startBalance);s.startReputation=Math.Max(0,Math.Min(100,d.startReputation));s.startInventory=d.startInventory;s.Journal.Clear();if(d.journal!=null)s.Journal.AddRange(d.journal.Take(24));return s;
        }
    }
}
