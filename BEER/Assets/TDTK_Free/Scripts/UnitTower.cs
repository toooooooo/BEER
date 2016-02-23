using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using TDTK;

namespace TDTK
{

    public enum _TowerType { Turret, AOE, Support }
    public enum _TargetMode { Hybrid, Air, Ground }


    public class UnitTower : Unit
    {
        // last energy reciever tower that is being build thus expectung to find coresponding energy source
        public static UnitTower lastBuiltEnergyRecieverTower;

        private UnitTower energyProducer;
        public UnitTower EnergyProducer
        {
            get { return this.energyProducer; }
            set { this.energyProducer = value; }
        }


        private PlatformTD platform;
        public PlatformTD Platform
        {
            get { return this.platform; }
            set { this.platform = value; }
        }

        public delegate void TowerSoldHandler(UnitTower tower);
        public static event TowerSoldHandler onSoldE;                                   //listen by TDTK only

        public delegate void ConstructionStartHandler(UnitTower tower);
        public static event ConstructionStartHandler onConstructionStartE;      //listen by TDTK only

        public delegate void TowerUpgradedHandler(UnitTower tower);
        public static event TowerUpgradedHandler onUpgradedE;

        public delegate void ConstructionCompleteHandler(UnitTower tower);
        public static event ConstructionCompleteHandler onConstructionCompleteE;


        public delegate void PlayConstructAnimation();
        public PlayConstructAnimation playConstructAnimation;
        public delegate void PlayDeconstructAnimation();
        public PlayDeconstructAnimation playDeconstructAnimation;


        public _TowerType type = _TowerType.Turret;
        public _TargetMode targetMode = _TargetMode.Hybrid;


        public bool disableInBuildManager = false;  //when set to true, tower wont appear in BuildManager buildList

        private enum _Construction { None, Constructing, Deconstructing }
        private _Construction construction = _Construction.None;
        public bool _IsInConstruction() { return construction == _Construction.None ? false : true; }

        public override void Awake()
        {
            SetSubClass(this);

            base.Awake();

            if (stats.Count == 0) stats.Add(new UnitStat());
        }

        public override void Start()
        {
            base.Start();
        }

        public void InitTower(int ID, PlatformTD currentPlatform)
        {
            Init();

            instanceID = ID;
            platform = currentPlatform;

            value = stats[currentActiveStat].cost;

            int rscCount = ResourceManager.GetResourceCount();
            for (int i = 0; i < stats.Count; i++)
            {
                UnitStat stat = stats[i];
                stat.slow.effectID = instanceID;
                stat.dot.effectID = instanceID;
                stat.buff.effectID = instanceID;
                if (stat.rscGain.Count != rscCount)
                {
                    while (stat.rscGain.Count < rscCount) stat.rscGain.Add(0);
                    while (stat.rscGain.Count > rscCount) stat.rscGain.RemoveAt(stat.rscGain.Count - 1);
                }
            }

            if (type == _TowerType.Turret)
            {
                StartCoroutine(ScanForTargetRoutine());
                StartCoroutine(TurretRoutine());
            }
            if (type == _TowerType.AOE)
            {
                StartCoroutine(AOETowerRoutine());
            }
            if (type == _TowerType.Support)
            {
                StartCoroutine(SupportRoutine());
            }
        }


