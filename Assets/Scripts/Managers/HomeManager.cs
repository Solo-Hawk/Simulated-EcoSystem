﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Resourcing;

public class HomeManager : MonoBehaviour
{
    [Header("Home HangarBays")]
    [SerializeField]
    public GameObject[] HangarBay;

    [Header("Attention Ranges")]
    public float ExtractorRange;
    public float StorageRange;
    public float HomeRange;

    [Header("Home Base Costs")]
    public float ResourceConsumptionRate;
    private float m_RCRSecond;

    [Header("Civilian Ships")]
    public GameObject[] CivilianPrefabs;
    public int CivilianMax;
    public int CurrentCivilians;
    public float CivilianRange;

    [Header("Carrier Ships")]
    public GameObject[] CarrierPrefabs;
    public int CarrierMax;
    public int CarrierCurrent;
    public float CarrierRange;
    private List<Carrier> Carriers;

    [Header("Miner Ships")]
    public GameObject[] MinerPrefabs;
    public int MinerMax;
    public int MinerCurrent;
    public float MiningRange;

    [Header("Fighter Ships")]
    public GameObject[] FighterPrefabs;
    public int FighterMax;
    public float FighterRange;

    #region Mission Assignment Dictionaries
    private Dictionary<Container, List<Carrier>> ContainerHandlers = new Dictionary<Container, List<Carrier>>();
    private List<HomeManager> HomeManagerHandlers = new List<HomeManager>();
    private Dictionary<MineralMasterNode, List<Miner>> MiningHandlers = new Dictionary<MineralMasterNode, List<Miner>>(); 
    #endregion

    public delegate void LaunchDelegate(GameObject go);
    protected Queue<LaunchDelegate> LaunchQueue = new Queue<LaunchDelegate>();

    private Resource resource;

    private void Awake()
    {
        resource = GetComponent<Resource>();

        CurrentCivilians = CivilianMax;
        CarrierCurrent = CarrierMax;
        MinerCurrent = MinerMax;

        m_RCRSecond = ResourceConsumptionRate / 60f;


        FindContainers();
        FindHomemanagers();
        FindMinerals();

        StartCoroutine(HangarLaunch());

        StartCoroutine(CivilianUpdater());
        StartCoroutine(CarrierUpdater());
        StartCoroutine(MinerUpdater());
    }

