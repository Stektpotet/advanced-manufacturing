using System.Collections;
using UnityEngine;
using KIS;
using KISAPIv1;
using System.Collections.Generic;
using KSPDev.LogUtils;

namespace AdvancedManufacturing
{
    [KSPAddon(KSPAddon.Startup.EditorVAB, once: true)]
    public class AdvancedManufacturingAddon : MonoBehaviour
    {
        void Start()
        {
            DebugEx.Info("AdvancedManufacturingAddon Loaded! --- Make manufacturing great again!");
        }
    }
    

    public class ModulePartFactory : PartModule//, IResourceConsumer
    {
        [KSPField] public string requiredResourceName = KSPDev.ResourceUtils.StockResourceNames.Ore;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Currently producing")] public string currentlyManufacturingText = "Nothing";

        ModuleKISInventory storage;
        AvailablePart currentlyManufacturing;

        public override void OnStart( StartState state )
        {
            storage = part.FindModuleImplementing<ModuleKISInventory>();
        }

        [KSPEvent(guiName = "Mock Select Part", guiActive = true)]
        void MockSelection()
        {
            var randomPart = PartLoader.LoadedPartsList[Random.Range(0, PartLoader.LoadedPartsList.Count)];
            SelectManufacturingPart(randomPart);
        }

        void SelectManufacturingPart( AvailablePart partToMake, PartVariant variant = null )
        {
            DebugEx.Info("Attempting to manufacture {0}", partToMake.partConfig.ToString());
            var volume = KISAPI.PartUtils.GetPartVolume(partToMake, variant: variant);
            if (volume > storage.maxVolume)
            {
                ScreenMessages.PostScreenMessage(
                    $"{part.partName} on {vessel.vesselName} cannot manufacture {partToMake.title} " +
                    "as it exeeds the volume of the factory!",
                    5f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            ScreenMessages.PostScreenMessage(
                   $"{vessel.vesselName} will start producing {partToMake.title}...",
                   5f, ScreenMessageStyle.UPPER_CENTER);
            SelectManufacturingPartInternal(partToMake);
        }


        [KSPEvent(guiName = "Start Production", guiActive = true)]
        void StartProduction()
        {
            if (currentlyManufacturing == null)
            {
                ScreenMessages.PostScreenMessage(
                  $"{part.partName} is not set to produce anything!",
                  5f, ScreenMessageStyle.UPPER_CENTER);
            }
            if (storage == null)
                DebugEx.Error("No storage was found for the produced item to be put in!");
            if (storage.isFull())
            {
                DebugEx.Info("Storage was full!");
                return;
            }

            var partConfig = KISAPI.PartNodeUtils.PartSnapshot(currentlyManufacturing.partPrefab);
            DebugEx.Info("Setting KIS to create part: {0}", partConfig.ToString());
            KIS_Shared.CreatePart(
                partConfig,
                vessel.GetWorldPos3D() + vessel.up * 3, 
                Quaternion.identity, 
                part, 
                onPartReady: p => { DebugEx.Info("Made part: {0}", p); storage.AddItem(p, 1, 0); }
            );
            //var manufacturedPart = KIS_Shared.CreatePart(currentlyManufacturing, vessel.transform.position + vessel.transform.up * 4, Quaternion.identity, part);

            //storage.AddItem(manufacturedPart, slot: storage.GetFreeSlot());
        }

        void SelectManufacturingPartInternal( AvailablePart partToMake )
        {
            currentlyManufacturing = partToMake;
            currentlyManufacturingText = (partToMake != null) ? currentlyManufacturing.title : "Nothing";
        }

        public override void OnLoad( ConfigNode node )
        {
            DebugEx.Info($"loading {node}");
            var manufacturingPart = node.GetValue("currently_manufacturing");
            if(manufacturingPart != "None")
                SelectManufacturingPartInternal(new AvailablePart(manufacturingPart));
            else
                SelectManufacturingPartInternal(null);
        }

        public override void OnSave( ConfigNode node )
        {
            DebugEx.Info($"saving {node}");
            if (currentlyManufacturing == null)
                node.AddValue("currently_manufacturing", "None");
            else
                node.AddValue("currently_manufacturing", currentlyManufacturing.partPath);
        }
    }



    //public class PartFactory : PartModule //TODO: Rename ModulePartFactory
    //{
    //    // IDEA: Early stage assemblers require kerbal engineers

    //    // TODO: ManufacturingQueue -> if KIS-storage is sufficient, transfer the part
    //    // TODO: ModuleAssembler -> fetch sub-assemblies from VAB list?
    //    // TODO: ShipAssembler -> size/volume/part count restrictions?

    //    [KSPField(isPersistant = true, guiName = "Running", guiActive = true)]
    //    public bool isRunning;

    //    [KSPField(guiActive = true, guiActiveEditor = true)]
    //    public string requiredResourceName = "Ore";

    //    [KSPField(guiActive = true, guiActiveEditor = true)]
    //    public float resourceConsumptionRate = 10f; // 10 units/s

    //    [KSPField(guiActive = true, guiActiveEditor = true)]
    //    public float consumedResources = 0;

    //    PartResourceDefinition resourceDefinition;
        
    //    // BaseField for showing info in editor?
        
    //    [KSPEvent(guiName = "Start Construction", name = "StartManufacturer", guiActive = true, guiActiveUncommand = true, guiActiveUnfocused = true, guiActiveEditor = false, externalToEVAOnly = false)]
    //    public void StartManufacturer()
    //    {
    //        isRunning = true;
    //    }

    //    void SelectManufacturingPart()
    //    {

    //    }

    //    public void Start()
    //    {
    //        resourceDefinition = PartResourceLibrary.Instance.GetDefinition(requiredResourceName);
    //    }

    //    void Update()
    //    {
    //        if (isRunning)
    //        {
    //            var required = TimeWarp.deltaTime * resourceConsumptionRate;
    //            if (ResourceAvailable() > required)
    //                RequestResource(required);
    //            else
    //                OnNoPower();
    //        }
    //    }

    //    private double ResourceAvailable()
    //    {
    //        part.GetConnectedResourceTotals(resourceDefinition.id, ResourceFlowMode.STACK_PRIORITY_SEARCH, out var amount, out var maxAmount);
    //        return amount;
    //    }

    //    private double RequestResource( double amount )
    //    {
    //        return part.RequestResource(resourceDefinition.id, amount, ResourceFlowMode.STACK_PRIORITY_SEARCH);
    //    }

    //    private void OnNoPower()
    //    {
    //        ScreenMessages.PostScreenMessage($"Not enough {requiredResourceName}", 5f, ScreenMessageStyle.UPPER_CENTER);
    //        isRunning = false;
    //    }
    //}
}
