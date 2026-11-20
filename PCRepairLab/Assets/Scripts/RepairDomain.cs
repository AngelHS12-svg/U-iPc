using System;
using UnityEngine;

namespace PcFixLab
{
    // Encapsulación: el progreso y el dinero sólo cambian mediante métodos del perfil.
    [Serializable]
    public sealed class WorkshopProfile
    {
        public int CompletedOrders { get; private set; }
        public int Credits { get; private set; }
        public WorkshopProfile(int completed = 0, int credits = 0)
        { CompletedOrders = Mathf.Clamp(completed, 0, 3); Credits = Mathf.Max(0, credits); }
        public void CompleteOrder(int reward) { CompletedOrders++; Credits += reward; }
        public void Save()
        {
            PlayerPrefs.SetInt("CompletedOrders", CompletedOrders);
            PlayerPrefs.SetInt("Credits", Credits);
            PlayerPrefs.Save();
        }
        public static WorkshopProfile Load()
        { return new WorkshopProfile(PlayerPrefs.GetInt("CompletedOrders", 0), PlayerPrefs.GetInt("Credits", 0)); }
    }

    [Serializable]
    public sealed class RepairOrder
    {
        public string ClientName { get; private set; }
        public string ClientRole { get; private set; }
        public string Title { get; private set; }
        public string Story { get; private set; }
        public PartKind Target { get; private set; }
        public int Reward { get; private set; }
        public Color ClientColor { get; private set; }
        public ContractDefinition Contract { get; private set; }
        public RepairOrder(ContractDefinition c)
        {
            Contract=c;ClientName=c.Client;ClientRole=c.Role;Title=c.Title;Story=c.Story;Reward=c.Reward;
            Target=(PartKind)Mathf.Max(0,Array.FindIndex(c.Faults,f=>f!=PartFault.None));
            ClientColor=c.World==WorldKind.Home?new Color(.84f,.56f,.31f):c.World==WorldKind.Campus?new Color(.34f,.57f,.78f):new Color(.60f,.45f,.72f);
        }
        public RepairOrder(string name, string role, string title, string story, PartKind target, int reward, Color color)
        { ClientName = name; ClientRole = role; Title = title; Story = story; Target = target; Reward = reward; ClientColor = color; }
        public static RepairOrder[] CreateOrders()
        {
            return Array.ConvertAll(CampaignCatalog.Contracts,c=>new RepairOrder(c));
        }
    }

    // Abstracción: todos los objetos seleccionables ofrecen la misma interfaz.
    public abstract class Interactable : MonoBehaviour
    {
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public virtual void Select(RepairGame game) { game.Select(this); }
    }

    // Herencia y polimorfismo: cada componente define su prueba y su fallo.
    public abstract class RepairablePart : Interactable
    {
        public abstract PartKind Kind { get; }
        public abstract string Specification { get; }
        protected abstract string FailureMessage { get; }
        PartState condition;
        PartState State => condition ?? (condition = new PartState(Kind, false));
        public PartState Condition => State;
        public void BindState(PartState state)
        { condition=state;transform.position=MountedPosition;transform.rotation=MountedRotation; }
        public bool IsFaulty => State.IsFaulty;
        public bool IsInstalled => State.IsInstalled;
        public bool Diagnosed => State.Diagnosed;
        public bool Replaced => State.Replaced;
        public bool HasDust => State.HasDust;
        public bool NeedsPaste => State.NeedsPaste;
        public bool Serviced => State.Serviced;
        public bool IsHealthy => State.IsHealthy;
        public override string Description => Specification;
        public Vector3 MountedPosition { get; private set; }
        public Quaternion MountedRotation { get; private set; }

        public void Configure(bool faulty)
        {
            condition = new PartState(Kind, faulty);
            MountedPosition = transform.position; MountedRotation = transform.rotation;
        }

        public string Diagnose()
        {
            State.TryPerform(RepairAction.Diagnose);
            return IsHealthy ? "Prueba correcta. El componente funciona bien." : FailureMessage;
        }
        public bool CanPerform(RepairAction action)
        {
            return State.CanPerform(action);
        }
        public void Remove() { State.TryPerform(RepairAction.Remove); }
        public void Replace() { State.TryPerform(RepairAction.Replace); }
        public void Clean() { State.TryPerform(RepairAction.Clean); }
        public void ApplyPaste() { State.TryPerform(RepairAction.ApplyPaste); }
        public void Reinstall() { State.TryPerform(RepairAction.Reinstall); }
    }

    public sealed class MemoryModule : RepairablePart
    {
        public override PartKind Kind => PartKind.Memory;
        public override string DisplayName => "Memoria RAM";
        public override string Specification => "DDR4 · 8 GB · 2666 MHz";
        protected override string FailureMessage => "ERROR DE MEMORIA: el módulo no supera la prueba. Retíralo e instala el recambio compatible.";
    }
    public sealed class StorageDrive : RepairablePart
    {
        public override PartKind Kind => PartKind.Storage;
        public override string DisplayName => "Unidad SSD";
        public override string Specification => "SATA · 480 GB · 2,5 pulgadas";
        protected override string FailureMessage => "ERROR DE DISCO: la unidad no responde. Sustitúyela por el SSD de recambio. La recuperación de datos queda fuera de este encargo.";
    }
    public sealed class CoolingAssembly : RepairablePart
    {
        public override PartKind Kind => PartKind.Cooling;
        public override string DisplayName => "Disipador del procesador";
        public override string Specification => "Ventilador · disipador · pasta térmica";
        protected override string FailureMessage => "TEMPERATURA ALTA: hay polvo y pasta térmica seca. Limpia, retira el disipador, cambia la pasta y vuelve a montarlo.";
    }
    public sealed class PowerSupplyUnit : RepairablePart
    {
        public override PartKind Kind=>PartKind.PowerSupply;
        public override string DisplayName=>"Fuente de alimentación";
        public override string Specification=>"ATX · potencia según contrato";
        protected override string FailureMessage=>"Revisa la salida de la fuente y su conector con el probador de banco.";
    }
    public sealed class GraphicsCard : RepairablePart
    {
        public override PartKind Kind=>PartKind.Graphics;
        public override string DisplayName=>"Tarjeta gráfica";
        public override string Specification=>"PCIe · memoria según contrato";
        protected override string FailureMessage=>"Comprueba salida de vídeo y conexión antes de comprar otra GPU.";
    }
    public sealed class CaseCover : Interactable
    {
        public override string DisplayName => "Tapa del gabinete";
        public override string Description => "Panel de acceso · bisagra de servicio";
    }
    public sealed class PowerConnector : Interactable
    {
        public override string DisplayName => "Cable de alimentación";
        public override string Description => "Desconecta la alimentación antes de abrir o manipular la PC.";
    }
    public sealed class TestMonitor : Interactable
    {
        public override string DisplayName => "Monitor de diagnóstico";
        public override string Description => "Usa ENCENDER Y PROBAR para comprobar la reparación.";
    }
}
