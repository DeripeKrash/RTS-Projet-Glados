using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using Color = UnityEngine.Color;

namespace Map
{
    [System.Serializable]
    public class InfluenceMap : MonoBehaviour
    {
        #region Structs
        
        [System.Serializable]
        public struct MapTile
        {
            public int teamIndex;
            public float influencePower;
            public Vector3 position;
        }

        [System.Serializable]
        public struct InfluencerData
        {
            public int teamIndex;
            public Vector3 position;
            public float radius;
        }

        #endregion
        
        #region Parameters
        
        #region Setup
        
        [SerializeField] private int baseCost = 0;
        [SerializeField] private int unreachableCost = -1;

        [SerializeField] private int gridSizeX = 100;
        [SerializeField] private int gridSizeZ = 100;
       
        [SerializeField] private int squareSize = 1;
        
        [SerializeField] private int maxHeight = 10;
        [SerializeField] private int maxWalkableHeight = 5;
        
        [SerializeField] private List<Color> teamColors = new List<Color>();
        
        [SerializeField] private float refreshRate = 1f;
        
        #endregion
        
        #region Grid Data
        
        private Vector3 _gridStartPos = Vector3.zero;
        [SerializeField] [HideInInspector] private int _nbTilesX = 0;
        [SerializeField] [HideInInspector] private int _nbTilesZ = 0;

        [SerializeField] [HideInInspector] private MapTile[] _tiles = null;
        
        #endregion
        
        #region Compute Shader
        [SerializeField] private ComputeShader shader = null;

        private int _indexKernelInfluence;
        
        private RenderTexture texture = null;
        private int _indexKernelDebugMap;
        [SerializeField] private Renderer _renderer = null;
        
        #endregion

        #region Influencer
        
        private List<Influencer> _influencers = new List<Influencer>();
        
        #endregion

        #region Getter


        public int NbTileX => _nbTilesX;
        public int NbTileZ => _nbTilesZ;
        public Vector2Int NbTile => new Vector2Int(_nbTilesX, _nbTilesZ);

        public int Count => _nbTilesX * _nbTilesZ;

        public RenderTexture Texture => texture;

        #endregion

        #region Debug

        [SerializeField] private bool displayAllNodes = false;

        #endregion
        
        #endregion

        #region Grid
        public void CreateGrid()
	    {
            _gridStartPos = transform.position + new Vector3(-gridSizeX / 2f, 0f, -gridSizeZ / 2f);

		    _nbTilesX = gridSizeX / squareSize;
		    _nbTilesZ = gridSizeZ / squareSize;

            _tiles = new MapTile[_nbTilesX * _nbTilesZ];

            
		    for(int i = 0; i < _nbTilesZ; i++)
		    {
			    for(int j = 0; j < _nbTilesX; j++)
			    {
				    MapTile mapTile = new MapTile();
                    Vector3 tilePos = _gridStartPos + new Vector3((j + 0.5f) * squareSize, 0f, (i + 0.5f) * squareSize);

				    int Weight = 0;
				    RaycastHit hitInfo = new RaycastHit();

                    // Compute tile height
                    if (Physics.Raycast(tilePos + Vector3.up * maxHeight, Vector3.down, out hitInfo, maxHeight + 1, 1 << LayerMask.NameToLayer("Floor")))
                    {
                        if (Weight == 0)
                            mapTile.teamIndex = hitInfo.point.y >= maxWalkableHeight ? unreachableCost : baseCost;
                        tilePos.y = hitInfo.point.y;
                    }

                    //mapTile.teamIndex = Weight ;
                    mapTile.position = tilePos;
                    _tiles[j * _nbTilesZ + i] = mapTile;
			    }
		    }
            
            SetupComputeShader();
        }
        
        protected bool RaycastNode(Vector3 nodePos, string layerName, out RaycastHit hitInfo)
        {
            int layer = 1 << LayerMask.NameToLayer(layerName);

            return (Physics.Raycast(nodePos - new Vector3(0f, 0f, squareSize / 2f) + Vector3.up * 5, Vector3.down,
                        out hitInfo, maxHeight + 1, layer)
                    || Physics.Raycast(nodePos + new Vector3(0f, 0f, squareSize / 2f) + Vector3.up * 5, Vector3.down,
                        out hitInfo, maxHeight + 1, layer)
                    || Physics.Raycast(nodePos - new Vector3(squareSize / 2f, 0f, 0f) + Vector3.up * 5, Vector3.down,
                        out hitInfo, maxHeight + 1, layer)
                    || Physics.Raycast(nodePos + new Vector3(squareSize / 2f, 0f, 0f) + Vector3.up * 5, Vector3.down,
                        out hitInfo, maxHeight + 1, layer));
        }
        
        public bool IsPosValid(Vector3 pos)
        {
            return pos.x > (-gridSizeX / 2f) 
                   && pos.x < (gridSizeX / 2f)
                   && pos.z > (-gridSizeZ / 2f) 
                   && pos.z < (gridSizeZ / 2f);
        }

        // Converts world 3d pos to tile 2d pos
        protected Vector2Int GetTileCoordFromPos(Vector3 pos)
	    {
            Vector3 realPos = pos - _gridStartPos;
            Vector2Int tileCoords = Vector2Int.zero;
            tileCoords.x = Mathf.FloorToInt(realPos.x / squareSize);
            tileCoords.y = Mathf.FloorToInt(realPos.z / squareSize);
		    return tileCoords;
	    }

        public MapTile GetTile(Vector3 pos)
        {
            return GetTile(GetTileCoordFromPos(pos));
        }

        public MapTile GetTile(Vector2Int pos)
        {
            return GetTile(pos.x, pos.y);
        }

