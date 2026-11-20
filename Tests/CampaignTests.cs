using System;
using System.Linq;
using PcFixLab;
public static class CampaignTests
{
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static CareerProfile Unlocked(int level)
    {var p=new CareerProfile();var d=p.Export();d.balance=20000;d.records=Enumerable.Range(0,level).Select(i=>new LevelRecord{level=i,score=100,stars=3}).ToArray();return CareerProfile.Restore(d);}
    static void Safe(SimulationSession s)
    {if(s.Powered)s.TestBoot();Check(s.ToggleConnection().Success,"Disconnect safely");Check(s.ToggleCover().Success,"Open isolated PC");}
    static void Repair(SimulationSession s)
    {
        for(int n=0;n<5;n++)
        {
            var k=(PartKind)n;var p=s.Parts[n];if(p.IsHealthy)continue;
            Check(s.Diagnose(k,DiagnosticTool.Visual).Success,"Visual inspection "+k);
            if(p.LooseCable)Check(s.Perform(k,RepairAction.Reconnect).Success,"Secure connector "+k);
            if(p.Unseated){Check(s.Perform(k,RepairAction.Remove).Success,"Remove unseated "+k);Check(s.Perform(k,RepairAction.Reinstall).Success,"Reseat "+k);}
            Check(s.Diagnose(k,p.CorrectTool).Success,"Specific test "+k);
            if(p.IsFaulty)
            {
                Check(p.HardwareConfirmed,"Fault confirmed before replacement");
                Check(s.Perform(k,RepairAction.Remove).Success,"Remove faulty "+k);
                var item=PartCatalog.Items.Where(i=>s.Contract.Compatible(i,k)).OrderBy(i=>i.Price).First();
                if(s.Profile.Stock(item.Id)==0)Check(s.Buy(item.Id).Success,"Buy compatible "+item.Id);
                Check(s.Perform(k,RepairAction.Replace,item.Id).Success,"Install "+item.Id);
            }
            if(p.HasDust)Check(s.Perform(k,RepairAction.Clean).Success,"Clean cooling");
            if(p.NeedsPaste){Check(s.Perform(k,RepairAction.Remove).Success,"Remove cooler");Check(s.Perform(k,RepairAction.ApplyPaste).Success,"Apply paste");Check(s.Perform(k,RepairAction.Reinstall).Success,"Reinstall cooler");}
            Check(p.IsHealthy&&p.IsInstalled,"Repaired component "+k);
        }
    }
    public static int Main()
    {
        try
        {
            Check(CampaignCatalog.Contracts.Length==12,"Twelve contracts");Check(CampaignCatalog.Contracts.Select(c=>c.World).Distinct().Count()==3,"Three worlds");
            var career=new CareerProfile();
            for(int i=0;i<12;i++)
            {
                var s=new SimulationSession(career,i);int balance=career.Balance;
                Check(s.TestBoot().Success,"Initial symptom "+i);Safe(s);Repair(s);
                Check(s.ToggleCover().Success,"Close assembled PC");Check(s.ToggleConnection().Success,"Reconnect closed PC");Check(s.TestBoot().Success&&s.Status==JobStatus.Ready,"POST passes "+i);
                if(s.Contract.NeedsStress)Check(s.StressTest().Success&&s.StressPassed,"Stress passes "+i);
                Check(s.Deliver().Success,"Deliver contract "+i);int paid=career.Balance;
                Check(!s.Deliver().Success&&career.Balance==paid,"No double reward "+i);
                Check(career.Completed==i+1&&career.Stars(i)==3,"Progress and three stars "+i);
                var restored=CareerProfile.Restore(career.Export(s.Export()));var work=SimulationSession.Restore(restored,s.Export());
                Check(restored.Balance==career.Balance&&work.Status==JobStatus.Delivered&&!work.Deliver().Success,"Delivered save cannot duplicate reward");
            }
            Check(career.Completed==12,"Campaign completes with starting funds and legal inventory");
            bool locked=false;try{new SimulationSession(new CareerProfile(),2);}catch(ArgumentException){locked=true;}Check(locked,"Locked contracts unavailable");
            var a=new SimulationSession(new CareerProfile(),0);Check(!a.ToggleCover().Success&&!a.CoverOpen,"Safety rejects connected opening");
            Safe(a);int cash=a.Profile.Balance,spent=a.Spent;
            Check(!a.Buy("ddr5").Success&&a.Profile.Balance==cash&&a.Spent==spent,"Wrong DDR generation does not charge");
            Check(!a.Buy("nvme").Success&&a.Profile.Balance==cash,"Wrong storage interface does not charge");
            Check(a.Buy("ram16").Success,"Compatible purchase within budget");
            var retry=a.Retry();Check(retry.Profile.Balance==700&&retry.Profile.Stock("ram16")==0&&retry.Profile.Stock("ram8")==1,"Retry restores wallet and inventory");
            var cap=new SimulationSession(Unlocked(8),8);int before=cap.Profile.Balance;
            Check(!cap.Buy("psu450").Success&&cap.Profile.Balance==before,"Power requirement enforced");Check(!cap.Buy("gpu4").Success,"VRAM requirement enforced");
            var loose=new SimulationSession(Unlocked(9),9);Safe(loose);loose.Diagnose(PartKind.Storage,DiagnosticTool.Smart);
            Check(!loose.Parts[1].HardwareConfirmed,"Loose cable makes SMART inconclusive");
            loose.Perform(PartKind.Storage,RepairAction.Remove);Check(!loose.Perform(PartKind.Storage,RepairAction.Replace,"ssd480").Success,"Cannot replace without fault confirmation");
            var mixed=new PartState(PartKind.Memory,PartFault.Hardware|PartFault.Unseated);mixed.RunDiagnostic(DiagnosticTool.Memory);mixed.TryPerform(RepairAction.Remove);
            Check(mixed.TryPerform(RepairAction.Reinstall)&&!mixed.Unseated&&mixed.IsFaulty,"Can reseat faulty memory before confirming its fault");
            mixed.RunDiagnostic(DiagnosticTool.Memory);Check(mixed.HardwareConfirmed,"Fault discoverable after reseating");
            var timed=new SimulationSession(Unlocked(8),8);timed.Tick(601);Check(timed.Status==JobStatus.Failed&&!timed.Powered,"Expert deadline fails and powers off");
            Check(!timed.Buy("psu650").Success,"Failed contract cannot buy");
            int rep=timed.Profile.Reputation;var next=timed.Retry();Check(next.Profile.Reputation==rep-5&&next.Status==JobStatus.Active,"Failure retry has reputation consequence");
            var mistakes=new SimulationSession(Unlocked(8),8);for(int i=0;i<4;i++)mistakes.ToggleCover();Check(mistakes.Status==JobStatus.Failed,"Expert error limit enforced");
            var hints=new SimulationSession(Unlocked(8),8);for(int i=0;i<4;i++)hints.Hint();Check(hints.Status==JobStatus.Failed,"Hints also enforce error limit");
            var sandbox=new SimulationSession(new CareerProfile(),0);sandbox.Tick(100000);Check(sandbox.Status==JobStatus.Active,"Guided mode has no hard deadline");
            var snap=new SimulationSession(Unlocked(5),5);Safe(snap);snap.Diagnose(PartKind.Cooling,DiagnosticTool.Thermal);snap.Perform(PartKind.Cooling,RepairAction.Clean);snap.Perform(PartKind.Cooling,RepairAction.Remove);
            var copy=SimulationSession.Restore(CareerProfile.Restore(snap.Profile.Export()),snap.Export());Check(!copy.Connected&&copy.CoverOpen&&!copy.Parts[2].IsInstalled&&!copy.Parts[2].HasDust&&copy.Parts[2].NeedsPaste,"Partial repair round trip");
            snap.Export();var corrupt=snap.Export();corrupt.connected=true;bool invalid=false;try{SimulationSession.Restore(snap.Profile,corrupt);}catch(ArgumentException){invalid=true;}Check(invalid,"Reject unsafe saved power state");
            Console.WriteLine("PASSED: "+checks+" campaign checks");return 0;
        }
        catch(Exception e){Console.Error.WriteLine("FAILED: "+e);return 1;}
    }
}