    private void FindContainers()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, StorageRange, Physics.AllLayers, QueryTriggerInteraction.Collide);
        foreach (Collider col in hitColliders)
        {
            Container current = col.transform.GetComponent<Container>();

            Debug.LogWarning("Has Hit");
            Debug.LogWarning(current);

            if (current != null)
            {
                if (!ContainerHandlers.ContainsKey(current))
                {
                    Debug.LogWarning("Add Container");
                    ContainerHandlers.Add(current, new List<Carrier>());
                }
            }
        }
    }

    private void FindHomemanagers()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, HomeRange, Physics.AllLayers, QueryTriggerInteraction.Collide);
        foreach (Collider col in hitColliders)
        {
            HomeManager current = col.transform.GetComponent<HomeManager>();

            if (current != null && current != this)
            {
                if (!HomeManagerHandlers.Contains(current))
                {
                    Debug.LogError("Add HomeManager");
                    HomeManagerHandlers.Add(current);
                }
            }
        }
    }

    private void FindMinerals()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, StorageRange, Physics.AllLayers, QueryTriggerInteraction.Collide);
        foreach (Collider col in hitColliders)
        {
            MineralMasterNode current = col.transform.GetComponent<MineralMasterNode>();

            if (current != null)
            {
                if (!MiningHandlers.ContainsKey(current))
                {
                    Debug.LogError("Add Mineral");
                    MiningHandlers.Add(current, new List<Miner>());
                }
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        resource.Pool = resource.Pool - (m_RCRSecond * Time.fixedDeltaTime) < 0 ? 0 : resource.Pool - (m_RCRSecond * Time.fixedDeltaTime);
    }

    IEnumerator HangarLaunch()
    {
        while (true)
        {
            foreach(GameObject hangar in HangarBay) 
            {
                if (LaunchQueue.Count > 0)
                {
                    LaunchQueue.Dequeue().Invoke(hangar);
                }
            }
            

            yield return new WaitForSeconds(2f);
        }

    }

    IEnumerator CivilianUpdater()
    {
        yield return new WaitForFixedUpdate();
        while (true)
        {
            UpdateCivilianMission();
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator CarrierUpdater()
    {
        yield return new WaitForFixedUpdate();
        while (true)
        {
            UpdateCarrierMissions();
            yield return new WaitForSeconds(5f);
        }
    }

    IEnumerator MinerUpdater()
    {
        yield return new WaitForFixedUpdate();
        while (true)
        {
            UpdateMinerMissions();
            yield return new WaitForSeconds(5f);
        }
    }

    public void AskForNewOrder(UnitBase unitBase)
    {

    }

    void UpdateCivilianMission()
    {
        foreach (var item in HomeManagerHandlers)
        {
            if (CurrentCivilians > 0) 
            {
                float weight = (transform.position - item.transform.position).magnitude / HomeRange;
                if (weight > Random.Range(0f, 1f)) 
                {
                    CurrentCivilians--;
                    LaunchQueue.Enqueue((hangar) =>
                    {
                        LaunchNewCivilian(hangar, item);
                    });
                }                
            }
            
        }
        

    }

    void LaunchNewCivilian(GameObject hangar, HomeManager destination) 
    {
        Debug.Log("Launching New Civilian");

        GameObject go = Instantiate(
            CivilianPrefabs[Random.Range(0, CivilianPrefabs.Length)],
            hangar.transform.position,
            Quaternion.LookRotation(hangar.transform.localPosition));

        go.GetComponent<UnitBase>().home = this;

        //go.GetComponent<Rigidbody>().AddForce(hangar.transform.localPosition.normalized * 300f);

        StartCoroutine(LaunchCivilianMission(go, destination));
    }

    IEnumerator LaunchCivilianMission(GameObject go, HomeManager destination) 
    {
        Debug.Log("Sending Civilian Mission");
        yield return new WaitForFixedUpdate();
        go.GetComponent<Civilian>().TravelTo(destination);
    }

    void UpdateCarrierMissions() 
    {
        Debug.Log("Update Carrier");
        foreach (var item in ContainerHandlers) 
        {
            Debug.Log("Checking Container Assignement");
            Debug.LogWarning(item.Value.Count);
            if (CarrierCurrent > 0)
            {
                float pool;
                pool = item.Key.GetPool();
                if (pool - (300 * item.Value.Count) > 0) 
                {
                    CarrierCurrent--;
                    ContainerHandlers[item.Key].Add(null);
                    Debug.Log("Sending Carrier to Queue");
                    LaunchQueue.Enqueue((hangar) =>
                    {
                        LaunchNewCarrier(hangar, item.Key);
                    });
                }
            }
        }
    }

    GameObject LaunchNewCarrier(GameObject hangar, Container target) 
    {
        Debug.Log("Launching New Carrier");

        GameObject go = Instantiate(
            CarrierPrefabs[Random.Range(0,CarrierPrefabs.Length)],
            hangar.transform.position,
            hangar.transform.rotation);

        go.GetComponent<UnitBase>().home = this;
        ContainerHandlers[target].Remove(null);
        
        ContainerHandlers[target].Add(go.GetComponent<Carrier>());

        //go.GetComponent<Rigidbody>().AddForce(hangar.transform.localPosition.normalized * 300f);

        StartCoroutine(LaunchCarrierMission(go, target));

        return go;
    }

    IEnumerator LaunchCarrierMission(GameObject go, Container target) 
    {
        Debug.Log("Sending Carrier Mission");
        yield return new WaitForFixedUpdate();
        go.GetComponent<Carrier>().DeliverResource(
            target.GetComponent<Resource>(),
            GetComponent<Resource>());
    }

    void UpdateMinerMissions()
    {
        Debug.LogError("Update Miner");
        foreach (var item in MiningHandlers)
        {
            Debug.LogError("Checking Mineral Assignement");
            if (item.Value.Count >= 0 && item.Value.Count < MinerMax)
            {
                
                Debug.LogError("Sending Miner to Queue");
                MiningHandlers[item.Key].Add(null);
                LaunchQueue.Enqueue((hangar) =>
                {
                    LaunchNewMiner(hangar, item.Key);
                });
                
            }
        }
    }

    GameObject LaunchNewMiner(GameObject hangar, MineralMasterNode target)
    {
        Debug.Log("Launching New Miner");

        GameObject go = Instantiate(
            MinerPrefabs[Random.Range(0, MinerPrefabs.Length)],
            hangar.transform.position,
            hangar.transform.rotation);

        go.GetComponent<UnitBase>().home = this;

        MiningHandlers[target].Remove(null);
        MiningHandlers[target].Add(go.GetComponent<Miner>());

        //go.GetComponent<Rigidbody>().AddForce(hangar.transform.localPosition.normalized * 300f);

        StartCoroutine(LaunchMinerMission(go, target));

        return go;
    }

    IEnumerator LaunchMinerMission(GameObject go, MineralMasterNode target)
    {
        Debug.Log("Sending Miner Mission");
        yield return new WaitForFixedUpdate();
        go.GetComponent<Miner>().MineFromNode(target);
    }

    public GameObject GetHangar() 
    {
        return HangarBay[Random.Range(0, HangarBay.Length)];
    }

    public void Dock(UnitBase unit) 
    {
        if (unit.type == ManageObject.ObjectType.CARRIER)
        {
            CarrierCurrent++;
            foreach (var item in ContainerHandlers)
            {
                if (item.Value.Contains(unit.GetComponent<Carrier>()))
                {
                    item.Value.Remove(unit.GetComponent<Carrier>());
                }
            }
        }
        else if (unit.type == ManageObject.ObjectType.CIVILIAN) 
        {
            CurrentCivilians++;
        }
        else if (unit.type == ManageObject.ObjectType.MINER)
        {
            foreach (var item in MiningHandlers)
            {
                if (item.Value.Contains(unit.GetComponent<Miner>()))
                {
                    item.Value.Remove(unit.GetComponent<Miner>());
                }
            }
        }

    }


    private void OnDrawGizmosSelected()
    {
        
    }
}
