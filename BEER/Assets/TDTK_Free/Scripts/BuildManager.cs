using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using TDTK;
using BEERPath;

namespace TDTK
{



    public class BuildManager : MonoBehaviour
    {

        public delegate void AddNewTowerHandler(UnitTower tower);
        public static event AddNewTowerHandler onAddNewTowerE;      //add new tower in runtime

        //prefabID of tower unavailable int this level
        public List<int> unavailableTowerIDList = new List<int>();

        //only used in runtime, filled up using info from unavailableTowerIDList
        public List<UnitTower> towerList = new List<UnitTower>();

        private static float _gridSize = 0;
        public float gridSize = 1.5f;
        public List<PlatformTD> buildPlatforms = new List<PlatformTD>();

        public bool AutoAdjustTextureToGrid = true;

        public enum _CursorIndicatorMode { All, ValidOnly, None }
        public _CursorIndicatorMode cursorIndicatorMode = _CursorIndicatorMode.All;


        public bool autoSearchForPlatform = false;






        public static BuildManager instance;
        private static BuildInfo buildInfo;
        private int towerCount = 0;
        public static int GetTowerCount() { return instance.towerCount; }


        public void Init()
        {
            instance = this;

            gridSize = Mathf.Max(0.25f, gridSize);
            _gridSize = gridSize;

            buildInfo = null;

            InitTower();
            InitPlatform();

            InitPathFinder();
        }

        public GameObject indicatorBuildPoint;
        public GameObject indicatorCursor;

        private Renderer indicatorBuildPointRen;
        private Renderer indicatorCursorRen;


        void Start()
        {
            if (cursorIndicatorMode != _CursorIndicatorMode.None)
            {
                if (indicatorBuildPoint != null)
                {
                    indicatorBuildPoint = (GameObject)Instantiate(indicatorBuildPoint);
                    indicatorBuildPoint.transform.localScale = new Vector3(gridSize, 1, gridSize);
                    indicatorBuildPoint.transform.parent = transform;
                    indicatorBuildPoint.SetActive(false);
                    indicatorCursor.name = "TileIndicator_BuildPoint";

                    foreach (Transform child in indicatorBuildPoint.transform)
                        indicatorBuildPointRen = child.GetComponent<Renderer>();
                }

                if (indicatorCursor != null)
                {
                    indicatorCursor = (GameObject)Instantiate(indicatorCursor);
                    indicatorCursor.transform.localScale = new Vector3(gridSize, 1, gridSize);
                    indicatorCursor.transform.parent = transform;
                    indicatorCursor.SetActive(false);
                    indicatorCursor.name = "TileIndicator_Cursor";

                    foreach (Transform child in indicatorCursor.transform)
                        indicatorCursorRen = child.GetComponent<Renderer>();
                }
            }

            InitiateSampleTowerList();
        }

        public void InitTower()
        {
            List<UnitTower> towerListDB = TowerDB.Load();

            towerList = new List<UnitTower>();
            for (int i = 0; i < towerListDB.Count; i++)
            {
                if (towerListDB[i].disableInBuildManager) continue;
                if (!unavailableTowerIDList.Contains(towerListDB[i].prefabID))
                {
                    towerList.Add(towerListDB[i]);
                }
            }
        }


        // Use this for initialization
        void InitPlatform()
        {
            if (autoSearchForPlatform)
            {
                buildPlatforms = new List<PlatformTD>();
                PlatformTD[] platList = FindObjectsOfType(typeof(PlatformTD)) as PlatformTD[];
                for (int i = 0; i < platList.Length; i++)
                {
                    buildPlatforms.Add(platList[i]);
                }
            }

            for (int i = 0; i < buildPlatforms.Count; i++)
            {
                FormatPlatform(buildPlatforms[i].transform);
                buildPlatforms[i].VerifyTowers(towerList);
            }
        }


