using System;
using PcFixLab;

public static class PartStateTests
{
    static int count;
    static void Check(bool value,string message)
    { if(!value)throw new Exception(message);count++;Console.WriteLine("PASS: "+message); }
    public static int Main()
    {
        try
        {
            foreach(var kind in new[]{PartKind.Memory,PartKind.Storage})
            {
                var part=new PartState(kind,true);
                Check(part.IsInstalled&&!part.IsHealthy,kind+": avería inicial");
                Check(!part.TryPerform(RepairAction.Remove),kind+": diagnosticar antes de retirar");
                Check(!part.TryPerform(RepairAction.Replace),kind+": no sustituir componente montado");
                Check(part.TryPerform(RepairAction.Diagnose)&&part.Diagnosed,kind+": diagnóstico");
                Check(part.TryPerform(RepairAction.Remove)&&!part.IsInstalled,kind+": retirar");
                Check(!part.TryPerform(RepairAction.Reinstall),kind+": no reinstalar pieza averiada");
                Check(part.TryPerform(RepairAction.Replace)&&part.IsHealthy&&part.IsInstalled&&part.Replaced&&part.Serviced,kind+": recambio completo");
                Check(!part.TryPerform(RepairAction.Replace),kind+": impedir segundo recambio");
            }
            var healthy=new PartState(PartKind.Memory,false);
            healthy.TryPerform(RepairAction.Diagnose);healthy.TryPerform(RepairAction.Remove);
            Check(!healthy.TryPerform(RepairAction.Replace),"No consumir recambio para pieza sana");
            Check(healthy.TryPerform(RepairAction.Reinstall)&&healthy.IsHealthy&&healthy.IsInstalled,"Recuperar pieza sana retirada");
            var cooling=new PartState(PartKind.Cooling,true);
            Check(cooling.HasDust&&cooling.NeedsPaste&&!cooling.IsHealthy,"Avería térmica inicial");
            Check(!cooling.TryPerform(RepairAction.Clean),"Diagnosticar antes de limpiar");
            cooling.TryPerform(RepairAction.Diagnose);
            Check(!cooling.TryPerform(RepairAction.ApplyPaste),"No aplicar pasta con disipador montado");
            cooling.TryPerform(RepairAction.Remove);
            Check(!cooling.TryPerform(RepairAction.ApplyPaste),"Retirar polvo antes de aplicar pasta");
            Check(cooling.TryPerform(RepairAction.Clean)&&!cooling.HasDust&&!cooling.IsHealthy,"Limpiar no resuelve la pasta deteriorada");
            Check(cooling.TryPerform(RepairAction.ApplyPaste)&&cooling.IsHealthy,"Cambiar pasta resuelve el estado térmico");
            Check(!cooling.IsInstalled,"El servicio no monta automáticamente el disipador");
            Check(cooling.TryPerform(RepairAction.Reinstall)&&cooling.IsInstalled,"Montaje final del disipador");
            Check(!cooling.TryPerform(RepairAction.Clean)&&!cooling.TryPerform(RepairAction.ApplyPaste),"No repetir mantenimiento innecesario");
            foreach(PartKind kind in Enum.GetValues(typeof(PartKind)))
            {
                var original=new PartState(kind,true);original.TryPerform(RepairAction.Diagnose);original.TryPerform(RepairAction.Remove);
                var reset=new PartState(kind,true);
                Check(reset.IsInstalled&&!reset.Diagnosed&&!reset.Serviced&&!reset.IsHealthy,kind+": reinicio restaura estado inicial");
                foreach(RepairAction action in Enum.GetValues(typeof(RepairAction)))
                {
                    var state=new PartState(kind,true);state.TryPerform(action);
                    Check(!state.IsHealthy,kind+": una acción aislada no puede completar una reparación ("+action+")");
                }
            }
            Console.WriteLine("PASSED: "+count+" checks");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine("FAILED: "+ex.Message);return 1;}
    }
}