        public MapTile GetTile(int x, int y)
        {
            if (x < 0 || y < 0 || x > _nbTilesX - 1 || y > _nbTilesZ - 1)
                return new MapTile();
            
            int index = y * _nbTilesZ + x;
            if (index >= Count || index < 0)
                return new MapTile();

            return _tiles[index];
        }

        public bool IsTileWall(MapTile mapTile)
        {
            return mapTile.teamIndex == unreachableCost;
        }

        public List<MapTile> GetTileWithThreshold(float threshold, int teamIndex)
        {
            List<MapTile> sorted = new List<MapTile>();

            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i].teamIndex == teamIndex && _tiles[i].influencePower >= threshold - float.Epsilon)
                    sorted.Add(_tiles[i]);
            }
 
            return sorted;
        }

        public float GetTeamMaxInfluence(int teamIndex)
        {
            int   tilesLen = _tiles.Length;
            float max      = float.MinValue;

            for (int i = 0; i < tilesLen; i++)
            {
                max = Mathf.Max(max, _tiles[i].influencePower);
            }
 
            return max;
        }

        #endregion

        #region Shader
        
        private void SetupComputeShader()
        {
            _indexKernelInfluence = shader.FindKernel("CSInfluenceMap");
            _indexKernelDebugMap = shader.FindKernel("CSMap");
            
            texture = new RenderTexture(NbTileX, NbTileZ, 1);
            texture.enableRandomWrite = true;
            texture.filterMode = FilterMode.Point;
            
            UseComputeShaderDemoMap();
            
            if (_renderer)
                _renderer.sharedMaterial.mainTexture = texture;
            
            SetShaderColor();
        }
        void UseComputeShaderInfluence()
        {
            if (_influencers.Count == 0)
                UseComputeShaderDemoMap();
            
            shader.SetTexture(_indexKernelInfluence, "renderTexture", texture);
            shader.SetInt("nbTileX", _nbTilesX);
            shader.SetInt("nbTileY", _nbTilesZ);
            
            ComputeBuffer tileBuffer = new ComputeBuffer(Count, sizeof(int) + sizeof(float) * 4);
            tileBuffer.SetData(_tiles);
            shader.SetBuffer(_indexKernelInfluence, "tiles", tileBuffer);

            shader.SetInt("influencerNumber", _influencers.Count);
            
            InfluencerData[] influencerData = GetInfluenceData();
            ComputeBuffer influenceBuffer = new ComputeBuffer(_influencers.Count, sizeof(int) + sizeof(float) * 4);
            influenceBuffer.SetData(influencerData);
            shader.SetBuffer(_indexKernelInfluence, "influencers", influenceBuffer);

            shader.Dispatch(_indexKernelInfluence, _nbTilesX / 32 + 1, _nbTilesZ / 32 + 1, 1);

            tileBuffer.GetData(_tiles);
            
            influenceBuffer.Dispose();
            tileBuffer.Dispose();
        }
        
        void UseComputeShaderDemoMap()
        {
            shader.SetTexture(_indexKernelDebugMap, "renderTexture", texture);
            shader.SetInt("nbTileX", _nbTilesX);
            shader.SetInt("nbTileY", _nbTilesZ);
            
            ComputeBuffer tileBuffer = new ComputeBuffer(Count, sizeof(int) + sizeof(float) * 4);
            tileBuffer.SetData(_tiles);
            shader.SetBuffer(_indexKernelDebugMap, "tiles", tileBuffer);
            
            shader.Dispatch(_indexKernelDebugMap, _nbTilesX / 32 + 1, _nbTilesZ / 32 + 1, 1);

            tileBuffer.Dispose();
        }

        private void SetShaderColor()
        {
            Vector4[] colors = new Vector4[teamColors.Count];
            
            for (int i = 0; i < teamColors.Count; i++)
            {
                colors[i] = teamColors[i];
            }
            
            shader.SetVectorArray("colors", colors);

        }
        #endregion

        #region Influencer
        
        private InfluencerData[] GetInfluenceData()
        {
            InfluencerData[] influencers = new InfluencerData[_influencers.Count];

            for (int i = 0; i < _influencers.Count; i++)
            {
                influencers[i] = _influencers[i].GetData();
            }
            
            return influencers;
        }
        
        public void AddInfluencer(Influencer Influencer)
        {
            if (!_influencers.Contains(Influencer))
            {
                _influencers.Add(Influencer);
            }
        }

        public void RemoveInfluencer(Influencer Influencer)
        {
            _influencers.Remove(Influencer);
        }

        #endregion
        
        #region Gizmos
        private void OnDrawGizmos()
	    {
            if (displayAllNodes)
            {
		        for(int i = 0; i < Count; i++)
		        {
                    MapTile mapTile = _tiles[i];
                    Gizmos.color = IsTileWall(mapTile) ? Color.white : Color.Lerp(Color.black, teamColors[mapTile.teamIndex], mapTile.influencePower) ;
                    Gizmos.DrawCube(mapTile.position, Vector3.one);
                }
            }
        }
        #endregion

        #region MonoBehavior

        private void Start()
        {
            SetupComputeShader();
            StartCoroutine(Process());
        }
        
        IEnumerator Process()
        {
            yield return new WaitForSeconds(refreshRate); //avoid data race at the start 
            
            for(;;)
            {
                UseComputeShaderInfluence();
                yield return new WaitForSeconds(refreshRate);
            }
        }
        
        
        
        #endregion

    }

    #region Editor
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(InfluenceMap))]
    public class InfluenceMapInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            InfluenceMap map = target as InfluenceMap;
            
            if (!map)
                return;

            if (GUILayout.Button("Create Tile Grid"))
            {
                map.CreateGrid();
            }
        }
    }
    #endif
    
    #endregion
}