        void FormatPlatform(Transform platformT)
        {
            //clear the platform of any unneeded collider
            ClearPlatformColliderRecursively(platformT);

            //make sure the plane is perfectly horizontal, rotation around the y-axis is presreved
            platformT.eulerAngles = new Vector3(0, platformT.rotation.eulerAngles.y, 0);

            //adjusting the scale
            float scaleX = Mathf.Max(1, Mathf.Round(Utility.GetWorldScale(platformT).x / gridSize)) * gridSize;
            float scaleZ = Mathf.Max(1, Mathf.Round(Utility.GetWorldScale(platformT).z / gridSize)) * gridSize;

            platformT.localScale = new Vector3(scaleX, 1, scaleZ);

            //adjusting the texture
            if (AutoAdjustTextureToGrid)
            {
                Material mat = platformT.GetComponent<Renderer>().material;

                float x = (Utility.GetWorldScale(platformT).x) / gridSize;
                float z = (Utility.GetWorldScale(platformT).z) / gridSize;

                mat.mainTextureOffset = new Vector2(0, 0);
                mat.mainTextureScale = new Vector2(x, z);
            }
        }

        void ClearPlatformColliderRecursively(Transform t)
        {
            foreach (Transform child in t)
            {
                ClearPlatformColliderRecursively(child);
                Collider col = child.gameObject.GetComponent<Collider>();
                if (col != null && !col.enabled)
                {
                    Destroy(col);
                }
            }
        }


        public static void AddNewTower(UnitTower newTower)
        {
            if (instance.towerList.Contains(newTower)) return;
            instance.towerList.Add(newTower);
            instance.AddNewSampleTower(newTower);
            if (onAddNewTowerE != null) onAddNewTowerE(newTower);
        }






        // Update is called once per frame
        void Update()
        {

        }


        static public void ClearBuildPoint()
        {
            //Debug.Log("ClearBuildPoint");
            buildInfo = null;
            ClearIndicator();
        }

        static public void ClearIndicator()
        {
            if (instance.indicatorBuildPoint != null) instance.indicatorBuildPoint.SetActive(false);
        }


        static public Vector3 GetTilePos(Transform platformT, Vector3 hitPos)
        {
            //check if the row count is odd or even number
            float remainderX = Utility.GetWorldScale(platformT).x / _gridSize % 2;
            float remainderZ = Utility.GetWorldScale(platformT).z / _gridSize % 2;

            //get the rotation offset of the plane
            Quaternion rot = Quaternion.LookRotation(hitPos - platformT.position);

            //get the x and z distance from the centre of the plane in the baseplane orientation
            //from this point on all x and z will be in reference to the basePlane orientation
            float dist = Vector3.Distance(hitPos, platformT.position);
            float distX = Mathf.Sin((rot.eulerAngles.y - platformT.rotation.eulerAngles.y) * Mathf.Deg2Rad) * dist;
            float distZ = Mathf.Cos((rot.eulerAngles.y - platformT.rotation.eulerAngles.y) * Mathf.Deg2Rad) * dist;

            //get the sign (1/-1) of the x and y direction
            float signX = distX != 0 ? distX / Mathf.Abs(distX) : 1;
            float signZ = distZ != 0 ? distZ / Mathf.Abs(distZ) : 1;

            //calculate the tile number selected in z and z direction
            float numX = Mathf.Round((distX + (remainderX - 1) * (signX * _gridSize / 2)) / _gridSize);
            float numZ = Mathf.Round((distZ + (remainderZ - 1) * (signZ * _gridSize / 2)) / _gridSize);

            //calculate offset in x-axis, 
            float offsetX = -(remainderX - 1) * signX * _gridSize / 2;
            float offsetZ = -(remainderZ - 1) * signZ * _gridSize / 2;

            //get the pos and apply the offset
            Vector3 p = platformT.TransformDirection(new Vector3(numX, 0, numZ) * _gridSize);
            p += platformT.TransformDirection(new Vector3(offsetX, 0, offsetZ));

            //set the position;
            Vector3 pos = p + platformT.position;

            return pos;
        }

