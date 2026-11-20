using System;
namespace PcFixLab
{
    public enum PartKind { Memory, Storage, Cooling, PowerSupply, Graphics }
    public enum RepairAction { Diagnose, Remove, Replace, Clean, ApplyPaste, Reinstall, Reconnect }
    [Flags] public enum PartFault { None=0, Hardware=1, LooseCable=2, Unseated=4, Dust=8, DryPaste=16 }
    public enum DiagnosticTool { Visual, Memory, Smart, Thermal, Power, Video }
    [Serializable] public sealed class PartSnapshot
    {
        public int kind,faults;
        public bool installed,diagnosed,inspected,hardwareConfirmed,serviced,replaced;
        public string sku;
    }
    public sealed class PartState
    {
        public PartKind Kind {get;private set;}
        public PartFault Faults {get;private set;}
        public bool IsFaulty => (Faults&PartFault.Hardware)!=0;
        public bool HasDust => (Faults&PartFault.Dust)!=0;
        public bool NeedsPaste => (Faults&PartFault.DryPaste)!=0;
        public bool LooseCable => (Faults&PartFault.LooseCable)!=0;
        public bool Unseated => (Faults&PartFault.Unseated)!=0;
        public bool IsInstalled {get;private set;}=true;
        public bool Diagnosed {get;private set;}
        public bool Inspected {get;private set;}
        public bool HardwareConfirmed {get;private set;}
        public bool Replaced {get;private set;}
        public bool Serviced {get;private set;}
        public bool IsHealthy => Faults==PartFault.None;
        public string Sku {get;private set;}
        public PartState(PartKind k,bool bad):this(k,!bad?PartFault.None:k==PartKind.Cooling?PartFault.Dust|PartFault.DryPaste:PartFault.Hardware){}
        public PartState(PartKind k,PartFault faults,string sku=null){Kind=k;Faults=faults;Sku=sku??PartCatalog.DefaultSku(k);}
        public DiagnosticTool CorrectTool => Kind==PartKind.Memory?DiagnosticTool.Memory:Kind==PartKind.Storage?DiagnosticTool.Smart:Kind==PartKind.Cooling?DiagnosticTool.Thermal:Kind==PartKind.PowerSupply?DiagnosticTool.Power:DiagnosticTool.Video;
        public string Inspect()
        {
            Inspected=true;if(HasDust||NeedsPaste||LooseCable||Unseated)Diagnosed=true;
            return (HasDust?"Polvo acumulado. ":"")+(LooseCable?"Conector flojo. ":"")+(Unseated?"Pieza mal asentada. ":"")+
                (Kind==PartKind.Cooling&&!IsInstalled&&NeedsPaste?"Pasta seca. ":"")+"Una prueba específica permite descartar averías internas.";
        }
        public string RunDiagnostic(DiagnosticTool tool)
        {
            if(tool==DiagnosticTool.Visual)return Inspect();
            if(tool!=CorrectTool)return "Prueba no adecuada para esta pieza.";
            Diagnosed=true;
            if(LooseCable||Unseated)return "INCONCLUSO: corrige conexión o asiento y repite antes de sustituir.";
            HardwareConfirmed=true;
            return IsFaulty?"FALLO CONFIRMADO: no supera la prueba. Requiere un recambio compatible.":HasDust||NeedsPaste?"Temperatura anormal. Revisa polvo y pasta térmica.":"PRUEBA CORRECTA: no se detecta una avería en esta simulación.";
        }
        public bool CanPerform(RepairAction action)
        {
            switch(action)
            {
                case RepairAction.Diagnose:return true;
                case RepairAction.Remove:return IsInstalled&&Diagnosed;
                case RepairAction.Replace:return !IsInstalled&&Diagnosed&&IsFaulty&&HardwareConfirmed;
                case RepairAction.Clean:return HasDust&&Diagnosed;
                case RepairAction.ApplyPaste:return Kind==PartKind.Cooling&&!IsInstalled&&Diagnosed&&!HasDust&&NeedsPaste;
                case RepairAction.Reinstall:return !IsInstalled&&(!IsFaulty||Unseated);
                case RepairAction.Reconnect:return IsInstalled&&Diagnosed&&LooseCable;
                default:return false;
            }
        }
        public bool TryPerform(RepairAction action)
        {
            if(!CanPerform(action))return false;
            switch(action)
            {
                case RepairAction.Diagnose:Diagnosed=true;Inspected=true;HardwareConfirmed=!LooseCable&&!Unseated;break;
                case RepairAction.Remove:IsInstalled=false;break;
                case RepairAction.Replace:Faults=PartFault.None;Replaced=true;IsInstalled=true;Serviced=true;break;
                case RepairAction.Clean:Faults &= ~PartFault.Dust;Serviced=true;break;
                case RepairAction.ApplyPaste:Faults &= ~PartFault.DryPaste;Serviced=true;break;
                case RepairAction.Reinstall:Faults &= ~PartFault.Unseated;IsInstalled=true;Serviced=true;break;
                case RepairAction.Reconnect:Faults &= ~PartFault.LooseCable;Serviced=true;break;
            }
            return true;
        }
        public void SetSku(string sku){Sku=sku;}
        public PartSnapshot Export()=>new PartSnapshot{kind=(int)Kind,faults=(int)Faults,installed=IsInstalled,diagnosed=Diagnosed,inspected=Inspected,hardwareConfirmed=HardwareConfirmed,serviced=Serviced,replaced=Replaced,sku=Sku};
        public static PartState Restore(PartSnapshot d)
        {
            if(d==null||d.kind<0||d.kind>4||(d.faults&~31)!=0)throw new ArgumentException("Estado de pieza no válido");
            return new PartState((PartKind)d.kind,(PartFault)d.faults,d.sku){IsInstalled=d.installed,Diagnosed=d.diagnosed,Inspected=d.inspected,HardwareConfirmed=d.hardwareConfirmed,Serviced=d.serviced,Replaced=d.replaced};
        }
    }
}
