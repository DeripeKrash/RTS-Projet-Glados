using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Map
{
    public class FogOfWar : MonoBehaviour
    {
        [SerializeField] private ComputeShader _computeShader = null;
        [SerializeField] private Renderer _targetRenderer = null;

        [SerializeField] [Range(1, 512)] private int precision = 2;

        [SerializeField] [Min(0.0f)] public float refreshRate = 0.05f;

        [SerializeField] [Min(1)] private int nbTeams = 1;
        private List<RenderTexture> _textures = new List<RenderTexture>();
        [SerializeField] private int _teamToDisplay = 0;
        
        private int _indexKernel;

        public int Tiles => (int)System.Math.Pow(precision * 32, 2);
        
        public int NbTeams => nbTeams;
        
        [HideInInspector]
        public List<FogElement> clearers = new List<FogElement>();

        public struct FogClearData
        {
            public Vector3 position;
            public float clearRadius;
            public float entityRadius;
            public int isVisible;
            public int teamIndex;
        }
        
        public RenderTexture GetFogForTeam(int teamIndex)
        {
            if (teamIndex >= _textures.Count)
                return null;

            return _textures[teamIndex];
        }

        public void SetVisibleFog(int teamIndexToDisplay)
        {
            if (teamIndexToDisplay >= _textures.Count)
                return;

            _teamToDisplay = teamIndexToDisplay;
            
            if (_targetRenderer)
                _targetRenderer.sharedMaterial.mainTexture = _textures[_teamToDisplay];
        }

        private void Start()
        {
            SetupTextures();
            SetupShader();
            StartCoroutine(UpdateForDisplayedTeam());
        }

        static FogClearData[] GetClearersData(List<FogElement> clearersToGet)
        {
            int size = clearersToGet.Count;
            FogClearData[] data = new FogClearData[size];

            for (int i = 0; i < size; i++)
            {
                data[i] = clearersToGet[i].GetData();
            }

            return data;
        }
        
        static FogClearData[] GetClearersData(List<FogElement> clearersToGet, int teamIndex, bool inclusive)
        {
            int size = clearersToGet.Count;
            FogClearData[] data = new FogClearData[size];

            for (int i = 0; i < size; i++)
            {
                if ((teamIndex == clearersToGet[i].GetTeamIndex()) == inclusive)
                {
                    data[i] = clearersToGet[i].GetData();
                }
            }

            return data;
        }

        private void SetupTextures()
        {
            _textures.Clear();

            for (int i = 0; i < nbTeams; i++)
            {
                RenderTexture texture = new RenderTexture(32 * precision, 32 * precision, 1);
                texture.enableRandomWrite = true;
                texture.filterMode = FilterMode.Point;
                texture.format = RenderTextureFormat.R8;
                
                _textures.Add(texture);
            }
        }
        
        public void SetupShader()
        {
            if (!_computeShader)
                return;
            
            // Shader Setup
            _indexKernel = _computeShader.FindKernel("CSFogOfWar");
            
            if (_targetRenderer)
                _targetRenderer.sharedMaterial.mainTexture = _textures[_teamToDisplay];
            
            Vector3 lossyScale = transform.lossyScale;
            Transform targetTransform = _targetRenderer.transform;
            
            _computeShader.SetVector("origin", 
                targetTransform.position - new Vector3(targetTransform.lossyScale.x / 2.0f, 
                                                                    targetTransform.position.y, 
                                                                    lossyScale.y / 2.0f));
            
            _computeShader.SetFloat("pixelSizeX", lossyScale.x / (precision * 32.0f));
            _computeShader.SetFloat("pixelSizeZ", lossyScale.y / (precision * 32.0f));
            
            _computeShader.SetInt("precision", precision);

            // First Calculation
            if (clearers.Count != 0)
                ProcessForDisplayedTeam();
        }

        void UpdateClearersVisiblity(FogClearData[] clearData)
        {
            int clearersCount = clearers.Count;

            for (int i = 0; i < clearersCount; i++)
            {
                bool isVisible = clearData[i].isVisible > 0;
                
                clearers[i].UpdateVisibility(isVisible);
            }
        }

        void ProcessForDisplayedTeam()
        {
            if (clearers.Count == 0)
                return;
            
            if (_targetRenderer)
                _targetRenderer.sharedMaterial.mainTexture = _textures[_teamToDisplay];
            
            float ratio;
            FogClearData[] clearData = ProcessFogForTeam(clearers, _teamToDisplay, out ratio);
            
            UpdateClearersVisiblity(clearData);
        }
        
        /// <summary>
        /// Process the for of war for the selected team
        /// </summary>
        /// <param name="clearers"> List of all clearers</param>
        /// <param name="teamIndex"> Referenced team </param>
        /// <param name="size"> Size of the returned list </param>
        /// <returns> List of all the clearer with their updated state </returns>
        public FogClearData[] ProcessFogForTeam(List<FogElement> clearers, int teamIndex, out float ratio)
        {
            int size = clearers.Count;
            FogClearData[] clearersData = GetClearersData(clearers);

            if (size == 0)
            {
                ratio = 0f;
                return clearersData;
            }
            
            _computeShader.SetTexture(_indexKernel, "Result", _textures[teamIndex]);

            int[] nbVisible = {0};
            ComputeBuffer ratioBuffer = new ComputeBuffer(1, sizeof(int));
            ratioBuffer.SetData(nbVisible);
            _computeShader.SetBuffer(_indexKernel, "ratio", ratioBuffer);

            ComputeBuffer resultBuffer = new ComputeBuffer(size, sizeof(float) * 5 + sizeof(int) * 2);
            resultBuffer.SetData(clearersData);
            _computeShader.SetBuffer(_indexKernel, "clearers", resultBuffer);
            
            _computeShader.SetInt("currentTeam", teamIndex);
            _computeShader.SetInt("clearersNb", size);

            _computeShader.Dispatch(_indexKernel, precision, precision, 1);
            
            resultBuffer.GetData(clearersData);
            ratioBuffer.GetData(nbVisible);
            
            ratio = (float) nbVisible[0] / Tiles;
            resultBuffer.Dispose();
            ratioBuffer.Dispose();

            return clearersData;
        }

        /// <summary>
        /// Process the for of war for the selected team
        /// </summary>
        /// <param name="teamIndex"> Referenced team </param>
        /// <param name="size"> Size of the returned list </param>
        /// <returns> List of all the clearer with their updated state </returns>
        public FogClearData[] ProcessFogForTeam(int teamIndex, out float ratio)
        {
            if (clearers.Count == 0)
            {
                ratio = 0;
                return null;
            }

            return ProcessFogForTeam(clearers, teamIndex, out ratio);
        }
        
        IEnumerator UpdateForDisplayedTeam()
        {
            yield return new WaitForSeconds(refreshRate); //avoid data race at the start
            
            for(;;)
            {
                ProcessForDisplayedTeam();
                yield return new WaitForSeconds(refreshRate);
            };
        }
        
        #if UNITY_EDITOR
        [CustomEditor(typeof(FogOfWar))]
        public class FogOfWarInspector : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                
                FogOfWar fog = target as FogOfWar;
            
                if (!fog)
                    return;
                
                if (GUILayout.Button("Create Tile Grid"))
                {
                    fog.SetupTextures();
                    fog.SetupShader();
                }
                if (GUILayout.Button("Update Tile Grid"))
                {
                    fog.ProcessForDisplayedTeam();
                }
            }
        }
        #endif
    }
}