        //called to set indicator to a particular node, set the color as well
        //not iOS performance friendly
        public static void SetIndicator(Vector3 pointer) { instance._SetIndicator(pointer); }
        public void _SetIndicator(Vector3 pointer)
        {

            //~ if(!buildManager.enableTileIndicator) return;
            if (cursorIndicatorMode == _CursorIndicatorMode.None) return;

            if (buildInfo != null)
            {
                indicatorCursor.SetActive(false);
                return;
            }

            //layerMask for platform only
            LayerMask maskPlatform = 1 << LayerManager.LayerPlatform();
            //layerMask for detect all collider within buildPoint
            LayerMask maskAll = 1 << LayerManager.LayerPlatform();
            int terrainLayer = LayerManager.LayerTerrain();
            if (terrainLayer >= 0) maskAll |= 1 << terrainLayer;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Ray ray = mainCam.ScreenPointToRay(pointer);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, maskPlatform))
                {

                    for (int i = 0; i < buildPlatforms.Count; i++)
                    {
                        if (hit.transform == buildPlatforms[i].thisT)
                        {
                            //calculating the build center point base on the input position
                            Vector3 pos = GetTilePos(buildPlatforms[i].thisT, hit.point);

                            //Debug.Log(new Vector3(remainderX, 0, remainderZ)+"  "+new Vector3(signX, 0, signZ)+"  "+p+"  "+basePlane.position);
                            indicatorCursor.transform.position = pos;
                            indicatorCursor.transform.rotation = buildPlatforms[i].thisT.rotation;

                            Collider[] cols = Physics.OverlapSphere(pos, _gridSize / 2 * 0.9f, ~maskAll);
                            if (cols.Length > 0)
                            {
                                if (cursorIndicatorMode == _CursorIndicatorMode.All)
                                {
                                    indicatorCursor.SetActive(true);
                                    indicatorCursorRen.material.SetColor("_TintColor", Color.red);
                                }
                                else indicatorCursor.SetActive(false);
                            }
                            else
                            {
                                indicatorCursor.SetActive(true);
                                indicatorCursorRen.material.SetColor("_TintColor", Color.green);
                            }
                        }
                    }
                }
                else
                {
                    indicatorCursor.SetActive(false);
                }
            }
            else
            {
                indicatorCursor.SetActive(false);
            }
        }
        //not in use outside this script
        public static void HideCursorIndicator() { instance.indicatorCursor.SetActive(false); }

        public static void ShowIndicator(UnitTower tower)
        {
            instance.indicatorCursor.SetActive(true);
            instance.indicatorCursor.transform.position = tower.thisT.position;
            instance.indicatorCursor.transform.rotation = tower.thisT.rotation;
        }
        public static void HideIndicator()
        {
            instance.indicatorCursor.SetActive(false);
        }


        public static _TileStatus CheckBuildPoint(Vector3 pointer, int footprint = -1, int ID = -1)
        {
            return instance._CheckBuildPoint(pointer, footprint, ID);
        }
        public _TileStatus _CheckBuildPoint(Vector3 pointer, int footprint = -1, int ID = -1)
        {
            _TileStatus status = _TileStatus.Available;
            BuildInfo newBuildInfo = new BuildInfo();

            //disable indicator first (for dragNdrop mode), it will be re-enable if the build-point is valid
            indicatorBuildPoint.SetActive(false);

            //layerMask for platform only
            LayerMask maskPlatform = 1 << LayerManager.LayerPlatform();
            //layerMask for detect all collider within buildPoint
            LayerMask maskAll = 1 << LayerManager.LayerPlatform();
            int terrainLayer = LayerManager.LayerTerrain();
            if (terrainLayer >= 0) maskAll |= 1 << terrainLayer;

            //int creepLayer=LayerManager.layerCreep();
            //if(creepLayer>=0) maskAll|=1<<creepLayer;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Ray ray = mainCam.ScreenPointToRay(pointer);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, maskPlatform))
                {

                    for (int i = 0; i < buildPlatforms.Count; i++)
                    {
                        if (hit.transform == buildPlatforms[i].thisT)
                        {
                            PlatformTD platform = buildPlatforms[i];

                            //checking if tower can be built on the platform, for dragNdrop mode
                            if (ID >= 0 && !platform.availableTowerIDList.Contains(ID)) return _TileStatus.Unavailable;

                            //calculating the build center point base on the input position
                            Vector3 pos = GetTilePos(platform.thisT, hit.point);

                            //check if the position is blocked, by any other obstabcle other than the baseplane itself
                            Collider[] cols = Physics.OverlapSphere(pos, _gridSize / 2 * 0.9f + footprint * _gridSize, ~maskAll);
                            if (cols.Length > 0)
                            {
                                //Debug.Log("something's in the way "+cols[0]);
                                return _TileStatus.Unavailable;
                            }
                            else
                            {
                                //confirm that we can build here
                                newBuildInfo.position = pos;
                                newBuildInfo.platform = platform;
                            }

                            //newBuildInfo.availableTowerIDList=platform.availableTowerIDList;
                            //map platform availableTowerIDList (which is the towers' prefabID) to the list elements' ID in towerList
                            newBuildInfo.availableTowerIDList = new List<int>();
                            for (int m = 0; m < platform.availableTowerIDList.Count; m++)
                            {
                                for (int n = 0; n < towerList.Count; n++)
                                {
                                    if (platform.availableTowerIDList[m] == towerList[n].prefabID)
                                    {
                                        newBuildInfo.availableTowerIDList.Add(n);
                                        break;
                                    }
                                }
                            }

                            //List<int> tempList=new List<int>();
                            //for(int n=0; n<towerList.Count; n++) tempList.Add(towerList[n].prefabID);
                            //newBuildInfo.availableTowerIDList=tempList;

                            buildInfo = newBuildInfo;

                            break;
                        }

                    }

                }
                else return _TileStatus.NoPlatform;
            }
            else return _TileStatus.NoPlatform;



            if (buildInfo != null && cursorIndicatorMode != _CursorIndicatorMode.None)
            {
                if (status == _TileStatus.Available) indicatorBuildPointRen.material.SetColor("_TintColor", new Color(0, 1, 0, 1));
                else indicatorBuildPointRen.material.SetColor("_TintColor", new Color(1, 0, 0, 1));

                indicatorBuildPoint.SetActive(true);
                indicatorBuildPoint.transform.position = buildInfo.position;
                if (buildInfo.platform != null)
                {
                    indicatorBuildPoint.transform.rotation = buildInfo.platform.thisT.rotation;
                }

                HideCursorIndicator();
            }

            return status;
        }




        //called when a tower building is initated in DragNDrop, use the sample tower as the model and set it in DragNDrop mode
        public static string BuildTowerDragNDrop(UnitTower tower) { return instance._BuildTowerDragNDrop(tower); }
        public string _BuildTowerDragNDrop(UnitTower tower)
        {

            UnitTower sampleTower = GetSampleTower(tower);
            List<int> cost = sampleTower.GetCost();

            int suffCost = ResourceManager.HasSufficientResource(cost);
            if (suffCost == -1)
            {
                sampleTower.thisObj.SetActive(true);
                GameControl.SelectTower(sampleTower);
                UnitTower towerInstance = sampleTower;
                towerInstance.StartCoroutine(towerInstance.DragNDropRoutine());

                return "";
            }

            return "Insufficient Resource   " + suffCost;
        }


        //called by any external component to build tower, uses buildInfo
        public static string BuildTower(UnitTower tower)
        {
            if (buildInfo == null) return "Select a Build Point First";

            UnitTower sampleTower = GetSampleTower(tower);

            /***/
            // check if there's energy reciving tower
            if (!tower.electricityReciever && !tower.electricityFacility)
            {
                LayerMask maskTarget = 1 << LayerManager.LayerTower();

                // List<UnitTower> tgtList = new List<UnitTower>();
                Collider[] cols = Physics.OverlapSphere(buildInfo.position, 1000 /*GetRange()*/, maskTarget);
                UnitTower unit = null;
                if (cols.Length > 0)
                {
                    // find closest electric facility

                    float min_d = 5000;
                    for (int i = 0; i < cols.Length; i++)
                    {
                        // if it's not electric reciever skip
                        if (!cols[i].gameObject.GetComponent<UnitTower>().electricityReciever)
                            continue;

                        if (Vector3.Distance(cols[i].gameObject.GetComponent<UnitTower>().transform.position, buildInfo.position) < min_d)
                        {
                            min_d = Vector3.Distance(cols[i].gameObject.GetComponent<UnitTower>().transform.position, buildInfo.position);
                            unit = cols[i].gameObject.GetComponent<UnitTower>();
                        }
                    }

                    if (unit == null || min_d > unit.GetRange())
                    {
                        // set electricity source for tower weapon
                        return "There is not enough electricity";
                    }
                    else
                        tower.electricitySource = unit;
                }
            }
            /***/


            //check if there are sufficient resource
            List<int> cost = sampleTower.GetCost();
            int suffCost = ResourceManager.HasSufficientResource(cost);
            if (suffCost == -1)
            {
                ResourceManager.SpendResource(cost);

                GameObject towerObj = (GameObject)Instantiate(tower.gameObject, buildInfo.position, buildInfo.platform.thisT.rotation);
                UnitTower towerInstance = towerObj.GetComponent<UnitTower>();
                towerInstance.InitTower(instance.towerCount += 1, buildInfo.platform);
                towerInstance.Build();

                //clear the build info and indicator for build manager
                ClearBuildPoint();


                return "";
            }

            return "Insufficient Resource";
        }


        public static void PreBuildTower(UnitTower tower)
        {
            PlatformTD platform = null;
            LayerMask mask = 1 << LayerManager.LayerPlatform();
            Collider[] cols = Physics.OverlapSphere(tower.thisT.position, _gridSize, mask);
            if (cols.Length > 0) platform = cols[0].gameObject.GetComponent<PlatformTD>();

            if (platform != null)
            {
                Vector3 buildPos = GetTilePos(platform.thisT, tower.thisT.position);
                tower.thisT.position = buildPos;
                tower.thisT.rotation = platform.thisT.rotation;
            }
            else Debug.Log("no platform found for pre-placed tower");

            tower.InitTower(instance.towerCount += 1, platform);
        }






        private List<UnitTower> sampleTowerList = new List<UnitTower>();
        private int currentSampleID = -1;
        public void InitiateSampleTowerList()
        {
            sampleTowerList = new List<UnitTower>();
            for (int i = 0; i < towerList.Count; i++)
            {
                UnitTower towerInstance = CreateSampleTower(towerList[i]);
                sampleTowerList.Add(towerInstance);
            }
        }
        public void AddNewSampleTower(UnitTower newTower)
        {
            UnitTower towerInstance = CreateSampleTower(newTower);
            sampleTowerList.Add(towerInstance);
        }
        public UnitTower CreateSampleTower(UnitTower towerPrefab)
        {
            GameObject towerObj = (GameObject)Instantiate(towerPrefab.gameObject);

            towerObj.transform.parent = transform;
            if (towerObj.GetComponent<Collider>() != null) Destroy(towerObj.GetComponent<Collider>());
            Utility.DestroyColliderRecursively(towerObj.transform);
            towerObj.SetActive(false);

            UnitTower towerInstance = towerObj.GetComponent<UnitTower>();
            towerInstance.SetAsSampleTower(towerPrefab);

            return towerInstance;
        }

        public static UnitTower GetSampleTower(int ID) { return instance.sampleTowerList[ID]; }
        public static UnitTower GetSampleTower(UnitTower tower)
        {
            for (int i = 0; i < instance.sampleTowerList.Count; i++)
            {
                if (instance.sampleTowerList[i].prefabID == tower.prefabID) return instance.sampleTowerList[i];
            }
            return null;
        }

        public static void ShowSampleTower(int ID) { instance._ShowsampleTowerList(ID); }
        public void _ShowsampleTowerList(int ID)
        {
            if (currentSampleID == ID || buildInfo == null) return;

            if (currentSampleID >= 0) ClearSampleTower();

            currentSampleID = ID;
            sampleTowerList[ID].thisT.position = buildInfo.position;
            sampleTowerList[ID].thisT.rotation = buildInfo.platform.transform.rotation;

            GameControl.SelectTower(sampleTowerList[ID]);

            sampleTowerList[ID].thisObj.SetActive(true);

        }

        public static void ClearSampleTower() { instance._ClearSampleTower(); }
        public void _ClearSampleTower()
        {
            if (currentSampleID < 0) return;
            sampleTowerList[currentSampleID].thisObj.SetActive(false);
            GameControl.ClearSelectedTower();
            currentSampleID = -1;
        }





        public static BuildInfo GetBuildInfo()
        {
            return buildInfo;
        }

        public static int GetTowerListCount() { return instance.towerList.Count; }
        public static List<UnitTower> GetTowerList() { return instance.towerList; }
        public static UnitTower GetTower(int ID)
        {
            foreach (UnitTower tower in instance.towerList)
            {
                if (tower.prefabID == ID) return tower;
            }
            return null;
        }

        public static float GetGridSize()
        {
            return _gridSize;
        }

        // init beermap and nodemap
        private bool[,] beerMap = new bool[39, 36];
        private Object[,] nodeMap = new Object[39, 36];
        PathFindingParameters parameters;
        PathFinder pf;
        List<PathNode> path;

        Point pt_spawnOne = new Point(0, 0);
        Point pt_spawnTwo = new Point(0, 0);
        Point pt_goal = new Point(0, 0);

        float platform_min_z = 0;
        float platform_min_x = 0;
        float platform_max_z = 0;

        public void InitPathFinder()
        {
            GameObject[] platformsMain = GameObject.FindGameObjectsWithTag("Grid");

            foreach (GameObject item in platformsMain)
            {
                if (item.transform.position.z < platform_min_z)
                    platform_min_z = item.transform.position.z;
                else if (item.transform.position.z > platform_max_z)
                    platform_max_z = item.transform.position.z;

                if (item.transform.position.x < platform_min_x)
                    platform_min_x = item.transform.position.x;
            }

            foreach (GameObject item in platformsMain)
            {
                int index_z = (int)Mathf.Round(item.transform.position.z - platform_min_z) / 2;
                int index_x = (int)Mathf.Round(item.transform.position.x - platform_min_x) / 2;
                beerMap[index_x, index_z + 1] = true;
                nodeMap[index_x, index_z + 1] = item;
            }

            // Spawn Plaforms
            GameObject[] platformsStart = GameObject.FindGameObjectsWithTag("StartPlatform");

            foreach (GameObject item in platformsStart)
            {
                int index_x = (int)Mathf.Round(item.transform.position.x - platform_min_x) / 2;
                beerMap[index_x, 35] = true;
                nodeMap[index_x, 35] = item;

                if (item.transform.position.x < 0f)
                    pt_spawnOne = new Point(index_x, 35);
                else
                    pt_spawnTwo = new Point(index_x, 35);
            }

            // Goal Plaforms
            GameObject[] platformsGoal = GameObject.FindGameObjectsWithTag("GoalPlatform");

            foreach (GameObject item in platformsGoal)
            {
                int index_x = (int)Mathf.Round(item.transform.position.x - platform_min_x) / 2;
                beerMap[index_x, 0] = true;
                nodeMap[index_x, 0] = item;
                pt_goal = new Point(index_x, 0);
            }

            //string msg = "";
            //for (int x = 0; x < 39; x++)
            //{
            //  for (int z = 0; z < 36; z++)
            //  {
            //    if (beerMap[x, z] == true)
            //      msg += "*";
            //    else
            //      msg += "-";
            //  }
            //  msg += "\r\n";
            //}
            //Debug.Log(msg);

            GenerateGlobalPaths();
        }

        public bool GenerateGlobalPaths()
        {
            // Delete the old paths from the previous calculations
            Object[] existingPaths = GameObject.FindObjectsOfType(typeof(PathTD));
            foreach (PathTD item in existingPaths)
            {
                Destroy(item.gameObject);
            }

            parameters = new PathFindingParameters(pt_spawnOne, pt_goal, beerMap, nodeMap);
            pf = new PathFinder(parameters);
            Transform spawnOneTf = (nodeMap[pt_spawnOne.X, pt_spawnOne.Y] as GameObject).transform;
            PathTD pathOne = pf.FindPathTD(spawnOneTf, "GlobalPathOne");

            parameters = new PathFindingParameters(pt_spawnTwo, pt_goal, beerMap, nodeMap);
            pf = new PathFinder(parameters);
            Transform spawnTwoTf = (nodeMap[pt_spawnTwo.X, pt_spawnTwo.Y] as GameObject).transform;
            PathTD pathTwo = pf.FindPathTD(spawnTwoTf, "GlobalPathTwo");

            if(pathOne.wpList.Count == 0 || pathTwo.wpList.Count == 0)
            {
                return false;
            }

            // TODO better capsulation from the Spawnmanager
            // and also check the initialization of the spawnmanager
            SpawnManager.instance.defaultPath = pathOne;
            SpawnManager.instance.waveGenerator.pathList.Clear();
            SpawnManager.instance.waveGenerator.pathList.Add(pathOne);
            SpawnManager.instance.waveGenerator.pathList.Add(pathTwo);

            return true;
        }

        public bool UpdatePathMaps(UnitTower tow, bool destroy = false)
        {
            int index_z = (int)Mathf.Round(tow.Platform.transform.position.z - platform_min_z) / 2;
            int index_x = (int)Mathf.Round(tow.Platform.transform.position.x - platform_min_x) / 2;
            beerMap[index_x, index_z + 1] = destroy;
            

            if(!GenerateGlobalPaths())
            {
                beerMap[index_x, index_z + 1] = !destroy;
                GenerateGlobalPaths();
                return false;
            }

            // TODO position of this SpawnManager functions ?
            // Here becazse GenerateGlobalPaths is called at startup
            // Waves not generated at that time
            for (int i = 0; i < SpawnManager.instance.waveList.Count; i++)
            {
                SpawnManager.instance.waveGenerator.UpdateWavePath(SpawnManager.instance.waveList[i]);
            }

            UnitCreep[] creeps = ObjectPoolManager.FindObjectsOfType<UnitCreep>();
            foreach (UnitCreep creep in creeps)
            {
                // Problem with creeps not in the maze -> between start and normal platforms
                // or between normal platforms and goal
                // No new Path, if creep is already past the last tower row
                if (creep.transform.position.z > platform_min_z)
                {
                    int c_z;
                    if (creep.transform.position.z > platform_max_z)
                        c_z = 34;
                    else
                        c_z = Mathf.CeilToInt(creep.transform.position.z - platform_min_z) / 2; // Changed to CeilToInt and not round

                    int c_x = (int)Mathf.Round(creep.transform.position.x - platform_min_x) / 2;
                    parameters = new PathFindingParameters(new Point(c_x, c_z), pt_goal, beerMap, nodeMap);
                    pf = new PathFinder(parameters);

                    PathTD pt = pf.FindPathTD(creep.transform, "CreepCustomPath");

                    creep.SetNewPath(pt);
                }
            }

            //Object[] towers = GameObject.FindObjectsOfType(typeof(UnitTower));

            //Object[] platforms = GameObject.FindObjectsOfType(typeof(PlatformTD));

            //Object[] paths = GameObject.FindObjectsOfType(typeof(PathTD));

            //SpawnManager.ChangeDefaultPath(paths[0] as PathTD);
            return true;
        }
    }







}