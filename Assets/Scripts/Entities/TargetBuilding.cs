using Map;
using UnityEngine;
using UnityEngine.UI;
public class TargetBuilding : MonoBehaviour
{
    [SerializeField]
    float CaptureGaugeStart = 100f;
    [SerializeField]
    float CaptureGaugeSpeed = 1f;
    [SerializeField]
    public int BuildPoints = 5;
    [SerializeField]
    Material BlueTeamMaterial = null;
    [SerializeField]
    Material RedTeamMaterial = null;

    Material NeutralMaterial = null;
    MeshRenderer BuildingMeshRenderer = null;
    Image GaugeImage;
    int[] TeamScore;
    float CaptureGaugeValue;
    ETeam OwningTeam = ETeam.Neutral;
    ETeam CapturingTeam = ETeam.Neutral;
    public ETeam GetTeam() { return OwningTeam; }

    [SerializeField] private Collider selfCollider = null;
    
    [SerializeField]
    private FogElementScene fogElementScene = null;


    #region MonoBehaviour methods
    void Start()
    {
        BuildingMeshRenderer = GetComponentInChildren<MeshRenderer>();
        NeutralMaterial = BuildingMeshRenderer.material;

        GaugeImage = GetComponentInChildren<Image>();
        if (GaugeImage)
            GaugeImage.fillAmount = 0f;
        CaptureGaugeValue = CaptureGaugeStart;
        TeamScore = new int[2];
        TeamScore[0] = 0;
        TeamScore[1] = 0;

        if (selfCollider)
            selfCollider.enabled = true;
    }
    void Update()
    {
        if (CapturingTeam == OwningTeam || CapturingTeam == ETeam.Neutral)
            return;

        CaptureGaugeValue -= TeamScore[(int)CapturingTeam] * CaptureGaugeSpeed * Time.deltaTime;

        GaugeImage.fillAmount = 1f - CaptureGaugeValue / CaptureGaugeStart;

        if (CaptureGaugeValue <= 0f)
        {
            CaptureGaugeValue = 0f;
            OnCaptured(CapturingTeam);
        }
    }
    #endregion

    #region Capture methods
    public void StartCapture(Unit unit)
    {
        if (unit == null)
            return;

        TeamScore[(int)unit.GetTeam()] += unit.Cost;

        if (CapturingTeam == ETeam.Neutral)
        {
            if (TeamScore[(int)GameServices.GetOpponent(unit.GetTeam())] == 0)
            {
                CapturingTeam = unit.GetTeam();
                GaugeImage.color = GameServices.GetTeamColor(CapturingTeam);
            }
        }
        else
        {
            if (TeamScore[(int) GameServices.GetOpponent(unit.GetTeam())] > 0)
            {
                ResetCapture();
                CapturingTeam = unit.GetTeam();
                GaugeImage.color = GameServices.GetTeamColor(CapturingTeam);
            }
        }
    }
    public void StopCapture(Unit unit)
    {
        if (unit == null)
            return;

        TeamScore[(int)unit.GetTeam()] -= unit.Cost;
        if (TeamScore[(int)unit.GetTeam()] == 0)
        {
            ETeam opponentTeam = GameServices.GetOpponent(unit.GetTeam());
            if (TeamScore[(int)opponentTeam] == 0)
            {
                ResetCapture();
            }
            else
            {
                CapturingTeam = opponentTeam;
                GaugeImage.color = GameServices.GetTeamColor(CapturingTeam);
            }
        }
    }
    void ResetCapture()
    {
        CaptureGaugeValue = CaptureGaugeStart;
        CapturingTeam = ETeam.Neutral;
        GaugeImage.fillAmount = 0f;
    }
    void OnCaptured(ETeam newTeam)
    {
        Debug.Log("target captured by " + newTeam.ToString());
        if (OwningTeam != newTeam)
        {
            UnitController teamController = GameServices.GetControllerByTeam(newTeam);
            if (teamController != null)
                teamController.TargetCaptured(BuildPoints);

            if (OwningTeam != ETeam.Neutral)
            {
                // remove points to previously owning team
                teamController = GameServices.GetControllerByTeam(OwningTeam);
                if (teamController != null)
                    teamController.TargetLost(BuildPoints);
            }
            
            if (fogElementScene)
            {
                fogElementScene.teamIndex = (int) newTeam;
                if (!teamController.FogOfWar.clearers.Contains(fogElementScene))
                    teamController.FogOfWar.clearers.Add(fogElementScene);
            }
        }

        ResetCapture();
        OwningTeam = newTeam;
        BuildingMeshRenderer.material = newTeam == ETeam.Blue ? BlueTeamMaterial : RedTeamMaterial;
        fogElementScene.teamIndex = (int) newTeam;
    }
    #endregion
}