        [HideInInspector]
        public float builtDuration;
        [HideInInspector]
        public float buildDuration;
        public void UnBuild() { StartCoroutine(Building(stats[currentActiveStat].unBuildDuration, true)); }
        public void Build() { StartCoroutine(Building(stats[currentActiveStat].buildDuration)); }
        IEnumerator Building(float duration, bool reverse = false)
        {       //reverse flag is set to true when selling (thus unbuilding) the tower
            bool possible = BuildManager.instance.UpdatePathMaps(this, reverse);
            if (!possible)
            {
                reverse = true;
            }


            construction = !reverse ? _Construction.Constructing : _Construction.Deconstructing;

            builtDuration = 0;
            buildDuration = duration;

            if (onConstructionStartE != null) onConstructionStartE(this);

            yield return null;
            if (!reverse && playConstructAnimation != null) playConstructAnimation();
            else if (reverse && playDeconstructAnimation != null) playConstructAnimation();

            while (true)
            {
                yield return null;
                builtDuration += Time.deltaTime;
                if (builtDuration > buildDuration) break;
            }

            construction = _Construction.None;

            if (!reverse && onConstructionCompleteE != null) onConstructionCompleteE(this);

            buildFinished();

            /****
            if (electricityReciever)
            {
                lastBuiltEnergyRecieverTower = this;
            }
            ******/



            if (reverse)
            {
                if (onSoldE != null) onSoldE(this);
                
                if(possible)
                    ResourceManager.GainResource(GetValue());
                
                Dead();
            }
        }

        private GameObject drone;
        IEnumerator droneCorutineHndl;

        public void startDroneFlight(UnitTower electricitySource)
        {
            if (droneCorutineHndl != null)
            {
                StopCoroutine(droneCorutineHndl);
                if (moveObjectHndl != null) StopCoroutine(moveObjectHndl);
                Destroy(drone);
            }
            
            drone = Instantiate(Resources.Load("UAV_Trident")) as GameObject;
            drone.transform.position = new Vector3(transform.position.x, transform.position.y + GetComponent<Collider>().bounds.size.y, transform.position.z);
            droneCorutineHndl = StartDroneFlight(electricitySource, drone, new Vector3(transform.position.x, transform.position.y + GetComponent<Collider>().transform.position.y, transform.position.z),
                new Vector3(electricitySource.transform.position.x, electricitySource.transform.position.y + electricitySource.GetComponent<Collider>().transform.position.y, electricitySource.transform.position.z));
            StartCoroutine(droneCorutineHndl);
        }


        private IEnumerator moveObjectHndl;
        IEnumerator StartDroneFlight(UnitTower tower, GameObject drone, Vector3 point_A, Vector3 point_B)
        {
            // rotate drone to electric facility
            // 
            float electricity_taken = 0f;
            while (true)
            {
                // look at electricity source (windmill...)
                drone.transform.LookAt(tower.transform);
                // fly to it
                moveObjectHndl = MoveObject(drone.transform, point_A, point_B, 3.0f);
                yield return StartCoroutine(moveObjectHndl);

                // take energy from electricty source
                if (tower.currentElectricity - electricityRegenerationRate < 0)
                {
                    electricity_taken = 0;
                }
                else
                {
                    electricity_taken = electricityRegenerationRate;
                    tower.currentElectricity -= electricityRegenerationRate;
                }

                // turn back
                drone.transform.LookAt(transform);

                moveObjectHndl = MoveObject(drone.transform, point_B, point_A, 3.0f);
                // fly back
                yield return StartCoroutine(moveObjectHndl);

                // drone "delivered" energy
                currentElectricity += electricity_taken;
                // if (electricity_taken != 0)
                // {
                    // new TextOverlay(thisT.position, "+" + electricity_taken.ToString(), new Color(0f, 1f, 0f, 1f));
                // }
                // else
                // {
                    // new TextOverlay(thisT.position, "+" + electricity_taken.ToString(), new Color(1f, 0f, 0f, 1f));
                // }

            }
        }

        IEnumerator MoveObject(Transform thisTransform, Vector3 startPos, Vector3 endPos, float time)
        {
            var i = 0.0f;
            var rate = 1.0f / time;
            while (i < 1.0f)
            {
                i += Time.deltaTime * rate;
                thisTransform.position = Vector3.Lerp(startPos, endPos, i);
                yield return null;
            }
        }

        void OnMouseDown()
        {
            if(lastBuiltEnergyRecieverTower == null && electricityReciever)
            {
                Debug.Log("setting dron start point");
                lastBuiltEnergyRecieverTower = this;
            }

            Debug.Log((lastBuiltEnergyRecieverTower != null) + " " + electricityFacility);

            if(lastBuiltEnergyRecieverTower != null && electricityFacility)
            {
                Debug.Log("sending drone to the new destination");
                lastBuiltEnergyRecieverTower.energyProducer = this;
                lastBuiltEnergyRecieverTower.startDroneFlight(this);
                lastBuiltEnergyRecieverTower = null;
            }
        }

        public float GetBuildProgress()
        {
            if (construction == _Construction.Constructing) return builtDuration / buildDuration;
            if (construction == _Construction.Deconstructing) return (buildDuration - builtDuration) / buildDuration;
            else return 0;
        }

        private void buildFinished()
        {
            if (electricityFacility)
            {
                UIOverlay.NewElectricity(this);
                StartCoroutine(GenerateEnergyRoutine(this));
            }
            else if (electricityReciever)
            {
                UIOverlay.NewElectricityReciever(this);
                // StartCoroutine(GenerateEnergyRoutine(this));
            }
            // new TextOverlay(thisT.position, "100", new Color(0f, 1f, .4f, 1f));
            // if (onDamagedE != null) onDamagedE(this);
        }

        IEnumerator GenerateEnergyRoutine(Unit unit)
        {
            if (!unit.electricityFacility)
                yield return null;

            while(unit != null)
            {
                unit.currentElectricity += unit.electricityRegenerationRate;

                yield return null;
            }
        }

        IEnumerator ReceiveEnergyRoutine(Unit unit)
        {
            if (!unit.electricityReciever)
                yield return null;

            while (unit != null)
            {
                unit.currentElectricity += unit.electricityRegenerationRate-unit.currentSpendingRate;

                yield return null;
            }
        }


        public void Sell()
        {
            UnBuild();
        }


        private bool isSampleTower;
        private UnitTower srcTower;
        public void SetAsSampleTower(UnitTower tower)
        {
            isSampleTower = true;
            srcTower = tower;
            thisT.position = new Vector3(0, 9999, 0);
        }
        public bool IsSampleTower() { return isSampleTower; }
        public IEnumerator DragNDropRoutine()
        {
            GameControl.SelectTower(this);
            yield return null;

            while (true)
            {
                Vector3 pos = Input.mousePosition;

                _TileStatus status = BuildManager.CheckBuildPoint(pos, -1, prefabID);

                if (status == _TileStatus.Available)
                {
                    BuildInfo buildInfo = BuildManager.GetBuildInfo();
                    thisT.position = buildInfo.position;
                    thisT.rotation = buildInfo.platform.thisT.rotation;
                }
                else
                {
                    Ray ray = Camera.main.ScreenPointToRay(pos);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity)) thisT.position = hit.point;
                    //this there is no collier, randomly place it 30unit from camera
                    else thisT.position = ray.GetPoint(30);
                }


                //left-click, build
                if (Input.GetMouseButtonDown(0) && !UIUtilities.IsCursorOnUI())
                {
                    //if current mouse point position is valid, build the tower
                    if (status == _TileStatus.Available)
                    {
                        string exception = BuildManager.BuildTower(srcTower);
                        if (exception != "") GameControl.DisplayMessage(exception);
                    }
                    else
                    {
                        BuildManager.ClearBuildPoint();
                    }
                    GameControl.ClearSelectedTower();
                    thisObj.SetActive(false);
                    break;
                }

                //right-click, cancel
                if (Input.GetMouseButtonDown(1) || GameControl.GetGameState() == _GameState.Over)
                {
                    GameControl.ClearSelectedTower();
                    BuildManager.ClearBuildPoint();
                    thisObj.SetActive(false);
                    break;
                }

                yield return null;
            }

            thisT.position = new Vector3(0, 9999, 0);
        }



        public override void Update()
        {
            base.Update();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }




        IEnumerator AOETowerRoutine()
        {
            if (targetMode == _TargetMode.Hybrid)
            {
                LayerMask mask1 = 1 << LayerManager.LayerCreep();
                LayerMask mask2 = 1 << LayerManager.LayerCreepF();
                maskTarget = mask1 | mask2;
            }
            else if (targetMode == _TargetMode.Air)
            {
                maskTarget = 1 << LayerManager.LayerCreepF();
            }
            else if (targetMode == _TargetMode.Ground)
            {
                maskTarget = 1 << LayerManager.LayerCreep();
            }

            while (true)
            {
                yield return new WaitForSeconds(GetCooldown());

                while (stunned || IsInConstruction()) yield return null;

                Transform soPrefab = GetShootObjectT();
                if (soPrefab != null) Instantiate(soPrefab, thisT.position, thisT.rotation);

                Collider[] cols = Physics.OverlapSphere(thisT.position, GetRange(), maskTarget);
                if (cols.Length > 0)
                {
                    for (int i = 0; i < cols.Length; i++)
                    {
                        Unit unit = cols[i].transform.GetComponent<Unit>();
                        if (unit == null && !unit.dead) continue;

                        AttackInstance attInstance = new AttackInstance();
                        attInstance.srcUnit = this;
                        attInstance.tgtUnit = unit;
                        attInstance.Process();

                        unit.ApplyEffect(attInstance);
                    }
                }
            }
        }






        private int level = 1;
        public int GetLevel() { return level; }
        public void SetLevel(int lvl) { level = lvl; }

        [HideInInspector]
        public UnitTower prevLevelTower;
        [HideInInspector]
        public UnitTower nextLevelTower;
        public int ReadyToBeUpgrade()
        {
            if (currentActiveStat < stats.Count - 1) return 1;
            //if(nextLevelTower!=null) return 1;
            return 0;
        }
        public string Upgrade(int ID = 0)
        {   //ID specify which nextTower to use
            if (currentActiveStat < stats.Count - 1) return UpgradeToNextStat();
            //else if(nextLevelTower!=null) return UpgradeToNextTower();
            return "Tower is at maximum level!";
        }
        public string UpgradeToNextStat()
        {
            List<int> cost = GetCost();
            int suffCost = ResourceManager.HasSufficientResource(cost);
            if (suffCost == -1)
            {
                level += 1;
                currentActiveStat += 1;
                ResourceManager.SpendResource(cost);
                AddValue(stats[currentActiveStat].cost);
                Build();

                if (onUpgradedE != null) onUpgradedE(this);
                return "";
            }
            return "Insufficient Resource";
        }



        //only use cost from sample towers or in game tower instance, not the prefab
        //ID is for upgrade path
        public List<int> GetCost(int ID = 0)
        {
            List<int> cost = new List<int>();
            if (isSampleTower)
            {
                cost = new List<int>(stats[currentActiveStat].cost);
            }
            else
            {
                if (currentActiveStat < stats.Count - 1) cost = new List<int>(stats[currentActiveStat + 1].cost);
                //if(nextLevelTower!=null) return cost=new List<int>( nextLevelTower.stats[0].cost );
            }
            return cost;
        }



        public List<int> value = new List<int>();
        //apply the refund ratio from gamecontrol
        public List<int> GetValue()
        {
            List<int> newValue = new List<int>();
            for (int i = 0; i < value.Count; i++) newValue.Add((int)(value[i] * GameControl.GetSellTowerRefundRatio()));
            return newValue;
        }
        //called when tower is upgraded to bring the value forward
        public void AddValue(List<int> list)
        {
            for (int i = 0; i < value.Count; i++)
            {
                value[i] += list[i];
            }
        }




        public bool DealDamage()
        {
            if (type == _TowerType.Turret || type == _TowerType.AOE) return true;
            return false;
        }





        //not compatible with PointNBuild mode
        void OnMouseEnter()
        {
            if (UIUtilities.IsCursorOnUI()) return;
            BuildManager.ShowIndicator(this);
        }
        void OnMouseExit() { BuildManager.HideIndicator(); }
    }

}